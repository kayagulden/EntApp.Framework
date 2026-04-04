"use client";

import { useEffect, useState, useCallback } from "react";
import { apiClient } from "@/lib/api-client";

interface Tenant {
  id: string;
  name: string;
  identifier: string;
  displayName: string | null;
  status: string;
  plan: string;
  subdomain: string | null;
  adminEmail: string | null;
  activatedAt: string | null;
  settingCount: number;
}

export default function TenantsPage() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);

  const fetchTenants = useCallback(async () => {
    setLoading(true);
    try {
      const res = await apiClient.get(`/api/admin/tenants?page=${page}&pageSize=10`);
      setTenants(res.data.items);
      setTotalCount(res.data.totalCount);
    } finally {
      setLoading(false);
    }
  }, [page]);

  useEffect(() => { fetchTenants(); }, [fetchTenants]);

  const handleAction = async (id: string, action: string) => {
    await apiClient.post(`/api/admin/tenants/${id}/${action}`);
    fetchTenants();
  };

  const statusColor: Record<string, string> = {
    Active: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
    Suspended: "bg-amber-500/10 text-amber-400 border-amber-500/20",
    PendingSetup: "bg-blue-500/10 text-blue-400 border-blue-500/20",
    Deactivated: "bg-red-500/10 text-red-400 border-red-500/20",
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Tenant Yönetimi</h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">{totalCount} tenant kayıtlı</p>
        </div>
        <button
          onClick={() => setShowCreate(!showCreate)}
          className="px-4 py-2 bg-gradient-to-r from-amber-500 to-orange-500 text-white text-sm font-medium rounded-xl hover:shadow-lg hover:shadow-amber-500/25 transition-all"
        >
          + Yeni Tenant
        </button>
      </div>

      {/* ── Create Form ──────────────── */}
      {showCreate && (
        <CreateTenantForm
          onCreated={() => { setShowCreate(false); fetchTenants(); }}
          onCancel={() => setShowCreate(false)}
        />
      )}

      {/* ── Table ────────────────────── */}
      <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center h-32">
            <div className="w-6 h-6 border-2 border-amber-400 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
                <th className="text-left px-4 py-3 font-medium text-[var(--color-text-muted)]">Tenant</th>
                <th className="text-left px-4 py-3 font-medium text-[var(--color-text-muted)]">Identifier</th>
                <th className="text-left px-4 py-3 font-medium text-[var(--color-text-muted)]">Plan</th>
                <th className="text-left px-4 py-3 font-medium text-[var(--color-text-muted)]">Durum</th>
                <th className="text-left px-4 py-3 font-medium text-[var(--color-text-muted)]">Ayarlar</th>
                <th className="text-right px-4 py-3 font-medium text-[var(--color-text-muted)]">İşlem</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {tenants.map((t) => (
                <tr key={t.id} className="hover:bg-[var(--color-bg-secondary)] transition-colors">
                  <td className="px-4 py-3">
                    <div>
                      <p className="font-medium text-[var(--color-text)]">{t.name}</p>
                      {t.adminEmail && <p className="text-xs text-[var(--color-text-muted)]">{t.adminEmail}</p>}
                    </div>
                  </td>
                  <td className="px-4 py-3 font-mono text-[var(--color-text-secondary)]">{t.identifier}</td>
                  <td className="px-4 py-3">
                    <span className="px-2 py-0.5 rounded-md bg-[var(--color-bg-tertiary)] text-xs font-medium text-[var(--color-text)]">{t.plan}</span>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`px-2.5 py-0.5 rounded-full text-xs font-medium border ${statusColor[t.status] || ""}`}>
                      {t.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-[var(--color-text-muted)]">{t.settingCount}</td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex items-center justify-end gap-1">
                      {t.status !== "Active" && (
                        <ActionBtn label="Aktifleştir" onClick={() => handleAction(t.id, "activate")} color="emerald" />
                      )}
                      {t.status === "Active" && (
                        <ActionBtn label="Askıya Al" onClick={() => handleAction(t.id, "suspend")} color="amber" />
                      )}
                      {t.status !== "Deactivated" && (
                        <ActionBtn label="Deaktif" onClick={() => handleAction(t.id, "deactivate")} color="red" />
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {tenants.length === 0 && (
                <tr>
                  <td colSpan={6} className="text-center py-8 text-[var(--color-text-muted)]">
                    Henüz tenant bulunmuyor.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>

      {/* ── Pagination ───────────────── */}
      {totalCount > 10 && (
        <div className="flex items-center justify-center gap-2">
          <button disabled={page <= 1} onClick={() => setPage(p => p - 1)}
            className="px-3 py-1.5 text-sm rounded-lg border border-[var(--color-border)] disabled:opacity-40 hover:bg-[var(--color-bg-secondary)] transition-colors text-[var(--color-text)]">
            ← Önceki
          </button>
          <span className="text-sm text-[var(--color-text-muted)]">Sayfa {page}</span>
          <button disabled={page * 10 >= totalCount} onClick={() => setPage(p => p + 1)}
            className="px-3 py-1.5 text-sm rounded-lg border border-[var(--color-border)] disabled:opacity-40 hover:bg-[var(--color-bg-secondary)] transition-colors text-[var(--color-text)]">
            Sonraki →
          </button>
        </div>
      )}
    </div>
  );
}

function ActionBtn({ label, onClick, color }: { label: string; onClick: () => void; color: string }) {
  const colors: Record<string, string> = {
    emerald: "text-emerald-400 hover:bg-emerald-500/10",
    amber: "text-amber-400 hover:bg-amber-500/10",
    red: "text-red-400 hover:bg-red-500/10",
  };
  return (
    <button onClick={onClick} className={`px-2 py-1 text-xs rounded-lg transition-colors ${colors[color]}`}>
      {label}
    </button>
  );
}

function CreateTenantForm({ onCreated, onCancel }: { onCreated: () => void; onCancel: () => void }) {
  const [form, setForm] = useState({ name: "", identifier: "", adminEmail: "", plan: "Free" });
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await apiClient.post("/api/admin/tenants", form);
      onCreated();
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="bg-[var(--color-surface)] rounded-2xl border border-amber-500/20 p-5 space-y-4">
      <h3 className="font-semibold text-[var(--color-text)]">Yeni Tenant Oluştur</h3>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <input type="text" placeholder="Tenant Adı" required value={form.name} onChange={e => setForm({ ...form, name: e.target.value })}
          className="px-3 py-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-input-bg)] text-sm text-[var(--color-text)] focus:outline-none focus:border-amber-500" />
        <input type="text" placeholder="Tanımlayıcı (slug)" required value={form.identifier} onChange={e => setForm({ ...form, identifier: e.target.value })}
          className="px-3 py-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-input-bg)] text-sm text-[var(--color-text)] focus:outline-none focus:border-amber-500" />
        <input type="email" placeholder="Admin Email" value={form.adminEmail} onChange={e => setForm({ ...form, adminEmail: e.target.value })}
          className="px-3 py-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-input-bg)] text-sm text-[var(--color-text)] focus:outline-none focus:border-amber-500" />
        <select value={form.plan} onChange={e => setForm({ ...form, plan: e.target.value })}
          className="px-3 py-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-input-bg)] text-sm text-[var(--color-text)] focus:outline-none focus:border-amber-500">
          <option value="Free">Free</option>
          <option value="Starter">Starter</option>
          <option value="Professional">Professional</option>
          <option value="Enterprise">Enterprise</option>
        </select>
      </div>
      <div className="flex items-center gap-2 pt-2">
        <button type="submit" disabled={saving}
          className="px-4 py-2 bg-gradient-to-r from-amber-500 to-orange-500 text-white text-sm font-medium rounded-xl hover:shadow-lg transition-all disabled:opacity-50">
          {saving ? "Kaydediliyor..." : "Oluştur"}
        </button>
        <button type="button" onClick={onCancel} className="px-4 py-2 text-sm text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-colors">
          İptal
        </button>
      </div>
    </form>
  );
}
