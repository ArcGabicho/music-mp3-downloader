# Empaquetado para el AUR

Paquete: **`music-mp3-downloader-bin`** — instala el binario Linux publicado en la
GitHub Release. No compila .NET en el chroot de `makepkg` (el `dotnet restore` necesita
red, que el build reproducible no permite).

Estos archivos son la copia de referencia; el repositorio del AUR es aparte
(`ssh://aur@aur.archlinux.org/music-mp3-downloader-bin.git`).

## Requisitos (primera vez)

- Cuenta en <https://aur.archlinux.org> con tu **clave SSH pública** subida.
- `base-devel` instalado. Para probar en limpio: `devtools` (`extra-x86_64-build`).

## Publicar una versión nueva

1. Asegúrate de que existe la Release `vX.Y.Z` con el asset
   `MusicMp3Downloader-X.Y.Z-linux-x64.tar.gz` (lo genera `.github/workflows/deploy.yml`
   al hacer push del tag).
2. En este directorio, edita `PKGBUILD`:
   - `pkgver=X.Y.Z`
   - `pkgrel=1` (o súbelo si cambias solo el empaquetado sin cambiar la versión).
3. `updpkgsums` — descarga los `source` y rellena los `sha256sums` reales.
4. `makepkg --printsrcinfo > .SRCINFO`
5. Prueba local:
   - rápida: `makepkg -si`
   - limpia (recomendada): `extra-x86_64-build` y luego instala el `.pkg.tar.zst`.
6. Publica en el AUR:
   ```bash
   git clone ssh://aur@aur.archlinux.org/music-mp3-downloader-bin.git aur
   cp PKGBUILD .SRCINFO music-mp3-downloader.desktop aur/
   cd aur
   git add -A
   git commit -m "upgpkg: X.Y.Z-1"
   git push
   ```

## Notas

- `depends=('vlc' 'icu')`: `libvlc` para reproducir; `icu` para la globalización de .NET.
  El resto (X11, fontconfig, gcc-libs, zlib, openssl) lo aporta cualquier escritorio / base.
- `yt-dlp` y `ffmpeg` van **incluidos** en el tarball (`/opt/music-mp3-downloader/tools/`),
  así que no son dependencias del paquete.
- El icono usa el nombre genérico `multimedia-player`; para un icono propio, añádelo al
  `source`, instálalo en `usr/share/icons/hicolor/…` y cambia `Icon=` en el `.desktop`.
- Automatizar el push al AUR con una GitHub Action es posible, pero requiere guardar la
  clave SSH del AUR como secret del repo.
