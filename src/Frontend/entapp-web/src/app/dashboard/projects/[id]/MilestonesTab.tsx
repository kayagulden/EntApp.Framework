"use client";
import { useState, useEffect, useCallback } from "react";
import { Plus, Edit3, Save, Trash2, Loader2, Calendar, ListTodo, Archive, Milestone } from "lucide-react";
import { cn } from "@/lib/utils";

interface MilestoneItem {
  id: string; name: string; description?: string; status: string;
  dueDate: string; completedDate?: string; sortOrder: number;
  workItemCount: number; sprintCount: number; createdAt: string;
}

const MS_STATUS_CONFIG: Record<string, { label: string; color: string; icon: string }> = {
  Pending: { label: "Bekliyor", color: "bg-slate-500/15 text-slate-400 border-slate-500/30", icon: "⏳" },
  InProgress: { label: "Devam Ediyor", color: "bg-blue-500/15 text-blue-400 border-blue-500/30", icon: "🔄" },
  Reached: { label: "Ulaşıldı", color: "bg-emerald-500/15 text-emerald-400 border-emerald-500/30", icon: "✅" },
  Missed: { label: "Kaçırıldı", color: "bg-red-500/15 text-red-400 border-red-500/30", icon: "❌" },
  Cancelled: { label: "İptal", color: "bg-gray-500/15 text-gray-400 border-gray-500/30", icon: "🚫" },
};

