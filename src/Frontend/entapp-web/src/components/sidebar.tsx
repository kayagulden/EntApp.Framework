"use client";

import { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  Settings,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  Layers,
  Database,
  Loader2,
  Shield,
  Wrench,
  User,
  ClipboardCheck,
  TicketIcon,
  ListTodo,
  FolderKanban,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useUiStore } from "@/stores";
import { useDynamicMenu } from "@/lib/hooks/use-dynamic-meta";

// ── Section types ─────────────────────────────────
type SectionKey = string; // dynamic group names + static keys

interface SidebarLink {
  href: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  matchPrefix?: boolean; // true → startsWith match
}

interface SidebarSection {
  key: SectionKey;
  label: string;
  links: SidebarLink[];
}

// ── Static sections ───────────────────────────────
const STATIC_SECTIONS: SidebarSection[] = [
  {
    key: "request-mgmt",
    label: "Talep Yönetimi",
    links: [
      { href: "/dashboard/tickets", label: "Talepler", icon: TicketIcon, matchPrefix: true },
      { href: "/dashboard/tasks", label: "Görevler", icon: ListTodo, matchPrefix: true },
      { href: "/dashboard/projects", label: "Projeler", icon: FolderKanban, matchPrefix: true },
    ],
  },
  {
    key: "personal",
    label: "Kişisel",
    links: [
      { href: "/dashboard/approvals", label: "Bekleyen Onaylar", icon: ClipboardCheck },
      { href: "/dashboard/profile", label: "Profilim", icon: User },
    ],
  },
];

// ── Helper: find which section owns current path ──
function findActiveSection(pathname: string, sections: SidebarSection[]): SectionKey | null {
  for (const section of sections) {
    for (const link of section.links) {
      if (link.matchPrefix ? pathname.startsWith(link.href) : pathname === link.href) {
        return section.key;
      }
    }
  }
  return null;
}

export function Sidebar() {
  const pathname = usePathname();
  const { sidebarCollapsed, toggleCollapse } = useUiStore();
  const { data: dynamicMenu, isLoading: menuLoading } = useDynamicMenu();

  // Build dynamic sections from API
  const dynamicSections: SidebarSection[] = (dynamicMenu ?? []).map((group) => ({
    key: `dyn-${group.name}`,
    label: group.name,
    links: group.items.map((item) => ({
      href: `/dashboard/dynamic/${item.entity}`,
      label: item.title,
      icon: Database,
    })),
  }));

  const allSections = [...dynamicSections, ...STATIC_SECTIONS];

  // Accordion state — only one section open at a time
  const [openSection, setOpenSection] = useState<SectionKey | null>(null);

  // Auto-open the section that contains the active route
  const autoSection = findActiveSection(pathname, allSections);
  useEffect(() => {
    if (autoSection) setOpenSection(autoSection);
  }, [autoSection]);

  const toggleSection = useCallback((key: SectionKey) => {
    setOpenSection((prev) => (prev === key ? null : key));
  }, []);

  const isLinkActive = (link: SidebarLink) =>
    link.matchPrefix ? pathname.startsWith(link.href) : pathname === link.href;

  // ── Render a collapsible section ────────────────
  const renderSection = (section: SidebarSection) => {
    const isOpen = openSection === section.key;
    const hasActiveChild = section.links.some(isLinkActive);

    return (
      <div key={section.key} className="mb-1">
        {/* Section header / toggle */}
        {!sidebarCollapsed ? (
          <button
            onClick={() => toggleSection(section.key)}
            className={cn(
              "w-full flex items-center justify-between px-3 py-2 rounded-lg text-xs font-semibold uppercase tracking-wider",
              "transition-all duration-200 group",
              hasActiveChild
                ? "text-slate-300"
                : "text-slate-500 hover:text-slate-300"
            )}
          >
            <span>{section.label}</span>
            <ChevronDown
              className={cn(
                "w-3.5 h-3.5 transition-transform duration-200",
                isOpen ? "rotate-0" : "-rotate-90"
              )}
            />
          </button>
        ) : (
          <div className="h-px mx-3 my-2 bg-white/10" />
        )}

        {/* Section items — animated collapse */}
        <div
          className={cn(
            "overflow-hidden transition-all duration-200 ease-in-out",
            sidebarCollapsed
              ? "max-h-[500px] opacity-100" // always visible when collapsed
              : isOpen
                ? "max-h-[500px] opacity-100"
                : "max-h-0 opacity-0"
          )}
        >
          <ul className="space-y-0.5 mt-0.5">
            {section.links.map((link) => {
              const Icon = link.icon;
              const active = isLinkActive(link);
              return (
                <li key={link.href}>
                  <Link
                    href={link.href}
                    className={cn(
                      "flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium",
                      "transition-all duration-200",
                      active
                        ? "bg-[var(--color-sidebar-active)] text-white shadow-md shadow-indigo-500/20"
                        : "text-[var(--color-sidebar-text)] hover:bg-[var(--color-sidebar-hover)] hover:text-white"
                    )}
                    title={sidebarCollapsed ? link.label : undefined}
                  >
                    <Icon className="w-4 h-4 shrink-0" />
                    {!sidebarCollapsed && (
                      <span className="animate-fade-in truncate">{link.label}</span>
                    )}
                  </Link>
                </li>
              );
            })}
          </ul>
        </div>
      </div>
    );
  };

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
        {/* Ana Sayfa — always visible, no section */}
        <ul className="space-y-1 mb-3">
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

        {/* Menu Loading */}
        {menuLoading && (
          <div className="flex justify-center py-3">
            <Loader2 className="w-4 h-4 text-slate-500 animate-spin" />
          </div>
        )}

        {/* All accordion sections */}
        {allSections.map(renderSection)}
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

