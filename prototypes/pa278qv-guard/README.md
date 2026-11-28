# PA278QV guard (prototype)

The fix that started PowerTamer. An ASUS PA278QV (EDID `AUS2700`) on DisplayPort hot-plug loops
every ~8.7 s once Windows turns the displays off; each reconnect makes Explorer, DWM and the NVIDIA
container hold memory and handles. Powering the monitor off over DDC/CI before Windows does stops
the loop.

| File | Role |
|---|---|
| `pa278qv-guard.cs` | Hidden WinForms app: on lock, turns the monitor off 10 s before the Windows lock-screen display timeout (read from the active plan); on displays-off, turns it off; on unlock, turns it back on |
| `ddc.ps1` | Helper: lists monitors with their hardware ID and reads or writes a VCP code |

Build with the .NET Framework compiler that ships with Windows (C# 5):

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /out:pa278qv-guard.exe pa278qv-guard.cs
```

Run `pa278qv-guard.exe [hardware-id]` (default `AUS2700`). Log: `%USERPROFILE%\Diag\pa278qv-guard.log`.

This is a single-purpose prototype, frozen and kept for reference; PowerTamer will generalize it
into the monitors module with a per-model quirks table (#16 to #19).

**Do not port its shortcuts.** It was written for one machine. The monitors module must not carry
over:
- the default hardware ID (`AUS2700`): monitors are discovered at runtime, quirks are data;
- the log path under `%USERPROFILE%\Diag\` and the model-named mutex: the core uses
  `%LOCALAPPDATA%\PowerTamer\` and a session-scoped name (#49);
- only the first physical monitor behind a display handle (`pm[0]`), and model substring as
  identity: two identical monitors must be handled separately;
- `Thread.Sleep` and DDC writes inside the window procedure: writes go to a worker;
- volatile ownership (`weTurnedOff`) and no restore on shutdown or crash: every change goes
  through the ownership journal (#36);
- treating a lock-screen display timeout of `0` (never) like an unreadable value.
