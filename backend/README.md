# Assignment & Submission Management System — Backend

ASP.NET Core 8 Web API organised as a Clean Architecture solution.

## Layout

```
backend/
├── AssignmentSubmissionSystem.sln
├── Directory.Build.props          # Shared build settings for every project
├── .editorconfig                  # Formatting + analyzer configuration
├── global.json                    # Pins the .NET SDK major version
├── docker-compose.yml             # Postgres + API
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

API on `http://localhost:${API_PORT:-5000}`, Postgres on `${POSTGRES_PORT:-5434}`.

### Locally

Postgres must be reachable at the host and port in `ConnectionStrings__Default`:

```bash
docker compose up -d postgres
dotnet run --project src/Api
```

Swagger UI is served at `/swagger` in the Development environment.

Migrations and seed data are applied automatically at start-up.

### Seeded accounts

Development seed data only.

| Role    | Email               | Password        |
| ------- | ------------------- | --------------- |
| Admin   | `admin@lms.test`    | `Admin@12345`   |
| Teacher | `teacher@lms.test`  | `Teacher@12345` |
| Student | `student@lms.test`  | `Student@12345` |

## Tests

```bash
dotnet test
```

Integration tests require Postgres to be running (`docker compose up -d postgres`).
They read configuration from the environment and fall back to the docker-compose
defaults, so the suite runs on a clean checkout without a `.env`.

## Build conventions

`Directory.Build.props` centralises `TargetFramework`, `Nullable`, `ImplicitUsings` and
`TreatWarningsAsErrors` — individual `.csproj` files declare only their own references.
Analyzer exceptions are documented inline in `.editorconfig`.
