<#
.SYNOPSIS
  Builds, tests and publishes LazyBlox.exe (Windows x64).
.PARAMETER Aot
  Publish with Native AOT (fastest startup). Requires Visual Studio Build Tools with the
  "Desktop development with C++" workload. Without it, a self-contained single-file
  ReadyToRun build is produced instead.
.PARAMETER SkipTests
  Skip unit / integration tests.
#>
param([switch]$Aot, [switch]$SkipTests)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not $SkipTests) {
    dotnet test tests/LazyBlox.Tests -c Release
    if ($LASTEXITCODE) { throw "unit tests failed" }
    dotnet test tests/LazyBlox.IntegrationTests -c Release
    if ($LASTEXITCODE) { throw "integration tests failed" }
}

$out = Join-Path $PSScriptRoot 'dist'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
$props = @('-c', 'Release', '-r', 'win-x64', '-o', $out)
if ($Aot) { $props += '-p:LazyBloxAot=true' }
dotnet publish src/LazyBlox/LazyBlox.csproj @props
if ($LASTEXITCODE) { throw "publish failed" }

Get-ChildItem $out -Filter *.pdb | Remove-Item
Get-ChildItem $out | Select-Object Name, @{n='MB'; e={[math]::Round($_.Length / 1MB, 2)}}
