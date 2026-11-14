<#
.SYNOPSIS
  Measures LazyBlox launcher-side latency without launching Roblox or a browser.
  For each run, starts "LazyBlox.exe dry-run <url>" as a fresh process and records:
    - internal : ms from OS process creation until the launch request would be issued (reported by the exe)
    - wall     : ms from Process.Start to process exit, measured from outside
  Plus the in-process routing micro benchmark ("bench").
.EXAMPLE
  .\scripts\benchmark.ps1 -Exe .\dist\LazyBlox.exe -Runs 30
#>
param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\dist\LazyBlox.exe'),
    [int]$Runs = 30,
    [string]$Url = 'https://www.roblox.com/share?code=0123456789abcdef0123456789abcdef&type=Server'
)
$ErrorActionPreference = 'Stop'
$Exe = (Resolve-Path $Exe).Path

function Measure-Case([string]$label, [string]$url) {
    $internal = @(); $wall = @()
    # one warm-up run (disk cache), not counted
    & $Exe dry-run $url | Out-Null
    for ($i = 0; $i -lt $Runs; $i++) {
        # (Windows PowerShell 5.1 has no ArgumentList; test URLs contain no quotes)
        $psi = New-Object Diagnostics.ProcessStartInfo $Exe, ("dry-run `"" + $url + "`"")
        $psi.UseShellExecute = $false; $psi.RedirectStandardOutput = $true; $psi.CreateNoWindow = $true
        $sw = [Diagnostics.Stopwatch]::StartNew()
        $p = [Diagnostics.Process]::Start($psi)
        $text = $p.StandardOutput.ReadToEnd(); $p.WaitForExit(); $sw.Stop()
        if ($text -match 'elapsed since process creation: ([\d.]+) ms') { $internal += [double]$Matches[1] }
        $wall += $sw.Elapsed.TotalMilliseconds
    }
    function Stat($xs) {
        $s = $xs | Sort-Object
        [pscustomobject]@{ median = [math]::Round($s[[int]($s.Count / 2)], 1); min = [math]::Round($s[0], 1); max = [math]::Round($s[-1], 1) }
    }
    $a = Stat $internal; $b = Stat $wall
    [pscustomobject]@{
        case = $label; runs = $Runs
        'internal median ms' = $a.median; 'internal min' = $a.min; 'internal max' = $a.max
        'wall median ms' = $b.median; 'wall min' = $b.min; 'wall max' = $b.max
    }
}

Write-Host "exe: $Exe ($([math]::Round((Get-Item $Exe).Length / 1MB, 2)) MB)"

# Isolated settings (like an installed setup with a configured browser); the real config is not touched.
$env:LAZYBLOX_HOME = Join-Path ([IO.Path]::GetTempPath()) ("lazyblox-bench-" + [guid]::NewGuid().ToString('N'))
& $Exe set-browser MSEdgeHTM | Out-Null
@(
    Measure-Case 'roblox share link' $Url
    Measure-Case 'normal https (browser forward)' 'https://example.com/path?q=1'
) | Format-Table -AutoSize

Write-Host "In-process routing micro benchmark:"
& $Exe bench $Url 200000 | Out-String | Write-Host  # piping makes PowerShell wait for the GUI-subsystem exe

Remove-Item $env:LAZYBLOX_HOME -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item Env:\LAZYBLOX_HOME
