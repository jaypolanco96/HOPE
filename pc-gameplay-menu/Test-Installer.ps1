$ErrorActionPreference = 'Stop'
# Synthetic files only. Never runs the game, launcher or a real installation.
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ('hope-installer-test-' + [guid]::NewGuid().ToString('N'))
$fixtureRoot = [IO.Path]::GetFullPath($fixtureRoot)
$target = Join-Path $fixtureRoot 'HOPE'
$package = Join-Path $fixtureRoot 'package'
$payload = Join-Path $package 'payload'
$names = @('skate3.exe','rexruntimerd.dll','HOPE.exe','HOPE.dll','HOPE.deps.json','HOPE.runtimeconfig.json','HOPE.pdb')
$checks = 0
function Check([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw $Message }; $script:checks++
}
try {
    New-Item -ItemType Directory -Path $target,$payload -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-PCMenu.ps1') -Destination (Join-Path $package 'Install-PCMenu.ps1')
    foreach ($name in $names) {
        [IO.File]::WriteAllText((Join-Path $target $name), 'original-' + $name)
        [IO.File]::WriteAllText((Join-Path $payload $name), 'updated-' + $name)
    }
    [IO.File]::WriteAllText((Join-Path $target 'portable.txt'), '')
    [IO.File]::WriteAllText((Join-Path $target 'settings.toml'), 'settings sentinel')
    $careerRoots = @()
    foreach ($format in @('N','D')) {
        $career = Join-Path $target ('careers\' + [guid]::NewGuid().ToString($format))
        New-Item -ItemType Directory -Path $career -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $career 'career.ready'), '')
        foreach ($name in $names[0..1]) { [IO.File]::WriteAllText((Join-Path $career $name), 'original-' + $name) }
        [IO.File]::WriteAllText((Join-Path $career 'SKATER.P'), 'save sentinel')
        $careerRoots += $career
    }
    $unfinished = Join-Path $target ('careers\' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $unfinished -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $unfinished 'skate3.exe'), 'unfinished sentinel')
    $manifest = @{files=@($names | ForEach-Object { @{name=$_;sha256=(Get-FileHash -LiteralPath (Join-Path $payload $_) -Algorithm SHA256).Hash} })}
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $package 'payload.json')
    $locked = [IO.File]::Open((Join-Path $careerRoots[0] 'rexruntimerd.dll'), [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
    try {
        $rejected = $false
        try { & (Join-Path $package 'Install-PCMenu.ps1') -Target $target | Out-Null } catch { $rejected = $true }
        Check $rejected 'Locked native runtime in an N-format career must block the update.'
        foreach ($name in $names) { Check ([IO.File]::ReadAllText((Join-Path $target $name)) -eq ('original-' + $name)) 'Main program changed despite locked career.' }
        Check ([IO.File]::ReadAllText((Join-Path $careerRoots[0] 'skate3.exe')) -eq 'original-skate3.exe') 'Career program changed before all files were acquired.'
    } finally { $locked.Dispose() }
    & (Join-Path $package 'Install-PCMenu.ps1') -Target $target | Out-Null
    foreach ($name in $names) { Check ([IO.File]::ReadAllText((Join-Path $target $name)) -eq ('updated-' + $name)) 'Main program update failed.' }
    foreach ($career in $careerRoots) {
        foreach ($name in $names[0..1]) { Check ([IO.File]::ReadAllText((Join-Path $career $name)) -eq ('updated-' + $name)) 'N/D career program update failed.' }
        Check ([IO.File]::ReadAllText((Join-Path $career 'SKATER.P')) -eq 'save sentinel') 'Career save was modified.'
    }
    Check ([IO.File]::ReadAllText((Join-Path $unfinished 'skate3.exe')) -eq 'unfinished sentinel') 'Incomplete career must remain untouched.'
    Check ([IO.File]::ReadAllText((Join-Path $target 'settings.toml')) -eq 'settings sentinel') 'Settings were modified.'
    "Passed $checks synthetic installer checks. No real installation was modified."
} finally {
    # Only this generated GUID directory, directly under the OS temp folder.
    if ((Split-Path $fixtureRoot -Parent) -eq ([IO.Path]::GetTempPath()).TrimEnd('\') -and
        (Split-Path $fixtureRoot -Leaf) -match '^hope-installer-test-[a-f0-9]{32}$') {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}
