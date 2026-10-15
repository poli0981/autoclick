<#
.SYNOPSIS
    Fails when any direct or transitive NuGet package has a known vulnerability.
.DESCRIPTION
    `dotnet package list --vulnerable` always exits 0, so its JSON report is parsed instead.
    A vulnerability can be accepted on purpose with <NuGetAuditSuppress Include="advisory URL" />
    in Directory.Build.props (the same item NuGet's restore-time audit honors). Results go to the
    GitHub job summary when running in Actions.
#>
[CmdletBinding()]
param(
    [string] $Solution = 'AutoClick.slnx'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $json = dotnet package list --project $Solution --vulnerable --include-transitive --format json
    if ($LASTEXITCODE -ne 0) { throw "dotnet package list failed (exit code $LASTEXITCODE). Run dotnet restore first." }
    $report = ($json | Out-String) | ConvertFrom-Json

    $suppressed = @()
    $items = dotnet msbuild src/AutoClick.UI/AutoClick.UI.csproj -getItem:NuGetAuditSuppress | Out-String
    if ($LASTEXITCODE -eq 0 -and $items.Trim()) {
        $suppressed = @((ConvertFrom-Json $items).Items.NuGetAuditSuppress | ForEach-Object { $_.Identity })
    }

    $problems = @($report.problems) + @($report.projects | ForEach-Object { $_.problems }) | Where-Object { $_ }
    $findings = foreach ($project in $report.projects) {
        foreach ($framework in $project.frameworks) {
            foreach ($package in @($framework.topLevelPackages) + @($framework.transitivePackages) | Where-Object { $_ }) {
                foreach ($vulnerability in $package.vulnerabilities) {
                    [pscustomobject]@{
                        Project = Split-Path -Leaf $project.path
                        Package = $package.id
                        Version = $package.resolvedVersion
                        Severity = $vulnerability.severity
                        Advisory = $vulnerability.advisoryurl
                        Suppressed = $suppressed -contains $vulnerability.advisoryurl
                    }
                }
            }
        }
    }
    $findings = @($findings)
    $blocking = @($findings | Where-Object { -not $_.Suppressed })

    $summary = [Text.StringBuilder]::new()
    [void]$summary.AppendLine('### NuGet vulnerability audit')
    if ($findings.Count -eq 0) {
        [void]$summary.AppendLine('No known vulnerabilities in direct or transitive packages.')
    }
    else {
        [void]$summary.AppendLine('| Package | Version | Severity | Advisory | Project | Accepted |')
        [void]$summary.AppendLine('|---|---|---|---|---|---|')
        foreach ($f in $findings | Sort-Object Package, Advisory -Unique) {
            [void]$summary.AppendLine("| $($f.Package) | $($f.Version) | $($f.Severity) | $($f.Advisory) | $($f.Project) | $($f.Suppressed) |")
        }
    }
    if ($env:GITHUB_STEP_SUMMARY) { Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summary.ToString() }
    Write-Host $summary.ToString()

    if ($problems.Count -gt 0) {
        $problems | ForEach-Object { Write-Host "::error title=Vulnerability audit::$($_.text ?? $_)" }
        throw 'The vulnerability data could not be read; the audit is incomplete.'
    }

    foreach ($f in $blocking) {
        Write-Host "::error title=Vulnerable package::$($f.Package) $($f.Version) ($($f.Severity)): $($f.Advisory)"
    }
    if ($blocking.Count -gt 0) {
        throw "$($blocking.Count) vulnerable package reference(s). Update them, or accept one deliberately with NuGetAuditSuppress."
    }
}
finally {
    Pop-Location
}
