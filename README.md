# Vortex MP3 Downloader

<p align="center">
  <img src="https://imgur.com/2f0d7887-418f-421b-bab8-8008feaa9206" alt="Vortex MP3 Downloader">
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

- Python 3.12+
- Docker y Docker Compose (opcional)
- FFmpeg instalado en el sistema (para conversión local)
- Cuenta de Cloudflare R2 con bucket creado
- Base de datos PostgreSQL (local o remota)

### 1. Clonar el repositorio

```bash
git clone https://github.com/tu-usuario/vortex-mp3-downloader.git
cd vortex-mp3-downloader
```

### 2. Configurar variables de entorno

Copia el archivo de ejemplo y completa los valores:

```bash
cp .env.example .env
```

| Variable                  | Descripción                          |
|---------------------------|--------------------------------------|
| `DATABASE_URL`            | URI de conexión a PostgreSQL         |
| `R2_ACCESS_KEY_ID`        | Access Key ID de Cloudflare R2       |
| `R2_SECRET_ACCESS_KEY`    | Secret Access Key de Cloudflare R2   |
| `R2_BUCKET_NAME`          | Nombre del bucket en R2              |
| `R2_ENDPOINT_URL`         | Endpoint del bucket R2               |
| `R2_PUBLIC_URL`           | URL pública del bucket (opcional)    |

### 3. Ejecutar con Docker (recomendado)

```bash
docker compose up -d
```

La aplicación estará disponible en `http://localhost:8000` y la documentación interactiva en `http://localhost:8000/docs`.

### 4. Ejecutar sin Docker

```bash
pip install -r requirements.txt
uvicorn main:app --reload
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

## 🧪 Desarrollo

```bash
# Entorno virtual
python -m venv .venv
source .venv/bin/activate

# Instalar dependencias
pip install -r requirements.txt

# Ejecutar en modo desarrollo
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

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
