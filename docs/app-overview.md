# Descripción general de la aplicación: Descargador de música MP3

## 1. Resumen ejecutivo

**Music MP3 Downloader** es una aplicación web autoalojada de código abierto diseñada para liberar el procesamiento de medios de alto rendimiento de las limitaciones del almacenamiento local tradicional.

El objetivo principal de la aplicación es proporcionar a los usuarios un servicio autónomo y ligero para extraer, transcodificar y archivar audio MP3 de alta calidad desde URLs de YouTube. Mediante una arquitectura asíncrona, el sistema garantiza que las operaciones multimedia de larga duración y con alto consumo de CPU no bloqueen el servidor web, lo que permite múltiples operaciones concurrentes con un consumo mínimo de recursos.

## 2. Principios arquitectónicos principales

La aplicación se basa en tres pilares de diseño fundamentales para garantizar la escalabilidad y el bajo consumo de recursos en una infraestructura autoalojada:

### ⚡ Asincronía sin bloqueo
Las operaciones de E/S vinculadas a la red (como la comunicación con la base de datos o la carga de objetos grandes a Cloudflare R2) y las tareas de procesamiento intensivo se ejecutan de forma asíncrona mediante **FastAPI** y `asyncpg`. Esto evita que los procesos del servidor web se sobrecarguen al gestionar múltiples solicitudes de usuarios simultáneas.

### 🪶 Computación sin estado con almacenamiento en la nube
A diferencia de los descargadores tradicionales que almacenan archivos multimedia en el disco local (lo que provoca escasez de almacenamiento y scripts de limpieza complejos), esta aplicación trata el entorno local como efímero. Los fragmentos descargados se almacenan temporalmente, se procesan, se transmiten inmediatamente a **Cloudflare R2 Object Storage** y se eliminan instantáneamente del entorno local.

### 🔒 Autonomía operativa (modelo autoalojado)
El sistema está completamente contenerizado con Docker, lo que permite a los usuarios implementar su propia instancia aislada. Esto les otorga control total sobre su historial de descargas, claves de acceso a la API, registros y ancho de banda de red, sin depender de alternativas SaaS de terceros.

## 3. Flujo de trabajo del sistema de alto nivel

Cuando un usuario interactúa con la aplicación (ya sea a través de la interfaz web o directamente mediante la API REST), el sistema gestiona los datos a través del framework, la base de datos y los proveedores de almacenamiento mediante el siguiente ciclo de vida:

1. **Ingesta y validación:** El cliente envía una URL de YouTube mediante una solicitud `POST /download`. FastAPI valida la carga útil de la consulta.

2. **Registro de la tarea:** La aplicación proporciona instantáneamente un UUID único para la tarea de descarga, crea un registro en **PostgreSQL** con estado `pending` y devuelve el ID de seguimiento al cliente de inmediato (evitando tiempos de espera HTTP).

3. **Procesamiento en segundo plano:** El servidor inicia una tarea de trabajo asíncrona en segundo plano para desacoplar la extracción del hilo principal.
4. **Extracción y transcodificación:** El motor principal invoca `yt-dlp` para extraer el flujo de audio sin procesar de YouTube y lo envía a `FFmpeg` para transcodificar el flujo variable en un contenedor MP3 estandarizado de alta fidelidad.

5. **Archivo en la nube:** El archivo MP3 completado se envía de forma segura al bucket de **Cloudflare R2** mediante un cliente compatible con S3 (`boto3`).

6. **Finalización del estado:** Una vez que la carga a la nube se confirma correctamente, el proceso en segundo plano actualiza el estado del registro de la base de datos a `completado`, asignando la entrada a la URL pública de descarga del archivo R2 e indexando los metadatos multimedia (título, duración, tamaño del archivo).

## 4. Desglose y responsabilidades de los componentes

- **`config/` (Inicialización del sistema):** Gestiona las conexiones del ciclo de vida. Se conecta a las variables de entorno para crear la fábrica de sesiones del motor SQLAlchemy asíncrono e inicializa las credenciales de almacenamiento de Cloudflare R2.
- **`models/` (Capa de persistencia de datos):** Define los esquemas estructurales de la base de datos. Asigna campos relacionales como tareas de descarga, marcas de tiempo, estados de procesamiento (`pendiente`, `procesando`, `completado`, `fallido`) e información de archivos.

- **`services/` (Lógica de negocio principal):**

- `mp3_download.py`: Gestiona la ejecución de subprocesos para la manipulación de medios.

- `cloudflare_r2.py`: Gestiona las cargas seguras de transmisión multiparte al bucket de almacenamiento de objetos en la nube.