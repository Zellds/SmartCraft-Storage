#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds the Thunderstore/r2modman zip for this mod.

.DESCRIPTION
    Produces dist/SmartCraftStorage-<version>.zip containing exactly the files a
    package needs — manifest.json, icon.png, README.md, CHANGELOG.md and
    plugins/SmartCraftStorage.dll. Deliberately NOT the rest of the build output:
    bin/Release/net48 also holds assembly_valheim.dll, Jotunn.dll and every
    UnityEngine module, and shipping those would collide with the real ones.

.PARAMETER Version
    Overrides the version in manifest.json. Use this for a local test build, so
    r2modman can tell it apart from whatever is installed from Thunderstore.

.PARAMETER LocalImport
    Adds the "author" field r2modman wants when importing a mod from a file
    rather than from Thunderstore. Harmless to leave on; on by default.

.PARAMETER ValheimInstall
    Path to the Valheim install. Defaults to $env:VALHEIM_INSTALL, and failing
    that the script looks Valheim up in every Steam library on the machine —
    Directory.Build.props only guesses the default C:\ location, which is wrong
    as soon as the game lives in a library on another drive.

.EXAMPLE
    ./scripts/package.ps1
    ./scripts/package.ps1 -Version 0.2.1
#>
[CmdletBinding()]
param(
    [string] $Version,
    [string] $Author = 'Zellds',
    [bool]   $LocalImport = $true,
    [string] $ValheimInstall = $env:VALHEIM_INSTALL,
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# A Valheim install is only the right one if the assemblies we compile against
# are actually in it — a path that merely exists produces 60 unresolved-reference
# warnings and then a compile failure, which is far harder to read than this.
function Test-ValheimInstall([string] $path) {
    return $path -and (Test-Path (Join-Path $path 'valheim_Data/Managed/assembly_valheim.dll'))
}

# Steam records every library folder, on every drive, in libraryfolders.vdf.
function Get-SteamLibraries {
    $steam = $null
    foreach ($key in 'HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam', 'HKLM:\SOFTWARE\Valve\Steam') {
        try {
            $prop = Get-ItemProperty -Path $key -ErrorAction Stop
            foreach ($name in 'SteamPath', 'InstallPath') {
                if ($prop.PSObject.Properties.Name -contains $name -and $prop.$name) {
                    $steam = $prop.$name
                    break
                }
            }
        }
        catch { }
        if ($steam) { break }
    }

    $roots = @()
    if ($steam) { $roots += $steam }
    $roots += 'C:/Program Files (x86)/Steam'

    $libraries = @()
    foreach ($root in $roots) {
        $libraries += $root
        $vdf = Join-Path $root 'steamapps/libraryfolders.vdf'
        if (Test-Path $vdf) {
            foreach ($match in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) {
                $libraries += $match.Groups[1].Value -replace '\\\\', '\'
            }
        }
    }

    return $libraries | Select-Object -Unique
}

function Find-ValheimInstall([string] $explicit) {
    if ($explicit) {
        if (-not (Test-ValheimInstall $explicit)) {
            throw "No Valheim install at '$explicit' — expected valheim_Data/Managed/assembly_valheim.dll under it."
        }
        return $explicit
    }

    foreach ($library in Get-SteamLibraries) {
        $candidate = Join-Path $library 'steamapps/common/Valheim'
        if (Test-ValheimInstall $candidate) { return $candidate }
    }

    throw @'
Could not find your Valheim install.

This build compiles against the game's own assemblies, so it needs to know
where Valheim is. Steam puts it under whichever library folder you chose,
which is often not the default C:\ one.

Pass it explicitly:
    ./scripts/package.ps1 -ValheimInstall "D:\SteamLibrary\steamapps\common\Valheim"

or set it once for all builds (including plain `dotnet build`):
    $env:VALHEIM_INSTALL = "D:\SteamLibrary\steamapps\common\Valheim"

The folder you want is the one containing valheim.exe.
'@
}

$repo = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repo 'manifest.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

if (-not $Version) { $Version = $manifest.version_number }
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version must be major.minor.patch (Thunderstore rejects anything else); got '$Version'."
}

$valheim = Find-ValheimInstall $ValheimInstall
$env:VALHEIM_INSTALL = $valheim
Write-Host "Valheim:  $valheim" -ForegroundColor DarkGray

Write-Host "Building $Configuration..." -ForegroundColor Cyan
dotnet build (Join-Path $repo 'SmartCraftStorage.csproj') -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Build failed (compiling against $valheim). Scroll up for the compiler errors."
}

$dll = Join-Path $repo "bin/$Configuration/net48/SmartCraftStorage.dll"
if (-not (Test-Path $dll)) { throw "Built plugin not found at $dll" }

$stage = Join-Path ([System.IO.Path]::GetTempPath()) "smartcraft-package-$([guid]::NewGuid())"
$dist  = Join-Path $repo 'dist'
$zip   = Join-Path $dist "SmartCraftStorage-$Version.zip"

try {
    New-Item -ItemType Directory -Path (Join-Path $stage 'plugins') -Force | Out-Null

    $manifest.version_number = $Version
    if ($LocalImport -and -not ($manifest.PSObject.Properties.Name -contains 'author')) {
        $manifest | Add-Member -NotePropertyName author -NotePropertyValue $Author
    }
    $manifest | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $stage 'manifest.json') -Encoding utf8

    foreach ($file in 'icon.png', 'README.md', 'CHANGELOG.md') {
        Copy-Item (Join-Path $repo $file) $stage
    }
    Copy-Item $dll (Join-Path $stage 'plugins')

    New-Item -ItemType Directory -Path $dist -Force | Out-Null
    if (Test-Path $zip) { Remove-Item $zip }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
}
finally {
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
}

Write-Host ''
Write-Host "Packaged $zip" -ForegroundColor Green
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $archive.Entries | ForEach-Object { '  {0,-38} {1,8} bytes' -f $_.FullName, $_.Length }
}
finally {
    $archive.Dispose()
}
Write-Host ('  SHA256 {0}' -f (Get-FileHash $zip -Algorithm SHA256).Hash)
