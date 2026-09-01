<#
    Descarga binarios autónomos de yt-dlp + ffmpeg para un RID en core/tools/<rid>/.
    Lo invoca el build (target FetchExternalTools) en Windows y también puede lanzarse a mano:
        powershell -ExecutionPolicy Bypass -File core/tools/fetch-tools.ps1 -Rid win-x64
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
        'win-*'       { 'yt-dlp.exe' }
        'osx-*'       { 'yt-dlp_macos' }
        'linux-arm64' { 'yt-dlp_linux_aarch64' }
        default       { 'yt-dlp_linux' }
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
    else {
        Write-Host "· ffmpeg (gyan.dev, essentials)"
        Expand-One "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip" 'ffmpeg.exe' $ffmpegOut
    }
}

Write-Host "Herramientas listas en $dest"