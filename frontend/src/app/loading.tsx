import { LoadingLine, Skeleton } from "@/components/ui/primitives";

/** Reserves the height of a typical dashboard so the real content does not shift it (CLS). */
export default function Loading() {
  return (
    <main className="mx-auto w-full max-w-5xl px-4 py-8 sm:px-8">
      <LoadingLine />
      <div className="mt-6 flex flex-col gap-4">
        <Skeleton className="h-11 w-64" />
        <Skeleton className="h-28 w-full" />
        <Skeleton className="h-28 w-full" />
      </div>
    </main>
  );
}
