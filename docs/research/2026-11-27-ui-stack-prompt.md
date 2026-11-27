Technical decision task: choose the UI stack for PowerTamer's settings window. Search the web thoroughly, read the supplied Modern Web Guidance guides (below), and write the final report in French (keep product, library and API names in English). Cite a source URL for every factual claim about a library or platform feature; say clearly when something could not be verified. You must decide: for each question, give one recommendation, not a survey.

Allowed actions: web searches, reading web pages, and reading files in this repository. Do NOT run terminal commands, do NOT use MCP tools, and do NOT create, modify or delete any file. Repository revision: main at 79d526d.

Return the complete report directly in your final response. Do not write it to an artifact, a brain/ file or any other file, and do not answer with a summary that points to one: the final chat response must contain the whole report.

## Context
Read `docs/spec.md` and `README.md` in this repository first. In short:
- PowerTamer is an open-source Windows 11 power manager (keep-awake, tiered idle states, per-monitor DDC/CI, CPU/GPU idle limits).
- Three executables: a Rust core (always running, owns a native tray icon), an optional elevated helper, and `powertamer-ui`, the settings window.
- `powertamer-ui` is a Tauri 2 app on WebView2, started on demand and fully closed when the window closes. It holds no logic: it reads and changes the core's state over a local channel. It shows settings (tiers, timeouts, per-monitor rules, Process Lasso coexistence, optional GPU module) and live telemetry (current tier, tier history, CPU/GPU power in watts over time).
- Runtime is evergreen WebView2 (Chromium) only. No other browser needs to be supported.
- Look and feel: native Windows 11, Fluent 2, like the Windows 11 Settings app: Mica backdrop, Segoe UI Variable (system font, not bundled), follows the system light/dark theme and accent color, Fluent UI System Icons.
- The user's initial proposal: Tailwind CSS 4 for styling, Fluent 2 as the design language written down in a DESIGN.md file (Google's DESIGN.md format for agents), Modern Web Guidance for HTML/CSS/JS practices. Challenge it where it is wrong.
- Code will be written mostly by AI coding agents; agent-friendliness matters.
- Priorities, in order: native Windows 11 look, small and fast (window open time, bundle size, CPU while open), accessibility (keyboard, Narrator, high contrast), maintainability.

## Modern Web Guidance
Modern Web Guidance is Google Chrome's catalogue of current web-platform best practices. The caller already ran its searches and retrieved the relevant guides into `.agy-runs/mwg/` in this repository, one file per guide id (file name = id): html, css, accessibility, color, performance, security, dialog, switch, branded-select-styling, browser-ui-theming, dark-mode, brand-consistent-forms, menu, web-components, styling-web-components, motion, icons, forms, respect-os-text-scale, typography, trusted-types. Read the ones relevant to each question. Use them to decide which native HTML/CSS features replace component-library widgets, and cite the guide ids you relied on. The guides assume "Baseline widely available" by default; our target is evergreen WebView2 only, so say where that lets us skip a guide's fallback.

## Questions to decide
1. UI framework: none (vanilla TS + web components), Lit, Svelte 5, SolidJS, Preact, React 19, Vue 3. Judge on bundle size, startup time, CPU while idle with live charts, accessibility, fit with native HTML features (popover, dialog, switch, customizable select), Tauri 2 support, and how reliably AI agents write it. Answer explicitly: is React worth it here?
2. Styling: Tailwind CSS 4, plain modern CSS (custom properties, cascade layers, nesting, container queries), or both. How Fluent 2 tokens should be expressed (Tailwind 4 `@theme`, CSS custom properties, `@fluentui/tokens`). Any Tailwind 4 pitfalls with Tauri/Vite or with native elements.
3. Fluent implementation: Fluent UI Web Components (v3), Fluent UI React v9, another Fluent-styled library, or own components on Fluent 2 tokens. Check their current state, size and accessibility. Recommend one.
4. Charts for live telemetry (a few series, updates about once per second, must stay cheap): uPlot, Chart.js, ECharts, Observable Plot, or hand-made SVG/canvas.
5. Native Windows integration in Tauri 2: Mica backdrop with a transparent WebView2 (support, pitfalls, Windows 10 fallback), system light/dark and accent color (CSS `AccentColor` system colors in WebView2 vs reading them in Rust and passing them in), high-contrast mode (`forced-colors`), Segoe UI Variable, title bar (native vs custom).
6. Build and IPC: TypeScript and Vite settings for this target; Tauri commands/events vs the UI talking to the core's named pipe directly; Tauri 2 security settings (CSP, capabilities) for a window that can reach a process able to change power settings.
7. DESIGN.md: what Google's DESIGN.md format is (with source), and an outline of the DESIGN.md this project should have for Fluent 2.
8. A one-line browser support policy for AGENTS.md (Modern Web Guidance suggests one for Tauri targets): which Baseline level and which fallbacks to skip, given evergreen WebView2 only.

## Output
- One section per question with the decision first, then the reasons and sources.
- A "Modern Web Guidance" section listing each guide id you retrieved and what it changed in your decisions.
- Risks and unknowns.
- A final table: concern → choice → main reason → rejected alternatives.
