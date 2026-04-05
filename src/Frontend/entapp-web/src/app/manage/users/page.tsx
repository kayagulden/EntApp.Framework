"use client";

import { Users, Plus, Search } from "lucide-react";
import { cn } from "@/lib/utils";

const mockUsers = [
  { id: 1, name: "Ahmet Yılmaz", email: "ahmet.yilmaz@company.com", role: "Admin", status: "Active" },
  { id: 2, name: "Mehmet Demir", email: "mehmet.demir@company.com", role: "Manager", status: "Active" },
  { id: 3, name: "Ayşe Kaya", email: "ayse.kaya@company.com", role: "User", status: "Active" },
  { id: 4, name: "Fatma Çelik", email: "fatma.celik@company.com", role: "User", status: "Inactive" },
  { id: 5, name: "Ali Öztürk", email: "ali.ozturk@company.com", role: "ReadOnly", status: "Active" },
];

export default function UsersPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Kullanıcılar</h1>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">Sistem kullanıcılarını yönetin.</p>
        </div>
        <button className="flex items-center gap-2 px-4 py-2.5 rounded-lg bg-indigo-500 hover:bg-indigo-600 text-white text-sm font-medium transition-colors shadow-md shadow-indigo-500/20">
          <Plus className="w-4 h-4" />
          Yeni Kullanıcı
        </button>
      </div>

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
        <input
          type="text"
          placeholder="Kullanıcı ara..."
          className="w-full pl-10 pr-4 py-2.5 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-500 transition-all"
        />
      </div>

      {/* Table */}
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
              <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">Ad Soyad</th>
              <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">E-posta</th>
              <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">Rol</th>
              <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">Durum</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[var(--color-border)]">
            {mockUsers.map((user) => (
              <tr key={user.id} className="hover:bg-[var(--color-surface-hover)] transition-colors cursor-pointer">
                <td className="px-6 py-3.5">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-white text-xs font-bold">
                      {user.name.split(" ").map(n => n[0]).join("")}
                    </div>
                    <span className="font-medium text-[var(--color-text)]">{user.name}</span>
                  </div>
                </td>
                <td className="px-6 py-3.5 text-[var(--color-text-secondary)]">{user.email}</td>
                <td className="px-6 py-3.5">
                  <span className="px-2.5 py-1 rounded-full text-xs font-medium bg-indigo-500/10 text-indigo-500">{user.role}</span>
                </td>
                <td className="px-6 py-3.5">
                  <span className={cn(
                    "px-2.5 py-1 rounded-full text-xs font-medium",
                    user.status === "Active" ? "bg-emerald-500/10 text-emerald-500" : "bg-red-500/10 text-red-500"
                  )}>
                    {user.status === "Active" ? "Aktif" : "Pasif"}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
