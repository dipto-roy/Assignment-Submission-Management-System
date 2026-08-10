"use client";

import { useEffect, useState } from "react";
import { enrollStudent, getClassStudents, getUsers, unenrollStudent } from "@/lib/api/admin";
import { useClasses } from "@/lib/hooks/useClasses";
import {
  dangerButtonClass,
  inputClass,
  mutedTextClass,
  primaryButtonClass,
} from "@/components/ui/styles";
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
      <h2 className="mb-1 text-lg font-semibold">Enrollment</h2>
      <p className={`mb-3 ${mutedTextClass}`}>
        A student belongs to one class at a time — enrolling them here moves them out of their
        previous class.
      </p>

      <div className="mb-4 flex flex-wrap gap-2">
        <select
          value={classId}
          onChange={(e) => setClassId(e.target.value)}
          aria-label="Class"
          className={inputClass}
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
          className={inputClass}
        >
          <option value="">Select student…</option>
          {enrollable.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name} — {s.email}
            </option>
          ))}
        </select>

        <button
          type="button"
          onClick={handleEnroll}
          disabled={!classId || !selectedStudentId}
          className={primaryButtonClass}
        >
          Enroll
        </button>
      </div>

      {error && (
        <p role="alert" className="mb-3 text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}

      {isLoadingClasses ? (
        <p className={mutedTextClass}>Loading…</p>
      ) : !classId ? (
        <p className={mutedTextClass}>Select a class to see its roster.</p>
      ) : students === null ? (
        <p className={mutedTextClass}>Loading roster…</p>
      ) : (
        <ul className="divide-y divide-black/10 dark:divide-white/10">
          {students.map((student) => (
            <li key={student.id} className="flex items-center justify-between gap-2 py-2 text-sm">
              <span>
                {student.name}{" "}
                <span className="text-black/50 dark:text-white/50">{student.email}</span>
              </span>
              <button
                type="button"
                onClick={() => handleUnenroll(student.id)}
                className={dangerButtonClass}
              >
                Unenroll
              </button>
            </li>
          ))}
          {students.length === 0 && (
            <li className={`py-2 ${mutedTextClass}`}>No students enrolled in this class.</li>
          )}
        </ul>
      )}
    </section>
  );
}
