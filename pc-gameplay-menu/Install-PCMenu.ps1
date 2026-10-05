param([string]$Target = (Join-Path (Split-Path $PSScriptRoot -Parent) 'HOPE'))
$ErrorActionPreference = 'Stop'
if (Get-Process skate3 -ErrorAction SilentlyContinue) {
    throw 'Finish saving and close Skate 3 before installing this update.'
}
$Target = [IO.Path]::GetFullPath($Target)
function Assert-OrdinaryPath([string]$Path) {
    $cursor = $Path
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Linked paths cannot be updated: $cursor"
            }
        }
        $next = Split-Path $cursor -Parent
        if ($next -eq $cursor) { break }
        $cursor = $next
    }
}
Assert-OrdinaryPath $Target
if (!(Test-Path -LiteralPath (Join-Path $Target 'portable.txt')) -or
    !(Test-Path -LiteralPath (Join-Path $Target 'HOPE.exe'))) {
    throw 'Choose an existing portable HOPE folder.'
}
$payload = Join-Path $PSScriptRoot 'payload'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'payload.json') -Raw | ConvertFrom-Json
$allowed = @('skate3.exe','rexruntimerd.dll','HOPE.exe','HOPE.dll','HOPE.deps.json','HOPE.runtimeconfig.json','HOPE.pdb')
foreach ($file in $manifest.files) {
    if ($allowed -notcontains $file.name) { throw 'Unexpected payload filename.' }
    $path = Join-Path $payload $file.name
    Assert-OrdinaryPath $path
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.sha256) {
        throw "Update checksum failed: $($file.name)"
    }
}
if (@($manifest.files).Count -ne $allowed.Count -or
    @($manifest.files.name | Select-Object -Unique).Count -ne $allowed.Count) { throw 'Incomplete update payload.' }
$roots = @($Target)
$careers = Join-Path $Target 'careers'
if (Test-Path -LiteralPath $careers) {
    Assert-OrdinaryPath $careers
    foreach ($career in Get-ChildItem -LiteralPath $careers -Directory) {
        $id = [guid]::Empty
        if ([guid]::TryParseExact($career.Name, 'D', [ref]$id) -and
            (Test-Path -LiteralPath (Join-Path $career.FullName 'career.ready'))) {
            Assert-OrdinaryPath $career.FullName
            $roots += $career.FullName
        }
    }
}
$operations = @()
foreach ($root in $roots) {
    $names = if ($root -eq $Target) { $allowed } else { @('skate3.exe','rexruntimerd.dll') }
    foreach ($name in $names) {
        $path = Join-Path $root $name
        Assert-OrdinaryPath $path
        $operations += [pscustomobject]@{ path=$path; name=$name; existed=(Test-Path -LiteralPath $path); backup='' }
    }
}
$backup = Join-Path $Target ('updates\pc-menu-' + [guid]::NewGuid().ToString('D'))
Assert-OrdinaryPath $backup
New-Item -ItemType Directory -Path $backup | Out-Null
for ($i=0; $i -lt $operations.Count; $i++) {
    $operation = $operations[$i]
    $operation.backup = Join-Path $backup ($i.ToString() + '-' + $operation.name)
    if ($operation.existed) {
        Copy-Item -LiteralPath $operation.path -Destination $operation.backup
        if ((Get-FileHash -LiteralPath $operation.path).Hash -ne (Get-FileHash -LiteralPath $operation.backup).Hash) {
            throw 'Backup verification failed; installation has not begun.'
        }
    }
}
$operations | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'restore-map.json')
if (Get-Process skate3 -ErrorAction SilentlyContinue) { throw 'Skate 3 started; installation has not begun.' }
$changed = @()
try {
    foreach ($operation in $operations) {
        $changed += $operation
        Copy-Item -LiteralPath (Join-Path $payload $operation.name) -Destination $operation.path -Force
        $expected = ($manifest.files | Where-Object name -eq $operation.name).sha256
        if ((Get-FileHash -LiteralPath $operation.path).Hash -ne $expected) { throw 'Installed checksum mismatch.' }
    }
} catch {
    foreach ($operation in $changed) {
        if ($operation.existed) { Copy-Item -LiteralPath $operation.backup -Destination $operation.path -Force }
        elseif (Test-Path -LiteralPath $operation.path) { Remove-Item -LiteralPath $operation.path }
    }
    throw
}
Write-Output "PC gameplay menu installed in $Target. Existing saves/settings were untouched. Program backups: $backup"
