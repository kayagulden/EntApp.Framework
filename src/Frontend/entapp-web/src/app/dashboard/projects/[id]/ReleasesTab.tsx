"use client";
import { useState, useEffect, useCallback } from "react";
import { Plus, Edit3, Save, Trash2, Loader2, ChevronDown, ChevronRight, Rocket, Package, Tag, Calendar, Shield, FileText, X } from "lucide-react";
import { cn } from "@/lib/utils";
import ReleaseDetailPanel from "./ReleaseDetailPanel";

// ── Types ───────────────────────────────────────────────────
export interface ReleaseListItem {
  id: string; key: string; version: string; title: string;
  status: string; type: string;
  plannedDate?: string | null; actualDate?: string | null;
  releaseManagerId?: string | null; targetEnvironment?: string | null;
  sprintId?: string | null; milestoneId?: string | null;
  itemCount: number; tags?: string | null;
  sortOrder: number; createdAt: string;
}

// ── Config ──────────────────────────────────────────────────
const STATUS_CFG: Record<string, { label: string; color: string; icon: string; step: number }> = {
  Planning:   { label: "Planlama",    color: "bg-slate-500/15 text-slate-400 border-slate-500/30",   icon: "📋", step: 0 },
  CodeFreeze: { label: "Code Freeze", color: "bg-blue-500/15 text-blue-400 border-blue-500/30",     icon: "🧊", step: 1 },
  Testing:    { label: "Test",        color: "bg-amber-500/15 text-amber-400 border-amber-500/30",   icon: "🧪", step: 2 },
  GoNoGo:     { label: "Go/No-Go",   color: "bg-purple-500/15 text-purple-400 border-purple-500/30", icon: "⚖️", step: 3 },
  Staging:    { label: "Staging",     color: "bg-cyan-500/15 text-cyan-400 border-cyan-500/30",      icon: "🚀", step: 4 },
  Deployed:   { label: "Deployed",    color: "bg-emerald-500/15 text-emerald-400 border-emerald-500/30", icon: "✅", step: 5 },
  Closed:     { label: "Kapatıldı",  color: "bg-teal-500/15 text-teal-400 border-teal-500/30",      icon: "🏁", step: 6 },
  Cancelled:  { label: "İptal",      color: "bg-gray-500/15 text-gray-400 border-gray-500/30",      icon: "🚫", step: -1 },
  Rollback:   { label: "Rollback",   color: "bg-red-500/15 text-red-400 border-red-500/30",         icon: "⏪", step: -2 },
};

const TYPE_CFG: Record<string, { label: string; color: string }> = {
  Major:    { label: "Major",   color: "bg-red-500/15 text-red-400" },
  Minor:    { label: "Minor",   color: "bg-blue-500/15 text-blue-400" },
  Patch:    { label: "Patch",   color: "bg-green-500/15 text-green-400" },
  Hotfix:   { label: "Hotfix",  color: "bg-orange-500/15 text-orange-400" },
  Rollback: { label: "Rollback", color: "bg-gray-500/15 text-gray-400" },
};

const API = "/api/pm";

