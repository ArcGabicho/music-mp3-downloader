from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker, create_async_engine
from sqlalchemy.orm import DeclarativeBase

import os

# Importar credenciales del .env
DATABASE_URL = os.getenv("DATABASE_URL")

# Crear el motor asíncrono
engine = create_async_engine(DATABASE_URL, echo=True, future=True)

# Crear la sesión asíncrona
async_session = async_sessionmaker(
    bind=engine, 
    class_=AsyncSession, 
    expire_on_commit=False
)

Base = DeclarativeBase()


async def get_db():
    async with async_session() as session:
        yield session