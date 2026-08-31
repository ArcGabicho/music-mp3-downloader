# Guía de CI/CD

El proyecto usa dos workflows de GitHub Actions:

| Workflow                        | Disparador                     | Propósito                                            |
|---------------------------------|--------------------------------|-----------------------------------------------------|
| `.github/workflows/ci.yml`      | `push` / `pull_request` a `master` | Compilar, probar y verificar formato.           |
| `.github/workflows/deploy.yml`  | `push` de tag `v*` o ejecución manual | Empaquetar binarios y publicar un GitHub Release. |

---

## `deploy.yml`

Genera ejecutables **self-contained de un solo archivo** para las cuatro plataformas
soportadas y los adjunta a un GitHub Release.

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

Necesario para que el `GITHUB_TOKEN` pueda crear el release y subir los binarios. No
requiere secretos adicionales.

### Variables de entorno

| Variable          | Valor                                   | Uso                                    |
|-------------------|-----------------------------------------|----------------------------------------|
| `DOTNET_VERSION`  | `10.0.x`                                | SDK que instala `actions/setup-dotnet`.|
| `PROJECT`         | `core/MusicMp3Downloader.App.csproj`    | Proyecto a publicar.                   |
| `CONFIGURATION`   | `Release`                               | Configuración de compilación.          |

### Job `package`

Matriz de 4 combinaciones runner/RID:

| RID          | Runner         | Plataforma            |
|--------------|----------------|-----------------------|
| `linux-x64`  | `ubuntu-latest`| Linux x64             |
| `win-x64`    | `windows-latest`| Windows x64          |
| `osx-x64`    | `macos-13`     | macOS Intel           |
| `osx-arm64`  | `macos-latest` | macOS Apple Silicon   |

`fail-fast: false` — si una plataforma falla, las demás siguen.

Pasos:

1. **Checkout** — `actions/checkout@v4`.
2. **Setup .NET** — `actions/setup-dotnet@v4` con `DOTNET_VERSION`.
3. **Resolve version** — deriva dos valores del tag:
   - `tag`: la etiqueta tal cual (`v1.2.3`).
   - `number`: sin la `v` inicial (`1.2.3`), usado para `-p:Version=`.
   La fuente es `github.event.inputs.version` (manual) o `github.ref_name` (push de tag).
4. **Publish** — `dotnet publish` con `--self-contained true -p:PublishSingleFile=true`
   hacia `./artifacts/<rid>`. No se activa trimming (Avalonia no lo soporta de forma fiable).
5. **Archive** — comprime la carpeta publicada:
   - `tar.gz` en Linux y macOS.
   - `zip` en Windows (vía PowerShell `Compress-Archive`).
   Nombre: `MusicMp3Downloader-<number>-<rid>.(tar.gz|zip)`.
6. **Upload package** — sube el archivo como artifact `package-<rid>` (`if-no-files-found: error`).

### Job `release`

Depende de `package` (`needs: package`) y corre en `ubuntu-latest`.

1. **Download packages** — `actions/download-artifact@v4` con `pattern: package-*` y
   `merge-multiple: true`, dejando todos los archivos juntos en `./dist`.
2. **Create or update release** — usando la CLI `gh` con `GH_TOKEN: ${{ github.token }}`:
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
#    Al terminar, el Release aparece en la pestaña Releases con 4 binarios adjuntos.
```

Para re-publicar la misma versión (por ejemplo, si un binario salió mal), vuelve a
lanzar el workflow desde **Actions → Deploy → Run workflow** indicando el mismo tag; los
binarios se sobrescriben con `--clobber`.

### Resolución de problemas

| Síntoma                                             | Causa probable                                              |
|----------------------------------------------------|------------------------------------------------------------|
| `release` falla con `HTTP 403`                     | Falta `permissions: contents: write` o el repo restringe el `GITHUB_TOKEN`. |
| `gh release create` falla con `tag already exists` sin release | Borra el tag remoto y vuelve a empujarlo, o crea el release manualmente. |
| Un RID de macOS no compila                          | `macos-13` (Intel) puede quedar deprecado; migra a `macos-14`/`macos-15` con `-r osx-x64`. |
| El binario arranca pero la descarga falla           | Faltan `yt-dlp` y/o FFmpeg en la máquina destino; son dependencias de runtime, no se empaquetan. |

---

## `ci.yml`

Referencia rápida (el detalle está en el propio archivo):

| Job      | Runner(s)                                      | Qué hace                                             |
|----------|-----------------------------------------------|-----------------------------------------------------|
| `build`  | `ubuntu-latest`, `windows-latest`, `macos-latest` | `restore` → `build` → `test` (resultados `.trx` como artifact). Cachea `~/.nuget/packages`. |
| `format` | `ubuntu-latest`                                | `dotnet format --verify-no-changes`.               |
| `publish`| matriz `linux-x64` / `win-x64` / `osx-arm64`   | Solo en `push` a `master`: binarios self-contained como artifacts de build (no crean Release). |

La diferencia con `deploy.yml`: el job `publish` de `ci.yml` produce artifacts efímeros
para inspección; `deploy.yml` produce archivos versionados y los publica en un Release.