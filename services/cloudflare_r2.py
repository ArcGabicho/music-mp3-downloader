import asyncio
from pathlib import Path

import boto3

from config.cloudflare import settings


def _get_client():
    return boto3.client(
        "s3",
        endpoint_url=settings.r2_endpoint_url,
        aws_access_key_id=settings.r2_access_key_id,
        aws_secret_access_key=settings.r2_secret_access_key,
    )


async def upload_file(filepath: Path, key: str) -> str:
    def _sync_upload():
        client = _get_client()
        client.upload_file(str(filepath), settings.r2_bucket_name, key)
        return f"{settings.r2_public_url}/{key}"

    return await asyncio.to_thread(_sync_upload)