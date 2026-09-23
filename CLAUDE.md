# CLAUDE.md

Guía para trabajar en este repositorio.

## Qué es

Aplicación de bandeja para **Windows** (.NET 10 + .NET MAUI + Blazor Hybrid) que descarga
audio de YouTube a MP3 **100 % en local** y lo reproduce. Sin servidor, sin nube. No hay
ventana de escritorio normal: la app arranca oculta y el ícono de la bandeja muestra/oculta
un popup emergente (como Mega/Discord). **Solo Windows**: el proyecto declara un único TFM,
`net10.0-windows10.0.19041.0` (Mac Catalyst se retiró).

## Estructura

```
MusicMp3Downloader.slnx          # solución (formato XML .slnx)
core/                            # proyecto MAUI (MusicMp3Downloader.App.csproj) — Components (Razor/CSS), wwwroot, Views, Platforms/
src/core/                        # class library sin MAUI (MusicMp3Downloader.Core.csproj) — ViewModels, Services, Data, Models
test/                            # xUnit, referencia solo src/core
packaging/windows/installer.iss  # Inno Setup (lo compila deploy.yml)
docs/app-overview.md             # perfil completo del proyecto
docs/ci-guide.md                 # workflows de GitHub Actions
CONTRIBUTING.md                  # flujo de contribución
```

## Comandos

```bash
dotnet workload restore MusicMp3Downloader.slnx            # una vez, instala el workload MAUI que falte
dotnet build MusicMp3Downloader.slnx -c Release             # compilar todo (App + Core + tests)
dotnet run --project core/MusicMp3Downloader.App.csproj -f net10.0-windows10.0.19041.0   # ejecutar (solo Windows)
dotnet test MusicMp3Downloader.slnx                          # tests (solo referencian Core, no requieren workload MAUI)
dotnet format MusicMp3Downloader.slnx --verify-no-changes    # estilo (la CI lo exige)
```

La CI ejecuta exactamente `build` + `test` + `format`, únicamente en runners Windows
(`windows-latest`); deben pasar los tres. La app es una aplicación de bandeja de Windows
(WinUI + H.NotifyIcon.Maui) y no tiene objetivo para otro sistema operativo.

## Arquitectura

Dos proyectos, separados a propósito para que `test/` no necesite el workload de MAUI:

- **`src/core/MusicMp3Downloader.Core.csproj`** — class library `net10.0` normal, **sin
  ninguna referencia a `Microsoft.Maui.*`**: `Models/`, `Services/` (interfaz `IFoo` +
  impl `Foo`), `Data/` (`AppDbContext` de EF Core), `ViewModels/` (derivan de
  `ViewModelBase`, usan los generadores de `CommunityToolkit.Mvvm`). Los ViewModels no
  pueden usar tipos de MAUI: la portada se expone como `byte[]?`, y el marshalling a UI
  pasa por la interfaz propia `IUiDispatcher`.
- **`core/MusicMp3Downloader.App.csproj`** — proyecto MAUI solo Windows
  (`net10.0-windows10.0.19041.0`) con SDK `Microsoft.NET.Sdk.Razor`
  (necesario para compilar `.razor`), referencia a `Core.csproj`. La UI es **Blazor
  Hybrid**, no XAML: `Components/Player.razor` + `Player.razor.css` (CSS isolation)
  implementan toda la interfaz en HTML/CSS/C#, montados dentro de un
  `BlazorWebView` (WebView2) alojado por `Views/MainPage.xaml` — la única página XAML
  real que queda es ese contenedor. `wwwroot/index.html` es la página host del
  `BlazorWebView`. `Platforms/` trae la cabecera Windows/WinUI3, y las
  implementaciones que sí necesitan MAUI: `PluginMauiAudioPlayer` (`IAudioPlayer`) y
  `MauiUiDispatcher` (`IUiDispatcher`).
- **Componentes Razor + ViewModels.** Los servicios y ViewModels se registran en
  `MauiProgram.ConfigureServices` (incluye `AddMauiBlazorWebView()`). `Player.razor`
  recibe `MainWindowViewModel` por `[Inject]` (el mismo singleton del contenedor de MAUI)
  y se suscribe a `PropertyChanged`/`CollectionChanged` en `OnInitialized()`, llamando a
  `StateHasChanged()` en cada cambio — Blazor no re-renderiza solo porque el ViewModel
  implemente `INotifyPropertyChanged`, hay que conectarlo a mano.
- Servicios clave: `ILibraryService` (escanea MP3 + tags), `IAudioPlayer`
  (`PluginMauiAudioPlayer`, sobre `Plugin.Maui.Audio`), `IDownloadService` (orquesta
  yt-dlp), `IWaveformService` (decodifica el MP3 con el FFmpeg empaquetado a picos de
  amplitud reales, cacheados en SQLite), `IMusicLibrary` (carpeta de música por SO),
  `IExternalTools` (localiza yt-dlp/ffmpeg).
