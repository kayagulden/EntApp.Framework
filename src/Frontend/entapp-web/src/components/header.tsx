"use client";

import { useState, useRef, useEffect } from "react";
import { Bell, Menu, User, ChevronDown, UserCog, Check } from "lucide-react";
import { ThemeToggle } from "./theme-toggle";
import { useUiStore, useAuthStore, DEV_USERS } from "@/stores";
import { cn } from "@/lib/utils";

export function Header() {
  const { sidebarCollapsed, toggleSidebar } = useUiStore();
  const { user, loginAs } = useAuthStore();
  const [showUserMenu, setShowUserMenu] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  // Close menu on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setShowUserMenu(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  const initials = user?.fullName
    ?.split(" ")
    .map((w) => w[0])
    .join("")
    .toUpperCase()
    .slice(0, 2) || "?";

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
        <div className="relative ml-1" ref={menuRef}>
          <button
            onClick={() => setShowUserMenu(!showUserMenu)}
            className={cn(
              "flex items-center gap-2 px-3 py-1.5 rounded-lg transition-colors",
              showUserMenu
                ? "bg-[var(--color-bg-tertiary)]"
                : "hover:bg-[var(--color-bg-tertiary)]"
            )}
          >
            <div className="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600
                            flex items-center justify-center text-white text-xs font-bold">
              {initials}
            </div>
            <div className="hidden md:flex flex-col items-start">
              <span className="text-sm font-medium text-[var(--color-text)] leading-tight">
                {user?.fullName || "Seçilmedi"}
              </span>
              <span className="text-[10px] text-[var(--color-text-muted)] leading-tight">
                {user?.role || ""}
              </span>
            </div>
            <ChevronDown className={cn(
              "w-3.5 h-3.5 text-[var(--color-text-muted)] transition-transform duration-200",
              showUserMenu && "rotate-180"
            )} />
          </button>

          {/* Dropdown Menu */}
          {showUserMenu && (
            <div className={cn(
              "absolute right-0 top-full mt-2 w-72 rounded-xl overflow-hidden",
              "bg-[var(--color-card-bg)] border border-[var(--color-border)]",
              "shadow-xl shadow-black/20 animate-fade-in"
            )}>
              {/* Dev Mode Header */}
              <div className="px-4 py-3 border-b border-[var(--color-border)] bg-amber-500/5">
                <div className="flex items-center gap-2">
                  <UserCog className="w-4 h-4 text-amber-400" />
                  <span className="text-xs font-semibold text-amber-400 uppercase tracking-wider">
                    Dev Mode — Login As
                  </span>
                </div>
                <p className="text-[11px] text-[var(--color-text-muted)] mt-1">
                  Geliştirme ortamı kullanıcı simülasyonu
                </p>
              </div>

              {/* User List */}
              <div className="py-1 max-h-72 overflow-y-auto">
                {DEV_USERS.map((devUser) => {
                  const isActive = user?.id === devUser.id;
                  const userInitials = devUser.fullName
                    .split(" ")
                    .map((w) => w[0])
                    .join("")
                    .toUpperCase();

                  return (
                    <button
                      key={devUser.id}
                      onClick={() => {
                        loginAs(devUser.id);
                        setShowUserMenu(false);
                      }}
                      className={cn(
                        "w-full flex items-center gap-3 px-4 py-2.5 text-left transition-all duration-150",
                        isActive
                          ? "bg-indigo-500/10"
                          : "hover:bg-[var(--color-border)]/40"
                      )}
                    >
                      <div className={cn(
                        "w-9 h-9 rounded-full flex items-center justify-center text-xs font-bold shrink-0",
                        isActive
                          ? "bg-gradient-to-br from-indigo-500 to-purple-600 text-white"
                          : "bg-[var(--color-border)] text-[var(--color-text-muted)]"
                      )}>
                        {userInitials}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span className={cn(
                            "text-sm font-medium truncate",
                            isActive ? "text-indigo-400" : "text-[var(--color-text)]"
                          )}>
                            {devUser.fullName}
                          </span>
                          {isActive && (
                            <Check className="w-3.5 h-3.5 text-indigo-400 shrink-0" />
                          )}
                        </div>
                        <div className="flex items-center gap-2 mt-0.5">
                          <span className="text-[11px] text-[var(--color-text-muted)] truncate">
                            {devUser.userName}
                          </span>
                          <span className="text-[10px] px-1.5 py-0.5 rounded bg-[var(--color-border)] text-[var(--color-text-muted)] font-mono">
                            {devUser.role}
                          </span>
                        </div>
                      </div>
                    </button>
                  );
                })}
              </div>

              {/* Footer */}
              <div className="px-4 py-2 border-t border-[var(--color-border)] bg-[var(--color-bg)]/50">
                <p className="text-[10px] text-[var(--color-text-muted)] font-mono text-center">
                  ID: {user?.id?.slice(0, 8)}…
                </p>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
