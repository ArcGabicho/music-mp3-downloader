from pydantic_settings import BaseSettings


class CloudflareSettings(BaseSettings):
    r2_access_key_id: str = ""
    r2_secret_access_key: str = ""
    r2_bucket_name: str = "vortex-mp3"
    r2_endpoint_url: str = ""
    r2_public_url: str = ""

    model_config = {"env_file": ".env"}


settings = CloudflareSettings()