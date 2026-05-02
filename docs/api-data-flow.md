## API Data Flow

### POST /api/auth/bootstrap-user
**Purpose:** Create the first API user (one-time operation).

```
Request: { username, password }
  ↓
Check: Does any ApiUser exist?
  ├─ YES → Return 409 Conflict
  └─ NO → Continue
  ↓
Hash password (PBKDF2 SHA-512)
  ↓
Insert into ApiUsers table
  ↓
Response: 201 Created with user details
```

---

### POST /api/auth/token
**Purpose:** Obtain a JWT bearer token.

```
Request: { username, password }
  ↓
Query ApiUsers table by username
  ├─ NOT FOUND → Return 401 Unauthorized
  └─ FOUND → Continue
  ↓
Verify password hash (PBKDF2 SHA-512)
  ├─ MISMATCH → Return 401 Unauthorized
  └─ MATCH → Continue
  ↓
Create JWT (Issuer, Audience, SigningKey, 60-min TTL)
  ↓
Response: 200 OK with { accessToken, tokenType, expiresAtUtc }
```

---

### GET /api/stations
**Purpose:** List all weather stations.

```
Request: Authorization: Bearer <token>
  ↓
Validate JWT signature & expiry
  ├─ INVALID → Return 401 Unauthorized
  └─ VALID → Continue
  ↓
Query WeatherStations table (all rows)
  ↓
Response: 200 OK with array of stations
```

---

### GET /api/weather/current?stationId=S108
**Purpose:** Get latest readings for all 5 metrics for one station.

```
Request: Authorization: Bearer <token>, Query: stationId
  ↓
Validate token & parameters
  ├─ INVALID → Return 400/401
  └─ VALID → Continue
  ↓
Trigger: Call SyncAllAsync (fetch all 5 metrics from upstream)
  ├─ For each metric:
  │   ├─ GET upstream data
  │   ├─ Upsert WeatherStations
  │   ├─ Deduplicate WeatherReadings (skip if exists)
  │   └─ Insert new readings
  └─ Return count of inserted rows
  ↓
Query WeatherReadings table for exact stationId
  ├─ STATION NOT FOUND → Return 200 OK with empty array
  ├─ STATION FOUND → Continue
  └─ Group by Metric
  ↓
For each metric: Select reading with newest TimestampUtc
  ↓
Response: 200 OK with array of latest readings (max 5 rows)
```

---

### GET /api/weather/historical?stationId=S108&metric=AirTemperature&date=2026-05-01
**Purpose:** Get all readings for one metric on one entire day.

```
Request: Authorization: Bearer <token>, Query: stationId, metric, date
  ↓
Validate token & parameters
  ├─ INVALID → Return 400/401
  └─ VALID → Continue
  ↓
Trigger: Call SyncMetricDayAsync (fetch only requested metric for that date)
  ├─ GET upstream data for metric + date
  ├─ Upsert WeatherStations
  ├─ Deduplicate WeatherReadings (skip if exists)
  └─ Insert new readings
  ↓
Calculate time range: date 00:00:00 UTC to 23:59:59.999 UTC
  ↓
Query WeatherReadings:
  ├─ Filter: stationId, metric, TimestampUtc in range
  └─ Sort: TimestampUtc ascending
  ↓
Response: 200 OK with
  {
    date, metric, fromUtc, toUtc,
    readings: [] (may be empty if no data synced)
  }
```

---

### GET /api/weather/forecast
**Purpose:** Get 4-day weather outlook from upstream.

```
Request: Authorization: Bearer <token>
  ↓
Validate token
  ├─ INVALID → Return 401 Unauthorized
  └─ VALID → Continue
  ↓
Call: GET upstream /four-day-outlook
  ↓
Parse response:
  ├─ Extract latest record (most recent timestamp)
  ├─ For each forecast item:
  │   ├─ Extract: TimestampUtc, Day, Temperature (Low/High), 
  │   │           Humidity (Low/High), ForecastSummary, Wind
  │   └─ Map to FourDayForecastResponse
  └─ Sort by TimestampUtc ascending
  ↓
Response: 200 OK with array of forecast items (typical: 4 days)
```

---

### Background Sync (Hosted Service)
**Purpose:** Automatically keep weather data fresh.

```
Every 30 minutes (configurable: WeatherApi:PollingIntervalMinutes):
  ↓
Call: SyncAllAsync for all 5 metrics
  ├─ For each metric:
  │   ├─ GET upstream data
  │   ├─ Upsert WeatherStations
  │   ├─ Deduplicate WeatherReadings
  │   └─ Insert new readings
  │   (If error: log warning, skip metric, continue)
  └─ Return total inserted rows
  ↓
Log: "Hosted sync completed. Inserted {count} rows."
```

---

## Database Schema

**WeatherStations** — One row per unique station  
| Column | Type | Notes |
|---|---|---|
| Id | TEXT PRIMARY KEY | Station ID (e.g., "S108") |
| DeviceId | TEXT | Device identifier |
| Name | TEXT | Station name |
| Latitude | DECIMAL | Geographic location |
| Longitude | DECIMAL | Geographic location |

**WeatherReadings** — One row per (station, metric, timestamp)  
| Column | Type | Notes |
|---|---|---|
| Id | INTEGER PRIMARY KEY | Auto-increment |
| StationId | TEXT FOREIGN KEY | References WeatherStations.Id |
| Metric | TEXT | AirTemperature, Rainfall, RelativeHumidity, WindSpeed, WindDirection |
| TimestampUtc | DATETIME | Reading timestamp |
| Value | DECIMAL | Metric value |
| Unit | TEXT | Metric unit (°C, mm, %, km/h, etc.) |
| ReadingType | TEXT | "current" or "historical" |
| **Unique Index** | (StationId, Metric, TimestampUtc) | Prevents duplicate readings |

**ApiUsers** — One row per API user  
| Column | Type | Notes |
|---|---|---|
| Id | TEXT PRIMARY KEY | UUID |
| Username | TEXT UNIQUE | User login name |
| PasswordHash | TEXT | PBKDF2 SHA-512 hash (format: `iterations.base64salt.base64hash`) |
| CreatedUtc | DATETIME | Account creation timestamp |
| IsActive | INTEGER (bool) | User status |

