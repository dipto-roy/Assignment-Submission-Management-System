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

## 10. Open Assumptions (to confirm/document in README)

- Submission = text content (not file upload) unless file upload explicitly wanted — keep scope lean, note as assumption; file upload listed as stretch goal.
- One student belongs to exactly one class; one subject belongs to exactly one class; one teacher can teach multiple subjects.
- "Update submission before deadline" — after deadline, submission locked (status auto → Late if submitted late, per teacher-configurable rule or fixed).
- Assignment delete blocked if submissions already exist (data integrity over destructive cascade).
