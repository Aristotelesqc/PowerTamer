# PowerTamer

A tiered power manager for Windows 11 desktops that must stay reachable 24/7.

Windows offers two real states: awake, or asleep. Sleep cuts remote sessions, so a PC that hosts
remote work (coding agents, SSH, remote desktop) ends up fully awake all night, with only the
monitors turned off. PowerTamer adds the states in between.

**Status:** design stage. Nothing to install yet.

## What it will do

- **Keep-awake, built in.** Replaces PowerToys Awake, with readable reasons (`powercfg /requests`).
- **Tiers between awake and asleep.** Active → away → drowsy, driven by idle time, session lock,
  input, sustained load and remote connections. Drowsy stays reachable but draws as little as
  possible, and wakes fast when load or input returns.
- **Per-monitor handling.** Turns off the monitors that support DDC/CI (VCP `0xD6`) on their own
  timeouts, leaves the others to Windows standby, and applies per-model quirk rules.
- **CPU limits when idle.** A drowsy processor profile (boost off, EPP towards efficiency, E-core
  preference) that still lets cores wake on demand.
- **Optional GPU module.** NVIDIA idle limits and refresh-rate lowering, through an elevated helper
  set up once, so there is no UAC prompt at every use.
- **Plays well with Process Lasso.** Each setting has a single owner; see [the spec](docs/spec.md).

## Repository layout

| Path | Content |
|---|---|
| [`AGENTS.md`](AGENTS.md) | Rules for contributors and agents: sources of truth, pull requests, issues |
| [`docs/spec.md`](docs/spec.md) | Design notes: tiers, modules, coexistence rules |
| [`docs/adr/`](docs/adr/) | Architecture decisions (settings window stack, CSS architecture, interface language) |
| [`docs/research/`](docs/research/) | Research inputs (in French) with their prompts: market, UI stack, CSS architecture |
| [`prototypes/pa278qv-guard/`](prototypes/pa278qv-guard/) | The working prototype that started the project |

## Origin

An ASUS PA278QV on DisplayPort hot-plug loops every ~8.7 s once Windows turns the displays off,
and each reconnect makes Explorer and DWM hold memory. Powering that one monitor off over DDC/CI
before Windows does stops the loop. That fix, a monitor quirk handled per model on a PC that never
sleeps, is the seed of PowerTamer.
