"use client";

import { useState, type FormEvent } from "react";
import { createUser, deleteUser, getUsersPage } from "@/lib/api/admin";
import { useClasses } from "@/lib/hooks/useClasses";
import { usePagedList } from "@/lib/hooks/usePagedList";
import { Pagination } from "@/components/ui/Pagination";
import type { UserRole, UserSummary } from "@/types";
import { Button } from "@/components/ui/Button";
import { Icon, type IconName } from "@/components/ui/Icon";
import {
  Alert,
  Badge,
  EmptyState,
  LoadingLine,
  SectionHeading,
} from "@/components/ui/primitives";
import { compactInputClass, dividedListClass, subtleTextClass } from "@/components/ui/styles";

const ROLES: UserRole[] = ["Admin", "Teacher", "Student"];

/** Role → its badge so a roster is scannable without reading every line. */
const ROLE_BADGE: Record<UserRole, { tone: "danger" | "primary" | "info"; icon: IconName }> = {
  Admin: { tone: "danger", icon: "shield" },
  Teacher: { tone: "primary", icon: "book-open" },
  Student: { tone: "info", icon: "graduation-cap" },
};

export function UsersPanel() {
  const { classes } = useClasses();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<UserRole>("Teacher");
  const [classId, setClassId] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const {
    items: users,
    meta,
    isLoading,
    isRefreshing,
    error: loadError,
    setPage,
    setPageSize,
    reload,
  } = usePagedList<UserSummary>((params) => getUsersPage(params), {
    errorMessage: "Failed to load users.",
  });

  const error = formError ?? loadError;

  const handleCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);
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
      reload();
    } catch (e: unknown) {
      setFormError(e instanceof Error ? e.message : "Failed to create user.");
    }
  };

  const handleDelete = async (id: string) => {
    setFormError(null);
    try {
      await deleteUser(id);
      reload();
    } catch (e: unknown) {
      setFormError(e instanceof Error ? e.message : "Failed to delete user.");
    }
  };

  return (
    <section>
      <SectionHeading
        icon="users"
        title="Users"
        description="Create accounts and remove people who have left."
        meta={meta.total > 0 ? <Badge tone="primary">{meta.total}</Badge> : undefined}
      />

      <form onSubmit={handleCreate} className="mb-5 flex flex-wrap items-end gap-2">
        <input
          placeholder="Name"
          aria-label="Name"
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
          className={compactInputClass}
        />
        <input
          type="email"
          placeholder="Email"
          aria-label="Email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className={compactInputClass}
        />
        <input
          type="password"
          placeholder="Password (min 8 chars)"
          aria-label="Password"
          required
          minLength={8}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className={compactInputClass}
        />
        <select
          value={role}
          aria-label="Role"
          onChange={(e) => setRole(e.target.value as UserRole)}
          className={compactInputClass}
        >
          {ROLES.map((r) => (
            <option key={r} value={r}>
              {r}
            </option>
          ))}
        </select>

        {role === "Student" && (
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
        )}

        <Button type="submit" icon="user-plus">
          Add
        </Button>
      </form>

      {error && <Alert className="mb-3">{error}</Alert>}

      {isLoading ? (
        <LoadingLine label="Loading users…" />
      ) : users.length === 0 ? (
        <EmptyState
          icon="users"
          title="No users yet"
          description="Add the first teacher or student with the form above."
        />
      ) : (
        <>
        <ul className={dividedListClass}>
          {users.map((u) => {
            const badge = ROLE_BADGE[u.role];

            return (
              <li
                key={u.id}
                className="flex flex-wrap items-center justify-between gap-2 py-2.5 text-sm"
              >
                <span className="flex min-w-0 items-center gap-2.5">
                  <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-muted text-foreground-muted">
                    <Icon name="user" size="md" />
                  </span>
                  <span className="min-w-0">
                    <span className="block font-medium text-foreground">{u.name}</span>
                    <span className={`block truncate ${subtleTextClass}`}>{u.email}</span>
                  </span>
                  <Badge tone={badge.tone} icon={badge.icon}>
                    {u.role}
                  </Badge>
                </span>

                <Button
                  variant="danger"
                  icon="trash"
                  onClick={() => handleDelete(u.id)}
                  aria-label={`Delete ${u.name}`}
                >
                  Delete
                </Button>
              </li>
            );
          })}
        </ul>

        <Pagination
          meta={meta}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
          label="users"
          isBusy={isRefreshing}
        />
        </>
      )}
    </section>
  );
}
