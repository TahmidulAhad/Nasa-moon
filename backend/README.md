# NASA Moon — ASP.NET Core Backend

Backend for the **CLPS Lunar Mission Browser** (`Nasa-moon` frontend), built with
**ASP.NET Core 8 + Entity Framework Core + PostgreSQL (Neon)** in **Clean Architecture**

## Structure

```
backend/
├── NasaMoon.sln
└── src/
    ├── Domain/            # Entities only: Mission, Payload, Source
    │   └── Entities/
    ├── Application/       # DTOs, repository + service interfaces, services, mappings, DI
    │   ├── Dtos/          # MissionDto, PayloadDto, SourceDto, Create/UpdateMissionDto, MissionFilter
    │   ├── Interfaces/    # IMissionRepository, IPayloadRepository, ISourceRepository,
    │   │                  # IMissionService, IPayloadService
    │   ├── Mappings/      # MissionMapper (entity <-> DTO)
    │   └── Services/      # MissionService (search + filters), PayloadService
    ├── Infrastructure/    # EF Core: NasaMoonDbContext (Npgsql), repositories, seed data
    │   ├── Repositories/
    │   └── SeedData/      # DbSeeder + missions.json (copied from the frontend's src/data)
    └── Api/               # ASP.NET Core Web API: MissionsController, PayloadsController
```

## Entities (match the frontend's `src/data/missions.json` schema)

- **Mission** — `id` (e.g. `to2-ab`), name, taskOrder, provider, lander, status
  (`completed` | `failed` | `partial` | `upcoming`), launchDate, launchDatePrecision,
  landingDate, landingDatePrecision, landing site (name, latitude, longitude, region,
  coordinatesApproximate), payloadCountReported, objective, outcomeSummary, notes,
  lastVerified + navigation to Payloads and Sources.
- **Payload** — name, organization, purpose, type (belongs to a Mission).
- **Source** — label, url, accessed (belongs to a Mission).

## API endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/missions?search=&status=&provider=&launchYear=&region=` | List missions, free-text search + filters (same filters as the frontend FilterBar) |
| GET | `/api/missions/{id}` | One mission with payloads + sources |
| POST | `/api/missions` | Create a mission |
| PUT | `/api/missions/{id}` | Update a mission |
| DELETE | `/api/missions/{id}` | Delete a mission (payloads + sources cascade) |
| GET | `/api/missions/{missionId}/payloads` | Payloads of one mission |

Swagger UI: `https://localhost:5001/swagger` (Development).

CORS allows the Vite dev server (`http://localhost:5173`) and the deployed site
(`https://clps-agent-kit.vercel.app`).

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

Then open `https://localhost:5001/swagger` and try `GET /api/missions`.

### Frontend wiring (next step)

Point the React app at the API instead of the static JSON, e.g. in a `.env` file:

```
VITE_API_URL=https://localhost:5001
```

and fetch `/api/missions` in place of `missions.json`.
