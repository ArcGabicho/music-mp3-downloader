# CLAUDE.md

Guía para trabajar en este repositorio.

## Qué es

Reproductor de música de escritorio para **Windows y macOS** (.NET 10 + .NET MAUI) que,
además, descarga audio de YouTube a MP3 **100 % en local**. Sin servidor, sin nube.
MAUI no soporta escritorio Linux, así que la app no se compila ni distribuye ahí.

## Estructura

```
MusicMp3Downloader.slnx          # solución (formato XML .slnx)
core/                            # proyecto MAUI (MusicMp3Downloader.App.csproj) — Views, Styles, Platforms/
core/Core/                       # class library sin MAUI (MusicMp3Downloader.Core.csproj) — ViewModels, Services, Data, Models
test/                            # xUnit, referencia solo core/Core
packaging/windows/installer.iss  # Inno Setup (lo compila deploy.yml)
docs/app-overview.md             # perfil completo del proyecto
docs/ci-guide.md                 # workflows de GitHub Actions
CONTRIBUTING.md                  # flujo de contribución
```

## Comandos

```bash
dotnet workload restore MusicMp3Downloader.slnx            # una vez, instala el workload MAUI que falte
dotnet build MusicMp3Downloader.slnx -c Release             # compilar todo (App + Core + tests)
dotnet run --project core/MusicMp3Downloader.App.csproj -f net10.0-windows10.0.19041.0   # ejecutar (Windows); usa net10.0-maccatalyst en macOS
dotnet test MusicMp3Downloader.slnx                          # tests (solo referencian Core, no requieren workload MAUI)
dotnet format MusicMp3Downloader.slnx --verify-no-changes    # estilo (la CI lo exige)
```

La CI ejecuta exactamente `build` + `test` + `format` en Windows y macOS; deben pasar los tres.

## Arquitectura

Dos proyectos dentro de `core/`, separados a propósito para que `test/` no necesite el
workload de MAUI:

- **`core/Core/MusicMp3Downloader.Core.csproj`** — class library `net10.0` normal, **sin
  ninguna referencia a `Microsoft.Maui.*`**: `Models/`, `Services/` (interfaz `IFoo` +
  impl `Foo`), `Data/` (`AppDbContext` de EF Core), `ViewModels/` (derivan de
  `ViewModelBase`, usan los generadores de `CommunityToolkit.Mvvm`). Los ViewModels no
  pueden usar tipos de MAUI: la portada se expone como `byte[]?` (no `ImageSource`), y el
  marshalling a UI pasa por la interfaz propia `IUiDispatcher`.
- **`core/MusicMp3Downloader.App.csproj`** — proyecto MAUI multi-target
  (`net10.0-windows10.0.19041.0;net10.0-maccatalyst`), referencia a `Core.csproj`:
  `Views/` (`.xaml` + code-behind), `Styles/` (`Palette.xaml` recursos +
  `AppStyles.xaml` con `StyleClass`), `Controls/` (`WaveformScrubber`, un `GraphicsView`),
  `Converters/`, `Platforms/` (cabeceras Windows/WinUI3 y MacCatalyst), y las
  implementaciones que sí necesitan MAUI: `PluginMauiAudioPlayer` (`IAudioPlayer`) y
  `MauiUiDispatcher` (`IUiDispatcher`).
- **MVVM + inyección de dependencias.** Los servicios y ViewModels se registran en
  `MauiProgram.ConfigureServices`. `App.xaml.cs → CreateWindow` crea la ventana con
  `MainPage` resuelto del contenedor.
- Servicios clave: `ILibraryService` (escanea MP3 + tags), `IAudioPlayer`
  (`PluginMauiAudioPlayer`, sobre `Plugin.Maui.Audio` — reemplazó a LibVLC porque
  `VideoLAN.LibVLC.Mac` no soporta Mac Catalyst), `IDownloadService` (orquesta yt-dlp),
  `IMusicLibrary` (carpeta de música por SO), `IExternalTools` (localiza yt-dlp/ffmpeg).
