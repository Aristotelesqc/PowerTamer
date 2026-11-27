# ADR 0002: CSS architecture of the settings window

- Status: accepted
- Date: 2026-11-27
- Builds on: [ADR 0001](0001-ui-stack.md) (plain CSS, no Tailwind, in-house components)
- Research: [CSS architecture](../research/2026-11-27-css-architecture.md) (Gemini 4 Neon, with
  caller verification notes)

## Context

ADR 0001 drops Tailwind for plain modern CSS. Without a framework, the CSS needs its own engine:
well-defined reusable classes, one place for each kind of rule, and checks that keep agents from
drifting. Paths below assume the settings window lives in `apps/ui/`; they move with it if the
scaffold chooses another root.

## Decision

### One owner per kind of rule

| Rule | Lives in | Layer |
|---|---|---|
| Tokens | `src/styles/tokens.css` | `tokens` |
| `@property` registrations | `src/styles/properties.css` | none (top level) |
| Targeted normalisation | `src/styles/reset.css` | `reset` |
| Page base: Mica canvas, type ramp, global `:focus-visible`, icon variables | `src/styles/base.css`, `icons.css` | `base` |
| Reusable primitives (`.pt-*`) | `src/styles/components/<name>.css`, one file per primitive | `components` |
| View-local layout | `<style>` in `src/views/*.svelte`, wrapped in `@layer views` by a Svelte preprocessor | `views` |
| Utilities (`.u-*`) | `src/styles/utilities.css` | `utilities` |

Components in `src/components/*.svelte` have no `<style>` block; their styles are the `.pt-*` files.
No exception: the chart's styles are `components/telemetry-chart.css`, and uPlot's own stylesheet is
imported by `index.css` into the `components` layer, before `telemetry-chart.css`.

Icons are the `.pt-icon` primitive (`components/icon.css`): an element with `class="pt-icon"` and
the icon chosen through a custom property (`style="--icon: var(--icon-chevron-right)"`), drawn as a
`currentColor` mask. The icon variables live in `icons.css`, in the `base` layer.

### Layers

`src/styles/index.css` is the single entry point, imported first by `main.ts`. It holds the only
layer statement, `@layer reset, tokens, base, components, views, utilities;`, and assigns files to
layers with `@import "…" layer(…)`. No other file declares `@layer`. Vite bundles one CSS file
(`build.cssCodeSplit: false`) so the order is the same in dev and build.

### Tokens

- Two global tiers in `tokens.css`:
  1. inputs and scales: the Windows accent ramp (`--win-accent`, `--win-accent-light-1..3`,
     `--win-accent-dark-1..3`, defaults in CSS, overwritten at runtime from Rust), spacing on a 4 px
     grid in `rem`, radii, control heights, type ramp (Segoe UI Variable optical families), motion
     durations and curves;
  2. semantic tokens mapped from WinUI 3 theme resources (`Common_themeresources_any.xaml`), with
     `light-dark()` for both themes: `--surface-*`, `--control-fill-*`, `--accent-fill-*`,
     `--text-*`, `--stroke-*`, `--focus-*`, `--status-<tone>-<bg|fg>`.
- Component-private hooks are `--_<name>`, declared only inside a `.pt-*` rule, never on `:root`.
- One `@media (forced-colors: active)` block remaps semantic tokens to system colors;
  `:root[data-backdrop="solid"]` gives an opaque window where Mica is unavailable.
- Naming: `--<category>-<role>[-<sub-role>][-<state>]`, lowercase kebab-case.
- Derived accent states use `color-mix(in srgb, …, transparent)`; never `in oklch`.

### Classes and state

| What | How |
|---|---|
| Primitive | `.pt-<component>` (`.pt-button`, `.pt-switch`, `.pt-card`) |
| Structural part | `.pt-<component>-<part>` (`.pt-card-title`), only when the HTML element is not enough |
| Variant, tone, size | `data-variant`, `data-tone`, `data-size` |
| Interaction and form state | native pseudo-classes: `:hover:not(:disabled)`, `:active:not(:disabled)`, `:focus-visible`, `:disabled`, `:checked`, `:user-invalid`, `:open`, `:popover-open`, `:modal` |
| Widget state | ARIA attributes as selectors: `[aria-expanded="true"]`, `[aria-current="page"]` |
| Child presence or state | `:has()` |
| Available space | container queries |

No BEM and no state classes (`.is-open`, `.active`): state exists once, in the DOM, so the look
cannot disagree with what assistive technology reads.

### Utilities

Exactly six: `.u-stack`, `.u-cluster`, `.u-sr-only`, `.u-tabular`, `.u-truncate`,
`.u-scroll-region`. A utility encodes a layout primitive, an accessibility behaviour or a
typographic rule, never a single visual property. A check fails when `utilities.css` declares any
other `.u-*` class or a cosmetic property. Adding one needs the owner's approval, a new ADR that
supersedes this list, and a `DESIGN.md` update.

### Enforcement

- Stylelint with `stylelint-config-standard`, `stylelint-config-html` (for `.svelte`) and
  `stylelint-declaration-strict-value`; `no-unknown-custom-properties` with `referenceFiles`
  pointing at the token files. Rules forbid raw colors and magic values outside `tokens.css`,
  unknown layers, `@layer`/`@property`/`:global()` outside their owner, `!important` (except
  `.u-sr-only`, through a per-file override), ID selectors, the `*` selector, physical
  properties where a logical one exists, `transition: all`, `outline: none`,
  `scrollbar-width: thin`, and `color-mix(in oklch`. Per-file overrides enforce the `.pt-*` and `.u-*`
  class patterns and keep those prefixes out of view `<style>` blocks.
- `svelte-check --fail-on-warnings` in runes mode (Svelte 4 syntax, `a11y_*`, unused selectors).
- An architecture check fails on a `<style>` block in a component, on `font-size` set on
  `html`/`:root`, on inline `style` other than the allowed custom properties, and on the utility
  limits above.
- `tokens.css` is the executable source of values. `DESIGN.md` uses the canonical schema of
  `@google/design.md` (its front matter keys and eight sections) and must pass
  `design.md lint`. A sync check compares what that schema can hold (for example colors,
  typography, spacing, radii and documented components) and fails when a covered token exists on
  one side only or when values differ. What the schema cannot express (light/dark pairs, the
  runtime accent, `forced-colors`) is described in `DESIGN.md` prose and checked only in
  `tokens.css`; the exact split is settled at the prototype.

## Rejected

- Primitive styles in Svelte `<style>` blocks: unlayered, so they beat every layer; children
  passed as snippets would need `:global()`.
- BEM and state classes: MWG `css` advises layers and `:where()` instead; state classes duplicate
  the DOM state.
- Atomic visual utilities: that is Tailwind again.
- Four global token tiers (`--button-bg-hover` on `:root`): too much for about twelve components.
- Generating `tokens.css` from `DESIGN.md`: the generator cannot express `light-dark()`,
  `forced-colors` or the runtime accent, and a custom YAML would break `design.md lint`.

## To verify at the first prototype

- The `@layer views` preprocessor with nested rules, `@container`, `@media` and `@keyframes`, in
  `vite dev` and `vite build`.
- Vite's inlining of `@import "…" layer(…)`.
- Stylelint 18 with the two plugins (the research assumed Stylelint 16).
- What `design.md lint` accepts for light/dark pairs and component entries (`design.md spec`),
  which fixes the scope of the sync check.
- uPlot's stylesheet inside the `components` layer.
- The WinUI 3 values the research could not read in the XAML source.
- `forced-colors` rendering of the switch and the accent button in the four Windows 11 contrast
  themes.
