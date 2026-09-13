#!/usr/bin/env bash
# Descarga binarios autónomos de yt-dlp + ffmpeg para un RID concreto en core/tools/<rid>/.
# Lo invoca el build (target FetchExternalTools) y también puede ejecutarse a mano:
#     bash core/tools/fetch-tools.sh win-x64
#
# No instala nada en el sistema: todo queda dentro de la carpeta del proyecto.
set -euo pipefail

RID="${1:?Uso: fetch-tools.sh <rid>   (win-x64 | win-arm64 | osx-x64 | osx-arm64)}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEST="$HERE/$RID"
mkdir -p "$DEST"

is_win() { [[ "$RID" == win-* ]]; }

if is_win; then
  YTDLP_OUT="$DEST/yt-dlp.exe"
  FFMPEG_OUT="$DEST/ffmpeg.exe"
else
  YTDLP_OUT="$DEST/yt-dlp"
  FFMPEG_OUT="$DEST/ffmpeg"
fi

fetch() { curl --fail --location --retry 3 --silent --show-error "$1" --output "$2"; }
extract_to() { # <archivo> <name> <dest>
  local archive="$1" name="$2" out="$3" tmp
  tmp="$(mktemp -d)"
  case "$archive" in
    *.zip)    unzip -o -q "$archive" -d "$tmp" ;;
    *.tar.xz) tar -xf "$archive" -C "$tmp" ;;
  esac
  cp "$(find "$tmp" -type f -name "$name" | head -n1)" "$out"
  rm -rf "$tmp"
}

# ---------------- yt-dlp ----------------
if [[ ! -f "$YTDLP_OUT" ]]; then
  case "$RID" in
    win-x64|win-arm64) asset="yt-dlp.exe" ;;
    osx-x64|osx-arm64) asset="yt-dlp_macos" ;;
    *) echo "RID no soportado: $RID" >&2; exit 1 ;;
  esac
  echo "· yt-dlp ($asset)"
  fetch "https://github.com/yt-dlp/yt-dlp/releases/latest/download/$asset" "$YTDLP_OUT"
  chmod +x "$YTDLP_OUT" 2>/dev/null || true
fi

# ---------------- ffmpeg ----------------
if [[ ! -f "$FFMPEG_OUT" ]]; then
  tmp="$(mktemp -d)"; trap 'rm -rf "$tmp"' EXIT
  case "$RID" in
    win-x64)
      echo "· ffmpeg (gyan.dev, essentials)"
      fetch "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip" "$tmp/ff.zip"
      extract_to "$tmp/ff.zip" ffmpeg.exe "$FFMPEG_OUT" ;;
    win-arm64)
      echo "· ffmpeg (BtbN, winarm64 lgpl)"
      fetch "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-winarm64-lgpl.zip" "$tmp/ff.zip"
      extract_to "$tmp/ff.zip" ffmpeg.exe "$FFMPEG_OUT" ;;
    osx-x64|osx-arm64)
      echo "· ffmpeg (evermeet.cx)"
      fetch "https://evermeet.cx/ffmpeg/getrelease/ffmpeg/zip" "$tmp/ff.zip"
      extract_to "$tmp/ff.zip" ffmpeg "$FFMPEG_OUT" ;;
  esac
  chmod +x "$FFMPEG_OUT" 2>/dev/null || true
fi

echo "Herramientas listas en $DEST"