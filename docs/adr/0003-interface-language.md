# ADR 0003: Interface language and translations

- Status: accepted
- Date: 2026-11-27
- Builds on: [ADR 0001](0001-ui-stack.md); modelled on the owner's Flourprint ADR 0009

## Context

The owner is French-speaking; the project is public and written in English. Everything a user
reads must be translatable, both in the settings window and in what the core shows itself (tray
menu, notifications). The product is built section by section, each section with its own Rust
module and its own page, so translations must follow the same split.

## Decision

- **English is the source language; French is the first translation.** Other languages are added
  as catalogs, without code changes.
- **No user-facing text in code.** Svelte components and the core's tray menu and notifications
  use message keys; the text lives in per-locale catalogs. A check fails when a locale's keys
  differ from the English keys, and when a component contains a literal user-facing string.
- **Keys are namespaced by section** (`common.*`, `status.*`, `tiers.*`, `keepAwake.*`,
  `monitors.*`, `cpu.*`, `gpu.*`, `general.*`, `tray.*`), so each section owns its messages and can
  be added or removed with its module.
- **The core stays language-neutral on the wire.** It sends codes and data (tier-change causes,
  error codes, values with units), never sentences; the settings window maps codes to messages.
  Only the core's own surfaces (tray, notifications) read catalogs directly.
- **Locale** follows the Windows display language, falls back to English, and can be overridden on
  the General page.
- **Formatting**: numbers, units (W, %, s), durations and dates are formatted per locale with the
  platform `Intl` APIs in the window; plurals and placeholders use a message syntax that supports
  them (ICU MessageFormat or Fluent).
- **Not translated**: data from the system or the user, shown as is (monitor model names from EDID,
  process names, power plan names).

## Open, settled at the prototype (#29)

The library and catalog format. Criteria: keys checked at build time (a missing key fails the
build), small runtime, plural support, and a format the Rust core can read for the tray menu and
notifications (for example Fluent, which has Rust and JavaScript implementations, or ICU messages
in JSON).

## Consequences

- The first page already goes through message keys; no retrofit later.
- Each section's contract (ADR to come) lists the codes the core may send for that section.
