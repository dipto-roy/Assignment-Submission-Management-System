"use client";

import { useEffect, useState } from "react";
import { getClasses } from "@/lib/api/admin";
import type { SchoolClass } from "@/types";

/** Shared read-only class list for dropdowns (Users/Subjects panels). */
export function useClasses(): { classes: SchoolClass[]; isLoading: boolean } {
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    getClasses()
      .then(setClasses)
      .finally(() => setIsLoading(false));
  }, []);

  return { classes, isLoading };
}
