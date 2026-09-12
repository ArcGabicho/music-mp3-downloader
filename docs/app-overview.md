# Perfil del proyecto — Music MP3 Downloader

## Resumen

**Music MP3 Downloader** es un **reproductor de música de escritorio** para **Windows y
macOS** construido con **.NET 10** y **.NET MAUI**. Reproduce la biblioteca local del
usuario (la carpeta de música del sistema) y, además, descarga audio de YouTube a MP3 de
forma 100 % local con `yt-dlp` + FFmpeg, dejándolo en esa misma carpeta.

Nació como un servicio web FastAPI, se migró a una aplicación de escritorio con Avalonia
UI para eliminar la necesidad de servidor y, después, a **.NET MAUI** — Avalonia soportaba
Linux pero MAUI no lo hace de escritorio, así que el soporte de Linux se retiró en esa
migración a cambio de reproducción de audio y empaquetado más alineados con el
ecosistema de Microsoft en Windows/macOS.

## Estado

En desarrollo temprano, pero funcional de extremo a extremo:

- **Reproductor** — UI de dos paneles (portada + lista de pistas + barra de reproducción
  con forma de onda). Escanea los MP3 de la carpeta de música, lee sus tags con TagLib#
  y reproduce con `Plugin.Maui.Audio`. Transporte: play/pausa, anterior/siguiente,
  buscar, volumen.
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
- RIDs soportados: `win-x64`, `win-arm64`, `osx-x64`, `osx-arm64` (la carpeta `osx-x64`
  se usa también para el bundle universal de Mac Catalyst).

### Reproducción de audio

`IAudioPlayer` la implementa `PluginMauiAudioPlayer`, que envuelve el paquete
`Plugin.Maui.Audio` (headless: no requiere montar ningún control en el árbol visual,
a diferencia de `CommunityToolkit.Maui.Views.MediaElement`). Sustituyó a LibVLCSharp
porque `VideoLAN.LibVLC.Mac` apunta a macOS de escritorio clásico, no al entorno
sandboxeado de Mac Catalyst.

### Dónde se guardan los archivos

El MP3 se guarda en la carpeta de música del usuario, resuelta por `IMusicLibrary`
según el sistema operativo:

| Plataforma          | Carpeta                                                          |
|---------------------|-------------------------------------------------------------------|
| Windows             | `SpecialFolder.MyMusic` (la carpeta «Música»/«Music» localizada) |
| macOS / Mac Catalyst| `~/Music`                                                        |

La base de datos SQLite guarda **solo los metadatos** de cada descarga
(`DownloadRecord`: URL, título, artista, ruta del archivo, tamaño, estado y fecha).
El archivo MP3 nunca se almacena en la base de datos. La ruta de la base de datos se
resuelve con `FileSystem.AppDataDirectory` (MAUI Essentials), portable entre plataformas.

## Características

- 🎧 **Reproductor local** — Recorre los MP3 de la carpeta de música, muestra portada y metadatos, y los reproduce con controles de transporte y barra de forma de onda.
- 🎵 **Descarga de MP3** — Extrae y convierte audio de vídeos de YouTube a MP3, 100 % en local, dentro de la propia carpeta de música.
- 🖥️ **Aplicación de escritorio nativa** — Sin navegador ni servidor: se ejecuta directamente en tu equipo.
- 🌐 **Windows y macOS** — Un mismo código base para ambas plataformas gracias a .NET MAUI.
- 🎨 **UI oscura y responsive** — Diseño de dos paneles que colapsa a uno solo en ventanas estrechas; pensado para pantalla maximizada.
- 📦 **Distribución autónoma** — Publicación self-contained, sin necesidad de instalar el runtime de .NET.

## Stack tecnológico

| Componente          | Tecnología                          |
|---------------------|-------------------------------------|
| **Runtime**         | .NET 10                             |
| **Lenguaje**        | C# 13                               |
| **UI**              | .NET MAUI (tema oscuro)             |
| **Patrón**          | MVVM (CommunityToolkit.Mvvm) + inyección de dependencias (Microsoft.Extensions.DependencyInjection) |
| **Reproducción**    | Plugin.Maui.Audio                   |
| **Metadatos MP3**   | TagLibSharp                         |
| **Persistencia**    | SQLite vía Entity Framework Core 10 |
| **Descarga**        | yt-dlp (binario empaquetado con la app) |
| **Conversión audio**| FFmpeg (binario empaquetado con la app) |
| **CI/CD**           | GitHub Actions                      |

## Estructura del proyecto

