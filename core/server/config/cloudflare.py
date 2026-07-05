from pydantic_settings import BaseSettings

import os

class CloudflareSettings(BaseSettings):
    r2_access_key_id: str = os.getenv("R2_ACCESS_KEY_ID")
    r2_secret_access_key: str = os.getenv("R2_SECRET_ACCESS_KEY")
    r2_bucket_name: str = os.getenv("R2_BUCKET_NAME")
    r2_endpoint_url: str = os.getenv("R2_ENDPOINT_URL")
    r2_public_url: str = os.getenv("R2_PUBLIC_URL")