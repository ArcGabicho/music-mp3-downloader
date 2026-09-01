# tools/

Binarios externos que la app **empaqueta** para que el usuario final no tenga que
instalar nada:

- **yt-dlp** — descarga de audio desde YouTube.
- **ffmpeg** / **ffprobe** — conversión a MP3.

## Cómo se obtienen

Los scripts `fetch-tools.sh` (Linux/macOS) y `fetch-tools.ps1` (Windows) descargan los
binarios autónomos en `tools/<rid>/`. Se ejecutan **automáticamente durante la
compilación** (target `BundleExternalTools` del `.csproj`) para el RID que se esté
compilando, y solo si aún no están descargados.

A mano:

```bash
bash core/Tools/fetch-tools.sh linux-x64
```

```powershell
powershell -ExecutionPolicy Bypass -File core/Tools/fetch-tools.ps1 -Rid win-x64
```

RIDs soportados: `linux-x64`, `linux-arm64`, `win-x64`, `win-arm64`, `osx-x64`, `osx-arm64`.

## Notas

- Las carpetas `tools/<rid>/` **no** se versionan (`.gitignore`); solo los scripts.
- Se usa `releases/latest` a propósito: yt-dlp necesita actualizaciones frecuentes para
  seguir el ritmo de los cambios de YouTube.
- Fuentes: yt-dlp (releases oficiales), ffmpeg de [BtbN/FFmpeg-Builds] en Linux/Windows y
  [evermeet.cx] en macOS.
- Para compilar sin red: `dotnet build -p:BundleExternalTools=false` (la app recurrirá
  entonces a un yt-dlp/ffmpeg del `PATH`, si existe).

[BtbN/FFmpeg-Builds]: https://github.com/BtbN/FFmpeg-Builds
[evermeet.cx]: https://evermeet.cx/ffmpeg/