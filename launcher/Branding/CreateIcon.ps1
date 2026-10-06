Add-Type -AssemblyName System.Drawing
$folder=$PSScriptRoot
function Rounded($x,$y,$w,$h,$r) {
 $p=[Drawing.Drawing2D.GraphicsPath]::new(); $d=$r*2
 $p.AddArc($x,$y,$d,$d,180,90);$p.AddArc($x+$w-$d,$y,$d,$d,270,90);$p.AddArc($x+$w-$d,$y+$h-$d,$d,$d,0,90);$p.AddArc($x,$y+$h-$d,$d,$d,90,90);$p.CloseFigure();return $p
}
$pngs=@(); $sizes=@(16,20,24,32,48,64,128,256)
foreach($size in $sizes) {
 $bitmap=[Drawing.Bitmap]::new($size,$size,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[Drawing.Graphics]::FromImage($bitmap);$g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias;$g.ScaleTransform($size/256.0,$size/256.0);$g.Clear([Drawing.Color]::Transparent)
 $badge=Rounded 8 8 240 240 50;$bg=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#111022'));$g.FillPath($bg,$badge)
 $pink=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#FF55CA'));$lime=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#D8FF3E'));$mint=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#68FFE4'))
 # Three bold strokes form the H; no font dependency at any icon size.
 $g.FillRectangle($lime,62,49,34,116);$g.FillRectangle($lime,160,49,34,116);$g.FillRectangle($lime,87,91,82,31)
 $deck=Rounded 47 178 162 20 10;$g.FillPath($pink,$deck)
 $g.FillEllipse($mint,72,203,16,16);$g.FillEllipse($mint,168,203,16,16)
 $g.Dispose();$stream=[IO.MemoryStream]::new();$bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png);$bytes=$stream.ToArray();$pngs+=,@{size=$size;data=$bytes};if($size -eq 256){[IO.File]::WriteAllBytes((Join-Path $folder 'hope.png'),$bytes)}
 $stream.Dispose();$bitmap.Dispose();$badge.Dispose();$deck.Dispose();$bg.Dispose();$pink.Dispose();$lime.Dispose();$mint.Dispose()
}
$ico=[IO.MemoryStream]::new();$writer=[IO.BinaryWriter]::new($ico);$writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sizes.Count);$offset=6+16*$sizes.Count
foreach($entry in $pngs){$dim=if($entry.size -eq 256){0}else{$entry.size};$writer.Write([byte]$dim);$writer.Write([byte]$dim);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$entry.data.Length);$writer.Write([uint32]$offset);$offset+=$entry.data.Length}
foreach($entry in $pngs){$writer.Write([byte[]]$entry.data)};$writer.Flush();[IO.File]::WriteAllBytes((Join-Path $folder 'hope.ico'),$ico.ToArray());$writer.Dispose();$ico.Dispose()

