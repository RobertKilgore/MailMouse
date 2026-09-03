import argparse
from datetime import datetime

try:
    from .database import AccessCode, Session, engine, initialize_database
except ImportError:
    from database import AccessCode, Session, engine, initialize_database


def add_code(code: str, product_id: str, expires_at: datetime | None) -> None:
    with Session(engine) as session:
        existing = session.query(AccessCode).filter_by(code=code, product_id=product_id).first()
        if existing is not None:
            raise SystemExit("That code already exists for this product.")

        session.add(
            AccessCode(
                code=code,
                product_id=product_id,
                active=True,
                expires_at=expires_at,
            )
        )
        session.commit()


def set_active(code: str, product_id: str, active: bool) -> None:
    with Session(engine) as session:
        access_code = session.query(AccessCode).filter_by(code=code, product_id=product_id).first()
        if access_code is None:
            raise SystemExit("That code was not found.")

        access_code.active = active
        session.commit()


def list_codes(product_id: str | None) -> None:
    with Session(engine) as session:
        query = session.query(AccessCode).order_by(AccessCode.product_id, AccessCode.code)
        if product_id:
            query = query.filter_by(product_id=product_id)

        for access_code in query.all():
            expiration = access_code.expires_at.isoformat() if access_code.expires_at else "never"
            state = "active" if access_code.active else "deactivated"
            print(f"{access_code.code} | {access_code.product_id} | {state} | expires: {expiration}")


def parse_expiration(value: str | None) -> datetime | None:
    if value is None:
        return None
    try:
        return datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as error:
        raise SystemExit("expires-at must be ISO-8601, for example 2027-01-31T23:59:59+00:00") from error


def main() -> None:
    parser = argparse.ArgumentParser(description="Manage Mail Mouse access codes.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    add_parser = subparsers.add_parser("add")
    add_parser.add_argument("code")
    add_parser.add_argument("--product-id", default="mail-mouse")
    add_parser.add_argument("--expires-at")

    for command, active in (("deactivate", False), ("reactivate", True)):
        command_parser = subparsers.add_parser(command)
        command_parser.set_defaults(active=active)
        command_parser.add_argument("code")
        command_parser.add_argument("--product-id", default="mail-mouse")

    list_parser = subparsers.add_parser("list")
    list_parser.add_argument("--product-id")

    args = parser.parse_args()
    initialize_database()

    if args.command == "add":
        add_code(args.code.strip().casefold(), args.product_id.strip().casefold(), parse_expiration(args.expires_at))
        print("Access code added.")
    elif args.command in ("deactivate", "reactivate"):
        set_active(args.code.strip().casefold(), args.product_id.strip().casefold(), args.active)
        print(f"Access code {args.command}d.")
    else:
        list_codes(args.product_id.strip().casefold() if args.product_id else None)


if __name__ == "__main__":
    main()
