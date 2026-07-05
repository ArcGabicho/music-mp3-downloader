from pathlib import Path

from fastapi import Depends, FastAPI, HTTPException, Query
from fastapi.responses import RedirectResponse
from fastapi.staticfiles import StaticFiles
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select

from core.server.config.database import async_session, get_db, init_db
from core.server.config.cloudflare import settings
from core.server.models.download import Download, DownloadStatus
from core.server.services.cloudflare_r2 import upload_file
from core.server.services.mp3_download import download_mp3
from core.server.views.router import router as views_router

import asyncio
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="Vortex MP3 Downloader")
app.mount("/static", StaticFiles(directory=Path(__file__).parent / "views" / "static"), name="static")
app.include_router(views_router)

@app.on_event("startup")
async def startup():
    await init_db()

async def process_download(download_id: str, url: str):
    async with async_session() as db:
        result = await db.execute(select(Download).where(Download.id == download_id))
        download = result.scalar_one()

        try:
            download.status = DownloadStatus.PROCESSING
            await db.commit()

            filepath, title, duration = await download_mp3(url)

            file_key = f"{download_id}.mp3"
            r2_url = await upload_file(filepath, file_key)

            file_size = filepath.stat().st_size
            filepath.unlink(missing_ok=True)

            download.status = DownloadStatus.COMPLETED
            download.title = title
            download.filename = filepath.name
            download.file_key = file_key
            download.file_size = file_size
            download.duration = duration
            await db.commit()

            logger.info("Download %s completed: %s", download_id, title)

        except Exception as exc:
            logger.error("Download %s failed: %s", download_id, exc)
            download.status = DownloadStatus.FAILED
            download.error_message = str(exc)
            await db.commit()

@app.post("/download")
async def create_download(url: str = Query(..., description="YouTube video URL"), db: AsyncSession = Depends(get_db)):
    download = Download(url=url)
    db.add(download)
    await db.commit()
    await db.refresh(download)

    asyncio.create_task(process_download(download.id, url))

    return {
        "id": download.id,
        "status": download.status.value,
        "url": url,
    }

@app.get("/download/{download_id}")
async def get_download(download_id: str, db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(Download).where(Download.id == download_id))
    download = result.scalar_one_or_none()

    if not download:
        raise HTTPException(status_code=404, detail="Download not found")

    return {
        "id": download.id,
        "url": download.url,
        "status": download.status.value,
        "title": download.title,
        "duration": download.duration,
        "file_size": download.file_size,
        "filename": download.filename,
        "error_message": download.error_message,
        "created_at": str(download.created_at),
        "updated_at": str(download.updated_at),
    }

@app.get("/download/{download_id}/file")
async def get_download_file(download_id: str, db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(Download).where(Download.id == download_id))
    download = result.scalar_one_or_none()

    if not download:
        raise HTTPException(status_code=404, detail="Download not found")
    if download.status != DownloadStatus.COMPLETED:
        raise HTTPException(status_code=400, detail="Download not yet completed")
    if not download.file_key:
        raise HTTPException(status_code=500, detail="File key not found")

    file_url = f"{settings.r2_public_url}/{download.file_key}"
    return RedirectResponse(url=file_url)

@app.get("/downloads")
async def list_downloads(db: AsyncSession = Depends(get_db)):
    result = await db.execute(
        select(Download).order_by(Download.created_at.desc()).limit(20)
    )
    downloads = result.scalars().all()
    return [
        {
            "id": d.id,
            "url": d.url,
            "status": d.status.value,
            "title": d.title,
            "duration": d.duration,
            "file_size": d.file_size,
            "filename": d.filename,
            "error_message": d.error_message,
            "created_at": str(d.created_at),
            "updated_at": str(d.updated_at),
        }
        for d in downloads
    ]

@app.get("/health")
async def health():
    return {"status": "ok"}