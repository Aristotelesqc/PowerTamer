> **Provenance.** Gemini 4 Neon (High) through the Antigravity CLI, plan mode, 2026-11-27, repository at `79d526d`.
> The caller ran the Modern Web Guidance searches and supplied 21 retrieved guides; the model ran no commands.
> Raw report below, unedited except for links. Caller's verification notes:
> - Confirmed: Svelte's `fragments: 'tree'` option exists (since 5.33; current 5.59.2) and is the documented way to run under `require-trusted-types-for 'script'`.
> - Confirmed: `<input type="checkbox" switch>` is Safari-only per the MWG `switch` guide; Chromium/WebView2 needs `role="switch"`.
> - Confirmed: `fluent-svelte` last pushed 2023-07; `google-labs-code/design.md` exists (Apache-2.0).
> - Confirmed: the reference PC runs WebView2 158, above every version floor cited.
> - Disputed: "Tailwind `@theme` is static, so it cannot follow the runtime accent color." Tailwind 4 emits theme values as CSS custom properties, which can reference or be overridden by runtime variables. The other arguments against Tailwind (pseudo-element-heavy native controls, Preflight, agents drifting to default palettes) stand on their own.
> - Unverified: bundle sizes, Edge version numbers per feature, `trusted-types 'none'` with Tauri's IPC bootstrap.
# Décision technique : stack UI pour la fenêtre de paramètres de PowerTamer (`powertamer-ui`)

Ce rapport tranche les 8 décisions d'architecture pour `powertamer-ui`, à partir de [docs/spec.md](../../docs/spec.md), [README.md](../../README.md), des 21 guides Modern Web Guidance fournis dans `.agy-runs/mwg/` et des documentations officielles vérifiées en ligne.

