import asyncio
from pathlib import Path

TEMP_DIR = Path("/tmp/vortex-downloads")


async def download_mp3(url: str) -> tuple[Path, str, int]:
    TEMP_DIR.mkdir(parents=True, exist_ok=True)

    output_template = str(TEMP_DIR / "%(id)s.%(ext)s")

    proc = await asyncio.create_subprocess_exec(
        "yt-dlp",
        "-x",
        "--audio-format", "mp3",
        "--audio-quality", "0",
        "-o", output_template,
        "--print", "filename",
        "--print", "title",
        "--print", "duration",
        url,
        stdout=asyncio.subprocess.PIPE,
        stderr=asyncio.subprocess.PIPE,
    )

    stdout, stderr = await proc.communicate()

    if proc.returncode != 0:
        raise RuntimeError(stderr.decode().strip())

    lines = stdout.decode().strip().split("\n")
    filepath = Path(lines[0])
    title = lines[1]
    duration = int(lines[2])

    return filepath, title, duration