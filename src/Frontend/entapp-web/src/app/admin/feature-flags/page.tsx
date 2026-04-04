"use client";

import { useEffect, useState, useCallback } from "react";
import { apiClient } from "@/lib/api-client";

interface FeatureFlag {
  id: string;
  name: string;
  displayName: string;
  description: string | null;
  isEnabled: boolean;
  tenantId: string | null;
  enabledFrom: string | null;
  enabledUntil: string | null;
  effectivelyEnabled: boolean;
}

export default function FeatureFlagsPage() {
  const [flags, setFlags] = useState<FeatureFlag[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);

  const fetchFlags = useCallback(async () => {
    setLoading(true);
    try {
      const res = await apiClient.get("/api/admin/feature-flags");
      setFlags(res.data);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchFlags(); }, [fetchFlags]);

  const handleToggle = async (id: string) => {
    await apiClient.post(`/api/admin/feature-flags/${id}/toggle`);
    fetchFlags();
  };

  const handleDelete = async (id: string, name: string) => {
    if (!confirm(`"${name}" flag'ini silmek istediğinize emin misiniz?`)) return;
    await apiClient.delete(`/api/admin/feature-flags/${id}`);
    fetchFlags();
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Feature Flags</h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">
            {flags.length} flag tanımlı · {flags.filter(f => f.effectivelyEnabled).length} aktif
          </p>
        </div>
        <button
          onClick={() => setShowCreate(!showCreate)}
          className="px-4 py-2 bg-gradient-to-r from-amber-500 to-orange-500 text-white text-sm font-medium rounded-xl hover:shadow-lg hover:shadow-amber-500/25 transition-all"
        >
          + Yeni Flag
        </button>
      </div>

      {showCreate && (
        <CreateFlagForm onCreated={() => { setShowCreate(false); fetchFlags(); }} onCancel={() => setShowCreate(false)} />
      )}

      {loading ? (
        <div className="flex items-center justify-center h-32">
          <div className="w-6 h-6 border-2 border-amber-400 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {flags.map((f) => (
            <div key={f.id} className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-5 hover:border-amber-500/20 transition-colors">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <h3 className="font-semibold text-[var(--color-text)]">{f.displayName}</h3>
                    <span className={`px-2 py-0.5 rounded-full text-[10px] font-medium border ${
                      f.effectivelyEnabled
                        ? "bg-emerald-500/10 text-emerald-400 border-emerald-500/20"
                        : "bg-slate-500/10 text-slate-400 border-slate-500/20"
                    }`}>
                      {f.effectivelyEnabled ? "Aktif" : "Pasif"}
                    </span>
                  </div>
                  <p className="text-xs font-mono text-[var(--color-text-muted)] mt-0.5">{f.name}</p>
                  {f.description && (
                    <p className="text-sm text-[var(--color-text-secondary)] mt-2">{f.description}</p>
                  )}
                  {(f.enabledFrom || f.enabledUntil) && (
                    <p className="text-xs text-[var(--color-text-muted)] mt-2">
                      📅 {f.enabledFrom ? new Date(f.enabledFrom).toLocaleDateString("tr-TR") : "—"}
                      {" → "}
                      {f.enabledUntil ? new Date(f.enabledUntil).toLocaleDateString("tr-TR") : "—"}
                    </p>
                  )}
                </div>

                {/* Toggle Switch */}
                <button
                  onClick={() => handleToggle(f.id)}
                  className={`relative w-12 h-6 rounded-full transition-colors ${f.isEnabled ? "bg-emerald-500" : "bg-slate-600"}`}
                >
                  <span className={`absolute top-0.5 w-5 h-5 rounded-full bg-white shadow transition-transform ${f.isEnabled ? "translate-x-6" : "translate-x-0.5"}`} />
                </button>
              </div>

              <div className="flex items-center justify-between mt-4 pt-3 border-t border-[var(--color-border)]">
                {f.tenantId ? (
                  <span className="text-xs text-[var(--color-text-muted)]">Tenant-specific</span>
                ) : (
                  <span className="text-xs text-[var(--color-text-muted)]">Global</span>
                )}
                <button onClick={() => handleDelete(f.id, f.name)} className="text-xs text-red-400 hover:text-red-300 transition-colors">
                  Sil
                </button>
              </div>
            </div>
          ))}
          {flags.length === 0 && (
            <div className="col-span-2 text-center py-12 text-[var(--color-text-muted)]">
              Henüz feature flag tanımlanmamış.
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function CreateFlagForm({ onCreated, onCancel }: { onCreated: () => void; onCancel: () => void }) {
  const [form, setForm] = useState({ name: "", displayName: "", description: "", isEnabled: false });
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await apiClient.post("/api/admin/feature-flags", form);
      onCreated();
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="bg-[var(--color-surface)] rounded-2xl border border-amber-500/20 p-5 space-y-4">
      <h3 className="font-semibold text-[var(--color-text)]">Yeni Feature Flag</h3>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <input type="text" placeholder="Flag Adı (unique key)" required value={form.name} onChange={e => setForm({ ...form, name: e.target.value })}
          className="px-3 py-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-input-bg)] text-sm text-[var(--color-text)] focus:outline-none focus:border-amber-500" />
        <input type="text" placeholder="Görünen Ad" required value={form.displayName} onChange={e => setForm({ ...form, displayName: e.target.value })}
          className="px-3 py-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-input-bg)] text-sm text-[var(--color-text)] focus:outline-none focus:border-amber-500" />
        <input type="text" placeholder="Açıklama" value={form.description} onChange={e => setForm({ ...form, description: e.target.value })}
          className="px-3 py-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-input-bg)] text-sm text-[var(--color-text)] focus:outline-none focus:border-amber-500 md:col-span-2" />
      </div>
      <div className="flex items-center gap-4">
        <label className="flex items-center gap-2 text-sm text-[var(--color-text)]">
          <input type="checkbox" checked={form.isEnabled} onChange={e => setForm({ ...form, isEnabled: e.target.checked })} className="rounded" />
          Oluşturulunca aktif olsun
        </label>
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
