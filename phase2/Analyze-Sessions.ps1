param(
  [Parameter(Mandatory=$true)][string]$LogDirectory,
  [Parameter(Mandatory=$true)][string]$OutputPath
)
$ErrorActionPreference = 'Stop'
# Read-only log analysis. Does not start the game or modify player files.
function Read-Time([string]$line) {
  if ($line -match '^\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})\]') {
    return [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss.fff', [cultureinfo]::InvariantCulture)
  }
  return $null
}
$sessions = foreach ($file in Get-ChildItem -LiteralPath $LogDirectory -Filter 'skate3_*.log' -File | Sort-Object Name) {
  $lines = @(Get-Content -LiteralPath $file.FullName)
  $closing = $null
  $missing = @{}
  $errors = @()
  for ($i=0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line.Contains('Window closing, shutting down')) { $closing = Read-Time $line }
    if ($line.Contains('VFS: entry not found')) {
      if ($line -match "entry not found for '([^']*)'") { $name=$Matches[1] } else { $name=$line }
      if (-not $missing.ContainsKey($name)) { $missing[$name]=0 }
      $missing[$name]++
    }
    if ($line.Contains('BaseHeap::Release failed')) {
      $time = Read-Time $line
      $delay = if ($null -ne $closing -and $null -ne $time) { ($time-$closing).TotalMilliseconds } else { $null }
      $errors += [ordered]@{ line=$i+1; after_shutdown_marker=($null -ne $closing); milliseconds_after_shutdown=$delay; message=$line }
    }
  }
  [ordered]@{
    file=$file.Name; sha256=(Get-FileHash -LiteralPath $file.FullName).Hash
    execution_complete=(@($lines | Where-Object { $_.Contains('Execution complete') }).Count -gt 0)
    heap_release_errors=$errors
    missing_requests=@($missing.GetEnumerator() | Sort-Object Name | ForEach-Object {
      [ordered]@{ path=$_.Key; count=$_.Value; classification='Unclassified request; verify caller/fallback before treating as a required missing asset.' }
    })
  }
}
$report = [ordered]@{ generated_at=(Get-Date).ToString('o'); sessions=@($sessions); limitation='Shutdown timing does not establish root cause. Execution complete does not prove progress was saved or gameplay was successful.' }
$destination = [IO.Path]::GetFullPath($OutputPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $destination -Encoding utf8
Write-Output "Analyzed $(@($sessions).Count) session logs without modifying them."
