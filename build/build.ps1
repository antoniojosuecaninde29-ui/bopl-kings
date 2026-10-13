# Bopl Kings build script (run on a PC with the games installed and the .NET 6 SDK).
# Builds the plugin against 9 Kings' BepInEx interop assemblies and assembles the release zip.
# Uses only pinned official downloads; ships no game content.
param(
    [string]$GameDir = "",        # the 9 Kings install folder ({game}); BepInEx runs there first
    [string]$Version = "0.1.0"
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$bepinexVendor = Join-Path $root "vendor/bepinex"

# 1. BepInEx 6 IL2CPP be.788 (pinned official build, LGPL-2.1) if not vendored yet
if (-not (Test-Path (Join-Path $bepinexVendor "core"))) {
    New-Item -ItemType Directory -Force -Path $bepinexVendor | Out-Null
    $url = "https://builds.bepinex.dev/projects/BepInEx/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788.zip"
    $zip = Join-Path $bepinexVendor "bepinex.zip"
    Invoke-WebRequest -Uri $url -OutFile $zip
    Expand-Archive $zip -DestinationPath (Join-Path $bepinexVendor "raw") -Force
}

# 2. Build GameCore + Plugin. Plugin references: BepInEx core dlls + {game}/BepInEx/interop.
#    The interop assemblies exist after one vanilla launch with BepInEx in {game}.
$core = Join-Path $bepinexVendor "raw/BepInEx/core"
if ($GameDir -eq "") { Write-Error "pass -GameDir <9 Kings install folder>" }
$interop = Join-Path $GameDir "BepInEx/interop"
if (-not (Test-Path $interop)) {
    Write-Error "no interop assemblies at $interop - launch 9 Kings once with BepInEx installed, then rebuild"
}
dotnet build (Join-Path $root "src/Plugin/BoplKings.csproj") -c Release /p:Refs="$core/" /p:GameInterop="$interop" -o (Join-Path $root "build/out")

# 3. Assemble the release zip (no game content ships): BepInEx + the mod + its data sheets
$stage = Join-Path $root "build/stage"
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Copy-Item -Recurse (Join-Path $bepinexVendor "raw/BepInEx") (Join-Path $stage "BepInEx")
New-Item -ItemType Directory -Force -Path (Join-Path $stage "BepInEx/plugins/BoplKings/data") | Out-Null
Copy-Item (Join-Path $root "build/out/BoplKings.dll") (Join-Path $stage "BepInEx/plugins/BoplKings.dll")
Copy-Item (Join-Path $root "build/out/GameCore.dll") (Join-Path $stage "BepInEx/plugins/BoplKings/GameCore.dll")
Copy-Item -Recurse (Join-Path $root "sheets/*") (Join-Path $stage "BepInEx/plugins/BoplKings/data/")
@'
BepInEx 6 (be.788, IL2CPP, win-x64) - LGPL-2.1 - (c) the BepInEx team - https://github.com/BepInEx/BepInEx
Bundled unmodified so Bopl Kings plays in one click. Source: the pinned official build.
Bopl Kings itself: open for remix. Reads Bopl Battle content from the player's own installed copy.
'@ | Set-Content (Join-Path $stage "CREDITS.txt")
$release = Join-Path $root "build/BoplKings-$Version.zip"
if (Test-Path $release) { Remove-Item $release }
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $release
Write-Host "release: $release"