"use client";
import { useState, useEffect, useCallback } from "react";
import { Plus, Edit3, Save, Trash2, Loader2, GripVertical, Layout } from "lucide-react";
import { cn } from "@/lib/utils";

interface BoardColumnData { id: string; name: string; order: number; mappedStatus: string; wipLimit?: number | null; }

const STATUS_OPTIONS = [
  { value: "Backlog", label: "Backlog" }, { value: "Todo", label: "Yapılacak" },
  { value: "InProgress", label: "İşlemde" }, { value: "InReview", label: "İnceleme" },
  { value: "Done", label: "Tamamlandı" }, { value: "Cancelled", label: "İptal" },
];

export default function BoardColumnSettings({ projectId }: { projectId: string }) {
  const [boardColumns, setBoardColumns] = useState<BoardColumnData[]>([]);
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editingCol, setEditingCol] = useState<BoardColumnData | null>(null);
  const [form, setForm] = useState({ name: "", mappedStatus: "Backlog", wipLimit: "" });
  const [saving, setSaving] = useState(false);

  const fetchCols = useCallback(async () => {
    setLoading(true);
    try { const res = await fetch(`/api/pm/projects/${projectId}/board-columns`); if (res.ok) setBoardColumns(await res.json()); }
    catch { /* */ } finally { setLoading(false); }
  }, [projectId]);

  useEffect(() => { fetchCols(); }, [fetchCols]);

  const handleSave = async () => {
    if (!form.name.trim()) return;
    setSaving(true);
    try {
      const body = { name: form.name, mappedStatus: form.mappedStatus, wipLimit: form.wipLimit ? parseInt(form.wipLimit) : null };
      if (editingCol) await fetch(`/api/pm/board-columns/${editingCol.id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      else await fetch(`/api/pm/projects/${projectId}/board-columns`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ ...body, order: boardColumns.length }) });
      setShowForm(false); setEditingCol(null); setForm({ name: "", mappedStatus: "Backlog", wipLimit: "" }); fetchCols();
    } catch { /* */ } finally { setSaving(false); }
  };

  const handleDelete = async (colId: string) => {
    if (!confirm("Bu kolonu silmek istediğinize emin misiniz?")) return;
    await fetch(`/api/pm/board-columns/${colId}`, { method: "DELETE" }); fetchCols();
  };

  const startEdit = (col: BoardColumnData) => {
    setEditingCol(col); setForm({ name: col.name, mappedStatus: col.mappedStatus, wipLimit: col.wipLimit ? String(col.wipLimit) : "" }); setShowForm(true);
  };

  const sorted = [...boardColumns].sort((a, b) => a.order - b.order);
  const statusCfg: Record<string, string> = { Backlog: "bg-slate-500/15 text-slate-400", Todo: "bg-blue-500/15 text-blue-400", InProgress: "bg-amber-500/15 text-amber-400", InReview: "bg-purple-500/15 text-purple-400", Done: "bg-emerald-500/15 text-emerald-400", Cancelled: "bg-red-500/15 text-red-400" };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold text-[var(--color-text)] flex items-center gap-2">
            <Layout className="w-5 h-5 text-indigo-400" /> Board Kolonları
            <span className="text-sm font-normal text-[var(--color-text-muted)]">({sorted.length})</span>
          </h3>
          <p className="text-xs text-[var(--color-text-muted)] mt-1">Kanban board kolonlarını düzenleyin, WIP limitleri belirleyin ve durum eşleştirmelerini yapılandırın.</p>
        </div>
        <button onClick={() => { setEditingCol(null); setForm({ name: "", mappedStatus: "Backlog", wipLimit: "" }); setShowForm(true); }}
          className="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium bg-indigo-600 text-white hover:bg-indigo-700 shadow-lg shadow-indigo-500/20 transition-all">
          <Plus className="w-4 h-4" /> Kolon Ekle
        </button>
      </div>

      {showForm && (
        <div className="p-4 rounded-xl border border-indigo-500/30 bg-indigo-500/5 space-y-4">
          <h4 className="text-sm font-semibold text-[var(--color-text)]">{editingCol ? "Kolonu Düzenle" : "Yeni Kolon"}</h4>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Kolon Adı</label>
              <input type="text" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} placeholder="Ör: Code Review"
                className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Durum Eşleştirmesi</label>
              <select value={form.mappedStatus} onChange={e => setForm(f => ({ ...f, mappedStatus: e.target.value }))}
                className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40">
                {STATUS_OPTIONS.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">WIP Limiti <span className="text-[var(--color-text-muted)]">(opsiyonel)</span></label>
              <input type="number" value={form.wipLimit} onChange={e => setForm(f => ({ ...f, wipLimit: e.target.value }))} placeholder="Sınırsız" min={1}
                className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
            </div>
          </div>
          <div className="flex gap-2 justify-end">
            <button onClick={() => { setShowForm(false); setEditingCol(null); }}
              className="px-3 py-1.5 rounded-lg text-sm border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50 transition-colors">İptal</button>
            <button onClick={handleSave} disabled={saving || !form.name.trim()}
              className="px-4 py-1.5 rounded-lg text-sm font-medium bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors flex items-center gap-1">
              {saving ? <Loader2 className="w-3 h-3 animate-spin" /> : <Save className="w-3 h-3" />}
              {editingCol ? "Güncelle" : "Ekle"}
            </button>
          </div>
        </div>
      )}

      {loading ? (
        <div className="flex justify-center py-8"><Loader2 className="w-6 h-6 animate-spin text-indigo-400" /></div>
      ) : sorted.length === 0 ? (
        <div className="text-center py-8 text-[var(--color-text-muted)] text-sm">Henüz board kolonu tanımlanmamış.</div>
      ) : (
        <div className="space-y-2">
          {sorted.map((col, idx) => (
            <div key={col.id} className="flex items-center gap-3 p-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] hover:border-[var(--color-border)]/80 transition-all group">
              <GripVertical className="w-4 h-4 text-[var(--color-text-muted)] opacity-30 group-hover:opacity-100 transition-opacity cursor-grab" />
              <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-indigo-500/20 to-violet-500/20 flex items-center justify-center text-xs font-bold text-indigo-400">{idx + 1}</div>
              <div className="flex-1 min-w-0">
                <div className="text-sm font-medium text-[var(--color-text)]">{col.name}</div>
                <div className="flex items-center gap-2 mt-0.5">
                  <span className={cn("px-1.5 py-0.5 rounded-full text-[9px] font-medium border", statusCfg[col.mappedStatus] || "bg-slate-500/15 text-slate-400")}>
                    {STATUS_OPTIONS.find(s => s.value === col.mappedStatus)?.label || col.mappedStatus}
                  </span>
                  {col.wipLimit && <span className="text-[10px] text-[var(--color-text-muted)]">WIP: {col.wipLimit}</span>}
                </div>
              </div>
              <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                <button onClick={() => startEdit(col)} className="p-1.5 rounded-lg hover:bg-indigo-500/10 text-[var(--color-text-muted)] hover:text-indigo-400 transition-colors"><Edit3 className="w-3.5 h-3.5" /></button>
                <button onClick={() => handleDelete(col.id)} className="p-1.5 rounded-lg hover:bg-red-500/10 text-[var(--color-text-muted)] hover:text-red-400 transition-colors"><Trash2 className="w-3.5 h-3.5" /></button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
