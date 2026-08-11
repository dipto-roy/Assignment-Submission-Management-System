"use client";

import { useEffect, useState, type FormEvent } from "react";
import { createClass, deleteClass, getClasses } from "@/lib/api/admin";
import type { SchoolClass } from "@/types";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import {
  Alert,
  Badge,
  EmptyState,
  LoadingLine,
  SectionHeading,
} from "@/components/ui/primitives";
import { compactInputClass, dividedListClass } from "@/components/ui/styles";

export function ClassesPanel() {
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [name, setName] = useState("");
  const [section, setSection] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const reload = () => getClasses().then(setClasses).catch((e) => setError(e.message));

  useEffect(() => {
    reload().finally(() => setIsLoading(false));
  }, []);

  const handleCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    try {
      await createClass({ name, section: section || null });
      setName("");
      setSection("");
      await reload();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to create class.");
    }
  };

  const handleDelete = async (id: string) => {
    setError(null);
    try {
      await deleteClass(id);
      await reload();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to delete class.");
    }
  };

  return (
    <section>
      <SectionHeading
        icon="users"
        title="Classes"
        description="The groups students belong to. A class holds subjects and enrollment."
        meta={classes.length > 0 ? <Badge tone="primary">{classes.length}</Badge> : undefined}
      />

      <form onSubmit={handleCreate} className="mb-5 flex flex-wrap items-end gap-2">
        <input
          placeholder="Name (e.g. Class 10)"
          aria-label="Class name"
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
          className={compactInputClass}
        />
        <input
          placeholder="Section (e.g. A)"
          aria-label="Section"
          value={section}
          onChange={(e) => setSection(e.target.value)}
          className={compactInputClass}
        />
        <Button type="submit" icon="plus">
          Add
        </Button>
      </form>

      {error && <Alert className="mb-3">{error}</Alert>}

      {isLoading ? (
        <LoadingLine label="Loading classes…" />
      ) : classes.length === 0 ? (
        <EmptyState
          icon="users"
          title="No classes yet"
          description="Create a class before adding students or subjects."
        />
      ) : (
        <ul className={dividedListClass}>
          {classes.map((c) => (
            <li
              key={c.id}
              className="flex flex-wrap items-center justify-between gap-2 py-2.5 text-sm"
            >
              <span className="flex items-center gap-2.5">
                <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-primary-soft text-primary-soft-foreground">
                  <Icon name="users" size="md" />
                </span>
                <span className="font-medium text-foreground">
                  {c.name}
                  {c.section ? ` — ${c.section}` : ""}
                </span>
              </span>

              <Button
                variant="danger"
                icon="trash"
                onClick={() => handleDelete(c.id)}
                aria-label={`Delete ${c.name}`}
              >
                Delete
              </Button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
