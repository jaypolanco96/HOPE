$ErrorActionPreference = 'Stop'
$checks = 0
function Require($Condition, $Message) { if (!$Condition) { throw $Message }; $script:checks++ }
$sourceRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
$root = Join-Path $temporaryBase ('hope-update-fixture-' + [guid]::NewGuid().ToString('D'))
New-Item -ItemType Directory -Path $root | Out-Null
try {
    $updater = Join-Path $root 'Install-HOPEUpdate.ps1'
    Copy-Item -LiteralPath (Join-Path $sourceRoot 'pc-gameplay-menu\Install-PCMenu.ps1') -Destination $updater
    $payload = Join-Path $root 'payload'; $target = Join-Path $root 'HOPE'
    New-Item -ItemType Directory -Path $payload,$target | Out-Null
    $career = Join-Path $target ('careers\' + [guid]::NewGuid().ToString('D'))
    New-Item -ItemType Directory -Path $career | Out-Null
    Set-Content -LiteralPath (Join-Path $career 'career.ready') 'ready'
    Set-Content -LiteralPath (Join-Path $target 'portable.txt') ''
    $names = @('skate3.exe','rexruntimerd.dll','HOPE.exe','HOPE.dll','HOPE.deps.json','HOPE.runtimeconfig.json','HOPE.pdb')
    foreach ($name in $names) {
        Set-Content -LiteralPath (Join-Path $payload $name) ('new-' + $name)
        Set-Content -LiteralPath (Join-Path $target $name) ('old-' + $name)
    }
    Set-Content -LiteralPath (Join-Path $career 'skate3.exe') 'old-career'
    Set-Content -LiteralPath (Join-Path $career 'rexruntimerd.dll') 'old-runtime'
    Set-Content -LiteralPath (Join-Path $target 'settings.toml') 'keep-settings'
    Set-Content -LiteralPath (Join-Path $career 'SKATER.P') 'keep-save'
    $files = @($names | ForEach-Object { @{name=$_;sha256=(Get-FileHash -LiteralPath (Join-Path $payload $_)).Hash} })
    @{files=$files} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $root 'payload.json')
    # A locked executable must prevent every write, even after earlier files open.
    $before = @{}; foreach ($name in $names) { $before[$name]=(Get-FileHash -LiteralPath (Join-Path $target $name)).Hash }
    Remove-Item -LiteralPath (Join-Path $target 'rexruntimerd.dll')
    $locked = [IO.File]::Open((Join-Path $target 'HOPE.exe'),[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
    $failed = $false
    try { & $updater -Target $target } catch { $failed = $true } finally { $locked.Dispose() }
    Require $failed 'Locked launcher was not refused'
    foreach ($name in $names | Where-Object { $_ -ne 'rexruntimerd.dll' }) {
        Require ((Get-FileHash -LiteralPath (Join-Path $target $name)).Hash -eq $before[$name]) 'Lock refusal changed an existing file'
    }
    Require (!(Test-Path -LiteralPath (Join-Path $target 'rexruntimerd.dll'))) 'Lock refusal left a new empty file'
    Require (!(Test-Path -LiteralPath (Join-Path $target 'updates'))) 'Preflight lock failure began backups/writes'
    Set-Content -LiteralPath (Join-Path $target 'rexruntimerd.dll') 'old-rexruntimerd.dll'
    & $updater -Target $target
    foreach ($name in $names) {
        Require ((Get-FileHash -LiteralPath (Join-Path $target $name)).Hash -eq (Get-FileHash -LiteralPath (Join-Path $payload $name)).Hash) 'Root payload mismatch'
    }
    foreach ($name in @('skate3.exe','rexruntimerd.dll')) {
        Require ((Get-FileHash -LiteralPath (Join-Path $career $name)).Hash -eq (Get-FileHash -LiteralPath (Join-Path $payload $name)).Hash) 'Career payload mismatch'
    }
    Require ((Get-Content -LiteralPath (Join-Path $target 'settings.toml')) -eq 'keep-settings') 'Settings changed'
    Require ((Get-Content -LiteralPath (Join-Path $career 'SKATER.P')) -eq 'keep-save') 'Career save changed'
    $map = Get-ChildItem -LiteralPath (Join-Path $target 'updates') -Filter 'restore-map.json' -Recurse
    $entries = @(Get-Content -LiteralPath $map.FullName -Raw | ConvertFrom-Json)
    Require ($entries.Count -eq 9) 'Backup coverage mismatch'
    foreach ($entry in $entries) {
        Require ((Get-FileHash -LiteralPath $entry.backup).Hash -eq $entry.originalHash) 'Backup checksum mismatch'
    }
    Set-Content -LiteralPath (Join-Path $payload 'HOPE.exe') 'corrupt'
    $beforeCorrupt = (Get-FileHash -LiteralPath (Join-Path $target 'HOPE.exe')).Hash
    $failed = $false; try { & $updater -Target $target } catch { $failed=$true }
    Require ($failed -and (Get-FileHash -LiteralPath (Join-Path $target 'HOPE.exe')).Hash -eq $beforeCorrupt) 'Corrupt payload changed destination'
    Write-Output "Passed $checks update lock, backup, career and preservation checks using disposable data."
} finally {
    $resolved = [IO.Path]::GetFullPath($root)
    if (!$resolved.StartsWith($temporaryBase + '\', [StringComparison]::OrdinalIgnoreCase) -or
        !(Split-Path $resolved -Leaf).StartsWith('hope-update-fixture-')) { throw 'Refusing unsafe fixture cleanup path' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
