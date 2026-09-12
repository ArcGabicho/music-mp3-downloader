# Guía de CI/CD

El proyecto usa dos workflows de GitHub Actions, **ambos exclusivamente en runners
Windows** (`windows-latest`): la app es una aplicación de bandeja de Windows (WinUI +
H.NotifyIcon.Maui), así que no tiene sentido compilarla ni probarla en otro sistema.

| Workflow                        | Disparador                     | Propósito                                            |
|---------------------------------|--------------------------------|-----------------------------------------------------|
| `.github/workflows/ci.yml`      | `push` / `pull_request` a `master` | Compilar, probar y verificar formato.           |
| `.github/workflows/deploy.yml`  | `push` de tag `v*` o ejecución manual | Empaquetar el binario y publicar un GitHub Release. |

> El proyecto sigue declarando el TFM `net10.0-maccatalyst` en `MusicMp3Downloader.App.csproj`
> (compila localmente en macOS si hiciera falta), pero ninguno de los dos workflows lo
> compila, prueba ni empaqueta.

---

## `deploy.yml`

Genera el binario **self-contained** de Windows y lo adjunta a un GitHub Release.

### Disparadores

| Evento               | Cómo se usa                                                                 |
|----------------------|---------------------------------------------------------------------------|
| `push` de tag `v*`   | Flujo normal de publicación: `git tag v1.2.3 && git push origin v1.2.3`.  |
| `workflow_dispatch`  | Ejecución manual desde la pestaña **Actions**; pide el input `version` (p. ej. `v1.2.3`). Si el tag no existe, se crea sobre el commit actual. |

### Permisos

```yaml
permissions:
  contents: write
```

Necesario para que el `GITHUB_TOKEN` pueda crear el release y subir el binario. No
requiere secretos adicionales.

### Variables de entorno

| Variable          | Valor                                   | Uso                                    |
|-------------------|-----------------------------------------|----------------------------------------|
| `DOTNET_VERSION`  | `10.0.x`                                | SDK que instala `actions/setup-dotnet`.|
| `PROJECT`         | `core/MusicMp3Downloader.App.csproj`    | Proyecto a publicar.                   |
| `CONFIGURATION`   | `Release`                               | Configuración de compilación.          |

### Job `package`

Corre en `windows-latest`. Pasos:

1. **Checkout** — `actions/checkout@v4`.
2. **Setup .NET** — `actions/setup-dotnet@v4` con `DOTNET_VERSION`.
3. **Restore .NET MAUI workload** — `dotnet workload restore` instala los workloads que
   necesitan los proyectos de la solución (equivalente a `dotnet workload install maui`
   pero acotado a lo que declara el `.slnx`).
4. **Resolve version** — deriva dos valores del tag:
   - `tag`: la etiqueta tal cual (`v1.2.3`).
   - `number`: sin la `v` inicial (`1.2.3`), usado para `-p:Version=`.
   La fuente es `github.event.inputs.version` (manual) o `github.ref_name` (push de tag).
5. **Publish** — `dotnet publish -f net10.0-windows10.0.19041.0
   --self-contained true -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true`
   hacia `./artifacts/win-x64`: publica sin empaquetar en MSIX, como carpeta autónoma.
   **Sin `-r`/`--runtime` explícito a propósito**: en un proyecto multi-target, pasarlo
   en la línea de comandos lo vuelve una propiedad global de MSBuild que NuGet también
   intenta aplicar al TFM de Mac Catalyst del mismo proyecto (combinación sin sentido
   maccatalyst+win-x64), buscando un paquete de runtime Mono para Windows inexistente
   (`NU1102: Microsoft.NETCore.App.Runtime.Mono.win-x64`). El RID se resuelve solo,
   de forma implícita, para el TFM de Windows en un runner Windows.
6. **Archive (zip)** — `zip` vía PowerShell `Compress-Archive` sobre `artifacts/win-x64`
   a `MusicMp3Downloader-<number>-win-x64.zip`.
7. **Build Windows installer** — `iscc` (Inno Setup 6, preinstalado en el runner)
   compila `packaging/windows/installer.iss` sobre `artifacts/win-x64` y produce
   `MusicMp3Downloader-Setup-<number>-x64.exe`; se copia también como
   `MusicMp3Downloader-Setup-x64.exe` (nombre estable para `releases/latest/download/…`).
   Instalación por usuario (`{localappdata}\Programs`), sin admin, con accesos directos y
   desinstalador. Sin firma de código (SmartScreen avisa).
8. **Upload package** — sube todo lo que empiece por `MusicMp3Downloader-*` como artifact
   `package-win-x64` (`if-no-files-found: error`).

### Job `release`

Depende de `package` (`needs: package`) y corre en `windows-latest`.

1. **Download packages** — `actions/download-artifact@v4` con `pattern: package-*` y
   `merge-multiple: true`, dejando todos los archivos juntos en `./dist`.
2. **Create or update release** — usando la CLI `gh` (paso con `shell: bash`, disponible
   vía Git for Windows en el runner) con `GH_TOKEN: ${{ github.token }}`:
   - Si el release del tag ya existe → `gh release upload "$TAG" ./dist/* --clobber`.
   - Si no existe → `gh release create` con `--generate-notes` y `--target $GITHUB_SHA`.
   - Si el tag contiene un guion (`v1.2.3-rc1`) se marca `--prerelease`.

### Publicar una versión

```bash
# 1. Asegúrate de que master está verde en CI.
git checkout master && git pull

# 2. Crea y empuja el tag.
git tag v1.2.3
git push origin v1.2.3

# 3. Sigue el progreso en la pestaña Actions.
#    Al terminar, el Release lleva adjuntos: el .zip, el instalador de Windows
#    versionado y su copia de nombre estable Setup-x64.exe.
```

Para re-publicar la misma versión (por ejemplo, si el binario salió mal), vuelve a
lanzar el workflow desde **Actions → Deploy → Run workflow** indicando el mismo tag; los
binarios se sobrescriben con `--clobber`.

### Resolución de problemas

| Síntoma                                             | Causa probable                                              |
|----------------------------------------------------|------------------------------------------------------------|
| `release` falla con `HTTP 403`                     | Falta `permissions: contents: write` o el repo restringe el `GITHUB_TOKEN`. |
| `gh release create` falla con `tag already exists` sin release | Borra el tag remoto y vuelve a empujarlo, o crea el release manualmente. |
| El build falla buscando manifiestos/workloads de MAUI | Falta el paso `dotnet workload restore`, o el runner cacheó una versión de workload desincronizada del SDK — reintenta sin caché. |
| El binario arranca pero la descarga falla           | El paso de publicación no llegó a descargar yt-dlp/FFmpeg (sin red en el runner); revisa el log del target `FetchExternalTools`. Se empaquetan en `tools/` junto al ejecutable. |

---

## `ci.yml`

Referencia rápida (el detalle está en el propio archivo):

| Job      | Runner           | Qué hace                                             |
|----------|------------------|-------------------------------------------------------|
| `build`  | `windows-latest` | `dotnet workload restore` → `restore` → `build` → `test` (resultados `.trx` como artifact). Cachea `~/.nuget/packages`. |
| `format` | `windows-latest` | `dotnet format --verify-no-changes`.                 |
| `publish`| `windows-latest` | Solo en `push` a `master`: binario self-contained `win-x64` como artifact de build (no crea Release). |

La diferencia con `deploy.yml`: el job `publish` de `ci.yml` produce un artifact efímero
para inspección; `deploy.yml` produce el archivo versionado y lo publica en un Release.