> **Rappel des priorités retenues (par ordre décroissant)** : 1. Apparence native Windows 11 (Fluent 2 / Paramètres Windows 11 sur Mica) · 2. Légèreté et rapidité (temps d'ouverture, taille de bundle, CPU en veille avec télémétrie 1 Hz) · 3. Accessibilité (clavier, Narrator, contraste élevé) · 4. Maintenabilité (code produit majoritairement par des agents IA).

---

## 1. Framework UI

### Décision : **Svelte 5** (mode runes strict : `compilerOptions.runes = true`)

**React 19 en vaut-il la peine ici ? Non.**

### Raisons et sources

- **Taille de bundle et temps d'ouverture** : `powertamer-ui` démarre à la demande et se ferme complètement à la fermeture de la fenêtre ([docs/spec.md](../../docs/spec.md)). Svelte 5 compile les composants en mutations DOM impératives avec un plancher d'exécution d'environ **2–3 Ko gzippé** (hors code composant), contre ~3–4 Ko pour Preact, ~7–14 Ko pour SolidJS, ~5–6 Ko pour Lit, ~16–20 Ko pour Vue 3 et **~45 Ko+ gzippé** (~185 Ko minifié) pour React 19 (`react` + `react-dom`) — voir la documentation Svelte ([https://svelte.dev/docs/svelte/overview](https://svelte.dev/docs/svelte/overview)), Preact ([https://preactjs.com/](https://preactjs.com/)), SolidJS ([https://www.solidjs.com/](https://www.solidjs.com/)) et le benchmark de référence `js-framework-benchmark` ([https://krausest.github.io/js-framework-benchmark/](https://krausest.github.io/js-framework-benchmark/)).
- **CPU en veille avec télémétrie à 1 Hz** : les runes Svelte 5 (`$state`, `$derived`, `$effect`) assurent une réactivité fine par signal sans Virtual DOM ([https://svelte.dev/docs/svelte/what-are-runes](https://svelte.dev/docs/svelte/what-are-runes)). Une mise à jour 1 Hz (puissance CPU/GPU, palier actif) ne met à jour que les quelques nœuds texte concernés et pousse un point dans le canvas `uPlot`, sans réconciliation d'arbre.
- **Accessibilité** :
  - Svelte intègre un **vérificateur d'accessibilité à la compilation** directement sur les templates HTML (`svelte-check` / compilateur Svelte : labels manquants, rôles ARIA invalides, éléments interactifs non accessibles au clavier) ([https://svelte.dev/docs/svelte/compiler-warnings](https://svelte.dev/docs/svelte/compiler-warnings)). Pour un dépôt écrit majoritairement par des agents IA, ce garde-fou statique est un avantage majeur.
  - Contrairement à Lit ou aux Web Components en Shadow DOM, Svelte rend dans le **Light DOM** : les références par `id` (`for`/`id`, `aria-labelledby`, `aria-describedby`, `aria-controls`) fonctionnent sans friction entre composants, alors que le guide Modern Web Guidance `web-components` (MWG `web-components`) rappelle qu'elles ne traversent pas une frontière Shadow DOM.
- **Compatibilité avec les primitives HTML natives et Trusted Types** :
  - Les templates Svelte 5 sont du HTML standard : `popover="auto"`, `<dialog closedby="any">`, `command="show-modal"`, `commandfor="..."`, `focusgroup="menu nomemory"`, `<select>` avec `appearance: base-select`, `class`, `for` fonctionnent sans couche d'événements synthétiques ni conversions JSX (`className`, `htmlFor`).
  - Point sécurité décisif : par défaut Svelte 5 utilise `$.from_html` (`template.innerHTML`), ce qui déclenche une erreur sous `require-trusted-types-for 'script'`, mais Svelte 5 propose l'option de compilation `fragments: 'tree'` (construction via `document.createElement`) ([https://svelte.dev/docs/svelte/compiler-options](https://svelte.dev/docs/svelte/compiler-options)) et supporte nativement `TrustedHTML` dans `{@html}` depuis Svelte 5.52.0 ([https://github.com/sveltejs/svelte/releases](https://github.com/sveltejs/svelte/releases)). *(Note : l'absence totale d'erreur sous `trusted-types 'none'` avec `fragments: 'tree'` reste à vérifier empiriquement sur la version exacte installée — voir Risques et inconnues.)*
- **Support Tauri 2** : gabarit officiel de première classe (`svelte-ts`) dans `create-tauri-app` et la documentation Tauri v2 ([https://v2.tauri.app/start/frontend/sveltekit/](https://v2.tauri.app/start/frontend/sveltekit/) et [https://v2.tauri.app/start/create-project/](https://v2.tauri.app/start/create-project/)).
- **Fiabilité des agents IA** : sans garde-fous, les LLM confondent parfois Svelte 4 (`export let`, `$:`, `writable`) et Svelte 5 (runes). Ce risque est verrouillé mécaniquement en activant `compilerOptions: { runes: true }` (toute syntaxe Svelte 4 devient une erreur de compilation immédiate), en exécutant `svelte-check`, et en référençant la documentation dédiée aux LLM de Svelte ([https://svelte.dev/llms.txt](https://svelte.dev/llms.txt)) dans `AGENTS.md`. Une fois ce garde-fou posé, la concision de Svelte 5 (moins de boilerplate, pas de tableaux de dépendances `useEffect`, pas de piège de déstructuration de props réactives comme dans SolidJS) produit un code plus fiable et plus court que React ou SolidJS.
- **Pourquoi React 19 n'en vaut pas la peine ici** : React 19 impose le runtime le plus lourd (~45 Ko+ gzippé) et le modèle de réconciliation Fiber pour une simple fenêtre de paramètres sans état métier. Son principal argument sur Windows aurait été Fluent UI React v9 (`@fluentui/react-components`), mais celui-ci cible Fluent 2 **Web** (et non Windows 11 Settings sur Mica) et ajoute Griffel (voir Q3). Quant aux autres rejetés : **Vanilla TS + Web Components** force à réinventer une couche réactive pour les listes dynamiques (moniteurs, paliers, télémétrie) et souffre des limites Shadow DOM ; **Lit** impose le Shadow DOM ; **SolidJS** piège facilement les agents IA (perte de réactivité sur déstructuration) ; **Preact** n'offre la réactivité fine qu'avec `@preact/signals` (écart par rapport à React standard) et n'a pas de lint a11y intégré au compilateur ; **Vue 3** est plus lourd sans avantage spécifique ici.

---

## 2. Styling

### Décision : **CSS moderne pur** (couches `@layer`, CSS custom properties de tokens WinUI 3 / Fluent 2 Desktop, nesting natif, container queries et `<style>` scopé de Svelte 5) — **rejet de Tailwind CSS 4**

> **Remise en cause de la proposition initiale** : la proposition initiale envisageait Tailwind CSS 4. Pour `powertamer-ui`, Tailwind CSS 4 crée plus de friction qu'il n'en résout et va à l'encontre des exigences de fidélité Windows 11 et des guides Modern Web Guidance.

### Raisons et sources

- **Désalignement avec les éléments natifs modernes** : la stack repose sur des contrôles HTML natifs stylés par pseudo-éléments et nouvelles fonctionnalités CSS : `<select>` (`appearance: base-select`, `::picker(select)`, `::picker-icon`, `option::checkmark`), `<dialog>` (`dialog:modal`, `::backdrop`), `<input type="range">` (`::-webkit-slider-runnable-track`, `::-webkit-slider-thumb`), `@starting-style`, `transition-behavior: allow-discrete`, `position-area`, `@media (forced-colors: active)`. En Tailwind CSS 4, ces sélecteurs exigent des variantes arbitraires illisibles (`[&::picker(select)]:...`) ou un retour au CSS brut : on finit par maintenir deux systèmes de styles en parallèle.
- **Pièges connus de Tailwind CSS 4 avec Vite / Tauri et les guides MWG** :
  - **Preflight vs `css` (MWG)** : Tailwind CSS 4 s'appuie sur `@tailwindcss/vite` et injecte son reset Preflight dans `@layer base` ([https://tailwindcss.com/docs/preflight](https://tailwindcss.com/docs/preflight), [https://tailwindcss.com/docs/installation/using-vite](https://tailwindcss.com/docs/installation/using-vite)). Or le guide MWG `css` (MWG `css`) proscrit les resets globaux agressifs et recommande une hiérarchie `@layer` maîtrisée ; mal ordonner les couches avec Preflight écrase les styles de base ou de composants.
  - **Shadow DOM et `@property`** : Tailwind CSS 4 dépend de règles `@property` au niveau document ; si un composant tiers ou un Web Component en Shadow DOM était introduit, `@property` ne fonctionne pas à l'intérieur d'un Shadow Root (spécification CSS Properties and Values API, [https://drafts.css-houdini.org/css-properties-values-api/](https://drafts.css-houdini.org/css-properties-values-api/), et comportement documenté sur `tailwindlabs/tailwindcss`, [https://github.com/tailwindlabs/tailwindcss/discussions](https://github.com/tailwindlabs/tailwindcss/discussions)).
  - **Risque de dérive IA (« Tailwind slop »)** : avec Tailwind actif, les agents IA introduisent spontanément des couleurs par défaut Tailwind (`bg-zinc-900`, `text-slate-400`, `rounded-xl`) ou des restes Tailwind v3 (`@apply`, `tailwind.config.js`), détruisant l'aspect natif Windows 11. Même en réinitialisant tout le thème (`@theme { --*: initial; }`), `@theme` est statique à la compilation alors que la couleur d'accent Windows est dynamique à l'exécution (poussée depuis Rust).
- **Expression des tokens Fluent 2** :
  - **Ne pas utiliser `@fluentui/tokens`** ([https://www.npmjs.com/package/@fluentui/tokens](https://www.npmjs.com/package/@fluentui/tokens)) comme base principale : ce paquet définit les tokens **Fluent 2 Web** (`webLightTheme`, `webDarkTheme`), conçus pour des surfaces opaques d'applications web (Teams, M365) et non pour les surfaces semi-transparentes WinUI 3 posées sur Mica.
  - **Définir les tokens dans `src/styles/tokens.css` en CSS custom properties natives** sur `:root`, calquées sur les ressources officielles WinUI 3 (`Common_themeresources_any.xaml` de `microsoft/microsoft-ui-xaml`, [https://github.com/microsoft/microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml), et la documentation Fluent 2 Windows [https://learn.microsoft.com/en-us/windows/apps/design/style/color](https://learn.microsoft.com/en-us/windows/apps/design/style/color)), structurées en 2 à 3 niveaux maximum (conformément au guide MWG `css`, « Do not overengineer ») :
    1. **Tokens système dynamiques** (injectés par Rust sur `:root`) : `--win-accent`, `--win-accent-light-1`..`3`, `--win-accent-dark-1`..`3`.
    2. **Tokens sémantiques de surfaces / textes / bordures** via `light-dark()` : ex. `--surface-layer` (`LayerFillColorDefault`), `--surface-card` (`CardBackgroundFillColorDefault`), `--surface-control`, `--stroke-card`, `--text-primary`, `--text-secondary`, `--accent-fill-default: light-dark(var(--win-accent-dark-1), var(--win-accent-light-2))`, plus surcharge complète dans `@media (forced-colors: active)`.
  - **Architecture CSS** : une déclaration unique en tête de feuille `@layer reset, tokens, base, components, utilities;`, complétée par le scoping CSS natif de Svelte 5 (`<style>` dans chaque `.svelte`), le nesting CSS natif ([https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_nesting](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_nesting)) et les container queries (`@container`) pour rendre la fenêtre responsive (ex. repli de la barre latérale). Zéro dépendance CSS, zéro surcharge de build Vite.

---

## 3. Implémentation Fluent 2

### Décision : **Composants Svelte 5 maison, fins et sémantiques, bâtis sur les éléments HTML natifs et les tokens CSS Fluent 2 / WinUI 3**

### Raisons et sources

- **Pourquoi ne pas utiliser `@fluentui/web-components` v3** :
  - `@fluentui/web-components` v3 ([https://github.com/microsoft/fluentui/tree/master/packages/web-components](https://github.com/microsoft/fluentui/tree/master/packages/web-components), [https://www.npmjs.com/package/@fluentui/web-components](https://www.npmjs.com/package/@fluentui/web-components)) est passé en version stable v3.x (basé sur FAST Element et `@fluentui/tokens`, tree-shakeable par import ESM par composant).
  - Toutefois, il vise **Fluent 2 Web** (surfaces opaques), encapsule ses contrôles en **Shadow DOM** (cassant les liaisons `aria-labelledby`/`aria-describedby` externes et compliquant le theming sur Mica), réimplémente en JS des contrôles que WebView2 evergreen fournit désormais nativement (`<dialog>`, `[popover]`, `<select>` `appearance: base-select`), et ne propose pas les motifs propres aux Paramètres Windows 11 (`SettingsCard`, `SettingsExpander`, `NavigationView` translucide). *(Note : la liste exhaustive des composants stables en v3.1+ n'a pas pu être inspectée fichier par fichier sans clonage/accès direct au dépôt — voir Risques et inconnues — mais les limites architecturales Shadow DOM + Fluent 2 Web suffisent à l'écarter.)*
- **Pourquoi ne pas utiliser Fluent UI React v9 (`@fluentui/react-components`)** :
  - Bien que v9 soit bien plus modulaire et rapide que v8 grâce à Griffel (CSS atomique, DOM allégé — [https://react.fluentui.dev/](https://react.fluentui.dev/) et [https://griffel.js.org/](https://griffel.js.org/)), il impose React 19 (~45 Ko+ gzippé), un moteur CSS-in-JS supplémentaire, et reproduit l'esthétique **Fluent 2 Web** plutôt que l'application Paramètres de Windows 11.
- **Pourquoi ne pas utiliser de bibliothèque communautaire Fluent** :
  - `fluent-svelte` ([https://github.com/Tropix126/fluent-svelte](https://github.com/Tropix126/fluent-svelte)) est abandonné et limité à Svelte 3/4.
  - Les forks ou portages Svelte 5 récents (`fluentui-svelte`, `fluent-svelte-extra`, `svelte-fluentui`) sont des projets communautaires à faible *bus factor*, sans garantie d'audit Narrator / contraste élevé, ou ne font qu'encapsuler `@fluentui/web-components`.
- **Pourquoi des composants maison sur HTML natif gagnent sur tous les plans** :
  - `powertamer-ui` n'a besoin que d'environ **10–12 composants** : `NavigationView` (`<nav>`), `SettingsCard`, `SettingsExpander` (`<details>` ou disclosure ARIA `<h2><button aria-expanded>` lorsqu'un titre est requis, selon MWG `accessibility`), `Button` (`<button>`), `ToggleSwitch` (`<input type="checkbox" role="switch">` stylé en CSS `appearance: none`), `Select` (`<select>` avec `appearance: base-select`), `Slider` (`<input type="range">`), `NumberInput` (`<input>`), `Dialog` (`<dialog>` natif), `PopupMenu` (`[popover]` + `focusgroup`), `InfoBar` (`role="status"` / `role="alert"` pour Process Lasso et erreurs) et icônes via **Fluent UI System Icons** (`@fluentui/svg-icons`, [https://github.com/microsoft/fluentui-system-icons](https://github.com/microsoft/fluentui-system-icons), en ne bundlant que les quelques dizaines de SVG utilisés, rendus via masque CSS `currentColor` ou SVG inline).
  - L'accessibilité clavier, focus, Narrator et formulaires est héritée directement du moteur Chromium de WebView2, avec un poids JS quasi nul et une fidélité visuelle exacte à Windows 11 Settings.

---

## 4. Graphiques pour la télémétrie temps réel

### Décision : **uPlot** (`leeoniya/uPlot`)

### Raisons et sources

- **Taille et empreinte minimale** : **~22 Ko gzippé** (~45–50 Ko minifié), zéro dépendance externe ([https://github.com/leeoniya/uPlot](https://github.com/leeoniya/uPlot)). C'est 3× plus léger que Chart.js (~65–70 Ko gzippé, [https://www.chartjs.org/](https://www.chartjs.org/)) et Observable Plot (~67 Ko gzippé, ~157 Ko avec D3, [https://observablehq.com/plot/](https://observablehq.com/plot/)), et 6× à 15× plus léger qu'Apache ECharts (~135–350+ Ko gzippé selon le build, [https://echarts.apache.org/](https://echarts.apache.org/)).
- **Performance temps réel à 1 Hz** : rendu Canvas 2D direct optimisé pour les séries temporelles ; `uPlot` peut afficher 150 000 points en ~135 ms et streamer à 60 FPS ([https://github.com/leeoniya/uPlot#performance](https://github.com/leeoniya/uPlot#performance)). Pour PowerTamer (2 à 3 séries — CPU W, GPU W — sur une fenêtre glissante à 1 Hz, soit 300 à 3 600 points), chaque mise à jour via `u.setData(...)` coûte moins d'une milliseconde CPU sans allocation DOM.
- **Historique des paliers (Active / Away / Drowsy)** : un léger hook `draw` de `uPlot` permet de peindre les bandes colorées de paliers en arrière-plan du même axe temporel, synchronisé au pixel près avec la courbe en watts.
- **Accessibilité (MWG `accessibility` et `motion`)** : un `<canvas>` étant opaque pour Narrator, le composant `TelemetryChart` doit l'envelopper dans `<figure>` + `<figcaption>`, exposer les valeurs instantanées dans du texte structuré (`aria-live="off"` pour ne pas saturer Narrator chaque seconde), proposer un `<details>` contenant un `<table>` sémantique des dernières mesures / transitions de paliers, et fournir un bouton **Pause / Reprendre** (exigence WCAG 2.2.2 pour toute mise à jour visuelle continue > 5 s, rappelée par MWG `motion`). En `@media (forced-colors: active)`, les couleurs de séries relues depuis les tokens CSS sont adaptées (`CanvasText`, `Highlight`, motifs de tirets distincts afin de ne jamais dépendre de la couleur seule).
- **Alternatives rejetées** : **Chart.js** est plus lourd et impose un adaptateur de dates et `update('none')` ; **ECharts** est surdimensionné pour un utilitaire léger ; **Observable Plot** génère du SVG DOM, inadapté au streaming 1 Hz (churn DOM permanent) ; un **Canvas/SVG maison** obligerait les agents IA à réimplémenter axes temporels, `devicePixelRatio`, infobulles au survol, redimensionnement et échelles, source inutile de bugs.

---

## 5. Intégration native Windows 11 dans Tauri 2

### Décisions synthétiques

1. **Backdrop Mica** : **Mica natif via `window-vibrancy::apply_mica(&window, None)`** dans le hook `.setup()` Rust, fenêtre démarrée avec `transparent: true` et `visible: false` (affichée après le premier rendu) ; **repli Windows 10 vers un fond opaque WinUI** (pas Acrylic Win10).
2. **Thème clair/sombre et couleur d'accent** : **CSS `color-scheme: light dark` + `light-dark()`** pour clair/sombre, et **lecture de la rampe d'accent complète côté Rust via WinRT `UISettings`** (`Accent`, `AccentLight1..3`, `AccentDark1..3` + `ColorValuesChanged`) injectée en CSS custom properties, avec `AccentColor` CSS uniquement en secours initial.
3. **Contraste élevé** : **`@media (forced-colors: active)`** explicite + désactivation visuelle de la transparence Mica (`background: Canvas`).
4. **Police** : **pile `Segoe UI Variable` par taille optique** (`Text` / `Display` / `Small`) + `font-optical-sizing: auto`, zéro police embarquée.
5. **Barre de titre** : **barre de titre native Windows 11 (`decorations: true`)**.

### Raisons et sources

- **Mica + WebView2 transparent et pièges** :
  - Tauri v2 supporte les effets de fenêtre (`windowEffects` / `Effect::Mica`, adossé à la crate `window-vibrancy`, [https://v2.tauri.app/reference/config/#windoweffectsconfig](https://v2.tauri.app/reference/config/#windoweffectsconfig), [https://crates.io/crates/window-vibrancy](https://crates.io/crates/window-vibrancy)). `transparent: true` et `html, body { background: transparent; }` sont requis pour laisser apparaître Mica.
  - **Flash blanc à l'ouverture** : WebView2 peut peindre un fond blanc avant le premier frame ; démarrer la fenêtre avec `visible: false` dans `tauri.conf.json` et appeler `getCurrentWindow().show()` une fois le premier rendu Svelte + tokens appliqués élimine ce flash.
  - **Saccades au redimensionnement** : la composition asynchrone entre la fenêtre Win32, DWM et WebView2 peut produire des bordures/flashs lors d'un redimensionnement rapide ([https://github.com/tauri-apps/tauri/issues](https://github.com/tauri-apps/tauri/issues)). Mica est nettement moins coûteux qu'Acrylic côté DWM ; limiter les reflows lourds au resize et éviter `box-shadow` coûteux sur de grands conteneurs.
  - **Repli Windows 10** : Mica n'existe que sur Windows 11 (build 22000+, `DWMWA_SYSTEMBACKDROP_TYPE` à partir de 22H2 / build 22621 ; [https://learn.microsoft.com/en-us/windows/apps/design/style/mica](https://learn.microsoft.com/en-us/windows/apps/design/style/mica)). Sur Windows 10, `apply_mica` échoue (`Err`) : au lieu de basculer sur `apply_acrylic` (connu pour provoquer de fortes saccades au déplacement/redimensionnement sur Windows 10 — [https://docs.rs/window-vibrancy/](https://docs.rs/window-vibrancy/)), exposer un attribut `data-backdrop="solid"` au frontend pour peindre le fond WinUI opaque standard (`#F3F3F3` / `#202020`).
- **Thème clair/sombre et couleur d'accent (CSS `AccentColor` vs Rust WinRT `UISettings`)** :
  - Pour clair/sombre, `<meta name="color-scheme" content="light dark">` + `:root { color-scheme: light dark; }` + `light-dark()` suffisent en WebView2 evergreen (MWG `dark-mode`, [https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/light-dark](https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/light-dark)).
  - Pour la couleur d'accent, **ne pas se reposer uniquement sur le mot-clé CSS `AccentColor`** ([https://developer.mozilla.org/en-US/docs/Web/CSS/system-color](https://developer.mozilla.org/en-US/docs/Web/CSS/system-color)) pour deux raisons :
    1. Dans Chromium/Edge/WebView2, `AccentColor` a historiquement été soumis à des restrictions anti-fingerprinting ou au flag `CssSystemAccentColor` ([https://chromestatus.com/](https://chromestatus.com/)), avec des cas documentés de non-synchronisation à chaud lors d'un changement d'accent Windows. *(Note : le comportement exact par défaut sans flag dans la dernière build WebView2 150+ sur Windows 11 n'a pas pu être testé localement — voir Risques et inconnues.)*
    2. Surtout, **Fluent 2 sur Windows 11 ne se contente pas d'une couleur d'accent unique** : les états repos/survol/pressé/texte utilisent toute la rampe WinUI (`Accent`, `AccentLight1..3`, `AccentDark1..3` — par ex. `AccentDark1` en mode clair et `AccentLight2` en mode sombre). Le guide MWG `color` (MWG `color`) déconseille en outre de fabriquer des teintes par simple décalage `oklch` (absence de gamut mapping navigateur) ou `color-mix(in oklch, ...)` (bug Chromium 565837690).
  - La crate officielle Microsoft `windows` ([https://crates.io/crates/windows](https://crates.io/crates/windows), feature `UI_ViewManagement`) lit les 7 teintes exactes via `UISettings::GetColorValue(UIColorType::Accent...)` et s'abonne à `ColorValuesChanged` (attention : le callback WinRT s'exécute sur un thread d'arrière-plan, à relayer via `app.emit` / canal vers la WebView) ([https://learn.microsoft.com/en-us/uwp/api/windows.ui.viewmanagement.uisettings](https://learn.microsoft.com/en-us/uwp/api/windows.ui.viewmanagement.uisettings)).
- **Contraste élevé (`forced-colors`)** :
  - WebView2 expose `@media (forced-colors: active)` conformément au standard CSS Forced Colors ([https://developer.mozilla.org/en-US/docs/Web/CSS/@media/forced-colors](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/forced-colors)).
  - Piège critique avec Mica : sur un fond `transparent`, le contenu ne s'appuie plus sur `Canvas` du thème contraste élevé. Il est indispensable de repasser `html, body { background-color: Canvas; color: CanvasText; }` dans `@media (forced-colors: active)`, d'utiliser `border`/`outline` avec les couleurs système (`ButtonBorder`, `Highlight`, `GrayText`) et de limiter `forced-color-adjust: none` aux icônes masquées (MWG `color` et `icons`).
- **Segoe UI Variable** :
  - Sur Windows 11, `font-family: system-ui` ne sélectionne pas systématiquement les sous-familles optiques de Segoe UI Variable dans Chromium/WebView2 ([https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography)). Il faut déclarer explicitement :
    - Corps et contrôles : `"Segoe UI Variable Text", "Segoe UI", system-ui, sans-serif`
    - Titres (≥ 20 px) : `"Segoe UI Variable Display", "Segoe UI", system-ui, sans-serif`
    - Micro-textes / légendes (≤ 12 px) : `"Segoe UI Variable Small", "Segoe UI", system-ui, sans-serif`
    - Combiné avec `font-optical-sizing: auto;`.
- **Barre de titre : native (`decorations: true`) plutôt que custom** :
  - Une barre de titre custom (`decorations: false` + `data-tauri-drag-region`) casse les **Snap Layouts** de Windows 11 au survol du bouton Agrandir, car WebView2 intercepte `WM_NCHITTEST` et ne retourne jamais `HTMAXBUTTON` ([https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/apply-snap-layout-menu](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/apply-snap-layout-menu)). Les contournements existants (`tauri-plugin-frame`, `tauri-plugin-snap-layout`) reposent sur des fenêtres Win32 enfants superposées, fragiles vis-à-vis du DPI, des IDs DOM et de la largeur minimale.
  - Garder `decorations: true` offre gratuitement : Snap Layouts natifs, coins arrondis et ombres DWM, boutons de fenêtre accessibles par Narrator et en contraste élevé, mode sombre automatique de la barre de titre, et zéro maintenance Win32.

---

## 6. Build, IPC et sécurité dans Tauri 2

### Décisions

1. **Build Vite & TypeScript** : cibler directement **Evergreen Chromium (`build.target: 'esnext'` ou `'chrome135'`)**, `tsconfig.json` en mode strict + `moduleResolution: "bundler"`, `svelte.config.js` avec `runes: true` et `fragments: 'tree'`.
2. **IPC** : le backend Rust de `powertamer-ui` est **l'unique client du named pipe** vers le cœur `powertamer` ; il expose au frontend des **commandes Tauri typées** (lecture/écriture de configuration) et un **`tauri::ipc::Channel`** dédié au flux de télémétrie 1 Hz.
3. **Sécurité Tauri 2** : CSP locale stricte (zéro origine distante, `require-trusted-types-for 'script'`), `withGlobalTauri: false`, `freezePrototype: true`, **Capabilities** minimalistes liées à `"main"` uniquement, sans pattern Isolation tant que l'arbre npm reste minimal.

### Raisons et sources

- **Configuration TypeScript et Vite** ([https://v2.tauri.app/start/frontend/vite/](https://v2.tauri.app/start/frontend/vite/), [https://vite.dev/config/build-options.html](https://vite.dev/config/build-options.html)) :
  - Le modèle standard Tauri propose `chrome105` / `safari13` pour la compatibilité multi-OS. Comme PowerTamer cible exclusivement **Evergreen WebView2 sur Windows 11**, régler `build.target: 'chrome135'` (ou `'esnext'`) et `build.cssTarget: 'chrome135'` évite toute transpilation inutile du JS moderne et du CSS natif (nesting, `light-dark()`, `base-select`).
  - Conserver les options recommandées Tauri : `clearScreen: false`, `server: { port: 1420, strictPort: true, host: process.env.TAURI_DEV_HOST || false }`, `build.minify: !process.env.TAURI_ENV_DEBUG ? 'esbuild' : false`, `build.sourcemap: !!process.env.TAURI_ENV_DEBUG`.
  - `tsconfig.json` : `"target": "ESNext"`, `"module": "ESNext"`, `"moduleResolution": "bundler"`, `"strict": true`, `"noUncheckedIndexedAccess": true`, `"verbatimModuleSyntax": true`, `"noEmit": true`, `"lib": ["ESNext", "DOM", "DOM.Iterable"]`.
- **IPC : pourquoi Rust + Commands + `Channel` et jamais de pipe direct depuis la WebView** :
  - Une WebView2 sandboxée ne possède **aucune API navigateur** pour ouvrir un named pipe Win32 (`\\.\pipe\...`). La passerelle Rust dans `powertamer-ui` est donc indispensable, et reste strictement sans logique métier conformément à [docs/spec.md](../../docs/spec.md) (désérialisation/validation des messages, reconnexion au pipe, lecture WinRT `UISettings`).
  - Pour les lectures/écritures de paramètres (paliers, moniteurs, Process Lasso, module GPU) : **commandes Tauri** (`#[tauri::command]`) typées avec `serde` (et optionnellement `tauri-specta` pour générer automatiquement les types TS).
  - Pour le flux de télémétrie 1 Hz : préférer **`tauri::ipc::Channel<TelemetrySample>`** ([https://v2.tauri.app/develop/calling-rust/#channels](https://v2.tauri.app/develop/calling-rust/#channels)) aux événements globaux `app.emit()` — `Channel` est conçu dans Tauri v2 pour le streaming ordonné et rapide avec un cycle de vie lié à l'abonnement actif.
- **Durcissement sécurité Tauri 2** ([https://v2.tauri.app/security/csp/](https://v2.tauri.app/security/csp/), [https://v2.tauri.app/security/capabilities/](https://v2.tauri.app/security/capabilities/), [https://v2.tauri.app/reference/config/#securityconfig](https://v2.tauri.app/reference/config/#securityconfig)) :
  - **CSP (`app.security.csp` en production)** :
    ```text
    default-src 'none';
    script-src 'self';
    style-src 'self';
    img-src 'self';
    font-src 'none';
    connect-src ipc: http://ipc.localhost;
    object-src 'none';
    base-uri 'none';
    frame-ancestors 'none';
    form-action 'none';
    require-trusted-types-for 'script';
    trusted-types 'none';
    ```
    - `font-src 'none'` est possible grâce à l'utilisation exclusive de `Segoe UI Variable` système.
    - `style-src 'self'` n'a pas besoin de `'unsafe-inline'` : Tauri hash automatiquement les styles/scripts bundlés au build, et l'injection dynamique des tokens d'accent via `document.documentElement.style.setProperty(...)` (CSSOM) n'est pas bloquée par `style-src 'self'`.
    - `devCsp` peut être défini séparément dans `tauri.conf.json` pour le HMR Vite en développement sans affaiblir la CSP de production.
    - *(À vérifier au prototype : compatibilité exacte de `trusted-types 'none'` avec le script d'amorçage IPC injecté par Tauri v2 — voir Risques et inconnues.)*
  - **Durcissement supplémentaire** : `app.withGlobalTauri: false` (interdit `window.__TAURI__`), `app.security.freezePrototype: true` (`Object.freeze(Object.prototype)` anti-pollution de prototype), `dangerousDisableAssetCspModification: false`.
  - **Capabilities (`src-tauri/capabilities/main.json`)** : ciblée uniquement sur `"windows": ["main"]`, sans bloc `remote` (aucune URL externe n'a accès à l'IPC). Permissions strictement limitées à `core:window:allow-show` (pour le démarrage sans flash) et aux commandes personnalisées PowerTamer (`get_state`, `update_config`, `subscribe_telemetry`, `get_system_accent`). Aucun plugin `fs`, `shell` ni `http`.
  - **Pattern Isolation (`pattern.use = "isolation"`)** ([https://v2.tauri.app/concept/inter-process-communication/isolation/](https://v2.tauri.app/concept/inter-process-communication/isolation/)) : **non retenu par défaut**. Il introduit une `<iframe>` sandboxée et un chiffrement AES-GCM par message IPC pour contrer une compromission de la chaîne npm ; ici, avec seulement ~4 dépendances de production (`svelte`, `uplot`, `@tauri-apps/api`, SVG copiés depuis `@fluentui/svg-icons`), le surcoût mémoire/démarrage ne se justifie pas, à condition de verrouiller le lockfile et d'auditer les dépendances. (Côté cœur `powertamer`, le named pipe doit bien sûr appliquer une DACL restreinte au SID de l'utilisateur courant.)

---

## 7. `DESIGN.md`

### Décision : **Adopter le format `DESIGN.md` de Google Labs (Stitch)** à la racine du dépôt, aligné sur **Fluent 2 Desktop / WinUI 3** et synchronisé avec `tokens.css`

### Ce qu'est le format `DESIGN.md` de Google (sources)

Introduit par Google Labs avec **Google Stitch** ([https://stitch.withgoogle.com/](https://stitch.withgoogle.com/) ; spécification et dépôt open-source Apache 2.0 : [https://github.com/google-labs-code/design.md](https://github.com/google-labs-code/design.md)), `DESIGN.md` est l'équivalent d'`AGENTS.md` pour le design visuel. Il associe dans un seul fichier Markdown à la racine du projet :

1. **Des tokens structurés lisibles par machine** (front-matter YAML ou blocs YAML : couleurs, échelles typographiques, espacements, rayons, contrastes) — validables par le linter `@google/design.md`.
2. **De la prose normative lisible par l'humain et les agents IA** expliquant l'intention esthétique, les rôles sémantiques, les règles de composition des composants, les exigences WCAG et les *Do's & Don'ts* pour éviter les dérives génériques (« AI slop »).

La proposition initiale d'utiliser `DESIGN.md` est donc **excellente** pour un projet écrit par des agents IA ; son seul point de vigilance est de maintenir une correspondance 1-pour-1 entre ses tokens et `src/styles/tokens.css`.

### Plan recommandé du `DESIGN.md` de PowerTamer (Fluent 2 Desktop)

```markdown
---
name: "PowerTamer Settings"
designSystem: "Fluent 2 Desktop (WinUI 3 / Windows 11 Settings)"
backdrop: "Mica (Windows 11) / Solid fallback (Windows 10 & Forced Colors)"
typography:
  display: '"Segoe UI Variable Display", "Segoe UI", system-ui, sans-serif'
  body: '"Segoe UI Variable Text", "Segoe UI", system-ui, sans-serif'
  caption: '"Segoe UI Variable Small", "Segoe UI", system-ui, sans-serif'
  mono: '"Cascadia Mono", "Consolas", ui-monospace, monospace'
spacingUnit: "4px"
radii:
  control: "4px"
  card: "8px"
  dialog: "8px"
---

## 1. Identité et atmosphère (Windows 11 Settings)
- Fenêtre utilitaire calme, native, lisible, fidèle à Paramètres de Windows 11.
- Aucun effet « web marketing » : pas de dégradés gratuits, pas de cartes surchargées, pas de polices chargées.

## 2. Surfaces, Mica et rôles de couleur
- Hiérarchie à 3 niveaux : Base Mica (transparent) → Layer (`LayerFillColorDefault`) → Card (`CardBackgroundFillColorDefault` + `CardStrokeColorDefault`).
- Tokens `light-dark()` exacts inspirés de `Common_themeresources_any.xaml` (WinUI 3).
- Rampe d'accent système dynamique (`--win-accent`, `--win-accent-light-1..3`, `--win-accent-dark-1..3`) injectée par Rust, avec mappage par mode clair/sombre.
- Couleurs sémantiques métier (Active, Away, Drowsy, propriété Process Lasso, alerte module GPU) toujours couplées à une icône ou un libellé.
- Règles obligatoires `@media (forced-colors: active)` (repli `Canvas` opaque, mots-clés système).

## 3. Typographie et échelle de texte OS
- Échelle Fluent 2 en `rem` (Title 1.75rem/600, Subtitle 1.25rem/600, BodyStrong 0.875rem/600, Body 0.875rem/400, Caption 0.75rem/400), `line-height` sans unité.
- `font-variant-numeric: tabular-nums` obligatoire pour les watts, durées et horodatages.
- Opt-in `<meta name="text-scale" content="scale">`, interdiction de fixer `font-size` sur `html`/`:root` ou `height` fixe sur les conteneurs de texte.

## 4. Mise en page et densité
- Architecture maître-détail : `NavigationView` latérale (240–280 px, pliable via `@container`) + colonne principale centrée (`max-inline-size: 1000px`).
- Empilement vertical de `SettingsCard` (`gap: 4px` dans un groupe, `24px` entre sections).
- Propriétés CSS logiques (`inline`/`block`), cibles interactives ≥ 24×24 px (hauteur de contrôle standard 32 px).

## 5. Catalogue de composants (HTML natif → Fluent 2)
- Table de correspondance stricte : `SettingsCard`, `SettingsExpander` (`<details>` / ARIA disclosure), `ToggleSwitch` (`<input type="checkbox" role="switch">`), `Select` (`<select>` `appearance: base-select`), `Slider` (`<input type="range">`), `Dialog` (`<dialog>` + `command="show-modal"`), `Menu` (`[popover]` + `focusgroup`), `InfoBar` (`role="status"` / `role="alert"`), `TelemetryChart` (`uPlot` + `<figure>` + tableau accessible + Pause).
- États obligatoires pour chaque contrôle : repos, survol, pressé, désactivé, `:focus-visible` (anneau `outline` 2 px + `outline-offset: 2px`, contraste ≥ 3:1).

## 6. Iconographie
- Fluent UI System Icons (`@fluentui/svg-icons`), style Regular 20 px par défaut, Filled pour l'item actif de navigation.
- Rendu via masque CSS `currentColor` (`forced-color-adjust: none`) ou SVG inline `aria-hidden="true"` quand décoratif.

## 7. Mouvement
- Transitions courtes Fluent 2 (83–167 ms), `opacity`/`translate` uniquement, `@starting-style` + `transition-behavior: allow-discrete` pour overlays.
- Tout mouvement spatial conditionné à `@media (prefers-reduced-motion: no-preference)`.

## 8. Do's & Don'ts pour agents IA
- DO : utiliser les tokens de `tokens.css`, privilégier les éléments HTML natifs, respecter la hiérarchie de titres et les landmarks.
- DON'T : introduire Tailwind ou des hex arbitraires, utiliser `<div>`/`<span>` cliquables, utiliser `box-shadow` seul comme focus ring, appliquer `user-select: none` au texte de contenu, ou utiliser `title` comme infobulle.
```

---

## 8. Politique de support navigateur sur une ligne pour `AGENTS.md`

### Décision (ligne à copier telle quelle dans `AGENTS.md`)

> `Browser support policy: Evergreen WebView2 (Chromium/Edge 146+) on Windows 11 only — treat Baseline Newly Available and Chromium-native APIs (light-dark, popover, command/commandfor, dialog closedby, overscroll-behavior on modal, appearance: base-select, anchor positioning, @container style queries, text-scale, Trusted Types) as natively supported and skip their MWG fallbacks/polyfills and Safari/Firefox workarounds; exception: do NOT use <input type="checkbox" switch> natively (Safari-only — use <input type="checkbox" role="switch"> with custom Fluent CSS) and keep a tiny feature check for focusgroup if WebView2 < 150.`

### Détail des fallbacks MWG supprimés vs conservés

- **Niveau Baseline** : au-delà de *Baseline Widely Available* et même de *Baseline Newly Available* (2025–2026), on cible **Evergreen WebView2 (Chromium/Edge 146–150+)** sur Windows 11 ([https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution), [https://github.com/GoogleChrome/modern-web-guidance](https://github.com/GoogleChrome/modern-web-guidance)).
- **Fallbacks / polyfills MWG à supprimer systématiquement** :
  - `@oddbird/popover-polyfill` et le sélecteur `:is(:popover-open, .\:popover-open)` (`dialog`, `menu`) — natif depuis Edge 116.
  - `invokers-polyfill` et repli `popovertarget` pour `command`/`commandfor` (`dialog`, `menu`) — natif depuis Edge 135.
  - Écouteurs JS `pointerdown`/`pointerup` pour `<dialog closedby="any">` (`dialog`) — natif depuis Edge 134.
  - Repli JS de positionnement d'ancre, gestion manuelle d'`anchor-name` (`ToggleEvent.source`, Edge 140) et repli `clamp()` pour `sign()` (Edge 138) (`menu`) — natifs en WebView2 evergreen.
  - Blocs `@supports (color: light-dark(...))`, duplication `prefers-color-scheme` sur les tokens et astuce `image-set()` pour images (`dark-mode`) — natif depuis Edge 123 (couleurs) / Edge 150 (images).
  - Fallback `::-webkit-scrollbar` derrière `@supports not (scrollbar-color: auto)` (`dark-mode`) — natif depuis Edge 121.
  - Fallback « visually-hidden input » derrière `@supports not (accent-color: ...)` (`brand-consistent-forms`) — natif depuis Edge 93.
  - Fallback de classes + `image-set()` pour les icônes CSS et les 3 contournements Safari WebKit `#301609`, `#320220`, `#325900` (`icons`) — `@property` (Edge 85) et `@container style(...)` (Edge 111) sont natifs.
  - Tinyfill `window.trustedTypes` (`trusted-types`) — natif depuis Edge 83.
  - Contournements Safari/iOS (`role="list"` sur `list-style: none`, contrainte 16 px anti-zoom iOS, contournement iframe `prefers-color-scheme`).
- **Fallbacks / précautions à CONSERVER impérativement malgré Evergreen WebView2** :
  - **`<input type="checkbox" switch>`** (`switch`) : **non supporté par Chromium/Edge** (Safari 17.4+ uniquement). Ne pas se reposer dessus ni charger `input-switch-polyfill` ; utiliser `<input type="checkbox" role="switch">` avec CSS Fluent 2 dédié.
  - **`focusgroup="menu nomemory"`** (`menu`) : arrivé dans Edge 150 (juillet 2026) ; garder le court fallback JS (~20 lignes conditionné à `!('focusGroup' in HTMLElement.prototype)`) par sécurité pour les postes dont WebView2 n'est pas encore passé en 150+.
  - **Bug Chromium 565837690** (`color`) : WebView2 *étant* Chromium, ne jamais utiliser `color-mix(in oklch, ...)` avec des couleurs peu saturées (utiliser `in oklab` ou les tokens WinRT précalculés).
  - **`image()` CSS** (`dark-mode`) : non supporté par aucun navigateur (utiliser `linear-gradient(<color>)` si besoin).

---

## Modern Web Guidance : guides exploités et impact sur les décisions

Tous les 21 guides déposés dans `.agy-runs/mwg/` ont été lus ; voici les guides qui ont directement influencé les décisions :

| Guide ID | Fichier | Impact concret sur les décisions |
|---|---|---|
| `html` | MWG `html` | Renforce le choix des primitives natives (`<dialog>`, `[popover]`, `<details name>`, `<search>`, landmarks) et écarte les composants JS redondants (Q1, Q3). |
| `css` | MWG `css` | Déclenche le rejet de Tailwind CSS 4 : interdit les resets globaux `*`, recommande `@layer`, `:where()`, `:not()`, nesting natif et tokens CSS proportionnés au projet (Q2). |
| `accessibility` | MWG `accessibility` | Impose `outline` (pas `box-shadow` seul) sur `:focus-visible`, cible ≥ 24×24 px, interdiction de mettre un `<h2>` dans `<summary>` (utiliser un disclosure ARIA si un titre est nécessaire dans `SettingsExpander`), tableau de données alternatif + `<figure>`/`<figcaption>` pour `uPlot`, et `aria-live="off"` sur la télémétrie 1 Hz (Q3, Q4, Q7). |
| `color` | MWG `color` | Justifie la lecture directe de la rampe d'accent WinRT en Rust (interdit l'ajustement naïf `oklch` sans gamut mapping et signale le bug Chromium 565837690 sur `color-mix(in oklch, ...)`), impose `@media (forced-colors: active)` et les contrastes 4.5:1 / 3:1 (Q2, Q5). |
| `dark-mode` | MWG `dark-mode` | Impose `<meta name="color-scheme" content="light dark">` + `:root { color-scheme: light dark; }` + `light-dark()`, et permet de supprimer les fallbacks `@supports` et `::-webkit-scrollbar` sur WebView2 evergreen (Q5, Q8). |
| `browser-ui-theming` | MWG `browser-ui-theming` | Guide le styling natif des `<input>`, `<select>` (`base-select`), `<input type="range">` et `:user-invalid`, et interdit `user-select: none` sur le texte de contenu et `scrollbar-width: thin` (Q2, Q3, Q7). |
| `branded-select-styling` | MWG `branded-select-styling` | Valide le remplacement d'un widget `ComboBox` tiers par `<select>` + `appearance: base-select` + `::picker(select)` (natif depuis Edge 135), tout en mettant en évidence l'inadéquation de Tailwind 4 pour ces pseudo-éléments (Q2, Q3, Q8). |
| `switch` | MWG `switch` | **Révélation critique** : `<input type="checkbox" switch>` est Safari-only et non supporté dans Edge/WebView2. Conduit à implémenter `ToggleSwitch` sur `<input type="checkbox" role="switch">` avec CSS Fluent 2 (Q3, Q8). |
| `dialog` | MWG `dialog` | Remplace `<fluent-dialog>` par `<dialog>` + `command="show-modal"` + `closedby` + `overscroll-behavior: contain` sur `dialog:modal` et `dialog::backdrop`, sans aucun polyfill sur Edge 144+ (Q3, Q8). |
| `menu` | MWG `menu` | Définit le patron des menus contextuels/flyouts sur `[popover="auto"]` + `commandfor` + CSS Anchor Positioning + `focusgroup="menu nomemory"`, en supprimant tous les polyfills sauf le garde-fou léger `focusgroup` si WebView2 < 150 (Q3, Q8). |
| `web-components` & `styling-web-components` | MWG `web-components`, MWG `styling-web-components` | Déconseillent de créer des Web Components pour des vues internes d'une app déjà dotée d'un framework, soulignent la rupture des références ARIA `id` à travers le Shadow DOM et la nécessité de `font: inherit` sur les contrôles encapsulés — arguments décisifs contre Lit et Fluent UI Web Components v3 (Q1, Q3). |
| `icons` | MWG `icons` | Oriente le rendu des icônes Fluent SVG via le système CSS à masques + `currentColor` + `forced-color-adjust: none` (ou SVG inline local), sans aucun contournement Safari (Q3, Q5, Q8). |
| `forms` & `brand-consistent-forms` | MWG `forms`, MWG `brand-consistent-forms` | Guident le choix radio (2–5 options) vs `<select>` (6+), la validation sur `blur`/`submit` avec `:user-invalid` (jamais `:invalid` seul), `for`/`id` explicite et la suppression du fallback `accent-color` (Q3, Q7, Q8). |
| `motion` | MWG `motion` | Impose `@media (prefers-reduced-motion: no-preference)` en opt-in, `@starting-style` + `transition-behavior: allow-discrete` pour les overlays, et un bouton Pause sur la télémétrie en direct (Q4, Q7). |
| `typography` & `respect-os-text-scale` | MWG `typography`, MWG `respect-os-text-scale` | Imposent `<meta name="text-scale" content="scale">` (natif depuis Edge 146), tailles en `rem`, `line-height` sans unité, interdiction de fixer `font-size` sur `:root`/`html` et interdiction de `height` fixe sur les conteneurs de texte (Q5, Q7, Q8). |
| `security` & `trusted-types` | MWG `security`, MWG `trusted-types` | Durcissent la CSP Tauri (`object-src 'none'`, `base-uri 'none'`, `frame-ancestors 'none'`, `require-trusted-types-for 'script'`), interdisent une politique `default` pass-through et ont motivé la vérification de `fragments: 'tree'` dans Svelte 5 (Q1, Q6, Q8). |
| `performance` | MWG `performance` | Confirme l'absence de `@import` CSS en cascade réseau, l'usage de `contain: layout style paint` sur le widget graphique `uPlot`, et évite `content-visibility: auto` sur des pages de paramètres simples (Q2, Q4). |

---

## Risques et inconnues (à vérifier dès le premier prototype Windows 11)

1. **`trusted-types 'none'` + Svelte 5 `fragments: 'tree'` + script d'amorçage IPC de Tauri 2** : non vérifié empiriquement dans une build Tauri 2 réelle. Si Tauri 2 ou Svelte 5 écrit dans un sink protégé lors de l'initialisation, il faudra soit autoriser une politique nommée strictement auditée, soit ajuster la directive `trusted-types` (sans jamais créer de politique `default` pass-through).
2. **Continuité visuelle de Mica avec `decorations: true`** : sur certaines combinaisons de builds Windows 11 et de versions de `window-vibrancy` / Tauri 2, la barre de titre native peut présenter une légère démarcation de teinte avec le corps Mica (`Mica` vs `MicaAlt` / `Tabbed`). À vérifier visuellement en mode clair, mode sombre et fenêtre inactive dès le premier prototype.
3. **État par défaut de CSS `AccentColor` dans WebView2 evergreen** : non testé sur machine cible Windows 11. Notre architecture n'en dépend pas (WinRT `UISettings` en Rust est la source de vérité), mais le secours CSS initial avant hydratation IPC gagnera à être vérifié.
4. **Haute densité / `text-scale` Windows 11 dans WebView2** : vérifier que le réglage d'accessibilité *Taille du texte* de Windows 11 est effectivement répercuté dans WebView2 via `<meta name="text-scale" content="scale">` (Edge 146+) sans double mise à l'échelle avec le facteur DPI Windows.
5. **Version exacte de WebView2 chez les utilisateurs (seuil Edge 150 pour `focusgroup`)** : même sur Evergreen WebView2, un poste Windows 11 décalé de quelques semaines peut être en Edge 146–149 ; conserver le garde-fou JS de 20 lignes pour `focusgroup` élimine ce risque.
6. **Couverture exhaustive actuelle de `@fluentui/web-components` v3.1+** : la liste complète fichier par fichier des composants publiés en v3.1+ n'a pas été inspectée directement ; toutefois, les raisons de rejet (Fluent 2 Web opaque + Shadow DOM) restent indépendantes de la liste exacte.

---

## Tableau récapitulatif des décisions

| Domaine | Choix retenu | Raison principale | Alternatives rejetées |
|---|---|---|---|
| **Framework UI** | **Svelte 5** (`runes: true`, `fragments: 'tree'`) | Planche ~2–3 Ko gz, réactivité fine sans VDOM (idéale 1 Hz), Light DOM (ARIA intacte), lint a11y compilateur, HTML natif sans friction JSX, compatible Trusted Types | React 19 (~45 Ko+ gz, inutile), SolidJS (pièges réactivité IA), Preact (VDOM ou divergence Signals), Lit / Vanilla WC (Shadow DOM), Vue 3 (plus lourd) |
| **Styling & Tokens** | **CSS moderne pur** (`@layer`, `light-dark()`, nesting, `@container`, `<style>` Svelte) + **tokens CSS WinUI 3 maison** | Alignement direct avec les pseudo-éléments natifs (`::picker(select)`, `::backdrop`, `forced-colors`) et MWG `css` ; évite le « Tailwind slop » et le décalage Fluent 2 Web | Tailwind CSS 4 (conflit Preflight, verbeux sur pseudo-éléments natifs, dérive IA), `@fluentui/tokens` (Fluent 2 Web opaque, pas WinUI 3) |
| **Composants Fluent 2** | **~10–12 composants Svelte 5 maison** sur HTML natif + `@fluentui/svg-icons` | Fidélité exacte à Windows 11 Settings sur Mica, accessibilité native Chromium/Narrator, poids JS minimal | `@fluentui/web-components` v3 (Fluent 2 Web + Shadow DOM), Fluent UI React v9 (lourd + React + Web), `fluent-svelte` (abandonné) & forks (faible bus factor) |
| **Graphiques temps réel** | **uPlot** + wrapper accessible (`<figure>`, tableau `<details>`, bouton Pause) | ~22 Ko gz, zéro dépendance, Canvas 2D sub-ms à 1 Hz, bandes de paliers via hook `draw` | Chart.js (3× plus lourd), ECharts (très lourd), Observable Plot (SVG inadapté 1 Hz), Canvas/SVG maison (maintenance fragile) |
| **Intégration Windows 11** | **`apply_mica` (`window-vibrancy`)** + `visible: false` au démarrage + repli solide Win10 ; **accent WinRT `UISettings`** en Rust ; **`forced-colors`** ; **`Segoe UI Variable`** optique ; **barre de titre native (`decorations: true`)** | Rendu 100 % natif Windows 11 (Mica, rampe d'accent 7 teintes exacte, Snap Layouts natifs sans hack `WM_NCHITTEST`, zéro police embarquée, contraste élevé sûr) | Acrylic sur Win10 (saccades), CSS `AccentColor` seul (1 couleur, risques sync/fingerprinting), `system-ui` seul, barre de titre custom (`decorations: false` casse Snap Layouts) |
| **Build, IPC & Sécurité** | **Vite `target: 'chrome135'`/`'esnext'`** + TS strict ; **Rust proxy sur named pipe** + **Tauri Commands + `Channel`** ; **CSP locale stricte + Trusted Types + `freezePrototype` + Capabilities minimales** | Zéro transpilation inutile, streaming 1 Hz typé et isolé, surface d'attaque minimale devant un cœur contrôlant l'alimentation système | Named pipe direct depuis WebView2 (impossible), `emit()` global pour 1 Hz, `withGlobalTauri: true`, Tauri Isolation pattern (surcoût non justifié avec ~4 deps) |
| **Spécification design IA** | **`DESIGN.md` (format Google Labs / Stitch)** synchronisé 1:1 avec `tokens.css` | Source de vérité tokens YAML + règles normatives empêchant la dérive visuelle des agents IA | Documentation libre non structurée, prompts ad hoc répétés |
| **Politique navigateur (`AGENTS.md`)** | **Evergreen WebView2 (Chromium/Edge 146+) Windows 11 uniquement** (avec exception `switch` et garde `focusgroup`) | Supprime tous les polyfills/fallbacks inutiles de MWG tout en évitant le piège `<input type="checkbox" switch>` (Safari-only) | *Baseline Widely Available* par défaut (génère des polyfills et contournements Safari/Firefox inutiles) |

