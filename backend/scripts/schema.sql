CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE TABLE "Classes" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Section" text,
        CONSTRAINT "PK_Classes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Email" character varying(256) NOT NULL,
        "PasswordHash" text NOT NULL,
        "Role" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE TABLE "Subjects" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Code" text NOT NULL,
        "ClassId" uuid NOT NULL,
        CONSTRAINT "PK_Subjects" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Subjects_Classes_ClassId" FOREIGN KEY ("ClassId") REFERENCES "Classes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE TABLE "StudentClasses" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "ClassId" uuid NOT NULL,
        CONSTRAINT "PK_StudentClasses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StudentClasses_Classes_ClassId" FOREIGN KEY ("ClassId") REFERENCES "Classes" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_StudentClasses_Users_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE TABLE "Assignments" (
        "Id" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" text NOT NULL,
        "Deadline" timestamp with time zone NOT NULL,
        "MaxMarks" integer NOT NULL,
        "Status" character varying(20) NOT NULL,
        "SubjectId" uuid NOT NULL,
        "TeacherId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_Assignments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Assignments_Subjects_SubjectId" FOREIGN KEY ("SubjectId") REFERENCES "Subjects" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Assignments_Users_TeacherId" FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE TABLE "TeacherSubjects" (
        "Id" uuid NOT NULL,
        "TeacherId" uuid NOT NULL,
        "SubjectId" uuid NOT NULL,
        CONSTRAINT "PK_TeacherSubjects" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TeacherSubjects_Subjects_SubjectId" FOREIGN KEY ("SubjectId") REFERENCES "Subjects" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TeacherSubjects_Users_TeacherId" FOREIGN KEY ("TeacherId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE TABLE "Submissions" (
        "Id" uuid NOT NULL,
        "AssignmentId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Content" text NOT NULL,
        "Status" character varying(20) NOT NULL,
        "Marks" integer,
        "Feedback" text,
        "SubmittedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "GradedAt" timestamp with time zone,
        CONSTRAINT "PK_Submissions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Submissions_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId") REFERENCES "Assignments" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Submissions_Users_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE INDEX "IX_Assignments_SubjectId" ON "Assignments" ("SubjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE INDEX "IX_Assignments_TeacherId" ON "Assignments" ("TeacherId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE INDEX "IX_StudentClasses_ClassId" ON "StudentClasses" ("ClassId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_StudentClasses_StudentId_ClassId" ON "StudentClasses" ("StudentId", "ClassId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE INDEX "IX_Subjects_ClassId" ON "Subjects" ("ClassId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Submissions_AssignmentId_StudentId" ON "Submissions" ("AssignmentId", "StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE INDEX "IX_Submissions_StudentId" ON "Submissions" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE INDEX "IX_TeacherSubjects_SubjectId" ON "TeacherSubjects" ("SubjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_TeacherSubjects_TeacherId_SubjectId" ON "TeacherSubjects" ("TeacherId", "SubjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804174510_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260804174510_InitialCreate', '8.0.11');
    END IF;
END $EF$;
COMMIT;

