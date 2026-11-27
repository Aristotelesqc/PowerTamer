// PA278QV guard: the ASUS PA278QV (EDID AUS2700) hot-plug loops every ~8.7 s once Windows turns
// the displays off, which leaks memory in Explorer/DWM. Powering it off over DDC/CI stops the loop.
// - session locked      -> monitor off (VCP 0xD6 = 5) 10 s before Windows turns the displays off.
//                          The delay is read from the active power plan at each lock (lock-screen display
//                          timeout, else the normal display timeout), so it follows Windows settings.
// - displays turned off -> monitor off (Windows display-state notification, no timer)
// - session unlocked, or displays back on while unlocked -> monitor on (VCP 0xD6 = 1), only if we turned it off
// Usage:  pa278qv-guard.exe [hardware-id]   (default AUS2700)   Log: %USERPROFILE%\Diag\pa278qv-guard.log
// Build:  C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /out:pa278qv-guard.exe pa278qv-guard.cs
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class MonitorGuard : Form {
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
  struct PHYSICAL_MONITOR { public IntPtr h; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string desc; }
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
  struct MONITORINFOEX { public int cbSize; public int l,t,r,b; public int wl,wt,wr,wb; public uint flags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string dev; }
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
  struct DISPLAY_DEVICE { public int cb; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string name;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string str; public uint flags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string id; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string key; }
  [StructLayout(LayoutKind.Sequential)]
  struct SYSTEM_POWER_STATUS { public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag; public int BatteryLifeTime, BatteryFullLifeTime; }
  delegate bool EnumProc(IntPtr hMon, IntPtr hdc, IntPtr rect, IntPtr data);

  [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, EnumProc cb, IntPtr data);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr hMon, ref MONITORINFOEX mi);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern bool EnumDisplayDevices(string dev, uint i, ref DISPLAY_DEVICE dd, uint flags);
  [DllImport("dxva2.dll")] static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMon, out uint n);
  [DllImport("dxva2.dll")] static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMon, uint n, [Out] PHYSICAL_MONITOR[] arr);
  [DllImport("dxva2.dll")] static extern bool SetVCPFeature(IntPtr h, byte code, uint val);
  [DllImport("dxva2.dll")] static extern bool DestroyPhysicalMonitors(uint n, PHYSICAL_MONITOR[] arr);
  [DllImport("wtsapi32.dll")] static extern bool WTSRegisterSessionNotification(IntPtr hWnd, int flags);
  [DllImport("user32.dll")] static extern IntPtr RegisterPowerSettingNotification(IntPtr hWnd, ref Guid g, int flags);
  [DllImport("powrprof.dll")] static extern uint PowerGetActiveScheme(IntPtr root, out IntPtr scheme);
  [DllImport("powrprof.dll")] static extern uint PowerReadACValueIndex(IntPtr root, IntPtr scheme, ref Guid sub, ref Guid setting, out uint val);
  [DllImport("powrprof.dll")] static extern uint PowerReadDCValueIndex(IntPtr root, IntPtr scheme, ref Guid sub, ref Guid setting, out uint val);
  [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr p);
  [DllImport("kernel32.dll")] static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS s);

  const int WM_WTSSESSION_CHANGE = 0x02B1, WTS_SESSION_LOCK = 7, WTS_SESSION_UNLOCK = 8;
  const int WM_POWERBROADCAST = 0x0218, PBT_POWERSETTINGCHANGE = 0x8013;
  static Guid GUID_CONSOLE_DISPLAY_STATE = new Guid("6fe69556-704a-47a0-8f24-c28d936fda47");
  static Guid SUB_VIDEO = new Guid("7516b95f-f776-4464-8c53-06167f40cc99");
  static Guid VIDEOCONLOCK = new Guid("8ec4b3a5-6868-48c2-be75-4f3044be88a7");  // display off on lock screen
  static Guid VIDEOIDLE = new Guid("3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e");     // display off after

  string match, logPath;
  Timer lockTimer = new Timer();
  bool locked = false, weTurnedOff = false;

  [STAThread]
  static void Main(string[] args) {
    string match = args.Length > 0 ? args[0] : "AUS2700";
    string log = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Diag"), "pa278qv-guard.log");
    bool created;
    using (var mutex = new System.Threading.Mutex(true, @"Local\PA278QV-Guard", out created)) {
      if (!created) return;   // already running
      Application.Run(new MonitorGuard(match, log));
    }
  }

  public MonitorGuard(string match, string logPath) {
    this.match = match; this.logPath = logPath;
    try { Directory.CreateDirectory(Path.GetDirectoryName(logPath)); } catch { }
    ShowInTaskbar = false; FormBorderStyle = FormBorderStyle.FixedToolWindow; Opacity = 0;
    lockTimer.Tick += (s, e) => { lockTimer.Stop(); if (locked) Off("locked, before Windows display timeout"); };
  }

  protected override void SetVisibleCore(bool value) {
    if (!IsHandleCreated) { CreateHandle(); value = false; }
    base.SetVisibleCore(value);
  }

  protected override void OnHandleCreated(EventArgs e) {
    base.OnHandleCreated(e);
    WTSRegisterSessionNotification(Handle, 0);
    RegisterPowerSettingNotification(Handle, ref GUID_CONSOLE_DISPLAY_STATE, 0);
    Log("start, monitor " + match);
  }

  protected override void WndProc(ref Message m) {
    if (m.Msg == WM_WTSSESSION_CHANGE) {
      int code = m.WParam.ToInt32();
      if (code == WTS_SESSION_LOCK) { locked = true; StartLockTimer(); }
      else if (code == WTS_SESSION_UNLOCK) { locked = false; lockTimer.Stop(); Log("unlock"); On("unlock"); }
    } else if (m.Msg == WM_POWERBROADCAST && m.WParam.ToInt32() == PBT_POWERSETTINGCHANGE && m.LParam != IntPtr.Zero) {
      // POWERBROADCAST_SETTING: Guid(16) + DataLength(4) + Data
      var g = (Guid)Marshal.PtrToStructure(m.LParam, typeof(Guid));
      if (g == GUID_CONSOLE_DISPLAY_STATE) {
        int state = Marshal.ReadInt32(m.LParam, 20);
        if (state == 0) { Log("displays off"); Off("displays off"); }
        else if (state == 1) { Log("displays on"); if (!locked) On("displays on"); }
      }
    }
    base.WndProc(ref m);
  }

  // On lock: turn the monitor off 10 s before Windows turns the displays off (half the timeout if it is short).
  void StartLockTimer() {
    lockTimer.Stop();
    int secs = ReadVideoTimeout(VIDEOCONLOCK);
    string src = "lock-screen display timeout";
    if (secs <= 0) { secs = ReadVideoTimeout(VIDEOIDLE); src = "display timeout"; }
    if (secs < 0) { Log("lock: display timeouts unreadable, waiting for the displays-off notification"); return; }
    if (secs == 0) { Log("lock: " + src + " = never, nothing to do"); return; }
    int delay = secs > 20 ? secs - 10 : Math.Max(1, secs / 2);
    Log("lock: " + src + " = " + secs + " s, monitor off in " + delay + " s");
    lockTimer.Interval = delay * 1000;
    lockTimer.Start();
  }

  // Reads a display timeout (seconds) from the active power plan, AC or DC as currently in use; -1 if unreadable.
  static int ReadVideoTimeout(Guid setting) {
    IntPtr scheme;
    if (PowerGetActiveScheme(IntPtr.Zero, out scheme) != 0) return -1;
    try {
      SYSTEM_POWER_STATUS ps; bool onBattery = GetSystemPowerStatus(out ps) && ps.ACLineStatus == 0;
      uint val; Guid sub = SUB_VIDEO;
      uint rc = onBattery ? PowerReadDCValueIndex(IntPtr.Zero, scheme, ref sub, ref setting, out val)
                          : PowerReadACValueIndex(IntPtr.Zero, scheme, ref sub, ref setting, out val);
      return rc == 0 ? (int)val : -1;
    } finally { LocalFree(scheme); }
  }

  void Off(string why) { if (weTurnedOff) return; if (SetPower(5)) { weTurnedOff = true; Log("monitor off (" + why + ")"); } }
  void On(string why) { if (!weTurnedOff) return; if (SetPower(1)) { weTurnedOff = false; Log("monitor on (" + why + ")"); } }

  // True if an attached monitor device on this display output carries the hardware ID.
  // Not filtered on DISPLAY_DEVICE_ACTIVE: a monitor powered off over DDC may no longer be flagged active,
  // and it must still be found to be turned back on.
  bool OutputHasMonitor(string dev) {
    for (uint i = 0; i < 16; i++) {
      var dd = new DISPLAY_DEVICE(); dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
      if (!EnumDisplayDevices(dev, i, ref dd, 1)) break;   // flag 1 = device interface path (contains hardware ID)
      if (dd.id != null && dd.id.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0) return true;
    }
    return false;
  }

  bool SetPower(uint val) {
    for (int attempt = 1; attempt <= 3; attempt++) {
      string info = "monitor not found";
      var list = new List<IntPtr>();
      EnumProc cb = (h, d, r, x) => { list.Add(h); return true; };
      EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, cb, IntPtr.Zero);
      GC.KeepAlive(cb);
      foreach (var hm in list) {
        var mi = new MONITORINFOEX(); mi.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
        if (!GetMonitorInfo(hm, ref mi) || !OutputHasMonitor(mi.dev)) continue;
        uint n;
        if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hm, out n) || n == 0) { info = "no physical monitor handle"; continue; }
        var pm = new PHYSICAL_MONITOR[n];
        if (!GetPhysicalMonitorsFromHMONITOR(hm, n, pm)) { info = "no physical monitor handle"; continue; }
        bool ok = SetVCPFeature(pm[0].h, 0xD6, val);
        DestroyPhysicalMonitors(n, pm);
        if (ok) return true;
        info = "SetVCPFeature failed";
      }
      Log("set power " + val + " attempt " + attempt + ": " + info);
      System.Threading.Thread.Sleep(500);
    }
    return false;
  }

  void Log(string msg) {
    try { File.AppendAllText(logPath, DateTime.Now.ToString("s") + " " + msg + Environment.NewLine); } catch { }
  }
}
