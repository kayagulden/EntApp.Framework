"use client";

import { useState, useEffect, useCallback, ReactNode } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";

// ── Types ─────────────────────────────────────────
interface MenuItem {
  label: string;
  href: string;
  icon: ReactNode;
  matchExact?: boolean; // true → exact match only (for Dashboard)
  disabled?: boolean; // true → greyed out with "Yakında" badge
}

interface MenuSection {
  key: string;
  label: string;
  items: MenuItem[];
}

// ── Inline SVG icon helper ────────────────────────
const svgIcon = (d: string | string[]) => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    {(Array.isArray(d) ? d : [d]).map((path, i) => (
      <path key={i} strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d={path} />
    ))}
  </svg>
);

// ── Menu definition ───────────────────────────────
const MENU_SECTIONS: MenuSection[] = [
  {
    key: "identity",
    label: "Kimlik & Erişim",
    items: [
      {
        label: "Kullanıcılar",
        href: "/manage/users",
        icon: svgIcon("M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z"),
      },
      {
        label: "Roller & Yetkiler",
        href: "/manage/roles",
        icon: svgIcon("M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z"),
      },
      {
        label: "Organizasyon",
        href: "/manage/organizations",
        icon: svgIcon("M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3.375c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21M3 3h12m-.75 4.5H21m-3.75 3H21m-3.75 3H21"),
      },
    ],
  },
  {
    key: "config",
    label: "Konfigürasyon",
    items: [
      {
        label: "Tenant Ayarları",
        href: "/manage/settings",
        icon: svgIcon([
          "M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.324.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 011.37.49l1.296 2.247a1.125 1.125 0 01-.26 1.431l-1.003.827c-.293.24-.438.613-.431.992a6.759 6.759 0 010 .255c-.007.378.138.75.43.99l1.005.828c.424.35.534.954.26 1.43l-1.298 2.247a1.125 1.125 0 01-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.57 6.57 0 01-.22.128c-.331.183-.581.495-.644.869l-.213 1.28c-.09.543-.56.941-1.11.941h-2.594c-.55 0-1.02-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 01-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 01-1.369-.49l-1.297-2.247a1.125 1.125 0 01.26-1.431l1.004-.827c.292-.24.437-.613.43-.992a6.932 6.932 0 010-.255c.007-.378-.138-.75-.43-.99l-1.004-.828a1.125 1.125 0 01-.26-1.43l1.297-2.247a1.125 1.125 0 011.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.087.22-.128.332-.183.582-.495.644-.869l.214-1.281z",
          "M15 12a3 3 0 11-6 0 3 3 0 016 0z",
        ]),
      },
      {
        label: "Feature Flags",
        href: "/manage/feature-flags",
        icon: svgIcon("M3 3v1.5M3 21v-6m0 0l2.77-.693a9 9 0 016.208.682l.108.054a9 9 0 006.086.71l3.114-.732a48.524 48.524 0 01-.005-10.499l-3.11.732a9 9 0 01-6.085-.711l-.108-.054a9 9 0 00-6.208-.682L3 4.5M3 15V4.5"),
      },
      {
        label: "UI Konfigürasyon",
        href: "/manage/ui-config",
        icon: svgIcon("M9.53 16.122a3 3 0 00-5.78 1.128 2.25 2.25 0 01-2.4 2.245 4.5 4.5 0 008.4-2.245c0-.399-.078-.78-.22-1.128zm0 0a15.998 15.998 0 003.388-1.62m-5.043-.025a15.994 15.994 0 011.622-3.395m3.42 3.42a15.995 15.995 0 004.764-4.648l3.876-5.814a1.151 1.151 0 00-1.597-1.597L14.146 6.32a15.996 15.996 0 00-4.649 4.763m3.42 3.42a6.776 6.776 0 00-3.42-3.42"),
      },
      {
        label: "Prompt Şablonları",
        href: "/manage/prompts",
        icon: svgIcon("M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 01.865-.501 48.172 48.172 0 003.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0012 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018z"),
      },
    ],
  },
  {
    key: "request-mgmt",
    label: "Talep Yönetimi",
    items: [
      {
        label: "Talep Kategorileri",
        href: "/manage/categories",
        icon: svgIcon([
          "M9.568 3H5.25A2.25 2.25 0 003 5.25v4.318c0 .597.237 1.17.659 1.591l9.581 9.581c.699.699 1.78.872 2.607.33a18.095 18.095 0 005.223-5.223c.542-.827.369-1.908-.33-2.607L11.16 3.66A2.25 2.25 0 009.568 3z",
          "M6 6h.008v.008H6V6z",
        ]),
      },
      {
        label: "Hizmet Kuyrukları",
        href: "/manage/queues",
        icon: svgIcon("M2.25 13.5h3.86a2.25 2.25 0 012.012 1.244l.256.512a2.25 2.25 0 002.013 1.244h3.218a2.25 2.25 0 002.013-1.244l.256-.512a2.25 2.25 0 012.013-1.244h3.859m-19.5.338V18a2.25 2.25 0 002.25 2.25h15A2.25 2.25 0 0021.75 18v-4.162c0-.224-.034-.447-.1-.661L19.24 5.338a2.25 2.25 0 00-2.15-1.588H6.911a2.25 2.25 0 00-2.15 1.588L2.35 13.177a2.25 2.25 0 00-.1.661z"),
      },
      {
        label: "Durum Akışları",
        href: "/manage/state-flows",
        icon: svgIcon("M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5"),
      },
      {
        label: "Otomasyon Kuralları",
        href: "/manage/automation-rules",
        icon: svgIcon("M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75z"),
      },
    ],
  },
  {
    key: "cmdb",
    label: "CMDB",
    items: [
      {
        label: "Uygulamalar",
        href: "/manage/cmdb/applications",
        icon: svgIcon("M6.429 9.75L2.25 12l4.179 2.25m0-4.5l5.571 3 5.571-3m-11.142 0L2.25 7.5 12 2.25l9.75 5.25-4.179 2.25m0 0L12 12.75 6.429 9.75m11.142 0l4.179 2.25L12 17.25 2.25 12l4.179-2.25m11.142 0l4.179 2.25-9.75 5.25-9.75-5.25 4.179-2.25"),
      },
      {
        label: "Sunucular",
        href: "/manage/cmdb/servers",
        icon: svgIcon(["M21.75 17.25v-.228a4.5 4.5 0 00-.12-1.03l-2.268-9.64a3.375 3.375 0 00-3.285-2.602H7.923a3.375 3.375 0 00-3.285 2.602l-2.268 9.64a4.5 4.5 0 00-.12 1.03v.228m19.5 0a3 3 0 01-3 3H5.25a3 3 0 01-3-3m19.5 0a3 3 0 00-3-3H5.25a3 3 0 00-3 3m16.5 0h.008v.008h-.008v-.008zm-3 0h.008v.008h-.008v-.008z"]),
      },
      {
        label: "Veritabanları",
        href: "/manage/cmdb/databases",
        icon: svgIcon(["M20.25 6.375c0 2.278-3.694 4.125-8.25 4.125S3.75 8.653 3.75 6.375m16.5 0c0-2.278-3.694-4.125-8.25-4.125S3.75 4.097 3.75 6.375m16.5 0v11.25c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125V6.375m16.5 3.75c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125m16.5 3.75c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125"]),
      },
      {
        label: "Lisanslar",
        href: "/manage/cmdb/licences",
        icon: svgIcon("M15.75 5.25a3 3 0 013 3m3 0a6 6 0 01-7.029 5.912c-.563-.097-1.159.026-1.563.43L10.5 17.25H8.25v2.25H6v2.25H2.25v-2.818c0-.597.237-1.17.659-1.591l6.499-6.499c.404-.404.527-1 .43-1.563A6 6 0 1121.75 8.25z"),
      },
      {
        label: "Ağ Cihazları",
        href: "/manage/cmdb/network-devices",
        icon: svgIcon("M8.288 15.038a5.25 5.25 0 017.424 0M5.106 11.856c3.807-3.808 9.98-3.808 13.788 0M1.924 8.674c5.565-5.565 14.587-5.565 20.152 0M12.53 18.22l-.53.53-.53-.53a.75.75 0 011.06 0z"),
        disabled: true,
      },
    ],
  },
  {
    key: "monitoring",
    label: "İzleme",
    items: [
      {
        label: "Audit Logları",
        href: "/manage/audit-logs",
        icon: svgIcon("M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"),
      },
    ],
  },
];

