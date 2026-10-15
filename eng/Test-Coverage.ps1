<#
.SYNOPSIS
    Fails when line coverage of an assembly drops below its minimum.
.DESCRIPTION
    Reads the merged Cobertura report written by ReportGenerator. The UI and Win32 layers are
    covered by the smoke test and the Win32 tests rather than by a percentage gate.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Cobertura,
    [hashtable] $Minimum = @{ 'AutoClick.Core' = 90; 'AutoClick.Services' = 70 }
)

$ErrorActionPreference = 'Stop'
[xml] $report = Get-Content -LiteralPath $Cobertura -Raw
$failed = $false
foreach ($assembly in $Minimum.Keys | Sort-Object) {
    $package = $report.coverage.packages.package | Where-Object { $_.name -eq $assembly }
    if (-not $package) {
        Write-Host "::error title=Coverage::$assembly is missing from the coverage report."
        $failed = $true
        continue
    }

    $percent = [math]::Round([double]::Parse($package.'line-rate', [Globalization.CultureInfo]::InvariantCulture) * 100, 1)
    if ($percent -lt $Minimum[$assembly]) {
        Write-Host "::error title=Coverage::$assembly line coverage is $percent% (minimum $($Minimum[$assembly])%)."
        $failed = $true
    }
    else {
        Write-Host "$assembly line coverage: $percent% (minimum $($Minimum[$assembly])%)"
    }
}

if ($failed) { throw 'Line coverage is below the minimum.' }
