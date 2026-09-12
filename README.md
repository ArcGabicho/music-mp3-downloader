# Music MP3 Downloader

![Wallpaper](https://i.imgur.com/2lcUDNj.png)

<a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET"></a>
<a href="https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-13"><img src="https://img.shields.io/badge/C%23-13-239120?logo=csharp" alt="C#"></a>
<a href="https://learn.microsoft.com/dotnet/maui/"><img src="https://img.shields.io/badge/.NET%20MAUI-9-512BD4?logo=dotnet" alt=".NET MAUI"></a>
<a href="https://github.com/ArcGabicho/music-mp3-downloader"><img src="https://img.shields.io/badge/cross--platform-Windows%20%7C%20macOS-informational" alt="Cross-platform"></a>
<a href="LICENSE.md"><img src="https://img.shields.io/badge/license-MIT-green" alt="License"></a>

**Music MP3 Downloader** es una aplicación de escritorio construida con **.NET 10** y **.NET MAUI**
para Windows y macOS (Mac Catalyst) que permite descargar audio en formato MP3 a partir de URLs de
YouTube además de reproducirlas en una UI moderna.

---

#### Instalación en Windows

```powershell
iwr https://github.com/ArcGabicho/music-mp3-downloader/releases/latest/download/MusicMp3Downloader-Setup-x64.exe -OutFile MusicMp3Downloader-Setup.exe; .\MusicMp3Downloader-Setup.exe
```

> [!WARNING]
> Necesitas Windows 10/11 (incluye PowerShell y `iwr`) y conexión a internet. `yt-dlp` y `ffmpeg` vienen incluidos; no hay que instalar nada aparte.
> El comando descarga el instalador de la última release y lo ejecuta (instalación por usuario, sin permisos de administrador). Como el ejecutable no está firmado, SmartScreen puede avisar: **Más información → Ejecutar de todas formas**.

#### Instalación en macOS

Descarga el `.zip` de la [última release](https://github.com/ArcGabicho/music-mp3-downloader/releases/latest)
(`MusicMp3Downloader-*-maccatalyst.zip`), descomprímelo y mueve `Music MP3 Downloader.app` a
`Aplicaciones`.

> [!WARNING]
> La app no está firmada ni notarizada. La primera vez, macOS bloqueará la apertura: haz
> **clic derecho → Abrir** (o Preferencias del Sistema → Privacidad y seguridad → Abrir de todas formas).

---

Navega a https://music-mp3-downloader.astro.dev/ para acceder al sitio web del proyecto y descargar la aplicación.
