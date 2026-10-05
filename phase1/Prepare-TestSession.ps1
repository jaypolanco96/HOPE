param(
    [ValidateSet('Saved', 'Fresh')][string]$Profile = 'Saved',
    [ValidateSet('Native', 'Emulated')][string]$Renderer = 'Native',
    [ValidateSet('Installed', 'Diagnostic')][string]$Build = 'Installed',
    [ValidateSet('Controller', 'Keyboard')][string]$InputMethod = 'Controller'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$installRoot = Split-Path -Parent $repoRoot
$baseline = Join-Path $repoRoot 'local-phase1\baseline'
$binaryRoot = if ($Build -eq 'Diagnostic') { Join-Path $repoRoot 'out\build\phase1' } else { $baseline }
$runtimeName = if ($Build -eq 'Diagnostic') { 'rexruntimerd.dll' } else { 'rexruntime.dll' }
$gameRoot = Join-Path $installRoot 'game'
foreach ($required in @('skate3.exe', $runtimeName)) {
    if (-not (Test-Path -LiteralPath (Join-Path $binaryRoot $required))) {
        throw "Missing $Build binary: $required"
    }
}
if (-not (Test-Path -LiteralPath (Join-Path $baseline 'settings.toml'))) { throw 'Missing baseline settings.' }
if (-not (Test-Path -LiteralPath (Join-Path $gameRoot 'default.xex'))) {
    throw 'The extracted game is missing.'
}
$sessionName = '{0}-{1}-{2}-{3}-{4}-{5}' -f (Get-Date -Format 'yyyyMMdd-HHmmss'), $Profile, $Renderer, $Build, $InputMethod, ([guid]::NewGuid().ToString('N').Substring(0, 8))
$sessionRoot = Join-Path $repoRoot "local-phase1\sessions\$sessionName"
New-Item -ItemType Directory -Path $sessionRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $binaryRoot 'skate3.exe'), (Join-Path $binaryRoot $runtimeName) -Destination $sessionRoot
$settings = Get-Content -LiteralPath (Join-Path $baseline 'settings.toml')
$settings = $settings | Where-Object { $_ -notmatch '^\s*(skate3_demo_path|skate3_demo_path_probe|skate3_native_render_scene|mnk_mode)\s*=' }
$nativeValue = if ($Renderer -eq 'Native') { 'true' } else { 'false' }
$mnkValue = if ($InputMethod -eq 'Keyboard') { 'true' } else { 'false' }
$settings += @('skate3_demo_path = false', 'skate3_demo_path_probe = true', "skate3_native_render_scene = $nativeValue", "mnk_mode = $mnkValue")
$settings | Set-Content -LiteralPath (Join-Path $sessionRoot 'settings.toml') -Encoding utf8
if ($Profile -eq 'Saved') {
    $saveFolders = @(Get-ChildItem -LiteralPath $baseline -Directory | Where-Object { $_.Name -match '^[0-9A-Fa-f]{16}$' })
    if ($saveFolders.Count -eq 0) { throw 'No backed-up profile saves found.' }
    foreach ($folder in $saveFolders) {
        Copy-Item -LiteralPath $folder.FullName -Destination $sessionRoot -Recurse
    }
}
# Single-quoted TOML literal strings preserve Windows paths and spaces.
"game_data_root = '$gameRoot'" | Set-Content -LiteralPath (Join-Path $sessionRoot 'skate3.toml') -Encoding utf8
New-Item -ItemType File -Path (Join-Path $sessionRoot 'portable.txt') | Out-Null
[ordered]@{
    profile = $Profile
    renderer = $Renderer
    release_base_commit = 'f6e0ae87fdfecbadb5c1e36c55d66a744187a3cd'
    binary = $Build
    executable_sha256 = (Get-FileHash -LiteralPath (Join-Path $sessionRoot 'skate3.exe')).Hash
    runtime_file = $runtimeName
    runtime_sha256 = (Get-FileHash -LiteralPath (Join-Path $sessionRoot $runtimeName)).Hash
    input = $InputMethod
    prepared_at = (Get-Date -Format o)
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $sessionRoot 'session.json') -Encoding utf8
Write-Output "Prepared isolated test session: $sessionRoot"
Write-Output "Launch: $(Join-Path $sessionRoot 'skate3.exe')"
Write-Output 'Logs and test saves stay in this portable session. The original save is untouched.'
