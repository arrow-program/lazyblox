<#
.SYNOPSIS
  Opens a LazyBlox screen, captures only that window (PrintWindow) to a PNG, then closes it.
  For UI review; nothing is installed or launched.
.EXAMPLE
  .\scripts\capture-ui.ps1 -Page home -Out ui-home.png
#>
param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\dist\LazyBlox.exe'),
    [ValidateSet('home', 'browser', 'test', 'message', 'error', 'confirm')][string]$Page = 'home',
    [string]$Out = "ui-$Page.png",
    [string[]]$ExeArgs = @()
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class CapWin {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
}
'@
[void][CapWin]::SetProcessDpiAwarenessContext([IntPtr](-4))
$env:LAZYBLOX_UI_PAGE = $Page
$p = if ($ExeArgs.Count) { Start-Process (Resolve-Path $Exe) -ArgumentList $ExeArgs -PassThru } else { Start-Process (Resolve-Path $Exe) -PassThru }
Remove-Item Env:\LAZYBLOX_UI_PAGE
try {
    $h = [IntPtr]::Zero
    for ($i = 0; $i -lt 50 -and $h -eq [IntPtr]::Zero; $i++) { Start-Sleep -Milliseconds 100; $p.Refresh(); $h = $p.MainWindowHandle }
    if ($h -eq [IntPtr]::Zero) { throw 'window not found' }
    Start-Sleep -Milliseconds 400
    $r = New-Object CapWin+RECT; [void][CapWin]::GetWindowRect($h, [ref]$r)
    $bmp = New-Object Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
    $g = [Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [void][CapWin]::PrintWindow($h, $hdc, 2); $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save((Join-Path (Get-Location) $Out), [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    "saved $Out ($($r.R - $r.L)x$($r.B - $r.T))"
}
finally { if (-not $p.HasExited) { $p.Kill() } }
