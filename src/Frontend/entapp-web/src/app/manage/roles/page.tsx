"use client";

import { Shield, Plus } from "lucide-react";
import { cn } from "@/lib/utils";

const mockRoles = [
  { name: "Admin", displayName: "Sistem Yöneticisi", description: "Tüm yetkilere sahip", isSystem: true, permissions: 6, users: 2 },
  { name: "Manager", displayName: "Yönetici", description: "Departman/modül yönetimi", isSystem: true, permissions: 4, users: 5 },
  { name: "User", displayName: "Kullanıcı", description: "Standart kullanıcı erişimi", isSystem: true, permissions: 2, users: 120 },
  { name: "ReadOnly", displayName: "Salt Okunur", description: "Yalnızca okuma yetkisi", isSystem: true, permissions: 1, users: 30 },
];

export default function RolesPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Roller</h1>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">Sistem rollerini ve yetkilerini yönetin.</p>
        </div>
        <button className="flex items-center gap-2 px-4 py-2.5 rounded-lg bg-indigo-500 hover:bg-indigo-600 text-white text-sm font-medium transition-colors shadow-md shadow-indigo-500/20">
          <Plus className="w-4 h-4" />
          Yeni Rol
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {mockRoles.map((role) => (
          <div
            key={role.name}
            className={cn(
              "rounded-xl p-5 border border-[var(--color-border)]",
              "bg-[var(--color-surface)] hover:border-[var(--color-border-hover)]",
              "hover:shadow-lg transition-all duration-300 cursor-pointer group"
            )}
          >
            <div className="flex items-start justify-between">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-violet-500 to-purple-600 flex items-center justify-center shadow-lg">
                  <Shield className="w-5 h-5 text-white" />
                </div>
                <div>
                  <h3 className="font-semibold text-[var(--color-text)]">{role.displayName}</h3>
                  <p className="text-xs text-[var(--color-text-muted)] mt-0.5">{role.description}</p>
                </div>
              </div>
              {role.isSystem && (
                <span className="px-2 py-0.5 rounded text-[10px] font-semibold uppercase tracking-wider bg-amber-500/10 text-amber-500">
                  Sistem
                </span>
              )}
            </div>
            <div className="mt-4 flex items-center gap-4 text-xs text-[var(--color-text-muted)]">
              <span>{role.permissions} yetki</span>
              <span>•</span>
              <span>{role.users} kullanıcı</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
