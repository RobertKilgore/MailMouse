import os

os.environ["DATABASE_URL"] = "sqlite:///./test_access_codes.db"

from fastapi.testclient import TestClient

from sqlalchemy import delete

try:
    from .app import app
    from .database import AccessCode, Session, engine, initialize_database
except ImportError:
    from app import app
    from database import AccessCode, Session, engine, initialize_database

initialize_database()
with Session(engine) as session:
    session.execute(delete(AccessCode))
    session.add_all(
        [
            AccessCode(code="demo-valid-code", product_id="mail-mouse", active=True),
            AccessCode(code="demo-expired-code", product_id="mail-mouse", active=False),
        ]
    )
    session.commit()

client = TestClient(app)

REQUEST = {
    "accessCode": "DEMO-VALID-CODE",
    "productId": "mail-mouse",
    "buildVersion": "0.0.1",
}


def test_valid_code():
    response = client.post("/api/validate-access", json=REQUEST)

    assert response.status_code == 200
    assert response.json() == {"valid": True, "status": "valid"}


def test_unknown_code():
    request = {**REQUEST, "accessCode": "DOES-NOT-EXIST"}
    response = client.post("/api/validate-access", json=request)

    assert response.status_code == 404
    assert response.json() == {"valid": False, "status": "not_found"}


def test_deactivated_code():
    request = {**REQUEST, "accessCode": "DEMO-EXPIRED-CODE"}
    response = client.post("/api/validate-access", json=request)

    assert response.status_code == 410
    assert response.json() == {"valid": False, "status": "deactivated"}


def test_product_mismatch_is_not_found():
    request = {**REQUEST, "productId": "another-product"}
    response = client.post("/api/validate-access", json=request)

    assert response.status_code == 404
    assert response.json() == {"valid": False, "status": "not_found"}
