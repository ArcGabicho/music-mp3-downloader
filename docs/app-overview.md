# Perfil del proyecto — Music MP3 Downloader

## Resumen

**Music MP3 Downloader** es un **reproductor de música de escritorio** multiplataforma
construido con **.NET 10** y **Avalonia UI**. Reproduce la biblioteca local del usuario
(la carpeta de música del sistema) y, además, descarga audio de YouTube a MP3 de forma
100 % local con `yt-dlp` + FFmpeg, dejándolo en esa misma carpeta.

Nació como un servicio web FastAPI y se migró a una aplicación nativa de escritorio para
eliminar la necesidad de servidor, base de datos remota y almacenamiento de objetos.

## Estado

En desarrollo temprano, pero funcional de extremo a extremo:

- **Reproductor** — UI de dos paneles (portada + lista de pistas + barra de reproducción
  con forma de onda). Escanea los MP3 de la carpeta de música, lee sus tags con TagLib#
  y reproduce con LibVLC. Transporte: play/pausa, anterior/siguiente, buscar, volumen.
- **Descarga** — panel accesible desde el botón «DESCARGAR»/menú; invoca `yt-dlp`, y al
  terminar reescanea la biblioteca para que la pista nueva aparezca en la lista.
- Pendiente: cancelación de descargas desde la UI, pantalla de historial, ajustes.

### Herramientas externas empaquetadas

**yt-dlp** y **FFmpeg** vienen **incluidos** en la app; el usuario no instala nada.
Los scripts `core/Tools/fetch-tools.{sh,ps1}` descargan los binarios autónomos en
`core/Tools/<rid>/` durante la compilación (target `FetchExternalTools` del `.csproj`,
solo si faltan) y se copian a `bin/…/tools/` y al publicado. En ejecución,
`IExternalTools` los resuelve desde `<carpeta del ejecutable>/tools/`, y si no
estuvieran recurre al `PATH`. `DownloadService` invoca ese yt-dlp y le pasa
`--ffmpeg-location` apuntando al FFmpeg empaquetado.

- Compilar sin red: `dotnet build -p:BundleExternalTools=false`.
- Se usa `releases/latest` a propósito (yt-dlp necesita actualizarse a menudo).
- Peso aproximado por plataforma: ~120 MB (yt-dlp ~40 MB + FFmpeg ~80 MB).

### Reproducción de audio

`IAudioPlayer` la implementa `LibVlcAudioPlayer`. En Windows y macOS los binarios nativos
de LibVLC llegan por NuGet (`VideoLAN.LibVLC.*`, referenciados solo para ese RID); en
**Linux se usa la `libvlc` del sistema** (`pacman -S vlc` / `apt install vlc`) — es la
única dependencia externa que el usuario de Linux instala. Si el motor nativo no está
disponible, `IsAvailable` es `false`, la app sigue abriendo y la barra muestra un aviso
en lugar de fallar.

### Dónde se guardan los archivos

El MP3 se guarda en la carpeta de música del usuario, resuelta por `IMusicLibrary`
según el sistema operativo:

| Plataforma | Carpeta                                                                       |
|------------|-----------------------------------------------------------------------------|
| Windows    | `SpecialFolder.MyMusic` (la carpeta «Música»/«Music» localizada)            |
| macOS      | `~/Music`                                                                    |
| Linux      | `XDG_MUSIC_DIR` (env o `~/.config/user-dirs.dirs`), p. ej. `~/Música`; si no hay configuración XDG, `~/Música` o `~/Music` |

La base de datos SQLite guarda **solo los metadatos** de cada descarga
(`DownloadRecord`: URL, título, artista, ruta del archivo, tamaño, estado y fecha).
El archivo MP3 nunca se almacena en la base de datos.

## Características

