# Frontend — Assignment & Submission Management System

Next.js (App Router) + React + TypeScript client for the ASP.NET Core API in `../backend`.

## Prerequisites

- Node.js 20+
- A running API (see `../backend/README.md`, or `docker compose up` in `backend/`)

## Setup

```bash
npm install
cp .env.example .env.local   # point NEXT_PUBLIC_API_URL at your API
npm run dev                  # http://localhost:3000
```

`NEXT_PUBLIC_API_URL` must include the `/api/v1` prefix and match the API's port
(`API_PORT` in `backend/.env`, default `5000`). The API's CORS policy must allow
`http://localhost:3000` — it does by default.

## Scripts

| Command | Purpose |
|---|---|
| `npm run dev` | Development server |
| `npm run build` | Production build |
| `npm start` | Serve the production build |
| `npm run lint` | ESLint |
| `npm test` | Vitest (single run) |
| `npm run test:watch` | Vitest in watch mode |
| `npm run test:coverage` | Vitest with a V8 coverage report |

## Structure

```
src/
  app/
    (auth)/login/      Sign-in form
    admin/             Users, classes, subjects, teacher assignment, enrollment
    teacher/           Assignment CRUD, publish/draft, submissions review, grading
    student/           Published assignments, submit/update, marks + feedback
    layout.tsx         AuthProvider + shared nav
    error.tsx          Route-level error boundary
    not-found.tsx      404
  components/
    admin/ teacher/ student/   Feature panels
    layout/AppNav.tsx          Role-aware nav + sign-out
    ui/styles.ts               Shared control class strings
  lib/
    api/               Typed fetch client (client.ts) + per-resource modules
    auth/              AuthContext, useRequireRole guard
    hooks/             Shared data hooks
    datetime.ts        Deadline formatting and remaining-time helpers
  types/               DTOs mirroring the backend
```

## Auth model

`lib/api/client.ts` stores the JWT in `localStorage` and attaches it as a bearer
token on every request. `AuthContext` hydrates the current user from that token on
first mount, and `useRequireRole(role)` redirects visitors who are unauthenticated
(to `/login`) or signed in with a different role (to their own dashboard).

Route guards and hidden UI are **convenience only**. Every rule — role checks,
ownership checks, deadline enforcement, mark ceilings — is enforced by the API, and
the UI simply surfaces the error the server returns.

## Testing

Vitest + React Testing Library, jsdom environment. Tests live next to the code they
cover as `*.test.ts(x)`.

```bash
npm test
npm run test:coverage
```

Covered today: the `apiFetch` envelope/error handling, token storage, `AuthContext`
hydration and login/logout, `useRequireRole` redirects, the deadline-lock logic in
the student submission form, and the teacher grading form's `marks <= maxMarks` rule.

`vitest.setup.ts` installs a small in-memory `localStorage`, because jsdom 30 no
longer ships a `Storage` implementation.

## Known limitations

- Submissions are plain text; file upload is out of scope (see plan §11).
- No pagination or filtering yet — the API returns full collections.
