# Assignment & Submission Management System — Backend

ASP.NET Core 8 Web API organised as a Clean Architecture solution.

## Layout

```
backend/
├── AssignmentSubmissionSystem.sln
├── Directory.Build.props          # Shared build settings for every project
├── .editorconfig                  # Formatting + analyzer configuration
├── global.json                    # Pins the .NET SDK major version
├── docker-compose.yml             # Postgres + API + frontend
├── Dockerfile                     # Multi-stage build, non-root runtime
├── .env.example                   # Template — copy to .env
├── src/
│   ├── Domain/                    # Entities and enums. No dependencies.
│   ├── Application/               # Services, DTOs, validators, abstractions
│   ├── Infrastructure/            # EF Core persistence, repositories, security
│   └── Api/                       # Controllers, middleware, host configuration
└── tests/
    ├── UnitTests/                 # Service, validator and security unit tests
    └── IntegrationTests/          # Full-pipeline HTTP tests via WebApplicationFactory
```

Dependencies point inward: `Api → Application → Domain`, with `Infrastructure`
implementing the abstractions declared in `Application`.

## Configuration

All secrets live in `backend/.env`, which is git-ignored. Copy the template and fill it in:

```bash
cp .env.example .env
```

Generate a signing key of at least 32 characters (256 bits, required by HMAC-SHA256):

```bash
openssl rand -base64 48
```

`.env` is read by **both** run modes:

- **docker-compose** reads it natively.
- **`dotnet run`** reads it via `DotEnvConfigurationExtensions.AddDotEnvFile` in `Program.cs`.

Precedence, lowest to highest: `appsettings.json` → `appsettings.{Environment}.json` →
`.env` → real environment variables. `appsettings.Development.json` deliberately contains
no credentials, so a missing or too-short `Jwt__Key` fails the start-up with a clear message
rather than falling back to a checked-in default.

## Running

### With Docker

```bash
docker compose up --build
```

Brings up Postgres, the API and the Next.js frontend (built from `../frontend`):
API on `http://localhost:${API_PORT:-5000}`, frontend on `http://localhost:${FRONTEND_PORT:-3000}`,
Postgres on `${POSTGRES_PORT:-5434}`.

`ASPNETCORE_ENVIRONMENT` defaults to `Development` here so a fresh stack has seeded demo
accounts and Swagger UI. Set it to `Production` for anything beyond local evaluation — both
are deliberately Development-gated in `Program.cs`.

See the [root README](../README.md) for the full setup guide and demo credentials.

### Locally

Postgres must be reachable at the host and port in `ConnectionStrings__Default`:

```bash
docker compose up -d postgres
dotnet run --project src/Api
```

Swagger UI is served at `/swagger` in the Development environment.

Migrations and seed data are applied automatically at start-up.

`GET /health` is anonymous and checks the database connection, so it answers `Healthy`
only when the API can actually serve requests. docker-compose uses it as the `api`
service health check; it also works as a smoke test after a manual start:

```bash
curl http://localhost:${API_PORT:-5000}/health
```

### Seeded accounts

Development seed data only.

| Role    | Email               | Password        |
| ------- | ------------------- | --------------- |
| Admin   | `admin@lms.test`    | `Admin@12345`   |
| Teacher | `teacher@lms.test`  | `Teacher@12345` |
| Student | `student@lms.test`  | `Student@12345` |

## API conventions

### Response envelope

Every response uses `{ success, data, error, meta }`. List endpoints put their page totals
in `meta`: `{ total, page, pageSize, totalPages }`.

### Pagination and filtering

`GET /users`, `GET /assignments`, `GET /assignments/{id}/submissions` and
`GET /submissions/mine` accept `?page=` and `?pageSize=` (default 20, maximum 100).
Out-of-range values are clamped rather than rejected, so a stray `?pageSize=100000`
cannot turn into an unbounded query.

| Endpoint | Filters |
| --- | --- |
| `GET /users` | `role`, `search` (name or email, case-insensitive) |
| `GET /assignments` | `status`, `subjectId`, `classId`, `search` (title) |
| `GET /assignments/{id}/submissions` | `status` |
| `GET /submissions/mine` | `status` |

Filters narrow what the caller's role already allows — they never widen it. A student
passing `?status=Draft` gets an empty page, because the role-scoped query runs first
(business rule §7.3).

### Rate limiting

`POST /auth/login` is throttled per client IP: 10 requests per 60 seconds by default,
configurable via `RateLimiting__Login__PermitLimit` and `RateLimiting__Login__WindowSeconds`.
Exceeding the budget returns `429` with the standard error envelope and a `Retry-After`
header. No other endpoint is throttled.

### Registration

`POST /auth/register` from the plan's API surface is **deliberately not implemented**.
Accounts are created by an Admin through `POST /users`, which keeps role assignment an
administrative decision instead of something a caller can choose for themselves. Login
(`POST /auth/login`) and `GET /auth/me` are the only endpoints under `/auth`.

## Tests

```bash
dotnet test
```

Integration tests require Postgres to be running (`docker compose up -d postgres`).
They read configuration from the environment and fall back to the docker-compose
defaults, so the suite runs on a clean checkout without a `.env`.

### Coverage

```bash
./scripts/coverage.sh
```

Collects line coverage from both test projects and prints a per-assembly summary.
Latest run — 114 tests, all passing:

| Assembly | Coverage |
| --- | --- |
| Application (services, validators, business rules) | 98.0% |
| Domain | 96.5% |
| Api (controllers, middleware, host wiring) | 87.6% |
| Infrastructure (EF Core repositories, security) | 78.0% |
| **Total** | **90.1%** |

Unit and integration runs emit separate Cobertura reports; the script merges them by
taking the better figure per class, so the total is a floor on real coverage rather than
an inflated number.

## Build conventions

`Directory.Build.props` centralises `TargetFramework`, `Nullable`, `ImplicitUsings` and
`TreatWarningsAsErrors` — individual `.csproj` files declare only their own references.
Analyzer exceptions are documented inline in `.editorconfig`.
