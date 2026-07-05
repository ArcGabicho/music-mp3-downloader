import asyncio
import json
import logging
from pathlib import Path

TEMP_DIR = Path("/tmp/vortex-downloads")

logger = logging.getLogger(__name__)

async def _run_ytdlp(*args: str) -> tuple[str, str, int]:
    proc = await asyncio.create_subprocess_exec(
        "yt-dlp",
        *args,
        stdout=asyncio.subprocess.PIPE,
        stderr=asyncio.subprocess.PIPE,
    )
    stdout, stderr = await proc.communicate()
    return stdout.decode(), stderr.decode(), proc.returncode


async def download_mp3(url: str) -> tuple[Path, str, int]:
    TEMP_DIR.mkdir(parents=True, exist_ok=True)

    stdout, stderr, rc = await _run_ytdlp(
        "--dump-json",
        "--no-progress",
        url,
    )
    if rc != 0:
        raise RuntimeError(stderr.strip() or f"yt-dlp metadata failed (code {rc})")

    info = json.loads(stdout.strip().split("\n")[0])
    video_id = info["id"]
    title = info["title"]
    duration = info["duration"]

    output_template = str(TEMP_DIR / "%(id)s.%(ext)s")

    stdout, stderr, rc = await _run_ytdlp(
        "-x",
        "-f", "bestaudio/best",
        "--audio-format", "mp3",
        "--audio-quality", "0",
        "--js-runtimes", "node",
        "--remote-components", "ejs:github",
        "-o", output_template,
        "--no-progress",
        url,
    )
    if rc != 0:
        raise RuntimeError(stderr.strip() or f"yt-dlp download failed (code {rc})")

    if stderr.strip():
        logger.warning("yt-dlp stderr:\n%s", stderr.strip())

    filepath = next(TEMP_DIR.glob(f"{video_id}.*"), None)
    if filepath is None:
        raise RuntimeError(
            f"File for video {video_id} not found in {TEMP_DIR}.\n"
            f"yt-dlp stdout: {stdout.strip()}\n"
            f"yt-dlp stderr: {stderr.strip()}"
        )

    return filepath, title, duration