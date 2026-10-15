<#
.SYNOPSIS
    Prints the product version and checks that the vpk CLI matches the Velopack library.
.DESCRIPTION
    Velopack recommends packaging with the same vpk version as the referenced library.
    Writes 'version=<x.y.z>' to $env:GITHUB_OUTPUT when running in GitHub Actions.
#>
[CmdletBinding()]
param(
    [string] $Project = (Join-Path $PSScriptRoot '..' 'src' 'AutoClick.UI' 'AutoClick.UI.csproj'),
    [string] $ExpectedVersion
)

$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '..'

$raw = dotnet msbuild $Project -getProperty:Version -getItem:PackageReference -getItem:PackageVersion
if ($LASTEXITCODE -ne 0) { throw "msbuild evaluation failed: $raw" }
$info = $raw | ConvertFrom-Json

$version = $info.Properties.Version
$velopack = ($info.Items.PackageVersion | Where-Object Identity -eq 'Velopack' | Select-Object -First 1).Version
if (-not $velopack) {
    $velopack = ($info.Items.PackageReference | Where-Object Identity -eq 'Velopack' | Select-Object -First 1).Version
}
$vpk = (Get-Content -LiteralPath (Join-Path $root '.config' 'dotnet-tools.json') -Raw | ConvertFrom-Json).tools.vpk.version

Write-Host "Version: $version | Velopack: $velopack | vpk: $vpk"

if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Project version '$version' is not X.Y.Z." }
if ($velopack -ne $vpk) { throw "vpk $vpk must match the Velopack library version $velopack (.config/dotnet-tools.json)." }
if ($ExpectedVersion -and $ExpectedVersion -ne $version) {
    throw "Tag version $ExpectedVersion does not match the project version $version."
}

if ($env:GITHUB_OUTPUT) { "version=$version" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8 }
