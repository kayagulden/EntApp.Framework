"use client";

import { AdminSidebar } from "@/components/admin/admin-sidebar";

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-screen bg-[var(--color-bg)]">
      <AdminSidebar />

      {/* ── Header Bar ────────────────── */}
      <header className="fixed top-0 right-0 left-64 h-16 z-30 flex items-center justify-between px-6 bg-[var(--color-surface)]/80 backdrop-blur-xl border-b border-[var(--color-border)]">
        <div className="flex items-center gap-2">
          <div className="w-2 h-2 rounded-full bg-amber-400 animate-pulse" />
          <span className="text-xs font-medium text-[var(--color-text-muted)] uppercase tracking-wider">
            Admin Panel
          </span>
        </div>
        <div className="flex items-center gap-3">
          <span className="text-xs text-[var(--color-text-muted)]">
            EntApp Framework v1.0
          </span>
        </div>
      </header>

      {/* ── Main Content ──────────────── */}
      <main className="ml-64 pt-16 min-h-screen transition-all duration-300">
        <div className="p-6 animate-fade-in">{children}</div>
      </main>
    </div>
  );
}
