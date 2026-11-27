> **Provenance.** Gemini 4 Neon (High) through the Antigravity CLI, plan mode, 2026-11-27, repository at `76d23e9`.
> The caller supplied 35 Modern Web Guidance guides; the model ran no commands. Raw report below, unedited except for links.
> Caller's verification notes:
> - Confirmed: Stylelint's `referenceFiles` option exists and feeds `no-unknown-custom-properties`; `@google/design.md` (0.4.0, CLI `design.md`), `stylelint-declaration-strict-value` (1.13.0) and `stylelint-config-html` (2.0.0) exist.
> - Caution: Stylelint is at 18.1.0, not 16; plugin compatibility with 18 must be checked at install.
> - Correction from the fact-check on PR #2: `DESIGN.md` must use the canonical schema of `@google/design.md` (front matter keys, 8 English sections). The DESIGN.md sync design in section 7.3 must follow that schema.
> - Inconsistencies inside the report (found in review; ADR 0002 resolves them, don't copy the examples as is): its Stylelint config forbids `!important` with no override for `.u-sr-only`; the `SettingsCard` example uses an `.icon` class outside the `.pt-*` scheme; the `ToggleSwitch` example hard-codes French UI strings; the architecture check allows `--control-value` while the slider section uses `attr()` and `progress()`.
> - Unverified: the Svelte preprocessor that wraps view `<style>` blocks in `@layer views`, Vite's inlining of `@import ... layer()`, and several WinUI 3 token values (the report flags them itself). All need the first prototype.
# Architecture du système CSS natif pour PowerTamer (`powertamer-ui`)

Ce document tranche l'architecture complète du système CSS natif de `powertamer-ui`, dans la continuité de [docs/spec.md](../../docs/spec.md) et de la décision de stack acceptée [docs/research/2026-11-27-ui-stack-decision.md](../../docs/research/2026-11-27-ui-stack-decision.md) (Tauri 2, WebView2 evergreen Chromium/Edge 146+ sur Windows 11, Svelte 5 en mode runes strict, TypeScript, Vite, CSS moderne pur, Fluent 2 Desktop / WinUI 3 sur Mica, ~10–12 composants maison et `DESIGN.md`).

---

## 1. Emplacement des styles réutilisables (global vs `<style>` Svelte)

### Décision : **Propriétaire unique par catégorie** — les 12 primitives Fluent 2 vivent exclusivement en CSS global (`@layer components`), et les blocs `<style>` Svelte sont réservés à la composition locale des vues et automatiquement placés dans `@layer views` via un préprocesseur `svelte.config.js`

| Catégorie de règle | Emplacement unique autorisé | Couche `@layer` |
|---|---|---|
| Tokens (`--*`) et enregistrements `@property` | `apps/ui/src/styles/tokens.css` et `properties.css` | `tokens` (et top-level pour `@property`) |
| Normalisations ciblées, canvas Mica, typographie globale, `:focus-visible`, moteur d'icônes | `apps/ui/src/styles/reset.css`, `base.css`, `icons.css` | `reset`, `base` |
| Primitives UI réutilisables (`.pt-button`, `.pt-switch`, `.pt-card`, etc.) | `apps/ui/src/styles/components/*.css` (**zéro `<style>`** dans `src/components/*.svelte`, sauf exception listée pour `TelemetryChart.svelte`) | `components` |
| Mise en page spécifique à une vue ou colle privée non réutilisable | `<style>` scopé dans `apps/ui/src/views/*.svelte` | `views` (injecté par préprocesseur) |
| Utilitaires fermés (`.u-*`) | `apps/ui/src/styles/utilities.css` | `utilities` |

### Raisons et sources

1. **Éviter le piège de priorité CSS Cascade Layers × Svelte** : dans Svelte 5, un bloc `<style>` est compilé en CSS **hors couche** (*unlayered*) avec une classe de hachage `.svelte-xxxxxx` ([https://svelte.dev/docs/svelte/scoped-styles](https://svelte.dev/docs/svelte/scoped-styles)). Or, selon la spécification CSS Cascade Layers, **tout style hors couche bat systématiquement tout style en couche**, quelle que soit sa spécificité ([https://developer.mozilla.org/en-US/docs/Web/CSS/@layer](https://developer.mozilla.org/en-US/docs/Web/CSS/@layer)). Sans mise en couche automatique des `<style>` Svelte, un utilitaire dans `@layer utilities` ne pourrait jamais surcharger une règle de vue sans `!important`.
2. **Préprocesseur de 6 lignes dans `svelte.config.js`** : envelopper automatiquement chaque bloc `<style>` de composant dans `@layer views { ... }` garantit qu'une vue peut spécialiser localement un composant (`views > components`) tout en restant toujours subordonnée à `@layer utilities` (`utilities > views`), sans aucune règle hors couche incontrôlée. *(Note : la compatibilité exacte de ce préprocesseur avec `@keyframes` et les commentaires de tête sur la version exacte de `@sveltejs/vite-plugin-svelte` installée devra être vérifiée au premier build — voir Risques et points ouverts.)*
3. **Zéro duplication entre HTML natif et composants Svelte** : les 12 primitives UI sont des enveloppes HTML fines (`<button class="pt-button">`, `<dialog class="pt-dialog">`, `<input type="checkbox" role="switch" class="pt-switch">`). Placer leurs règles dans `src/styles/components/*.css` permet d'utiliser directement `<button class="pt-button" value="cancel">` dans un `<form method="dialog">` ou un `SettingsCard` sans imposer un wrapper Svelte partout ni dupliquer des règles entre `.css` et `.svelte`.
4. **Composition par `Snippet` Svelte 5 sans `:global()`** : les conteneurs (`SettingsCard`, `SettingsExpander`, `Dialog`, `PopupMenu`) projettent du contenu via `{@render children()}`. En CSS scopé Svelte, cibler des enfants projetés impose `:global(...)`, ce qui perce l'encapsulation et complique le lint ([https://svelte.dev/docs/svelte/scoped-styles](https://svelte.dev/docs/svelte/scoped-styles)). Avec des classes primitives dans `@layer components`, les enfants directs s'agencent nativement sans aucun `:global()`.
5. **Élimination des faux positifs `css_unused_selector`** : le compilateur Svelte avertit sur tout sélecteur d'un `<style>` qu'il ne retrouve pas statiquement dans le template du même fichier ([https://svelte.dev/docs/svelte/compiler-warnings](https://svelte.dev/docs/svelte/compiler-warnings)). Garder les primitives dans `src/styles/components/*.css` réserve `css_unused_selector` à ce qu'il fait de mieux : détecter le CSS mort dans les vues.

```js
// apps/ui/svelte.config.js (extrait du préprocesseur de couche)
const layerViewsPreprocessor = {
  name: 'powertamer-layer-views',
  style({ content }) {
    if (!content.trim()) return;
    return { code: `@layer views {\n${content}\n}` };
  },
};
```

---

## 2. Arborescence, ordre unique `@layer` et contrat de chaque couche

### Décision : Arborescence plate sous `apps/ui/src/styles/`, un seul manifeste `index.css` portant l'ordre `@layer` et les `@import ... layer(...)`, avec `build.cssCodeSplit: false` dans Vite

```text
apps/ui/src/styles/
├── index.css                  # Point d'entrée unique importé en ligne 1 de main.ts
├── properties.css             # Uniquement @property (--icon-start, --icon-end)
├── reset.css                  # @layer reset — normalisation ciblée (jamais de *)
├── tokens.css                 # @layer tokens — source de vérité CSS Fluent 2 / WinUI 3
├── base.css                   # @layer base — Mica, typographie, focus-visible, scrollbars, selection
├── icons.css                  # @layer base — moteur d'icônes CSS (@container style) + data URI SVG
├── utilities.css              # @layer utilities — liste fermée de 6 utilitaires
└── components/                # @layer components — 1 fichier par primitive maison
    ├── button.css
    ├── dialog.css
    ├── info-bar.css
    ├── input.css
    ├── navigation-view.css
    ├── popup-menu.css
    ├── select.css
    ├── settings-card.css
    ├── settings-expander.css
    ├── slider.css
    ├── telemetry-chart.css
    └── toggle-switch.css
```

### Déclaration unique d'ordre des couches (`apps/ui/src/styles/index.css`)

```css
/* apps/ui/src/styles/index.css — importé en tout premier dans apps/ui/src/main.ts */
@layer reset, tokens, base, components, views, utilities;

@import "./properties.css";
@import "./reset.css" layer(reset);
@import "./tokens.css" layer(tokens);
@import "./base.css" layer(base);
@import "./icons.css" layer(base);

@import "./components/button.css" layer(components);
@import "./components/dialog.css" layer(components);
@import "./components/info-bar.css" layer(components);
@import "./components/input.css" layer(components);
@import "./components/navigation-view.css" layer(components);
@import "./components/popup-menu.css" layer(components);
@import "./components/select.css" layer(components);
@import "./components/settings-card.css" layer(components);
@import "./components/settings-expander.css" layer(components);
@import "./components/slider.css" layer(components);
@import "./components/telemetry-chart.css" layer(components);
@import "./components/toggle-switch.css" layer(components);

@import "./utilities.css" layer(utilities);
```

### Contrat strict de chaque couche

| Couche | Contenu autorisé | Strictement interdit |
|---|---|---|
| *(top-level)* `properties.css` | Uniquement `@property` (enregistrements globaux de propriétés non héritées comme `--icon-start` / `--icon-end`). | Toute règle de style ou sélecteur. |
| `reset` | Normalisations ciblées par listes d'éléments dans `:where(...)` (`box-sizing: border-box`, `font: inherit` sur contrôles de formulaire, `display: block; max-inline-size: 100%` sur médias/canvas, `overscroll-behavior: contain` sur `dialog:modal` et `dialog::backdrop`). | Sélecteur universel `*` (interdit par MWG `css`), couleurs, rayons, polices. |
| `tokens` | `color-scheme: light dark`, custom properties sur `:root`, `:root[data-backdrop="solid"]` et `@media (forced-colors: active)`. | Toute propriété visuelle (`color`, `background`, `padding`, etc.) et tout sélecteur de classe ou d'élément autre que `:root`. |
| `base` | `html, body` (fond transparent Mica, repli solide et `Canvas` en contraste élevé), échelle typographique sur `body` et `h1`–`h4`, anneau `:focus-visible` global en `:where(...)`, `::selection`, `scrollbar-color`, `accent-color`, moteur d'icônes (`icons.css`). | Classes `.pt-*` ou `.u-*`, `font-size` sur `html` ou `:root` (MWG `respect-os-text-scale`). |
| `components` | Règles des 12 primitives `.pt-*`, leurs variantes `[data-*]`, états natifs/ARIA, `@container` internes et variables privées locales `--_*`. | Blocs `@layer` imbriqués, valeurs brutes hors tokens, utilitaires `.u-*`. |
| `views` | Uniquement les blocs `<style>` de `src/views/*.svelte` (et `TelemetryChart.svelte`), enveloppés automatiquement par `svelte.config.js`. | Redéfinition de tokens ou de `.pt-*`, `:global(...)`, `@layer`, `@property`. |
| `utilities` | Exclusivement les 6 utilitaires fermés `.u-*` dans `utilities.css`. | Toute propriété cosmétique (couleurs, bordures, ombres, tailles de police). |

### Raisons et sources

- **Zéro cascade réseau à l'exécution malgré `@import`** : MWG `performance` interdit `@import` CSS en production web à cause des requêtes séquentielles. Avec Vite (et Lightning CSS), les `@import "..." layer(...)` locaux sont **résolus et inlinés à la compilation** dans un unique bundle servi localement par Tauri ([https://vite.dev/config/build-options.html](https://vite.dev/config/build-options.html), [https://lightningcss.dev/bundling.html](https://lightningcss.dev/bundling.html)).
- **Parité dev/prod via `build.cssCodeSplit: false`** : la documentation Vite documente que le découpage CSS asynchrone peut modifier l'ordre de chargement entre `vite dev` et `vite build` ([https://vite.dev/config/build-options.html#build-csscodesplit](https://vite.dev/config/build-options.html#build-csscodesplit)). Comme `powertamer-ui` est une fenêtre locale de paramètres légère (~10–12 composants), un seul fichier CSS bundlé élimine toute divergence d'ordre `@layer` et garantit une première peinture immédiate.
- **Assignation de couche centralisée dans `index.css`** : attacher la couche dans `@import "./components/button.css" layer(components);` évite d'indenter chaque fichier composant dans un bloc `@layer components { ... }`, simplifie `max-nesting-depth` dans Stylelint et rend impossible l'invention d'une couche arbitraire dans un fichier composant ([https://developer.mozilla.org/en-US/docs/Web/CSS/@import](https://developer.mozilla.org/en-US/docs/Web/CSS/@import)).
- **Pas de reset global `*`** : MWG `css` proscrit explicitement les styles sur `*` ; cibler les balises pertinentes dans `:where(...)` garantit une spécificité `(0, 0, 0)` facilement surchargeable.

---

## 3. Système de tokens : niveaux, mappage WinUI 3, accent dynamique et grammaire

### Décision : **2 niveaux globaux dans `tokens.css`** (Niveau 1 : entrées système & échelles brutes ; Niveau 2 : tokens sémantiques WinUI 3 / Fluent 2 en `light-dark()`) + **variables privées locales `--_*`** dans `components/*.css` (aucun niveau 3/4 global sur `:root`)

### Architecture des niveaux

1. **Niveau 1 — Entrées système dynamiques & échelles fondamentales (`tokens.css`)** :
   - **Rampe d'accent système Windows** poussée à l'exécution par Rust via WinRT `UISettings::GetColorValue` ([https://learn.microsoft.com/en-us/uwp/api/windows.ui.viewmanagement.uisettings](https://learn.microsoft.com/en-us/uwp/api/windows.ui.viewmanagement.uisettings)) sur `document.documentElement.style` : `--win-accent`, `--win-accent-light-1..3`, `--win-accent-dark-1..3`, avec des valeurs par défaut Windows 11 bleu Fluent dans `tokens.css` pour la peinture avant hydratation IPC.
   - **Échelles fixes** : espacements en `rem` (grille de 4 px : `--space-1` à `--space-8`), rayons (`--radius-control`, `--radius-card`, `--radius-overlay`, `--radius-pill`), hauteurs minimales de contrôles (`--control-height-sm`, `--control-height-md`, `--control-height-touch`), typographie (`Segoe UI Variable` optique, tailles `rem`, `line-height` sans unité, graisses), mouvement Fluent 2 (`83ms`, `167ms`, `250ms`, courbes `cubic-bezier`), épaisseurs de bordure et focus.
2. **Niveau 2 — Tokens sémantiques WinUI 3 / Fluent 2 (`tokens.css`)** :
   - Calqués sur `Common_themeresources_any.xaml` de WinUI 3 ([https://github.com/microsoft/microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml), [https://learn.microsoft.com/en-us/windows/apps/design/style/color](https://learn.microsoft.com/en-us/windows/apps/design/style/color)).
   - Exprimés via `light-dark(<clair>, <sombre>)` ([https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/light-dark](https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/light-dark)) — **aucune media query `prefers-color-scheme` dupliquée**, grâce à WebView2 evergreen (Edge 123+).
   - **Un seul bloc `@media (forced-colors: active)`** remappe les tokens sémantiques vers les mots-clés de couleurs système CSS (`Canvas`, `CanvasText`, `ButtonFace`, `ButtonText`, `ButtonBorder`, `Highlight`, `HighlightText`, `GrayText`, `Field`, `FieldText`) ([https://developer.mozilla.org/en-US/docs/Web/CSS/@media/forced-colors](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/forced-colors)).
   - **Un bloc `:root[data-backdrop="solid"]`** remplace `--surface-window: transparent` par `light-dark(#f3f3f3, #202020)` sur Windows 10 (où Mica échoue).
3. **Hooks locaux de composant (`--_<nom>`, uniquement dans `components/*.css`)** :
   - Pas de tokens globaux de type `--button-accent-hover-bg` dans `tokens.css` (MWG `css` : *« The smaller the scope of the use case, the fewer tiers it needs. Do not overengineer. »*). Chaque composant peut déclarer des alias locaux préfixés par `--_` (ex. `--_bg`, `--_border`, `--_fg`) qui pointent vers les tokens de niveau 2 et basculent selon `[data-variant]` ou les états.

### Correspondance WinUI 3 (`Common_themeresources_any.xaml`) → Tokens PowerTamer

| Ressource WinUI 3 | Token PowerTamer | Clair | Sombre | Contraste élevé (`forced-colors`) |
|---|---|---|---|---|
| *(Mica backdrop)* / `SolidBackgroundFillColorBase` | `--surface-window` | `transparent` (`#f3f3f3` si `solid`) | `transparent` (`#202020` si `solid`) | `Canvas` |
| `LayerFillColorDefault` | `--surface-layer` | `rgb(255 255 255 / 0.5)` *(à vérifier : `#0B000000` vs `#80FFFFFF` selon Mica/MicaAlt)* | `rgb(58 58 58 / 0.3)` (`#4C3A3A3A`) | `Canvas` |
| `CardBackgroundFillColorDefault` | `--surface-card` | `rgb(255 255 255 / 0.7)` *(WinUI Light : `#B3FFFFFF` / `#FFFFFFFF` selon version XAML)* | `rgb(255 255 255 / 0.051)` (`#0DFFFFFF`) | `Canvas` |
| `ControlFillColorDefault` | `--control-fill-default` | `rgb(255 255 255 / 0.7)` (`#B3FFFFFF`) | `rgb(255 255 255 / 0.06)` (`#0FFFFFFF`) | `ButtonFace` |
| `ControlFillColorSecondary` | `--control-fill-secondary` | `rgb(249 249 249 / 0.5)` | `rgb(255 255 255 / 0.084)` | `Highlight` / `ButtonFace` |
| `ControlFillColorTertiary` | `--control-fill-tertiary` | `rgb(249 249 249 / 0.3)` | `rgb(255 255 255 / 0.033)` | `ButtonFace` |
| `ControlFillColorDisabled` | `--control-fill-disabled` | `rgb(249 249 249 / 0.3)` | `rgb(255 255 255 / 0.042)` | `Canvas` |
| `AccentFillColorDefault` | `--accent-fill-default` | `var(--win-accent-dark-1)` | `var(--win-accent-light-2)` | `Highlight` |
| `AccentFillColorSecondary` | `--accent-fill-secondary` | `color-mix(in srgb, var(--accent-fill-default) 90%, transparent)` | idem | `Highlight` |
| `AccentFillColorTertiary` | `--accent-fill-tertiary` | `color-mix(in srgb, var(--accent-fill-default) 80%, transparent)` | idem | `Highlight` |
| `TextFillColorPrimary` | `--text-primary` | `rgb(0 0 0 / 0.895)` (`#E4000000`) | `#ffffff` (`#FFFFFFFF`) | `CanvasText` |
| `TextFillColorSecondary` | `--text-secondary` | `rgb(0 0 0 / 0.606)` (`#99000000`) | `rgb(255 255 255 / 0.773)` (`#C5FFFFFF`) | `CanvasText` |
| `TextFillColorDisabled` | `--text-disabled` | `rgb(0 0 0 / 0.361)` | `rgb(255 255 255 / 0.363)` | `GrayText` |
| `TextOnAccentFillColorPrimary` | `--text-on-accent-primary` | `#ffffff` | `#000000` | `HighlightText` |
| `CardStrokeColorDefault` | `--stroke-card` | `rgb(0 0 0 / 0.098)` (`#19000000`) | `rgb(0 0 0 / 0.059)` (`#0F000000`) *(ou blanc ~7 % si contraste insuffisant)* | `ButtonBorder` |
| `ControlStrokeColorDefault` | `--stroke-control` | `rgb(0 0 0 / 0.059)` (`#0F000000`) | `rgb(255 255 255 / 0.07)` (`#12FFFFFF`) | `ButtonBorder` |
| `ControlStrokeColorSecondary` | `--stroke-control-secondary` | `rgb(0 0 0 / 0.16)` (`#29000000`) | `rgb(255 255 255 / 0.093)` | `ButtonBorder` |
| `ControlStrongStrokeColorDefault` | `--stroke-control-strong` | `rgb(0 0 0 / 0.446)` | `rgb(255 255 255 / 0.544)` | `ButtonText` |
| `FocusStrokeColorOuter` | `--focus-stroke-outer` | `rgb(0 0 0 / 0.895)` (`#E4000000`) | `#ffffff` | `Highlight` |
| `FocusStrokeColorInner` | `--focus-stroke-inner` | `rgb(255 255 255 / 0.7)` | `rgb(0 0 0 / 0.7)` | `Canvas` |

> **Transparence de vérification** : les valeurs hexadécimales exactes de `CardBackgroundFillColorDefault` (sombre `#0DFFFFFF`, clair `#FFFFFFFF` selon le dictionnaire WinUI 3), `ControlFillColorDefault` (`#B3FFFFFF` / `#0FFFFFFF`), `FocusStrokeColorOuter` (`#E4000000` / `#FFFFFF`), `TextFillColorPrimary` (`#E4000000` / `#FFFFFFFF`), `TextFillColorSecondary` (`#99000000` / `#C5FFFFFF`), `CardStrokeColorDefault` (`#19000000` / `#0F000000`), `ControlStrokeColorDefault` (`#0F000000` / `#12FFFFFF`), `ControlStrokeColorSecondary` (clair `#29000000`) et `LayerFillColorDefault` (sombre `#4C3A3A3A`) ont été vérifiées contre des extraits de `Common_themeresources_any.xaml` issus de `microsoft/microsoft-ui-xaml`. Les autres opacités secondaires/tertiaires/désactivées et les teintes exactes de statut (`SystemFillColor*`) n'ont pas pu être lues ligne par ligne dans le fichier source XAML en direct et devront être recoupées directement dans `Common_themeresources_any.xaml` lors de l'implémentation initiale.

### Grammaire de nommage des tokens

- **Tokens globaux** : `--<catégorie>-<rôle>[-<sous-rôle>][-<état>]` en `kebab-case` minuscule :
  - Entrées OS : `--win-accent`, `--win-accent-light-1..3`, `--win-accent-dark-1..3`
  - Couleurs sémantiques : `--surface-*`, `--control-fill-*`, `--subtle-fill-*`, `--accent-fill-*`, `--accent-text-*`, `--text-*`, `--stroke-*`, `--focus-*`, `--status-<info|success|caution|critical>-<bg|fg>`
  - Dimensions & échelles : `--space-1..8`, `--radius-<control|card|overlay|pill>`, `--border-width`, `--control-height-<sm|md|touch>`, `--shadow-<flyout|dialog>`
  - Typographie : `--font-<display|body|caption|mono>`, `--text-size-<caption|body|subtitle|title>`, `--leading-<caption|body|subtitle|title>`, `--weight-<regular|semibold>`
  - Mouvement : `--duration-<fast|normal|slow>`, `--ease-<standard|decelerate|accelerate>`
- **Variables privées locales à un composant** : `--_<nom>` (ex. `--_bg`, `--_track-w`), jamais déclarées sur `:root`.
- **Pourquoi `color-mix(in srgb, ...)` pour l'opacité d'accent et jamais `in oklch`** : MWG `color` documente le bug Chromium **565837690** où `color-mix(in oklch, ...)` sur des couleurs peu saturées produit une teinte rose/magenta parasite, et déconseille le décalage naïf de luminance `oklch` faute de *gamut mapping* navigateur. Pour dériver les états survol/pressé de la rampe WinRT déjà calibrée par Windows, un simple réglage d'alpha en `srgb` (`90%` / `80%`) conserve fidèlement la teinte exacte du système sans aucune dérive chromatique.

### Extrait représentatif de `apps/ui/src/styles/tokens.css`

```css
/* apps/ui/src/styles/tokens.css — chargé dans @layer tokens via index.css */
:root {
  color-scheme: light dark;

  /* ── Niveau 1 : rampe d'accent Windows (écrasée au runtime par Rust / WinRT UISettings) ── */
  --win-accent: #0078d4;
  --win-accent-light-1: #0091f8;
  --win-accent-light-2: #4cc2ff;
  --win-accent-light-3: #99ebff;
  --win-accent-dark-1: #005fb8;
  --win-accent-dark-2: #003e92;
  --win-accent-dark-3: #001a68;

  /* ── Niveau 1 : grille d'espacement (base 4px en rem pour suivre text-scale OS) ── */
  --space-1: 0.25rem;   /* 4px — gap entre SettingsCard d'un même groupe */
  --space-2: 0.5rem;    /* 8px */
  --space-3: 0.75rem;   /* 12px */
  --space-4: 1rem;      /* 16px — padding interne SettingsCard */
  --space-5: 1.25rem;   /* 20px */
  --space-6: 1.5rem;    /* 24px — gap entre sections */
  --space-8: 2rem;      /* 32px */

  /* ── Niveau 1 : rayons, épaisseurs, cibles (Fluent 2 Desktop) ── */
  --radius-control: 4px;
  --radius-card: 8px;
  --radius-overlay: 8px;
  --radius-pill: 9999px;
  --border-width: 1px;
  --focus-width: 2px;
  --focus-offset: 2px;
  --control-height-sm: 1.75rem;   /* 28px */
  --control-height-md: 2rem;      /* 32px — hauteur standard bouton WinUI 3 */
  --control-height-touch: 2.75rem;/* 44px — cible pointer: coarse */

  /* ── Niveau 1 : typographie (Segoe UI Variable optique, zéro police embarquée) ── */
  --font-display: "Segoe UI Variable Display", "Segoe UI", system-ui, sans-serif;
  --font-body: "Segoe UI Variable Text", "Segoe UI", system-ui, sans-serif;
  --font-caption: "Segoe UI Variable Small", "Segoe UI", system-ui, sans-serif;
  --font-mono: "Cascadia Mono", "Consolas", ui-monospace, monospace;

  --text-size-caption: 0.75rem;   /* 12px */
  --text-size-body: 0.875rem;     /* 14px */
  --text-size-subtitle: 1.25rem;  /* 20px */
  --text-size-title: 1.75rem;     /* 28px */

  --leading-caption: 1.333;       /* 16 / 12 */
  --leading-body: 1.429;          /* 20 / 14 */
  --leading-subtitle: 1.4;        /* 28 / 20 */
  --leading-title: 1.286;         /* 36 / 28 */

  --weight-regular: 400;
  --weight-semibold: 600;

  /* ── Niveau 1 : mouvement (Fluent 2 / WinUI 3) ── */
  --duration-fast: 83ms;
  --duration-normal: 167ms;
  --duration-slow: 250ms;
  --ease-standard: cubic-bezier(0.1, 0.9, 0.2, 1);
  --ease-decelerate: cubic-bezier(0, 0, 0, 1);
  --ease-accelerate: cubic-bezier(1, 0, 1, 1);

  /* ── Niveau 2 : surfaces sémantiques sur Mica ── */
  --surface-window: transparent;
  --surface-layer: light-dark(rgb(255 255 255 / 0.5), rgb(58 58 58 / 0.3));
  --surface-card: light-dark(rgb(255 255 255 / 0.7), rgb(255 255 255 / 0.051));
  --surface-card-hover: light-dark(rgb(249 249 249 / 0.5), rgb(255 255 255 / 0.084));
  --surface-card-pressed: light-dark(rgb(249 249 249 / 0.3), rgb(255 255 255 / 0.033));
  --surface-flyout: light-dark(#fcfcfc, #2c2c2c);
  --surface-dialog: light-dark(#f3f3f3, #202020);

  /* ── Niveau 2 : contrôles & états ── */
  --control-fill-default: light-dark(rgb(255 255 255 / 0.7), rgb(255 255 255 / 0.06));
  --control-fill-secondary: light-dark(rgb(249 249 249 / 0.5), rgb(255 255 255 / 0.084));
  --control-fill-tertiary: light-dark(rgb(249 249 249 / 0.3), rgb(255 255 255 / 0.033));
  --control-fill-disabled: light-dark(rgb(249 249 249 / 0.3), rgb(255 255 255 / 0.042));
  --control-alt-secondary: light-dark(rgb(0 0 0 / 0.024), rgb(0 0 0 / 0.1));
  --control-alt-tertiary: light-dark(rgb(0 0 0 / 0.057), rgb(255 255 255 / 0.042));
  --subtle-fill-secondary: light-dark(rgb(0 0 0 / 0.037), rgb(255 255 255 / 0.06));
  --subtle-fill-tertiary: light-dark(rgb(0 0 0 / 0.024), rgb(255 255 255 / 0.042));

  /* ── Niveau 2 : accent (suit la rampe WinRT) ── */
  --accent-fill-default: light-dark(var(--win-accent-dark-1), var(--win-accent-light-2));
  --accent-fill-secondary: color-mix(in srgb, var(--accent-fill-default) 90%, transparent);
  --accent-fill-tertiary: color-mix(in srgb, var(--accent-fill-default) 80%, transparent);
  --accent-fill-disabled: light-dark(rgb(0 0 0 / 0.217), rgb(255 255 255 / 0.158));
  --accent-text-primary: light-dark(var(--win-accent-dark-2), var(--win-accent-light-3));

  /* ── Niveau 2 : textes ── */
  --text-primary: light-dark(rgb(0 0 0 / 0.895), #ffffff);
  --text-secondary: light-dark(rgb(0 0 0 / 0.606), rgb(255 255 255 / 0.773));
  --text-disabled: light-dark(rgb(0 0 0 / 0.361), rgb(255 255 255 / 0.363));
  --text-on-accent-primary: light-dark(#ffffff, #000000);
  --text-on-accent-secondary: light-dark(rgb(255 255 255 / 0.7), rgb(0 0 0 / 0.5));
  --text-on-accent-disabled: light-dark(#ffffff, rgb(255 255 255 / 0.53));

  /* ── Niveau 2 : bordures, focus & élévation (overlays uniquement) ── */
  --stroke-card: light-dark(rgb(0 0 0 / 0.098), rgb(0 0 0 / 0.18));
  --stroke-control: light-dark(rgb(0 0 0 / 0.059), rgb(255 255 255 / 0.07));
  --stroke-control-secondary: light-dark(rgb(0 0 0 / 0.16), rgb(255 255 255 / 0.093));
  --stroke-control-strong: light-dark(rgb(0 0 0 / 0.446), rgb(255 255 255 / 0.544));
  --stroke-control-strong-disabled: light-dark(rgb(0 0 0 / 0.217), rgb(255 255 255 / 0.158));
  --stroke-divider: light-dark(rgb(0 0 0 / 0.08), rgb(255 255 255 / 0.084));
  --focus-stroke-outer: light-dark(rgb(0 0 0 / 0.895), #ffffff);
  --focus-stroke-inner: light-dark(rgb(255 255 255 / 0.7), rgb(0 0 0 / 0.7));

  --shadow-flyout: light-dark(0 8px 16px rgb(0 0 0 / 0.14), 0 8px 16px rgb(0 0 0 / 0.26));
  --shadow-dialog: light-dark(0 32px 64px rgb(0 0 0 / 0.19), 0 32px 64px rgb(0 0 0 / 0.37));

  /* ── Niveau 2 : statuts sémantiques (InfoBar, Process Lasso, paliers) ── */
  --status-info-bg: light-dark(#f4f4f4, #272727);
  --status-info-fg: light-dark(#005fb8, #60cdff);
  --status-success-bg: light-dark(#dff6dd, #393d1b);
  --status-success-fg: light-dark(#0f7b0f, #6ccb5f);
  --status-caution-bg: light-dark(#fff4ce, #433519);
  --status-caution-fg: light-dark(#9d5d00, #fce100);
  --status-critical-bg: light-dark(#fde7e9, #442726);
  --status-critical-fg: light-dark(#c42b1c, #ff99a4);
}

/* Repli Windows 10 lorsque apply_mica échoue */
:root[data-backdrop="solid"] {
  --surface-window: light-dark(#f3f3f3, #202020);
}

/* Contraste élevé Windows : remappage intégral vers les couleurs système */
@media (forced-colors: active) {
  :root {
    --surface-window: Canvas;
    --surface-layer: Canvas;
    --surface-card: Canvas;
    --surface-card-hover: Canvas;
    --surface-card-pressed: Canvas;
    --surface-flyout: Canvas;
    --surface-dialog: Canvas;

    --control-fill-default: ButtonFace;
    --control-fill-secondary: ButtonFace;
    --control-fill-tertiary: ButtonFace;
    --control-fill-disabled: Canvas;
    --control-alt-secondary: Canvas;
    --control-alt-tertiary: Canvas;
    --subtle-fill-secondary: Canvas;
    --subtle-fill-tertiary: Canvas;

    --accent-fill-default: Highlight;
    --accent-fill-secondary: Highlight;
    --accent-fill-tertiary: Highlight;
    --accent-fill-disabled: GrayText;
    --accent-text-primary: LinkText;

    --text-primary: CanvasText;
    --text-secondary: CanvasText;
    --text-disabled: GrayText;
    --text-on-accent-primary: HighlightText;
    --text-on-accent-secondary: HighlightText;
    --text-on-accent-disabled: Canvas;

    --stroke-card: ButtonBorder;
    --stroke-control: ButtonBorder;
    --stroke-control-secondary: ButtonBorder;
    --stroke-control-strong: ButtonText;
    --stroke-control-strong-disabled: GrayText;
    --stroke-divider: ButtonBorder;
    --focus-stroke-outer: Highlight;
    --focus-stroke-inner: Canvas;

    --shadow-flyout: none;
    --shadow-dialog: none;

    --status-info-bg: Canvas;
    --status-info-fg: CanvasText;
    --status-success-bg: Canvas;
    --status-success-fg: CanvasText;
    --status-caution-bg: Canvas;
    --status-caution-fg: CanvasText;
    --status-critical-bg: Canvas;
    --status-critical-fg: CanvasText;
  }
}
```

---

## 4. Convention de nommage, variantes et états

### Décision : **Classes plates préfixées (`.pt-*` / `.u-*`, sans BEM)** + **`data-*` strictement pour les variantes visuelles** + **pseudo-classes natives et attributs ARIA comme unique source de vérité pour tout état**

### Table de partition (zéro duplication d'état)

| Nature | Mécanisme unique | Exemples canoniques | Anti-pattern interdit |
|---|---|---|---|
| **Identité d'une primitive** | Classe `.pt-<composant>` | `.pt-button`, `.pt-switch`, `.pt-card`, `.pt-infobar` | `.button`, `.Button` |
| **Sous-partie structurelle** *(uniquement si la balise HTML ne suffit pas)* | Classe plate `.pt-<composant>-<rôle>` | `.pt-card-header`, `.pt-card-title`, `.pt-card-desc`, `.pt-card-control`, `.pt-card-icon` | `.pt-card__header` (BEM) |
| **Variante visuelle / tonalité / taille** *(choix statique d'auteur, non sémantique)* | Attributs `data-variant`, `data-tone`, `data-size` | `<button class="pt-button" data-variant="accent">`, `<div class="pt-infobar" data-tone="caution">` | `.pt-button--accent`, `.pt-button-primary` |
| **État natif d'interaction ou de formulaire** | Pseudo-classes CSS natives | `:hover:not(:disabled)`, `:active:not(:disabled)`, `:focus-visible`, `:disabled`, `:checked`, `:indeterminate`, `:user-invalid`, `:user-valid`, `:placeholder-shown`, `:open` / `[open]`, `:modal`, `:popover-open` | `.is-disabled`, `.is-checked`, `.is-invalid`, `.is-open` |
| **État d'accessibilité / widget composite** | Attributs ARIA directement en sélecteur | `[aria-expanded="true"]`, `[aria-pressed="true"]`, `[aria-current="page"]`, `[aria-disabled="true"]`, `[aria-checked="true"]`, `[aria-invalid="true"]` | `.is-active` à côté de `aria-current="page"` |
| **Présence d'un enfant ou état descendant** | `:has()` | `.pt-card:has(> .pt-card-icon)`, `.pt-field:has(:user-invalid)` | `.pt-card--has-icon`, `.has-error` |
| **Adaptation au contexte ou à l'espace** | `@container (inline-size ...)` et `@container style(--...)` | `@container settings-card (inline-size < 26rem)`, `@container style(--icon-start)` | `.card-compact`, `.icon-start` |

### Raisons et sources

- **Rejet explicite de BEM** : MWG `css` recommande : *« Avoid introducing BEM naming conventions to manage specificity. Instead, use modern CSS features such as cascade layers and `:where()` »*. Les noms plats `.pt-<composant>-<rôle>` sont courts, prévisibles et faciles à contraindre par regex dans Stylelint (`^pt-[a-z][a-z0-9]*(-[a-z0-9]+)*$`).
- **Impossible de désynchroniser l'état visuel et l'arbre d'accessibilité** : lorsqu'un agent écrit le CSS sur `[aria-expanded="true"]`, `:checked`, `:disabled` ou `:popover-open`, l'état visuel ne fonctionne *que* si l'attribut sémantique correct est présent dans le DOM. Cela élimine toute classe fantôme `.is-open` / `.is-disabled` (MWG `accessibility`, `menu`, `dialog`).
- **Exclure les états non pertinents avec `:not()`** : MWG `css` recommande `:hover:not(:disabled)` plutôt qu'une surcharge ultérieure sur `:disabled`, afin que l'ordre des règles ne puisse jamais créer de fuite de survol sur un contrôle désactivé.
- **Validation avec `:user-invalid` uniquement** : MWG `browser-ui-theming` interdit `:invalid` nu (qui marque les champs requis en erreur dès l'ouverture de la fenêtre) au profit de `:user-invalid` (natif depuis Edge 119, Baseline Widely Available — [https://developer.mozilla.org/en-US/docs/Web/CSS/:user-invalid](https://developer.mozilla.org/en-US/docs/Web/CSS/:user-invalid)).
- **Couplage direct avec les props TypeScript Svelte 5** : une prop typée `variant?: 'standard' | 'accent' | 'subtle'` se projette en un seul attribut `data-variant={variant === 'standard' ? undefined : variant}`, sans concaténation conditionnelle de chaînes de classes.

---

## 5. Utilitaires : liste fermée et garde-fou anti-Tailwind

### Décision : **Oui, une liste fermée de 6 utilitaires purement structurels, typographiques et d'accessibilité (`.u-*`) dans `apps/ui/src/styles/utilities.css`**, zéro utilitaire cosmétique, verrouillée mécaniquement en CI

### Liste fermée exhaustive (6 utilitaires, pas un de plus)

| Classe | Rôle exact | Déclarations principales |
|---|---|---|
| `.u-stack` | Empilement vertical (colonne) de cartes ou de sections | `display: flex; flex-direction: column; gap: var(--_gap, var(--space-3));` + variantes `data-gap="card"` (`--space-1`, 4 px), `data-gap="group"` (`--space-3`), `data-gap="section"` (`--space-6`, 24 px) |
| `.u-cluster` | Groupe horizontal souple d'actions / badges / filtres | `display: flex; flex-wrap: wrap; align-items: safe center; gap: var(--_gap, var(--space-2));` + `data-justify="between" \| "end"` (MWG `css-layout`) |
| `.u-sr-only` | Texte accessible uniquement aux lecteurs d'écran (skip link, contexte de bouton) | Recette canonique MWG `accessibility` : `.u-sr-only:where(:not(:focus-within, :active))` avec `clip-path: inset(50%)` *(seule exception documentée autorisant `!important`)* |
| `.u-tabular` | Alignement stable des chiffres temps réel (watts, durées, horodatages) | `font-variant-numeric: tabular-nums;` |
| `.u-truncate` | Troncature mono-ligne pour identifiants bornés (EDID moniteur, nom de plan) — jamais sur un paragraphe | `overflow: hidden; text-overflow: ellipsis; white-space: nowrap; min-inline-size: 0;` |
| `.u-scroll-region` | Zone défilable bornée sans décalage de mise en page ni chaînage de scroll | `overflow-y: auto; scrollbar-gutter: stable; overscroll-behavior: contain;` (MWG `css-layout`) |

### Règle qui empêche la dérive vers un clone de Tailwind

1. **Critère normatif** : un utilitaire `.u-*` encode uniquement une **primitive de composition** (flux 1D, scroll), un **comportement d'accessibilité** (`.u-sr-only`) ou une **règle micro-typographique** (`.u-tabular`, `.u-truncate`). **Aucun utilitaire ne doit jamais mapper une propriété visuelle isolée** (interdiction absolue de `.u-p-4`, `.u-mt-2`, `.u-text-secondary`, `.u-bg-card`, `.u-rounded`, `.u-flex`, `.u-gap-2`).
2. **Verrouillage mécanique en CI** :
   - Stylelint interdit de définir une classe `.u-*` ailleurs que dans `src/styles/utilities.css`.
   - Le script de contrôle `scripts/check-css-architecture.mjs` échoue la CI si `utilities.css` déclare **plus de 8 classes `.u-*`** (marge maximale de +2 après revue humaine explicite et mise à jour de `DESIGN.md`) ou contient toute propriété cosmétique (`color`, `background*`, `border-radius`, `box-shadow`, `font-size`, `padding*`, `margin*` hors `.u-sr-only`).

### Raisons et sources

- **Composition sèche sans boilerplate dans les vues** : `.u-stack` et `.u-cluster` couvrent ~80 % des besoins de mise en page des panneaux de paramètres Windows 11 (`SettingsCardSpacing = 4px` en pile, rangées d'actions alignées avec `align-items: safe center`, MWG `css-layout`) tout en évitant le style en ligne `style="gap: ..."` grâce à `data-gap`.
- **Conformité accessibilité** : `.u-sr-only:where(:not(:focus-within, :active))` reprend mot pour mot la recette de MWG `accessibility`, garantissant qu'un lien d'évitement (*skip link*) redevient visible au focus clavier.

---

## 6. Exemples complets (CSS + Svelte 5 runes)

> **Rappel** : aucun des trois fichiers `.svelte` ci-dessous ne contient de bloc `<style>`. Leurs styles vivent dans `apps/ui/src/styles/components/` (importés dans `@layer components` via `index.css`). Ils supposent l'anneau de focus de base défini dans `base.css` et respectent l'ensemble des règles Stylelint de la section 7.

### 6.1 `Button` (variantes `standard` et `accent`)

#### `apps/ui/src/styles/components/button.css`

```css
/* apps/ui/src/styles/components/button.css — chargé dans @layer components */
.pt-button {
  --_bg: var(--control-fill-default);
  --_bg-hover: var(--control-fill-secondary);
  --_bg-active: var(--control-fill-tertiary);
  --_bg-disabled: var(--control-fill-disabled);
  --_fg: var(--text-primary);
  --_fg-active: var(--text-secondary);
  --_fg-disabled: var(--text-disabled);
  --_border: var(--stroke-control);
  --_border-bottom: var(--stroke-control-secondary);

  display: inline-flex;
  align-items: safe center;
  justify-content: safe center;
  gap: var(--space-2);
  min-block-size: var(--control-height-md);
  padding-block: var(--space-1);
  padding-inline: var(--space-3);
  border: var(--border-width) solid var(--_border);
  border-block-end-color: var(--_border-bottom);
  border-radius: var(--radius-control);
  background-color: var(--_bg);
  color: var(--_fg);
  font-family: var(--font-body);
  font-size: var(--text-size-body);
  font-weight: var(--weight-regular);
  line-height: var(--leading-body);
  text-align: center;
  cursor: pointer;
  user-select: none;
  transition:
    background-color var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard),
    color var(--duration-fast) var(--ease-standard);

  &[data-variant="accent"] {
    --_bg: var(--accent-fill-default);
    --_bg-hover: var(--accent-fill-secondary);
    --_bg-active: var(--accent-fill-tertiary);
    --_bg-disabled: var(--accent-fill-disabled);
    --_fg: var(--text-on-accent-primary);
    --_fg-active: var(--text-on-accent-secondary);
    --_fg-disabled: var(--text-on-accent-disabled);
    --_border: transparent;
    --_border-bottom: var(--stroke-control-secondary);
  }

  &[data-variant="subtle"] {
    --_bg: transparent;
    --_bg-hover: var(--subtle-fill-secondary);
    --_bg-active: var(--subtle-fill-tertiary);
    --_bg-disabled: transparent;
    --_border: transparent;
    --_border-bottom: transparent;
  }

  &:hover:not(:disabled, [aria-disabled="true"]) {
    background-color: var(--_bg-hover);
  }

  &:active:not(:disabled, [aria-disabled="true"]) {
    background-color: var(--_bg-active);
    border-block-end-color: var(--_border);
    color: var(--_fg-active);
  }

  &:focus-visible {
    outline: var(--focus-width) solid var(--focus-stroke-outer);
    outline-offset: var(--focus-offset);
  }

  &:is(:disabled, [aria-disabled="true"]) {
    background-color: var(--_bg-disabled);
    border-color: var(--_border);
    color: var(--_fg-disabled);
    cursor: not-allowed;
  }

  @media (pointer: coarse) {
    min-block-size: var(--control-height-touch);
  }

  @media (forced-colors: active) {
    --_border: ButtonBorder;
    --_border-bottom: ButtonBorder;

    &:hover:not(:disabled, [aria-disabled="true"]) {
      border-color: Highlight;
      color: Highlight;
    }

    &[data-variant="accent"] {
      forced-color-adjust: none;
      --_bg: Highlight;
      --_bg-hover: Highlight;
      --_bg-active: Highlight;
      --_fg: HighlightText;
      --_fg-active: HighlightText;
      --_border: Highlight;
      --_border-bottom: Highlight;

      &:hover:not(:disabled, [aria-disabled="true"]) {
        border-color: CanvasText;
        color: HighlightText;
      }
    }

    &:is(:disabled, [aria-disabled="true"]) {
      --_bg-disabled: Canvas;
      --_fg-disabled: GrayText;
      --_border: GrayText;
    }
  }
}
```

> **Note `prefers-reduced-motion` sur `Button`** : conformément à MWG `motion` (*« Keep non-motion transitions (such as `color` or `background-color` changes on links and buttons) enabled; only dampen or disable spatial movement, scaling, rotation, or persistent animations »*), les transitions de couleur/fond rapides (`83ms`) restent actives ; aucun mouvement spatial n'est appliqué au bouton.

#### `apps/ui/src/components/Button.svelte`

```svelte
<script lang="ts">
  import type { Snippet } from 'svelte';
  import type { HTMLButtonAttributes } from 'svelte/elements';

  interface Props extends HTMLButtonAttributes {
    variant?: 'standard' | 'accent' | 'subtle';
    children?: Snippet;
  }

  let {
    variant = 'standard',
    type = 'button',
    class: className,
    children,
    ...rest
  }: Props = $props();
</script>

<button
  {...rest}
  {type}
  class={['pt-button', className]}
  data-variant={variant === 'standard' ? undefined : variant}
>
  {@render children?.()}
</button>
```

---

### 6.2 `ToggleSwitch` (`<input type="checkbox" role="switch">`)

> **Rappel WebView2** : `<input type="checkbox" switch>` est exclusif à Safari 17.4+ et non supporté dans Chromium/Edge (MWG `switch`). On utilise `<input type="checkbox" role="switch">` avec `appearance: none`, étiquetage explicite `for`/`id` via `$props.id()` ([https://svelte.dev/docs/svelte/$props](https://svelte.dev/docs/svelte/$props)), et libellé d'état visuel optionnel masqué aux lecteurs d'écran (`aria-hidden="true"`) pour éviter la redondance avec l'état ARIA du rôle `switch` (MWG `accessibility`).

#### `apps/ui/src/styles/components/toggle-switch.css`

```css
/* apps/ui/src/styles/components/toggle-switch.css — chargé dans @layer components */
.pt-switch-field {
  display: inline-flex;
  align-items: safe center;
  gap: var(--space-3);
  min-block-size: var(--control-height-md);
  color: var(--text-primary);
  font-size: var(--text-size-body);
  line-height: var(--leading-body);
  cursor: pointer;

  &:has(> .pt-switch:disabled) {
    color: var(--text-disabled);
    cursor: not-allowed;
  }

  @media (pointer: coarse) {
    min-block-size: var(--control-height-touch);
  }
}

.pt-switch-state {
  min-inline-size: 3.5ch;
  color: var(--text-primary);
  user-select: none;

  .pt-switch-field:has(> .pt-switch:disabled) & {
    color: var(--text-disabled);
  }
}

.pt-switch {
  /* Anatomie Fluent 2 / WinUI 3 (40x20px en rem pour suivre text-scale OS) */
  --_track-w: 2.5rem;
  --_track-h: 1.25rem;
  --_knob: 0.75rem;
  --_pad: 0.1875rem;
  --_travel: calc(var(--_track-w) - var(--_knob) - var(--_pad) * 2 - var(--border-width) * 2);
  --_dir: 1;

  appearance: none;
  position: relative;
  display: inline-flex;
  align-items: center;
  flex: none;
  inline-size: var(--_track-w);
  block-size: var(--_track-h);
  margin: 0;
  padding: var(--_pad);
  border: var(--border-width) solid var(--stroke-control-strong);
  border-radius: var(--radius-pill);
  background-color: var(--control-alt-secondary);
  cursor: inherit;
  transition:
    background-color var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard);

  &:dir(rtl) {
    --_dir: -1;
  }

  &::before {
    content: "";
    display: block;
    inline-size: var(--_knob);
    block-size: var(--_knob);
    border-radius: var(--radius-pill);
    background-color: var(--text-secondary);
    translate: 0 0;
    transition: background-color var(--duration-fast) var(--ease-standard);

    @media (prefers-reduced-motion: no-preference) {
      transition:
        translate var(--duration-normal) var(--ease-standard),
        scale var(--duration-fast) var(--ease-standard),
        background-color var(--duration-fast) var(--ease-standard);
    }
  }

  /* Repos -> Survol / Pressé (Off) */
  &:hover:not(:disabled) {
    background-color: var(--control-alt-tertiary);

    &::before {
      @media (prefers-reduced-motion: no-preference) {
        scale: 1.167;
      }
    }
  }

  &:active:not(:disabled) {
    background-color: var(--control-fill-tertiary);

    &::before {
      @media (prefers-reduced-motion: no-preference) {
        scale: 1.35 1.167;
      }
    }
  }

  /* État activé (On) */
  &:checked {
    border-color: var(--accent-fill-default);
    background-color: var(--accent-fill-default);

    &::before {
      background-color: var(--text-on-accent-primary);
      translate: calc(var(--_travel) * var(--_dir)) 0;
    }

    &:hover:not(:disabled) {
      border-color: var(--accent-fill-secondary);
      background-color: var(--accent-fill-secondary);
    }

    &:active:not(:disabled) {
      border-color: var(--accent-fill-tertiary);
      background-color: var(--accent-fill-tertiary);
    }
  }

  /* Focus clavier uniquement */
  &:focus-visible {
    outline: var(--focus-width) solid var(--focus-stroke-outer);
    outline-offset: var(--focus-offset);
  }

  /* Désactivé */
  &:disabled {
    border-color: var(--stroke-control-strong-disabled);
    background-color: transparent;

    &::before {
      background-color: var(--text-disabled);
    }

    &:checked {
      border-color: transparent;
      background-color: var(--accent-fill-disabled);

      &::before {
        background-color: var(--text-on-accent-disabled);
      }
    }
  }

  /* Contraste élevé Windows : préserver la géométrie piste + pastille */
  @media (forced-colors: active) {
    forced-color-adjust: none;
    border-color: ButtonText;
    background-color: Canvas;

    &::before {
      background-color: ButtonText;
    }

    &:checked {
      border-color: Highlight;
      background-color: Highlight;

      &::before {
        background-color: HighlightText;
      }
    }

    &:disabled {
      border-color: GrayText;
      background-color: Canvas;

      &::before {
        background-color: GrayText;
      }

      &:checked {
        border-color: GrayText;
        background-color: GrayText;

        &::before {
          background-color: Canvas;
        }
      }
    }
  }
}
```

#### `apps/ui/src/components/ToggleSwitch.svelte`

```svelte
<script lang="ts">
  import type { HTMLInputAttributes } from 'svelte/elements';

  interface Props extends Omit<HTMLInputAttributes, 'type' | 'checked'> {
    checked?: boolean;
    label: string;
    hideLabel?: boolean;
    showStateText?: boolean;
    onText?: string;
    offText?: string;
  }

  let {
    checked = $bindable(false),
    label,
    hideLabel = false,
    showStateText = true,
    onText = 'Activé',
    offText = 'Désactivé',
    id,
    class: className,
    ...rest
  }: Props = $props();

  const generatedId = $props.id();
  const inputId = $derived(id ?? generatedId);
</script>

<label class={['pt-switch-field', className]} for={inputId}>
  <span class={hideLabel ? 'u-sr-only' : undefined}>{label}</span>
  {#if showStateText}
    <span class="pt-switch-state" aria-hidden="true">
      {checked ? onText : offText}
    </span>
  {/if}
  <input
    {...rest}
    id={inputId}
    type="checkbox"
    role="switch"
    class="pt-switch"
    bind:checked
  />
</label>
```

---

### 6.3 `SettingsCard` (statique ou cliquable, adaptative par container query + `:has()`)

#### `apps/ui/src/styles/components/settings-card.css`

```css
/* apps/ui/src/styles/components/settings-card.css — chargé dans @layer components */
.pt-card {
  container: settings-card / inline-size;

  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: safe center;
  column-gap: var(--space-4);
  row-gap: var(--space-2);
  inline-size: 100%;
  min-block-size: 4.25rem;
  padding-block: var(--space-4);
  padding-inline: var(--space-4);
  border: var(--border-width) solid var(--stroke-card);
  border-radius: var(--radius-card);
  background-color: var(--surface-card);
  color: var(--text-primary);
  text-align: start;

  /* Colonne d'icône activée uniquement si présente (zéro piste fantôme) */
  &:has(> .pt-card-icon) {
    grid-template-columns: auto minmax(0, 1fr) auto;
  }

  /* Variante interactive (SettingsCard action/navigation sur <button>) */
  &:is(button) {
    cursor: pointer;
    transition:
      background-color var(--duration-fast) var(--ease-standard),
      border-color var(--duration-fast) var(--ease-standard);

    &:hover:not(:disabled, [aria-disabled="true"]) {
      background-color: var(--surface-card-hover);
    }

    &:active:not(:disabled, [aria-disabled="true"]) {
      background-color: var(--surface-card-pressed);
    }

    &:focus-visible {
      outline: var(--focus-width) solid var(--focus-stroke-outer);
      outline-offset: var(--focus-offset);
    }

    &:is(:disabled, [aria-disabled="true"]) {
      background-color: var(--control-fill-disabled);
      color: var(--text-disabled);
      cursor: not-allowed;
    }

    @media (forced-colors: active) {
      &:hover:not(:disabled, [aria-disabled="true"]) {
        border-color: Highlight;
      }
    }
  }
}

.pt-card-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--text-primary);
  font-size: var(--text-size-subtitle);
}

.pt-card-header {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  min-inline-size: 0;
}

.pt-card-title {
  margin: 0;
  color: var(--text-primary);
  font-family: var(--font-body);
  font-size: var(--text-size-body);
  font-weight: var(--weight-regular);
  line-height: var(--leading-body);
}

.pt-card-desc {
  margin: 0;
  color: var(--text-secondary);
  font-family: var(--font-caption);
  font-size: var(--text-size-caption);
  font-weight: var(--weight-regular);
  line-height: var(--leading-caption);
}

.pt-card-control {
  display: inline-flex;
  align-items: safe center;
  gap: var(--space-2);
  justify-self: end;
}

/* Adaptation au conteneur étroit (fenêtre réduite ou fort text-scale OS) */
@container settings-card (inline-size < 26rem) {
  .pt-card-control {
    grid-column: 1 / -1;
    justify-self: start;
  }
}
```

> **Note sur la géométrie `SettingsCard`** : on utilise `min-block-size: 4.25rem` (68 px, proche de la hauteur standard Windows 11 Settings avec description) et jamais `height` fixe, afin de satisfaire WCAG 1.4.12 (*Text Spacing*) et l'agrandissement `text-scale` (MWG `typography`, `respect-os-text-scale`). Sur une carte cliquable (`<button class="pt-card">`), l'icône chevron est posée en sortie de `.pt-card-control` via le moteur d'icônes CSS (`--icon: var(--icon-chevron-right)`).

#### `apps/ui/src/components/SettingsCard.svelte`

```svelte
<script lang="ts">
  import type { Snippet } from 'svelte';

  interface Props {
    title: string;
    description?: string;
    disabled?: boolean;
    onclick?: (event: MouseEvent) => void;
    icon?: Snippet;
    control?: Snippet;
    class?: string;
  }

  let {
    title,
    description,
    disabled = false,
    onclick,
    icon,
    control,
    class: className,
  }: Props = $props();
</script>

{#snippet body()}
  {#if icon}
    <span class="pt-card-icon" aria-hidden="true">
      {@render icon()}
    </span>
  {/if}

  <span class="pt-card-header">
    <span class="pt-card-title">{title}</span>
    {#if description}
      <span class="pt-card-desc">{description}</span>
    {/if}
  </span>

  {#if control}
    <span class="pt-card-control">
      {@render control()}
    </span>
  {:else if onclick}
    <span class="pt-card-control" aria-hidden="true">
      <span class="icon pt-chevron"></span>
    </span>
  {/if}
{/snippet}

{#if onclick}
  <button
    type="button"
    class={['pt-card', className]}
    {disabled}
    {onclick}
  >
    {@render body()}
  </button>
{:else}
  <div class={['pt-card', className]}>
    {@render body()}
  </div>
{/if}
```

---

## 7. Garde-fous mécaniques : Stylelint, contrôles Svelte et synchronisation `DESIGN.md` ↔ `tokens.css`

### Décision : **Stylelint (`stylelint-config-standard` + `stylelint-config-html/svelte` + `stylelint-declaration-strict-value`) + `svelte-check` strict + `tokens.css` comme source exécutable vérifiée bidirectionnellement contre `DESIGN.md` en CI**

### 7.1 Configuration Stylelint (`apps/ui/stylelint.config.mjs`)

Outils retenus (tous standards et maintenus ; versions exactes non épinglées car non interrogées sur le registre npm en direct, seul le support de Stylelint 16 par `stylelint-config-html` v2+ et `stylelint-declaration-strict-value` a été vérifié) :
- `stylelint` + `stylelint-config-standard` ([https://stylelint.io/](https://stylelint.io/))
- `stylelint-config-html` (embarque `postcss-html` pour `<style>` dans `.svelte`, [https://www.npmjs.com/package/stylelint-config-html](https://www.npmjs.com/package/stylelint-config-html))
- `stylelint-declaration-strict-value` ([https://github.com/AndyOGo/stylelint-declaration-strict-value](https://github.com/AndyOGo/stylelint-declaration-strict-value)) — unique plugin tiers, pour imposer `var(--...)` sur les propriétés tokenisées.

```js
// apps/ui/stylelint.config.mjs
const SYSTEM_COLORS = [
  'Canvas', 'CanvasText', 'ButtonFace', 'ButtonText', 'ButtonBorder',
  'Highlight', 'HighlightText', 'GrayText', 'Field', 'FieldText', 'LinkText',
];

const CSS_KEYWORDS = [
  'transparent', 'currentColor', 'inherit', 'initial', 'unset',
  'revert', 'revert-layer', 'none', 'auto', '0',
];

/** @type {import('stylelint').Config} */
export default {
  extends: ['stylelint-config-standard', 'stylelint-config-html/svelte'],
  plugins: ['stylelint-declaration-strict-value'],
  // Résolution native des custom properties globales (règle core Stylelint)
  referenceFiles: ['src/styles/tokens.css', 'src/styles/icons.css'],
  rules: {
    // 1. Zéro token inconnu (faute de frappe dans var(--...))
    'no-unknown-custom-properties': true,

    // 2. Zéro couleur brute hors tokens.css
    'color-no-hex': true,
    'color-named': 'never',
    'function-disallowed-list': ['rgb', 'rgba', 'hsl', 'hsla', 'hwb', 'oklch', 'oklab', 'lab', 'lch'],

    // 3. Zéro nombre magique sur les propriétés tokenisées (laisse libre inline-size/block-size d'anatomie)
    'scale-unlimited/declaration-strict-value': [
      [
        'color', 'background-color', '/border.*-color$/', 'outline-color',
        'fill', 'stroke', 'font-family', 'font-size', 'font-weight',
        'line-height', 'border-radius', 'gap', 'row-gap', 'column-gap',
        '/^padding/', '/^margin/', 'transition-duration', 'animation-duration',
        'box-shadow', 'z-index',
      ],
      {
        ignoreValues: [...CSS_KEYWORDS, ...SYSTEM_COLORS],
        expandShorthand: true,
      },
    ],

    // 4. Contrôle strict des couches, de la spécificité et de la profondeur
    'layer-name-pattern': '^(reset|tokens|base|components|views|utilities)$',
    'declaration-no-important': true,
    'selector-max-id': 0,
    'selector-max-combinators': 2,
    'max-nesting-depth': [3, { ignore: ['blockless-at-rules', 'pseudo-classes'] }],

    // 5. Propriétés logiques obligatoires pour marges/paddings
    'property-disallowed-list': [
      'margin-left', 'margin-right', 'margin-top', 'margin-bottom',
      'padding-left', 'padding-right', 'padding-top', 'padding-bottom',
    ],

    // 6. Garde-fous MWG (a11y, perf, bug Chromium 565837690)
    'declaration-property-value-disallowed-list': {
      '/^transition/': ['/\\ball\\b/'],
      outline: ['none', '0'],
      'scrollbar-width': ['thin', 'none'],
      '/.+/': ['/color-mix\\(\\s*in\\s+oklch/i'],
    },
  },
  overrides: [
    {
      // tokens.css est le SEUL endroit autorisé pour les couleurs et valeurs brutes
      files: ['src/styles/tokens.css'],
      rules: {
        'color-no-hex': null,
        'function-disallowed-list': ['oklch', 'oklab', 'lab', 'lch'],
        'scale-unlimited/declaration-strict-value': null,
      },
    },
    {
      files: ['src/styles/reset.css', 'src/styles/icons.css'],
      rules: {
        'scale-unlimited/declaration-strict-value': null,
      },
    },
    {
      // Primitives : classes plates .pt-* uniquement (zéro BEM, zéro @layer local)
      files: ['src/styles/components/*.css'],
      rules: {
        'at-rule-disallowed-list': ['layer', 'property'],
        'selector-class-pattern': '^pt-[a-z][a-z0-9]*(-[a-z0-9]+)*$',
      },
    },
    {
      // Utilitaires : classes .u-* uniquement, zéro propriété cosmétique
      files: ['src/styles/utilities.css'],
      rules: {
        'at-rule-disallowed-list': ['layer', 'property'],
        'selector-class-pattern': '^u-[a-z][a-z0-9]*(-[a-z0-9]+)*$',
      },
    },
    {
      // Svelte : zéro :global(), zéro @layer, zéro @property, zéro redéfinition .pt-*/.u-*
      files: ['src/**/*.svelte'],
      rules: {
        'at-rule-disallowed-list': ['layer', 'property'],
        'selector-pseudo-class-disallowed-list': ['global'],
        'selector-class-pattern': '^(?!pt-|u-)[a-z][a-z0-9]*(-[a-z0-9]+)*$',
      },
    },
  ],
};
```

> **Note de vérification** : chaque règle Stylelint ci-dessus (`no-unknown-custom-properties` + `referenceFiles`, `color-no-hex`, `color-named`, `function-disallowed-list`, `layer-name-pattern`, `declaration-no-important`, `selector-max-id`, `selector-max-combinators`, `max-nesting-depth`, `property-disallowed-list`, `declaration-property-value-disallowed-list`, `selector-pseudo-class-disallowed-list`, `selector-class-pattern`, `overrides`) est documentée sur [https://stylelint.io/user-guide/rules](https://stylelint.io/user-guide/rules) et [https://stylelint.io/user-guide/configure/](https://stylelint.io/user-guide/configure/). La version mineure exacte de Stylelint introduisant `referenceFiles` n'a pas été vérifiée ; si une version antérieure devait être installée, le plugin officiel `csstools/stylelint-value-no-unknown-custom-properties` (`importFrom`) offre le même contrôle ([https://github.com/csstools/stylelint-value-no-unknown-custom-properties](https://github.com/csstools/stylelint-value-no-unknown-custom-properties)). De même, la règle `selector-class-pattern` sur `.svelte` interdit de déclarer ou surcharger une classe `.pt-*` ou `.u-*` dans un `<style>` de vue.

### 7.2 Vérifications spécifiques Svelte et architecture

1. **`svelte-check` (`pnpm exec svelte-check --tsconfig ./tsconfig.json --fail-on-warnings`)** : avec `compilerOptions.runes = true`, bloque toute syntaxe Svelte 4, toute erreur d'accessibilité (`a11y_*`) et tout sélecteur mort (`css_unused_selector`) ([https://svelte.dev/docs/svelte/compiler-warnings](https://svelte.dev/docs/svelte/compiler-warnings)).
2. **Garde-fou d'architecture (`apps/ui/scripts/check-css-architecture.mjs`, ~50 lignes Node ESM + PostCSS déjà présent)** exécuté dans `pnpm check` :
   - Échoue si `src/styles/utilities.css` déclare plus de 8 classes `.u-*` ou contient une propriété cosmétique hors `.u-sr-only`.
   - Échoue si un fichier dans `src/components/*.svelte` (hors liste blanche `TelemetryChart.svelte`) contient un bloc `<style>`.
   - Échoue si `:root` ou `html` déclare `font-size` (protège `<meta name="text-scale" content="scale">`, MWG `respect-os-text-scale`).
   - Échoue si un template `.svelte` utilise `style="..."` hors attributs CSS autorisés (`--icon`, `--icon-start`, `--icon-end`, `--control-value` pour `Slider`).

### 7.3 Synchronisation `DESIGN.md` ↔ `tokens.css`

- **Source de vérité** : **`apps/ui/src/styles/tokens.css` est la source de vérité exécutable** pour les valeurs CSS ; **`DESIGN.md` (à la racine) est le contrat de design** (front-matter YAML + prose normative) maintenu en **parité stricte 1-pour-1 vérifiée en CI**.
- **Pourquoi ne pas générer `tokens.css` depuis `DESIGN.md` ?** L'outil officiel `@google/design.md` ([https://github.com/google-labs-code/design.md](https://github.com/google-labs-code/design.md), [https://www.npmjs.com/package/@google/design.md](https://www.npmjs.com/package/@google/design.md)) fournit `lint`, `diff` et `export` vers `json-tailwind`, `css-tailwind` et `dtcg` (W3C Design Tokens), mais ne génère pas nativement `light-dark()`, `color-mix(in srgb, var(--win-accent-*), ...)`, `:root[data-backdrop="solid"]` ni `@media (forced-colors: active)`. Inventer un DSL YAML propriétaire pour générer tout `tokens.css` casserait `npx @google/design.md lint DESIGN.md`.
- **Détection mécanique de dérive (2 commandes en CI)** :
  1. `npx @google/design.md lint DESIGN.md` — valide la conformité structurelle de `DESIGN.md`, les références de tokens et les ratios de contraste WCAG.
  2. `node apps/ui/scripts/check-tokens-sync.mjs` — parse l'AST de `tokens.css` (via `postcss`) et le YAML de `DESIGN.md`, puis vérifie :
     - que chaque token déclaré dans `DESIGN.md` existe dans `tokens.css` avec la même valeur (ou paire clair/sombre exacte dans `light-dark(L, D)`) ;
     - que chaque token de niveau 1 ou 2 ajouté dans `tokens.css` est présent dans `DESIGN.md` (empêche un agent d'ajouter un token « discret » uniquement en CSS) ;
     - que chaque classe `.pt-*` et `.u-*` documentée correspond aux fichiers réellement présents dans `src/styles/components/` et `src/styles/utilities.css`.

---

## 8. Règles CSS prêtes à coller dans `AGENTS.md` (12 lignes)

```markdown
## CSS & UI Styling Rules (`powertamer-ui`)
- Target Evergreen WebView2 (Chromium/Edge 146+) on Windows 11 only: skip polyfills and Safari/Firefox fallbacks (exception: `<input type="checkbox" role="switch">` instead of Safari-only `switch`, and tiny fallback for `focusgroup` if `< 150`).
- Single owner per rule: all 12 Fluent 2 primitives (`.pt-*`) live in `apps/ui/src/styles/components/*.css` (no `<style>` in `src/components/*.svelte` except `TelemetryChart.svelte`); Svelte `<style>` is for view-local layout only (auto-wrapped in `@layer views`).
- Never add `@layer`, `@property`, or `:global()` inside components or views; `apps/ui/src/styles/index.css` owns the sole layer order `@layer reset, tokens, base, components, views, utilities;`.
- Never write raw colors, hex codes, or magic spacing/type/radius/duration values outside `tokens.css`; use `var(--...)` tokens and `light-dark()`, never `color-mix(in oklch, ...)` (Chromium bug 565837690).
- Keep `DESIGN.md` and `apps/ui/src/styles/tokens.css` in strict 1:1 sync; never add or change a token without updating both and passing `check-tokens-sync`.
- No BEM (`__`, `--`) and no state classes (`.is-open`, `.is-disabled`, `.active`): use flat `.pt-*` classes, `data-variant`/`data-tone` for visual variants, and native/ARIA selectors (`:checked`, `:disabled`, `:user-invalid`, `:popover-open`, `[aria-expanded="true"]`, `:has()`) for all states.
- Utilities are a closed list of 6 classes in `utilities.css` (`.u-stack`, `.u-cluster`, `.u-sr-only`, `.u-tabular`, `.u-truncate`, `.u-scroll-region`); never add cosmetic or single-property Tailwind-style utilities.
- Never use `*` resets, `#id` selectors, `!important` (except `.u-sr-only`), `outline: none`, `transition: all`, or `scrollbar-width: thin`.
- Use logical properties (`inline`/`block`), `rem` font sizes, unitless `line-height`, and `min-block-size` (never fixed `height` on text containers, never `font-size` on `html`/`:root`).
- Every interactive control must style `:hover:not(:disabled)`, `:active:not(:disabled)`, `:disabled`, `:focus-visible` (`outline`, ≥ 3:1 contrast), `@media (forced-colors: active)`, and guard spatial motion inside `@media (prefers-reduced-motion: no-preference)`.
- Before finishing any UI change, run `pnpm lint:css`, `pnpm check` (`svelte-check` + architecture/token-sync checks), and `@google/design.md lint DESIGN.md`.
```

---

## Modern Web Guidance : guides exploités, impact et fallbacks supprimés

Les 29 guides fournis dans `.agy-runs/mwg/` ont été lus. Le tableau ci-dessous recense ceux qui ont directement façonné les décisions, ainsi que les fallbacks supprimés grâce à la cible **Evergreen WebView2 (Edge 146+)** (le poste de référence tourne déjà sous WebView2 158 d'après [docs/research/2026-11-27-ui-stack-decision.md](../../docs/research/2026-11-27-ui-stack-decision.md)).

| Guide ID | Ce qu'il a changé dans l'architecture | Fallbacks du guide supprimés (WebView2 146+) |
|---|---|---|
| `css` | Interdiction du reset `*` dans `reset.css`, rejet de BEM au profit de `@layer` + `:where()`, `:not(:disabled)` anti-*overmatching*, propriétés logiques raisonnées, limitation à 2 niveaux de tokens globaux. | Aucun fallback à maintenir. |
| `css-layout` | Utilitaires `.u-stack` / `.u-cluster` (`align-items: safe center`, `flex-wrap: wrap`), `.u-scroll-region` (`scrollbar-gutter: stable`, `overscroll-behavior: contain`), `overflow: clip`, `min-inline-size: 0`. | Suppression du repli `grid-template-rows` pré-subgrid (Edge 117+) et du `@supports (anchor-name: --x)` (Edge 129+/157). |
| `design-token-reactivity` & `usage-aware-component-variations` | Adoption de `@container style(...)` pour le moteur d'icônes et les variations contextuelles découplées sans `container-type`, réservées aux cas réellement extensibles (YAGNI pour le reste). | Suppression des sélecteurs `:where([data-...])` de repli et des tests JS `getComputedStyle` (Edge 111+). |
| `selector-atrule-combinations` | Confirmation que `light-dark()` suffit pour clair/sombre sans dupliquer les règles ; patron `--color-scheme` + `@container style()` disponible si un sélecteur de thème manuel est ajouté plus tard. | Suppression de la duplication de règles `@media (prefers-color-scheme: dark)`. |
| `size-aware-styling` & `responsive-design` | `container: settings-card / inline-size` directement sur `.pt-card` et sur le shell de `NavigationView`, jamais de media queries viewport pour l'agencement interne des composants. | Suppression du repli `@media` + `@supports (container-type: inline-size)` (Edge 105+). |
| `content-based-styling` | `:has(> .pt-card-icon)` sur `.pt-card` et `:has(> .pt-switch:disabled)` sur `.pt-switch-field` pour supprimer toute classe `.has-icon` / `.is-disabled`. | Suppression de `@supports not selector(:has(*))` et du script JS de pose de classes (Edge 105+). |
| `color` | `color-mix(in srgb, ...)` pour l'opacité d'accent (interdiction formelle de `in oklch` via Stylelint à cause du bug Chromium **565837690**), bloc `@media (forced-colors: active)` complet, `forced-color-adjust: none` strictement borné aux pastilles/icônes masquées, contrastes 4.5:1 / 3:1. | Bug Chromium **conservé** en garde-fou Stylelint car WebView2 est Chromium. |
| `dark-mode` | `<meta name="color-scheme" content="light dark">` + `:root { color-scheme: light dark; }` + `light-dark()` + `scrollbar-color` + `accent-color` sur `:root`. | Suppression des `@supports (color: light-dark(...))`, des blocs `@media (prefers-color-scheme: dark)` en double, du repli `::-webkit-scrollbar` (Edge 121+) et du hack `image-set()` (Edge 150+). |
| `typography` & `respect-os-text-scale` | `<meta name="text-scale" content="scale">`, interdiction de `font-size` sur `html`/`:root` (vérifiée en CI), unités `rem` + `line-height` sans unité, `min-block-size` au lieu de `height` fixe sur boutons et cartes. | Natif depuis Edge 146 ; dégradation gracieuse sans code additionnel. |
| `browser-ui-theming` & `brand-consistent-forms` | `:user-invalid` / `:user-valid` (jamais `:invalid`), interdiction de `scrollbar-width: thin` (Stylelint) et de `user-select: none` sur le texte de contenu, `accent-color` natif sur `:root`. | Suppression du repli `@supports not (accent-color: ...)` et de l'input masqué (Edge 93+). |
| `branded-select-styling` | `Select` natif stylé via `appearance: base-select`, `::picker(select)`, `::picker-icon`, `option::checkmark` dans `components/select.css`. | Suppression de la détection `CSS.supports("appearance", "base-select")` (Edge 135+). |
| `checkbox` & `switch` | **Point critique** : `<input type="checkbox" switch>` est Safari-only ; `ToggleSwitch` implémenté sur `<input type="checkbox" role="switch">` + `appearance: none` + `::before` + `forced-colors` + cible `min-block-size` 32/44 px. | **Exception conservée** : pas de `switch` natif ni de `input-switch-polyfill` ; CSS Fluent 2 dédié. |
| `slider` | `Slider` sur `<input type="range">` + `appearance: none` + `::-webkit-slider-runnable-track` / `::-webkit-slider-thumb` + `attr(min/max type(<number>))` + `progress()` CSS + `:dir(rtl)`. | Suppression des blocs `::-moz-range-*`, du repli `@supports not (opacity: progress(...))` (Edge 138+) et de la sync JS `--attr-min`/`--attr-max` (Edge 133+). |
| `card` | Structure minimale sans sur-emballage, container query sur la carte elle-même, `:has(> .pt-card-icon)`, `<button class="pt-card">` pour une carte actionnable. | Aucun fallback nécessaire. |
| `dialog` | `<dialog>` natif + `command="show-modal"` + `closedby` + `overscroll-behavior: contain` dans `reset.css` ; style conditionné à `dialog[open]`. | Suppression de `invokers-polyfill` (Edge 135+), `@oddbird/popover-polyfill` (Edge 116+) et du listener `pointerdown`/`pointerup` pour `closedby` (Edge 134+). |
| `menu` | `PopupMenu` sur `[popover="auto"]` + `commandfor` + Anchor Positioning implicite (`position-area`, `position-try-fallbacks`, `@position-try --below`) + `focusgroup="menu nomemory"` + `sign()` pour le triangle de sécurité sous-menu. | Suppression des polyfills `popover` (`.\:popover-open`), `invokers`, du positionnement JS `@supports not (position-area: ...)` (Edge 129+/140+) et du repli `clamp()` pour `sign()` (Edge 138+). **Conservé** : petit garde-fou JS (~20 lignes) pour `focusgroup` si WebView2 < 150. |
| `icons` | Moteur CSS à `@property --icon-start/end` (`inherits: false`) + `@container style(--icon-start)` + `mask` + `currentColor` + `forced-color-adjust: none` + `0.5cap` dans `properties.css` et `icons.css`. | Suppression du repli de classes `.icon-start` / `image-set()` et des 3 contournements Safari WebKit `#301609`, `#320220`, `#325900`. |
| `motion` & `natural-interactions` | Transitions de couleur rapides conservées ; tout mouvement spatial (`translate`, `scale`) conditionné à `@media (prefers-reduced-motion: no-preference)` ; `@starting-style` + `transition-behavior: allow-discrete` séparé après `transition` pour `[popover]` / `<dialog>` ; focus `:focus-visible` jamais masqué. | Suppression de `.\:popover-open` et des replis d'animation `linear()` (Edge 113+). |
| `scoped-component-transitions` | `element.startViewTransition()` utilisable localement (ex. réordonnancement d'une liste de moniteurs) sans bloquer les overlays, avec reset `@media (prefers-reduced-motion: reduce)`. | Natif depuis Edge 147 ; garde léger `'startViewTransition' in Element.prototype` si un poste est encore en Edge 146. |
| `progress` | Si une barre `<progress>` est ajoutée : `appearance: none`, `::-webkit-progress-value`, `container-type: inline-size` + `cqi`, ralentissement (jamais `animation: none`) sous `prefers-reduced-motion: reduce`. | Suppression des règles `::-moz-progress-bar`. |
| `reduce-style-repetition` | CSS `@function` (Edge 139+) est supporté nativement, mais **non retenu par défaut** ici : avec 2 niveaux de tokens et 12 composants, son gain est faible et sa prise en charge dans Stylelint / Lightning CSS / les agents IA ajoute plus de friction que de valeur. | Non applicable (volontairement écarté). |
| `accessibility` | Recette canonique `.u-sr-only`, `for`/`id` explicite (`$props.id()`), `outline` + `outline-offset` sur `:focus-visible` (≥ 3:1), cible ≥ 24×24 px (32 px standard, 44 px tactile), pas de redondance d'état ARIA dans les labels. | Aucun fallback à supprimer. |
| `performance` | Un seul bundle CSS via Vite (`cssCodeSplit: false`, zéro `@import` réseau), `contain: layout style paint` sur `TelemetryChart`, pas de `content-visibility: auto` ni de `will-change` global sur des pages de paramètres simples. | Aucun fallback à supprimer. |

---

## Risques et points ouverts

1. **Vérification exacte des valeurs `Common_themeresources_any.xaml`** : plusieurs opacités secondaires/tertiaires/désactivées et les couleurs exactes `SystemFillColor*` de l'extrait `tokens.css` (Q3) doivent être vérifiées directement dans `Common_themeresources_any.xaml` du dépôt `microsoft/microsoft-ui-xaml` et testées visuellement sur Mica clair/sombre Windows 11.
2. **Interaction préprocesseur `@layer views` × Svelte 5 × Lightning CSS** : le préprocesseur qui enveloppe `<style>` dans `@layer views { ... }` est simple mais doit être testé sur un prototype réel (`vite dev` et `vite build`) avec des règles imbriquées, `@container`, `@media` et `@keyframes` pour vérifier qu'aucune transformation de hachage Svelte ou optimisation Lightning CSS ne modifie la couche ou ne casse le source map.
3. **Support de `referenceFiles` et de `postcss-html` sur la version exacte de Stylelint installée** : vérifier dès l'installation (`pnpm add -D`) que la version de `stylelint` résolue prend bien en charge `referenceFiles` avec `no-unknown-custom-properties` sur les fichiers `.svelte` (sinon basculer sur `csstools/stylelint-value-no-unknown-custom-properties` avec `importFrom`).
4. **Rendu de `forced-colors: active` sur `ToggleSwitch` et `Button[data-variant="accent"]`** : l'utilisation ponctuelle de `forced-color-adjust: none` couplée aux mots-clés système (`Highlight`, `HighlightText`, `Canvas`, `ButtonText`, `GrayText`) doit être validée dans les 4 thèmes de contraste Windows 11 (*Aquatique*, *Désert*, *Crépuscule*, *Ciel nocturne*) pour confirmer qu'aucun contrôle ne perd sa bordure ni sa pastille.
5. **Schéma YAML exact de `@google/design.md lint`** : le schéma des clés acceptées par `@google/design.md lint` pour les paires `light`/`dark` n'a pas été inspecté en source ; la structure exacte du front-matter YAML de `DESIGN.md` et le parseur associé dans `check-tokens-sync.mjs` devront être calés sur la sortie de `npx @google/design.md spec` dès l'initialisation du dépôt UI.
6. **Contraste `TextOnAccentFillColorPrimary` avec des couleurs d'accent Windows très claires ou très foncées** : WinUI 3 choisit `#ffffff` en clair sur `AccentDark1` et `#000000` en sombre sur `AccentLight2`, mais si un utilisateur choisit une couleur d'accent personnalisée extrême dans Windows 11, il faudra vérifier que `AccentDark1`/`AccentLight2` calculés par WinRT maintiennent bien ≥ 4.5:1 (ou calculer côté Rust le choix blanc/noir via luminance relative WCAG et pousser `--win-on-accent`).

---

## Tableau récapitulatif des décisions

| Domaine | Décision | Raison principale | Alternatives rejetées |
|---|---|---|---|
| **Emplacement des styles** | Primitives `.pt-*` en CSS global (`@layer components`) ; `<style>` Svelte réservé aux vues et auto-enveloppé dans `@layer views` | Propriétaire unique par règle, zéro duplication HTML/Svelte, composition par `Snippet` sans `:global()`, compatibilité intacte avec `@layer utilities` | CSS des primitives dans `*.svelte` (conflit unlayered vs `@layer`, duplication, `:global()`), tout-global sans `<style>` Svelte (fuite de styles locaux de vues) |
| **Arborescence & couches** | `apps/ui/src/styles/` + manifeste unique `index.css` (`@layer reset, tokens, base, components, views, utilities;`) + `@import ... layer(...)` + `build.cssCodeSplit: false` | Ordre de cascade déterministe et identique en dev et en prod, zéro requête `@import` réseau, impossible d'inventer une couche dans un composant | `@layer` répété dans chaque fichier (risque de couches sauvages + niveau d'indentation), CSS code-split par chunk (risque de réordonnancement) |
| **Tokens & thème** | 2 niveaux globaux dans `tokens.css` (échelles/entrées WinRT + sémantique WinUI 3 en `light-dark()`) + `--_*` locaux ; 1 bloc `forced-colors` et 1 bloc `data-backdrop="solid"` | Proportionné à ~12 composants (MWG `css`), suit nativement clair/sombre/contraste élevé et l'accent WinRT sans JS de thème ni bug `oklch` | 4 niveaux globaux (`--button-bg-hover` sur `:root` — sur-ingénierie), `@fluentui/tokens` (Fluent 2 Web opaque), `color-mix(in oklch, ...)` (bug Chromium 565837690) |
| **Nommage, variantes & états** | Classes plates `.pt-*` / `.u-*` ; `data-variant` / `data-tone` pour les variantes ; pseudo-classes natives, ARIA et `:has()` pour tous les états | Source de vérité unique DOM/accessibilité (impossible de désynchroniser état visuel et ARIA), faible spécificité, validable par regex | BEM (`__`/`--`, déconseillé par MWG `css`), classes d'état (`.is-open`, `.is-disabled`, `.active`), `:invalid` nu |
| **Utilitaires** | Liste fermée de 6 utilitaires structurels/a11y/typo (`.u-stack`, `.u-cluster`, `.u-sr-only`, `.u-tabular`, `.u-truncate`, `.u-scroll-region`) verrouillée en CI (plafond 8) | Évite la répétition flex/scroll/a11y dans les vues tout en bloquant mécaniquement toute dérive vers des classes cosmétiques façon Tailwind | Zéro utilitaire (duplication de grilles/stacks dans chaque vue), utilitaires atomiques de couleur/padding/texte (clone Tailwind) |
| **Contrôles & exemples** | HTML natif stylé par tokens WinUI 3 : `<button>`, `<input type="checkbox" role="switch">`, `SettingsCard` avec `@container` + `:has()` | Fidélité Windows 11 Settings, accessibilité clavier/Narrator native, résistance à `text-scale` OS et contraste élevé | `<input type="checkbox" switch>` (Safari-only), `<div>` cliquables, media queries viewport sur les cartes |
| **Enforcement & sync `DESIGN.md`** | Stylelint (`standard` + `html/svelte` + `declaration-strict-value` + `no-unknown-custom-properties`) + `svelte-check --fail-on-warnings` + `tokens.css` vérifié bidirectionnellement contre `DESIGN.md` (`@google/design.md lint` + `check-tokens-sync.mjs`) | Bloque mécaniquement les dérives des agents IA (hex, nombres magiques, `:global`, `!important`, couches inconnues, dérive `DESIGN.md`) sans DSL de génération fragile | Revue manuelle seule (dérive garantie avec agents IA), génération de `tokens.css` depuis un YAML propriétaire (casse `@google/design.md lint`) |
| **Règles `AGENTS.md`** | Bloc normatif compact de 12 lignes prêtes à coller | Rappelle toutes les bornes non négociables au moment de la génération sans saturer le contexte des agents | Guide long non synthétisé ignoré par les agents |

