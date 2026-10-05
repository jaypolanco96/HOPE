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
        $operations += [pscustomobject]@{ path=$path; name=$name; existed=(Test-Path -LiteralPath $path); backup=''; stream=$null; originalHash='' }
    }
}
function Hash-Stream($Stream) {
    $Stream.Position = 0
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($Stream)).Replace('-', '') }
    finally { $sha.Dispose() }
}
$backup = Join-Path $Target ('updates\hope-update-' + [guid]::NewGuid().ToString('D'))
Assert-OrdinaryPath $backup
$opened = @()
$changed = @()
$success = $false
try {
    # Acquire every destination before backing up or changing any existing bytes.
    # Keep these exclusive handles until verification/rollback is finished.
    foreach ($operation in $operations) {
        $mode = if ($operation.existed) { [IO.FileMode]::Open } else { [IO.FileMode]::CreateNew }
        $operation.stream = [IO.File]::Open($operation.path, $mode, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        $opened += $operation
    }
    New-Item -ItemType Directory -Path $backup | Out-Null
    for ($i=0; $i -lt $operations.Count; $i++) {
        $operation = $operations[$i]
        $operation.backup = Join-Path $backup ($i.ToString() + '-' + $operation.name)
        if ($operation.existed) {
            $operation.originalHash = Hash-Stream $operation.stream
            $operation.stream.Position = 0
            $output = [IO.File]::Open($operation.backup, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
            try { $operation.stream.CopyTo($output); $output.Flush($true) } finally { $output.Dispose() }
            if ((Get-FileHash -LiteralPath $operation.backup).Hash -ne $operation.originalHash) {
                throw 'Backup verification failed; installation has not begun.'
            }
        }
    }
    $operations | Select-Object path,name,existed,backup,originalHash | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $backup 'restore-map.json')
    if (Get-Process skate3 -ErrorAction SilentlyContinue) { throw 'Skate 3 started; installation has not begun.' }
    foreach ($operation in $operations) {
        $changed += $operation
        $operation.stream.Position = 0
        $operation.stream.SetLength(0)
        $inputFile = [IO.File]::OpenRead((Join-Path $payload $operation.name))
        try { $inputFile.CopyTo($operation.stream); $operation.stream.Flush($true) } finally { $inputFile.Dispose() }
        $expected = ($manifest.files | Where-Object name -eq $operation.name).sha256
        if ((Hash-Stream $operation.stream) -ne $expected) { throw 'Installed checksum mismatch.' }
    }
    $success = $true
} catch {
    $problem = $_
    $rollbackErrors = @()
    foreach ($operation in $changed) {
        if ($operation.existed) {
            try {
                $operation.stream.Position = 0
                $operation.stream.SetLength(0)
                $inputFile = [IO.File]::OpenRead($operation.backup)
                try { $inputFile.CopyTo($operation.stream); $operation.stream.Flush($true) } finally { $inputFile.Dispose() }
                if ((Hash-Stream $operation.stream) -ne $operation.originalHash) { throw 'Restored checksum mismatch.' }
            } catch { $rollbackErrors += $operation.path }
        }
    }
    if ($rollbackErrors.Count) { throw "Update failed and some program files could not be restored: $($rollbackErrors -join ', '). Backups: $backup" }
    throw "Update could not be installed: $($problem.Exception.Message) Close HOPE and Skate 3 before retrying. Existing-file rollback was verified."
} finally {
    foreach ($operation in $opened) { $operation.stream.Dispose() }
    if (!$success) {
        foreach ($operation in $opened) {
            if (!$operation.existed -and (Test-Path -LiteralPath $operation.path)) { Remove-Item -LiteralPath $operation.path }
        }
    }
}
Write-Output "HOPE update installed in $Target. Existing saves/settings were untouched. Program backups: $backup"