export default function MilestonesTab({ projectId }: { projectId: string }) {
  const [milestones, setMilestones] = useState<MilestoneItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingMilestone, setEditingMilestone] = useState<MilestoneItem | null>(null);
  const [msForm, setMsForm] = useState({ name: "", description: "", dueDate: "", sortOrder: "0" });
  const [msSaving, setMsSaving] = useState(false);

  const fetchMilestones = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetch(`/api/pm/projects/${projectId}/milestones`);
      if (res.ok) setMilestones(await res.json());
    } catch { /* */ }
    finally { setLoading(false); }
  }, [projectId]);

  useEffect(() => { fetchMilestones(); }, [fetchMilestones]);

  const handleSave = async () => {
    if (!msForm.name.trim() || !msForm.dueDate) return;
    setMsSaving(true);
    try {
      if (editingMilestone) {
        await fetch(`/api/pm/milestones/${editingMilestone.id}`, {
          method: "PUT", headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ name: msForm.name, description: msForm.description || null, dueDate: msForm.dueDate, sortOrder: parseInt(msForm.sortOrder) || 0 }),
        });
      } else {
        await fetch(`/api/pm/projects/${projectId}/milestones`, {
          method: "POST", headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ name: msForm.name, description: msForm.description || null, dueDate: msForm.dueDate, sortOrder: parseInt(msForm.sortOrder) || 0 }),
        });
      }
      setShowForm(false); setEditingMilestone(null);
      setMsForm({ name: "", description: "", dueDate: "", sortOrder: "0" });
      await fetchMilestones();
    } catch { /* */ }
    finally { setMsSaving(false); }
  };

  const handleStatusChange = async (msId: string, status: string) => {
    await fetch(`/api/pm/milestones/${msId}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status }) });
    await fetchMilestones();
  };

  const handleDelete = async (msId: string) => {
    await fetch(`/api/pm/milestones/${msId}`, { method: "DELETE" });
    await fetchMilestones();
  };

  const startEdit = (ms: MilestoneItem) => {
    setEditingMilestone(ms);
    setMsForm({ name: ms.name, description: ms.description || "", dueDate: ms.dueDate.split("T")[0], sortOrder: String(ms.sortOrder) });
    setShowForm(true);
  };

  const isOverdue = (dueDate: string, status: string) =>
    new Date(dueDate).getTime() < Date.now() && status !== "Reached" && status !== "Cancelled";

  const daysUntil = (dueDate: string) => {
    const diff = Math.ceil((new Date(dueDate).getTime() - Date.now()) / (1000 * 60 * 60 * 24));
    if (diff < 0) return `${Math.abs(diff)} gün geçti`;
    if (diff === 0) return "Bugün";
    return `${diff} gün kaldı`;
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-[var(--color-text)] flex items-center gap-2">
          <Milestone className="w-4 h-4 text-violet-400" /> Milestones
          <span className="text-xs font-normal text-[var(--color-text-muted)]">({milestones.length})</span>
        </h3>
        <button onClick={() => { setEditingMilestone(null); setMsForm({ name: "", description: "", dueDate: "", sortOrder: String(milestones.length) }); setShowForm(true); }}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium bg-violet-600 text-white hover:bg-violet-700 transition-colors shadow-lg shadow-violet-500/20">
          <Plus className="w-3.5 h-3.5" /> Yeni Milestone
        </button>
      </div>

      {showForm && (
        <div className="rounded-xl border border-violet-500/30 bg-violet-500/5 p-4 space-y-3">
          <h4 className="text-sm font-semibold text-[var(--color-text)]">{editingMilestone ? "Milestone Düzenle" : "Yeni Milestone"}</h4>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider mb-1">Ad *</label>
              <input type="text" value={msForm.name} onChange={e => setMsForm({ ...msForm, name: e.target.value })} placeholder="MVP Ready, Go-Live..."
                className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40" />
            </div>
            <div>
              <label className="block text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider mb-1">Hedef Tarih *</label>
              <input type="date" value={msForm.dueDate} onChange={e => setMsForm({ ...msForm, dueDate: e.target.value })}
                className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-violet-500/40" />
            </div>
          </div>
          <div>
            <label className="block text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider mb-1">Açıklama</label>
            <textarea value={msForm.description} onChange={e => setMsForm({ ...msForm, description: e.target.value })} rows={2} placeholder="Milestone açıklaması..."
              className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40 resize-none" />
          </div>
          <div className="flex items-center gap-2">
            <button onClick={handleSave} disabled={msSaving || !msForm.name.trim() || !msForm.dueDate}
              className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs font-medium bg-violet-600 text-white hover:bg-violet-700 disabled:opacity-50 transition-colors">
              {msSaving ? <Loader2 className="w-3 h-3 animate-spin" /> : <Save className="w-3 h-3" />}
              {editingMilestone ? "Güncelle" : "Oluştur"}
            </button>
            <button onClick={() => { setShowForm(false); setEditingMilestone(null); }}
              className="px-4 py-2 rounded-lg text-xs text-[var(--color-text-muted)] hover:bg-[var(--color-border)] transition-colors">İptal</button>
          </div>
        </div>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-12"><Loader2 className="w-6 h-6 text-violet-400 animate-spin" /></div>
      ) : milestones.length === 0 ? (
        <div className="text-center py-16 text-[var(--color-text-muted)]">
          <Milestone className="w-12 h-12 mx-auto opacity-20 mb-3" />
          <p className="text-sm">Henüz milestone oluşturulmadı</p>
          <p className="text-xs mt-1">Proje kontrol noktaları ekleyerek ilerlemeyi takip edin</p>
        </div>
      ) : (
        <div className="relative">
          <div className="absolute left-6 top-0 bottom-0 w-px bg-gradient-to-b from-violet-500/40 via-violet-500/20 to-transparent" />
          <div className="space-y-4">
            {milestones.map((ms) => {
              const cfg = MS_STATUS_CONFIG[ms.status] || MS_STATUS_CONFIG.Pending;
              const overdue = isOverdue(ms.dueDate, ms.status);
              return (
                <div key={ms.id} className="relative pl-14">
                  <div className={cn("absolute left-4 top-4 w-5 h-5 rounded-full border-2 flex items-center justify-center text-[10px] z-10",
                    ms.status === "Reached" ? "border-emerald-500 bg-emerald-500/20" :
                    ms.status === "Missed" || overdue ? "border-red-500 bg-red-500/20" :
                    ms.status === "InProgress" ? "border-blue-500 bg-blue-500/20" :
                    ms.status === "Cancelled" ? "border-gray-500 bg-gray-500/20" :
                    "border-violet-500 bg-violet-500/20"
                  )}><span>{cfg.icon}</span></div>
                  <div className={cn("rounded-xl border p-4 transition-all hover:shadow-lg",
                    overdue && ms.status !== "Missed" ? "border-red-500/30 bg-red-500/5" : "border-[var(--color-border)] bg-[var(--color-card-bg)]"
                  )}>
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <h4 className="text-sm font-semibold text-[var(--color-text)] truncate">{ms.name}</h4>
                          <span className={cn("inline-flex px-2 py-0.5 rounded-full text-[9px] font-medium border shrink-0", cfg.color)}>{cfg.label}</span>
                        </div>
                        {ms.description && <p className="text-xs text-[var(--color-text-muted)] line-clamp-2 mb-2">{ms.description}</p>}
                        <div className="flex items-center gap-4 text-[10px] text-[var(--color-text-muted)]">
                          <span className={cn("flex items-center gap-1", overdue && "text-red-400 font-medium")}>
                            <Calendar className="w-3 h-3" />
                            {new Date(ms.dueDate).toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" })}
                          </span>
                          <span className={cn(overdue && ms.status !== "Reached" && ms.status !== "Cancelled" ? "text-red-400" : "text-[var(--color-text-muted)]")}>
                            {daysUntil(ms.dueDate)}
                          </span>
                          {ms.workItemCount > 0 && <span className="flex items-center gap-1"><ListTodo className="w-3 h-3" /> {ms.workItemCount} iş kalemi</span>}
                          {ms.sprintCount > 0 && <span className="flex items-center gap-1"><Archive className="w-3 h-3" /> {ms.sprintCount} sprint</span>}
                        </div>
                      </div>
                      <div className="flex items-center gap-1 shrink-0">
                        <select value={ms.status} onChange={e => handleStatusChange(ms.id, e.target.value)}
                          className="px-2 py-1 rounded text-[10px] bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] cursor-pointer">
                          {Object.entries(MS_STATUS_CONFIG).map(([key, val]) => <option key={key} value={key}>{val.label}</option>)}
                        </select>
                        <button onClick={() => startEdit(ms)} className="p-1.5 rounded-lg hover:bg-[var(--color-border)] transition-colors text-[var(--color-text-muted)] hover:text-[var(--color-text)]"><Edit3 className="w-3.5 h-3.5" /></button>
                        <button onClick={() => handleDelete(ms.id)} className="p-1.5 rounded-lg hover:bg-red-500/10 transition-colors text-[var(--color-text-muted)] hover:text-red-400"><Trash2 className="w-3.5 h-3.5" /></button>
                      </div>
                    </div>
                    {ms.workItemCount > 0 && ms.status !== "Cancelled" && (
                      <div className="mt-3 pt-3 border-t border-[var(--color-border)]">
                        <div className="flex items-center justify-between text-[10px] text-[var(--color-text-muted)] mb-1">
                          <span>İlerleme</span><span>{ms.workItemCount} iş kalemi bağlı</span>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
