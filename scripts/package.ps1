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
    Path to the Valheim install. Defaults to $env:VALHEIM_INSTALL, then to the
    location Directory.Build.props already guesses.

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

$repo = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repo 'manifest.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

if (-not $Version) { $Version = $manifest.version_number }
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version must be major.minor.patch (Thunderstore rejects anything else); got '$Version'."
}

if ($ValheimInstall) { $env:VALHEIM_INSTALL = $ValheimInstall }

Write-Host "Building $Configuration..." -ForegroundColor Cyan
dotnet build (Join-Path $repo 'SmartCraftStorage.csproj') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

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
