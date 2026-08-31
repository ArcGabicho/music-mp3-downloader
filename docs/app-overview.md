# Perfil del proyecto — Music MP3 Downloader

## Resumen

**Music MP3 Downloader** es una aplicación de escritorio multiplataforma construida con
**.NET 10** y **Avalonia UI**. Descarga el audio de una URL de YouTube, lo convierte a
MP3 con FFmpeg, le escribe los metadatos ID3 y registra la descarga en una base de datos
local SQLite.

Nació como un servicio web FastAPI y se migró a una aplicación nativa de escritorio para
eliminar la necesidad de servidor, base de datos remota y almacenamiento de objetos.

## Estado

En desarrollo temprano. La estructura MVVM, la inyección de dependencias y la capa de
datos están cableadas. El motor de descarga (`Services/DownloadService.cs`) invoca
`yt-dlp` para descargar el vídeo, extraer el audio y convertirlo a MP3; falta pulir el
reporte de progreso, la cancelación desde la UI y la pantalla de historial.

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

- 🎵 **Descarga de MP3** — Extrae y convierte audio de videos de YouTube a formato MP3.
- 🖥️ **Aplicación de escritorio nativa** — Sin navegador ni servidor: se ejecuta directamente en tu equipo.
- 🌐 **Multiplataforma** — Un mismo código base para Windows, macOS y Linux gracias a Avalonia UI.
- 🎨 **Tema Fluent** — Interfaz moderna con soporte para modo claro/oscuro siguiendo el tema del sistema.
- 📦 **Distribución autónoma** — Publicación como ejecutable único self-contained, sin necesidad de instalar el runtime de .NET.

## Stack tecnológico

| Componente          | Tecnología                          |
|---------------------|-------------------------------------|
| **Runtime**         | .NET 10                             |
| **Lenguaje**        | C# 13                               |
| **UI**              | Avalonia UI 12 + Fluent Theme       |
| **Patrón**          | MVVM (CommunityToolkit.Mvvm) + inyección de dependencias (Microsoft.Extensions.DependencyInjection) |
| **Tipografía**      | Inter (Avalonia.Fonts.Inter)        |
| **Persistencia**    | SQLite vía Entity Framework Core 10 |
| **Metadatos MP3**   | TagLibSharp                         |
| **Diagnóstico**     | AvaloniaUI.DiagnosticsSupport (solo Debug) |
| **Descarga**        | yt-dlp (proceso externo)            |
| **Conversión audio**| FFmpeg (dependencia externa de yt-dlp) |
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
│   ├── Styles/                          # Diccionarios de recursos y temas
│   ├── Views/                           # Ventanas y UserControls (.axaml)
│   │   └── MainWindow.axaml / .cs
│   ├── ViewModels/                      # ViewModelBase, MainWindowViewModel, DownloadItemViewModel
│   ├── Models/                          # DownloadItem, DownloadStatus
│   ├── Controls/                        # Controles personalizados reutilizables
│   ├── Services/                        # IDownloadService, IAudioTagger + implementaciones
│   └── Data/                            # AppDbContext (EF Core) y entidades persistidas
├── docs/                                # Documentación
├── test/                                # Proyectos de pruebas (pendiente)
├── .github/workflows/                   # ci.yml (integración) y deploy.yml (publicación)
└── README.md
```

## Convenciones

- **MVVM:** cada `FooViewModel` en `ViewModels/` se empareja con `Views/FooView.axaml` mediante `ViewLocator`. `MainWindow` se instancia directamente en `App.axaml.cs` con su `DataContext` resuelto desde el contenedor.
- **Inyección de dependencias:** los servicios se registran en `App.ConfigureServices`; los ViewModels reciben sus dependencias por constructor.
- **Datos:** el `AppDbContext` se obtiene mediante `IDbContextFactory<AppDbContext>` (apto para apps de escritorio, sin ámbito ambiental). La base SQLite vive en `%APPDATA%/MusicMp3Downloader/app.db` (o el equivalente por plataforma).

## Flujo de una descarga

1. El usuario pega una URL en `MainWindow` y ejecuta `DownloadCommand`.
2. `MainWindowViewModel` crea un `DownloadItemViewModel` y llama a `IDownloadService.DownloadAsync`, pasando un `IProgress<double>` para el avance.
3. `DownloadService` resuelve la carpeta de música con `IMusicLibrary` e invoca `yt-dlp` (`--extract-audio --audio-format mp3 --embed-metadata`) con la plantilla de salida `<carpeta>/%(title)s.%(ext)s`.
4. Se lee la línea `after_move:filepath` que imprime yt-dlp para conocer la ruta final; el progreso se parsea de las líneas `[download] NN%`.
5. Se materializa un `DownloadItem` (título, artista, ruta, tamaño, estado) y se guarda como `DownloadRecord` en SQLite mediante `IDbContextFactory` (`EnsureCreated` + `SaveChanges`). Solo metadatos: el MP3 vive únicamente en el sistema de archivos.
6. La UI refleja el estado (`DownloadStatus`) y el progreso en la lista de descargas.

## Roadmap

- [x] Motor de descarga con `yt-dlp` que guarda el MP3 en la carpeta de música del usuario.
- [ ] Reporte de progreso robusto y cancelación desde la UI.
- [ ] Migraciones de EF Core y pantalla de historial de descargas.
- [ ] Selección de carpeta de salida y calidad de audio.
- [ ] Suite de pruebas en `test/`.
- [ ] Empaquetado firmado por plataforma.