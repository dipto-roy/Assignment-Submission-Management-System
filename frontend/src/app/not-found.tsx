import Link from "next/link";
import { Icon } from "@/components/ui/Icon";
import { secondaryButtonClass } from "@/components/ui/styles";

export default function NotFound() {
  return (
    <main className="mx-auto flex w-full max-w-md flex-1 flex-col items-center justify-center gap-4 px-4 py-16 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-2xl bg-muted text-foreground-subtle">
        <Icon name="search" size="xl" />
      </span>

      <h1 className="text-xl font-semibold tracking-tight text-foreground">Page not found</h1>
      <p className="text-sm text-foreground-muted">
        The page you are looking for does not exist or you do not have access to it.
      </p>

      <Link href="/" className={secondaryButtonClass}>
        <Icon name="arrow-left" size="sm" />
        Back to the start
      </Link>
    </main>
  );
}
