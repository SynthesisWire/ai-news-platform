"use client";
import { useTransition } from "react";
import { useRouter } from "next/navigation";
export function RetryButton({ label }: { label: string }) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  return (
    <button
      className="button button-dark"
      disabled={pending}
      aria-busy={pending}
      onClick={() => startTransition(() => router.refresh())}
    >
      {label}
      <span aria-hidden="true">↻</span>
    </button>
  );
}
