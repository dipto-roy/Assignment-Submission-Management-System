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

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811130048_AddAttachmentsAndNotifications') THEN
    CREATE TABLE "Attachments" (
        "Id" uuid NOT NULL,
        "FileName" character varying(255) NOT NULL,
        "ContentType" character varying(150) NOT NULL,
        "SizeBytes" bigint NOT NULL,
        "StorageKey" character varying(500) NOT NULL,
        "StorageProvider" character varying(30) NOT NULL,
        "AssignmentId" uuid,
        "SubmissionId" uuid,
        "UploadedById" uuid NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Attachments" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Attachments_ExactlyOneOwner" CHECK (("AssignmentId" IS NOT NULL AND "SubmissionId" IS NULL)
                      OR ("AssignmentId" IS NULL AND "SubmissionId" IS NOT NULL)),
        CONSTRAINT "FK_Attachments_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId") REFERENCES "Assignments" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Attachments_Submissions_SubmissionId" FOREIGN KEY ("SubmissionId") REFERENCES "Submissions" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Attachments_Users_UploadedById" FOREIGN KEY ("UploadedById") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811130048_AddAttachmentsAndNotifications') THEN
    CREATE TABLE "Notifications" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Type" character varying(30) NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Message" character varying(1000) NOT NULL,
        "AssignmentId" uuid,
        "SubmissionId" uuid,
        "IsRead" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ReadAt" timestamp with time zone,
        CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Notifications_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId") REFERENCES "Assignments" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Notifications_Submissions_SubmissionId" FOREIGN KEY ("SubmissionId") REFERENCES "Submissions" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811130048_AddAttachmentsAndNotifications') THEN
    CREATE INDEX "IX_Attachments_AssignmentId" ON "Attachments" ("AssignmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811130048_AddAttachmentsAndNotifications') THEN
    CREATE INDEX "IX_Attachments_SubmissionId" ON "Attachments" ("SubmissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811130048_AddAttachmentsAndNotifications') THEN
    CREATE INDEX "IX_Attachments_UploadedById" ON "Attachments" ("UploadedById");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811130048_AddAttachmentsAndNotifications') THEN
    CREATE INDEX "IX_Notifications_AssignmentId" ON "Notifications" ("AssignmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811130048_AddAttachmentsAndNotifications') THEN
    CREATE UNIQUE INDEX "IX_Notifications_DeadlineReminder_Once" ON "Notifications" ("UserId", "AssignmentId") WHERE "Type" = 'DeadlineApproaching';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811130048_AddAttachmentsAndNotifications') THEN
    CREATE INDEX "IX_Notifications_SubmissionId" ON "Notifications" ("SubmissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811130048_AddAttachmentsAndNotifications') THEN
    CREATE INDEX "IX_Notifications_UserId_IsRead_CreatedAt" ON "Notifications" ("UserId", "IsRead", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811130048_AddAttachmentsAndNotifications') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260811130048_AddAttachmentsAndNotifications', '8.0.11');
    END IF;
END $EF$;
COMMIT;

