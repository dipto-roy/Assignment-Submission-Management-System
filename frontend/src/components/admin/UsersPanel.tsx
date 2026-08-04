"use client";

import { useEffect, useState, type FormEvent } from "react";
import { createUser, deleteUser, getUsers } from "@/lib/api/admin";
import { useClasses } from "@/lib/hooks/useClasses";
import type { UserRole, UserSummary } from "@/types";

const ROLES: UserRole[] = ["Admin", "Teacher", "Student"];

export function UsersPanel() {
  const { classes } = useClasses();
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<UserRole>("Teacher");
  const [classId, setClassId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const reload = () => getUsers().then(setUsers).catch((e) => setError(e.message));

  useEffect(() => {
    reload().finally(() => setIsLoading(false));
  }, []);

  const handleCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    try {
      await createUser({
        name,
        email,
        password,
        role,
        classId: role === "Student" ? classId || null : null,
      });
      setName("");
      setEmail("");
      setPassword("");
      setClassId("");
      await reload();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to create user.");
    }
  };

  const handleDelete = async (id: string) => {
    setError(null);
    try {
      await deleteUser(id);
      await reload();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to delete user.");
    }
  };

  const inputClass =
    "rounded border border-black/20 px-2 py-1 text-sm dark:border-white/20 dark:bg-transparent";

  return (
    <section>
      <h2 className="mb-3 text-lg font-semibold">Users</h2>

      <form onSubmit={handleCreate} className="mb-4 flex flex-wrap gap-2">
        <input placeholder="Name" required value={name} onChange={(e) => setName(e.target.value)} className={inputClass} />
        <input
          type="email"
          placeholder="Email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className={inputClass}
        />
        <input
          type="password"
          placeholder="Password (min 8 chars)"
          required
          minLength={8}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className={inputClass}
        />
        <select value={role} onChange={(e) => setRole(e.target.value as UserRole)} className={inputClass}>
          {ROLES.map((r) => (
            <option key={r} value={r}>
              {r}
            </option>
          ))}
        </select>
        {role === "Student" && (
          <select required value={classId} onChange={(e) => setClassId(e.target.value)} className={inputClass}>
            <option value="">Select class…</option>
            {classes.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
                {c.section ? ` — ${c.section}` : ""}
              </option>
            ))}
          </select>
        )}
        <button type="submit" className="rounded bg-black px-3 py-1 text-sm text-white dark:bg-white dark:text-black">
          Add
        </button>
      </form>

      {error && <p role="alert" className="mb-3 text-sm text-red-600 dark:text-red-400">{error}</p>}

      {isLoading ? (
        <p className="text-sm text-black/60 dark:text-white/60">Loading…</p>
      ) : (
        <ul className="divide-y divide-black/10 dark:divide-white/10">
          {users.map((u) => (
            <li key={u.id} className="flex items-center justify-between py-2 text-sm">
              <span>
                {u.name} <span className="text-black/50 dark:text-white/50">({u.role}) — {u.email}</span>
              </span>
              <button onClick={() => handleDelete(u.id)} className="text-red-600 hover:underline dark:text-red-400">
                Delete
              </button>
            </li>
          ))}
          {users.length === 0 && <li className="py-2 text-sm text-black/60 dark:text-white/60">No users yet.</li>}
        </ul>
      )}
    </section>
  );
}
