# Agent instructions (Claude, Codex, Gemini, others)

## Sources of truth

1. `docs/adr/`: architecture decisions. A decision changes only through a new ADR that supersedes
   the old one, in its own pull request.
2. `docs/spec.md`: current design of the product (tiers, modules, architecture, coexistence rules).
   It follows the ADRs; when they disagree, the ADR wins and the spec is fixed.
3. `DESIGN.md` (once added): visual language and tokens of the settings window.
4. GitHub issues: needs and backlog. Issues describe needs, not decisions.

`docs/research/` holds research inputs (with their prompt and provenance). They inform decisions;
they are not decisions. Cite the ADR, not the research report.

## Rules

- English everywhere in the repository and on GitHub: code, comments, docs, commit messages,
  issues, pull requests.
- Dates written in the repository or on GitHub are in **UTC**, ISO form (`2026-11-27`,
  `2026-11-27T19:46Z`). Take them from the clock, never from memory. No relative time words
  ("yesterday", "recently") in the repository.
- No new dependency without a demonstrated need, named in the pull request.
- No fixed-interval polling in the core: react to Windows notifications (spec, principle 6).
- Read settings from Windows at the moment they matter; never hardcode a timeout or a device.
- The core runs without admin rights. Anything that needs elevation lives in the optional
  elevated helper.
- Keep machine-specific data out of the repository: no user names, local paths, serial numbers,
  monitor instance IDs or logs from a real machine. Model hardware IDs (EDID like `AUS2700`) in a
  quirks table are fine.

## Pull requests

- Never push to `main`. Every change lands through a focused pull request into `main`: one per
  decision (ADR), reviewed slice of implementation, or documentation change. Don't mix an ADR with
  the code that depends on it: the ADR comes first, in its own pull request.
- Every commit is pushed to its branch. Branch names: `<type>/<short-topic>` with `type` one of
  `feat`, `fix`, `docs`, `adr`, `chore`, `proto`.
- Pull requests follow `.github/pull_request_template.md` and name the issues they advance
  (`Refs #N`) or close (`Closes #N`, only when every acceptance criterion is met), or say
  `No issue:` with a one-line reason.
- Merging into `main` needs the owner's explicit approval for that pull request. With it, an agent
  may merge; without it, nobody does. Approval for one pull request does not extend to another, and
  auto-merge is never enabled without the owner asking. Never force-push, rewrite or reset `main`.
- Review reports (code review, audit, verification of a pull request) are posted as comments on
  the pull request or issue concerned, naming the commit SHA they examined. They are not committed.
- Run the checks that exist for the touched area before every push; a red push is fixed first.

## Issues and labels

GitHub issues are the only tracking system.

- `status:backlog`, `status:ready`, `status:in-progress`, `status:review`, `status:blocked`
  (say why in a comment), `status:verified`. Every open issue has exactly one.
- `area:core`, `area:monitors`, `area:cpu`, `area:gpu`, `area:ui`, `area:platform` (build, CI,
  dependencies, packaging), `area:docs`, `area:governance`.
- Workstreams are parent issues; a slice that one pull request can deliver is a GitHub sub-issue
  of its parent, one level deep. A sub-issue's body names its parent (`Parent #N`). A parent is
  done when its own acceptance is shown, not because its sub-issues are closed.
- `priority:p0` to `priority:p3`: set by the owner only; an agent may propose one in a comment.
- Starting work sets `status:in-progress`; opening its pull request sets `status:review`. A merged
  pull request does not close an issue by itself unless its acceptance is fully met.

## Settings window (`powertamer-ui`)

See [ADR 0001](docs/adr/0001-ui-stack.md).

- Browser support: evergreen WebView2 on Windows 11 only. Features shipped in current Chromium
  are used without fallbacks or polyfills. Exception: `<input type="checkbox" switch>` is not in
  Chromium; build switches on `<input type="checkbox" role="switch">`.
- Before writing HTML, CSS or client-side JS, consult Modern Web Guidance
  (`npx -y modern-web-guidance@latest search "<query>"`, then `retrieve`).
- No Tailwind, no CSS-in-JS, no component library. Plain CSS on the project's tokens.

### CSS ([ADR 0002](docs/adr/0002-css-architecture.md))

- One owner per rule: `.pt-*` primitives in `src/styles/components/*.css`; no `<style>` in
  `src/components/*.svelte` (except `TelemetryChart.svelte`); view `<style>` blocks are for local
  layout only and land in `@layer views`.
- `src/styles/index.css` holds the only `@layer` statement. Never write `@layer`, `@property` or
  `:global()` anywhere else.
- No raw color or magic number outside `tokens.css`: use tokens and `light-dark()`. Never
  `color-mix(in oklch, …)`.
- A new or changed token is changed in `tokens.css` and `DESIGN.md` together.
- Flat class names (`.pt-<component>[-<part>]`), `data-variant`/`data-tone`/`data-size` for
  variants, native pseudo-classes, ARIA attributes and `:has()` for state. No BEM, no state classes.
- Utilities are the closed list in ADR 0002. Adding one needs the owner's approval.
- No `*` reset, ID selector, `!important` (except `.u-sr-only`), `outline: none`,
  `transition: all` or `scrollbar-width: thin`.
- Logical properties, `rem` sizes, unitless `line-height`, `min-block-size` rather than a fixed
  `height` on text containers; never set `font-size` on `html` or `:root`.
- Every interactive control styles hover, active, disabled, `:focus-visible` (outline, 3:1
  contrast) and `forced-colors`, and puts spatial motion under
  `@media (prefers-reduced-motion: no-preference)`.
- No `{@html}` in Svelte templates (Trusted Types, ADR 0001).
- Before pushing UI work, run the CSS lint, `svelte-check`, the architecture and token-sync checks
  and `design.md lint`.
- No remote content in the window; keep the CSP strict.

## Delegation to other agents

- One writer at a time for overlapping files.
- A delegate's report is evidence, not a decision: the coordinator verifies its claims before
  anything enters an ADR or the spec.
