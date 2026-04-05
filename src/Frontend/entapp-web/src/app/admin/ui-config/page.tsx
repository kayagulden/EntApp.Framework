"use client";

import { useState, useEffect } from "react";

interface UIConfig {
  id: string;
  entityName: string;
  tenantId: string | null;
  configJson: string;
  createdAt: string;
}

export default function AdminUIConfigPage() {
  const [configs, setConfigs] = useState<UIConfig[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch("/api/admin/ui-configs")
      .then((res) => (res.ok ? res.json() : []))
      .then(setConfigs)
      .catch(() => setConfigs([]))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">
            UI Konfigürasyon
          </h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">
            Tüm entity ekranları için global UI override&apos;ları yönetin.
          </p>
        </div>
        <button className="flex items-center gap-2 px-4 py-2.5 rounded-lg bg-amber-500 hover:bg-amber-600 text-white text-sm font-medium transition-colors shadow-md shadow-amber-500/20">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          Yeni Override
        </button>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12">
          <div className="w-6 h-6 border-2 border-amber-500 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : configs.length === 0 ? (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-12 text-center">
          <p className="text-[var(--color-text-muted)]">
            Henüz UI konfigürasyonu tanımlanmamış.
          </p>
          <p className="text-xs text-[var(--color-text-muted)] mt-2">
            &quot;Yeni Override&quot; butonu ile entity ekranlarını özelleştirebilirsiniz.
          </p>
        </div>
      ) : (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
                <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">Entity</th>
                <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">Tenant</th>
                <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">Oluşturulma</th>
                <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">Config</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {configs.map((c) => (
                <tr key={c.id} className="hover:bg-[var(--color-surface-hover)] transition-colors cursor-pointer">
                  <td className="px-6 py-3.5 font-medium text-[var(--color-text)]">{c.entityName}</td>
                  <td className="px-6 py-3.5">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                      c.tenantId ? "bg-teal-500/10 text-teal-400" : "bg-amber-500/10 text-amber-400"
                    }`}>
                      {c.tenantId ? "Tenant" : "Global"}
                    </span>
                  </td>
                  <td className="px-6 py-3.5 text-[var(--color-text-muted)]">
                    {new Date(c.createdAt).toLocaleDateString("tr-TR")}
                  </td>
                  <td className="px-6 py-3.5">
                    <code className="text-xs bg-[var(--color-bg-secondary)] px-2 py-1 rounded text-[var(--color-text-muted)]">
                      {c.configJson.length > 50 ? c.configJson.slice(0, 50) + "..." : c.configJson}
                    </code>
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
