"use client";

import { useEffect, useState, type FormEvent } from "react";
import { createClass, deleteClass, getClasses } from "@/lib/api/admin";
import type { SchoolClass } from "@/types";

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
          className="rounded border border-black/20 px-2 py-1 text-sm dark:border-white/20 dark:bg-transparent"
        />
        <input
          placeholder="Section (e.g. A)"
          value={section}
          onChange={(e) => setSection(e.target.value)}
          className="rounded border border-black/20 px-2 py-1 text-sm dark:border-white/20 dark:bg-transparent"
        />
        <button type="submit" className="rounded bg-black px-3 py-1 text-sm text-white dark:bg-white dark:text-black">
          Add
        </button>
      </form>

      {error && <p role="alert" className="mb-3 text-sm text-red-600 dark:text-red-400">{error}</p>}

      {isLoading ? (
        <p className="text-sm text-black/60 dark:text-white/60">Loading…</p>
      ) : (
        <ul className="divide-y divide-black/10 dark:divide-white/10">
          {classes.map((c) => (
            <li key={c.id} className="flex items-center justify-between py-2 text-sm">
              <span>
                {c.name}
                {c.section ? ` — ${c.section}` : ""}
              </span>
              <button onClick={() => handleDelete(c.id)} className="text-red-600 hover:underline dark:text-red-400">
                Delete
              </button>
            </li>
          ))}
          {classes.length === 0 && <li className="py-2 text-sm text-black/60 dark:text-white/60">No classes yet.</li>}
        </ul>
      )}
    </section>
  );
}