// ── Helper: find active section by pathname ───────
function findActiveSection(pathname: string): string | null {
  for (const section of MENU_SECTIONS) {
    for (const item of section.items) {
      if (item.matchExact ? pathname === item.href : pathname?.startsWith(item.href)) {
        return section.key;
      }
    }
  }
  return null;
}

// ── Component ─────────────────────────────────────
interface ManageSidebarProps {
  collapsed: boolean;
  onToggle: () => void;
}

export function ManageSidebar({ collapsed, onToggle }: ManageSidebarProps) {
  const pathname = usePathname();

  // Accordion state — only one section open at a time
  const [openSection, setOpenSection] = useState<string | null>(null);

  // Auto-open section containing active route
  const autoSection = findActiveSection(pathname);
  useEffect(() => {
    if (autoSection) setOpenSection(autoSection);
  }, [autoSection]);

  const toggleSection = useCallback((key: string) => {
    setOpenSection((prev) => (prev === key ? null : key));
  }, []);

  const isLinkActive = (item: MenuItem) =>
    item.matchExact ? pathname === item.href : pathname?.startsWith(item.href);

  // ── Render collapsible section ──────────────────
  const renderSection = (section: MenuSection) => {
    const isOpen = openSection === section.key;
    const hasActiveChild = section.items.some(isLinkActive);

    return (
      <div key={section.key} className="mb-1">
        {/* Section header / toggle */}
        {!collapsed ? (
          <button
            onClick={() => toggleSection(section.key)}
            className={cn(
              "w-full flex items-center justify-between px-3 py-2 rounded-lg text-[10px] font-semibold uppercase tracking-wider",
              "transition-all duration-200",
              hasActiveChild
                ? "text-slate-300"
                : "text-slate-500 hover:text-slate-300"
            )}
          >
            <span>{section.label}</span>
            <svg
              className={cn(
                "w-3.5 h-3.5 transition-transform duration-200",
                isOpen ? "rotate-0" : "-rotate-90"
              )}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>
        ) : (
          <div className="h-px mx-3 my-2 bg-white/10" />
        )}

        {/* Section items — animated collapse */}
        <div
          className={cn(
            "overflow-hidden transition-all duration-200 ease-in-out",
            collapsed
              ? "max-h-[500px] opacity-100"
              : isOpen
                ? "max-h-[500px] opacity-100"
                : "max-h-0 opacity-0"
          )}
        >
          <div className="space-y-0.5 mt-0.5">
            {section.items.map((item) => {
              const active = isLinkActive(item);
              if (item.disabled) {
                return (
                  <span
                    key={item.href}
                    className="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 cursor-not-allowed select-none"
                    title={collapsed ? `${item.label} (Yakında)` : undefined}
                  >
                    <span>{item.icon}</span>
                    {!collapsed && (
                      <>
                        <span>{item.label}</span>
                        <span className="ml-auto text-[9px] font-semibold uppercase tracking-wider bg-slate-700/50 text-slate-500 px-1.5 py-0.5 rounded">Yakında</span>
                      </>
                    )}
                  </span>
                );
              }
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={cn(
                    "flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200",
                    active
                      ? "bg-gradient-to-r from-teal-500/20 to-cyan-500/10 text-teal-400 shadow-lg shadow-teal-500/5"
                      : "text-slate-400 hover:text-white hover:bg-white/5"
                  )}
                  title={collapsed ? item.label : undefined}
                >
                  <span className={cn(active && "text-teal-400")}>{item.icon}</span>
                  {!collapsed && <span>{item.label}</span>}
                </Link>
              );
            })}
          </div>
        </div>
      </div>
    );
  };

  return (
    <aside
      className={cn(
        "fixed left-0 top-0 h-screen z-40 flex flex-col transition-all duration-300",
        "bg-gradient-to-b from-[#0f172a] to-[#0f2a2a] border-r border-white/5",
        collapsed ? "w-[68px]" : "w-64"
      )}
    >
      {/* ── Header ─────────────────────── */}
      <div className="h-16 flex items-center justify-between px-4 border-b border-white/10">
        {!collapsed && (
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-teal-400 to-cyan-500 flex items-center justify-center">
              <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
              </svg>
            </div>
            <span className="text-white font-semibold text-sm">Tenant Yönetimi</span>
          </div>
        )}
        <button
          onClick={onToggle}
          className="text-slate-400 hover:text-white p-1.5 rounded-lg hover:bg-white/5 transition-colors"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={collapsed ? "M13 5l7 7-7 7M5 5l7 7-7 7" : "M11 19l-7-7 7-7m8 14l-7-7 7-7"} />
          </svg>
        </button>
      </div>

      {/* ── Menu Items ─────────────────── */}
      <nav className="flex-1 py-4 px-2 overflow-y-auto">
        {/* Dashboard — always visible, no section */}
        <div className="mb-3">
          <Link
            href="/manage"
            className={cn(
              "flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200",
              pathname === "/manage"
                ? "bg-gradient-to-r from-teal-500/20 to-cyan-500/10 text-teal-400 shadow-lg shadow-teal-500/5"
                : "text-slate-400 hover:text-white hover:bg-white/5"
            )}
            title={collapsed ? "Dashboard" : undefined}
          >
            <span className={cn(pathname === "/manage" && "text-teal-400")}>
              {svgIcon("M3.75 6A2.25 2.25 0 016 3.75h2.25A2.25 2.25 0 0110.5 6v2.25a2.25 2.25 0 01-2.25 2.25H6a2.25 2.25 0 01-2.25-2.25V6zM3.75 15.75A2.25 2.25 0 016 13.5h2.25a2.25 2.25 0 012.25 2.25V18a2.25 2.25 0 01-2.25 2.25H6A2.25 2.25 0 013.75 18v-2.25zM13.5 6a2.25 2.25 0 012.25-2.25H18A2.25 2.25 0 0120.25 6v2.25A2.25 2.25 0 0118 10.5h-2.25a2.25 2.25 0 01-2.25-2.25V6zM13.5 15.75a2.25 2.25 0 012.25-2.25H18a2.25 2.25 0 012.25 2.25V18A2.25 2.25 0 0118 20.25h-2.25A2.25 2.25 0 0113.5 18v-2.25z")}
            </span>
            {!collapsed && <span>Dashboard</span>}
          </Link>
        </div>

        {/* Accordion sections */}
        {MENU_SECTIONS.map(renderSection)}
      </nav>

      {/* ── Footer ─────────────────────── */}
      <div className="p-3 border-t border-white/10 space-y-1">
        <Link
          href="/dashboard"
          className="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm text-slate-400 hover:text-white hover:bg-white/5 transition-colors"
          title={collapsed ? "Dashboard'a Dön" : undefined}
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 15L3 9m0 0l6-6M3 9h12a6 6 0 010 12h-3" />
          </svg>
          {!collapsed && <span>Dashboard&apos;a Dön</span>}
        </Link>
      </div>
    </aside>
  );
}
