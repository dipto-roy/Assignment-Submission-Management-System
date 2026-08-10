"use client";

import { useEffect, useState, type FormEvent } from "react";
import { createClass, deleteClass, getClasses } from "@/lib/api/admin";
import type { SchoolClass } from "@/types";
import {
  dangerButtonClass,
  inputClass,
  mutedTextClass,
  primaryButtonClass,
} from "@/components/ui/styles";

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
      <h2 className="mb-3 text-lg font-semibold">Classes</h2>

      <form onSubmit={handleCreate} className="mb-4 flex flex-wrap gap-2">
        <input
          placeholder="Name (e.g. Class 10)"
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
          className={inputClass}
        />
        <input
          placeholder="Section (e.g. A)"
          value={section}
          onChange={(e) => setSection(e.target.value)}
          className={inputClass}
        />
        <button type="submit" className={primaryButtonClass}>
          Add
        </button>
      </form>

      {error && <p role="alert" className="mb-3 text-sm text-red-600 dark:text-red-400">{error}</p>}

      {isLoading ? (
        <p className={mutedTextClass}>Loading…</p>
      ) : (
        <ul className="divide-y divide-black/10 dark:divide-white/10">
          {classes.map((c) => (
            <li key={c.id} className="flex flex-wrap items-center justify-between gap-2 py-2 text-sm">
              <span>
                {c.name}
                {c.section ? ` — ${c.section}` : ""}
              </span>
              <button onClick={() => handleDelete(c.id)} className={dangerButtonClass}>
                Delete
              </button>
            </li>
          ))}
          {classes.length === 0 && <li className={`py-2 ${mutedTextClass}`}>No classes yet.</li>}
        </ul>
      )}
    </section>
  );
}
