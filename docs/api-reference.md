# API Reference

All protected endpoints require the `Authorization: Bearer <token>` header (see [api-key.md](api-key.md)).  
Get tokens from `POST /api/auth/token` with API username/password.
Base path: `/api`

---

## Authentication

### POST /api/auth/bootstrap-user

Creates the first API user in the database. Returns **409** once any user already exists — this is a one-time operation.

**Auth:** Public  
**Rate limit:** 60 requests / minute per IP

**Request body**

```json
{
  "username": "api-user",
  "password": "your-strong-password"
}
```

**Response 201**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "api-user",
  "createdUtc": "2026-05-03T08:00:00+00:00"
}
```

**Error responses**

| Status | Reason |
|---|---|
| 400 | Missing username or password |
| 409 | An API user already exists |

---

### POST /api/auth/token

Returns a bearer token for valid API login credentials stored in the database.

**Auth:** Public  
**Rate limit:** 60 requests / minute per IP

**Request body**

```json
{
  "username": "api-user",
  "password": "your-strong-password"
}
```

**Response 200**

```json
{
  "accessToken": "<jwt-token>",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-05-03T09:00:00+00:00"
}
```

**Error responses**

| Status | Reason |
|---|---|
| 400 | Missing username or password |
| 401 | Invalid username or password |

---

## Bearer Header

| Header | Value |
|---|---|
| `Authorization` | `Bearer <your-token>` |

Missing or invalid token → **401 Unauthorized**

---

## Stations

### GET /api/stations

Returns all known weather stations stored in the local database.

**Auth:** Required  
**Rate limit:** 120 requests / minute per token

**Response 200**

```json
[
  {
    "stationId": "S108",
    "name": "Marina Barrage",
    "latitude": 1.2799,
    "longitude": 103.8703
  }
]
```

---

## Weather Readings

### GET /api/weather/current

Returns the latest reading per metric for a given station. Triggers a live sync for all 5 metrics before returning.  
Returns an empty array if the station id is not found — no fallback to nearby stations.

**Auth:** Required  
**Rate limit:** 120 requests / minute per token

**Query parameters**

| Parameter | Type | Required | Description |
|---|---|---|---|
| `stationId` | string | Yes | Exact station id, e.g. `S108` |

**Response 200**

```json
[
  {
    "stationId": "S108",
    "stationName": "Marina Barrage",
    "metric": "AirTemperature",
    "value": 28.4,
    "unit": "deg C",
    "readingType": "DBT 1M F",
    "timestampUtc": "2026-05-03T10:00:00+00:00",
    "latitude": 1.2799,
    "longitude": 103.8703
  }
]
```

> Returns up to 5 items — one per metric that has data for the station. Metrics with no stored readings are omitted.

**Error responses**

| Status | Reason |
|---|---|
| 400 | `stationId` is missing or blank |
| 401 | Invalid or missing bearer token |

---

### GET /api/weather/historical

Returns all readings for a station for a specific metric across the entire selected day. Syncs that metric/day from upstream before querying.

**Auth:** Required  
**Rate limit:** 120 requests / minute per token

**Query parameters**

| Parameter | Type | Required | Description |
|---|---|---|---|
| `stationId` | string | Yes | Exact station id, e.g. `S108` |
| `metric` | string | Yes | One of: `AirTemperature`, `Rainfall`, `RelativeHumidity`, `WindDirection`, `WindSpeed` |
| `date` | DateOnly | Yes | Date to retrieve (`YYYY-MM-DD`). The whole day is returned. |

**Example request**

```http
GET /api/weather/historical?stationId=S108&metric=AirTemperature&date=2026-05-01
Authorization: Bearer <token>
```

**Response 200**

```json
{
  "date": "2026-05-01",
  "metric": "AirTemperature",
  "fromUtc": "2026-05-01T00:00:00+00:00",
  "toUtc": "2026-05-01T23:59:59.9999999+00:00",
  "readings": [
    {
      "stationId": "S108",
      "stationName": "Marina Barrage",
      "metric": "AirTemperature",
      "value": 27.6,
      "unit": "deg C",
      "readingType": "DBT 1M F",
      "timestampUtc": "2026-05-01T08:00:00+00:00",
      "latitude": 1.2799,
      "longitude": 103.8703
    }
  ]
}
```

**Error responses**

| Status | Reason |
|---|---|
| 400 | `stationId`, `metric`, or `date` is missing or invalid |
| 401 | Invalid or missing bearer token |

---

### GET /api/weather/forecast

Returns the latest 4-day weather outlook from the upstream Singapore API.

**Auth:** Required  
**Rate limit:** 120 requests / minute per token

**Query parameters:** None required

**Example request**

```http
GET /api/weather/forecast
Authorization: Bearer <token>
```

**Response 200**

```json
[
  {
    "timestampUtc": "2026-05-03T00:00:00+00:00",
    "day": "Saturday",
    "temperatureLow": 26,
    "temperatureHigh": 33,
    "temperatureUnit": "Degrees Celsius",
    "humidityLow": 55,
    "humidityHigh": 90,
    "humidityUnit": "Percentage",
    "forecastSummary": "Fair and Warm",
    "forecastText": "Fair and warm",
    "windDirection": "SSE",
    "windSpeedLow": 10,
    "windSpeedHigh": 20,
    "windSpeedUnit": "km/h"
  }
]
```

**Error responses**

| Status | Reason |
|---|---|
| 401 | Invalid or missing bearer token |

---

## Health Checks

These routes are **public** (no bearer token required).

| Route | Description |
|---|---|
| `GET /health/live` | Returns 200 if the process is running |
| `GET /health/ready` | Returns 200 if the database is reachable |

---

## Error Response Shape

All 4xx errors return an [RFC 7807 ProblemDetails](https://tools.ietf.org/html/rfc7807) body:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "stationId is required.",
  "traceId": "00-abc123-def456-00"
}
```

