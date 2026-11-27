# ADR 0001: Settings window stack

- Status: accepted
- Date: 2026-11-27
- Research: [UI stack decision](../research/2026-11-27-ui-stack-decision.md) (Gemini 4 Neon, with
  caller verification notes); fact-check by Gemini 4 Hydrogen posted on
  [PR #2](https://github.com/Aristotelesqc/PowerTamer/pull/2#issuecomment-7226999777).

## Context

`powertamer-ui` is the settings window: started on demand from the tray menu, fully closed when
the window closes, no logic of its own. It must look like Windows 11 Settings, open fast, cost
little while open with live telemetry, and be accessible (keyboard, Narrator, high contrast). Most
code will be written by AI agents.

## Decision

| Concern | Choice |
|---|---|
| Shell | Tauri 2 on evergreen WebView2, Windows 11 only |
| Framework | Svelte 5 in runes mode (`runes: true`), TypeScript strict, Vite |
| Styling | Plain modern CSS: cascade layers, custom properties, nesting, container queries, `light-dark()`, `forced-colors`. No Tailwind |
| Components | About 10–12 in-house Svelte components on native HTML (`<dialog>`, `[popover]`, `<select>` with `appearance: base-select`, `<input type="range">`, `<details>`), styled after WinUI 3 theme resources |
| Icons | Fluent UI System Icons, only the SVGs used |
| Charts | uPlot, wrapped in `<figure>` with a text table and a pause control |
| Windows integration | Mica backdrop, native title bar (`decorations: true`), accent ramp read in Rust (WinRT `UISettings`) and pushed as custom properties, system font Segoe UI Variable |
| IPC | The UI's Rust side is the only client of the core's named pipe; Tauri commands for settings, a Tauri `Channel` for telemetry |
| Security | Strict local CSP with Trusted Types, no remote content, minimal capabilities, `withGlobalTauri: false`. No `{@html}` in Svelte templates |
| Design language | Root `DESIGN.md` in Google's DESIGN.md format, using its canonical front matter and sections so `@google/design.md lint` passes; Fluent 2 as on Windows 11 |

## Rejected

- React 19 and Fluent UI React v9: heaviest runtime, CSS-in-JS, targets Fluent 2 for the web.
- Fluent UI Web Components v3, Lit: Shadow DOM breaks ID-based ARIA references; Fluent 2 for the
  web, not WinUI 3.
- Tailwind CSS 4 (initial proposal): native controls are styled through pseudo-elements
  (`::picker(select)`, `::backdrop`, slider parts), which Tailwind expresses poorly; agents drift to
  its default palette. Note: the research's claim that Tailwind's `@theme` cannot follow a runtime
  accent is wrong; the decision rests on the other reasons.
- Custom title bar: breaks Windows 11 Snap Layouts.

## Consequences

- A CSS architecture with reusable classes and enforced rules is needed (research pending; its
  rules go into `AGENTS.md` with a follow-up ADR).
- `require-trusted-types-for 'script'` needs Svelte's `fragments: 'tree'` (Svelte 5.33+). Svelte's
  `{@html}` still writes `innerHTML`, so it is not used; if it ever becomes necessary, it needs a
  named, audited Trusted Types policy instead of `trusted-types 'none'`.
- Tauri injects its bootstrap through WebView2's `AddScriptToExecuteOnDocumentCreated` and only adds
  hashes to `script-src`/`style-src`; whether its scripts or `@tauri-apps/api` touch a Trusted Types
  sink is unverified and must be tested at the first prototype.
