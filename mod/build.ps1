# PZA's SlowMo for Meitou: build the code mod and stage its install folder. Author: PZA. Licence: GPLv3 or later.
#
#   .\build.ps1            build Release, stage out\PZASlowMo\ (PZASlowMo.Meitou.dll, meitou.json, PZASlowMo.mod, lang\)
#   .\build.ps1 -Check     also run tools\check on the staged folder: Meitou 0.20.1's own code-mod checks, the rules (SlowCore), the language
#                          files, the four patches applied to the real MeitouClient.dll, and the window built and clicked through Meitou's UiHost.
#                          On a copy of the .NET 11 runtime inside the Meitou release; it never starts the game.
#   -MeitouDir <dir>       the unpacked Meitou release (default: MeitouDir in mod\local.props, git-ignored)
#   -PluginDir <dir>       the RE_Kenshi plugin's repo (default ..\..\PZASlowMo): its PZASlowMo.mod and lang\*.txt are shared, so one
#                          folder serves both games
#
# It installs nothing (README "Install"). out\ is git-ignored.
param(
    [switch]$Check,
    [string]$MeitouDir = "",
    [string]$PluginDir = "",
    [string]$KenshiDir = "C:\Program Files (x86)\Steam\steamapps\common\Kenshi"
)
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$localProps = Join-Path $here "local.props"
if ($MeitouDir -eq "" -and (Test-Path $localProps)) { $MeitouDir = ([xml](Get-Content $localProps -Raw)).Project.PropertyGroup.MeitouDir }
if (-not $MeitouDir) { throw "Set -MeitouDir <unpacked Meitou release> or create mod\local.props (see the comment in the csproj)" }
if ($PluginDir -eq "") { $PluginDir = Join-Path $here "..\..\PZASlowMo" }
$MeitouDir = [System.IO.Path]::GetFullPath($MeitouDir)
$PluginDir = [System.IO.Path]::GetFullPath($PluginDir)
if (-not (Test-Path (Join-Path $MeitouDir "MeitouClient.dll"))) { throw "MeitouClient.dll not found in $MeitouDir" }
if (-not (Test-Path (Join-Path $PluginDir "dist\PZASlowMo.mod"))) { throw "the plugin's dist\PZASlowMo.mod not found in $PluginDir" }

& dotnet build (Join-Path $here "PZASlowMo.Meitou.csproj") -c Release -nologo "-p:MeitouDir=$MeitouDir"
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)" }

$bin = Get-ChildItem (Join-Path $here "bin\Release") -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$stage = Join-Path $here "out\PZASlowMo"
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force (Join-Path $stage "lang") | Out-Null
Copy-Item (Join-Path $bin.FullName "PZASlowMo.Meitou.dll") $stage -Force
Copy-Item (Join-Path $here "meitou.json") $stage -Force
# shared with the RE_Kenshi plugin: the data mod Meitou lists the folder by, and the language files (their keys this build uses)
Copy-Item (Join-Path $PluginDir "dist\PZASlowMo.mod") $stage -Force
Copy-Item (Join-Path $PluginDir "dist\lang\*.txt") (Join-Path $stage "lang") -Force
Copy-Item (Join-Path $here "lang\meitou.*.txt") (Join-Path $stage "lang") -Force

Write-Host "staged $stage :"
Get-ChildItem $stage -Recurse -File | ForEach-Object { Write-Host ("  {0,-28} {1,8} bytes" -f $_.FullName.Substring($stage.Length + 1), $_.Length) }

if ($Check) {
    $rt = Join-Path $here "out\checkrt"
    New-Item -ItemType Directory -Force $rt | Out-Null
    $patterns = "System*.dll", "Microsoft.*.dll", "mscorlib.dll", "netstandard.dll", "WindowsBase.dll", "coreclr.dll", "clrjit.dll",
                "clrgc.dll", "hostfxr.dll", "hostpolicy.dll", "Meitou*.dll", "0Harmony.dll", "Bepu*.dll", "DotRecast*.dll"
    foreach ($p in $patterns) { Copy-Item (Join-Path $MeitouDir $p) $rt -Force }
    $stagedDll = Join-Path $stage "PZASlowMo.Meitou.dll"
    & dotnet build (Join-Path $here "tools\check\check.csproj") -c Release -nologo "-p:MeitouDir=$MeitouDir" "-p:ModDll=$stagedDll" -o (Join-Path $here "out\checkbin")
    if ($LASTEXITCODE -ne 0) { throw "check tool build failed ($LASTEXITCODE)" }
    Copy-Item (Join-Path $here "out\checkbin\check.dll") $rt -Force
    Copy-Item $stagedDll $rt -Force
    $cfg = '{ "runtimeOptions": { "tfm": "net11.0", "includedFrameworks": [ { "name": "Microsoft.NETCore.App", "version": "11.0.0" } ] } }'
    Set-Content -Path (Join-Path $rt "check.runtimeconfig.json") -Value $cfg -Encoding ascii
    Push-Location $rt
    try { & dotnet exec --runtimeconfig check.runtimeconfig.json check.dll $stage $KenshiDir } finally { Pop-Location }
    if ($LASTEXITCODE -ne 0) { throw "check FAILED ($LASTEXITCODE)" }
}
