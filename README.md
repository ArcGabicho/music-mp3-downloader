# Music MP3 Downloader

![Wallpaper](https://i.imgur.com/2lcUDNj.png)

<img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET">
<img src="https://img.shields.io/badge/C%23-13-239120?logo=csharp" alt="C#">
<img src="https://img.shields.io/badge/Avalonia%20UI-12.1-8B44AC?logo=avalonia" alt="Avalonia UI">
<img src="https://img.shields.io/badge/cross--platform-Windows%20%7C%20macOS%20%7C%20Linux-informational" alt="Cross-platform">
<img src="https://img.shields.io/badge/license-MIT-green" alt="License">


**Music MP3 Downloader** es una aplicación de escritorio multiplataforma construida con **.NET 10** y **Avalonia UI** que permite descargar audio en formato MP3 a partir de URLs de YouTube. El proyecto migró de un servicio web FastAPI a una aplicación nativa de escritorio para funcionar sin servidor ni dependencias externas de infraestructura.

> ⚠️ **Estado:** el proyecto se encuentra en fase temprana de desarrollo. La interfaz y el motor de descarga están en construcción.

## ✨ Características

- 🎵 **Descarga de MP3** — Extrae y convierte audio de videos de YouTube a formato MP3.
- 🖥️ **Aplicación de escritorio nativa** — Sin navegador ni servidor: se ejecuta directamente en tu equipo.
- 🌐 **Multiplataforma** — Un mismo código base para Windows, macOS y Linux gracias a Avalonia UI.
- 🎨 **Tema Fluent** — Interfaz moderna con soporte para modo claro/oscuro siguiendo el tema del sistema.
- 📦 **Distribución autónoma** — Publicación como ejecutable único self-contained, sin necesidad de instalar el runtime de .NET.

## 🛠️ Stack Tecnológico

| Componente          | Tecnología                          |
|---------------------|-------------------------------------|
| **Runtime**         | .NET 10                             |
| **Lenguaje**        | C# 13                               |
| **UI**              | Avalonia UI 12 + Fluent Theme       |
| **Tipografía**      | Inter (Avalonia.Fonts.Inter)        |
| **Diagnóstico**     | AvaloniaUI.DiagnosticsSupport (solo Debug) |
| **Conversión audio**| FFmpeg (dependencia externa)        |
| **CI/CD**           | GitHub Actions                      |

## 📦 Estructura del Proyecto

```
music-mp3-downloader/
├── MusicMp3Downloader.slnx              # Solución (formato XML .slnx)
├── core/
│   └── MusicMp3Downloader.App/          # Proyecto de aplicación Avalonia
│       ├── App.axaml / App.axaml.cs     # Punto de arranque de la aplicación Avalonia
│       ├── MainWindow.axaml / .cs       # Ventana principal
│       ├── Program.cs                   # Entry point (AppBuilder + desktop lifetime)
│       ├── app.manifest                 # Manifiesto de aplicación (Windows)
│       └── MusicMp3Downloader.App.csproj
├── docs/                                # Documentación
├── test/                                # Proyectos de pruebas (pendiente)
├── .github/workflows/ci.yml             # Pipeline de integración continua
└── README.md
```

## 🚀 Inicio Rápido

### Prerrequisitos

- **.NET SDK 10.0+** — [Descargar](https://dotnet.microsoft.com/download/dotnet/10.0).
- **FFmpeg** — Necesario para la conversión de audio a MP3.

```bash
# Linux (Debian/Ubuntu)
sudo apt install ffmpeg

# macOS
brew install ffmpeg

# Windows
winget install ffmpeg
```

### 1. Clonar el repositorio

```bash
git clone https://github.com/ArcGabicho/music-mp3-downloader.git
cd music-mp3-downloader
```

### 2. Restaurar dependencias

```bash
dotnet restore MusicMp3Downloader.slnx
```

### 3. Ejecutar la aplicación

```bash
dotnet run --project core/MusicMp3Downloader.App/MusicMp3Downloader.App.csproj
```

## 🔨 Compilación

### Compilación de desarrollo

```bash
dotnet build MusicMp3Downloader.slnx --configuration Debug
```

### Publicación (ejecutable autónomo)

```bash
# Windows
dotnet publish core/MusicMp3Downloader.App/MusicMp3Downloader.App.csproj \
  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# macOS (Apple Silicon)
dotnet publish core/MusicMp3Downloader.App/MusicMp3Downloader.App.csproj \
  -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true

# Linux
dotnet publish core/MusicMp3Downloader.App/MusicMp3Downloader.App.csproj \
  -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

El binario resultante queda en `core/MusicMp3Downloader.App/bin/Release/net10.0/<rid>/publish/`.

## 🧪 Pruebas

```bash
dotnet test MusicMp3Downloader.slnx
```

Los proyectos de pruebas se ubicarán en `test/`. Actualmente no hay suites implementadas.

## 🔄 Integración Continua

El workflow [`.github/workflows/ci.yml`](.github/workflows/ci.yml) se ejecuta en cada `push` y `pull_request` sobre `master`:

| Job       | Descripción                                                                 |
|-----------|-----------------------------------------------------------------------------|
| `build`   | Restaura, compila y ejecuta las pruebas en `ubuntu-latest`, `windows-latest` y `macos-latest`. |
| `format`  | Verifica el estilo del código con `dotnet format --verify-no-changes`.     |
| `publish` | Solo en `push` a `master`: genera ejecutables self-contained para `linux-x64`, `win-x64` y `osx-arm64` y los sube como artefactos. |

## 🤝 Contribuciones

Las contribuciones son bienvenidas. Por favor:

1. Haz un fork del proyecto.
2. Crea una rama para tu feature (`git checkout -b feature/nueva-funcionalidad`).
3. Ejecuta `dotnet format` y `dotnet test` antes de hacer commit.
4. Haz commit de tus cambios (`git commit -m 'feat: agregar nueva funcionalidad'`).
5. Haz push a la rama (`git push origin feature/nueva-funcionalidad`).
6. Abre un Pull Request.

## 📄 Licencia

Este proyecto está licenciado bajo la Licencia MIT. Consulta el archivo [LICENSE.md](LICENSE.md) para más detalles.
