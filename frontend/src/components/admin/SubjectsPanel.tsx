"use client";

import { useEffect, useState, type FormEvent } from "react";
import { assignTeacher, createSubject, deleteSubject, getSubjects, getUsers } from "@/lib/api/admin";
import { useClasses } from "@/lib/hooks/useClasses";
import type { Subject, UserSummary } from "@/types";
import {
  dangerButtonClass,
  inputClass,
  mutedTextClass,
  primaryButtonClass,
} from "@/components/ui/styles";

export function SubjectsPanel() {
  const { classes } = useClasses();
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [teachers, setTeachers] = useState<UserSummary[]>([]);
  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [classId, setClassId] = useState("");
  const [assignSelections, setAssignSelections] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const reload = () => getSubjects().then(setSubjects).catch((e) => setError(e.message));

  useEffect(() => {
    Promise.all([reload(), getUsers().then((all) => setTeachers(all.filter((u) => u.role === "Teacher")))])
      .catch((e) => setError(e.message))
      .finally(() => setIsLoading(false));
  }, []);

  const handleCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    try {
      await createSubject({ name, code, classId });
      setName("");
      setCode("");
      setClassId("");
      await reload();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to create subject.");
    }
  };

  const handleDelete = async (id: string) => {
    setError(null);
    try {
      await deleteSubject(id);
      await reload();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to delete subject.");
    }
  };

  const handleAssign = async (subjectId: string) => {
    const teacherId = assignSelections[subjectId];
    if (!teacherId) return;
    setError(null);
    try {
      await assignTeacher(subjectId, teacherId);
      await reload();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to assign teacher.");
    }
  };

  return (
    <section>
      <h2 className="mb-3 text-lg font-semibold">Subjects</h2>

      <form onSubmit={handleCreate} className="mb-4 flex flex-wrap gap-2">
        <input placeholder="Name (e.g. Mathematics)" required value={name} onChange={(e) => setName(e.target.value)} className={inputClass} />
        <input placeholder="Code (e.g. MATH101)" required value={code} onChange={(e) => setCode(e.target.value)} className={inputClass} />
        <select required value={classId} onChange={(e) => setClassId(e.target.value)} className={inputClass}>
          <option value="">Select class…</option>
          {classes.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
              {c.section ? ` — ${c.section}` : ""}
            </option>
          ))}
        </select>
        <button type="submit" className={primaryButtonClass}>
          Add
        </button>
      </form>

      {error && <p role="alert" className="mb-3 text-sm text-red-600 dark:text-red-400">{error}</p>}

      {isLoading ? (
        <p className={mutedTextClass}>Loading…</p>
      ) : (
        <ul className="divide-y divide-black/10 dark:divide-white/10">
          {subjects.map((s) => (
            <li key={s.id} className="flex flex-wrap items-center justify-between gap-2 py-2 text-sm">
              <span>
                {s.name} ({s.code}) — {s.className}
                {s.teachers.length > 0 && (
                  <span className="text-black/50 dark:text-white/50">
                    {" "}
                    — teachers: {s.teachers.map((t) => t.name).join(", ")}
                  </span>
                )}
              </span>

              <span className="flex items-center gap-2">
                <select
                  value={assignSelections[s.id] ?? ""}
                  onChange={(e) => setAssignSelections((prev) => ({ ...prev, [s.id]: e.target.value }))}
                  className={inputClass}
                >
                  <option value="">Assign teacher…</option>
                  {teachers.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.name}
                    </option>
                  ))}
                </select>
                <button onClick={() => handleAssign(s.id)} className="text-sm underline">
                  Assign
                </button>
                <button onClick={() => handleDelete(s.id)} className={dangerButtonClass}>
                  Delete
                </button>
              </span>
            </li>
          ))}
          {subjects.length === 0 && <li className={`py-2 ${mutedTextClass}`}>No subjects yet.</li>}
        </ul>
      )}
    </section>
  );
}
