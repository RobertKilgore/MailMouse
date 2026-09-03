from datetime import datetime, timezone

from fastapi import FastAPI, HTTPException
from fastapi.responses import JSONResponse
from pydantic import BaseModel, Field

from database import find_access_code, initialize_database

app = FastAPI(title="Mail Mouse Access API", version="1.0.0")


@app.on_event("startup")
def startup() -> None:
    initialize_database()


class AccessRequest(BaseModel):
    accessCode: str = Field(min_length=1, max_length=128)
    productId: str = Field(min_length=1, max_length=128)
    buildVersion: str = Field(default="unknown", max_length=64)


class AccessResponse(BaseModel):
    valid: bool
    status: str


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/api/validate-access", response_model=AccessResponse)
def validate_access(request: AccessRequest) -> AccessResponse:
    requested_code = request.accessCode.strip().casefold()
    requested_product = request.productId.strip().casefold()

    try:
        matching_code = find_access_code(requested_code, requested_product)
    except Exception as error:
        raise HTTPException(status_code=500, detail="Access-code database is unavailable") from error

    if matching_code is None:
        return JSONResponse(
            status_code=404,
            content={"valid": False, "status": "not_found"},
        )

    if not matching_code.active or (
        matching_code.expires_at is not None
        and matching_code.expires_at <= datetime.now(timezone.utc)
    ):
        return JSONResponse(
            status_code=410,
            content={"valid": False, "status": "deactivated"},
        )

    return AccessResponse(valid=True, status="valid")
