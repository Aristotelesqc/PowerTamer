Technical design task: define the structure of PowerTamer's native CSS system for its settings window. Search the web where useful, read the supplied Modern Web Guidance guides (below), and write the final report in French (keep code, file names, class names, CSS and API names in English). Cite a source URL for every factual claim about a tool or platform feature; say clearly when something could not be verified. You must decide: give one structure, not a survey of methodologies.

Allowed actions: web searches, reading web pages, and reading files in this repository. Do NOT run terminal commands, do NOT use MCP tools, and do NOT create, modify or delete any file. Repository revision: main at 76d23e9.

Return the complete report directly in your final response. Do not write it to an artifact, a brain/ file or any other file, and do not answer with a summary that points to one: the final chat response must contain the whole report.

## Context
Read first: `docs/spec.md`, then `docs/research/2026-11-27-ui-stack-decision.md` (the accepted UI stack decision, including the caller's verification notes at its top).

Accepted decisions you build on, not reopen:
- `powertamer-ui`: Tauri 2 + WebView2 (evergreen Chromium only, Windows 11), Svelte 5 in runes mode, TypeScript, Vite.
- No Tailwind. Plain modern CSS: cascade layers, custom properties, native nesting, container queries, `light-dark()`, `@media (forced-colors: active)`.
- Fluent 2 as on Windows 11 Settings (WinUI 3 look, Mica backdrop), Segoe UI Variable (system font), Fluent UI System Icons, accent color ramp pushed at runtime from Rust as custom properties.
- About 10–12 in-house components on native HTML (`SettingsCard`, `SettingsExpander`, `Button`, `ToggleSwitch`, `Select`, `Slider`, `NumberInput`, `Dialog`, `PopupMenu`, `InfoBar`, `NavigationView`, `TelemetryChart` with uPlot).
- A root `DESIGN.md` in Google's DESIGN.md format describing the design language.
- Code is written mostly by AI coding agents. The owner's requirement, in his words: native CSS needs "an engine with well-defined reusable classes; it must be clean".

## Modern Web Guidance
Google Chrome's catalogue of current web-platform practices. The caller retrieved the relevant guides into `.agy-runs/mwg/` (file name = guide id). The most relevant here: css, css-layout, design-token-reactivity, reduce-style-repetition, selector-atrule-combinations, usage-aware-component-variations, size-aware-styling, responsive-design, color, dark-mode, typography, respect-os-text-scale, browser-ui-theming, brand-consistent-forms, branded-select-styling, checkbox, slider, switch, card, dialog, menu, icons, motion, natural-interactions, scoped-component-transitions, content-based-styling, progress, accessibility, performance. Cite the guide ids you relied on. Target is evergreen WebView2 only: say where that lets us skip a guide's fallback.

## Questions to decide
1. Where reusable styles live. Svelte scopes `<style>` per component. Decide what is global (tokens, base, reusable component classes, a bounded set of utilities) and what stays in component `<style>` blocks, so there is exactly one place for each kind of rule and no duplication between a Svelte component and a global class.
2. Directory and file structure under `apps/ui/src/styles/` (or your better path), the single `@layer` order statement, and what each layer may contain.
3. Token tiers: names and tiers (primitive / semantic / component, at most what is needed), how WinUI 3 theme resources map to them, how the runtime accent ramp and light/dark/forced-colors plug in, spacing, radius, elevation, typography ramp and motion tokens. Give the naming grammar and a representative excerpt of `tokens.css`.
4. Class naming convention for reusable classes and their variants and states. Decide between classes, data attributes and ARIA/native state (`:checked`, `[aria-expanded]`, `:disabled`, `:user-invalid`, `:popover-open`) for each kind of state, so state is never duplicated in a class.
5. Utilities: whether to have any, and if so the closed list (e.g. layout stack/cluster, visually-hidden), and the rule that stops the list from growing into a Tailwind clone.
6. Worked examples, complete CSS and Svelte markup, for `Button` (accent and standard variants), `ToggleSwitch` (`<input type="checkbox" role="switch">`) and `SettingsCard`, showing tokens, layers, states, focus ring, forced colors and reduced motion.
7. Enforcement so agents keep it clean: stylelint configuration (rules that forbid raw colors and magic numbers outside tokens, unknown layers, `!important`, deep selectors, ID selectors, etc.), any Svelte-specific checks, and how `DESIGN.md` and `tokens.css` stay in sync (which one is the source, and how a check detects drift). Prefer standard, maintained tools; give versions only if verified.
8. A short rule list (max ~12 lines) ready to paste into `AGENTS.md` for anyone writing CSS in this repository.

## Output
- One section per question, decision first, then reasons and sources.
- A "Modern Web Guidance" section: each guide id used and what it changed.
- Risks and open points.
- A final summary table: concern → decision → main reason → rejected alternatives.