Returns a bearer token for valid API login credentials.

**Auth:** Public  
**Rate limit:** 60 requests / minute per IP

**Request body**

```json
{
  "username": "api-user",
  "password": "api-password"
}
```

**Response 200**

```json
{
  "accessToken": "<jwt-token>",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-05-02T12:00:00+00:00"
}
```

**Error responses**

| Status | Reason |
|---|---|
| 400 | Missing username or password |
| 401 | Invalid username or password |

---

## Authentication

| Header | Value |
|---|---|
| `Authorization` | `Bearer <your-token>` |

Missing or invalid token → **401 Unauthorized**

---

## Stations

### GET /api/stations

Returns all known weather stations stored in the local database.

**Auth:** Required  
**Rate limit:** 120 requests / minute per token

**Response 200**

```json
[
  {
    "stationId": "S108",
    "deviceId": "S108",
    "name": "Marina Barrage",
    "latitude": 1.2799,
    "longitude": 103.8703
  }
]
```

---

## Weather Readings

### GET /api/weather/current

Returns the latest reading for all 5 metrics for a given station. Triggers a live sync from upstream before returning.

**Auth:** Required  
**Rate limit:** 120 requests / minute per token

**Query parameters**

| Parameter | Type | Required | Description |
|---|---|---|---|
| `stationId` | string | Yes | Exact station id, e.g. `S108` |

**Response 200**

```json
[
  {
    "stationId": "S108",
    "stationName": "Marina Barrage",
    "metric": "AirTemperature",
    "value": 28.4,
    "unit": "°C",
    "readingType": "current",
    "timestampUtc": "2026-05-02T10:00:00+00:00",
    "latitude": 1.2799,
    "longitude": 103.8703
  }
]
```

**Error responses**

| Status | Reason |
|---|---|
| 400 | `stationId` is missing or blank |
| 401 | Invalid or missing bearer token |

---

