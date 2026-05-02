# Bearer Token (Username/Password Login)

> Security notice: this file is for local development reference only.
> Never commit production credentials or JWT signing keys to source control.
> Store the signing key in user-secrets locally and Azure Key Vault in production.
> API user credentials are stored in the database — not in appsettings or user-secrets.

---

## Authentication Flow

1. On a fresh database, call `POST /api/auth/bootstrap-user` once to create the first API user.
2. Call `POST /api/auth/token` with that username and password to obtain a JWT.
3. Send that token as `Authorization: Bearer <token>` on all protected routes.

---

## Step 1 — Create the First User (Bootstrap)

`POST /api/auth/bootstrap-user` is a one-time endpoint. It returns **409** once any user already exists in the database.

```http
POST /api/auth/bootstrap-user
Content-Type: application/json

{
  "username": "api-user",
  "password": "your-strong-password"
}
```

Response 201:

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "api-user",
  "createdUtc": "2026-05-03T08:00:00+00:00"
}
```

---

## Step 2 — Request a Token

```http
POST /api/auth/token
Content-Type: application/json

{
  "username": "api-user",
  "password": "your-strong-password"
}
```

Response 200:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-05-03T09:00:00+00:00"
}
```

---

## Step 3 — Use the Token

```http
GET /api/weather/current?stationId=S108
Authorization: Bearer <accessToken>
```

---

## Configure the Signing Key Locally

The JWT signing key must never be committed to source control. Set it via .NET user-secrets:

```powershell
cd WeatherAPI
dotnet user-secrets set "JwtAuth:SigningKey" "replace-with-a-long-random-base64-secret"
```

Or as an environment variable:

```powershell
$env:JwtAuth__SigningKey = "replace-with-a-long-random-base64-secret"
```

---

## Swagger UI

1. Open `https://localhost:{port}/`.
2. Call `POST /api/auth/bootstrap-user` if this is a fresh database.
3. Call `POST /api/auth/token` and copy `accessToken`.
4. Click **Authorize**.
5. Paste the JWT token value (without the `Bearer` prefix — Swagger adds it).
6. Swagger sends `Authorization: Bearer <token>` automatically on all subsequent requests.
