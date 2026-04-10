"use client";

import { useState } from "react";
import { ManageSidebar } from "@/components/manage/manage-sidebar";

export default function ManageLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  return (
    <div className="min-h-screen bg-[var(--color-bg)]">
      <ManageSidebar
        collapsed={sidebarCollapsed}
        onToggle={() => setSidebarCollapsed(!sidebarCollapsed)}
      />

      {/* ── Header Bar ────────────────── */}
      <header
        className="fixed top-0 right-0 h-16 z-30 flex items-center justify-between px-6 bg-[var(--color-surface)]/80 backdrop-blur-xl border-b border-[var(--color-border)] transition-all duration-300"
        style={{ left: sidebarCollapsed ? 68 : 256 }}
      >
        <div className="flex items-center gap-2">
          <div className="w-2 h-2 rounded-full bg-teal-400 animate-pulse" />
          <span className="text-xs font-medium text-[var(--color-text-muted)] uppercase tracking-wider">
            Tenant Yönetimi
          </span>
        </div>
        <div className="flex items-center gap-3">
          <span className="text-xs text-[var(--color-text-muted)]">
            EntApp Framework v1.0
          </span>
        </div>
      </header>

      {/* ── Main Content ──────────────── */}
      <main
        className="pt-16 min-h-screen transition-all duration-300"
        style={{ marginLeft: sidebarCollapsed ? 68 : 256 }}
      >
        <div className="p-6 animate-fade-in">{children}</div>
      </main>
    </div>
  );
}
