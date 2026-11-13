# NASA Moon — ASP.NET Core Backend

Backend for the **CLPS Lunar Mission Browser** (`Nasa-moon` frontend), built with
**ASP.NET Core 8 + Entity Framework Core + PostgreSQL (Neon)** in **Clean Architecture**

## Structure

```\nbackend/\n├── NasaMoon.sln\n└── src/\n    ├── Domain/            # Entities only: Mission, Payload, Source, DataSnapshot,\n    │   └── Entities/      # User, RefreshToken, SavedItem, Collection, CollectionItem,\n    │                      # SavedItemTag, AuditLog\n    ├── Application/       # DTOs, repository + service interfaces, services, mappings, DI\n    │   ├── Dtos/          # MissionDto, PayloadDto, SourceDto, Create/UpdateMissionDto, MissionFilter,\n    │   │                  # SnapshotDto, SnapshotDiffDto, AuthDto, LibraryDto, CollectionDto\n    │   ├── Interfaces/    # IMissionRepository, IPayloadRepository, ISourceRepository,\n    │   │                  # ISnapshotRepository, IMissionService, IPayloadService, ISnapshotService,\n    │   │                  # IUserRepository, IRefreshTokenRepository, ILibraryRepository,\n    │   │                  # IAuditLogRepository, IAuthService, ILibraryService,\n    │   │                  # IExportService, IAuditLogService, IPasswordHasher, IJwtTokenService\n    │   ├── Mappings/      # MissionMapper (entity <-> DTO), SnapshotMapper (snapshot JSON),\n    │   │                  # LibraryMapper, UserMapper\n    │   └── Services/      # MissionService (search + filters), PayloadService,\n    │                      # SnapshotService (snapshot + diff), AuthService,\n    │                      # LibraryService, ExportService, AuditLogService\n    ├── Infrastructure/    # EF Core: NasaMoonDbContext (Npgsql), repositories, seed data\n    │   ├── Repositories/  # + User/RefreshToken/Library/AuditLog repositories,\n    │   │                  # BCrypt password hasher, JWT token service\n    │   └── SeedData/      # DbSeeder + missions.json (copied from the frontend's src/data)\n    └── Api/               # ASP.NET Core Web API: MissionsController, PayloadsController,\n                           # SnapshotsController, AuthController, LibraryController,\n                           # CollectionsController, AdminController\n```

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
  comparisons. Since Researcher Accounts (Phase 1) it also has an optional
  `UserId` owner — snapshots without an owner are **legacy (public)** and stay
  readable by everyone.
- **User** — a researcher account: email (unique), displayName, BCrypt password
  hash, role (`Researcher` | `Admin`), createdAt.
- **RefreshToken** — a rotating 7-day refresh token (only its SHA-256 hash is
  stored; each use replaces it with a new one).
- **SavedItem** — one mission saved to a researcher's library (UserId,
  MissionId, personal note, createdAt) + **SavedItemTag** (one tag per row).
- **Collection** — a researcher's folder of missions (UserId, name,
  description) + **CollectionItem** (CollectionId, MissionId).
- **AuditLog** — who changed mission data: UserId, action (`mission.create`,
  `mission.update`, `mission.delete`, `snapshot.create`, `snapshot.delete`),
  entity type/id, createdAt.