- 🎧 **Reproductor local** — Recorre los MP3 de la carpeta de música, muestra portada y metadatos, y los reproduce con controles de transporte y barra de forma de onda.
- 🎵 **Descarga de MP3** — Extrae y convierte audio de vídeos de YouTube a MP3, 100 % en local, dentro de la propia carpeta de música.
- 🖥️ **Aplicación de escritorio nativa** — Sin navegador ni servidor: se ejecuta directamente en tu equipo.
- 🌐 **Multiplataforma** — Un mismo código base para Windows, macOS y Linux gracias a Avalonia UI.
- 🎨 **UI oscura y responsive** — Diseño de dos paneles que colapsa a uno solo en ventanas estrechas; pensado para pantalla maximizada.
- 📦 **Distribución autónoma** — Publicación como ejecutable único self-contained, sin necesidad de instalar el runtime de .NET.

## Stack tecnológico

| Componente          | Tecnología                          |
|---------------------|-------------------------------------|
| **Runtime**         | .NET 10                             |
| **Lenguaje**        | C# 13                               |
| **UI**              | Avalonia UI 12 + Fluent Theme (variante oscura) |
| **Patrón**          | MVVM (CommunityToolkit.Mvvm) + inyección de dependencias (Microsoft.Extensions.DependencyInjection) |
| **Tipografía**      | Inter (Avalonia.Fonts.Inter)        |
| **Reproducción**    | LibVLCSharp (LibVLC; nativo por NuGet en Windows/macOS, del sistema en Linux) |
| **Metadatos MP3**   | TagLibSharp                         |
| **Persistencia**    | SQLite vía Entity Framework Core 10 |
| **Diagnóstico**     | AvaloniaUI.DiagnosticsSupport (solo Debug) |
| **Descarga**        | yt-dlp (binario empaquetado con la app) |
| **Conversión audio**| FFmpeg (binario empaquetado con la app) |
| **CI/CD**           | GitHub Actions                      |

## Estructura del proyecto

```
music-mp3-downloader/
├── MusicMp3Downloader.slnx              # Solución (formato XML .slnx)
├── core/                                # Proyecto de aplicación Avalonia
│   ├── MusicMp3Downloader.App.csproj
│   ├── Program.cs                       # Entry point (AppBuilder + desktop lifetime)
│   ├── App.axaml / App.axaml.cs         # Arranque de Avalonia + contenedor de DI
│   ├── ViewLocator.cs                   # Resuelve View a partir del ViewModel
│   ├── app.manifest                     # Manifiesto de aplicación (Windows)
│   ├── Assets/                          # Iconos, imágenes y fuentes empaquetadas
│   ├── Styles/                          # Palette.axaml (recursos) + AppStyles.axaml (estilos)
│   ├── Views/                           # Ventanas y UserControls (.axaml)
│   │   └── MainWindow.axaml / .cs       # UI de dos paneles + overlay de descarga
│   ├── ViewModels/                      # MainWindowViewModel, PlayerViewModel, TrackViewModel, DownloadItemViewModel
│   ├── Models/                          # Track, DownloadItem, DownloadStatus
│   ├── Controls/                        # WaveformScrubber (barra de progreso con forma de onda)
│   ├── Services/                        # ILibraryService, IAudioPlayer, IDownloadService, IExternalTools, IMusicLibrary, IAudioTagger + impl.
│   ├── Data/                            # AppDbContext (EF Core) y entidades persistidas
│   └── Tools/                           # fetch-tools.{sh,ps1} + binarios yt-dlp/ffmpeg (descargados, no versionados)
├── docs/                                # Documentación
├── test/   # xUnit (ViewModels con fakes + lógica pura)
├── .github/workflows/                   # ci.yml (integración) y deploy.yml (publicación)
└── README.md
```

## Convenciones

