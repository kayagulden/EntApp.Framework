"use client";

import { useEffect, useState, useCallback } from "react";
import { apiClient } from "@/lib/api-client";

interface AuditLogEntry {
  id: string;
  userId: string | null;
  userName: string | null;
  action: string;
  entityType: string | null;
  entityId: string | null;
  description: string | null;
  ipAddress: string | null;
  timestamp: string;
}

interface AuditStats {
  totalLogs: number;
  byAction: { action: string; count: number }[];
  byEntity: { entityType: string; count: number }[];
  topUsers: { userName: string; count: number }[];
  recentLogins: { result: string; count: number }[];
}

export default function AuditLogsPage() {
  const [logs, setLogs] = useState<AuditLogEntry[]>([]);
  const [stats, setStats] = useState<AuditStats | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState({ action: "", entityType: "" });
  const [showStats, setShowStats] = useState(true);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: "15" });
      if (filters.action) params.set("action", filters.action);
      if (filters.entityType) params.set("entityType", filters.entityType);

      const [logsRes, statsRes] = await Promise.allSettled([
        apiClient.get(`/api/admin/audit-logs?${params}`),
        showStats ? apiClient.get("/api/admin/audit-logs/stats?days=7") : Promise.resolve(null),
      ]);
      if (logsRes.status === "fulfilled") {
        setLogs(logsRes.value.data.items);
        setTotalCount(logsRes.value.data.totalCount);
      }
      if (statsRes.status === "fulfilled" && statsRes.value) setStats(statsRes.value.data);
    } finally {
      setLoading(false);
    }
  }, [page, filters, showStats]);

  useEffect(() => { fetchLogs(); }, [fetchLogs]);

  const actionColors: Record<string, string> = {
    Create: "text-emerald-400 bg-emerald-500/10",
    Update: "text-blue-400 bg-blue-500/10",
    Delete: "text-red-400 bg-red-500/10",
    Login: "text-indigo-400 bg-indigo-500/10",
    Logout: "text-slate-400 bg-slate-500/10",
    Export: "text-amber-400 bg-amber-500/10",
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Audit Logs</h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">{totalCount} kayıt</p>
        </div>
        <button
          onClick={() => setShowStats(!showStats)}
          className={`px-3 py-1.5 text-sm rounded-xl border transition-colors ${
            showStats ? "border-amber-500/30 text-amber-400 bg-amber-500/5" : "border-[var(--color-border)] text-[var(--color-text-muted)]"
          }`}
        >
          📊 İstatistikler
        </button>
      </div>

      {/* ── Stats Cards ──────────────── */}
      {showStats && stats && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-4">
            <h3 className="text-sm font-medium text-[var(--color-text-muted)] mb-3">Aksiyon Bazlı (7 gün)</h3>
            <div className="space-y-2">
              {stats.byAction.map((a) => (
                <div key={a.action} className="flex items-center justify-between">
                  <span className={`text-xs px-2 py-0.5 rounded-md ${actionColors[a.action] || "text-slate-400 bg-slate-500/10"}`}>
                    {a.action}
                  </span>
                  <span className="text-sm font-medium text-[var(--color-text)]">{a.count}</span>
                </div>
              ))}
            </div>
          </div>
          <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-4">
            <h3 className="text-sm font-medium text-[var(--color-text-muted)] mb-3">Entity Bazlı (Top 10)</h3>
            <div className="space-y-2">
              {stats.byEntity.slice(0, 5).map((e) => (
                <div key={e.entityType} className="flex items-center justify-between">
                  <span className="text-xs text-[var(--color-text-secondary)]">{e.entityType}</span>
                  <span className="text-sm font-medium text-[var(--color-text)]">{e.count}</span>
                </div>
              ))}
            </div>
          </div>
          <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-4">
            <h3 className="text-sm font-medium text-[var(--color-text-muted)] mb-3">En Aktif Kullanıcılar</h3>
            <div className="space-y-2">
              {stats.topUsers.slice(0, 5).map((u) => (
                <div key={u.userName} className="flex items-center justify-between">
                  <span className="text-xs text-[var(--color-text-secondary)]">{u.userName}</span>
                  <span className="text-sm font-medium text-[var(--color-text)]">{u.count}</span>
                </div>
              ))}
              {stats.topUsers.length === 0 && (
                <p className="text-xs text-[var(--color-text-muted)]">Henüz veri yok</p>
              )}
            </div>
          </div>
        </div>
      )}

      {/* ── Filters ──────────────────── */}
      <div className="flex items-center gap-3">
        <select value={filters.action} onChange={e => { setFilters({ ...filters, action: e.target.value }); setPage(1); }}
          className="px-3 py-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-input-bg)] text-sm text-[var(--color-text)] focus:outline-none focus:border-amber-500">
          <option value="">Tüm Aksiyonlar</option>
          <option value="Create">Create</option>
          <option value="Update">Update</option>
          <option value="Delete">Delete</option>
          <option value="Login">Login</option>
          <option value="Export">Export</option>
        </select>
        <input type="text" placeholder="Entity Type filtrele..." value={filters.entityType}
          onChange={e => { setFilters({ ...filters, entityType: e.target.value }); setPage(1); }}
          className="px-3 py-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-input-bg)] text-sm text-[var(--color-text)] focus:outline-none focus:border-amber-500 w-48" />
      </div>

      {/* ── Log Table ────────────────── */}
      <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center h-32">
            <div className="w-6 h-6 border-2 border-amber-400 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
                <th className="text-left px-4 py-3 font-medium text-[var(--color-text-muted)]">Zaman</th>
                <th className="text-left px-4 py-3 font-medium text-[var(--color-text-muted)]">Kullanıcı</th>
                <th className="text-left px-4 py-3 font-medium text-[var(--color-text-muted)]">Aksiyon</th>
                <th className="text-left px-4 py-3 font-medium text-[var(--color-text-muted)]">Entity</th>
                <th className="text-left px-4 py-3 font-medium text-[var(--color-text-muted)]">Açıklama</th>
                <th className="text-left px-4 py-3 font-medium text-[var(--color-text-muted)]">IP</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {logs.map((log) => (
                <tr key={log.id} className="hover:bg-[var(--color-bg-secondary)] transition-colors">
                  <td className="px-4 py-3 text-xs text-[var(--color-text-muted)] whitespace-nowrap">
                    {new Date(log.timestamp).toLocaleString("tr-TR")}
                  </td>
                  <td className="px-4 py-3 text-[var(--color-text)]">{log.userName || "—"}</td>
                  <td className="px-4 py-3">
                    <span className={`text-xs px-2 py-0.5 rounded-md ${actionColors[log.action] || ""}`}>
                      {log.action}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    {log.entityType && (
                      <span className="text-xs font-mono text-[var(--color-text-secondary)]">
                        {log.entityType}{log.entityId ? ` #${log.entityId.substring(0, 8)}` : ""}
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-xs text-[var(--color-text-secondary)] max-w-[200px] truncate">
                    {log.description || "—"}
                  </td>
                  <td className="px-4 py-3 text-xs font-mono text-[var(--color-text-muted)]">{log.ipAddress || "—"}</td>
                </tr>
              ))}
              {logs.length === 0 && (
                <tr>
                  <td colSpan={6} className="text-center py-8 text-[var(--color-text-muted)]">
                    Kayıt bulunamadı.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>

      {totalCount > 15 && (
        <div className="flex items-center justify-center gap-2">
          <button disabled={page <= 1} onClick={() => setPage(p => p - 1)}
            className="px-3 py-1.5 text-sm rounded-lg border border-[var(--color-border)] disabled:opacity-40 hover:bg-[var(--color-bg-secondary)] transition-colors text-[var(--color-text)]">
            ← Önceki
          </button>
          <span className="text-sm text-[var(--color-text-muted)]">Sayfa {page}</span>
          <button disabled={page * 15 >= totalCount} onClick={() => setPage(p => p + 1)}
            className="px-3 py-1.5 text-sm rounded-lg border border-[var(--color-border)] disabled:opacity-40 hover:bg-[var(--color-bg-secondary)] transition-colors text-[var(--color-text)]">
            Sonraki →
          </button>
        </div>
      )}
    </div>
  );
}
