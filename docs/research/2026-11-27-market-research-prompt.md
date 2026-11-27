Market research task. Search the web thoroughly and write the final report in French (keep product names, API names and technical terms in English). Cite a source URL for every factual claim about a product; say clearly when something could not be verified. Do not modify any files; just print the report.

## Context
I want to build a new open-source Windows 11 power-management utility (separate project, lightweight native exe, likely Rust). Before writing it, I need to know what already exists so I don't reinvent the wheel, and what gaps remain.

Target machine: Windows 11 26H2 desktop, Intel i9-14900K (hybrid P/E cores), NVIDIA RTX 3080 Ti, three monitors (two ASUS ProArt over DisplayPort, one EIZO ColorEdge CS2740 calibrated with ColorNavigator). The PC must stay awake 24/7 because remote AI coding agents run on it and often wait for my input; true sleep (S3/S0ix) is not acceptable because I would lose access to the sessions.

Already in use and must keep working side by side: Process Lasso (Bitsum) — ProBalance, CPU affinities, IdleSaver, profile-based power plans, Performance Mode; and Microsoft PowerToys Awake (to be replaced).

## Envisioned features
1. Built-in keep-awake (replacing PowerToys Awake), with readable reasons (e.g. PowerCreateRequest).
2. Intermediate power states between "awake" and "sleep": a "drowsy" state where the system stays reachable (network, remote sessions) but consumes as little as possible, with fast wake-up when load or input returns. Several tiers (active → away → drowsy), driven by idle time, session lock, input, sustained load, remote connections.
3. Per-monitor handling: turn off individual monitors via DDC/CI (VCP 0xD6) at different timeouts, leave monitors without DDC to Windows standby, per-model quirk rules (e.g. a monitor that hot-plug loops in DisplayPort deep sleep).
4. CPU limiting when idle/away beyond C-states: switch to a "drowsy" power plan or overlay (max processor state, turbo/boost off, EPP, core parking, prefer E-cores), while letting cores wake up when needed. EcoQoS / efficiency mode for chosen background apps.
5. Optional GPU module requiring admin: NVIDIA power limit / clocks / P-state control when idle/away (NVAPI, NVML/nvidia-smi), with a way to avoid repeated UAC prompts (e.g. elevated scheduled task or small service).

## Questions to answer
A. Existing tools, for each: what it does, overlap with features 1-5, license/price, still maintained (last release date), footprint, admin/driver requirements. Cover at least: Process Lasso (IdleSaver, power profiles, Performance Mode), Bitsum ParkControl, PowerToys Awake, Don't Sleep / Caffeine-like tools, Twinkle Tray, Monitorian, ClickMonitorDDC, NirSoft ControlMyMonitor, ThrottleStop, Intel XTU, MSI Afterburner (profile auto-switching), NVIDIA Inspector "Multi Display Power Saver", nvidia-smi / NVML, Windows Away Mode (ES_AWAYMODE_REQUIRED), PowerPlanSwitcher-type tools, any newer projects on GitHub. Also mention Linux equivalents only as design inspiration (TLP, power-profiles-daemon, tuned, auto-cpufreq).
B. Is there any single tool that already combines keep-awake + tiered idle states + per-monitor DDC power + CPU/GPU idle limiting? If so, how close is it?
C. Coexistence with Process Lasso: which of its features would conflict (e.g. both switching the active power plan, IdleSaver, ParkControl), and recommended coexistence strategies (e.g. never switch plans while Lasso manages them, write overlay settings instead, detect Lasso, integrate with its config).
D. Technical feasibility notes on Windows 11: power-plan switching without admin, processor power settings that matter on Intel hybrid CPUs (EPP, heterogeneous policies, core parking), EcoQoS on third-party processes, Away Mode semantics, measuring package/GPU power without a kernel driver (Windows Energy Meter / EMI counters, NVML), DDC/CI pitfalls, and NVIDIA idle P-state behaviour with multiple monitors.
E. Gaps: what the new tool would bring that nothing else does, and which features are better left to existing tools.

End with a short recommendation table: feature → build / reuse existing tool / skip.
