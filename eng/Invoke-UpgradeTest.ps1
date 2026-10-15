<#
.SYNOPSIS
    Tests updating an installed older release to a new release, inside Windows Sandbox.
.DESCRIPTION
    Installs the old release with its Setup.exe, adds 1.x user data (tests/Fixtures/v1), applies
    the new full package with the installed Update.exe (the path the in-app updater takes), starts
    the new version once (settings migration), runs its smoke test, uninstalls, and installs the
    new Setup.exe on a clean profile (eng/upgrade-test/run-upgrade-test.ps1). Nothing is installed
    on this machine. Needs the Windows Sandbox feature; a run takes about two minutes.
    Prints one line per check and exits 1 if any fails. Logs and result.json stay in the run folder.
.EXAMPLE
    gh release download v1.3.4 -p AutoClick-win-Setup.exe -D artifacts/upgrade-test/old
    gh release download v2.0.1 -p 'AutoClick-*-full.nupkg' -p AutoClick-win-Setup.exe -D artifacts/upgrade-test/new
    ./eng/Invoke-UpgradeTest.ps1 -OldSetup artifacts/upgrade-test/old/AutoClick-win-Setup.exe -NewDir artifacts/upgrade-test/new
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $OldSetup,
    [Parameter(Mandatory)] [string] $NewDir,
    [string] $OutDir = (Join-Path $PSScriptRoot '..' 'artifacts' 'upgrade-test' 'runs'),
    [int] $TimeoutMinutes = 15
)

$ErrorActionPreference = 'Stop'

$sandboxExe = Join-Path $env:WINDIR 'System32\WindowsSandbox.exe'
if (-not (Test-Path -LiteralPath $sandboxExe)) {
    throw 'Windows Sandbox is not installed: enable the optional feature Containers-DisposableClientVM.'
}
$sandboxProcesses = 'WindowsSandboxRemoteSession', 'WindowsSandboxClient'
if (Get-Process $sandboxProcesses -ErrorAction SilentlyContinue) {
    throw 'Windows Sandbox is already running; only one instance can run at a time.'
}

$oldSetupPath = (Resolve-Path -LiteralPath $OldSetup).Path
$newDirPath = (Resolve-Path -LiteralPath $NewDir).Path
$package = Get-ChildItem -LiteralPath $newDirPath -Filter 'AutoClick-*-full.nupkg' | Select-Object -First 1
$newSetup = Join-Path $newDirPath 'AutoClick-win-Setup.exe'
if (-not $package -or -not (Test-Path -LiteralPath $newSetup)) {
    throw "$newDirPath must contain AutoClick-X.Y.Z-full.nupkg and AutoClick-win-Setup.exe."
}
$version = $package.Name -replace '^AutoClick-(.+)-full\.nupkg$', '$1'
$expectedFileVersion = ($version -split '-')[0] + '.0'

# Everything the sandbox sees lives in one run folder, mapped to C:\test.
$fixtures = Join-Path $PSScriptRoot '..' 'tests' 'Fixtures' 'v1'
$work = Join-Path ([IO.Path]::GetFullPath($OutDir)) (Get-Date -Format 'yyyyMMdd-HHmmss')
foreach ($dir in 'old', 'new', 'fixtures\profiles', 'results') {
    New-Item -ItemType Directory -Force (Join-Path $work $dir) | Out-Null
}
Copy-Item -LiteralPath $oldSetupPath (Join-Path $work 'old')
Copy-Item -LiteralPath $package.FullName, $newSetup (Join-Path $work 'new')
Copy-Item (Join-Path $fixtures 'settings.json') (Join-Path $work 'fixtures')
Copy-Item (Join-Path $fixtures 'profiles\*.json') (Join-Path $work 'fixtures\profiles')
Copy-Item (Join-Path $PSScriptRoot 'upgrade-test\run-upgrade-test.ps1') $work

$wsb = Join-Path $work 'upgrade-test.wsb'
@"
<Configuration>
  <VGpu>Disable</VGpu>
  <Networking>Disable</Networking>
  <ClipboardRedirection>Disable</ClipboardRedirection>
  <PrinterRedirection>Disable</PrinterRedirection>
  <MappedFolders>
    <MappedFolder>
      <HostFolder>$work</HostFolder>
      <SandboxFolder>C:\test</SandboxFolder>
      <ReadOnly>false</ReadOnly>
    </MappedFolder>
  </MappedFolders>
  <LogonCommand>
    <Command>powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\test\run-upgrade-test.ps1</Command>
  </LogonCommand>
</Configuration>
"@ | Set-Content -LiteralPath $wsb -Encoding utf8

Write-Host "Windows Sandbox: $(Split-Path $oldSetupPath -Leaf) (old) -> $version, run folder $work"
Start-Process -FilePath $sandboxExe -ArgumentList "`"$wsb`""

$resultFile = Join-Path $work 'results\result.json'
$deadline = (Get-Date).AddMinutes($TimeoutMinutes)
while (-not (Test-Path -LiteralPath $resultFile) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 5 }
if (-not (Test-Path -LiteralPath $resultFile)) {
    throw "No result after $TimeoutMinutes minutes; see $work\results\transcript.txt."
}
# The sandbox shuts itself down after writing the result.
$closeBy = (Get-Date).AddMinutes(2)
while ((Get-Process $sandboxProcesses -ErrorAction SilentlyContinue) -and (Get-Date) -lt $closeBy) { Start-Sleep -Seconds 3 }

$r = Get-Content -LiteralPath $resultFile -Raw | ConvertFrom-Json
$v1 = Get-Content -LiteralPath (Join-Path $fixtures 'settings.json') -Raw | ConvertFrom-Json
$warnings = @($r.firstRunWarnings | Where-Object { $_ })
$checks = [ordered]@{
    'Old release installs'                              = $r.oldSetupExit -eq 0 -and [bool]$r.oldVersion
    "Installed Update.exe applies $version"            = $r.applyExit -eq 0 -and $r.newVersion -eq $expectedFileVersion
    'New version starts and migrates the 1.x settings'  = $r.firstRunAlive -and $r.settingsV2Exists
    'Hotkeys and language carried over'                 = $r.migratedHotkeys -eq "$($v1.hotkeys.pauseResume) / $($v1.hotkeys.stopAll) / $($v1.hotkeys.startAll)" -and $r.migratedLanguage -eq $v1.language
    'Upgraded install keeps administrator mode'         = $r.migratedAlwaysRunAsAdmin -eq $true -and $r.migratedShowAdminNotice -eq $true
    '1.x data untouched and backed up'                  = $r.v1DataUntouched -and $r.v1BackupExists
    'No warning or error in the first-run log'          = $warnings.Count -eq 0
    'Smoke test'                                        = $r.smokeExit -eq 0
    'Uninstall'                                         = $r.uninstallExit -eq 0
    'Fresh install starts'                              = $r.newSetupExit -eq 0 -and $r.freshInstallVersion -eq $expectedFileVersion -and $r.freshFirstRunAlive
    'Fresh install does not use administrator mode'     = $r.freshAlwaysRunAsAdmin -eq $false
}

$failed = 0
foreach ($check in $checks.GetEnumerator()) {
    $ok = [bool]$check.Value
    if (-not $ok) { $failed++ }
    Write-Host ("{0}  {1}" -f ($ok ? 'PASS' : 'FAIL'), $check.Key)
}
$warnings | ForEach-Object { Write-Host "  log: $_" }
Write-Host "Details: $resultFile"
if ($failed) {
    Write-Host "$failed of $($checks.Count) checks failed." -ForegroundColor Red
    exit 1
}
Write-Host "All $($checks.Count) checks passed."
