"use client";

import { Building2, Plus, ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";

const mockOrgs = [
  {
    name: "EntApp Holding",
    code: "HQ",
    children: [
      { name: "İstanbul Şubesi", code: "IST", children: [] },
      { name: "Ankara Şubesi", code: "ANK", children: [] },
    ],
  },
  {
    name: "Teknoloji A.Ş.", code: "TECH", children: [
      { name: "Yazılım Departmanı", code: "SW", children: [] },
      { name: "DevOps Departmanı", code: "OPS", children: [] },
    ],
  },
];

interface OrgNode {
  name: string;
  code: string;
  children: OrgNode[];
}

function OrgTreeItem({ org, depth = 0 }: { org: OrgNode; depth?: number }) {
  return (
    <div>
      <div
        className={cn(
          "flex items-center gap-3 px-4 py-3 hover:bg-[var(--color-surface-hover)] transition-colors cursor-pointer rounded-lg",
        )}
        style={{ paddingLeft: `${depth * 24 + 16}px` }}
      >
        {org.children.length > 0 && (
          <ChevronRight className="w-4 h-4 text-[var(--color-text-muted)]" />
        )}
        <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-amber-500 to-orange-500 flex items-center justify-center shadow">
          <Building2 className="w-4 h-4 text-white" />
        </div>
        <div>
          <p className="text-sm font-medium text-[var(--color-text)]">{org.name}</p>
          <p className="text-xs text-[var(--color-text-muted)]">{org.code}</p>
        </div>
      </div>
      {org.children.map((child) => (
        <OrgTreeItem key={child.code} org={child} depth={depth + 1} />
      ))}
    </div>
  );
}

export default function OrganizationsPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Organizasyon</h1>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">Organizasyon ağacını yönetin.</p>
        </div>
        <button className="flex items-center gap-2 px-4 py-2.5 rounded-lg bg-indigo-500 hover:bg-indigo-600 text-white text-sm font-medium transition-colors shadow-md shadow-indigo-500/20">
          <Plus className="w-4 h-4" />
          Yeni Organizasyon
        </button>
      </div>

      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-2">
        {mockOrgs.map((org) => (
          <OrgTreeItem key={org.code} org={org} />
        ))}
      </div>
    </div>
  );
}
