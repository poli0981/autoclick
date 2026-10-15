<#
.SYNOPSIS
    Runs inside Windows Sandbox (Windows PowerShell 5.1); started by eng/Invoke-UpgradeTest.ps1.
.DESCRIPTION
    Installs the old release with its Setup.exe, adds 1.x user data, applies the new full package
    with the installed Update.exe (the path the in-app updater takes), starts the new version once
    to migrate the data, runs its smoke test, uninstalls, then installs the new Setup.exe and starts
    it once on the clean profile. Writes C:\test\results\result.json and shuts the sandbox down.
    Inputs: C:\test\old\*Setup.exe, C:\test\new\*-full.nupkg + *Setup.exe, C:\test\fixtures.
#>
$ErrorActionPreference = 'Continue'
$root = 'C:\test'
$out = Join-Path $root 'results'
New-Item -ItemType Directory -Force $out | Out-Null
Start-Transcript -Path (Join-Path $out 'transcript.txt') | Out-Null

$result = [ordered]@{ started = (Get-Date).ToString('o') }
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$result.elevated = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
$app = Join-Path $env:LOCALAPPDATA 'AutoClick'

function Wait-Proc($p, $seconds) {
    if ($p.WaitForExit($seconds * 1000)) { return $p.ExitCode }
    try { $p.Kill() } catch {}
    return 'timeout'
}
function Stop-App { Get-Process AutoClick.UI -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue }
function Get-V1Hashes {
    $files = @(Get-ChildItem (Join-Path $app 'settings.json'), (Join-Path $app 'profiles\*.json') -ErrorAction SilentlyContinue)
    ($files | Sort-Object FullName | ForEach-Object { "$($_.Name)=$((Get-FileHash $_.FullName).Hash)" }) -join ';'
}
# Callers wrap the result in @(): a function returning an empty array returns $null.
function Get-LogLines($pattern) {
    Get-Content (Join-Path $app 'logs\*.log') -ErrorAction SilentlyContinue | Where-Object { $_ -match $pattern } | ForEach-Object { [string]$_ }
}

# 1. Install the old release.
$old = Get-ChildItem (Join-Path $root 'old') -Filter '*Setup.exe' | Select-Object -First 1
$p = Start-Process $old.FullName -ArgumentList '--silent', '--log', (Join-Path $out 'setup-old.log') -PassThru
$result.oldSetupExit = Wait-Proc $p 300
Start-Sleep -Seconds 8
Stop-App
$result.oldVersion = (Get-Item (Join-Path $app 'current\AutoClick.UI.exe') -ErrorAction SilentlyContinue).VersionInfo.FileVersion

# 2. Add 1.x user data (what an existing user has on disk).
Copy-Item (Join-Path $root 'fixtures\*') $app -Recurse -Force
$result.v1DataBefore = Get-V1Hashes

# 3. Apply the new full package with the installed (old) Update.exe.
$pkgDir = Join-Path $app 'packages'
New-Item -ItemType Directory -Force $pkgDir | Out-Null
$newPkg = Get-ChildItem (Join-Path $root 'new') -Filter '*-full.nupkg' | Select-Object -First 1
Copy-Item $newPkg.FullName $pkgDir -Force
$pkgPath = Join-Path $pkgDir $newPkg.Name
$p = Start-Process (Join-Path $app 'Update.exe') -ArgumentList 'apply', '--norestart', '--silent', '--package', $pkgPath, '--log', (Join-Path $out 'update-apply.log') -PassThru
$result.applyExit = Wait-Proc $p 600
Start-Sleep -Seconds 5
Stop-App
$result.newVersion = (Get-Item (Join-Path $app 'current\AutoClick.UI.exe') -ErrorAction SilentlyContinue).VersionInfo.FileVersion

# 4. First start of the new version: migrates the 1.x data. The logon script runs elevated, so the
#    "keep running as administrator" setting of an upgraded install does not relaunch.
$p = Start-Process (Join-Path $app 'current\AutoClick.UI.exe') -PassThru
Start-Sleep -Seconds 15
$result.firstRunAlive = -not $p.HasExited
Stop-App
Start-Sleep -Seconds 2
$v2 = Join-Path $app 'settings.v2.json'
$result.settingsV2Exists = Test-Path $v2
if ($result.settingsV2Exists) {
    $s = Get-Content $v2 -Raw | ConvertFrom-Json
    $result.migratedHotkeys = "$($s.hotkeys.pauseResume) / $($s.hotkeys.stopAll) / $($s.hotkeys.startAll)"
    $result.migratedLanguage = $s.language
    $result.migratedAlwaysRunAsAdmin = $s.alwaysRunAsAdmin
    $result.migratedShowAdminNotice = $s.showAdminModeNotice
}
$result.v1BackupExists = Test-Path (Join-Path $app 'backup\settings.v1.json')
$result.v1DataAfter = Get-V1Hashes
$result.v1DataUntouched = ($result.v1DataBefore -eq $result.v1DataAfter) -and ($result.v1DataBefore -ne '')
Copy-Item (Join-Path $app 'logs\*.log') $out -ErrorAction SilentlyContinue
$result.firstRunWarnings = @(Get-LogLines '\[(WRN|ERR|FTL)\]')

# 5. Smoke test of the updated app (uses its own temporary data folder).
$p = Start-Process (Join-Path $app 'current\AutoClick.UI.exe') -ArgumentList '--smoke-test' -PassThru
$result.smokeExit = Wait-Proc $p 180

# 6. Uninstall: removes the install root, which is also the data folder.
$p = Start-Process (Join-Path $app 'Update.exe') -ArgumentList 'uninstall', '--silent', '--log', (Join-Path $out 'uninstall.log') -PassThru
$result.uninstallExit = Wait-Proc $p 300
Start-Sleep -Seconds 5
$result.afterUninstallFiles = @(Get-ChildItem $app -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName.Substring($app.Length + 1) })

# 7. Fresh install of the new Setup.exe. A silent setup does not start the app, so start it once
#    like a new user would: it must not turn on administrator mode.
$new = Get-ChildItem (Join-Path $root 'new') -Filter '*Setup.exe' | Select-Object -First 1
$p = Start-Process $new.FullName -ArgumentList '--silent', '--log', (Join-Path $out 'setup-new.log') -PassThru
$result.newSetupExit = Wait-Proc $p 300
Start-Sleep -Seconds 5
Stop-App
$result.freshInstallVersion = (Get-Item (Join-Path $app 'current\AutoClick.UI.exe') -ErrorAction SilentlyContinue).VersionInfo.FileVersion
$p = Start-Process (Join-Path $app 'current\AutoClick.UI.exe') -PassThru
Start-Sleep -Seconds 12
$result.freshFirstRunAlive = -not $p.HasExited
Stop-App
Start-Sleep -Seconds 2
$fresh = Get-Content (Join-Path $app 'settings.v2.json') -Raw -ErrorAction SilentlyContinue | ConvertFrom-Json
$result.freshAlwaysRunAsAdmin = $fresh.alwaysRunAsAdmin
$result.freshStartupLines = @(Get-LogLines 'ready \(settings')

$result.finished = (Get-Date).ToString('o')
$result | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $out 'result.json') -Encoding UTF8
Stop-Transcript | Out-Null
shutdown.exe /s /t 5