- **MVVM:** cada `FooViewModel` en `ViewModels/` se empareja con `Views/FooView.axaml` mediante `ViewLocator`. `MainWindow` se instancia directamente en `App.axaml.cs` con su `DataContext` resuelto desde el contenedor.
- **Inyección de dependencias:** los servicios se registran en `App.ConfigureServices`; los ViewModels reciben sus dependencias por constructor.
- **Datos:** el `AppDbContext` se obtiene mediante `IDbContextFactory<AppDbContext>` (apto para apps de escritorio, sin ámbito ambiental). La base SQLite vive en `%APPDATA%/MusicMp3Downloader/app.db` (o el equivalente por plataforma).

## La interfaz

Dos paneles dentro de un `Grid` (`MainWindow.axaml`):

- **Panel izquierdo (portada)** — fondo con degradado rojo→azul; encima, la portada
  incrustada de la pista en reproducción si existe. Contiene la marca, un botón
  **DESCARGAR** que abre el overlay, **PLAY ALL** y el contador de pistas (`#N`). Se
  oculta cuando la ventana baja de 900 px de ancho (`MainWindow.axaml.cs` → `IsWide`).
- **Panel derecho (lista)** — barra superior con menú (abre el overlay), cabecera
  `ARTISTA / AÑO` + título grande de la pista actual, la lista de pistas
  (clic = reproducir) y, abajo, la barra de reproducción: transporte, `WaveformScrubber`
  y volumen.
- **Overlay de descarga** — capa modal con el campo de URL, el botón de descarga y la
  cola de descargas en curso.

## Flujo de reproducción

1. Al arrancar, `MainWindowViewModel.LoadLibraryAsync` llama a `ILibraryService.ScanAsync`,
   que recorre `*.mp3` de la carpeta de música y crea un `TrackViewModel` por pista.
2. Un clic en una fila (o **PLAY ALL**) ejecuta `PlayTrackCommand` → `PlayerViewModel.Play`.
3. `PlayerViewModel` llama a `IAudioPlayer.Play(filePath)` y un `DispatcherTimer` de 250 ms
   refresca posición, duración y `Progress` (0..1) para la forma de onda.
4. `WaveformScrubber` dibuja barras deterministas a partir de `Track.Seed`; al hacer clic
   o arrastrar invoca `SeekCommand` con la fracción.
5. Al terminar una pista, `PlaybackEnded` se traslada al hilo de UI y pasa a la siguiente.

## Flujo de una descarga

1. El usuario abre el overlay, pega una URL y ejecuta `DownloadCommand`.
2. `MainWindowViewModel` crea un `DownloadItemViewModel` y llama a `IDownloadService.DownloadAsync`, pasando un `IProgress<double>` para el avance.
3. `DownloadService` resuelve la carpeta de música con `IMusicLibrary` e invoca `yt-dlp` (`--extract-audio --audio-format mp3 --embed-metadata`) con la plantilla de salida `<carpeta>/%(title)s.%(ext)s`.
4. Se lee la línea `after_move:filepath` que imprime yt-dlp para conocer la ruta final; el progreso se parsea de las líneas `[download] NN%`.
5. Se materializa un `DownloadItem` (título, artista, ruta, tamaño, estado) y se guarda como `DownloadRecord` en SQLite mediante `IDbContextFactory` (`EnsureCreated` + `SaveChanges`). Solo metadatos: el MP3 vive únicamente en el sistema de archivos.
6. La UI refleja el estado y el progreso en la cola del overlay, y `MainWindowViewModel` vuelve a llamar a `LoadLibraryAsync` para que la pista nueva aparezca en la lista.

## Roadmap

- [x] Motor de descarga con `yt-dlp` que guarda el MP3 en la carpeta de música del usuario.
- [x] Reproductor local con UI de dos paneles, lista de biblioteca y barra de reproducción.
- [ ] Reporte de progreso robusto y cancelación desde la UI.
- [ ] Cola de reproducción editable, orden aleatorio y repetición.
- [ ] Migraciones de EF Core y pantalla de historial de descargas.
- [ ] Selección de carpeta de salida y calidad de audio.
- [x] Suite de pruebas xUnit en `test/` (ViewModels + resolución de carpeta de música).
- [ ] Empaquetado firmado por plataforma.