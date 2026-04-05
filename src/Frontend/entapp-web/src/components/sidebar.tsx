"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  Settings,
  ChevronLeft,
  ChevronRight,
  Layers,
  Database,
  Loader2,
  Shield,
  Wrench,
  User,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useUiStore } from "@/stores";
import { useDynamicMenu } from "@/lib/hooks/use-dynamic-meta";

export function Sidebar() {
  const pathname = usePathname();
  const { sidebarCollapsed, toggleCollapse } = useUiStore();
  const { data: dynamicMenu, isLoading: menuLoading } = useDynamicMenu();

  return (
    <aside
      className={cn(
        "fixed left-0 top-0 z-40 h-screen flex flex-col transition-all duration-300 ease-in-out",
        "bg-[var(--color-sidebar-bg)] border-r border-[var(--color-border)]",
        sidebarCollapsed ? "w-[68px]" : "w-64"
      )}
    >
      {/* ── Logo ──────────────────────────────────── */}
      <div className="flex items-center h-16 px-4 border-b border-white/10">
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-indigo-500/20">
            <Layers className="w-5 h-5 text-indigo-400" />
          </div>
          {!sidebarCollapsed && (
            <span className="text-lg font-bold text-white tracking-tight animate-fade-in">
              EntApp
            </span>
          )}
        </div>
      </div>

      {/* ── Navigation ────────────────────────────── */}
      <nav className="flex-1 px-3 py-4 overflow-y-auto">
        {/* Ana Sayfa */}
        <ul className="space-y-1">
          <li>
            <Link
              href="/dashboard"
              className={cn(
                "flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium",
                "transition-all duration-200",
                pathname === "/dashboard"
                  ? "bg-[var(--color-sidebar-active)] text-white shadow-md shadow-indigo-500/20"
                  : "text-[var(--color-sidebar-text)] hover:bg-[var(--color-sidebar-hover)] hover:text-white"
              )}
              title={sidebarCollapsed ? "Ana Sayfa" : undefined}
            >
              <LayoutDashboard className="w-5 h-5 shrink-0" />
              {!sidebarCollapsed && (
                <span className="animate-fade-in">Ana Sayfa</span>
              )}
            </Link>
          </li>
        </ul>

        {/* Dynamic Menu (meta/menu API) */}
        {dynamicMenu && dynamicMenu.length > 0 && (
          <div className="mt-4">
            {dynamicMenu.map((group) => (
              <div key={group.name} className="mb-3">
                {!sidebarCollapsed && (
                  <p className="px-3 py-1.5 text-[11px] uppercase tracking-wider text-slate-500 font-semibold">
                    {group.name}
                  </p>
                )}
                {sidebarCollapsed && (
                  <div className="h-px mx-3 my-2 bg-white/10" />
                )}
                <ul className="space-y-0.5">
                  {group.items.map((item) => {
                    const href = `/dashboard/dynamic/${item.entity}`;
                    const isActive = pathname === href;

                    return (
                      <li key={item.entity}>
                        <Link
                          href={href}
                          className={cn(
                            "flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium",
                            "transition-all duration-200",
                            isActive
                              ? "bg-[var(--color-sidebar-active)] text-white shadow-md shadow-indigo-500/20"
                              : "text-[var(--color-sidebar-text)] hover:bg-[var(--color-sidebar-hover)] hover:text-white"
                          )}
                          title={sidebarCollapsed ? item.title : undefined}
                        >
                          <Database className="w-4 h-4 shrink-0" />
                          {!sidebarCollapsed && (
                            <span className="animate-fade-in truncate">
                              {item.title}
                            </span>
                          )}
                        </Link>
                      </li>
                    );
                  })}
                </ul>
              </div>
            ))}
          </div>
        )}

        {/* Menu Loading */}
        {menuLoading && (
          <div className="mt-6 flex justify-center">
            <Loader2 className="w-4 h-4 text-slate-500 animate-spin" />
          </div>
        )}

        {/* ── Kişisel ────────────────────────────── */}
        <div className="mt-4">
          {!sidebarCollapsed && (
            <div className="flex items-center gap-2 px-3 mb-2">
              <div className="h-px flex-1 bg-white/10" />
              <span className="text-[10px] uppercase tracking-widest text-slate-500 font-semibold">
                Kişisel
              </span>
              <div className="h-px flex-1 bg-white/10" />
            </div>
          )}
          <ul className="space-y-0.5">
            <li>
              <Link
                href="/dashboard/profile"
                className={cn(
                  "flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium",
                  "transition-all duration-200",
                  pathname === "/dashboard/profile"
                    ? "bg-[var(--color-sidebar-active)] text-white"
                    : "text-[var(--color-sidebar-text)] hover:bg-[var(--color-sidebar-hover)] hover:text-white"
                )}
                title={sidebarCollapsed ? "Profilim" : undefined}
              >
                <User className="w-4 h-4 shrink-0" />
                {!sidebarCollapsed && (
                  <span className="animate-fade-in">Profilim</span>
                )}
              </Link>
            </li>
          </ul>
        </div>
      </nav>

      {/* ── Footer: Admin & Manage Links ──────────── */}
      <div className="px-3 py-2 border-t border-white/10 space-y-1">
        <Link
          href="/manage"
          className="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-slate-400 hover:text-white hover:bg-[var(--color-sidebar-hover)] transition-colors"
          title={sidebarCollapsed ? "Tenant Yönetimi" : undefined}
        >
          <Settings className="w-4 h-4 shrink-0" />
          {!sidebarCollapsed && (
            <span className="animate-fade-in">Tenant Yönetimi</span>
          )}
        </Link>
        <Link
          href="/admin"
          className="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-slate-400 hover:text-white hover:bg-[var(--color-sidebar-hover)] transition-colors"
          title={sidebarCollapsed ? "Admin Panel" : undefined}
        >
          <Shield className="w-4 h-4 shrink-0" />
          {!sidebarCollapsed && (
            <span className="animate-fade-in">Admin Panel</span>
          )}
        </Link>
      </div>

      {/* ── Collapse Toggle ───────────────────────── */}
      <div className="p-3 border-t border-white/10">
        <button
          onClick={toggleCollapse}
          className="flex items-center justify-center w-full py-2 rounded-lg
                     text-slate-400 hover:text-white hover:bg-[var(--color-sidebar-hover)]
                     transition-all duration-200"
        >
          {sidebarCollapsed ? (
            <ChevronRight className="w-5 h-5" />
          ) : (
            <ChevronLeft className="w-5 h-5" />
          )}
        </button>
      </div>
    </aside>
  );
}
