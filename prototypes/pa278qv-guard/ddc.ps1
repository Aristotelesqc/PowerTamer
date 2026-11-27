# DDC/CI helper: lists monitors with their hardware ID, reads or writes a VCP code.
# Usage:
#   ddc.ps1                         -> list monitors, read power mode (VCP 0xD6)
#   ddc.ps1 -Match AUS2700 -Set 5   -> set power mode on matching monitor (1=on, 4=standby, 5=off)
param([string]$Match, [byte]$Code = 0xD6, [int]$Set = -1)

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class Ddc {
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
  public struct PHYSICAL_MONITOR { public IntPtr h; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string desc; }
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
  public struct MONITORINFOEX { public int cbSize; public int l,t,r,b; public int wl,wt,wr,wb; public uint flags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string dev; }
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
  public struct DISPLAY_DEVICE { public int cb; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string name;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string str; public uint flags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string id; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string key; }
  public delegate bool EnumProc(IntPtr hMon, IntPtr hdc, IntPtr rect, IntPtr data);
  [DllImport("user32.dll")] public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, EnumProc cb, IntPtr data);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern bool GetMonitorInfo(IntPtr hMon, ref MONITORINFOEX mi);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern bool EnumDisplayDevices(string dev, uint i, ref DISPLAY_DEVICE dd, uint flags);
  [DllImport("dxva2.dll")] public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMon, out uint n);
  [DllImport("dxva2.dll")] public static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMon, uint n, [Out] PHYSICAL_MONITOR[] arr);
  [DllImport("dxva2.dll")] public static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr h, byte code, IntPtr type, out uint cur, out uint max);
  [DllImport("dxva2.dll")] public static extern bool SetVCPFeature(IntPtr h, byte code, uint val);
  [DllImport("dxva2.dll")] public static extern bool DestroyPhysicalMonitors(uint n, PHYSICAL_MONITOR[] arr);
}
'@

$hmons = New-Object System.Collections.Generic.List[IntPtr]
[void][Ddc]::EnumDisplayMonitors([IntPtr]::Zero, [IntPtr]::Zero, { param($h,$d,$r,$x) $hmons.Add($h); $true }, [IntPtr]::Zero)

foreach ($hm in $hmons) {
  $mi = New-Object Ddc+MONITORINFOEX; $mi.cbSize = [Runtime.InteropServices.Marshal]::SizeOf($mi)
  [void][Ddc]::GetMonitorInfo($hm, [ref]$mi)
  $dd = New-Object Ddc+DISPLAY_DEVICE; $dd.cb = [Runtime.InteropServices.Marshal]::SizeOf($dd)
  [void][Ddc]::EnumDisplayDevices($mi.dev, 0, [ref]$dd, 1)   # flag 1 = device interface path (contains hardware ID)
  $n = 0; [void][Ddc]::GetNumberOfPhysicalMonitorsFromHMONITOR($hm, [ref]$n)
  if ($n -eq 0) { continue }
  $pm = New-Object Ddc+PHYSICAL_MONITOR[] $n
  [void][Ddc]::GetPhysicalMonitorsFromHMONITOR($hm, $n, $pm)
  $cur = 0; $max = 0
  $ok = [Ddc]::GetVCPFeatureAndVCPFeatureReply($pm[0].h, $Code, [IntPtr]::Zero, [ref]$cur, [ref]$max)
  $line = "{0}  {1}  VCP 0x{2:X2}: {3}" -f $mi.dev, $dd.id, $Code, $(if ($ok) { "$cur (max $max)" } else { 'no reply' })
  if ($Match -and $dd.id -match $Match -and $Set -ge 0) {
    $w = [Ddc]::SetVCPFeature($pm[0].h, $Code, [uint32]$Set)
    $line += "  -> set $Set : $(if ($w) {'ok'} else {'failed'})"
  }
  $line
  [void][Ddc]::DestroyPhysicalMonitors($n, $pm)
}
