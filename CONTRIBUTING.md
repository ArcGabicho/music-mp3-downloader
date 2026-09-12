# Guía de contribución

Gracias por tu interés en contribuir a **Music MP3 Downloader**. Este documento describe
el flujo de trabajo, los estándares de código y qué se espera de una Pull Request.

## Requisitos previos

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download/dotnet/10.0)
- El workload de .NET MAUI: `dotnet workload install maui`
- Git
- Solo se compila/ejecuta en **Windows** o **macOS**; MAUI no soporta escritorio Linux.

`yt-dlp` y `FFmpeg` **no** hace falta instalarlos: el build los descarga como binarios
autónomos en `core/Tools/<rid>/` (ver [`core/Tools/README.md`](core/Tools/README.md)).
La primera compilación necesita conexión; para compilar sin red usa
`dotnet build -p:BundleExternalTools=false`.

Editor recomendado: Visual Studio 2022+ (con la carga de trabajo ".NET Multi-platform App UI")
o JetBrains Rider con el plugin de .NET MAUI.

## Puesta en marcha

```bash
git clone https://github.com/ArcGabicho/music-mp3-downloader.git
cd music-mp3-downloader
dotnet workload restore MusicMp3Downloader.slnx
dotnet restore MusicMp3Downloader.slnx
dotnet build MusicMp3Downloader.slnx
dotnet run --project core/MusicMp3Downloader.App.csproj -f net10.0-windows10.0.19041.0   # o net10.0-maccatalyst en macOS
```

Antes de escribir código, lee el [perfil del proyecto](docs/app-overview.md) para
entender la estructura MVVM, la inyección de dependencias y la capa de datos.

## Flujo de trabajo

1. Haz un fork del repositorio y clónalo.
2. Crea una rama a partir de `master`:
   - `feature/<descripcion-corta>` para nuevas funcionalidades
   - `fix/<descripcion-corta>` para correcciones
   - `docs/<descripcion-corta>` para documentación
3. Haz tus cambios en commits pequeños y enfocados.
4. Ejecuta las [comprobaciones locales](#comprobaciones-antes-del-commit).
5. Empuja la rama a tu fork y abre una Pull Request contra `master`.
6. Responde a los comentarios de la revisión; se hace *squash & merge* al aprobar.

## Comprobaciones antes del commit

La CI ejecuta exactamente estos comandos; córrelos en local para no romper el pipeline:

```bash
dotnet format MusicMp3Downloader.slnx --verify-no-changes
dotnet build MusicMp3Downloader.slnx --configuration Release
dotnet test MusicMp3Downloader.slnx
```

Si `dotnet format` reporta cambios, aplícalos con `dotnet format MusicMp3Downloader.slnx`.

## Compilación y publicación

Compilación de desarrollo:

```bash
dotnet build MusicMp3Downloader.slnx --configuration Debug
```

Publicación local de una carpeta autónoma (self-contained):

```bash
# Windows (el RID win-x64 se resuelve solo; no pasar -r explícito, ver Trampas conocidas)
dotnet publish core/MusicMp3Downloader.App.csproj \
  -c Release -f net10.0-windows10.0.19041.0 --self-contained true \
  -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true

# macOS (universal x64+arm64, produce un bundle .app)
dotnet publish core/MusicMp3Downloader.App.csproj \
  -c Release -f net10.0-maccatalyst
```

El resultado queda en `core/bin/Release/<tfm>/<rid>/publish/` (Windows) o como un `.app`
bajo `core/bin/Release/net10.0-maccatalyst/.../publish/` (macOS).

La publicación oficial (GitHub Releases) la automatiza `deploy.yml`; no crees tags ni
subas releases a mano. Ver [Guía de CI/CD](docs/ci-guide.md).

## Pruebas

```bash
dotnet test MusicMp3Downloader.slnx
```

Las pruebas (xUnit) están en `test/`. Los ViewModels se
prueban con fakes (`Fakes/FakeAudioPlayer.cs`); la lógica pura testeable se expone como
`internal` + `InternalsVisibleTo`. Añade pruebas junto con cualquier lógica no trivial.

## Integración continua

Cada `push` y `pull_request` sobre `master` dispara `.github/workflows/ci.yml`:

| Job       | Descripción                                                                 |
|-----------|---------------------------------------------------------------------------|
| `build`   | Restaura, compila y ejecuta las pruebas en `windows-latest` y `macos-latest`. |
| `format`  | Verifica el estilo con `dotnet format --verify-no-changes`.                |
| `publish` | Solo en `push` a `master`: genera binarios self-contained como artefactos. |

El detalle completo, incluido el workflow de despliegue, está en [docs/ci-guide.md](docs/ci-guide.md).

## Estilo de código

- Sigue las convenciones por defecto de `dotnet format` (basadas en `.editorconfig` cuando exista).
- `Nullable` está activado: no introduzcas advertencias de nulabilidad.
- Un tipo por archivo; el nombre del archivo coincide con el del tipo.
- El código se reparte en dos proyectos dentro de `core/`:
  - `core/Core/MusicMp3Downloader.Core.csproj` — class library sin dependencias de MAUI:
    `Models/`, `Services/`, `Data/`, `ViewModels/` (deriva de `ViewModelBase`; usa los
    generadores de `CommunityToolkit.Mvvm`: `[ObservableProperty]`, `[RelayCommand]`).
    `Services/` sigue el patrón interfaz `IFoo` + implementación `Foo`.
  - `core/` (proyecto MAUI) — `Views/` (`.xaml` + code-behind), `Styles/`, `Controls/`,
    `Converters/`, `MauiProgram.cs` (DI, `IServiceCollection`) y `Platforms/`.
- Usa enlaces compilados en XAML (`x:DataType`) y `StyleClass` para reutilizar estilos
  (ver `Styles/AppStyles.xaml`).

## Mensajes de commit

Se usa el formato *Conventional Commits* en español:

```
<tipo>: <resumen en imperativo>
```

Tipos habituales: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `build`, `ci`.

Ejemplos:

```
feat: implementar descarga con yt-dlp
fix: evitar excepción al pegar una URL vacía
docs: documentar el workflow de deploy
```

## Pull Requests

Una PR debe:

- Tener un objetivo único y acotado.
- Pasar CI (build + test + format) en Windows y macOS.
- Incluir pruebas en `test/` cuando añada lógica no trivial.
- Actualizar la documentación afectada (`README.md`, `docs/`).
- Describir **qué** cambia y **por qué**, y cómo probarlo.

No incluyas en la PR:

- Cambios de formato masivos no relacionados con el objetivo.
- Archivos generados (`bin/`, `obj/`, la base de datos local).
- Aumentos de versión ni tags: la publicación la gestiona `deploy.yml`.

## Reportar bugs y proponer mejoras

Abre un *issue* describiendo:

- Sistema operativo y arquitectura.
- Versión de .NET (`dotnet --info`) y de la app.
- Pasos para reproducir, resultado esperado y resultado obtenido.
- Logs o capturas si aplica.

## Licencia

Al contribuir aceptas que tu aportación se publique bajo la [Licencia MIT](LICENSE.md).