## API endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/missions?search=&status=&provider=&launchYear=&region=` | List missions, free-text search + filters (same filters as the frontend FilterBar) — **public** |
| GET | `/api/missions/{id}` | One mission with payloads + sources — **public** |
| POST | `/api/missions` | Create a mission — **Admin only** |
| PUT | `/api/missions/{id}` | Update a mission — **Admin only** |
| DELETE | `/api/missions/{id}` | Delete a mission (payloads + sources cascade) — **Admin only** |
| GET | `/api/missions/{missionId}/payloads` | Payloads of one mission — **public** |
| POST | `/api/auth/register` | Register a researcher `{ email, password, displayName }` → access + refresh token + user (first user becomes Admin, see below) |
| POST | `/api/auth/login` | Log in `{ email, password }` → access + refresh token + user |
| POST | `/api/auth/refresh` | Rotate the refresh token `{ refreshToken }` → new tokens |
| POST | `/api/auth/logout` | Revoke one refresh token `{ refreshToken }` → 204 |
| GET | `/api/auth/me` | The logged-in researcher — **login required** |
| POST | `/api/auth/change-password` | Change password `{ currentPassword, newPassword }` — revokes all refresh tokens (logout everywhere) |
| GET | `/api/library?search=` | My saved missions (search matches name, note or tag) — **login required** |
| POST | `/api/library` | Save a mission `{ missionId, note?, tags? }` (idempotent) — **login required** |
| PUT | `/api/library/{id}` | Change my note / tags on a saved item — **login required** |
| DELETE | `/api/library/{id}` | Remove a mission from my library — **login required** |
| GET | `/api/library/export?format=json\|csv` | Download my whole library as a file — **login required** |
| GET | `/api/collections` | My collections — **login required** |
| POST | `/api/collections` | Create a collection `{ name, description? }` — **login required** |
| GET | `/api/collections/{id}` | One collection with its missions — **login required** |
| PUT | `/api/collections/{id}` | Rename / change description — **login required** |
| DELETE | `/api/collections/{id}` | Delete a collection — **login required** |
| POST | `/api/collections/{id}/items` | Add a mission `{ missionId }` — **login required** |
| DELETE | `/api/collections/{id}/items/{missionId}` | Remove a mission — **login required** |
| GET | `/api/collections/{id}/export?format=json\|csv` | Download one collection — **login required** |
| GET | `/api/admin/users` | All researcher accounts — **Admin only** |
| GET | `/api/admin/audit-logs?count=` | Newest audit entries — **Admin only** |
| POST | `/api/snapshots` | Save a snapshot: `{ "label": "Before update", "source": "live" }` (`source` is `live` or `static`, defaults to `live`) → 201 — **login required** (owned by the caller) |
| GET | `/api/snapshots` | List visible snapshots, newest first (id, label, source, createdAt, missionCount, userId, isLegacy) — mine + legacy public ones; admins see all |
| GET | `/api/snapshots/{id}` | One snapshot with its missions (404 for other researchers' snapshots) |
| DELETE | `/api/snapshots/{id}` | Delete a snapshot (owner or Admin; legacy: Admin only) → 204 |
| GET | `/api/snapshots/{id}/export?format=json\|csv` | Download a snapshot's missions (must be visible to the caller) |
| GET | `/api/snapshots/compare?fromId={guid}&toId={guid\|live}` | Diff two datasets: added / removed / changed missions (each change lists Field, OldValue, NewValue — name, status, dates, landing site, region, provider, lander, payload count, objective). Both snapshots must be visible to the caller |

## Researcher accounts (Phase 1)

Each researcher gets their own account and keeps their research in it, so the
same data never has to be searched twice. The frontend pages are `/login`,
`/register`, `/my-library` (saved missions + notes/tags + export), `/collections`,
`/profile`, `/admin` (mission management, accounts, audit log); the public
`/library` page (Research Library of conclusions) is unchanged, and mission
browsing stays public for logged-out visitors.

**Decisions for Phase 1** (approved by the user, 2026-11-13):

1. Registration is **open** — anyone can register.
2. **No email verification** in Phase 1 (no email service yet) — accounts are
   active immediately.
3. Legacy (ownerless) snapshots stay **public**, readable by everyone.
4. Export is **JSON/CSV only** (no PDF).
5. Collections are **private only** (no sharing).
6. **The FIRST registered user automatically becomes Admin** (bootstrap rule).
   There is no seeded admin and no seeded password — whoever registers first
   gets the Admin role; every later registration is a Researcher.

**Auth setup — `Jwt__Secret` is required in Production.** The API reads the
JWT signing secret from configuration key `Jwt:Secret` (env var `Jwt__Secret`).
In Development a missing/placeholder secret falls back to a random ephemeral
one (tokens stop working on restart); **in Production the API fails fast at
startup without a real secret (≥ 32 characters)** — set it before deploying:

```bash
# Local dev (user secrets)
cd src/Api
dotnet user-secrets set "Jwt:Secret" "<a-long-random-string-at-least-32-chars>"

# Render (production) — set in the dashboard, never commit it:
#   Jwt__Secret = <a-long-random-string-at-least-32-chars>
# Generate one with:  openssl rand -base64 48
```

Other auth details: access tokens are JWTs (1h), refresh tokens are opaque
random values rotated on every use (7d, only the SHA-256 hash is stored);
passwords are hashed with BCrypt (work factor 11); login/register are rate
limited to 10 attempts/minute per IP; every mission write and every snapshot
create/delete is written to the audit log (`GET /api/admin/audit-logs`).

Frontend auth client: `src/lib/auth.ts` (login/register/refresh/me, token
storage in localStorage, `authFetch` with Bearer header + one 401→refresh→retry),
`src/lib/authContext.tsx` (AuthProvider), `src/lib/library.ts` (library /
collections / admin calls), pages in `src/pages/` (LoginPage, RegisterPage,
MyLibraryPage, CollectionsPage, ProfilePage, AdminPage).

## Research snapshots

Logged-in researchers can freeze the dataset and compare it later
(frontend: **Research** page at `/research`, client in `src/lib/api.ts`).
Snapshots belong to the researcher who saves them; snapshots saved before
accounts existed are legacy (public) and stay readable by everyone:

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
