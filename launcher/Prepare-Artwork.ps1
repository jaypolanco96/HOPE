param(
    [Parameter(Mandatory = $true)][string]$GameRoot,
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [string]$Ffmpeg = 'ffmpeg'
)
$ErrorActionPreference = 'Stop'
$movie = Join-Path $GameRoot 'data\movies\Attract_english_ntsc.vp6'
if (-not (Test-Path -LiteralPath $movie)) { throw 'The supplied game folder does not contain the Skate 3 attract movie.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$temporary = Join-Path $OutputDirectory ('extract-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary | Out-Null
& $Ffmpeg -v error -i $movie -vf 'fps=1/12' -frames:v 9 -q:v 2 (Join-Path $temporary 'frame-%02d.jpg')
if ($LASTEXITCODE -ne 0) { throw 'Artwork extraction failed. No images were downloaded.' }
$selection = @{ '01-street-flight.jpg' = 'frame-08.jpg'; '02-rooftop-line.jpg' = 'frame-04.jpg'; '03-bank-transfer.jpg' = 'frame-05.jpg' }
foreach ($image in $selection.GetEnumerator()) {
    Copy-Item -LiteralPath (Join-Path $temporary $image.Value) -Destination (Join-Path $OutputDirectory $image.Key)
}
[ordered]@{
    source = $movie
    source_sha256 = (Get-FileHash -LiteralPath $movie).Hash
    dimensions = '1280x720 (native HD source; no upscaling)'
    description = 'Authentic Skate 3 in-game footage from the player-supplied game files.'
    images = @(Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.jpg' | Get-FileHash | Select-Object Path,Hash)
} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $OutputDirectory 'PROVENANCE.json') -Encoding utf8
# Only remove the verified temporary child created above.
$resolvedOutput = (Resolve-Path -LiteralPath $OutputDirectory).Path
$resolvedTemporary = (Resolve-Path -LiteralPath $temporary).Path
if ((Split-Path $resolvedTemporary -Parent) -ne $resolvedOutput) { throw 'Unexpected extraction path; temporary cleanup stopped.' }
Remove-Item -LiteralPath $resolvedTemporary -Recurse