// ── Main Component ──────────────────────────────────────────
export default function ReleasesTab({ projectId }: { projectId: string }) {
  const [releases, setReleases] = useState<ReleaseListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState("");
  const [typeFilter, setTypeFilter] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [form, setForm] = useState({
    version: "", title: "", type: "Minor", description: "",
    plannedDate: "", codeFreezeDate: "", targetEnvironment: "", tags: "",
  });

  const fetchReleases = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (statusFilter) params.set("status", statusFilter);
      if (typeFilter) params.set("type", typeFilter);
      const qs = params.toString() ? `?${params}` : "";
      const res = await fetch(`${API}/projects/${projectId}/releases${qs}`);
      if (res.ok) setReleases(await res.json());
    } catch { /* */ } finally { setLoading(false); }
  }, [projectId, statusFilter, typeFilter]);

  useEffect(() => { fetchReleases(); }, [fetchReleases]);

  const resetForm = () => {
    setShowForm(false); setEditingId(null);
    setForm({ version: "", title: "", type: "Minor", description: "", plannedDate: "", codeFreezeDate: "", targetEnvironment: "", tags: "" });
  };

  const handleSave = async () => {
    if (!form.version.trim() || !form.title.trim()) return;
    setSaving(true);
    try {
      const body: Record<string, unknown> = {
        version: form.version, title: form.title, type: form.type,
        description: form.description || null,
        plannedDate: form.plannedDate || null, codeFreezeDate: form.codeFreezeDate || null,
        targetEnvironment: form.targetEnvironment || null, tags: form.tags || null,
      };
      if (editingId) {
        await fetch(`${API}/releases/${editingId}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      } else {
        await fetch(`${API}/projects/${projectId}/releases`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      }
      resetForm(); await fetchReleases();
    } catch { /* */ } finally { setSaving(false); }
  };

  const handleDelete = async (id: string) => {
    if (!confirm("Bu release silinecek. Emin misiniz?")) return;
    await fetch(`${API}/releases/${id}`, { method: "DELETE" });
    if (selectedId === id) setSelectedId(null);
    await fetchReleases();
  };

  const startEdit = async (id: string) => {
    const res = await fetch(`${API}/releases/${id}`);
    if (!res.ok) return;
    const d = await res.json();
    setEditingId(id);
    setForm({
      version: d.version, title: d.title, type: d.type,
      description: d.description || "", plannedDate: d.plannedDate || "",
      codeFreezeDate: d.codeFreezeDate || "", targetEnvironment: d.targetEnvironment || "", tags: d.tags || "",
    });
    setShowForm(true);
  };

  const fmtDate = (d?: string | null) => d ? new Date(d).toLocaleDateString("tr-TR", { day: "2-digit", month: "short", year: "numeric" }) : "—";

  // ── Pipeline Steps ────────────────────────────────────────
  const PIPELINE_STEPS = ["Planning", "CodeFreeze", "Testing", "GoNoGo", "Staging", "Deployed", "Closed"];
  const renderPipeline = (status: string) => {
    const cfg = STATUS_CFG[status];
    const currentStep = cfg?.step ?? -1;
    if (currentStep < 0) return <span className={cn("px-2 py-0.5 rounded-full text-[9px] font-medium border", cfg?.color)}>{cfg?.icon} {cfg?.label}</span>;
    return (
      <div className="flex items-center gap-0.5">
        {PIPELINE_STEPS.map((s, i) => {
          const sc = STATUS_CFG[s];
          const done = i < currentStep; const active = i === currentStep;
          return (
            <div key={s} className="flex items-center gap-0.5">
              <div className={cn("w-5 h-5 rounded-full flex items-center justify-center text-[8px] border transition-all",
                active ? "border-2 border-indigo-500 bg-indigo-500/20 text-indigo-300 scale-110" :
                done ? "border-emerald-500/50 bg-emerald-500/15 text-emerald-400" :
                "border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text-muted)] opacity-40"
              )} title={sc.label}>{done ? "✓" : sc.icon}</div>
              {i < PIPELINE_STEPS.length - 1 && <div className={cn("w-3 h-px", done ? "bg-emerald-500/40" : "bg-[var(--color-border)]")} />}
            </div>
          );
        })}
      </div>
    );
  };

  return (
    <div className="flex gap-4 h-[calc(100vh-280px)]">
      {/* Left: Release List */}
      <div className={cn("flex flex-col min-w-0 transition-all", selectedId ? "flex-1" : "w-full")}>
        {/* Toolbar */}
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <h3 className="text-sm font-semibold text-[var(--color-text)] flex items-center gap-2">
              <Rocket className="w-4 h-4 text-indigo-400" /> Releases
              <span className="text-xs font-normal text-[var(--color-text-muted)]">({releases.length})</span>
            </h3>
            <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
              className="text-[10px] rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-2 py-1">
              <option value="">Tüm Durumlar</option>
              {Object.entries(STATUS_CFG).map(([k, v]) => <option key={k} value={k}>{v.icon} {v.label}</option>)}
            </select>
            <select value={typeFilter} onChange={e => setTypeFilter(e.target.value)}
              className="text-[10px] rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-2 py-1">
              <option value="">Tüm Tipler</option>
              {Object.entries(TYPE_CFG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
            </select>
          </div>
          <button onClick={() => { resetForm(); setShowForm(true); }}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-medium transition-colors shadow-lg shadow-indigo-500/20">
            <Plus className="w-3.5 h-3.5" /> Yeni Release
          </button>
        </div>

        {/* Create/Edit Form */}
        {showForm && (
          <div className="mb-4 rounded-xl border border-indigo-500/30 bg-indigo-500/5 p-4 space-y-3">
            <div className="flex items-center justify-between">
              <h4 className="text-sm font-semibold text-[var(--color-text)]">{editingId ? "Release Düzenle" : "Yeni Release"}</h4>
              <button onClick={resetForm} className="p-1 rounded hover:bg-[var(--color-border)]"><X className="w-4 h-4 text-[var(--color-text-muted)]" /></button>
            </div>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              <input placeholder="Versiyon * (v1.0.0)" value={form.version} onChange={e => setForm(f => ({ ...f, version: e.target.value }))}
                className="px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:ring-2 focus:ring-indigo-500/40 focus:outline-none" />
              <input placeholder="Başlık *" value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
                className="px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:ring-2 focus:ring-indigo-500/40 focus:outline-none" />
              <select value={form.type} onChange={e => setForm(f => ({ ...f, type: e.target.value }))}
                className="px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                {Object.entries(TYPE_CFG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
              </select>
              <input placeholder="Hedef Ortam" value={form.targetEnvironment} onChange={e => setForm(f => ({ ...f, targetEnvironment: e.target.value }))}
                className="px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:ring-2 focus:ring-indigo-500/40 focus:outline-none" />
            </div>
            <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
              <div><label className="block text-[10px] text-[var(--color-text-muted)] mb-1">Planlanan Tarih</label>
                <input type="date" value={form.plannedDate} onChange={e => setForm(f => ({ ...f, plannedDate: e.target.value }))}
                  className="w-full px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
              <div><label className="block text-[10px] text-[var(--color-text-muted)] mb-1">Code Freeze</label>
                <input type="date" value={form.codeFreezeDate} onChange={e => setForm(f => ({ ...f, codeFreezeDate: e.target.value }))}
                  className="w-full px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
              <div><label className="block text-[10px] text-[var(--color-text-muted)] mb-1">Etiketler</label>
                <input placeholder="backend, frontend" value={form.tags} onChange={e => setForm(f => ({ ...f, tags: e.target.value }))}
                  className="w-full px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
            </div>
            <textarea placeholder="Açıklama (Markdown)" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
              rows={2} className="w-full px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] resize-none" />
            <div className="flex gap-2">
              <button onClick={handleSave} disabled={saving || !form.version.trim() || !form.title.trim()}
                className="flex items-center gap-1.5 px-4 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white text-xs font-medium">
                {saving ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Save className="w-3.5 h-3.5" />} {editingId ? "Güncelle" : "Oluştur"}
              </button>
              <button onClick={resetForm} className="px-4 py-2 rounded-lg text-xs border border-[var(--color-border)] text-[var(--color-text-muted)]">İptal</button>
            </div>
          </div>
        )}

        {/* Release List */}
        <div className="flex-1 overflow-y-auto space-y-2">
          {loading ? (
            <div className="flex items-center justify-center py-16"><Loader2 className="w-6 h-6 text-indigo-400 animate-spin" /></div>
          ) : releases.length === 0 ? (
            <div className="text-center py-16 text-[var(--color-text-muted)]">
              <Rocket className="w-12 h-12 mx-auto opacity-20 mb-3" />
              <p className="text-sm">Henüz release oluşturulmamış</p>
              <p className="text-xs mt-1">Yukarıdaki &quot;Yeni Release&quot; butonuyla başlayın</p>
            </div>
          ) : releases.map(r => {
            const sc = STATUS_CFG[r.status] || STATUS_CFG.Planning;
            const tc = TYPE_CFG[r.type] || TYPE_CFG.Minor;
            const isSelected = selectedId === r.id;
            return (
              <div key={r.id} onClick={() => setSelectedId(isSelected ? null : r.id)}
                className={cn("rounded-xl border p-4 cursor-pointer transition-all hover:shadow-lg group",
                  isSelected ? "border-indigo-500/40 bg-indigo-500/5 shadow-lg shadow-indigo-500/10" : "border-[var(--color-border)] bg-[var(--color-card-bg)] hover:border-indigo-500/20")}>
                <div className="flex items-start justify-between gap-3">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1.5 flex-wrap">
                      <span className="text-xs font-mono font-bold text-indigo-400">{r.key}</span>
                      <span className={cn("px-2 py-0.5 rounded-full text-[9px] font-semibold border", tc.color)}>{tc.label}</span>
                      <span className="text-sm font-semibold text-[var(--color-text)] truncate">{r.version}</span>
                      <span className="text-xs text-[var(--color-text-muted)] truncate">— {r.title}</span>
                    </div>
                    {/* Pipeline */}
                    <div className="mb-2">{renderPipeline(r.status)}</div>
                    <div className="flex items-center gap-4 text-[10px] text-[var(--color-text-muted)] flex-wrap">
                      <span className="flex items-center gap-1"><Calendar className="w-3 h-3" /> {fmtDate(r.plannedDate)}</span>
                      <span className="flex items-center gap-1"><Package className="w-3 h-3" /> {r.itemCount} iş kalemi</span>
                      {r.targetEnvironment && <span className="flex items-center gap-1"><Shield className="w-3 h-3" /> {r.targetEnvironment}</span>}
                      {r.tags && <span className="flex items-center gap-1"><Tag className="w-3 h-3" /> {r.tags}</span>}
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity" onClick={e => e.stopPropagation()}>
                    <button onClick={() => startEdit(r.id)} className="p-1.5 rounded-lg hover:bg-[var(--color-border)] text-[var(--color-text-muted)] hover:text-[var(--color-text)]"><Edit3 className="w-3.5 h-3.5" /></button>
                    <button onClick={() => handleDelete(r.id)} className="p-1.5 rounded-lg hover:bg-red-500/10 text-[var(--color-text-muted)] hover:text-red-400"><Trash2 className="w-3.5 h-3.5" /></button>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Right: Detail Panel */}
      {selectedId && (
        <div className="w-[480px] shrink-0 border-l border-[var(--color-border)] pl-4 overflow-y-auto">
          <ReleaseDetailPanel releaseId={selectedId} onClose={() => setSelectedId(null)} onRefreshList={fetchReleases} />
        </div>
      )}
    </div>
  );
}