### GET /api/weather/historical

Returns all readings for a station within a date range (max 7 days). Syncs missing days before querying.

**Auth:** Required  
**Rate limit:** 120 requests / minute per token

**Query parameters**

| Parameter | Type | Required | Description |
|---|---|---|---|
| `stationId` | string | Yes | Exact station id, e.g. `S108` |
| `fromUtc` | DateTimeOffset | Yes | Start of range (ISO 8601) |
| `toUtc` | DateTimeOffset | Yes | End of range (ISO 8601, max 7 days after `fromUtc`) |

**Example request**

```http
GET /api/weather/historical?stationId=S108&fromUtc=2026-04-25T00:00:00Z&toUtc=2026-05-02T00:00:00Z
Authorization: Bearer dev-local-token
```

**Response 200**

```json
{
  "from": "2026-04-25T00:00:00+00:00",
  "to": "2026-05-02T00:00:00+00:00",
  "readings": [
    {
      "stationId": "S108",
      "stationName": "Marina Barrage",
      "metric": "AirTemperature",
      "value": 27.6,
      "unit": "°C",
      "readingType": "historical",
      "timestampUtc": "2026-04-25T08:00:00+00:00",
      "latitude": 1.2799,
      "longitude": 103.8703
    }
  ]
}
```

**Error responses**

| Status | Reason |
|---|---|
| 400 | `stationId` missing, `toUtc` < `fromUtc`, or range exceeds 7 days |
| 401 | Invalid or missing bearer token |

---

### GET /api/weather/forecast

Returns a linear trend projection for all 5 metrics using the last 8 readings per metric.

**Auth:** Required  
**Rate limit:** 120 requests / minute per token

**Query parameters**

| Parameter | Type | Required | Description |
|---|---|---|---|
| `stationId` | string | Yes | Exact station id, e.g. `S108` |

**Response 200**

```json
[
  {
    "metric": "AirTemperature",
    "stationId": "S108",
    "currentValue": 28.4,
    "forecastValue": 29.1,
    "unit": "°C",
    "forecastTimestampUtc": "2026-05-02T11:00:00+00:00",
    "dataPointsUsed": 8
  }
]
```

> Metrics with no data for the station are omitted (no 404 per metric).

**Error responses**

| Status | Reason |
|---|---|
| 400 | `stationId` is missing or blank |
| 401 | Invalid or missing bearer token |

---

### GET /api/weather/export

Returns all readings for a station as a CSV file.

**Auth:** Required  
**Rate limit:** 120 requests / minute per token

**Query parameters**

| Parameter | Type | Required | Description |
|---|---|---|---|
| `stationId` | string | Yes | Exact station id |
| `fromUtc` | DateTimeOffset | No | Start of range (ISO 8601) |
| `toUtc` | DateTimeOffset | No | End of range (ISO 8601) |

**Response 200** — `Content-Type: text/csv`

```
StationId,StationName,Metric,Value,Unit,TimestampUtc
S108,Marina Barrage,AirTemperature,28.4,°C,2026-05-02T10:00:00+00:00
```

**Error responses**

| Status | Reason |
|---|---|
| 400 | `stationId` is missing or blank |
| 401 | Invalid or missing bearer token |

---

### POST /api/weather/sync

Triggers an on-demand sync for all 5 metrics from the upstream Singapore API.

**Auth:** Required  
**Rate limit:** 120 requests / minute per token

**Request body:** None required

**Response 200**

```json
{ "synced": true }
```

---

## Health Checks

These routes are **public** (no bearer token required).

| Route | Description |
|---|---|
| `GET /health/live` | Returns 200 if the process is running |
| `GET /health/ready` | Returns 200 if the database is reachable |

---

## Error Response Shape

All 4xx errors return an [RFC 7807 ProblemDetails](https://tools.ietf.org/html/rfc7807) body:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "stationId is required.",
  "traceId": "00-abc123-def456-00"
}
```
