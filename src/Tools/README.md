# Tools/

Binarios externos que la app **empaqueta** para que el usuario final no tenga que
instalar nada:

- **yt-dlp** — descarga de audio desde YouTube.
- **ffmpeg** — conversión a MP3.
- **deno** — intérprete de JavaScript que yt-dlp usa para resolver los desafíos de
  YouTube (sin él, YouTube oculta formatos o bloquea videos).

## Cómo se obtienen

Los scripts `fetch-tools.sh` (macOS) y `fetch-tools.ps1` (Windows) descargan los
binarios autónomos en `src/Tools/<rid>/`. Se ejecutan **automáticamente durante la
compilación** (target `FetchExternalTools` del `.csproj`) para el RID que se esté
compilando, y solo si aún no están descargados.

A mano:

```bash
bash src/Tools/fetch-tools.sh osx-arm64
```

```powershell
powershell -ExecutionPolicy Bypass -File src/Tools/fetch-tools.ps1 -Rid win-x64
```

RIDs soportados: `win-x64`, `win-arm64`, `osx-x64`, `osx-arm64` (Linux se retiró al migrar
a .NET MAUI, que no soporta escritorio Linux).

## Notas

- Las carpetas `src/Tools/<rid>/` **no** se versionan (`.gitignore`); solo los scripts.
- Se usa `releases/latest` a propósito: yt-dlp necesita actualizaciones frecuentes para
  seguir el ritmo de los cambios de YouTube.
- Fuentes: yt-dlp (releases oficiales), ffmpeg de [gyan.dev]/[BtbN/FFmpeg-Builds] en
  Windows y [evermeet.cx] en macOS; deno de sus [releases oficiales][deno].
- Para compilar sin red: `dotnet build -p:BundleExternalTools=false` (la app recurrirá
  entonces a un yt-dlp/ffmpeg del `PATH`, si existe).

[gyan.dev]: https://www.gyan.dev/ffmpeg/builds/
[BtbN/FFmpeg-Builds]: https://github.com/BtbN/FFmpeg-Builds
[evermeet.cx]: https://evermeet.cx/ffmpeg/
[deno]: https://github.com/denoland/deno/releases