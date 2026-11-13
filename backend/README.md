# NASA Moon — ASP.NET Core Backend

Backend for the **CLPS Lunar Mission Browser** (`Nasa-moon` frontend), built with
**ASP.NET Core 8 + Entity Framework Core + PostgreSQL (Neon)** in **Clean Architecture**

## Structure

```
backend/
├── NasaMoon.sln
└── src/
    ├── Domain/            # Entities only: Mission, Payload, Source, DataSnapshot
    │   └── Entities/
    ├── Application/       # DTOs, repository + service interfaces, services, mappings, DI
    │   ├── Dtos/          # MissionDto, PayloadDto, SourceDto, Create/UpdateMissionDto, MissionFilter,
    │   │                  # SnapshotDto, SnapshotDiffDto
    │   ├── Interfaces/    # IMissionRepository, IPayloadRepository, ISourceRepository,
    │   │                  # ISnapshotRepository, IMissionService, IPayloadService, ISnapshotService
    │   ├── Mappings/      # MissionMapper (entity <-> DTO), SnapshotMapper (snapshot JSON)
    │   └── Services/      # MissionService (search + filters), PayloadService,
    │                      # SnapshotService (snapshot + diff)
    ├── Infrastructure/    # EF Core: NasaMoonDbContext (Npgsql), repositories, seed data
    │   ├── Repositories/
    │   └── SeedData/      # DbSeeder + missions.json (copied from the frontend's src/data)
    └── Api/               # ASP.NET Core Web API: MissionsController, PayloadsController,
                           # SnapshotsController
```

## Entities (match the frontend's `src/data/missions.json` schema)

- **Mission** — `id` (e.g. `to2-ab`), name, taskOrder, provider, lander, status
  (`completed` | `failed` | `partial` | `upcoming`), launchDate, launchDatePrecision,
  landingDate, landingDatePrecision, landing site (name, latitude, longitude, region,
  coordinatesApproximate), payloadCountReported, objective, outcomeSummary, notes,
  lastVerified + navigation to Payloads and Sources.
- **Payload** — name, organization, purpose, type (belongs to a Mission).
- **Source** — label, url, accessed (belongs to a Mission).
- **DataSnapshot** — a saved copy of the mission dataset at a point in time:
  label, source (`live` = current database, `static` = bundled missions.json),
  createdAt, and the full missions JSON array (MissionDto shape) for research
  comparisons.

## API endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/missions?search=&status=&provider=&launchYear=&region=` | List missions, free-text search + filters (same filters as the frontend FilterBar) |
| GET | `/api/missions/{id}` | One mission with payloads + sources |
| POST | `/api/missions` | Create a mission |
| PUT | `/api/missions/{id}` | Update a mission |
| DELETE | `/api/missions/{id}` | Delete a mission (payloads + sources cascade) |
| GET | `/api/missions/{missionId}/payloads` | Payloads of one mission |
| POST | `/api/snapshots` | Save a snapshot: `{ "label": "Before update", "source": "live" }` (`source` is `live` or `static`, defaults to `live`) → 201 |
| GET | `/api/snapshots` | List snapshots, newest first (id, label, source, createdAt, missionCount) |
| GET | `/api/snapshots/{id}` | One snapshot with its missions |
| DELETE | `/api/snapshots/{id}` | Delete a snapshot → 204 |
| GET | `/api/snapshots/compare?fromId={guid}&toId={guid\|live}` | Diff two datasets: added / removed / changed missions (each change lists Field, OldValue, NewValue — name, status, dates, landing site, region, provider, lander, payload count, objective) |

## Research snapshots

Visitors can freeze the dataset and compare it later (frontend: **Research**
page at `/research`, client in `src/lib/api.ts`):

1. `POST /api/snapshots` with a label — saves the live database (or the
   bundled static data when `source` is `static`).
2. Later, `GET /api/snapshots/compare?fromId=…&toId=live` shows what changed
   since that snapshot — new missions (Added), gone missions (Removed), and
   field-by-field changes (Changed), e.g. a mission whose status moved from
   `failed` to `completed`.

Swagger UI: `http://localhost:5083/swagger` (Development).

CORS allows the Vite dev server (`http://localhost:5173`) and the deployed sites
(`https://clps-agent-kit.vercel.app`, `https://nasa-moon.vercel.app`).

## Run it

### 1. Create a Neon PostgreSQL database (free)

1. Go to https://neon.tech → create a project → create a database (e.g. `nasamoon`).
2. Copy the connection string from the Neon dashboard. It looks like:
   `Host=ep-xxx.us-east-2.aws.neon.tech;Username=...;Password=...;Database=nasamoon;SSL Mode=Require`

### 2. Set the connection string

**Do NOT commit real credentials.** Use one of these:

```bash
# Option A — user secrets (recommended for local dev)
cd src/Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=ep-xxx...;Username=...;Password=...;Database=nasamoon;SSL Mode=Require;Trust Server Certificate=true"

# Option B — environment variable
export ConnectionStrings__DefaultConnection="Host=ep-xxx...;..."
```

`src/Api/appsettings.json` only contains a clearly-marked placeholder
(`<NEON_HOST>`, `<NEON_USER>`, `<NEON_PASSWORD>`) — replace it via secrets/env,
never by committing the real values.

### 3. Create the database tables

```bash
cd backend
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

(`dotnet-ef` tool: `dotnet tool install -g dotnet-ef` if missing.)
On first run the API also seeds the 15 CLPS missions + 50 payloads from the
frontend's static `missions.json` — only when the database is empty.

### 4. Run the API

```bash
cd src/Api
dotnet run
```

Then open `http://localhost:5083/swagger` and try `GET /api/missions`.

### Frontend wiring (done)

The React app is wired to this API — see `src/lib/api.ts`:

- `VITE_API_URL` (copy `.env.example` to `.env`) points at the API:
  `VITE_API_URL=http://localhost:5083`
- `main.tsx` awaits `initMissions()` before the app renders: the app fetches
  `GET /api/missions` and uses the API data. **If `VITE_API_URL` is unset or
  the API is unreachable, the site silently falls back to the bundled static
  `src/data/missions.json`** — it never breaks.
- `src/lib/api.ts` also exports `getMissionById(id)` and `getPayloads(missionId)`
  (`GET /api/missions/{id}`, `GET /api/missions/{id}/payloads`), and maps the
  API DTOs to the exact `Mission` shape the UI already expects.

Run backend + frontend together locally:

```bash
# Terminal 1 — API (from backend/)
cd src/Api && dotnet run            # http://localhost:5083

# Terminal 2 — frontend (from repo root)
cp .env.example .env                # VITE_API_URL=http://localhost:5083
npm install && npm run dev          # http://localhost:5173
```

Live deployment still needs a host for the API (e.g. Render / Railway) plus a
real PostgreSQL database (Neon) — then set `VITE_API_URL` to the deployed API
URL in the frontend's hosting settings (Vercel env vars).
