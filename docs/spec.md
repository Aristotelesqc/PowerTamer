# PowerTamer: first design notes

Status: draft, 2026-11-27. Based on [the market research](research/2026-11-27-market-research.md)
and on the [PA278QV guard prototype](../prototypes/pa278qv-guard/).

## Principles

1. **Start from Windows settings.** Read timeouts and plan values from the active power plan at the
   moment they matter; never hardcode a duration. PowerTamer adds tiers on top, it does not replace
   the plan.
2. **One owner per setting.** Before touching a setting, check whether another tool owns it
   (Process Lasso first). If it does, leave it alone and say so in the UI.
3. **Reachable at every tier.** No S3, S0ix or Away Mode. Network, remote sessions and running
   processes must keep working.
4. **No admin for the core.** Keep-awake, tiers, DDC/CI and plan switching run as the user.
   Anything that needs elevation lives in an optional module.
5. **Lightweight native exe.** Rust. No service unless a module needs one.
6. **No fixed-interval polling.** React to Windows notifications. Idle time sends no event, so
   schedule the next check from the time left before the next tier instead of checking every second.
   Under revision in ADR 0004 (#44): Windows sends no load notification, so detecting sustained
   load needs bounded, measured, adaptive sampling.
7. **Detect, never assume the machine.** Topology, devices, vendors and capabilities are read at
   runtime. Hardware named in this document comes from the reference PC and is an example or a
   measurement, never a default.

## Architecture

Three executables; only the first one runs all the time.

| Executable | Runs | Role |
|---|---|---|
| `powertamer` (core) | Always, as the user | State, rules, tiers, keep-awake, DDC/CI, plan switching. Owns the tray icon: native `Shell_NotifyIcon` with a native popup menu (current tier, keep-awake toggle, open settings, quit) |
| `powertamer-ui` | Only while the settings window is open | Tauri 2 + WebView2, Svelte 5, plain CSS ([ADR 0001](adr/0001-ui-stack.md)). Holds no logic: its Rust side is the only client of the core's named pipe and exposes Tauri commands and a telemetry channel to the page. Closing the window ends the process; it has no tray icon of its own |
| `powertamer-elevated` | On demand, optional | Admin-only actions for any module that needs them (writing plan values for the CPU module, GPU limits). Started through a scheduled task created once with highest privileges, so there is one UAC prompt at setup and none after. Security model in ADR 0007 (#47) |

Rules for the UI process:
- Local content only, no remote pages, strict CSP: a web view that can reach a process able to
  change power settings must load nothing but its own files.
- Its cost (WebView2 processes, roughly 100–200 MB) only exists while the window is open.
- Keeping the UI behind the pipe means its technology can change without touching the core.
- Look and feel of Windows 11 Settings: Mica, native title bar, system accent and theme, Segoe UI
  Variable, high-contrast support. Details and rejected options in ADR 0001.

## Tiers

| Tier | Meaning | Entered when | Effect (first idea) |
|---|---|---|---|
| Active | Someone is at the PC | Local input or unlock | Nothing; the user's plan as is |
| Away | Nobody at the PC, normal compute | Idle time or session lock; or sustained load while in Drowsy | DDC monitors off on their own timeouts; others left to Windows |
| Drowsy | Nobody at the PC, low load | Away for a while with low load | CPU drowsy profile, GPU module limits, keep-awake still held |

Physical presence, display state, compute activity, remote sessions and policy are separate
inputs. Local input or unlock goes straight back to Active. Sustained load (a remote agent that
starts working) moves Drowsy back to Away: full compute, monitors still off. Entry is slow
(thresholds, dwell time), exit is fast, and stale or missing evidence resolves toward normal
performance. The full transition contract is ADR 0004 (#44).

## Modules

### Keep-awake
`PowerCreateRequest` + `PowerSetRequest(PowerRequestSystemRequired)` with a reason string, rather
than `SetThreadExecutionState`, so `powercfg /requests` shows why the PC stays up.

### Monitors
- Identify each monitor through `EnumDisplayDevices(..., EDD_GET_DEVICE_INTERFACE_NAME)`, not by
  display index. The EDID model ID (for example `AUS2700`) keys the quirks; the instance identity
  keys per-monitor settings, so two identical monitors are configured separately.
- Power over DDC/CI with VCP `0xD6` (1 = on, 4 = standby, 5 = off); retry, and don't filter on
  `DISPLAY_DEVICE_ACTIVE` (a monitor turned off over DDC must still be found to turn it back on).
- Events, no polling: `WTSRegisterSessionNotification` (lock/unlock) and
  `RegisterPowerSettingNotification(GUID_CONSOLE_DISPLAY_STATE)` (displays off/on).
- Waking at the lock screen is a per-monitor option, not a global behaviour: for example when a
  Windows Hello webcam is plugged into the monitor's USB hub, which has no power while the monitor
  is off over DDC. If nobody unlocks, turn it off again before the lock-screen timeout.
- A quirks table per model, as data: for example "turn off over DDC before Windows does, or it
  hot-plug loops on DisplayPort" (seen on the reference PC's PA278QV).
- Every power change is an owned, journaled mutation (#36); DDC writes never block the message
  loop.

### CPU drowsy profile
Candidate settings: `PERFBOOSTMODE`, `PERFEPP`, `CPMINCORES`, `PROCTHROTTLEMAX`, and on hybrid
CPUs only `HETEROPOLICY`, `SCHEDPOLICY` / `SHORTSCHEDPOLICY`. Which ones apply is detected from the
CPU; symmetric Intel and AMD CPUs are supported. Preferred over a hard frequency cap, because cores
still wake on demand. Each setting is a hypothesis to measure (energy to completion, resume
latency), not an assumed gain.

Open points:
- The drowsy plan is derived from the user's current plan: only the CPU values PowerTamer owns
  change (#21). Switching between existing plans works without admin; which writes need admin is
  measured, not assumed.
- `PowerSetActiveOverlayScheme` only applies when the base plan is Balanced. On a High Performance
  base, the overlay route does not exist.

### GPU module (optional, admin)
- NVIDIA first: read power and P-state through NVML (no admin needed). Other GPUs show an explicit
  "unsupported" state until a backend exists.
- Lowering the refresh rate in Away/Drowsy can let the GPU leave P0 with several monitors on. It is
  vendor-independent (`SetDisplayConfig`, no elevation).
- Writing limits goes through the elevated helper, and only if measurements (#37) show a real gain.
- Measured on the reference PC: with all displays off, an RTX 3080 Ti already idles at P8, about
  9.4 W. The module matters mostly for "idle with the screens on", less at night.

## Coexistence with Process Lasso

Process Lasso can switch the active power plan through IdleSaver, Performance Mode, per-app and
load-dependent power profiles, and has per-process anti-sleep rules. Two tools switching the plan
will fight.

- Detect Lasso's active policies from its actual, current configuration (the documented INI and
  its location, governor and service mode, registry policies where relevant), not only from
  `ProcessGovernor.exe` (#22). Its selective JSON rule export is for deliberate migration (#40),
  not for detection: a saved export may be stale or incomplete.
- If Lasso manages plans, or the state is uncertain, PowerTamer does not switch plans and says so;
  the user can change Lasso's settings.
- Any external change of the active plan, from Lasso or another tool, is reconciled through the
  ownership journal (#36).
- For now, leave to Lasso what it already does well: ProBalance, affinities, permanent EcoQoS per
  app. Taking these over is a later, opt-in workstream (#40, ADR 0008).

## Non-goals

- Real sleep, hibernation, Away Mode.
- A kernel driver for power telemetry. CPU package power may not be readable without one; if EMI
  counters are absent, show estimates or nothing.
- Replacing Process Lasso. The owner has adopted the opposite long-term direction (#40); this line
  changes with ADR 0008 (#48). Until then, coexistence is the default.

## Research claims still to check

- That VCP `0xD6 = 5` can leave some ASUS ProArt monitors unable to wake (weak source; our own
  PA278QV test woke fine).
- That the EIZO CS2740 also hot-plug loops (never seen on the reference PC).
- Version numbers and release dates quoted in the research.
