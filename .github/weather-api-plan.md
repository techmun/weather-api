# Weather API — Product Plan

## Upstream Data Source
Base URL: `https://api-open.data.gov.sg/v2/real-time/api/`

Five metrics available. Each metric endpoint returns an array of **stations** (id, deviceId, name, latitude, longitude) and an array of **readings** (stationId, value, timestamp). Historical data is fetched by passing `?date=YYYY-MM-DD`.

| Metric | Upstream path | Unit |
|---|---|---|
| AirTemperature | `/air-temperature` | °C |
| Rainfall | `/rainfall` | mm |
| RelativeHumidity | `/relative-humidity` | % |
| WindDirection | `/wind-direction` | ° |
| WindSpeed | `/wind-speed` | km/h |

---

## Persistence
- SQLite (local dev) via EF Core, code-first.
- Tables: `WeatherStations`, `WeatherReadings`, `AlertSubscriptions`, `ApiUsers`.
- Unique index on `WeatherReadings(StationId, Metric, TimestampUtc)` to prevent duplicates.
- `AlertSubscriptions`: columns `Id` (Guid), `Email` (string, unique index), `SubscribedUtc` (DateTimeOffset).
- `ApiUsers`: columns `Id` (Guid), `Username` (string, unique index), `PasswordHash` (PBKDF2), `CreatedUtc` (DateTimeOffset), `IsActive` (bool).
- Background hosted service polls all metrics on a configurable interval (default 30 min).

---

## API Endpoints

### Authentication
| # | Method | Route | Auth | Status |
|---|---|---|---|---|
| T1 | POST | `/api/auth/token` | Public | ☑ Done |
| T3 | POST | `/api/auth/bootstrap-user` | Public (one-time) | ☑ Done |

**T1** — Accepts `{ username, password }` and returns a signed bearer token for valid API users stored in DB.

**T3** — Accepts `{ username, password }` and creates the first API user in DB (returns 409 once DB already has users).

---

### Stations
| # | Method | Route | Auth | Status |
|---|---|---|---|---|
| S1 | GET | `/api/stations` | Bearer Token | ☐ Not started |

**S1** — Returns all known stations from the local DB (id, name, lat/lon). No upstream call.

---

### Weather Readings

> **Design rules**
> - `stationId` is **required** on W1 and W2. Return 400 if missing or blank.
> - `metric` is required on W2.
> - `date` is required on W2 and is interpreted as the whole selected day.
> - Station is matched by exact station id (example: `S108`).

| # | Method | Route | Auth | Status |
|---|---|---|---|---|
| W1 | GET | `/api/weather/current` | Bearer Token | ☐ Not started |
| W2 | GET | `/api/weather/historical` | Bearer Token | ☐ Not started |
| W3 | GET | `/api/weather/forecast` | Bearer Token | ☐ Not started |
| W4 | GET | `/api/weather/export` | Bearer Token | ☐ Not started |

**W1 — Current**
- Query params: `stationId` (**required**, exact station id)
- Triggers a sync for all 5 metrics, then returns the latest reading per metric for that station.
- Response: `[{ stationId, stationName, metric, value, unit, readingType, timestampUtc, latitude, longitude }]`
- 400 if `stationId` is missing or blank.

**W2 — Historical**
- Query params: `stationId` (**required**), `metric` (**required**), `date` (**required**)
- Syncs the selected day before querying local DB.
- Response: `{ from, to, readings: [{ stationId, stationName, metric, value, unit, readingType, timestampUtc, latitude, longitude }] }`
- 400 if `stationId`, `metric`, or `date` is missing or invalid.

**W3 — Forecast (4-day outlook)**
- No query params required.
- Calls upstream `/four-day-outlook` and returns the latest 4-day forecast list.
- Response: `[{ timestampUtc, day, temperatureLow, temperatureHigh, temperatureUnit, humidityLow, humidityHigh, humidityUnit, forecastSummary, forecastText, windDirection, windSpeedLow, windSpeedHigh, windSpeedUnit }]`

**W4 — CSV Export**
- Query params: `stationId` (**required**), `fromUtc`, `toUtc`
- Returns `text/csv` with all 5 metrics for the station.
- Requires `Authorization: Bearer <token>` header.
- 400 if `stationId` is missing or blank.

---

### Manual Sync
| # | Method | Route | Auth | Status |
|---|---|---|---|---|
| Y1 | POST | `/api/weather/sync` | Bearer Token | ☐ Not started |

**Y1** — Triggers an on-demand sync for all 5 metrics. No body required.

---

### Alert Subscriptions

> **Design rules**
> - A subscriber is identified solely by their email address.
> - Subscribe stores the email. Unsubscribe removes it by id. Send dispatches a dummy email log to all stored subscribers.
> - No threshold/comparison logic — subscriptions are a simple mailing list.

| # | Method | Route | Auth | Status |
|---|---|---|---|---|
| A1 | GET | `/api/alerts/subscriptions` | Bearer Token | ☐ Not started |
| A2 | POST | `/api/alerts/subscribe` | Bearer Token | ☐ Not started |
| A3 | DELETE | `/api/alerts/subscriptions/{id}` | Bearer Token | ☐ Not started |
| A4 | POST | `/api/alerts/send` | Bearer Token | ☐ Not started |

**A1 — List subscribers**
- Returns all stored subscriber records.
- Response: `[{ id, email, subscribedUtc }]`

**A2 — Subscribe**
- Body: `{ email }` (required, must be valid email format)
- Stores the email in `AlertSubscriptions`. Returns 409 if already subscribed.
- Response 201: `{ id, email, subscribedUtc }`

