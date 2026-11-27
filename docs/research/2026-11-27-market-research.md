[PHASE: PLANIFICATION]

# Étude de marché et faisabilité technique : Utilitaire de gestion d’énergie Windows 11 (24/7 Workstation)

> [!NOTE]
> Aucun fichier n’a été créé ni modifié pour ce rapport. Toutes les informations factuelles ci-dessous proviennent de recherches web ciblées ; lorsque certaines données précises (p. ex. empreinte RAM exacte de certains binaires ou date de dernière release d’un dépôt spécifique) n’ont pas pu être vérifiées de manière fiable dans les sources consultées, cela est indiqué explicitement.

---

## A. Inventaire et analyse des outils existants

### 1. Outils Bitsum et gestion de processus / power plans

| Outil | Rôle principal | Recoupement (1–5) | Licence / Prix | Maintenance (dernière version) | Empreinte | Droits Admin / Driver |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Process Lasso** ([bitsum.com](https://bitsum.com/)) | Orchestration CPU/RAM (ProBalance, affinités, CPU Sets, EcoQoS, règles de power plans, IdleSaver, Performance Mode) | **4** (CPU, power plans, EcoQoS) et **2** partiel (bascule idle binaire via IdleSaver) | Freemium (gratuit avec écran de rappel et fonctions avancées Pro limitées après essai) ; Pro ~$39.95 USD/PC à vie ou ~$24.95/an ([bitsum.com](https://bitsum.com/)) | Actif : **v19.0** sortie en **novembre 2026** (v18.0/18.1 mi-2026 avec métriques GPU et widget Power Saver GPU) ([bitsum.com](https://bitsum.com/)) | Très faible : moteur `ProcessGovernor.exe` + `bitsumsessionagent.exe`, polling métriques à la demande uniquement ([bitsum.com](https://bitsum.com/)) | Peut tourner en service système (`Governor-as-a-Service`, admin) ; *note :* présence ou nom exact d’un driver kernel helper séparé non vérifié dans la doc publique récente, bien que la gestion système/service nécessite des privilèges élevés ([bitsum.com](https://bitsum.com/)) |
| **Bitsum ParkControl** ([bitsum.com/parkcontrol/](https://bitsum.com/parkcontrol/)) | Réglage fin du Core Parking, frequency scaling, hybrid thread scheduling P/E-cores, Dynamic Boost (Pro) | **4** (CPU parking, EPP/scaling, bascule active/idle) | Freemium (fonctions de base gratuites ; Pro ~$14.95 USD à vie pour sauvegarde avancée et Dynamic Boost) ([bitsum.com/parkcontrol/](https://bitsum.com/parkcontrol/)) | Actif : **v6.0.2.18** sortie **fin octobre 2026** ([bitsum.com/parkcontrol/](https://bitsum.com/parkcontrol/)) | Très légère (tray icon + live core map, sans suite driver propriétaire lourde) ([bitsum.com/parkcontrol/](https://bitsum.com/parkcontrol/)) | **Admin requis** pour appliquer des modifications aux power schemes système ; s’appuie sur les APIs natives Windows sans kernel driver dédié ([bitsum.com/parkcontrol/](https://bitsum.com/parkcontrol/)) |

**Détails clés :**
- **Process Lasso** inclut déjà :
  - **IdleSaver** (inclus dans l’édition gratuite) : bascule vers un power plan conservateur après un délai d’inactivité ([bitsum.com](https://bitsum.com/)).
  - **Performance Mode** et **Application Power Profiles** : passent automatiquement sur *Bitsum Highest Performance* (ou un autre plan) au lancement d’apps choisies et excluent les processus en Performance Mode de ProBalance ([bitsum.com](https://bitsum.com/)).
  - **Efficiency Mode (`EcoQoS`) persistant** (`Options > CPU > Efficiency Modes`), enregistré dans `ProcessLasso.ini` ([bitsum.com](https://bitsum.com/)).
- En revanche, Process Lasso ne gère **ni le keep-awake explicite** (`PowerCreateRequest`), **ni les tiers progressifs** (active → away → drowsy selon connexions réseau/SSH/RDP ou charge soutenue), **ni l’extinction DDC/CI par moniteur**, **ni les power limits / P-states GPU NVIDIA** au repos (v18.0 ajoute des métriques GPU et un sélecteur de préférence GPU Windows Power Saver/High Performance, pas un contrôle NVML/NVAPI des clocks/TDP).

---

### 2. Outils Keep-Awake (remplacement de PowerToys Awake)

| Outil | Rôle principal | Recoupement (1–5) | Licence / Prix | Maintenance (dernière version) | Empreinte | Droits Admin / Driver |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **PowerToys Awake** ([github.com/microsoft/PowerToys](https://github.com/microsoft/PowerToys), [learn.microsoft.com](https://learn.microsoft.com/windows/powertoys/awake)) | Keep-awake (indéfini, minuté, ou date d’expiration ; maintien écran optionnel) | **1** uniquement (via `SetThreadExecutionState`) | Open-source, **MIT License** ([github.com/microsoft/PowerToys](https://github.com/microsoft/PowerToys)) | Actif (mises à jour régulières via Microsoft PowerToys ; numéro exact de la toute dernière release non relevé ici, voir [releases](https://github.com/microsoft/PowerToys/releases)) | `PowerToys.Awake.exe` isolé : **< 10 Mo RAM**, ~0 % CPU au repos ; mais embarqué dans l’écosystème PowerToys Runner plus lourd ([github.com/microsoft/PowerToys](https://github.com/microsoft/PowerToys)) | Aucun admin ni driver requis (`SetThreadExecutionState` user-mode) ([github.com/microsoft/PowerToys](https://github.com/microsoft/PowerToys)) |
| **Don't Sleep** ([softwareok.com](https://www.softwareok.com/?seite=Microsoft/DontSleep)) | Bloque veille, hibernation, arrêt, extinction écran, avec règles conditionnelles (charge CPU, trafic réseau, minuterie) | **1** (blocage veille) + rudiments de **2** (déclencheurs CPU/réseau inverses) | **Freeware** (gratuit perso & commercial, dons acceptés) ([softwareok.com](https://www.softwareok.com/?seite=Microsoft/DontSleep)) | Actif : **v10.44** sortie **fin octobre 2026** ([softwareok.com](https://www.softwareok.com/?seite=Microsoft/DontSleep)) | Portable, ~267 Ko sur disque ; RAM exacte non vérifiée | Aucun driver requis ; usage standard sans admin pour le blocage basique (élévation pour certaines actions système non vérifiée) |
| **Caffeine** ([zhornsoftware.co.uk/caffeine/](https://www.zhornsoftware.co.uk/caffeine/)) | Simule une frappe clavier (`F15` toutes les 59 s, ou `-useshift`) pour empêcher veille et économiseur d’écran | **1** uniquement (approche par simulation d’input) | **Freeware** (perso & commercial) ([zhornsoftware.co.uk/caffeine/](https://www.zhornsoftware.co.uk/caffeine/)) | Stable/peu modifié : **v1.98** (date exacte de sortie non vérifiée) ([zhornsoftware.co.uk/caffeine/](https://www.zhornsoftware.co.uk/caffeine/)) | Minimaliste (binaire léger en tray, valeur RAM exacte non vérifiée) | Aucun admin ni driver requis ([zhornsoftware.co.uk/caffeine/](https://www.zhornsoftware.co.uk/caffeine/)) |

**Point critique pour le cahier des charges :**
- PowerToys Awake utilise `SetThreadExecutionState` (`ES_CONTINUOUS | ES_SYSTEM_REQUIRED`) et **non** `PowerCreateRequest` / `PowerSetRequest` ([github.com/microsoft/PowerToys](https://github.com/microsoft/PowerToys)). Dans `powercfg /requests`, il n’expose donc pas de chaîne de raison structurée (`REASON_CONTEXT`) aussi descriptive qu’un `PowerCreateRequest` dédié.
- `Caffeine` simule une touche (`F15`) toutes les 59 s, ce qui **casse la détection d’inactivité réelle** (`GetLastInputInfo`) et empêcherait tout moteur de tiers d’inactivité (active → away → drowsy) de fonctionner ! À éviter absolument dans votre architecture.

---

### 3. Contrôle moniteurs et DDC/CI

| Outil | Rôle principal | Recoupement (1–5) | Licence / Prix | Maintenance (dernière version) | Empreinte | Droits Admin / Driver |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Twinkle Tray** ([github.com/xanderfrangos/twinkle-tray/releases](https://github.com/xanderfrangos/twinkle-tray/releases)) | Luminosité multi-écrans (DDC/CI & WMI), VCP personnalisés, raccourcis, planification et action d’extinction d’écrans | **3** partiel (supporte VCP `0xD6` ou `SC_MONITORPOWER`, pas de timeouts différenciés par modèle ni profil de quirks complets) | Open-source, **MIT License** ([github.com/xanderfrangos/twinkle-tray/releases](https://github.com/xanderfrangos/twinkle-tray/releases)) | Actif : **v1.17.2** (date exacte du tag non vérifiée dans le résumé) ([github.com/xanderfrangos/twinkle-tray/releases](https://github.com/xanderfrangos/twinkle-tray/releases)) | **Lourde pour un outil tray (Electron)** : ~50–90 Mo RAM en arrière-plan sur plusieurs processus ([github.com/xanderfrangos/twinkle-tray/releases](https://github.com/xanderfrangos/twinkle-tray/releases)) | Aucun admin ni driver requis (user-mode DDC/CI) |
| **Monitorian** ([github.com/emoacht/Monitorian/releases](https://github.com/emoacht/Monitorian/releases)) | Contrôle rapide luminosité/contraste multi-écrans (DDC/CI & WMI) | **0–3 très faible** (focus luminosité, pas de gestion avancée de mise hors tension VCP `0xD6` par timeout) | Open-source, **MIT License** ([github.com/emoacht/Monitorian/releases](https://github.com/emoacht/Monitorian/releases)) | Actif : **v4.16.1** (installers jusqu’à `4.16.14`, date exacte non vérifiée) ([github.com/emoacht/Monitorian/releases](https://github.com/emoacht/Monitorian/releases)) | Très légère (.NET Framework 4.8) : ~1–1,5 Mo installateur, ~10–25 Mo RAM en idle ([github.com/emoacht/Monitorian/releases](https://github.com/emoacht/Monitorian/releases)) | Aucun admin ni driver requis |
| **ClickMonitorDDC** (site officiel disparu ; miroirs communautaires sur GitHub/MajorGeeks/Softpedia) | Tray DDC/CI historique (luminosité, contraste, volume, commandes VCP arbitraires incluant `0xD6`) | **3** partiel (commandes VCP manuelles/CLI, pas d’orchestration idle multi-écrans native) | Freeware propriétaire (termes exacts non vérifiables suite à la disparition du site d’origine) | **Abandonné / Discontinued** : dernière version **v7.2** (~mi-2020) | Très légère (outil natif Windows, valeur RAM exacte non vérifiée) | Aucun admin ni driver requis pour DDC/CI standard |
| **NirSoft ControlMyMonitor** ([nirsoft.net](https://www.nirsoft.net/utils/control_my_monitor.html)) | Inspection et écriture GUI/CLI de tous les VCP codes (`SetValue D6 4/5`, `/TurnOff`, `/TurnOn`) | **3** (outil CLI idéal pour exécuter VCP `0xD6`, sans daemon ni détection d’inactivité) | **Freeware pour usage personnel** (redistribution/usage commercial restreint sans autorisation) ([nirsoft.net](https://www.nirsoft.net/utils/control_my_monitor.html)) | Actif : **v1.43** (date exacte de sortie non vérifiée) ([nirsoft.net](https://www.nirsoft.net/utils/control_my_monitor.html)) | Minuscule (utilitaire natif C++, exécutable à la demande, pas de daemon résident) | Aucun admin ni driver requis (`dxva2.dll`) |

---

### 4. Tuning CPU / GPU et profils bas niveau

| Outil | Rôle principal | Recoupement (1–5) | Licence / Prix | Maintenance (dernière version) | Empreinte | Droits Admin / Driver |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **ThrottleStop** ([techpowerup.com](https://www.techpowerup.com/download/techpowerup-throttlestop/)) | Undervolting FIVR, PL1/PL2, Speed Shift EPP, PROCHOT, profils manuels ou AC/Battery | **4** partiel (bascule profils surtout AC/Battery ou alarmes thermiques, pas par idle Windows ni P/E-core QoS) | **Freeware** propriétaire ([techpowerup.com](https://www.techpowerup.com/download/techpowerup-throttlestop/)) | Actif : **v9.7** (26 déc. 2024) et **v9.7.3** (avril 2025) ([techpowerup.com](https://www.techpowerup.com/download/techpowerup-throttlestop/)) | Très légère (natif C++, valeur RAM exacte non vérifiée) | **Admin + driver kernel** (`ThrottleStop.sys` / accès MSR ring-0), souvent bloqué par Windows 11 24H2+ Vulnerable Driver Blocklist / HVCI |
| **Intel XTU** ([intel.com](https://www.intel.com/content/www/us/en/download/17881/intel-extreme-tuning-utility-intel-xtu.html)) | Overclocking/undervolting officiel Intel, ratios, power limits | **4** marginal (pas conçu pour bascule idle/drowsy automatique) | **Freeware** propriétaire Intel ([intel.com](https://www.intel.com/content/www/us/en/download/17881/intel-extreme-tuning-utility-intel-xtu.html)) | Actif : branche **v7.14** pour 14e gén (i9-14900K) ; branche **v10.0** (v10.0.1.248 nov. 2026) réservée aux Core Ultra Series 2 ([intel.com](https://www.intel.com/content/www/us/en/download/17881/intel-extreme-tuning-utility-intel-xtu.html)) | Lourde (service Intel XTU en arrière-plan + UI, RAM exacte non vérifiée) | **Admin + drivers kernel Intel** (IPF/XTU) |
| **MSI Afterburner** ([msi.com](https://www.msi.com/Landing/afterburner/graphics-cards)) | OC/UV GPU (courbe V/F), courbes ventilation, monitoring RTSS, bascule auto de profils 2D/3D par exécutable | **5** partiel (bascule selon app 2D/3D lancée, pas selon idle système) | **Freeware** propriétaire ([msi.com](https://www.msi.com/Landing/afterburner/graphics-cards)) | Actif : **v4.6.6 stable** (fin sept./oct. 2025) ([msi.com](https://www.msi.com/Landing/afterburner/graphics-cards)) | Modérée (MSI Afterburner + RTSS optionnel ; RAM exacte non vérifiée) | **Admin + driver kernel** (`RTCore64.sys` sur anciennes versions souvent bloqué par Defender/Blocklist Windows 11 ; v4.6.6 modernisée) |
| **NVIDIA Inspector / Multi Display Power Saver (MDPS)** ([github.com/Orbmu2k/nvidiaProfileInspector](https://github.com/Orbmu2k/nvidiaProfileInspector)) | MDPS (dans l’ancien NVIDIA Inspector) tente de forcer l’état P8 en multi-écrans ; `nvidiaProfileInspector` édite les profils driver NVAPI | **5** partiel (contournement P-state multi-écrans, souvent source de micro-stutter ou instabilités) | Open-source, **MIT License** ([github.com/Orbmu2k/nvidiaProfileInspector](https://github.com/Orbmu2k/nvidiaProfileInspector)) | `nvidiaProfileInspector` maintenu sur GitHub (date exacte de dernier tag non vérifiée) ; l’outil historique NVIDIA Inspector incluant MDPS n’est plus le focus principal ([github.com/Orbmu2k/nvidiaProfileInspector](https://github.com/Orbmu2k/nvidiaProfileInspector)) | Légère (.NET) | **Admin requis** pour modifier certains réglages/clocks via NVAPI ; pas de driver tiers (utilise le driver NVIDIA officiel) |
| **`nvidia-smi` / NVML** ([docs.nvidia.com/deploy/nvml-api](https://docs.nvidia.com/deploy/nvml-api/index.html)) | CLI & API C pour télémétrie GPU, power limit (`-pl` / `nvmlDeviceSetPowerManagementLimit`), lock de clocks core (`-lgc` / `nvmlDeviceSetGpuLockedClocks`) | **5** (socle idéal pour votre module GPU optionnel) | Propriétaire NVIDIA (SDK/driver inclus avec le pilote GeForce) ([docs.nvidia.com/deploy/nvml-api](https://docs.nvidia.com/deploy/nvml-api/index.html)) | Maintenu en continu avec les pilotes NVIDIA GeForce/Data Center | Négligeable (`nvml.dll` déjà présente avec le driver) | Lecture sans admin ; **Admin requis** sous Windows pour écrire les limites TDP/clocks (`NVML_ERROR_NO_PERMISSION` sinon) ; utilise uniquement le driver officiel NVIDIA |

---

### 5. Switchers de Power Plans, EcoQoS et projets GitHub récents

| Outil | Rôle principal | Recoupement (1–5) | Licence / Prix | Maintenance (dernière version) | Empreinte | Droits Admin / Driver |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Windows Away Mode** (`ES_AWAYMODE_REQUIRED` / `PowerRequestAwayModeRequired`) ([SetThreadExecutionState](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate), [PowerSetRequest](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-powersetrequest)) | Mécanisme OS natif permettant à un PC de « paraître éteint » (audio/vidéo coupés) au lieu d’entrer en veille tout en exécutant des tâches | **1** & **2** très partiel (conçu pour enregistrement/streaming média, ne réduit pas activement CPU/GPU ni ne gère le DDC par écran) | API native Windows | Intégré à Windows | Nulle | User-mode (soumis à la stratégie système « Allow Away Mode » et limité aux systèmes **S3**) |
| **PowerSwitcher** (`petrroll/PowerSwitcher`) & fork **`ChangemakerStudios/PowerSwitcher`** ([petrroll](https://github.com/petrroll/PowerSwitcher), [ChangemakerStudios](https://github.com/ChangemakerStudios/PowerSwitcher)) | Flyout tray Windows 10/11 pour changer manuellement de power plan | **4** très basique (manuel uniquement) | Open-source, **MIT License** ([petrroll](https://github.com/petrroll/PowerSwitcher)) | Original abandonné depuis des années ; fork `ChangemakerStudios` actif pour Windows 11 (date exacte de dernière release non vérifiée) | Légère (.NET) | Dépend des ACLs des schémas (voir Section D) |
| **PowerPlanSwitcher** (`SebastianBecker2/PowerPlanSwitcher`) ([github.com](https://github.com/SebastianBecker2/PowerPlanSwitcher)) | Tray Windows 10/11 avec règles auto (processus, secteur/batterie, démarrage, **idle time**) | **2** basique + **4** (bascule de power plan selon idle/processus) | Open-source, **MIT License** ([github.com](https://github.com/SebastianBecker2/PowerPlanSwitcher)) | Actif : **v1.3.1.0** sortie en **novembre 2026** | Légère (RAM exacte non vérifiée) | Sans driver ; écriture schémas selon ACLs/admin |
| **IdlePower** (`sytelus/idlepower`) ([github.com/sytelus/idlepower](https://github.com/sytelus/idlepower)) | Tray événementiel (.NET 8) basculant entre plan *Normal* et un plan *Away* dédié (max CPU 30 % par défaut, boost OFF, PCIe ASPM max) sur verrouillage session / absence Windows, avec récupération fail-closed (`IdlePower.Restore.exe`) | **2** & **4** (bascule binaire événementielle Normal ↔ Away, pas de tiers progressifs, pas de DDC/GPU/keep-awake) | Open-source, **MIT License** ([github.com/sytelus/idlepower](https://github.com/sytelus/idlepower)) | Disponible sur GitHub (date exacte de dernière release non vérifiée) ([github.com/sytelus/idlepower](https://github.com/sytelus/idlepower)) | Légère (.NET 8 self-contained ; valeur RAM exacte non vérifiée) | Sans driver, conçu pour tourner **sans droits admin** en configuration standard ([github.com/sytelus/idlepower](https://github.com/sytelus/idlepower)) |
| **EnergyStar** (`imbushuo/EnergyStar`), **EnergyStarX** (`JasonWei512/EnergyStarX`), **EnergyStarZ** (`wosledon/EnergyStarZ`) ([EnergyStar](https://github.com/imbushuo/EnergyStar), [EnergyStarX](https://github.com/JasonWei512/EnergyStarX), [EnergyStarZ](https://github.com/wosledon/EnergyStarZ)) | Applique dynamiquement **EcoQoS** (`PROCESS_POWER_THROTTLING_EXECUTION_SPEED`) aux processus en arrière-plan (et I/O/mémoire basse priorité dans `EnergyStarZ`) ; le foreground est épargné | **4** (EcoQoS dynamique uniquement) | `EnergyStar`: MIT avec exemptions Microsoft ; `EnergyStarX`: **GPL-3.0** ; `EnergyStarZ`: licence exacte non vérifiée | Activité/commits jusque fin 2025 pour `EnergyStarX` (dates exactes 2026 non vérifiées) | Très faible (API OS événementielle sans polling lourd ; WinUI 3 pour `EnergyStarX`) | **Sans admin ni driver** (ne peut toutefois pas throttler les processus système/élevés) ([github.com/imbushuo/EnergyStar](https://github.com/imbushuo/EnergyStar)) |

---

### 6. Équivalents Linux (inspiration d’architecture uniquement)

- **TLP** ([github.com/linrunner/TLP](https://github.com/linrunner/TLP), GPL-2.0+) : modèle déclaratif de profils AC/Battery + `tlp-pd` (D-Bus depuis TLP 1.9). *Leçon :* séparation nette entre moteur de politique et client UI, gestion des conflits via un unique propriétaire des sous-systèmes CPU/PCIe.
- **power-profiles-daemon (PPD)** ([gitlab.freedesktop.org/hadess/power-profiles-daemon](https://gitlab.freedesktop.org/hadess/power-profiles-daemon), GPL-3.0+) : expose trois profils canoniques (*power-saver*, *balanced*, *performance*) via D-Bus et supporte les **holds** temporaires avec raison (équivalent conceptuel de `PowerCreateRequest` + overlay Windows).
- **Tuned** ([github.com/redhat-performance/tuned](https://github.com/redhat-performance/tuned), GPL-2.0+) : profils composables par héritage + couche de compatibilité `tuned-ppd`. *Leçon :* séparer l’état cible en « profils composables » (CPU, PCIe, GPU, affichage) plutôt que tout coder en dur.
- **auto-cpufreq** ([github.com/AdnanHodzic/auto-cpufreq](https://github.com/AdnanHodzic/auto-cpufreq), GPL-3.0+) : boucle autonome basée sur télémétrie (charge CPU, température) qui adapte dynamiquement gouverneur/turbo. *Leçon :* éviter les boucles de polling agressives en idle (préférer les hooks événementiels Windows + hystérésis sur la charge soutenue pour vos agents IA).

---

## B. Existe-t-il déjà un outil tout-en-un ?

**Non.** D’après les recherches effectuées, **aucun outil Windows existant (open-source ou commercial)** ne combine :
1. Un keep-awake à raisons lisibles (`PowerCreateRequest`),
2. Des tiers progressifs (`active → away → drowsy`) sensibles aux connexions distantes / charge des agents IA,
3. L’extinction individuelle des moniteurs via DDC/CI (`0xD6`) avec timeouts différenciés et table de quirks par modèle,
4. Un bridage CPU hybride fin (P/E-cores, EPP, boost, EcoQoS),
5. Un contrôle GPU NVIDIA optionnel via NVML/NVAPI.

**Les plus proches partiels :**
- **`sytelus/idlepower`** ([github.com/sytelus/idlepower](https://github.com/sytelus/idlepower)) : se rapproche le plus de la philosophie *Normal ↔ Away* événementielle sans casser les plans existants ni laisser de trace en cas de crash, mais reste binaire (2 états), sans keep-awake, sans DDC/CI et sans GPU.
- **`SebastianBecker2/PowerPlanSwitcher`** ([github.com](https://github.com/SebastianBecker2/PowerPlanSwitcher)) : gère des règles idle/processus sur les power plans, mais sans DDC/CI, sans GPU, ni conscience des sessions réseau/agents.
- **`Process Lasso`** ([bitsum.com](https://bitsum.com/)) : couvre très bien le CPU et EcoQoS, mais pas le DDC/CI, pas le keep-awake et pas le power-capping GPU.

---

## C. Coexistence avec Process Lasso

### Conflits directs identifiés
1. **Conflit de propriétaire du Power Plan actif (`PowerSetActiveScheme`) :**
   - Si vous laissez activés **IdleSaver**, **Performance Mode** ou les **Application Power Profiles** dans Process Lasso, **et** que votre nouvel outil bascule aussi le power scheme actif, les deux outils vont se battre (race condition sur le schéma actif). Bitsum documente d’ailleurs déjà des conflits internes entre *IdleSaver* et *Performance Mode* global ([bitsum.com](https://bitsum.com/)).
   - **ParkControl** (*Dynamic Boost*) entre également en conflit avec Process Lasso *IdleSaver* ; Bitsum déconseille explicitement d’exécuter les deux en parallèle ([bitsum.com](https://bitsum.com/)).
2. **Conflit sur l’overlay Windows 11 (`PowerSetActiveOverlayScheme`) :**
   - `PowerSetActiveOverlayScheme` n’est actif que lorsque le plan de base est **Balanced**. Si Process Lasso bascule le système sur *Bitsum Highest Performance*, les overlays Windows 11 deviennent inopérants ([StackOverflow/GitHub refs via recherche](https://learn.microsoft.com/en-us/windows/win32/api/powersetting/nf-powersetting-powersetactivescheme)).
3. **Conflit sur EcoQoS (`SetProcessInformation`) :**
   - Si Process Lasso force *Efficiency Mode OFF/ON* sur un exécutable dans `ProcessLasso.ini` et que votre outil appelle `SetProcessInformation(ProcessPowerThrottling)` sur le même PID, le dernier appel gagne et Process Lasso risque de réappliquer sa règle.

### Stratégies de coexistence recommandées
1. **Séparation stricte des responsabilités (recommandé) :**
   - **Laissez à Process Lasso** : ProBalance, affinités CPU, CPU Sets, priorités I/O, et règles permanentes d’*Efficiency Mode* par application.
   - **Désactivez dans Process Lasso** : *IdleSaver* (et, si vous voulez que le nouvel outil gère les états *active/away/drowsy*, désactivez aussi *Performance Mode* auto-switching de plan ou limitez-le à des exclusions ProBalance).
2. **Ne pas muter le Power Plan si Process Lasso gère déjà les plans :**
   - Détectez la présence de `ProcessGovernor.exe` / `ProcessLasso.exe` (via énumération de processus).
   - Si Process Lasso a la main sur les power plans (p. ex. lecture passive de `ProcessLasso.ini` pour détecter si `IdleSaver` ou `PerformanceMode` est actif, ou configuration explicite côté utilisateur), votre outil peut soit :
     - **Mode « Overlay-only »** : écrire uniquement des overrides sur le schéma courant / overlay *Best Power Efficiency* **si et seulement si** le schéma courant est `Balanced` ;
     - **Mode « Drowsy Plan propriétaire »** (à la `sytelus/idlepower`) : uniquement si *IdleSaver* et *Performance Mode* sont désactivés dans Process Lasso.
3. **Ne touchez pas aux processus gérés par Process Lasso pour EcoQoS**, ou parsez la section correspondante de `ProcessLasso.ini` en lecture seule pour respecter une liste d’exclusions.

---

## D. Notes de faisabilité technique sur Windows 11 (26H2)

### 1. Changement de power plan sans droits administrateur
- **`PowerSetActiveScheme`** ([learn.microsoft.com](https://learn.microsoft.com/en-us/windows/win32/api/powersetting/nf-powersetting-powersetactivescheme)) : permet généralement à un utilisateur standard de basculer entre des power plans **existants** si les ACLs par défaut du profil utilisateur ne sont pas restreintes (c’est ainsi que fonctionne `sytelus/idlepower` sans admin : [github.com/sytelus/idlepower](https://github.com/sytelus/idlepower)).
- **`PowerWriteACValueIndex`** : modifie les index internes sous `HKLM\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes` et échoue typiquement avec `ERROR_ACCESS_DENIED` (5) sans droits admin — à moins d’avoir délégué les ACLs via `powercfg /setsecuritydescriptor` ou via une tâche planifiée / service élevé. Par ailleurs, toute écriture via `PowerWriteACValueIndex` ne prend effet qu’après un appel à `PowerSetActiveScheme` ([learn.microsoft.com](https://learn.microsoft.com/en-us/windows/win32/api/powersetting/nf-powersetting-powersetactivescheme)).
- **`PowerSetActiveOverlayScheme`** (API non documentée de `powrprof.dll`) : fonctionne **sans droits admin** avec les GUIDs `961cc777-2547-4f9d-8174-7d86181b8a7a` (*Best Power Efficiency*), `00000000-...` (*Balanced*), `ded574b5-...` (*Best Performance*), **mais uniquement lorsque le plan de base actif est `Balanced`**.

### 2. Paramètres CPU pertinents sur Intel hybride (i9-14900K)
Sous le sous-groupe `SUB_PROCESSOR` (`54533251-82be-4824-96c1-47b60b740d00`), les réglages déterminants pour un état *drowsy* réactif sont :
- **`PERFEPP` (`36687f9e-e3a5-4dbf-b1dc-15eb381c6863`)** : Energy Performance Preference (`0` = perf max, `80–100` = économie max). Sur CPU hybride, il existe aussi un variant de classe 1 (`PERFEPP1` pour P-cores — alias exact de classe 1 à vérifier localement via `powercfg /qh`).
- **`HETEROPOLICY` (`7f2f5cfa-f10c-4823-b5e1-e93ae85f46b5`)**, **`SCHEDPOLICY` (`93b8b6dc-0698-4d1c-9ee4-0644e900c85d`)** (threads longs) et **`SHORTSCHEDPOLICY` (`bae08b81-2d5e-4688-ad6a-13243356654b`)** (threads courts) : valeurs `3` (*Efficient processors*) ou `4` (*Prefer efficient processors*) pour orienter le travail vers les E-cores en état *drowsy*, tout en gardant `5` (*Automatic*) ou `2` en état *active*.
- **`CPMINCORES` (`0cc5b647-c1df-4637-891a-dec35c318583`)** / **`CPMAXCORES` (`ea062031-0e34-4ff1-9b6d-eb1059334028`)** : Core Parking (laisser le parking actif à 0–5 % en *drowsy* aide à économiser sans couper le wake-up rapide).
- **`PERFBOOSTMODE` (`be337238-0d82-4146-a960-4f3749d470c7`)** : `0` (*Disabled*) coupe le Turbo Boost (gros gain en conso idle/bruit sur 14900K, avec réveil immédiat dès repassage en *active*).
- **`PROCTHROTTLEMAX` (`bc5038f7-23e0-4960-96da-33abaf5935ec`)** : limite la fréquence maximale en % (sur hybride, `PROCTHROTTLEMAX1` cible la classe P-core). *Attention :* brider trop bas (`< 30 %`) peut ralentir vos agents IA si un burst survient avant la sortie de l’état *drowsy* ; couper le boost (`PERFBOOSTMODE = 0`) + `EPP = 85` + `SCHEDPOLICY = 4` offre un meilleur compromis latence/watts qu’un plafond strict à 20 %.

### 3. EcoQoS sur processus tiers
- Via `SetProcessInformation` (`ProcessPowerThrottling`, `PROCESS_POWER_THROTTLING_EXECUTION_SPEED`) ([learn.microsoft.com](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation)) :
  - Requiert d’ouvrir le processus cible avec `PROCESS_SET_INFORMATION | PROCESS_QUERY_LIMITED_INFORMATION`.
  - Fonctionne **sans admin** uniquement sur les processus du même utilisateur au même niveau d’intégrité (non élevés).
  - Pour cibler des processus élevés, d’autres sessions ou services, l’outil doit tourner en admin (et parfois activer `SeDebugPrivilege`), tout en échouant avec `ERROR_ACCESS_DENIED` (5) sur les processus protégés (PPL / anti-cheat).
  - *Doublon fort avec Process Lasso et EnergyStarX.*

### 4. Sémantique de Windows Away Mode
- `ES_AWAYMODE_REQUIRED` (`SetThreadExecutionState`) et `PowerRequestAwayModeRequired` (`PowerCreateRequest` / `PowerSetRequest`) n’existent **que sur les systèmes en veille traditionnelle S3**, et uniquement si « Allow Away Mode » est activé dans la stratégie d’alimentation ([SetThreadExecutionState](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate), [PowerSetRequest](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-powersetrequest)).
- Sur Modern Standby (S0ix), Away Mode ne s’applique pas ; et même en S3, Away Mode ne se déclenche qu’en réponse à une demande explicite de mise en veille utilisateur/système (il coupe l’audio/vidéo pour simuler une veille tout en laissant tourner le CPU).
- Pour votre besoin (PC éveillé 24/7 avec gestion graduée des écrans et du CPU/GPU sans jamais entrer en S3/S0ix), **Away Mode est inapproprié** : `PowerCreateRequest` (`PowerRequestSystemRequired` avec `POWER_REQUEST_CONTEXT_SIMPLE_STRING`) est bien plus propre et transparent dans `powercfg /requests`.

### 5. Mesure de puissance Package CPU / GPU sans kernel driver
- **CPU Package (Intel RAPL) :** Windows bloque tout accès direct user-mode aux MSR (`rdmsr`). Sans kernel driver (et les drivers classiques type `WinRing0.sys` sont bloqués par la Microsoft Vulnerable Driver Blocklist / HVCI de Windows 11 24H2+, ce qui pousse LibreHardwareMonitor vers le driver signé **`PawnIO`** : [github.com/namazso/PawnIO](https://github.com/namazso/PawnIO)), la seule voie native user-mode est **Windows Energy Meter Interface (EMI)** (`GUID_DEVICE_ENERGY_METER`, `IOCTL_EMI_GET_MEASUREMENT` : [learn.microsoft.com](https://learn.microsoft.com/en-us/windows/win32/power/ioctl-emi-get-measurement)).
  - *Limite majeure vérifiée :* si le firmware ACPI/plateforme d’une carte mère desktop ne expose pas de canal RAPL via EMI, l’interface n’est pas disponible (outils comme CodeCarbon basculent alors sur une estimation par charge CPU) — à tester sur votre carte mère Z790/14900K avant de compter dessus.
  - Les diagnostics `powercfg /energy` et `powercfg /srumutil` (E3/SRUM) fournissent des rapports a posteriori, pas une télémétrie temps réel à la seconde.
- **GPU (RTX 3080 Ti) :** **NVML** (`nvmlDeviceGetPowerUsage`) fournit la puissance carte en milliwatts **sans kernel driver tiers** (utilise le driver NVIDIA officiel) et fonctionne **en lecture sans droits admin** ([docs.nvidia.com/deploy/nvml-api](https://docs.nvidia.com/deploy/nvml-api/index.html)).

### 6. Pièges DDC/CI sur votre configuration (ASUS ProArt + EIZO ColorEdge CS2740)
- **API Windows (`dxva2.dll`) :** `SetVCPFeature` ([learn.microsoft.com](https://learn.microsoft.com/en-us/windows/win32/api/lowlevelmonitorconfigurationapi/nf-lowlevelmonitorconfigurationapi-setvcpfeature)) implique une latence matérielle (~50 ms minimum par appel sur le bus I²C/AUX) et bloque le thread appelant si exécuté de façon synchrone.
- **Boucle Hot-Plug DisplayPort (HPD) :** Sur de nombreux moniteurs DisplayPort (notamment **ASUS ProArt** et **EIZO ColorEdge CS2740**), le mode *DisplayPort Deep Sleep* coupe le signal HPD en veille. Windows interprète cela comme un débranchement physique, réorganise les fenêtres/icônes et peut déclencher une boucle de reconnexion/déconnexion infinie.
- **Risque VCP `0xD6` (Power Mode = 4 ou 5) :** Sur certains panneaux (explicitement signalé sur des **ASUS ProArt**), envoyer `0xD6 = 5` (ou `4`) coupe l’étage scaler/récepteur DDC/CI : le moniteur **ne se réveille plus** au mouvement souris/clavier ni via DDC (`0xD6 = 1`), imposant un débranchement électrique ([issue documentée Twinkle Tray / ASUS ProArt](https://github.com/xanderfrangos/twinkle-tray/releases)).
- **Spécificité EIZO ColorEdge CS2740 & ColorNavigator 7 :** ColorNavigator communique souvent via le câble **USB upstream** (USB-C / USB-B) et/ou DDC/CI pour calibrer la LUT interne. Des appels `SetVCPFeature` concurrents pendant une calibration ou pendant une perte HPD peuvent perturber ColorNavigator. Il est donc crucial d’avoir une **matrice de quirks par modèle (EDID/PNP ID)** : p. ex. ne jamais envoyer `0xD6=5` à l’EIZO ou à un ASUS ProArt sensible, ou préférer `SC_MONITORPOWER` / désactiver *DP Deep Sleep* dans l’OSD / réduire d’abord la luminosité (`0x10`) si `0xD6` est bloquant.

### 7. Comportement P-state NVIDIA RTX 3080 Ti en multi-écrans
- Avec **3 moniteurs** (fréquences mixtes ou élevées), les GPU Ampere (RTX 3080 Ti) restent fréquemment bloqués en état **P0** avec la VRAM à fréquence maximale (~9501 MHz), consommant **75–120 W au repos** au lieu de **10–20 W en P8**.
- **`nvidia-smi -pl` / `nvmlDeviceSetPowerManagementLimit`** : plafonne la puissance maximale sous charge, **mais ne fait pas redescendre la VRAM en P8 au repos**.
- **`nvidia-smi -lgc` / `nvmlDeviceSetGpuLockedClocks`** : verrouille le **core clock**, pas le memory clock (déjà bas au repos ~210 MHz), et n’abaisse donc que peu la conso idle multi-écrans.
- **Ce qui réduit vraiment la conso GPU en idle/drowsy :**
  1. Éteindre physiquement ou désactiver logiciellement les moniteurs secondaires (si l’écran est réellement hors ligne / en veille sans garder un pipeline multi-scanout à 144 Hz, la carte peut redescendre vers P8 — attention toutefois au réarrangement des fenêtres Windows si HPD disparaît) ;
  2. Uniformiser les taux de rafraîchissement (p. ex. passer à 60 Hz en état *drowsy* via `ChangeDisplaySettingsEx`, réversible instantanément sans perdre le layout multi-écrans ! — piste technique très supérieure au simple `-pl`) ;
  3. Utiliser `-pl` + `-lgc` en *drowsy* uniquement comme garde-fou thermique si un processus lance du calcul GPU léger par erreur, via un service ou une tâche planifiée élevée pour éviter les invites UAC.

---

## E. Manques identifiés (Gaps) : quoi développer vs quoi déléguer

**Ce que le nouvel outil apporterait d’inédit :**
1. **Un véritable daemon d’états gradués (`active → away → drowsy`) conçu pour station 24/7 « never-sleep »** : utilisant `PowerCreateRequest` (`PowerRequestSystemRequired` + raison lisible), et prenant en compte les sessions distantes (connexions TCP/SSH/RDP actives, charge soutenue des agents IA) avec retour instantané à l’état actif.
2. **Gestion DDC/CI par moniteur avec profil de quirks EDID** : timeouts différenciés par écran, repli automatique sur baisse de luminosité (`0x10`) ou veille Windows standard quand VCP `0xD6` est dangereux (ASUS ProArt / EIZO CS2740), et mécanisme de sécurité fail-closed (réveil garanti à la fermeture/crash).
3. **Abaissement dynamique du refresh rate des écrans + garde-fou NVML en *drowsy*** (via helper élevé optionnel sans UAC répété) pour contrer le blocage P0 multi-écrans de la RTX 3080 Ti sans couper le DisplayPort HPD.
4. **Coexistence native avec Process Lasso** (détection de `ProcessGovernor.exe`, lecture de sa config, absence de guerre de power plans).

**Ce qu’il vaut mieux laisser aux outils existants :**
- **ProBalance, affinités CPU, CPU Sets et règles permanentes EcoQoS par application** → à laisser intégralement à **Process Lasso** (déjà installé, extrêmement mature, à jour en nov. 2026).
- **Undervolting / overclocking CPU ou GPU** → à laisser au BIOS / **ThrottleStop** / **MSI Afterburner** (inutile et risqué de réimplémenter un kernel driver bloqué sous Windows 11 26H2).
- **Calibration LUT matérielle de l’EIZO** → à laisser à **ColorNavigator 7** (avec verrou/pause de vos polling DDC quand ColorNavigator tourne).

---

## Tableau de recommandations final

| Fonctionnalité envisagée | Décision | Justification synthétique |
| :--- | :--- | :--- |
| **1. Keep-awake intégré (`PowerCreateRequest` avec raison lisible)** | **Build** | Remplace PowerToys Awake (qui utilise `SetThreadExecutionState` sans raison riche et s’inscrit dans une suite plus lourde) avec une empreinte Rust minimale et un contrôle fin par état. |
| **2. États intermédiaires multi-niveaux (`active → away → drowsy`, session/load/réseau)** | **Build** | Aucun outil existant (même `IdlePower` ou `PowerPlanSwitcher`) ne combine tiers progressifs, sensibilisation aux connexions/agents IA 24/7 et hystérésis sans jamais passer en S3/S0ix. |
| **3. Extinction DDC/CI par moniteur (`0xD6`) + table de quirks EDID + repli veille Windows** | **Build** (ou prototypage CLI avec **ControlMyMonitor** au début) | Aucun outil tray (`Twinkle Tray`, `Monitorian`, `ClickMonitorDDC`) ne gère des timeouts indépendants par écran + protection anti-deep-sleep brick/hot-plug loop pour ASUS ProArt et EIZO CS2740. |
| **4a. Limitation CPU en état *drowsy* (EPP, Boost OFF, `SCHEDPOLICY` E-cores)** | **Build** (en coordination stricte avec Process Lasso) | À condition de désactiver *IdleSaver* dans Process Lasso (ou de passer par un overlay si plan `Balanced`), un basculement fail-closed façon `sytelus/idlepower` apporte le vrai gain énergétique en *drowsy*. |
| **4b. ProBalance, affinités CPU, et EcoQoS permanent par application** | **Reuse existing tool** (**Process Lasso**) | Process Lasso le fait déjà nativement avec persistance (`ProcessLasso.ini`), service système et gestion des conflits. |
| **5a. Module GPU optionnel via NVML (`-pl`, lecture puissance/P-state) + baisse de refresh rate en *drowsy*** | **Build** (module optionnel + service/tâche planifiée élevée) | Comble un vide réel sur RTX 3080 Ti multi-écrans (aucun outil idle ne baisse le refresh rate ou ne cappe le GPU uniquement à l’absence sans UAC). |
| **5b. Télémétrie CPU Package via driver kernel custom (type WinRing0)** | **Skip** | Risque de blocage HVCI/Vulnerable Driver Blocklist sur Windows 11 26H2 ; tenter **EMI** (`IOCTL_EMI_GET_MEASUREMENT`) en lecture passive uniquement, ou s’en passer. |
| **6. Windows Away Mode (`ES_AWAYMODE_REQUIRED`)** | **Skip** | Inadapté aux systèmes Modern Standby (S0ix) et inutile pour une machine qui ne doit jamais déclencher de transition de veille. |
