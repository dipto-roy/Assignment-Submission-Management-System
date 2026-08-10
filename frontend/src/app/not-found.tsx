import Link from "next/link";

export default function NotFound() {
  return (
    <main className="mx-auto flex max-w-md flex-col items-start gap-4 p-8">
      <h1 className="text-xl font-semibold">Page not found</h1>
      <p className="text-sm text-black/60 dark:text-white/60">
        The page you are looking for does not exist or you do not have access to it.
      </p>
      <Link href="/" className="text-sm underline">
        Back to the start
      </Link>
    </main>
  );
}
