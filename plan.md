# Project Plan — Assignment & Submission Management System

> Derived from `roadmap.md`. Deadline: **14 August, 2026**.

---

## 1. Tech Stack (per roadmap §3)

| Layer | Choice |
|-------|--------|
| Frontend | Next.js (App Router) + React + TypeScript |
| Backend | ASP.NET Core Web API (C#, .NET 8) |
| Database | PostgreSQL (relational — fits role/class/subject/assignment relationships cleanly) |
| Auth | JWT-based, role-based authorization (Admin / Teacher / Student) |
| API Docs | Swagger/OpenAPI |
| Testing | xUnit (backend), Jest/RTL (frontend) |
| Optional | Docker Compose, pagination, filtering, notifications |

**DB choice justification** (for README): PostgreSQL chosen over MongoDB — data is highly relational (Users↔Classes↔Subjects↔Assignments↔Submissions with FKs, cascades, uniqueness constraints). Relational integrity matters more than schema flexibility here.

---

## 2. Data Model

```
User (id, name, email, passwordHash, role[Admin|Teacher|Student], createdAt)
Class (id, name, section)                          -- e.g. "Class 10-A"
Subject (id, name, code, classId FK)                -- subject belongs to a class
TeacherSubject (id, teacherId FK->User, subjectId FK)   -- teacher assigned to subject/class
StudentClass (id, studentId FK->User, classId FK)       -- student enrolled in class
Assignment (id, title, description, deadline, maxMarks, status[Draft|Published],
            subjectId FK, teacherId FK->User, createdAt, updatedAt)
Submission (id, assignmentId FK, studentId FK->User, content/fileUrl, submittedAt,
            updatedAt, status[Submitted|Late|Graded|Returned], marks, feedback, gradedAt)
```

Constraints:
- Unique (assignmentId, studentId) on Submission — one submission per student per assignment (versioned via updatedAt, not multiple rows).
- Submission update allowed only before deadline (business rule) — enforce server-side.
- Assignment visible to students only when status = Published and student's class matches subject's class.

---

## 3. Role → Permission Matrix

| Action | Admin | Teacher | Student |
|---|---|---|---|
| Manage users | ✅ | ❌ | ❌ |
| Manage classes/subjects | ✅ | ❌ | ❌ |
| Assign teacher→subject/class | ✅ | ❌ | ❌ |
| Create/update/delete assignment | ❌ | ✅ (own subjects only) | ❌ |
| Publish/draft assignment | ❌ | ✅ | ❌ |
| View all assignments/submissions | ✅ | ✅ (own subjects only) | ✅ (own class only) |
| Submit / update submission | ❌ | ❌ | ✅ (before deadline) |
| Grade + feedback | ❌ | ✅ (own subjects only) | ❌ |
| Change submission status | ❌ | ✅ | ❌ |

All enforced server-side (JWT claims → role + user id → ownership checks), not just UI-hidden.

---

## 4. Backend API Surface (RESTful, versioned `/api/v1`)

```
POST   /auth/login                          -> JWT
POST   /auth/register (admin-only, or seed) 

GET    /users                    [Admin]
POST   /users                    [Admin]
PUT    /users/{id}               [Admin]
DELETE /users/{id}                [Admin]

GET    /classes                  [Admin, Teacher, Student(own)]
POST   /classes                  [Admin]
PUT    /classes/{id}             [Admin]
DELETE /classes/{id}             [Admin]

GET    /subjects                 [Admin, Teacher, Student]
POST   /subjects                 [Admin]
PUT    /subjects/{id}            [Admin]
POST   /subjects/{id}/assign-teacher   [Admin]

GET    /assignments              [role-filtered]
POST   /assignments              [Teacher]
GET    /assignments/{id}         [role-filtered]
PUT    /assignments/{id}         [Teacher-owner]
DELETE /assignments/{id}         [Teacher-owner]
PATCH  /assignments/{id}/publish [Teacher-owner]

GET    /assignments/{id}/submissions      [Teacher-owner, Admin]
POST   /assignments/{id}/submissions      [Student]  -- submit
PUT    /submissions/{id}                  [Student-owner, before deadline]
GET    /submissions/mine                  [Student]
PATCH  /submissions/{id}/grade            [Teacher-owner]  -- marks + feedback
PATCH  /submissions/{id}/status           [Teacher-owner]
```

Cross-cutting: global error-handling middleware → consistent error envelope, request validation (FluentValidation or DataAnnotations), Serilog logging, Swagger annotations.

---

## 5. Frontend Structure (Next.js App Router)

```
frontend/
  app/
    (auth)/login/
    admin/          -- users, classes, subjects, teacher-assignment mgmt
    teacher/         -- assignments CRUD, submissions review, grading
    student/         -- assignment list, submit/update, view marks+feedback
    layout.tsx        -- role-based nav guard
  lib/api/           -- typed fetch client, JWT storage/refresh
  lib/auth/          -- auth context, route guards
  components/
  types/             -- shared DTOs mirrored from backend
```

Requirements to hit: responsive UI, client+server form validation, loading/error states, role-based route protection (redirect if wrong role).

---

## 6. Backend Structure (ASP.NET Core, layered)

```
backend/
  src/
    Api/                -- Controllers, Program.cs, middleware, Swagger
    Application/         -- Services, DTOs, validators, business rules
    Domain/               -- Entities, enums
    Infrastructure/        -- EF Core DbContext, Migrations, Repositories
  tests/
    UnitTests/            -- business rules, auth, submission workflow
    IntegrationTests/     -- API endpoint tests (WebApplicationFactory)
```

EF Core Code-First → Migrations folder committed. `dotnet ef database update` sets up schema; seed data via `DbSeeder` run on startup (or a seed script) so evaluator needs zero manual table creation.

---

## 7. Business Rules to Test (unit + integration)

1. Student cannot submit after deadline.
2. Student cannot update submission after deadline (if update-window closed).
3. Student cannot view Draft assignments.
4. Student cannot see other students' submissions.
5. Teacher cannot grade/edit assignments outside their assigned subjects.
6. Teacher cannot exceed `maxMarks` when grading.
7. Only Admin can manage users/classes/subjects/teacher-assignment.
8. JWT missing/expired/wrong-role → 401/403.
9. Duplicate submission by same student on same assignment → reject or overwrite per defined rule (decide: overwrite, versioned by updatedAt).
10. Assignment delete cascades/blocks correctly (e.g. block delete if submissions exist, or cascade — document choice).

---

## 8. Phased Build Plan

| Phase | Scope | Output |
|---|---|---|
| 1. Setup | Repo scaffolding, .NET Web API project, Next.js project, Postgres via Docker Compose, EF Core + initial migration | Buildable skeleton, Swagger up |
| 2. Auth | User entity, JWT login, role middleware/policies, password hashing (BCrypt) | Working login for 3 roles |
| 3. Admin domain | Users/Classes/Subjects CRUD + teacher assignment endpoints + admin UI | Admin can manage data |
| 4. Teacher domain | Assignment CRUD, publish/draft, class/subject scoping | Teacher can create/manage assignments |
| 5. Student domain | View published assignments, submit, update before deadline | Student submission flow works end-to-end |
| 6. Grading | Teacher grades + feedback, status change; student views marks/feedback | Full loop closed |
| 7. Testing | xUnit unit + integration tests for rules in §7; Jest tests for key frontend logic | 80%+ coverage on business-rule code |
| 8. Polish | Pagination, filtering, error boundaries, responsive pass, seed data, Swagger polish | Demo-ready |
| 9. Packaging | README, .env.example, migration+seed scripts, demo credentials, Docker Compose (optional), final checklist pass | Submission-ready |

---

## 9. Deliverables Checklist (mirrors roadmap §4–§5)

- [ ] Frontend (Next.js/TS) complete
- [ ] Backend (ASP.NET Core Web API) complete
- [ ] PostgreSQL migrations + seed data + setup script
- [ ] Unit tests: business rules, authZ, submission workflow
- [ ] README: overview, features, stack, structure, setup (DB/frontend/backend), test run instructions, assumptions, known limitations
- [ ] Demo credentials for Admin/Teacher/Student (seeded, documented)
- [ ] `.env.example` (no real secrets)
- [ ] Role-based access enforced server-side, not just UI
- [ ] Optional: Docker Compose, pagination, filtering, notifications, Swagger URL

---

## 10. Missing Work Checklist (codebase audit — 10 Aug 2026)

Audit of `backend/` and `frontend/` against roadmap §3–§5 and the phases in §8.
Phases 1–6 are done on the **backend** and, as of 10 Aug 2026, on the **frontend** as well (admin + teacher + student). Remaining work is Phases 7–9: tests, polish, packaging.

### 10.1 Frontend — Teacher domain (Phase 4/6) — **DONE** (10 Aug 2026)

- [x] `lib/api/assignments.ts` — typed client for `GET/POST/PUT/DELETE /assignments`, `PATCH /assignments/{id}/publish`
- [x] `lib/api/submissions.ts` — `GET /assignments/{id}/submissions`, `PATCH /submissions/{id}/grade`, `PATCH /submissions/{id}/status` (plus the student-side calls for §10.2)
- [x] Teacher route guard (`useRequireRole("Teacher")`) — `app/teacher/page.tsx`
- [x] Assignment list scoped to teacher's subjects, with Draft/Published badge — `components/teacher/AssignmentsPanel.tsx`
- [x] Create/edit assignment form: title, description, deadline, maxMarks, subject picker + client-side validation mirroring `AssignmentValidators` — `components/teacher/AssignmentForm.tsx`
- [x] Delete assignment (surface the "blocked when submissions exist" error from the API)
- [x] Publish / unpublish toggle
- [x] Submissions review table per assignment (student, submittedAt, status) — `components/teacher/SubmissionsReview.tsx`
- [x] Grade + feedback form with `marks <= maxMarks` client validation mirroring `SubmissionValidators`
- [x] Submission status change control (Submitted / Late / Graded / Returned)

Supporting work landed with this slice:
- `lib/datetime.ts` — deadline formatting, `datetime-local` ↔ ISO conversion, remaining-time helper (reused by §10.2)
- `lib/hooks/useTeacherSubjects.ts` — subject picker scoped to the signed-in teacher
- `components/ui/styles.ts` — shared control classes extracted from the admin panels
- **Backend fix:** registered `JsonStringEnumConverter` in `Program.cs`. Enums were serialized out as names but only accepted as numbers, so any client sending `"Teacher"` / `"Returned"` got a 400 — this affected `POST /users` from the existing admin UI as well as the new status endpoint.

### 10.2 Frontend — Student domain (Phase 5/6) — **DONE** (10 Aug 2026)

- [x] Student route guard (`useRequireRole("Student")`) — `app/student/page.tsx`
- [x] Published-assignment list for the student's class — `components/student/StudentDashboard.tsx`; Draft never appears because `GET /assignments` is filtered server-side (`FindPublishedForStudentAsync`, business rule §7.3)
- [x] Assignment detail view: description, deadline, maxMarks, remaining-time indicator — `components/student/AssignmentCard.tsx`
- [x] Submit answer form (text content per §11 assumption) + validation — `components/student/SubmissionForm.tsx`
- [x] Update submission before deadline; UI locked after deadline with a clear reason (mirrors §7.1/§7.2, still enforced server-side)
- [x] `GET /submissions/mine` view — status, marks, teacher feedback — `components/student/MySubmissionsPanel.tsx`

The dashboard owns both lists and joins them by `assignmentId`, so a save updates
the assignment card and the marks/feedback table together.

### 10.3 Frontend — cross-cutting — **DONE** (10 Aug 2026)

- [x] **Frontend test runner + tests.** Vitest + React Testing Library (jsdom), `npm test` / `npm run test:coverage`. 57 tests across `apiFetch` envelope + error handling + token storage, `AuthContext` hydration/login/logout, `useRequireRole` redirects, `datetime` deadline helpers, the student deadline-lock, the teacher grading form's `marks <= maxMarks` rule, and `AssignmentForm` validation.
  `vitest.setup.ts` installs an in-memory `localStorage` because jsdom 30 no longer ships a `Storage` implementation.
- [x] `frontend/README.md` rewritten — setup, scripts, structure, auth model, testing, known limitations
- [x] `frontend/.env.example` added (`NEXT_PUBLIC_API_URL`)
- [x] Shared nav + role-based nav guard — `components/layout/AppNav.tsx`, rendered from `app/layout.tsx`; per-page sign-out buttons removed in favour of it
- [x] Error boundary / 404 / global loading — `app/error.tsx`, `app/not-found.tsx`, `app/loading.tsx`
- [x] Responsive pass — dashboards use `p-4 sm:p-8`, admin rows wrap, wide tables scroll inside `overflow-x-auto`, forms use `flex-wrap` / `sm:grid-cols-2`
- [x] Admin student-enrollment management — `components/admin/EnrollmentPanel.tsx` (roster per class, enroll/move, unenroll), backed by the new endpoints in §10.4
- [x] Frontend `types/index.ts` `Assignment` / `Submission` types — realigned with the backend DTOs and consumed by the teacher dashboard (§10.1)

Coverage today is 39% overall, but that number is dominated by presentational
panels. The business-rule modules the plan cares about sit well above the §8 Phase 7
bar: `client.ts` 96%, `AuthContext.tsx` 97%, `useRequireRole` 100%, `datetime.ts` 97%,
`SubmissionForm` 97%, `SubmissionsReview` 92%, `AssignmentForm` 92%. The thin
per-resource API modules (`admin.ts`, `assignments.ts`, `submissions.ts`) are
one-line `apiFetch` wrappers with no logic and are exercised through the components.

### 10.4 Backend gaps

Backend API surface matches §4 and all 10 business rules in §7 have tests (82 `[Fact]`/`[Theory]` across unit + integration).

- [x] **Enrollment endpoints** — `GET /classes/{id}/students`, `POST /classes/{id}/students`, `DELETE /classes/{id}/students/{studentId}` (all Admin-only). Enrolling *moves* the student, since plan §11 assumes one class per student. Covered by 6 new unit tests.
- [ ] `POST /auth/register` from §4 is intentionally not implemented (admin-only creation via `POST /users`) — **document this in the README** rather than leaving §4 stale
- [ ] No pagination or filtering anywhere (`grep Skip(/Take(` → zero hits). Roadmap §4 "Optional Additions" + plan §8 Phase 8.
- [ ] No notifications (optional)
- [ ] No health-check endpoint for compose `depends_on` / evaluator smoke test
- [ ] No rate limiting on `/auth/login`
- [ ] Test coverage figure is unmeasured — no coverage run/report to back the "80%+" claim in §8 Phase 7

### 10.5 Packaging & submission blockers (roadmap §4–§5)

- [ ] **No root `README.md`** — the single most-weighted deliverable. Needs: overview, features, stack, structure, setup, DB setup, frontend run, backend run, test instructions, assumptions, known limitations.
- [ ] **Demo credentials not documented outside code.** They live only in the `DbSeeder` XML comment (`admin@lms.test` / `teacher@lms.test` / `student@lms.test`). Roadmap §4 wants a table.
- [ ] **Seeding + Swagger are Development-only** (`Program.cs` lines ~165, ~172) while `docker-compose.yml` defaults `ASPNETCORE_ENVIRONMENT` to `Production`. An evaluator who copies `.env.example` without setting it gets **no demo users and no Swagger UI**. Either default compose to Development or make the README explicit.
- [ ] No frontend `Dockerfile` / no `frontend` service in `docker-compose.yml` — compose only brings up `postgres` + `api`, so "one command to run the project" is not true yet
- [ ] No DB script/backup fallback for evaluators who cannot run `dotnet ef` (migrations exist and run on startup, so this is optional — decide and document)
- [ ] No CI workflow (`.github/` absent) — optional, but a green build badge is cheap credibility
- [ ] Final pass on roadmap §5 checklist: verify no secrets committed (`backend/.env` holds a real `Jwt__Key` and is gitignored — confirm it never entered history)

### 10.6 Suggested order (deadline 14 Aug 2026)

1. ~~Teacher UI (§10.1)~~ — done
2. ~~Student UI (§10.2)~~ — done
3. Root README + demo credentials + environment fix (§10.5) — **the remaining submission blockers**
4. ~~Frontend tests (§10.3)~~ — done; backend coverage report (§10.4) still open
5. Optional extras: pagination, filtering, frontend Docker service, CI

---

## 11. Open Assumptions (to confirm/document in README)

- Submission = text content (not file upload) unless file upload explicitly wanted — keep scope lean, note as assumption; file upload listed as stretch goal.
- One student belongs to exactly one class; one subject belongs to exactly one class; one teacher can teach multiple subjects.
- "Update submission before deadline" — after deadline, submission locked (status auto → Late if submitted late, per teacher-configurable rule or fixed).
- Assignment delete blocked if submissions already exist (data integrity over destructive cascade).
