<#
.SYNOPSIS
    Extracts the CHANGELOG.md section for one version into a Markdown file for Velopack.
.DESCRIPTION
    Fails (exit 1) when the section is missing so a release can never ship with
    placeholder notes. Used by both the CI dry run and the Release workflow.
.EXAMPLE
    ./eng/Get-ReleaseNotes.ps1 -Version 1.3.4 -OutFile release_notes.md
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $OutFile,
    [string] $Changelog = (Join-Path $PSScriptRoot '..' 'CHANGELOG.md')
)

$ErrorActionPreference = 'Stop'

$text = Get-Content -LiteralPath $Changelog -Raw
$escaped = [regex]::Escape($Version)
$match = [regex]::Match($text, "(?ms)^## \[$escaped\][^\n]*\r?\n(.*?)(?=^## \[|^---\s*$|\z)")
if (-not $match.Success -or [string]::IsNullOrWhiteSpace($match.Groups[1].Value)) {
    Write-Error "CHANGELOG.md has no '## [$Version]' section with content."
    exit 1
}

$notes = $match.Groups[1].Value.Trim()
Set-Content -LiteralPath $OutFile -Value $notes -Encoding utf8NoBOM -NoNewline
Write-Host "Release notes for $Version ($($notes.Length) chars) written to $OutFile"
