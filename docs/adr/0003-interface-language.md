# ADR 0003: Interface language and translations

- Status: accepted
- Date: 2026-11-27
- Builds on: [ADR 0001](0001-ui-stack.md); modelled on the owner's Flourprint ADR 0009

## Context

The owner is French-speaking; the project is public and written in English. Everything a user
reads must be translatable, both in the settings window and in what the core shows itself (tray
menu, notifications, the keep-awake reason). The product is built section by section, each section
with its own Rust module and its own page, so translations must follow the same split.

## Decision

### Languages and catalogs

- **English is the source language; French is the first translation.** Other left-to-right
  languages are added as catalogs, without code changes. Right-to-left languages are out of scope
  until one is requested; the window still sets `lang` and `dir` on `<html>` from the locale, and
  ADR 0002 already requires logical CSS properties.
- **Keys are namespaced by section** (`common.*`, `status.*`, `tiers.*`, `keepAwake.*`,
  `monitors.*`, `cpu.*`, `gpu.*`, `general.*`, `tray.*`), so each section owns its messages and can
  be added or removed with its module.
- **One key per message, plurals inside it.** Plural and select variants live in the message
  syntax of a single key (no `_one`/`_other` key suffixes), so English and French always have the
  same key set even though their plural rules differ.
- **Both catalogs change together.** A pull request that adds or changes a key updates English and
  French in the same pull request; the owner reviews the French text.

### What counts as user-facing

Every string a user can see or hear: text nodes and user-facing attributes in `src/components/` and
`src/views/` (`aria-label`, `aria-description`, `aria-valuetext`, `title`, `placeholder`, `alt`,
`.u-sr-only` text, `<figcaption>`, table headers), strings in the window's TypeScript modules
(error messages, chart labels), the page and window titles, and in the core: the tray menu,
notifications and the keep-awake reason shown by `powercfg /requests`.

Not translated: data from the system or the user, shown as is (monitor model names from EDID,
process names, power plan names), and developer logs.

### Checks

- A key check fails when a locale's key set differs from English, or when a message's placeholders
  and their types differ between English and French.
- A literal-string check fails on user-facing text outside the catalogs in the places listed above
  (Svelte template text and the attributes listed, TypeScript string literals passed to UI APIs,
  Rust strings passed to the tray, notification and power-request APIs). Class names, `data-*`
  values, tokens and identifiers are not user-facing.
- Both checks join the pre-push checks in `AGENTS.md` once they exist (#33).

### Runtime

- **The core stays language-neutral on the wire.** It sends codes and data (tier-change causes,
  error codes, values with units), never sentences; the settings window maps codes to messages.
  Only the core's own surfaces read catalogs directly.
- **Locale resolution**: the user's choice if set, else the Windows display language, matched by
  BCP 47 (`fr-CA` → `fr` → `en`).
- **The core owns the setting.** The choice made on the General page is stored by the core and
  applied at once: the window re-renders, the core rebuilds its tray menu and recreates the
  keep-awake request with the translated reason. No restart.
- **Missing or broken message at runtime**: fall back to the English message, then to the key
  itself; never crash. This only matters for catalogs added later, since the build checks the
  shipped ones.
- **Formatting**: in the window, numbers, units (W, %, s), durations and dates use the platform
  `Intl` APIs. In the core, the few values its surfaces show are formatted per locale by the
  message library chosen below (or a CLDR-based Rust formatter if it has none), never by
  hand-written string building.

## Open, settled at the prototype (#29)

The library and catalog format. Criteria:

- keys checked at build time (a missing key fails the build) and placeholders checkable;
- works under ADR 0001's CSP and Trusted Types: no `eval`, `new Function`, `innerHTML` or `{@html}`
  (messages precompiled if the library needs it);
- integrates with Svelte 5 runes, so a locale change re-renders without a reload;
- small runtime and plural support;
- one format read by both the Rust core and the window. Fluent has maintained implementations on
  both sides (`fluent-bundle` in Rust, `@fluent/bundle` in JavaScript). ICU MessageFormat in JSON
  is fine in the window, but Rust has no complete standard evaluator for it, so it would mean a
  second system or a subset in the core.

## Consequences

- The first page already goes through message keys; no retrofit later.
- Each section's contract (ADR to come) lists the codes the core may send for that section.
