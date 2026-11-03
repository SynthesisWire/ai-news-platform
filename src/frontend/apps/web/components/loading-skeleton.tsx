export default function Loading() {
  return (
    <main id="main" className="shell loading-state" aria-busy="true">
      <div className="skeleton skeleton-title" />
      <div className="skeleton skeleton-text" />
      <div className="skeleton skeleton-card" />
      <span className="sr-only">Loading / Carregando…</span>
    </main>
  );
}
