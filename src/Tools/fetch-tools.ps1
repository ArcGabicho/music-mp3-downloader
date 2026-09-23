<#
    Descarga binarios autónomos de yt-dlp + ffmpeg + deno para un RID en src/Tools/<rid>/.
    Lo invoca el build (target FetchExternalTools) en Windows y también puede lanzarse a mano:
        powershell -ExecutionPolicy Bypass -File src/Tools/fetch-tools.ps1 -Rid win-x64
    No instala nada en el sistema.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Rid
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$dest = Join-Path $PSScriptRoot $Rid
New-Item -ItemType Directory -Force -Path $dest | Out-Null

$isWin = $Rid -like 'win-*'
$ytDlpOut  = if ($isWin) { Join-Path $dest 'yt-dlp.exe' } else { Join-Path $dest 'yt-dlp' }
$ffmpegOut = if ($isWin) { Join-Path $dest 'ffmpeg.exe' } else { Join-Path $dest 'ffmpeg' }
$denoOut   = if ($isWin) { Join-Path $dest 'deno.exe' } else { Join-Path $dest 'deno' }

function Expand-One([string]$Url, [string]$LeafName, [string]$OutFile) {
    $tmp = Join-Path ([IO.Path]::GetTempPath()) ("tool-" + [guid]::NewGuid())
    New-Item -ItemType Directory -Force -Path $tmp | Out-Null
    try {
        $archive = Join-Path $tmp 'archive.zip'
        Invoke-WebRequest $Url -OutFile $archive
        Expand-Archive -Path $archive -DestinationPath $tmp -Force
        $found = Get-ChildItem -Path $tmp -Recurse -Filter $LeafName | Select-Object -First 1
        Copy-Item $found.FullName $OutFile -Force
    }
    finally {
        Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
    }
}

# ---------------- yt-dlp ----------------
if (-not (Test-Path $ytDlpOut)) {
    $asset = switch -Wildcard ($Rid) {
        'win-*' { 'yt-dlp.exe' }
        'osx-*' { 'yt-dlp_macos' }
        default { throw "RID no soportado: $Rid" }
    }
    Write-Host "· yt-dlp ($asset)"
    Invoke-WebRequest "https://github.com/yt-dlp/yt-dlp/releases/latest/download/$asset" -OutFile $ytDlpOut
}

# ---------------- ffmpeg ----------------
if (-not (Test-Path $ffmpegOut)) {
    if ($Rid -eq 'win-arm64') {
        Write-Host "· ffmpeg (BtbN, winarm64 lgpl)"
        Expand-One "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-winarm64-lgpl.zip" 'ffmpeg.exe' $ffmpegOut
    }
    elseif ($isWin) {
        Write-Host "· ffmpeg (gyan.dev, essentials)"
        Expand-One "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip" 'ffmpeg.exe' $ffmpegOut
    }
    else {
        Write-Host "· ffmpeg (evermeet.cx)"
        Expand-One "https://evermeet.cx/ffmpeg/getrelease/ffmpeg/zip" 'ffmpeg' $ffmpegOut
    }
}

# ---------------- deno ----------------
# yt-dlp necesita un intérprete de JavaScript para resolver los desafíos de YouTube;
# sin él, cada vez más formatos (o videos enteros) dejan de estar disponibles.
if (-not (Test-Path $denoOut)) {
    $target = switch ($Rid) {
        'win-x64'   { 'x86_64-pc-windows-msvc' }
        'win-arm64' { 'aarch64-pc-windows-msvc' }
        'osx-x64'   { 'x86_64-apple-darwin' }
        'osx-arm64' { 'aarch64-apple-darwin' }
        default     { throw "RID no soportado: $Rid" }
    }
    Write-Host "· deno ($target)"
    Expand-One "https://github.com/denoland/deno/releases/latest/download/deno-$target.zip" (Split-Path $denoOut -Leaf) $denoOut
}

Write-Host "Herramientas listas en $dest"