- **Datos:** SQLite vía `IDbContextFactory<AppDbContext>` (sin ámbito ambiental, sin
  migraciones — `EnsureCreatedAsync` + `CREATE TABLE IF NOT EXISTS` puntual para tablas
  añadidas después de que la base ya existía). Guarda **solo metadatos**; el MP3 vive
  únicamente en el sistema de archivos, en la carpeta de música del usuario. La ruta de la
  base de datos usa `FileSystem.AppDataDirectory` (MAUI Essentials).
- **Bandeja e íconos:** `H.NotifyIcon.Maui` (`Views/TrayIconView.xaml`).
  Arranca oculta (se oculta en el primer `Activated` de la ventana, no antes — MAUI la
  muestra igual justo después de `OnWindowCreated`); clic izquierdo alterna
  mostrar/ocultar; clic derecho ofrece "Mostrar"/"Salir"; cerrar con la X oculta en vez de
  salir. La ventana es un popup sin marco (`OverlappedPresenter.SetBorderAndTitleBar(false,
  false)` + `DwmSetWindowAttribute(..., DWMWA_BORDER_COLOR, DWMWA_COLOR_NONE)` para quitar
  también el borde de sistema que dibuja DWM en Windows 11), tamaño fijo 440×620.

## Herramientas externas (yt-dlp + FFmpeg + Deno)

**Vienen empaquetadas; el usuario no instala nada.** `src/Tools/fetch-tools.{sh,ps1}`
descargan los binarios autónomos en `src/Tools/<rid>/` durante el build (target
`FetchExternalTools`, solo si faltan) y se copian junto al ejecutable en `tools/`.
En ejecución `IExternalTools` los resuelve desde ahí, con reserva al `PATH`.

- Compilar sin red: `dotnet build -p:BundleExternalTools=false`.
- `src/Tools/<rid>/` está en `.gitignore`; solo se versionan los scripts.
- Deno es el intérprete de JavaScript que yt-dlp necesita para resolver los desafíos de
  YouTube; `DownloadService` se lo pasa con `--js-runtimes deno:<ruta>`. Sin él, YouTube
  oculta formatos o bloquea videos enteros.
- RID empaquetado: `win-x64` (los scripts aceptan también `win-arm64`; los RIDs `osx-*`
  de `fetch-tools.sh` quedan de la época de Mac Catalyst; el build de Windows no los usa).

## Tests

xUnit en `test/`, con `ProjectReference` a `src/core/MusicMp3Downloader.Core.csproj`
(no al proyecto MAUI). Los ViewModels se prueban con fakes (`Fakes/FakeAudioPlayer.cs`,
`Fakes/ImmediateUiDispatcher.cs`, `Fakes/FakeWaveformService.cs`), sin runtime de MAUI ni
de Blazor. Añade pruebas junto con cualquier lógica no trivial.

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
- **Un `.xaml` bajo `Platforms/Windows/` no puede ser XAML de MAUI**: el SDK de MAUI quita
  incondicionalmente todo `.xaml` de esa carpeta del item `MauiXaml`, asumiendo que es
  XAML nativo de WinUI para `XamlCompiler.exe` (como `App.xaml`) — si se pone ahí un
  `ContentView` de MAUI, `XamlCompiler.exe` falla en silencio (exit code 1, sin mensaje).
  Cualquier vista de MAUI va en `Views/`.
- Blazor no re-renderiza un componente solo porque el ViewModel inyectado implemente
  `INotifyPropertyChanged`: hay que suscribirse a mano en `OnInitialized()` y llamar
  `StateHasChanged()` (ver `Player.razor`), y des-suscribirse en `Dispose()`
  (`@implements IDisposable`).
- El seek de la barra de progreso usa el `input[type=range]` nativo: `@oninput` marca
  `IsScrubbing = true` (arrastrando), `@onchange` ejecuta `SeekCommand` y limpia
  `IsScrubbing` (al soltar) — mismo patrón que `DragStarted`/`DragCompleted` tenía en XAML.
  `PlayerViewModel.Tick` no toca `Progress` mientras `IsScrubbing`.
- La forma de onda (`WaveformPeaks` en `PlayerViewModel`, vía `IWaveformService`) es
  decorativa: no controla el progreso, solo refleja la amplitud real del audio. La barra
  de progreso inferior sigue siendo el único control de seek.
- Publicar para Windows requiere `-p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true`
  para obtener una carpeta autónoma (sin MSIX) que `installer.iss` pueda envolver.
- No hace falta pasar `-r`/`--runtime`: el RID `win-x64` se resuelve solo, de forma
  implícita, al compilar/publicar con `-f net10.0-windows...` en una máquina Windows.
- El TFM `net10.0-windows...` **no compila en hosts que no son Windows**, ni con
  `EnableWindowsTargeting=true` (esa propiedad solo habilita el *restore*, no la
  compilación): el XAML de `Platforms/Windows/` pasa por `XamlCompiler.exe`
  (WindowsAppSDK), un binario de Windows. Como `App.csproj` es solo Windows, la solución
  completa solo compila en Windows; en otros sistemas usa `dotnet test test/` (Core no
  depende de MAUI).
