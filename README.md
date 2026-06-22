# Vortex MP3 Downloader

<p align="center">
  <img src="https://i.imgur.com/xwU0wIJ.png" alt="Vortex MP3 Downloader">
</p>

<p align="center">
  <img src="https://img.shields.io/badge/python-3.12+-blue?logo=python" alt="Python">
  <img src="https://img.shields.io/badge/FastAPI-0.115+-009688?logo=fastapi" alt="FastAPI">
  <img src="https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql" alt="PostgreSQL">
  <img src="https://img.shields.io/badge/Cloudflare%20R2-Object%20Storage-F38020?logo=cloudflare" alt="Cloudflare R2">
  <img src="https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker" alt="Docker">
  <img src="https://img.shields.io/badge/license-MIT-green" alt="License">
</p>

**Vortex MP3 Downloader** es un servicio web gratuito construido con **FastAPI** que permite descargar audio en formato MP3 a partir de URLs de YouTube. El proyecto está diseñado con una arquitectura moderna y escalable usando **PostgreSQL** como base de datos y **Cloudflare R2** para el almacenamiento de objetos.

---

## ✨ Características

- 🎵 **Descarga de MP3** — Extrae y convierte audio de videos de YouTube a formato MP3 de alta calidad.
- ☁️ **Almacenamiento en Cloudflare R2** — Los archivos descargados se almacenan de forma duradera y eficiente en R2.
- 🗄️ **Base de datos PostgreSQL** — Registro de descargas, usuarios y metadatos con SQLAlchemy + asyncpg.
- 🐳 **Dockerizado** — Despliegue reproducible con Docker Compose (app + PostgreSQL).
- ⚡ **API RESTful** — Endpoints rápidos y documentados automáticamente con OpenAPI/Swagger.
- 🔄 **Procesamiento asíncrono** — Operaciones no bloqueantes para manejar múltiples descargas concurrentes.

---

## 🛠️ Stack Tecnológico

| Componente       | Tecnología                                    |
|------------------|-----------------------------------------------|
| **Framework**    | FastAPI                                       |
| **Lenguaje**     | Python 3.12+                                  |
| **Base de Datos**| PostgreSQL 16 + asyncpg + SQLAlchemy (async)  |
| **Storage**      | Cloudflare R2 (compatible con S3 API)         |
| **Descargas**    | yt-dlp + FFmpeg                               |
| **Contenedores** | Docker + Docker Compose                        |
| **Cliente S3**   | boto3 / s3fs                                   |

---

## 📦 Estructura del Proyecto

```
vortex-mp3-downloader/
├── config/
│   ├── cloudflare.py       # Configuración de Cloudflare R2
│   └── database.py         # Configuración y sesión de base de datos
├── models/
│   └── download.py         # Modelos SQLAlchemy (descargas, usuarios)
├── services/
│   ├── cloudflare_r2.py    # Lógica de subida/descarga a R2
│   └── mp3_download.py     # Lógica de extracción y conversión de audio
├── main.py                 # Punto de entrada de la aplicación FastAPI
├── requirements.txt        # Dependencias de Python
├── Dockerfile              # Imagen de producción
├── docker-compose.yml      # Orquestación de servicios
├── .env                    # Variables de entorno (no versionado)
└── .gitignore
```

---

## 🚀 Inicio Rápido

### Prerrequisitos

- **Python 3.12+** — Entorno de ejecución.
- **Docker + Docker Compose** — Opcional, pero recomendado para evitar instalar dependencias manualmente.
- **FFmpeg** — Necesario para la conversión de audio (lo incluye la imagen Docker automáticamente).
- **Cuenta de Cloudflare R2** — Con un bucket creado y credenciales de acceso (Access Key + Secret Key).
- **PostgreSQL 16** — Solo si ejecutas sin Docker; con Docker Compose se levanta automáticamente.

### 1. Clonar el repositorio

```bash
git clone https://github.com/ArcGabicho/vortex-mp3-downloader.git && cd vortex-mp3-downloader
```

### 2. Configurar variables de entorno

Copia el archivo de ejemplo y completa los valores según tu cuenta de Cloudflare R2:

```bash
cp .env.example .env
```