```
music-mp3-downloader/
├── MusicMp3Downloader.slnx              # Solución (formato XML .slnx)
├── core/                                # Proyecto MAUI (cabecera de la app)
│   ├── MusicMp3Downloader.App.csproj    # multi-target: net10.0-windows…, net10.0-maccatalyst
│   ├── MauiProgram.cs                   # Arranque de MAUI + contenedor de DI
│   ├── App.xaml / App.xaml.cs           # Recursos globales + CreateWindow
│   ├── Platforms/                       # Cabeceras nativas (Windows/WinUI3, MacCatalyst)
│   ├── Resources/                       # Icono de app y splash screen
│   ├── Styles/                          # Palette.xaml (recursos) + AppStyles.xaml (StyleClass)
│   ├── Views/                           # MainPage.xaml / .xaml.cs — UI de dos paneles + overlay de descarga
│   ├── Controls/                        # WaveformScrubber (GraphicsView, barra con forma de onda)
│   ├── Converters/                      # ByteArrayToImageSourceConverter, IsNotNullConverter, InvertedBoolConverter
│   ├── Services/                        # Implementaciones ligadas a MAUI: PluginMauiAudioPlayer, MauiUiDispatcher
│   ├── Core/                            # Class library independiente de MAUI
│   │   ├── MusicMp3Downloader.Core.csproj
│   │   ├── Models/                      # Track, DownloadItem, DownloadStatus
│   │   ├── ViewModels/                  # MainWindowViewModel, PlayerViewModel, TrackViewModel, DownloadItemViewModel
│   │   ├── Services/                    # ILibraryService, IAudioPlayer, IDownloadService, IExternalTools, IMusicLibrary, IAudioTagger, IUiDispatcher + impl. agnósticas de UI
│   │   └── Data/                        # AppDbContext (EF Core) y entidades persistidas
│   └── Tools/                           # fetch-tools.{sh,ps1} + binarios yt-dlp/ffmpeg (descargados, no versionados)
├── docs/                                # Documentación
├── test/                                # xUnit (referencia solo Core.csproj, sin dependencia de MAUI)
├── packaging/                           # windows/installer.iss (Inno Setup)
├── .github/workflows/                   # ci.yml (integración) y deploy.yml (publicación)
└── README.md
```

## Convenciones

- **Dos proyectos:** `core/Core/MusicMp3Downloader.Core.csproj` (class library `net10.0`
  normal, sin ninguna referencia a `Microsoft.Maui.*`) contiene toda la lógica
  (ViewModels, Services, Data, Models) para que `test/` pueda compilarla y probarla sin
  el workload de MAUI. `core/MusicMp3Downloader.App.csproj` (multi-target MAUI) contiene
  solo la capa de presentación: Views, estilos, el control de forma de onda y las
  implementaciones que sí necesitan tipos de MAUI (`PluginMauiAudioPlayer`,
  `MauiUiDispatcher`).
- **Sin tipos de MAUI en los ViewModels:** la portada se expone como `byte[]?` (no
  `ImageSource`); la conversión a imagen ocurre en la vista vía
  `ByteArrayToImageSourceConverter`. El marshalling al hilo de UI pasa por la interfaz
  propia `IUiDispatcher`, implementada en el proyecto MAUI sobre
  `Microsoft.Maui.Dispatching.IDispatcher`.
- **Inyección de dependencias:** los servicios se registran en
  `MauiProgram.ConfigureServices`; los ViewModels reciben sus dependencias por
  constructor.
- **Datos:** el `AppDbContext` se obtiene mediante `IDbContextFactory<AppDbContext>`
  (apto para apps de escritorio, sin ámbito ambiental). La base SQLite vive en
  `FileSystem.AppDataDirectory/app.db`.

## La interfaz

Dos paneles dentro de un `Grid` (`Views/MainPage.xaml`):

- **Panel izquierdo (portada)** — fondo con degradado rojo→azul; encima, la portada
  incrustada de la pista en reproducción si existe. Contiene la marca y, en la barra
  superior del panel derecho, el botón que abre el overlay de descarga. Se oculta cuando
  la ventana baja de 900 px de ancho (`MainPage.xaml.cs` → `OnSizeAllocated`/`IsWide`).
- **Panel derecho (lista)** — barra superior con el botón de descarga, cabecera
  `ARTISTA / AÑO` + título grande de la pista actual, la lista de pistas
  (`CollectionView`, tocar una fila reproduce) y, abajo, la barra de reproducción:
  transporte, `WaveformScrubber` y volumen.
- **Overlay de descarga** — capa modal con el campo de URL, el botón de descarga y la
  cola de descargas en curso.

Los botones de icono (transporte, cerrar, alternar overlay) son `Border` + `Path`
(vector) + `TapGestureRecognizer`, no `Button`, porque `Button` en MAUI solo admite texto
e imagen, no contenido arbitrario.

## Flujo de reproducción

1. Al arrancar, `MainWindowViewModel.LoadLibraryAsync` llama a `ILibraryService.ScanAsync`,
   que recorre `*.mp3` de la carpeta de música y crea un `TrackViewModel` por pista.
2. Tocar una fila ejecuta `PlayTrackCommand` → `PlayerViewModel.Play`.
3. `PlayerViewModel` llama a `IAudioPlayer.Play(filePath)` y un `System.Threading.Timer`
   de 250 ms, marshallado a UI vía `IUiDispatcher`, refresca posición, duración y
   `Progress` (0..1) para la forma de onda.
4. `WaveformScrubber` (un `GraphicsView`) dibuja barras deterministas a partir de
   `Track.Seed`; al tocar o arrastrar (`StartInteraction`/`DragInteraction`/
   `EndInteraction`) invoca `SeekCommand` con la fracción.
5. Al terminar una pista, `PlaybackEnded` se traslada al hilo de UI vía `IUiDispatcher` y
   pasa a la siguiente.

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
- [x] Migración de Avalonia UI a .NET MAUI (Windows + macOS/Mac Catalyst).
- [ ] Reporte de progreso robusto y cancelación desde la UI.
- [ ] Cola de reproducción editable, orden aleatorio y repetición.
- [ ] Migraciones de EF Core y pantalla de historial de descargas.
- [ ] Selección de carpeta de salida y calidad de audio.
- [x] Suite de pruebas xUnit en `test/` (ViewModels + resolución de carpeta de música).
- [ ] Empaquetado firmado por plataforma.
