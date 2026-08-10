"use client";

import { useEffect, useState } from "react";
import { getSubjects } from "@/lib/api/admin";
import { useAuth } from "@/lib/auth/AuthContext";
import type { Subject } from "@/types";

/**
 * Subjects the signed-in teacher is assigned to. `GET /subjects` is open to any
 * authenticated role and returns every subject, so the teacher scoping happens here
 * for the picker UI; the API still rejects assignments on unowned subjects.
 */
export function useTeacherSubjects(): {
  subjects: Subject[];
  isLoading: boolean;
  error: string | null;
} {
  const { user } = useAuth();
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!user) return;

    let isActive = true;
    getSubjects()
      .then((all) => {
        if (!isActive) return;
        setSubjects(all.filter((s) => s.teachers.some((t) => t.id === user.id)));
      })
      .catch((e: unknown) => {
        if (!isActive) return;
        setError(e instanceof Error ? e.message : "Failed to load subjects.");
      })
      .finally(() => {
        if (isActive) setIsLoading(false);
      });

    return () => {
      isActive = false;
    };
  }, [user]);

  return { subjects, isLoading, error };
}
