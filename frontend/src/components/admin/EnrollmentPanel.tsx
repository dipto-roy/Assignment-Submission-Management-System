"use client";

import { useEffect, useState } from "react";
import { enrollStudent, getClassStudents, getUsers, unenrollStudent } from "@/lib/api/admin";
import { useClasses } from "@/lib/hooks/useClasses";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import {
  Alert,
  Badge,
  EmptyState,
  LoadingLine,
  SectionHeading,
} from "@/components/ui/primitives";
import { compactInputClass, dividedListClass, subtleTextClass } from "@/components/ui/styles";
import type { EnrolledStudent, UserSummary } from "@/types";

/**
 * Move students between classes after creation. `POST /users` can only set an initial
 * class, so this is the re-enroll / move / unenroll surface (plan §10.3).
 */
export function EnrollmentPanel() {
  const { classes, isLoading: isLoadingClasses } = useClasses();
  const [classId, setClassId] = useState("");
  // Keyed by class so a stale roster never renders under a newly picked class.
  const [roster, setRoster] = useState<{ classId: string; students: EnrolledStudent[] } | null>(null);
  const [allStudents, setAllStudents] = useState<UserSummary[]>([]);
  const [selectedStudentId, setSelectedStudentId] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getUsers()
      .then((users) => setAllStudents(users.filter((u) => u.role === "Student")))
      .catch((e: unknown) =>
        setError(e instanceof Error ? e.message : "Failed to load students."),
      );
  }, []);

  useEffect(() => {
    if (!classId) return;

    let isActive = true;

    getClassStudents(classId)
      .then((students) => {
        if (isActive) setRoster({ classId, students });
      })
      .catch((e: unknown) => {
        if (isActive) setError(e instanceof Error ? e.message : "Failed to load the roster.");
      });

    return () => {
      isActive = false;
    };
  }, [classId]);

  const reloadRoster = async () => {
    setRoster({ classId, students: await getClassStudents(classId) });
  };

  const students = roster?.classId === classId ? roster.students : null;

  const handleEnroll = async () => {
    if (!classId || !selectedStudentId) return;

    setError(null);
    try {
      await enrollStudent(classId, selectedStudentId);
      setSelectedStudentId("");
      await reloadRoster();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to enroll the student.");
    }
  };

  const handleUnenroll = async (studentId: string) => {
    setError(null);
    try {
      await unenrollStudent(classId, studentId);
      await reloadRoster();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to unenroll the student.");
    }
  };

  const enrolledIds = new Set(students?.map((s) => s.id) ?? []);
  const enrollable = allStudents.filter((s) => !enrolledIds.has(s.id));

  return (
    <section>
      <SectionHeading
        icon="user-plus"
        title="Enrollment"
        description="A student belongs to one class at a time — enrolling them here moves them out of their previous class."
        meta={students ? <Badge tone="primary">{students.length} enrolled</Badge> : undefined}
      />

      <div className="mb-5 flex flex-wrap items-end gap-2">
        <select
          value={classId}
          onChange={(e) => setClassId(e.target.value)}
          aria-label="Class"
          className={compactInputClass}
        >
          <option value="">Select class…</option>
          {classes.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
              {c.section ? ` — ${c.section}` : ""}
            </option>
          ))}
        </select>

        <select
          value={selectedStudentId}
          onChange={(e) => setSelectedStudentId(e.target.value)}
          disabled={!classId}
          aria-label="Student to enroll"
          className={compactInputClass}
        >
          <option value="">Select student…</option>
          {enrollable.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name} — {s.email}
            </option>
          ))}
        </select>

        <Button icon="user-plus" onClick={handleEnroll} disabled={!classId || !selectedStudentId}>
          Enroll
        </Button>
      </div>

      {error && <Alert className="mb-3">{error}</Alert>}

      {isLoadingClasses ? (
        <LoadingLine label="Loading classes…" />
      ) : !classId ? (
        <EmptyState
          icon="users"
          title="Select a class"
          description="Pick a class above to see and edit its roster."
        />
      ) : students === null ? (
        <LoadingLine label="Loading roster…" />
      ) : students.length === 0 ? (
        <EmptyState
          icon="user-plus"
          title="No students enrolled in this class"
          description="Choose a student above and enroll them to build the roster."
        />
      ) : (
        <ul className={dividedListClass}>
          {students.map((student) => (
            <li key={student.id} className="flex items-center justify-between gap-2 py-2.5 text-sm">
              <span className="flex min-w-0 items-center gap-2.5">
                <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-info-soft text-info">
                  <Icon name="graduation-cap" size="md" />
                </span>
                <span className="min-w-0">
                  <span className="block font-medium text-foreground">{student.name}</span>
                  <span className={`block truncate ${subtleTextClass}`}>{student.email}</span>
                </span>
              </span>

              <Button
                variant="danger"
                icon="user-minus"
                onClick={() => handleUnenroll(student.id)}
                aria-label={`Unenroll ${student.name}`}
              >
                Unenroll
              </Button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
