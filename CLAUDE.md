# CLAUDE.md

Guía para trabajar en este repositorio.

## Qué es

Reproductor de música de escritorio **multiplataforma** (.NET 10 + Avalonia UI 12) que,
además, descarga audio de YouTube a MP3 **100 % en local**. Sin servidor, sin nube.

## Estructura

```
MusicMp3Downloader.slnx          # solución (formato XML .slnx)
core/                            # el proyecto de la app (MusicMp3Downloader.App.csproj)
test/                            # xUnit
packaging/windows/installer.iss  # Inno Setup (lo compila deploy.yml)
packaging/aur/                   # PKGBUILD + .desktop de referencia para el AUR
docs/app-overview.md             # perfil completo del proyecto
docs/ci-guide.md                 # workflows de GitHub Actions
CONTRIBUTING.md                  # flujo de contribución
```

## Comandos

```bash
dotnet build MusicMp3Downloader.slnx -c Release       # compilar todo
dotnet run --project core/MusicMp3Downloader.App.csproj   # ejecutar la app (desde la raíz)
dotnet test MusicMp3Downloader.slnx                   # tests
dotnet format MusicMp3Downloader.slnx --verify-no-changes   # estilo (la CI lo exige)
```

La CI ejecuta exactamente `build` + `test` + `format`; deben pasar los tres.

## Arquitectura (dentro de `core/`)

- **MVVM + inyección de dependencias.** Los servicios y ViewModels se registran en
  `App.axaml.cs → ConfigureServices`. `MainWindow` se crea ahí con su `DataContext`
  resuelto del contenedor. `ViewLocator` mapea `…ViewModels.FooViewModel` → `…Views.FooView`.
- Carpetas: `Views/` (.axaml + code-behind), `ViewModels/` (derivan de `ViewModelBase`,
  usan los generadores de `CommunityToolkit.Mvvm`), `Models/` (POCOs), `Services/`
  (interfaz `IFoo` + impl `Foo`), `Data/` (`AppDbContext` de EF Core), `Controls/`,
  `Styles/` (`Palette.axaml` recursos + `AppStyles.axaml` estilos), `Tools/` (ver abajo).
- Servicios clave: `ILibraryService` (escanea MP3 + tags), `IAudioPlayer`
  (`LibVlcAudioPlayer`), `IDownloadService` (orquesta yt-dlp), `IMusicLibrary`
  (carpeta de música por SO), `IExternalTools` (localiza yt-dlp/ffmpeg).
- **Datos:** SQLite vía `IDbContextFactory<AppDbContext>` (sin ámbito ambiental). Guarda
  **solo metadatos** de las descargas; el MP3 vive únicamente en el sistema de archivos,
  en la carpeta de música del usuario (`~/Música` en Linux, `SpecialFolder.MyMusic` en
  Windows, `~/Music` en macOS).

## Herramientas externas (yt-dlp + FFmpeg)

**Vienen empaquetadas; el usuario no instala nada.** `core/Tools/fetch-tools.{sh,ps1}`
descargan los binarios autónomos en `core/Tools/<rid>/` durante el build (target
`FetchExternalTools`, solo si faltan) y se copian junto al ejecutable en `tools/`.
En ejecución `IExternalTools` los resuelve desde ahí, con reserva al `PATH`.

- Compilar sin red: `dotnet build -p:BundleExternalTools=false`.
- `core/Tools/<rid>/` está en `.gitignore`; solo se versionan los scripts.
- **Reproducción:** LibVLC. En Windows/macOS el nativo llega por NuGet; en **Linux hace
  falta `vlc` (libvlc) del sistema** — es la única dependencia que instala el usuario Linux.

## Tests

xUnit en `test/`. Los ViewModels se prueban con fakes (`Fakes/FakeAudioPlayer.cs`), sin
Avalonia headless. Lógica pura testeable expuesta como `internal` +
`InternalsVisibleTo` (ver `MusicLibrary.ParseXdgUserDirs`). Añade pruebas junto con
cualquier lógica no trivial.

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
- `core/Tools/` es **PascalCase** y Linux distingue mayúsculas; mantén la ruta exacta en
  `.csproj`, `.gitignore` y docs.
- Avalonia: la propiedad es `StrokeJoin` (no `StrokeLineJoin`); `TextBox` usa
  `PlaceholderText` (no `Watermark`, obsoleto en Avalonia 12).
- `WaveformScrubber` solo hace *seek* al soltar el ratón (no en cada `PointerMoved`) para
  no inundar a LibVLC de búsquedas; `PlayerViewModel.Tick` no toca `Progress` mientras
  `IsScrubbing`.
