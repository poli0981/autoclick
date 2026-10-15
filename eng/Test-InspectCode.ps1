<#
.SYNOPSIS
    Dead-code gate over a ReSharper InspectCode SARIF report.
.DESCRIPTION
    Fails on unused code (locals, parameters, private and public members, types), unused
    using directives and XAML bindings whose property does not exist. Everything else in the
    report (style suggestions, "can be private", naming) is summarized but does not fail the
    build. A deliberate exception is marked in code with "// ReSharper disable once <rule>".
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Sarif
)

$ErrorActionPreference = 'Stop'

$blocking = @(
    '\.Local$'
    '^UnusedType\.'
    '^UnusedMember\.'
    '^UnusedVariable'
    '^UnusedParameter'
    '^NotAccessedVariable'
    '^RedundantUsingDirective'
    '^Xaml\.BindingWithContextNotResolved$'
    '^Xaml\.PossibleNullReferenceException$'
)

$report = Get-Content -LiteralPath $Sarif -Raw | ConvertFrom-Json
$results = @($report.runs | ForEach-Object { $_.results } | Where-Object { $_ })

$failures = @($results | Where-Object { $rule = $_.ruleId; $blocking | Where-Object { $rule -match $_ } })
foreach ($result in $failures) {
    $location = $result.locations[0].physicalLocation
    $file = $location.artifactLocation.uri
    $line = $location.region.startLine
    Write-Host "::error file=$file,line=$line,title=$($result.ruleId)::$($result.message.text)"
}

$summary = [Text.StringBuilder]::new()
[void]$summary.AppendLine('### InspectCode')
[void]$summary.AppendLine("$($results.Count) finding(s), $($failures.Count) blocking.")
[void]$summary.AppendLine()
[void]$summary.AppendLine('| Rule | Count | Blocking |')
[void]$summary.AppendLine('|---|---:|---|')
foreach ($group in $results | Group-Object ruleId | Sort-Object Count -Descending) {
    $isBlocking = [bool]($blocking | Where-Object { $group.Name -match $_ })
    [void]$summary.AppendLine("| $($group.Name) | $($group.Count) | $(if ($isBlocking) { 'yes' } else { '' }) |")
}
if ($env:GITHUB_STEP_SUMMARY) { Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summary.ToString() }
Write-Host $summary.ToString()

if ($failures.Count -gt 0) {
    throw "$($failures.Count) unused-code finding(s). Remove the code, or mark a deliberate exception with '// ReSharper disable once <rule>'."
}
