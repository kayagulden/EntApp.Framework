"use client";

import { useState, useEffect } from "react";

interface UIConfig {
  id: string;
  entityName: string;
  tenantId: string | null;
  configJson: string;
  isGlobal: boolean;
  createdAt: string;
}

export default function ManageUIConfigPage() {
  const [configs, setConfigs] = useState<UIConfig[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch("/api/manage/ui-configs")
      .then((res) => (res.ok ? res.json() : []))
      .then(setConfigs)
      .catch(() => setConfigs([]))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">
          UI Konfigürasyon
        </h1>
        <p className="text-sm text-[var(--color-text-muted)] mt-1">
          Entity ekranlarının görünümünü özelleştirin.
        </p>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12">
          <div className="w-6 h-6 border-2 border-teal-500 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : configs.length === 0 ? (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-12 text-center">
          <p className="text-[var(--color-text-muted)]">
            Henüz UI konfigürasyonu tanımlanmamış.
          </p>
          <p className="text-xs text-[var(--color-text-muted)] mt-2">
            Admin panelden veya API üzerinden yeni konfigürasyon ekleyebilirsiniz.
          </p>
        </div>
      ) : (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
                <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">
                  Entity
                </th>
                <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">
                  Kapsam
                </th>
                <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">
                  Oluşturulma
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {configs.map((c) => (
                <tr
                  key={c.id}
                  className="hover:bg-[var(--color-surface-hover)] transition-colors cursor-pointer"
                >
                  <td className="px-6 py-3.5 font-medium text-[var(--color-text)]">
                    {c.entityName}
                  </td>
                  <td className="px-6 py-3.5">
                    <span
                      className={`px-2 py-0.5 rounded text-xs font-medium ${
                        c.isGlobal
                          ? "bg-slate-500/10 text-slate-400"
                          : "bg-teal-500/10 text-teal-400"
                      }`}
                    >
                      {c.isGlobal ? "Global" : "Tenant"}
                    </span>
                  </td>
                  <td className="px-6 py-3.5 text-[var(--color-text-muted)]">
                    {new Date(c.createdAt).toLocaleDateString("tr-TR")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
