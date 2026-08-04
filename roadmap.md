# © OnnoRokom Projukti Limited

# Assistant Software Engineer Recruitment Project

## Assignment & Submission Management System

A role-based school/college application for evaluating understanding of requirements, system design, API development, frontend implementation, and testing.

| Item | Details |
|------|---------|
| **Project Type** | Full-stack Web Application |
| **Submission Deadline** | **14 August, 2026** |

> **Please read the requirements carefully and make reasonable assumptions where the requirements are not explicitly defined. Document those assumptions in the README.**

---

# 1. Project Brief

Build a **role-based Assignment & Submission Management System** for a school or college.

The system should allow:

- Teachers to create assignments for specific classes or courses.
- Students to view and submit assignments.
- Teachers to review submissions and provide marks and feedback.

---

# 2. User Roles and Responsibilities

## Admin

- Manage users.
- Manage classes/courses and subjects.
- Assign teachers to subjects/classes.
- View all assignments and submissions.
- Manage application-level settings where necessary.

---

## Teacher

- Create, update, and delete assignments.
- Assign an assignment to a specific class/course and subject.
- Define:
  - Title
  - Description
  - Deadline
  - Maximum marks
- Publish an assignment or keep it as a draft.
- View student submissions.
- Assign marks and provide feedback.
- Change the submission status when necessary.

---

## Student

- View assignments assigned to their class/course.
- View assignment details and deadline.
- Submit an answer.
- Update a submission before the deadline, if allowed.
- View:
  - Submission status
  - Marks
  - Teacher feedback

> Applicants may use a different but suitable design. Any important design decisions should be explained in the **README**.

---

# 3. Technical Requirements

| Category | Requirements |
|----------|--------------|
| **Frontend** | Next.js, React, TypeScript, Responsive UI, Form Validation, API Integration |
| **Backend** | ASP.NET Core Web API, C#, RESTful API, Validation, Error Handling, Logging, Swagger/OpenAPI |
| **Database** | PostgreSQL or MongoDB. Implement the required relationships, or explain the chosen data model. |
| **Authentication** | Login, JWT-based Authentication, Role-based Authorization |
| **Testing** | Unit tests covering important business rules, authorization, and submission workflows |

---

# 4. Submission Guidelines

## Git Repository Link

Submit a GitHub or GitLab repository containing the complete source code.

---

## Complete Project Code

Include:

- Frontend
- Backend/API
- Database files
- Unit tests

---

## Database Files

Include:

- Migration files
- Seed/sample data
- Database script or backup file (if applicable)

The evaluator should be able to set up the database without manually creating tables or collections.

---

## README.md

Include:

- Project overview
- Main features
- Technology stack
- Project structure
- Setup instructions
- Database setup instructions
- Frontend run instructions
- Backend run instructions
- Test execution instructions
- Assumptions
- Known limitations

---

## Demo Credentials

Provide working login credentials for all three roles.

| Role | Email | Password |
|------|-------|----------|
| **Admin** | ____________________ | ____________________ |
| **Teacher** | ____________________ | ____________________ |
| **Student** | ____________________ | ____________________ |

---

## Environment Configuration

- Do **not** upload real passwords, API keys, or other sensitive information.
- Include an **`.env.example`** file showing the required environment variables.

---

## Easy Local Setup

Provide clear and complete setup instructions in the README so the project can be run locally.

---

## Optional Additions

These are optional but encouraged:

- Live project URL
- API / Swagger URL
- Docker configuration
- Pagination
- Advanced filtering
- Notifications
- Other additional features

---

# 5. Final Checklist

Before submitting, confirm the following:

- [ ] Repository link is accessible.
- [ ] Frontend and backend are both included.
- [ ] Database can be created using the provided files or instructions.
- [ ] Demo accounts for Admin, Teacher, and Student are available.
- [ ] README explains how to run the project and its tests.
- [ ] Role-based access is enforced by the backend API.
- [ ] Important business rules are implemented and tested.
- [ ] No real secrets or credentials are committed to the repository.

---



**Thank you, and best of luck!**