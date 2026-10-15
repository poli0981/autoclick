<#
.SYNOPSIS
    Runs the published app with --smoke-test and fails if it does not exit 0 in time.
.DESCRIPTION
    --smoke-test builds the DI graph, opens every page in every theme and language, the
    dialogs and a heatmap overlay, and exits 1 on any exception or data-binding error. It uses
    %TEMP%\AutoClick-smoke as its data folder; on failure the newest log from there is printed.
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

$logDir = Join-Path ([IO.Path]::GetTempPath()) 'AutoClick-smoke\logs'
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
