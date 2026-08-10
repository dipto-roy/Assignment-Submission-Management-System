# Assignment & Submission Management System

A role-based assignment and submission platform for a school or college: teachers publish
assignments for the classes they teach, students submit answers before the deadline, and
teachers return marks and feedback.

Built as a Next.js frontend against an ASP.NET Core Web API backed by PostgreSQL, with
JWT authentication and role-based authorization enforced **server-side** on every endpoint.

| | |
| --- | --- |
| Frontend | Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS |
| Backend | ASP.NET Core 8 Web API (C#), Clean Architecture layering |
| Database | PostgreSQL 16, EF Core code-first migrations |
| Auth | JWT bearer tokens, role claims (Admin / Teacher / Student) |
| API docs | Swagger / OpenAPI at `/swagger` |
| Tests | xUnit + FluentAssertions + Moq (backend), Vitest + React Testing Library (frontend) |

---

## Contents

- [Demo credentials](#demo-credentials)
- [Quick start (Docker)](#quick-start-docker)
- [Manual setup](#manual-setup)
- [Database setup](#database-setup)
- [Running the tests](#running-the-tests)
- [Features](#features)
- [API surface](#api-surface)
- [Project structure](#project-structure)
- [Design decisions](#design-decisions)
- [Assumptions](#assumptions)
- [Known limitations](#known-limitations)

---

## Demo credentials

Seeded automatically on start-up **in the Development environment only** (see
[Design decisions](#design-decisions)).

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@lms.test` | `Admin@12345` |
| Teacher | `teacher@lms.test` | `Teacher@12345` |
| Student | `student@lms.test` | `Student@12345` |

The seed also creates *Class 10-A*, a *Mathematics* subject taught by the demo teacher, one
published assignment, and enrolls the demo student — so every screen has data on first login.

---

## Quick start (Docker)

Brings up PostgreSQL, the API and the frontend together.

```bash
cd backend
cp .env.example .env          # then set Jwt__Key (see below)
docker compose up --build
```

Generate a signing key of at least 32 characters and put it in `Jwt__Key`:

```bash
openssl rand -base64 48
```

| Service | URL |
| --- | --- |
| Frontend | <http://localhost:3000> |
| API | <http://localhost:5000/api/v1> |
| Swagger UI | <http://localhost:5000/swagger> |
| Health check | <http://localhost:5000/health> |
| PostgreSQL | `localhost:5434` |

Ports are configurable via `FRONTEND_PORT`, `API_PORT` and `POSTGRES_PORT` in `.env`.

Migrations and demo data are applied on API start-up, so there is nothing to run by hand.
The frontend waits for the API's health check, which in turn waits for PostgreSQL.

> `NEXT_PUBLIC_API_URL` is compiled into the browser bundle at image build time. If you
> change `API_PORT`, rebuild the frontend image (`docker compose up --build frontend`).

---

## Manual setup

Prerequisites: **.NET SDK 8**, **Node.js 20+**, and a reachable PostgreSQL 16.

### 1. Database

```bash
cd backend
docker compose up -d postgres      # or point ConnectionStrings__Default at your own instance
```

### 2. Backend

```bash
cd backend
cp .env.example .env               # set Jwt__Key and ConnectionStrings__Default
dotnet run --project src/Api
```

API on <http://localhost:5000>, Swagger UI on <http://localhost:5000/swagger>.
`.env` is read by `dotnet run` as well as by docker compose; real environment variables
take precedence over it.

### 3. Frontend

```bash
cd frontend
npm install
cp .env.example .env.local         # NEXT_PUBLIC_API_URL, default http://localhost:5000/api/v1
npm run dev
```

App on <http://localhost:3000>. The API's CORS policy allows `http://localhost:3000` and
`http://127.0.0.1:3000` by default; override with `FRONTEND_ORIGIN` / `FRONTEND_ORIGIN_ALT`.

---

## Database setup

**Nothing needs to be created by hand.** `DbSeeder.MigrateAndSeedAsync` runs EF Core
migrations on every start-up, and demo data is seeded in Development. Both steps are held
behind a PostgreSQL advisory lock, so concurrent start-ups (several API replicas, or the
integration suite booting more than one test host) cannot race on a fresh database.

| Artifact | Path | Purpose |
| --- | --- | --- |
| Migrations | `backend/src/Infrastructure/Persistence/Migrations/` | Source of truth for the schema |
| Seed data | `backend/src/Infrastructure/Persistence/DbSeeder.cs` | Demo users, class, subject, assignment |
| SQL script | `backend/scripts/schema.sql` | Idempotent schema script, for setting up the database without the .NET SDK |

`schema.sql` is generated with `dotnet ef migrations script --idempotent`; it creates the
schema only and does **not** insert demo data, because the seeded passwords are BCrypt
hashes computed at runtime. To use it:

```bash
psql -h localhost -p 5434 -U postgres -d assignment_submission_dev -f backend/scripts/schema.sql
```

Regenerate it after adding a migration:

```bash
cd backend
dotnet ef migrations script --idempotent \
  --project src/Infrastructure --startup-project src/Api \
  --output scripts/schema.sql
```

### Data model

```
User (id, name, email, passwordHash, role[Admin|Teacher|Student], createdAt)
Class (id, name, section)
Subject (id, name, code, classId → Class)
TeacherSubject (teacherId → User, subjectId → Subject)     -- who teaches what
StudentClass (studentId → User, classId → Class)           -- who is enrolled where
Assignment (id, title, description, deadline, maxMarks,
            status[Draft|Published], subjectId, teacherId, createdAt, updatedAt)
Submission (id, assignmentId, studentId, content, submittedAt, updatedAt,
            status[Submitted|Late|Graded|Returned], marks, feedback, gradedAt)
```

Key constraints: one submission per (assignment, student); assignment deletion is blocked
while submissions exist; a student only ever sees Published assignments for their own class.

---

## Running the tests

### Backend

```bash
cd backend
docker compose up -d postgres      # integration tests use a real database
dotnet test
```

114 tests — unit tests over services, validators and security helpers, plus integration
tests that drive the real HTTP pipeline through `WebApplicationFactory`.

Coverage:

```bash
cd backend
./scripts/coverage.sh
```

| Assembly | Coverage |
| --- | --- |
| Application (services, validators, business rules) | 98.0% |
| Domain | 96.5% |
| Api (controllers, middleware, host wiring) | 87.6% |
| Infrastructure (EF Core repositories, security) | 78.0% |
| **Total** | **90.1%** |

### Frontend

```bash
cd frontend
npm test              # 67 tests
npm run test:coverage
```

Frontend coverage concentrates on logic rather than markup: `client.ts` 96%,
`AuthContext` 97%, `useRequireRole` 100%, `datetime.ts` 97%, and the deadline-lock and
`marks <= maxMarks` rules in the submission and grading forms.

Both suites run on every push via GitHub Actions (`.github/workflows/ci.yml`).

---

## Features

### Admin

- Create, update and delete users, with a role and (for students) an initial class.
- Manage classes and subjects; assign teachers to subjects.
- Manage class rosters — enroll, move and unenroll students.
- Oversight of every assignment across all classes — including teachers' drafts — filtered
  by class, status and title, with each assignment's submissions, marks and feedback. Read-only:
  editing from here would bypass the teacher-ownership rules the API enforces on writes.

### Teacher

- Create, edit and delete assignments for the subjects they are assigned to.
- Set title, description, deadline and maximum marks; keep as Draft or Publish.
- Review submissions per assignment with student, timestamp and status.
- Award marks (validated against the assignment's maximum) and write feedback.
- Move a submission between Submitted / Late / Graded / Returned.

### Student

- See Published assignments for their own class, with deadline and remaining time.
- Submit an answer, and update it until the deadline passes.
- Track submission status, marks and teacher feedback.

### Cross-cutting

- JWT login with BCrypt password hashing; role claims drive both routing and API authorization.
- Consistent `{ success, data, error, meta }` response envelope, including for errors.
- FluentValidation on every write endpoint, mirrored by client-side validation in the UI.
- Serilog request logging and centralised exception-to-status-code mapping.
- Pagination and filtering on all list endpoints; per-IP rate limiting on login.
- Responsive layouts, loading and error states, error boundary and 404 routes.

---

## API surface

Base path `/api/v1`. Full, interactive documentation at `/swagger`.

```
POST   /auth/login                        -> JWT                      [anonymous, rate-limited]
GET    /auth/me                                                       [any signed-in role]

GET    /users                             ?role= &search= &page= &pageSize=   [Admin]
POST   /users            PUT /users/{id}            DELETE /users/{id}        [Admin]

GET    /classes          POST /classes    PUT /classes/{id}   DELETE /classes/{id}
GET    /classes/{id}/students   POST /classes/{id}/students
DELETE /classes/{id}/students/{studentId}                                     [Admin]

GET    /subjects         POST /subjects   PUT /subjects/{id}  DELETE /subjects/{id}
GET    /subjects/{id}    POST /subjects/{id}/assign-teacher                   [Admin]

GET    /assignments      ?status= &subjectId= &classId= &search= &page= &pageSize=
GET    /assignments/{id}                                              [role-filtered]
POST   /assignments      PUT /assignments/{id}      DELETE /assignments/{id}  [Teacher-owner]
PATCH  /assignments/{id}/publish                                              [Teacher-owner]

GET    /assignments/{id}/submissions      ?status= &page= &pageSize= [Teacher-owner, Admin]
POST   /assignments/{id}/submissions                                          [Student]
PUT    /submissions/{id}                              [Student-owner, before deadline]
GET    /submissions/mine                  ?status= &page= &pageSize=          [Student]
PATCH  /submissions/{id}/grade            PATCH /submissions/{id}/status      [Teacher-owner]

GET    /health                                                        [anonymous]
```

List endpoints default to 20 items per page, maximum 100; out-of-range values are clamped.
Page totals are returned in `meta` as `{ total, page, pageSize, totalPages }`.

Role filtering happens in the query, not the UI: filters can only narrow what the caller's
role already permits. A student passing `?status=Draft` receives an empty page rather than
another user's drafts.

---

## Project structure

```
.
├── backend/
│   ├── src/
│   │   ├── Domain/          Entities and enums. No outward dependencies.
│   │   ├── Application/     Services, DTOs, validators, abstractions, business rules
│   │   ├── Infrastructure/  EF Core DbContext, migrations, repositories, BCrypt, JWT
│   │   └── Api/             Controllers, middleware, Program.cs, Swagger
│   ├── tests/
│   │   ├── UnitTests/       Services, validators, security helpers
│   │   └── IntegrationTests/ Full HTTP pipeline via WebApplicationFactory
│   ├── scripts/             coverage.sh, schema.sql
│   ├── docker-compose.yml   postgres + api + frontend
│   └── Dockerfile           Multi-stage build, non-root runtime
├── frontend/
│   ├── src/
│   │   ├── app/             App Router pages: login, admin, teacher, student
│   │   ├── components/      Per-role panels and shared UI
│   │   ├── lib/api/         Typed fetch client, JWT storage, per-resource modules
│   │   ├── lib/auth/        Auth context and role guards
│   │   └── types/           DTOs mirroring the backend contracts
│   └── Dockerfile           Standalone Next.js output, non-root runtime
├── .github/workflows/ci.yml
├── plan.md                  Implementation plan and progress log
└── roadmap.md               Original requirements
```

Dependencies point inward: `Api → Application → Domain`, with `Infrastructure` implementing
the abstractions declared in `Application`.

---

## Design decisions

**PostgreSQL over MongoDB.** The data is highly relational — users, classes, subjects,
assignments and submissions are joined constantly and depend on foreign keys, cascade rules
and uniqueness constraints (one submission per student per assignment). Referential
integrity matters more here than schema flexibility.

**Authorization is enforced in the query, not the controller.** Each role gets its own
repository query (`FindAllAsync` / `FindByTeacherAsync` / `FindPublishedForStudentAsync`),
so unauthorized rows are never loaded in the first place. Ownership checks additionally
guard every write.

**No `POST /auth/register`.** Accounts are created by an Admin through `POST /users`. Public
self-registration would let a caller choose their own role, which contradicts the role model.

**Seeding and Swagger are Development-only.** The demo passwords are published in this
README, so seeding a production database with them would plant known admin credentials.
`docker-compose.yml` therefore defaults `ASPNETCORE_ENVIRONMENT` to `Development` — which is
what an evaluator wants — and anything beyond local use should set it to `Production`.

**Assignment deletion is blocked when submissions exist** (FK `Restrict` → HTTP 409) rather
than cascading. Losing graded student work to a mis-click is worse than an error message.

**Submissions are updated in place, not versioned.** One row per (assignment, student), with
`updatedAt` recording the last edit — matching "update your submission before the deadline"
rather than a revision history.

**JWT is stored in `localStorage`.** Adequate for this scope and it keeps the API stateless;
see [Known limitations](#known-limitations) for the trade-off.

---

## Assumptions

Documented per the brief's instruction to make and record reasonable assumptions:

1. **A submission is text content, not a file upload.** File storage would add infrastructure
   without exercising any additional business rule. The content field is free text.
2. **A student belongs to exactly one class**, and a subject belongs to exactly one class.
   Enrolling a student in a new class therefore *moves* them out of the previous one.
3. **A teacher can teach several subjects**, across different classes.
4. **After the deadline, a submission is locked.** Students cannot create or edit one; the
   UI disables the form and the API rejects the request independently.
5. **Marking late work is the teacher's decision** — the `Late` status is set by the teacher
   rather than being derived automatically.
6. **Marks are whole numbers** between 0 and the assignment's `maxMarks`.
7. **"Manage application-level settings"** from the brief is read as class/subject/enrollment
   management; there is no separate settings screen.
8. **Deadlines are stored and compared in UTC**, and rendered in the browser's local time zone.

---

## Known limitations

- **No notifications.** Listed as optional in the brief and left out to keep the scope tight.
- **No refresh tokens.** Access tokens last 60 minutes; when one expires the user signs in
  again. `localStorage` storage means a successful XSS could read the token — an HTTP-only
  cookie plus CSRF protection would be the production choice.
- **No file uploads** on submissions (see assumption 1).
- **List UIs request the maximum page size (100) rather than paging.** The API is paginated
  and filterable, but the dashboards render whole lists; a class larger than 100 students
  would need pager controls in the UI.
- **Rate limiting covers only `POST /auth/login`**, and its fixed window is per process — a
  multi-instance deployment would need a shared store.
- **No email delivery**, password reset, or account self-service.
- **`schema.sql` creates the schema but seeds no data** (see [Database setup](#database-setup)).
- **Frontend coverage is 44% overall.** The business-rule modules are well above the 80%
  bar; the remainder is presentational panels covered indirectly.
