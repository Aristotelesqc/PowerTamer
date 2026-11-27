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
5. **Lightweight native exe.** Likely Rust. No service unless a module needs one.

## Tiers

| Tier | Entered when | Effect (first idea) |
|---|---|---|
| Active | Input, or sustained load | Nothing; the user's plan as is |
| Away | Idle time or session lock | DDC monitors off on their own timeouts; others left to Windows |
| Drowsy | Away for a while, low load | CPU drowsy profile, GPU module limits, keep-awake still held |

Leaving a tier: input or unlock goes straight back to Active. Sustained load in Drowsy goes back to
Away, so a remote agent that starts working gets full CPU without the monitors turning on.

## Modules

### Keep-awake
`PowerCreateRequest` + `PowerSetRequest(PowerRequestSystemRequired)` with a reason string, rather
than `SetThreadExecutionState`, so `powercfg /requests` shows why the PC stays up.

### Monitors
- Identify each monitor by its EDID hardware ID (e.g. `AUS2700`) through
  `EnumDisplayDevices(..., EDD_GET_DEVICE_INTERFACE_NAME)`, not by display index.
- Power over DDC/CI with VCP `0xD6` (1 = on, 4 = standby, 5 = off); retry, and don't filter on
  `DISPLAY_DEVICE_ACTIVE` (a monitor turned off over DDC must still be found to turn it back on).
- Events, no polling: `WTSRegisterSessionNotification` (lock/unlock) and
  `RegisterPowerSettingNotification(GUID_CONSOLE_DISPLAY_STATE)` (displays off/on).
- Turn a monitor back on as soon as the displays come back on, even at the lock screen: a Windows
  Hello webcam plugged into the monitor's USB hub has no power while the monitor is off over DDC.
  If nobody unlocks, turn it off again before the lock-screen timeout.
- A quirks table per model: for example "turn off over DDC before Windows does, or it hot-plug
  loops on DisplayPort".

### CPU drowsy profile
Settings that matter on Intel hybrid CPUs: `PERFBOOSTMODE`, `PERFEPP`, `HETEROPOLICY`,
`SCHEDPOLICY` / `SHORTSCHEDPOLICY`, `CPMINCORES`, `PROCTHROTTLEMAX`. Preferred over a hard
frequency cap, because cores still wake on demand.

Open points:
- Switching between existing plans works without admin; writing plan values probably needs admin.
  Option: create the drowsy plan once (elevated), then only switch to it.
- `PowerSetActiveOverlayScheme` only applies when the base plan is Balanced. On a High Performance
  base, the overlay route does not exist.

### GPU module (optional, admin)
- Read power and P-state through NVML (no admin needed).
- Write limits through an elevated helper: a scheduled task created once with highest privileges,
  started on demand, so there is one UAC prompt at setup and none after.
- Lowering the refresh rate in Away/Drowsy can let the GPU leave P0 with several monitors on.
- Measured on the reference PC: with all displays off, an RTX 3080 Ti already idles at P8, about
  9.4 W. The module matters mostly for "idle with the screens on", less at night.

## Coexistence with Process Lasso

Process Lasso can switch the active power plan through IdleSaver, Performance Mode and per-app
power profiles. Two tools switching the plan will fight.

- Detect `ProcessGovernor.exe` and read its config.
- If Lasso manages plans, PowerTamer does not switch plans; it offers to take over idle handling
  (the user disables IdleSaver) or stays out of the CPU tier.
- Leave to Lasso what it already does well: ProBalance, affinities, permanent EcoQoS per app.

## Non-goals

- Real sleep, hibernation, Away Mode.
- A kernel driver for power telemetry. CPU package power may not be readable without one; if EMI
  counters are absent, show estimates or nothing.
- Replacing Process Lasso.

## Research claims still to check

- That VCP `0xD6 = 5` can leave some ASUS ProArt monitors unable to wake (weak source; our own
  PA278QV test woke fine).
- That the EIZO CS2740 also hot-plug loops (never seen on the reference PC).
- Version numbers and release dates quoted in the research.
