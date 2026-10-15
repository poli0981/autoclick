<#
.SYNOPSIS
    Runs the published app with --smoke-test and fails if it does not exit 0 in time.
.DESCRIPTION
    --smoke-test builds the DI graph and constructs every window (XAML parsing) without
    showing UI, logs the result, and exits. On failure the newest AutoClick log is printed.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Exe,
    [int] $TimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'

$exePath = (Resolve-Path -LiteralPath $Exe).Path
$process = Start-Process -FilePath $exePath -ArgumentList '--smoke-test' -PassThru
if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    $process.Kill($true)
    $code = 'timeout'
} else {
    $code = $process.ExitCode
}

$logDir = Join-Path $env:LOCALAPPDATA 'AutoClick\logs'
$log = Get-ChildItem -LiteralPath $logDir -Filter '*.log' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime | Select-Object -Last 1

if ($code -ne 0) {
    if ($log) {
        Write-Host "--- $($log.FullName) ---"
        Get-Content -LiteralPath $log.FullName -Tail 80
    }
    Write-Error "Smoke test failed (exit code: $code)."
    exit 1
}

Write-Host "Smoke test passed ($exePath)."
