import os

from sqlalchemy import Boolean, DateTime, String, create_engine, select
from sqlalchemy import Integer
from sqlalchemy.orm import DeclarativeBase, Session, mapped_column


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

    id = mapped_column(Integer, primary_key=True)
    code = mapped_column(String(128), index=True)
    product_id = mapped_column(String(128), index=True)
    active = mapped_column(Boolean, default=True)
    expires_at = mapped_column(DateTime(timezone=True), nullable=True)


configured_database_url = database_url()
engine = create_engine(
    configured_database_url,
    pool_pre_ping=True,
    connect_args={"connect_timeout": 10} if configured_database_url.startswith("postgresql+") else {},
)


def initialize_database() -> None:
    Base.metadata.create_all(engine)


def find_access_code(code: str, product_id: str) -> AccessCode | None:
    with Session(engine) as session:
        statement = select(AccessCode).where(
            AccessCode.code == code,
            AccessCode.product_id == product_id,
        )
        return session.scalar(statement)
