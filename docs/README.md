# Weather API

A private .NET 10 Web API that ingests, stores, and serves real-time weather readings from Singapore government open data endpoints.

---

## Contents

| File | Description |
|---|---|
| [api-key.md] | Bearer token login, user bootstrap, and signing key configuration |
| [api-reference.md] | All endpoints — routes, parameters, responses, status codes |
| [api-data-flow.md] | All endpoints data flow |

---

## Upstream Data Source

Base URL: `https://api-open.data.gov.sg/v2/real-time/api/`

| Metric | Upstream path | Unit |
|---|---|---|
| Air Temperature | `/air-temperature` | °C |
| Rainfall | `/rainfall` | mm |
| Relative Humidity | `/relative-humidity` | % |
| Wind Direction | `/wind-direction` | ° |
| Wind Speed | `/wind-speed` | km/h |

4-day forecast is fetched from `/four-day-outlook`.

---

## Authentication

Every `/api/*` route requires the `Authorization: Bearer <token>` header.

1. On a fresh database, call `POST /api/auth/bootstrap-user` to create the first API user.
2. Call `POST /api/auth/token` with that username/password to obtain a JWT.
3. Send the JWT as `Authorization: Bearer <token>` on protected routes.

API user credentials are stored in the database (`ApiUsers` table).  
The JWT signing key is the only secret that lives in configuration — keep it in user-secrets locally and Azure Key Vault in production.

See [api-key.md](api-key.md) for full instructions.


