"use client";

import { useEffect, useState, type FormEvent } from "react";
import { assignTeacher, createSubject, deleteSubject, getSubjectsPage, getUsers } from "@/lib/api/admin";
import { useClasses } from "@/lib/hooks/useClasses";
import { usePagedList } from "@/lib/hooks/usePagedList";
import { Pagination } from "@/components/ui/Pagination";
import type { Subject, UserSummary } from "@/types";
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

export function SubjectsPanel() {
  const { classes } = useClasses();
  const [teachers, setTeachers] = useState<UserSummary[]>([]);
  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [classId, setClassId] = useState("");
  const [assignSelections, setAssignSelections] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState<string | null>(null);

  const {
    items: subjects,
    meta,
    isLoading,
    isRefreshing,
    error: loadError,
    setPage,
    setPageSize,
    reload,
  } = usePagedList<Subject>((params) => getSubjectsPage(params), {
    errorMessage: "Failed to load subjects.",
  });

  const error = formError ?? loadError;

  // Filtered server-side rather than client-side: the assign picker needs every teacher,
  // and narrowing by role first keeps them inside the one page this request asks for.
  useEffect(() => {
    getUsers({ role: "Teacher" })
      .then(setTeachers)
      .catch((e: unknown) =>
        setFormError(e instanceof Error ? e.message : "Failed to load teachers."),
      );
  }, []);

  const handleCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);
    try {
      await createSubject({ name, code, classId });
      setName("");
      setCode("");
      setClassId("");
      reload();
    } catch (e: unknown) {
      setFormError(e instanceof Error ? e.message : "Failed to create subject.");
    }
  };

  const handleDelete = async (id: string) => {
    setFormError(null);
    try {
      await deleteSubject(id);
      reload();
    } catch (e: unknown) {
      setFormError(e instanceof Error ? e.message : "Failed to delete subject.");
    }
  };

  const handleAssign = async (subjectId: string) => {
    const teacherId = assignSelections[subjectId];
    if (!teacherId) return;
    setFormError(null);
    try {
      await assignTeacher(subjectId, teacherId);
      reload();
    } catch (e: unknown) {
      setFormError(e instanceof Error ? e.message : "Failed to assign teacher.");
    }
  };

  return (
    <section>
      <SectionHeading
        icon="book-open"
        title="Subjects"
        description="A subject belongs to one class, and its teachers may set assignments for it."
        meta={meta.total > 0 ? <Badge tone="primary">{meta.total}</Badge> : undefined}
      />

      <form onSubmit={handleCreate} className="mb-5 flex flex-wrap items-end gap-2">
        <input
          placeholder="Name (e.g. Mathematics)"
          aria-label="Subject name"
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
          className={compactInputClass}
        />
        <input
          placeholder="Code (e.g. MATH101)"
          aria-label="Subject code"
          required
          value={code}
          onChange={(e) => setCode(e.target.value)}
          className={`${compactInputClass} font-mono`}
        />
        <select
          required
          aria-label="Class"
          value={classId}
          onChange={(e) => setClassId(e.target.value)}
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
        <Button type="submit" icon="plus">
          Add
        </Button>
      </form>

      {error && <Alert className="mb-3">{error}</Alert>}

      {isLoading ? (
        <LoadingLine label="Loading subjects…" />
      ) : subjects.length === 0 ? (
        <EmptyState
          icon="book-open"
          title="No subjects yet"
          description="Add a subject to a class, then assign the teacher who runs it."
        />
      ) : (
        <>
        <ul className={dividedListClass}>
          {subjects.map((s) => (
            <li key={s.id} className="flex flex-wrap items-center justify-between gap-3 py-3 text-sm">
              <span className="flex min-w-0 items-start gap-2.5">
                <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-accent-soft text-accent-soft-foreground">
                  <Icon name="book-open" size="md" />
                </span>
                <span className="min-w-0">
                  <span className="block font-medium text-foreground">
                    {s.name} <span className="font-mono text-foreground-muted">({s.code})</span>
                  </span>
                  <span className={`block ${subtleTextClass}`}>
                    {s.className}
                    {s.teachers.length > 0 && ` · ${s.teachers.map((t) => t.name).join(", ")}`}
                  </span>
                </span>
              </span>

              <span className="flex flex-wrap items-center gap-2">
                <select
                  value={assignSelections[s.id] ?? ""}
                  aria-label={`Assign a teacher to ${s.name}`}
                  onChange={(e) =>
                    setAssignSelections((prev) => ({ ...prev, [s.id]: e.target.value }))
                  }
                  className={compactInputClass}
                >
                  <option value="">Assign teacher…</option>
                  {teachers.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.name}
                    </option>
                  ))}
                </select>

                <Button
                  variant="subtle"
                  icon="user-plus"
                  onClick={() => handleAssign(s.id)}
                  disabled={!assignSelections[s.id]}
                >
                  Assign
                </Button>

                <Button
                  variant="danger"
                  icon="trash"
                  onClick={() => handleDelete(s.id)}
                  aria-label={`Delete ${s.name}`}
                >
                  Delete
                </Button>
              </span>
            </li>
          ))}
        </ul>

        <Pagination
          meta={meta}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
          label="subjects"
          isBusy={isRefreshing}
        />
        </>
      )}
    </section>
  );
}