**A3 — Unsubscribe**
- Deletes the subscription record by `id` (Guid).
- Returns 204 on success, 404 if not found.

**A4 — Send alert (dummy)**
- Body: `{ subject, message }` (both required)
- Iterates all active subscribers and logs one structured entry per subscriber:
	`"[DUMMY EMAIL] To: {email} | Subject: {subject} | Body: {message}"`
- Response 200: `{ recipientCount, subject }`
- Does not send real email. Logging only.

---

## Security
- Bearer token authentication via `Authorization: Bearer <token>` header.
- Tokens are issued by `POST /api/auth/token` for API users stored in DB.
- JWT signing key and token settings are configured in `appsettings.json`/user-secrets.
- First API user can be bootstrapped via `POST /api/auth/bootstrap-user`; subsequent user provisioning should be controlled operationally.
- Protected routes: all `/api/*` routes (S1, W1, W2, W3, W4, Y1, A1, A2, A3, A4).
- Public routes: non-API operational endpoints only (`/`, `/health/live`, `/health/ready`).
- Secrets (user passwords and signing key) must never be committed to source control (use user-secrets locally, Azure Key Vault in production).

### Security Best Practices (mandatory)
- Input validation on all query/body parameters with clear 400 responses (`ProblemDetails`).
- Email validation on A2; enforce max lengths on `subject` and `message` on A4.
- Request size limits on JSON endpoints to reduce abuse risk.
- Username/password verification must avoid timing-attack leakage.
- Global exception handling middleware must hide internal details and return standardized `ProblemDetails`.
- Structured audit logs for protected endpoints (who, route, result, correlation id) with sensitive data masked.
- CORS restricted to configured origins (no wildcard in production).
- Security headers enabled: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and strict transport security in production.
- API rate limiting:
	- Protected endpoints use per-key limits.
- Optional extra anti-abuse control on A2 (`/api/alerts/subscribe`): cooldown window per email.

---

## Resiliency (upstream calls)
- `HttpClient` with Polly standard resilience handler (retry + timeout).
- Cancellation tokens threaded through all async methods.
- Structured logging on sync start/end/error.

### Resiliency Best Practices (mandatory)
- Define explicit timeout budget per upstream request and per API request.
- Use exponential backoff + jitter retries only for transient failures (5xx, timeouts, network).
- Avoid retry storms: cap max retries and honor `Retry-After` when present.
- Ensure weather sync operations are idempotent (duplicate-safe writes enforced by DB unique index and dedup logic).
- Fail gracefully when one upstream metric fails: continue other metrics and return partial-result metadata where applicable.
- Background sync uses bounded execution time and skips overlap if a prior run is still active.
- Health checks:
	- `live` check for process readiness.
	- `ready` check validates DB connectivity and critical dependencies.
- Add correlation id propagation from incoming request to logs and downstream HTTP calls.

---

## Infrastructure
- OpenAPI/Swagger enabled at `/` (root) in all environments.
- CI/CD: GitHub Actions — build → test → publish → deploy to Azure App Service.
- EF Core migrations applied at startup via `EnsureCreated` (upgrade to proper migrations before production).

---

## Development Checklist

### Foundation
- [x] Solution and project created (.NET 10 Web API)
- [x] EF Core + SQLite wired up with `WeatherDbContext`
- [x] EF Core models: `WeatherStation`, `WeatherReading`, `AlertSubscription` (email + subscribedUtc only)
- [x] Initial migration / `EnsureCreated`
- [x] JWT bearer authentication configured
- [x] `appsettings.json` structure defined
- [x] Global exception handling + `ProblemDetails` configured
- [x] Security headers middleware configured
- [x] CORS policy from configuration
- [x] ASP.NET Core rate limiter configured (per-IP and per-key policies)
- [x] Health checks (`/health/live`, `/health/ready`) configured

### Ingestion
- [x] `ISingaporeWeatherApiClient` + implementation with Polly
- [x] `IWeatherIngestionService` — `SyncAllAsync`, `SyncDayAsync`
- [x] `WeatherSyncHostedService` background poller
- [x] Duplicate-prevention logic (HashSet dedup, not N+1 AnyAsync)

### Endpoints
- [x] S1 — GET /api/stations
- [x] W1 — GET /api/weather/current?stationId= (required, all 5 metrics)
- [x] W2 — GET /api/weather/historical?stationId= (required, all 5 metrics)
- [x] W3 — GET /api/weather/forecast?stationId= (required, all 5 metrics)
- [x] W4 — GET /api/weather/export?stationId= (Bearer token, all 5 metrics, CSV)
- [x] Y1 — POST /api/weather/sync (Bearer token)
- [x] A1 — GET /api/alerts/subscriptions (Bearer token)
- [x] A2 — POST /api/alerts/subscribe (Bearer token)
- [x] A3 — DELETE /api/alerts/subscriptions/{id} (Bearer token)
- [x] A4 — POST /api/alerts/send (Bearer token, dummy email log)
- [x] T1 — POST /api/auth/token (username/password to bearer token)
- [x] T3 — POST /api/auth/bootstrap-user (store first API user in DB)

### Quality
- [ ] Unit tests for `WeatherQueryService`
- [x] Input validation on all endpoints
- [x] Swagger annotations (`ProducesResponseType`, summaries)
- [x] Tests for Bearer token auth and authorization on protected routes
- [ ] Tests for rate limiting behavior (public and protected policies)
- [ ] Tests for resilient upstream behavior (retry/timeout/partial failures)
- [ ] Verify logs mask sensitive values (tokens, full email body if required)

### Delivery
- [ ] GitHub Actions CI (build + test)
- [ ] GitHub Actions CD (publish + deploy to Azure App Service)
- [ ] README with local run instructions
