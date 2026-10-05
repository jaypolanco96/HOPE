param(
    [ValidateSet('Saved', 'Fresh')][string]$Profile = 'Saved',
    [ValidateSet('Native', 'Emulated')][string]$Renderer = 'Native'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$installRoot = Split-Path -Parent $repoRoot
$baseline = Join-Path $repoRoot 'local-phase1\baseline'
$gameRoot = Join-Path $installRoot 'game'
foreach ($required in @('skate3.exe', 'rexruntime.dll', 'settings.toml')) {
    if (-not (Test-Path -LiteralPath (Join-Path $baseline $required))) {
        throw "Missing baseline file: $required"
    }
}
if (-not (Test-Path -LiteralPath (Join-Path $gameRoot 'default.xex'))) {
    throw 'The extracted game is missing.'
}
$sessionName = '{0}-{1}-{2}-{3}' -f (Get-Date -Format 'yyyyMMdd-HHmmss'), $Profile, $Renderer, ([guid]::NewGuid().ToString('N').Substring(0, 8))
$sessionRoot = Join-Path $repoRoot "local-phase1\sessions\$sessionName"
New-Item -ItemType Directory -Path $sessionRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $baseline 'skate3.exe'), (Join-Path $baseline 'rexruntime.dll') -Destination $sessionRoot
$settings = Get-Content -LiteralPath (Join-Path $baseline 'settings.toml')
$settings = $settings | Where-Object { $_ -notmatch '^\s*(skate3_demo_path|skate3_demo_path_probe|skate3_native_render_scene)\s*=' }
$nativeValue = if ($Renderer -eq 'Native') { 'true' } else { 'false' }
$settings += @('skate3_demo_path = false', 'skate3_demo_path_probe = true', "skate3_native_render_scene = $nativeValue")
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
    source_commit = 'f6e0ae87fdfecbadb5c1e36c55d66a744187a3cd'
    binary = 'Original installed v2.0.2; diagnostic source patch is not compiled'
    input = 'controller'
    prepared_at = (Get-Date -Format o)
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $sessionRoot 'session.json') -Encoding utf8
Write-Output "Prepared isolated test session: $sessionRoot"
Write-Output "Launch: $(Join-Path $sessionRoot 'skate3.exe')"
Write-Output 'Logs and test saves stay in this portable session. The original save is untouched.'
