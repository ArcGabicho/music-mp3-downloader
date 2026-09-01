# Music MP3 Downloader

![Wallpaper](https://i.imgur.com/2lcUDNj.png)

<a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET"></a>
<a href="https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-13"><img src="https://img.shields.io/badge/C%23-13-239120?logo=csharp" alt="C#"></a>
<a href="https://avaloniaui.net/"><img src="https://img.shields.io/badge/Avalonia%20UI-12.1-8B44AC?logo=avalonia" alt="Avalonia UI"></a>
<a href="https://github.com/ArcGabicho/music-mp3-downloader"><img src="https://img.shields.io/badge/cross--platform-Windows%20%7C%20macOS%20%7C%20Linux-informational" alt="Cross-platform"></a>
<a href="LICENSE.md"><img src="https://img.shields.io/badge/license-MIT-green" alt="License"></a>

**Music MP3 Downloader** es una aplicación de escritorio multiplataforma construida con **.NET 10** y **Avalonia UI** que permite descargar audio en formato MP3 a partir de URLs de YouTube además reproducirlas en una UI moderna.

---

#### Instalación en Arch Linux / CachyOS

```bash
yay -S music-mp3-downloader-bin
```

> [!WARNING]
> Necesitas el helper de AUR `yay` y el paquete `vlc` (libvlc) para la reproducción de audio.
> `yt-dlp` y `ffmpeg` vienen incluidos en la app; no hay que instalarlos.
> El paquete descarga el binario ya compilado de la última release de GitHub y lo instala,
> dejando el ejecutable `music-mp3-downloader` en el `PATH` y una entrada en el menú.

#### Instalación en Windows

```powershell
iwr https://github.com/ArcGabicho/music-mp3-downloader/releases/latest/download/MusicMp3Downloader-Setup-x64.exe -OutFile MusicMp3Downloader-Setup.exe; .\MusicMp3Downloader-Setup.exe
```

> [!WARNING]
> Necesitas Windows 10/11 (incluye PowerShell y `iwr`) y conexión a internet. `yt-dlp` y `ffmpeg` vienen incluidos; no hay que instalar nada aparte.
> El comando descarga el instalador de la última release y lo ejecuta (instalación por usuario, sin permisos de administrador). Como el ejecutable no está firmado, SmartScreen puede avisar: **Más información → Ejecutar de todas formas**.

---

Navega a https://music-mp3-downloader.astro.dev/ para acceder al sitio web del proyecto y descargar la aplicación.