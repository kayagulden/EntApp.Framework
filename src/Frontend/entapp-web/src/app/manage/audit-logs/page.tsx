"use client";

import { useState, useEffect } from "react";

interface AuditLogItem {
  id: string;
  action: string;
  entityType: string | null;
  entityId: string | null;
  userId: string | null;
  userName: string | null;
  createdAt: string;
}

interface AuditResponse {
  items: AuditLogItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export default function ManageAuditLogsPage() {
  const [data, setData] = useState<AuditResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);

  useEffect(() => {
    setLoading(true);
    fetch(`/api/manage/audit-logs?page=${page}&pageSize=20`)
      .then((res) =>
        res.ok ? res.json() : { items: [], total: 0, page: 1, pageSize: 20, totalPages: 0 }
      )
      .then(setData)
      .catch(() =>
        setData({ items: [], total: 0, page: 1, pageSize: 20, totalPages: 0 })
      )
      .finally(() => setLoading(false));
  }, [page]);

  const actionColors: Record<string, string> = {
    Create: "bg-emerald-500/10 text-emerald-500",
    Update: "bg-blue-500/10 text-blue-500",
    Delete: "bg-red-500/10 text-red-500",
    Login: "bg-violet-500/10 text-violet-500",
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">
          Audit Logları
        </h1>
        <p className="text-sm text-[var(--color-text-muted)] mt-1">
          Tenant&apos;ınızdaki tüm işlem geçmişini görüntüleyin.
        </p>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12">
          <div className="w-6 h-6 border-2 border-teal-500 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-12 text-center">
          <p className="text-[var(--color-text-muted)]">
            Henüz audit log kaydı bulunmuyor.
          </p>
        </div>
      ) : (
        <>
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
                  <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">
                    Tarih
                  </th>
                  <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">
                    İşlem
                  </th>
                  <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">
                    Entity
                  </th>
                  <th className="text-left px-6 py-3 font-medium text-[var(--color-text-muted)] uppercase tracking-wider text-xs">
                    Kullanıcı
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--color-border)]">
                {data.items.map((log) => (
                  <tr
                    key={log.id}
                    className="hover:bg-[var(--color-surface-hover)] transition-colors"
                  >
                    <td className="px-6 py-3.5 text-[var(--color-text-muted)] text-xs">
                      {new Date(log.createdAt).toLocaleString("tr-TR")}
                    </td>
                    <td className="px-6 py-3.5">
                      <span
                        className={`px-2 py-0.5 rounded text-xs font-medium ${
                          actionColors[log.action] || "bg-slate-500/10 text-slate-400"
                        }`}
                      >
                        {log.action}
                      </span>
                    </td>
                    <td className="px-6 py-3.5 text-[var(--color-text)]">
                      {log.entityType || "—"}
                    </td>
                    <td className="px-6 py-3.5 text-[var(--color-text-secondary)]">
                      {log.userName || log.userId || "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {data.totalPages > 1 && (
            <div className="flex items-center justify-between">
              <span className="text-xs text-[var(--color-text-muted)]">
                Toplam {data.total} kayıt
              </span>
              <div className="flex gap-2">
                <button
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page <= 1}
                  className="px-3 py-1.5 text-xs rounded-lg border border-[var(--color-border)] text-[var(--color-text)] disabled:opacity-40 hover:bg-[var(--color-surface-hover)] transition-colors"
                >
                  ← Önceki
                </button>
                <span className="px-3 py-1.5 text-xs text-[var(--color-text-muted)]">
                  {page} / {data.totalPages}
                </span>
                <button
                  onClick={() => setPage((p) => Math.min(data!.totalPages, p + 1))}
                  disabled={page >= data.totalPages}
                  className="px-3 py-1.5 text-xs rounded-lg border border-[var(--color-border)] text-[var(--color-text)] disabled:opacity-40 hover:bg-[var(--color-surface-hover)] transition-colors"
                >
                  Sonraki →
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
