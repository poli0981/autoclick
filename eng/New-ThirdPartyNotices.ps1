<#
.SYNOPSIS
    Checks the licenses of everything AutoClick ships and writes THIRD-PARTY-NOTICES.txt.
.DESCRIPTION
    Runs nuget-license on the UI project's package closure (what the installer contains) with
    the policy in eng/licenses:
      allowed-licenses.json  SPDX ids compatible with GPL-3.0 (also used by dependency review)
      url-mappings.json      license URLs of packages without an SPDX expression
      overrides.json         packages that publish no license metadata at all
    Fails when a package has a license outside the allow list or none at all. The notices file
    lists every package with its license and copyright, followed by the full license texts
    (eng/licenses/texts/<SPDX id>.txt).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $OutFile,
    [string] $Project = 'src/AutoClick.UI/AutoClick.UI.csproj'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$policy = Join-Path $PSScriptRoot 'licenses'
$report = Join-Path ([IO.Path]::GetTempPath()) "autoclick-licenses-$PID.json"

dotnet nuget-license --input (Join-Path $root $Project) --include-transitive `
    --allowed-license-types (Join-Path $policy 'allowed-licenses.json') `
    --licenseurl-to-license-mappings (Join-Path $policy 'url-mappings.json') `
    --override-package-information (Join-Path $policy 'overrides.json') `
    --output JsonPretty --file-output $report
$exitCode = $LASTEXITCODE
if (-not (Test-Path -LiteralPath $report)) {
    throw "nuget-license failed (exit code $exitCode) without writing a report."
}

$packages = @(Get-Content -LiteralPath $report -Raw | ConvertFrom-Json | Sort-Object PackageId)
Remove-Item -LiteralPath $report

$violations = @($packages | Where-Object { $_.ValidationErrors })
if ($violations.Count -gt 0 -or $exitCode -ne 0) {
    foreach ($package in $violations) {
        $errors = ($package.ValidationErrors | ForEach-Object { $_.Error }) -join '; '
        Write-Host "::error title=License policy::$($package.PackageId) $($package.PackageVersion): $errors (license: $($package.License); url: $($package.LicenseUrl))"
    }
    throw "$($violations.Count) package(s) violate the license policy (eng/licenses). Add a mapping or override only after checking the actual license."
}

$lines = [Collections.Generic.List[string]]::new()
$lines.Add('AutoClick - third-party notices')
$lines.Add('===============================')
$lines.Add('')
$lines.Add('AutoClick is free software under the GNU General Public License v3.0 (see LICENSE.txt).')
$lines.Add('It includes the components listed below, each under its own license.')
$lines.Add('')
$lines.Add('.NET and Windows Desktop runtime (self-contained in this package)')
$lines.Add('  License: MIT')
$lines.Add('  https://github.com/dotnet/runtime/blob/main/LICENSE.TXT')
$lines.Add('  Notices: https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT')
$lines.Add('           https://github.com/dotnet/wpf/blob/main/THIRD-PARTY-NOTICES.txt')
$lines.Add('')
$lines.Add('Packages')
$lines.Add('--------')
foreach ($package in $packages) {
    $lines.Add('')
    $lines.Add("$($package.PackageId) $($package.PackageVersion)")
    $lines.Add("  License: $($package.License)")
    if ($package.Copyright) { $lines.Add("  Copyright: $($package.Copyright)") }
    if ($package.Authors) { $lines.Add("  Authors: $($package.Authors -join ', ')") }
    if ($package.PackageProjectUrl) { $lines.Add("  Project: $($package.PackageProjectUrl)") }
}

$licenses = $packages | ForEach-Object { $_.License } | Sort-Object -Unique
$licenses = @('MIT') + @($licenses | Where-Object { $_ -ne 'MIT' })
$lines.Add('')
$lines.Add('License texts')
$lines.Add('-------------')
foreach ($license in $licenses) {
    $text = Join-Path $policy "texts/$license.txt"
    if (-not (Test-Path -LiteralPath $text)) {
        throw "No license text for '$license': add eng/licenses/texts/$license.txt (from https://github.com/spdx/license-list-data/tree/main/text)."
    }

    $lines.Add('')
    $lines.Add("=== $license ===")
    $lines.Add('')
    foreach ($line in Get-Content -LiteralPath $text) { $lines.Add($line) }
}

$directory = Split-Path -Parent ([IO.Path]::GetFullPath($OutFile))
New-Item -ItemType Directory -Force -Path $directory | Out-Null
[IO.File]::WriteAllText([IO.Path]::GetFullPath($OutFile), ($lines -join "`r`n") + "`r`n", [Text.UTF8Encoding]::new($false))
Write-Host "Wrote $OutFile ($($packages.Count) packages, licenses: $($licenses -join ', '))."
