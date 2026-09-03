import os
from datetime import datetime

from sqlalchemy import Boolean, DateTime, String, create_engine, select
from sqlalchemy.orm import DeclarativeBase, Mapped, Session, mapped_column


def database_url() -> str:
    url = os.getenv("DATABASE_URL", "sqlite:///./access_codes.db")
    if url.startswith("postgres://"):
        return "postgresql+psycopg://" + url[len("postgres://") :]
    if url.startswith("postgresql://"):
        return "postgresql+psycopg://" + url[len("postgresql://") :]
    return url


class Base(DeclarativeBase):
    pass


class AccessCode(Base):
    __tablename__ = "access_codes"

    id: Mapped[int] = mapped_column(primary_key=True)
    code: Mapped[str] = mapped_column(String(128), index=True)
    product_id: Mapped[str] = mapped_column(String(128), index=True)
    active: Mapped[bool] = mapped_column(Boolean, default=True)
    expires_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)


engine = create_engine(database_url(), pool_pre_ping=True)


def initialize_database() -> None:
    Base.metadata.create_all(engine)


def find_access_code(code: str, product_id: str) -> AccessCode | None:
    with Session(engine) as session:
        statement = select(AccessCode).where(
            AccessCode.code == code,
            AccessCode.product_id == product_id,
        )
        return session.scalar(statement)