- **Datos:** SQLite vía `IDbContextFactory<AppDbContext>` (sin ámbito ambiental). Guarda
  **solo metadatos** de las descargas; el MP3 vive únicamente en el sistema de archivos,
  en la carpeta de música del usuario (`SpecialFolder.MyMusic` en Windows, `~/Music` en
  macOS). La ruta de la base de datos usa `FileSystem.AppDataDirectory` (MAUI Essentials).

## Herramientas externas (yt-dlp + FFmpeg)

**Vienen empaquetadas; el usuario no instala nada.** `core/Tools/fetch-tools.{sh,ps1}`
descargan los binarios autónomos en `core/Tools/<rid>/` durante el build (target
`FetchExternalTools`, solo si faltan) y se copian junto al ejecutable en `tools/`.
En ejecución `IExternalTools` los resuelve desde ahí, con reserva al `PATH`.

- Compilar sin red: `dotnet build -p:BundleExternalTools=false`.
- `core/Tools/<rid>/` está en `.gitignore`; solo se versionan los scripts.
- RIDs soportados: `win-x64`, `win-arm64`, `osx-x64`, `osx-arm64` (Linux se quitó de
  ambos scripts al migrar a MAUI).

## Tests

xUnit en `test/`, con `ProjectReference` a `core/Core/MusicMp3Downloader.Core.csproj`
(no al proyecto MAUI). Los ViewModels se prueban con fakes
(`Fakes/FakeAudioPlayer.cs`, `Fakes/ImmediateUiDispatcher.cs`), sin runtime de MAUI.
Añade pruebas junto con cualquier lógica no trivial.

## Convenciones

- **Commits:** Conventional Commits en español — `feat:`, `fix:`, `docs:`, `refactor:`,
  `test:`, `chore:`, `build:`, `ci:`.
- Un tipo por archivo; nombre de archivo = nombre del tipo. `Nullable` activado, sin
  warnings de nulabilidad. Enlaces XAML compilados (`x:DataType`).
- `dotnet format` debe quedar limpio antes de commitear.
- No commitees `bin/`, `obj/`, `core/Tools/<rid>/` ni la base local. La publicación de
  releases la gestiona `deploy.yml`; no crees tags a mano.

## Trampas conocidas

- El `restore` de NuGet **no es por configuración**: no condiciones metadatos de
  `PackageReference` (`IncludeAssets`, etc.) sobre `$(Configuration)` — se hornea una vez
  y el IDE/Release ven un estado inconsistente.
- `core/Tools/` es **PascalCase**; mantén la ruta exacta en `.csproj`, `.gitignore` y docs.
- **No dupliques el globbing entre proyectos:** `core/MusicMp3Downloader.App.csproj` vive
  en el mismo directorio que `core/Core/`, así que su `.csproj` excluye explícitamente
  `Core\**` (`Compile`/`None`/`MauiXaml`/`EmbeddedResource Remove`) — si no, compila dos
  veces las mismas clases y duplica los atributos de ensamblado (`error CS0579`).
- `Button` de MAUI solo admite `Text`/`ImageSource`, no contenido arbitrario: los iconos
  vectoriales (transporte, cerrar, alternar overlay) son `Border` + `Path` (`Data=`) +
  `TapGestureRecognizer`, con las clases de estilo `iconGhost`/`transport`/`transportMain`
  en `AppStyles.xaml` apuntando a `Border`, no a `Button`.
- `WaveformScrubber` es un `GraphicsView`/`IDrawable` (no un `Control.Render` como en
  Avalonia); usa los eventos `StartInteraction`/`DragInteraction`/`EndInteraction` de
  `GraphicsView` para el *scrubbing*, y solo llama a `SeekCommand` al soltar.
  `PlayerViewModel.Tick` no toca `Progress` mientras `IsScrubbing`.
- macOS/Mac Catalyst: la app **no** usa App Sandbox (no hay `Entitlements.plist` en
  `Platforms/MacCatalyst/`) a propósito, porque necesita acceso directo a `~/Music` sin
  pasar por un selector de archivos; eso también significa que no se puede distribuir vía
  Mac App Store sin rediseñar ese acceso.
- Publicar para Windows requiere `-p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true`
  para obtener una carpeta autónoma (sin MSIX) que `installer.iss` pueda envolver.
