# Mail Mouse Access API

Small API for validating Mail Mouse access codes. It uses SQLite locally and PostgreSQL when `DATABASE_URL` is configured.

## Setup

From the `server` directory:

```powershell
..\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
```

If the project virtual environment is not available from this directory, activate it from the repository root instead:

```powershell
.\.venv\Scripts\Activate.ps1
```

## Run

```powershell
python -m uvicorn app:app --reload --host 0.0.0.0 --port 8000
```

Initialize a local database with demo codes:

```powershell
python seed_db.py
```

The endpoint is:

```text
http://localhost:8000/api/validate-access
```

Health check:

```text
http://localhost:8000/health
```

## Database

The API creates an `access_codes` table with:

```text
code, product_id, active, expires_at
```

For hosted deployment, set `DATABASE_URL` to the PostgreSQL connection string supplied by your database provider. Do not use `codes.json` in production.

For Render connecting to Supabase, use Supabase's **Session Pooler** connection string rather than the direct database connection string. The direct hostname may resolve to IPv6, which can produce `Network is unreachable` on Render. In Supabase, open **Connect**, choose **Session pooler**, select URI format, and copy that URL into Render's `DATABASE_URL` environment variable. It normally uses a host containing `.pooler.supabase.com`.

For Render, set the service root directory to `server`, set the build command to `pip install -r requirements.txt`, and set the start command to:

```text
uvicorn app:app --host 0.0.0.0 --port $PORT
```

Add the database provider's connection string as the `DATABASE_URL` environment variable. The table is created automatically when the service starts. Run `python seed_db.py` only for local demo data; production codes should be inserted through an admin tool or migration.

## Update a hosted database

Copy the PostgreSQL `DATABASE_URL` from Render, set it in a local PowerShell session, and run the management script from the repository root:

```powershell
$env:DATABASE_URL = "postgresql://..."
.\.venv\Scripts\python.exe server\manage_codes.py add PLAYER-CODE --product-id mail-mouse
```

Add an expiration date:

```powershell
.\.venv\Scripts\python.exe server\manage_codes.py add PLAYER-CODE --expires-at 2027-01-31T23:59:59+00:00
```

Generate random hexadecimal codes:

```powershell
.\.venv\Scripts\python.exe server\manage_codes.py generate --count 10 --length 12 --product-id mail-mouse
```

`--length` is the number of hexadecimal characters and must be even. Generated codes use cryptographically secure randomness, print in uppercase, and are stored case-insensitively. The default generates one 12-character code.

Other commands:

```powershell
.\.venv\Scripts\python.exe server\manage_codes.py list
.\.venv\Scripts\python.exe server\manage_codes.py deactivate PLAYER-CODE
.\.venv\Scripts\python.exe server\manage_codes.py reactivate PLAYER-CODE
```

## One-click Unity Editor generator

The Unity Editor has an admin-only tool at `Access Control > Generate One Access Code`. It runs the generator locally, inserts one 32-character hexadecimal code into the database, and copies the code to the clipboard. Editor scripts are not included in the shipped game.

Open the tool, paste the private Supabase Session Pooler URL into `Supabase Database URL`, and click `Generate and Add One Code`. The URL is held in the editor window's memory and passed to the generator process; it is not saved in the Unity project. The button is disabled until a URL is entered.

Run these commands from a trusted machine only. Do not add an unauthenticated admin endpoint to the public API.

Responses:

- `200`: `{ "valid": true, "status": "valid" }`
- `404`: code does not exist for that product
- `410`: code exists but is deactivated or expired

## Unity configuration

Set `validationUrl` in `Assets/Resources/access-control-config.json` to:

```json
{
  "validationUrl": "http://localhost:8000/api/validate-access",
  "productId": "mail-mouse"
}
```

`localhost` works when Unity and the API run on the same machine. A deployed build must use the server's HTTPS URL instead.
