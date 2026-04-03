"use client";

import { Bell, Menu, User } from "lucide-react";
import { ThemeToggle } from "./theme-toggle";
import { useUiStore } from "@/stores";
import { cn } from "@/lib/utils";

export function Header() {
  const { sidebarCollapsed, toggleSidebar } = useUiStore();

  return (
    <header
      className={cn(
        "fixed top-0 right-0 z-30 h-16 flex items-center justify-between px-6",
        "bg-[var(--color-surface)]/80 backdrop-blur-xl border-b border-[var(--color-border)]",
        "transition-all duration-300",
        sidebarCollapsed ? "left-[68px]" : "left-64"
      )}
    >
      {/* ── Left: Breadcrumb / Menu ───────────────── */}
      <div className="flex items-center gap-4">
        <button
          onClick={toggleSidebar}
          className="lg:hidden flex items-center justify-center w-9 h-9 rounded-lg
                     hover:bg-[var(--color-bg-tertiary)] transition-colors"
        >
          <Menu className="w-5 h-5" />
        </button>

        <div className="hidden sm:flex items-center text-sm text-[var(--color-text-muted)]">
          <span className="font-medium text-[var(--color-text)]">
            EntApp Framework
          </span>
          <span className="mx-2">/</span>
          <span>Dashboard</span>
        </div>
      </div>

      {/* ── Right: Actions ────────────────────────── */}
      <div className="flex items-center gap-2">
        <ThemeToggle />

        {/* Notification Bell */}
        <button
          className="relative flex items-center justify-center w-9 h-9 rounded-lg
                     hover:bg-[var(--color-bg-tertiary)] transition-colors"
        >
          <Bell className="w-4 h-4 text-[var(--color-text-secondary)]" />
          <span className="absolute top-1 right-1.5 w-2 h-2 rounded-full bg-red-500" />
        </button>

        {/* User Menu */}
        <button
          className="flex items-center gap-2 px-3 py-1.5 rounded-lg
                     hover:bg-[var(--color-bg-tertiary)] transition-colors ml-1"
        >
          <div className="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600
                          flex items-center justify-center">
            <User className="w-4 h-4 text-white" />
          </div>
          <span className="hidden md:block text-sm font-medium text-[var(--color-text)]">
            Admin
          </span>
        </button>
      </div>
    </header>
  );
}