| Variable                  | Descripción                                      | Ejemplo                                              |
|---------------------------|--------------------------------------------------|------------------------------------------------------|
| `DATABASE_URL`            | URI de conexión a PostgreSQL                     | `postgresql+asyncpg://postgres:postgres@db:5432/vortex` |
| `R2_ACCESS_KEY_ID`        | Access Key ID de Cloudflare R2                   | `7a8b9c0d1e2f3a4b5c6d`                              |
| `R2_SECRET_ACCESS_KEY`    | Secret Access Key de Cloudflare R2               | `a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0`         |
| `R2_BUCKET_NAME`          | Nombre del bucket en R2                          | `vortex-mp3`                                         |
| `R2_ENDPOINT_URL`         | Endpoint del bucket R2                           | `https://<accountid>.r2.cloudflarestorage.com`       |
| `R2_PUBLIC_URL`           | URL pública del bucket (para descargas)          | `https://pub-<hash>.r2.dev`                          |

> **Nota:** La `DATABASE_URL` del ejemplo asume que PostgreSQL corre en el contenedor llamado `db`. Si ejecutas sin Docker, cambia `db` por `localhost`.

### 3. Ejecutar con Docker (recomendado)

Este comando levanta la aplicación y PostgreSQL en segundo plano:

```bash
docker compose up -d
```

Para ver los logs en tiempo real:

```bash
docker compose logs -f
```

La aplicación estará disponible en:

| URL                        | Descripción                     |
|----------------------------|---------------------------------|
| `http://localhost:8000`    | API base                        |
| `http://localhost:8000/docs` | Swagger UI (documentación interactiva) |
| `http://localhost:8000/redoc` | ReDoc (documentación alternativa) |

Para detener los servicios:

```bash
docker compose down
```

> Para eliminar también el volumen de la base de datos: `docker compose down -v`

### 4. Ejecutar sin Docker (desarrollo local)

#### 4.1. Preparar PostgreSQL

Asegúrate de tener PostgreSQL 16 corriendo y crea la base de datos:

```bash
createdb vortex
```

O desde psql:

```sql
CREATE DATABASE vortex;
```

#### 4.2. Crear entorno virtual e instalar dependencias

```bash
python -m venv .venv
source .venv/bin/activate   # Linux/Mac
# .venv\Scripts\activate    # Windows

pip install -r requirements.txt
```

Asegúrate de tener **FFmpeg** instalado en tu sistema:

```bash
# Linux (Debian/Ubuntu)
sudo apt install ffmpeg

# Mac
brew install ffmpeg

# Windows
winget install ffmpeg
```

#### 4.3. Iniciar la aplicación

```bash
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

La API estará disponible en `http://localhost:8000`.

---

## 🧪 Uso de la API

### Descargar un MP3

```bash
curl -X POST "http://localhost:8000/download?url=https://www.youtube.com/watch?v=dQw4w9WgXcQ"
```

Respuesta:

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "pending",
  "url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
}
```

### Consultar el estado

```bash
curl "http://localhost:8000/download/a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

Respuesta (cuando ya terminó):

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
  "status": "completed",
  "title": "Rick Astley - Never Gonna Give You Up",
  "duration": 212,
  "file_size": 3456789,
  "filename": "dQw4w9WgXcQ.mp3",
  "error_message": null,
  "created_at": "2026-06-22 12:00:00+00:00",
  "updated_at": "2026-06-22 12:00:30+00:00"
}
```

### Descargar el archivo MP3

```bash
curl -L "http://localhost:8000/download/a1b2c3d4-e5f6-7890-abcd-ef1234567890/file" -o cancion.mp3
```

### Health check

```bash
curl "http://localhost:8000/health"
```

```json
{
  "status": "ok"
}
```

---

## 📖 API

| Método | Endpoint              | Descripción                            |
|--------|-----------------------|----------------------------------------|
| `POST` | `/download`           | Inicia la descarga de un MP3 desde URL |
| `GET`  | `/download/{id}`      | Obtiene el estado de una descarga      |
| `GET`  | `/download/{id}/file`  | Descarga el archivo MP3 resultante     |
| `GET`  | `/health`             | Health check del servicio              |

La documentación completa de la API está disponible en `/docs` (Swagger UI) y `/redoc` (ReDoc).

---

## 🤝 Contribuciones

Las contribuciones son bienvenidas. Por favor:

1. Haz un fork del proyecto
2. Crea una rama para tu feature (`git checkout -b feature/nueva-funcionalidad`)
3. Haz commit de tus cambios (`git commit -m 'feat: agregar nueva funcionalidad'`)
4. Haz push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Abre un Pull Request

---

## 📄 Licencia

Este proyecto está licenciado bajo la Licencia MIT. Consulta el archivo [LICENSE.md](LICENSE.md) para más detalles.