from datetime import datetime, timedelta, timezone

from database import AccessCode, Session, engine, initialize_database


initialize_database()
with Session(engine) as session:
    session.add_all(
        [
            AccessCode(code="demo-valid-code", product_id="mail-mouse", active=True),
            AccessCode(
                code="demo-expired-code",
                product_id="mail-mouse",
                active=True,
                expires_at=datetime.now(timezone.utc) - timedelta(days=1),
            ),
        ]
    )
    session.commit()

print("Database initialized with demo access codes.")
