"use client";
import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import { Plus, Edit3, Save, Loader2, ChevronDown, Play, Square, XCircle, Timer } from "lucide-react";
import { cn } from "@/lib/utils";

interface SprintListItem {
  id: string; name: string; goal?: string; status: string;
  startDate: string; endDate: string; capacityPoints?: number;
  projectId: string; projectKey: string;
  workItemCount: number; totalStoryPoints?: number; createdAt: string;
}

interface SprintDetail extends SprintListItem {
  projectName: string; updatedAt?: string;
  workItems: { id: string; workItemNumber: string; title: string; status: string; priority: string; type: string; assigneeUserId?: string; storyPoints?: number; wsjfScore?: number }[];
}

const SPRINT_STATUS_CONFIG: Record<string, { label: string; color: string; icon: string; bgClass: string }> = {
  Planning: { label: "Planlama", color: "text-blue-400", icon: "📋", bgClass: "bg-blue-500/10 border-blue-500/20" },
  Active: { label: "Aktif", color: "text-emerald-400", icon: "🚀", bgClass: "bg-emerald-500/10 border-emerald-500/20" },
  Completed: { label: "Tamamlandı", color: "text-teal-400", icon: "✅", bgClass: "bg-teal-500/10 border-teal-500/20" },
  Cancelled: { label: "İptal", color: "text-gray-400", icon: "🚫", bgClass: "bg-gray-500/10 border-gray-500/20" },
};

const TYPE_ICONS: Record<string,string> = { Task:"📋", Bug:"🐛", Feature:"🏗", Improvement:"⚡", Epic:"🎯", UserStory:"📖", TechDebt:"🔧", Spike:"🔬" };
const STATUS_LABELS: Record<string,{label:string;color:string}> = {
  Backlog: { label: "Backlog", color: "bg-slate-500/20 text-slate-400" },
  Todo: { label: "Yapılacak", color: "bg-blue-500/20 text-blue-400" },
  InProgress: { label: "Devam Ediyor", color: "bg-amber-500/20 text-amber-400" },
  InReview: { label: "İncelemede", color: "bg-purple-500/20 text-purple-400" },
  Done: { label: "Tamamlandı", color: "bg-emerald-500/20 text-emerald-400" },
  Cancelled: { label: "İptal", color: "bg-red-500/20 text-red-400" },
};

export default function SprintsTab({ projectId }: { projectId: string }) {
  const [sprints, setSprints] = useState<SprintListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [statusFilter, setStatusFilter] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [editingSprint, setEditingSprint] = useState<SprintListItem | null>(null);
  const [form, setForm] = useState({ name: "", goal: "", startDate: "", endDate: "", capacityPoints: "" });
  const [saving, setSaving] = useState(false);
  const [expandedSprint, setExpandedSprint] = useState<string | null>(null);
  const [sprintDetail, setSprintDetail] = useState<SprintDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [backlogItems, setBacklogItems] = useState<{ id: string; workItemNumber: string; title: string; type: string }[]>([]);
  const [assigningItem, setAssigningItem] = useState("");
  const router = useRouter();

  const fetchSprints = useCallback(async () => {
    setLoading(true);
    try {
      const params = statusFilter ? `?status=${statusFilter}` : "";
      const res = await fetch(`/api/pm/projects/${projectId}/sprints${params}`);
      if (res.ok) setSprints(await res.json());
    } catch { /* */ }
    finally { setLoading(false); }
  }, [projectId, statusFilter]);

  useEffect(() => { fetchSprints(); }, [fetchSprints]);

  const fetchSprintDetail = async (sprintId: string) => {
    setDetailLoading(true);
    try { const res = await fetch(`/api/pm/sprints/${sprintId}`); if (res.ok) setSprintDetail(await res.json()); }
    catch { /* */ } finally { setDetailLoading(false); }
  };

  const fetchBacklogItems = useCallback(() => {
    fetch(`/api/pm/projects/${projectId}/backlog?view=flat`)
      .then(r => r.ok ? r.json() : [])
      .then(data => {
        const items = (Array.isArray(data) ? data : [])
          .filter((i: any) => !i.sprintId)
          .map((i: any) => ({ id: typeof i.id === "object" ? i.id.value : i.id, workItemNumber: i.workItemNumber, title: i.title, type: i.type }));
        setBacklogItems(items);
      }).catch(() => {});
  }, [projectId]);

  const toggleExpand = (sprintId: string) => {
    if (expandedSprint === sprintId) { setExpandedSprint(null); setSprintDetail(null); }
    else { setExpandedSprint(sprintId); fetchSprintDetail(sprintId); fetchBacklogItems(); }
  };

  const handleSave = async () => {
    if (!form.name.trim() || !form.startDate || !form.endDate) return;
    setSaving(true);
    try {
      const body = { name: form.name, goal: form.goal || null, startDate: form.startDate, endDate: form.endDate, capacityPoints: form.capacityPoints ? parseInt(form.capacityPoints) : null };
      if (editingSprint) await fetch(`/api/pm/sprints/${editingSprint.id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      else await fetch(`/api/pm/projects/${projectId}/sprints`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      resetForm(); await fetchSprints();
    } catch { /* */ } finally { setSaving(false); }
  };

  const resetForm = () => { setShowForm(false); setEditingSprint(null); setForm({ name: "", goal: "", startDate: "", endDate: "", capacityPoints: "" }); };

  const startEdit = (s: SprintListItem) => {
    setEditingSprint(s);
    setForm({ name: s.name, goal: s.goal || "", startDate: new Date(s.startDate).toISOString().split("T")[0], endDate: new Date(s.endDate).toISOString().split("T")[0], capacityPoints: s.capacityPoints?.toString() || "" });
    setShowForm(true);
  };

  const handleAction = async (sprintId: string, action: "start" | "complete") => {
    try { await fetch(`/api/pm/sprints/${sprintId}/${action}`, { method: "POST" }); await fetchSprints(); if (expandedSprint === sprintId) fetchSprintDetail(sprintId); } catch { /* */ }
  };

  const handleAssignItem = async (sprintId: string) => {
    if (!assigningItem) return;
    try {
      await fetch(`/api/pm/work-items/${assigningItem}/sprint`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ sprintId }) });
      setAssigningItem(""); fetchSprintDetail(sprintId); fetchBacklogItems(); fetchSprints();
    } catch { /* */ }
  };

  const handleRemoveFromSprint = async (workItemId: string, sprintId: string) => {
    try {
      await fetch(`/api/pm/work-items/${workItemId}/sprint`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ sprintId: null }) });
      fetchSprintDetail(sprintId); fetchBacklogItems(); fetchSprints();
    } catch { /* */ }
  };

  const fmtDate = (d: string) => new Date(d).toLocaleDateString("tr-TR", { day: "2-digit", month: "short" });
  const daysLeft = (end: string) => { const d = Math.ceil((new Date(end).getTime() - Date.now()) / 86400000); return d > 0 ? `${d} gün kaldı` : d === 0 ? "Bugün bitiyor" : `${Math.abs(d)} gün geçti`; };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
            className="text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-3 py-1.5">
            <option value="">Tümü</option><option value="Planning">Planlama</option><option value="Active">Aktif</option><option value="Completed">Tamamlandı</option>
          </select>
        </div>
        <button onClick={() => { resetForm(); setShowForm(true); }}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-medium transition-colors">
          <Plus className="w-3.5 h-3.5" /> Yeni Sprint
        </button>
      </div>

      {showForm && (
        <div className="rounded-xl border border-indigo-500/30 bg-indigo-500/5 p-5 space-y-3">
          <h4 className="text-sm font-semibold text-[var(--color-text)]">{editingSprint ? "Sprint Düzenle" : "Yeni Sprint Oluştur"}</h4>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <input placeholder="Sprint adı *" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} className="text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-3 py-2" />
            <input placeholder="Kapasite (SP)" type="number" value={form.capacityPoints} onChange={e => setForm({ ...form, capacityPoints: e.target.value })} className="text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-3 py-2" />
            <input type="date" value={form.startDate} onChange={e => setForm({ ...form, startDate: e.target.value })} className="text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-3 py-2" />
            <input type="date" value={form.endDate} onChange={e => setForm({ ...form, endDate: e.target.value })} className="text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-3 py-2" />
          </div>
          <textarea placeholder="Sprint hedefi (opsiyonel)" value={form.goal} onChange={e => setForm({ ...form, goal: e.target.value })}
            className="w-full text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-3 py-2 h-16 resize-none" />
          <div className="flex gap-2">
            <button onClick={handleSave} disabled={saving || !form.name.trim() || !form.startDate || !form.endDate}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white text-xs font-medium">
              {saving ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Save className="w-3.5 h-3.5" />} Kaydet
            </button>
            <button onClick={resetForm} className="px-3 py-1.5 rounded-lg border border-[var(--color-border)] text-[var(--color-text-muted)] text-xs hover:text-[var(--color-text)]">İptal</button>
          </div>
        </div>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-12"><Loader2 className="w-6 h-6 text-indigo-400 animate-spin" /></div>
      ) : sprints.length === 0 ? (
        <div className="text-center py-12 text-[var(--color-text-muted)]">
          <Timer className="w-10 h-10 mx-auto opacity-20 mb-2" /><p className="text-sm">Henüz sprint oluşturulmamış</p>
        </div>
      ) : (
        <div className="space-y-3">
          {sprints.map(s => {
            const cfg = SPRINT_STATUS_CONFIG[s.status] || SPRINT_STATUS_CONFIG.Planning;
            const isExpanded = expandedSprint === s.id;
            return (
              <div key={s.id} className={cn("rounded-xl border transition-all", isExpanded ? "border-indigo-500/40 bg-indigo-500/5" : "border-[var(--color-border)] bg-[var(--color-card-bg)]")}>
                <div className="p-4 flex items-start gap-3 cursor-pointer" onClick={() => toggleExpand(s.id)}>
                  <div className={cn("w-9 h-9 rounded-lg border flex items-center justify-center text-lg shrink-0", cfg.bgClass)}>{cfg.icon}</div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-0.5">
                      <span className="text-sm font-semibold text-[var(--color-text)]">{s.name}</span>
                      <span className={cn("inline-flex px-2 py-0.5 rounded-full text-[9px] font-medium border", cfg.bgClass, cfg.color)}>{cfg.label}</span>
                    </div>
                    {s.goal && <p className="text-[11px] text-[var(--color-text-muted)] line-clamp-1 mb-1">{s.goal}</p>}
                    <div className="flex items-center gap-4 text-[10px] text-[var(--color-text-muted)]">
                      <span>📅 {fmtDate(s.startDate)} → {fmtDate(s.endDate)}</span>
                      <span>📦 {s.workItemCount} iş kalemi</span>
                      <span>⚡ {s.totalStoryPoints || 0} SP</span>
                      {s.capacityPoints && <span>🎯 Kapasite: {s.capacityPoints} SP</span>}
                      {s.status === "Active" && <span className="text-amber-400">{daysLeft(s.endDate)}</span>}
                    </div>
                  </div>
                  <div className="flex items-center gap-1.5 shrink-0" onClick={e => e.stopPropagation()}>
                    {s.status === "Planning" && (
                      <>
                        <button onClick={() => startEdit(s)} className="p-1.5 rounded-lg hover:bg-[var(--color-bg)] text-[var(--color-text-muted)] hover:text-[var(--color-text)]"><Edit3 className="w-3.5 h-3.5" /></button>
                        <button onClick={() => handleAction(s.id, "start")} className="flex items-center gap-1 px-2.5 py-1 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-[10px] font-medium"><Play className="w-3 h-3" /> Başlat</button>
                      </>
                    )}
                    {s.status === "Active" && (
                      <button onClick={() => handleAction(s.id, "complete")} className="flex items-center gap-1 px-2.5 py-1 rounded-lg bg-teal-600 hover:bg-teal-500 text-white text-[10px] font-medium"><Square className="w-3 h-3" /> Tamamla</button>
                    )}
                    <ChevronDown className={cn("w-4 h-4 text-[var(--color-text-muted)] transition-transform", isExpanded && "rotate-180")} />
                  </div>
                </div>

                {isExpanded && (
                  <div className="border-t border-[var(--color-border)] p-4 space-y-3">
                    {detailLoading ? (
                      <div className="flex items-center justify-center py-6"><Loader2 className="w-5 h-5 text-indigo-400 animate-spin" /></div>
                    ) : sprintDetail ? (
                      <>
                        {s.status !== "Completed" && s.status !== "Cancelled" && backlogItems.length > 0 && (
                          <div className="flex items-center gap-2 p-3 rounded-lg border border-dashed border-indigo-500/30 bg-indigo-500/5">
                            <select value={assigningItem} onChange={e => setAssigningItem(e.target.value)}
                              className="flex-1 text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-3 py-1.5">
                              <option value="">Backlog&apos;dan iş kalemi seçin...</option>
                              {backlogItems.map(i => <option key={i.id} value={i.id}>{i.workItemNumber} — {i.title}</option>)}
                            </select>
                            <button onClick={() => handleAssignItem(s.id)} disabled={!assigningItem}
                              className="flex items-center gap-1 px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white text-xs font-medium">
                              <Plus className="w-3 h-3" /> Ata
                            </button>
                          </div>
                        )}
                        {sprintDetail.workItems.length === 0 ? (
                          <p className="text-center text-[11px] text-[var(--color-text-muted)] py-4">Bu sprint&apos;e henüz iş kalemi atanmamış</p>
                        ) : (
                          <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
                            <table className="w-full text-xs">
                              <thead><tr className="border-b border-[var(--color-border)] bg-[var(--color-bg)]">
                                <th className="text-left px-3 py-2 text-[9px] font-semibold text-[var(--color-text-muted)] uppercase">Numara</th>
                                <th className="text-left px-3 py-2 text-[9px] font-semibold text-[var(--color-text-muted)] uppercase">Başlık</th>
                                <th className="text-left px-3 py-2 text-[9px] font-semibold text-[var(--color-text-muted)] uppercase">Tip</th>
                                <th className="text-left px-3 py-2 text-[9px] font-semibold text-[var(--color-text-muted)] uppercase">Durum</th>
                                <th className="text-center px-3 py-2 text-[9px] font-semibold text-[var(--color-text-muted)] uppercase">SP</th>
                                <th className="text-center px-3 py-2 text-[9px] font-semibold text-[var(--color-text-muted)] uppercase w-10"></th>
                              </tr></thead>
                              <tbody>
                                {sprintDetail.workItems.map(wi => {
                                  const wiId = typeof wi.id === "object" ? (wi.id as any).value : wi.id;
                                  const stl = STATUS_LABELS[wi.status] || { label: wi.status, color: "bg-gray-500/20 text-gray-400" };
                                  return (
                                    <tr key={wiId} className="border-b border-[var(--color-border)] hover:bg-indigo-500/5 transition-colors">
                                      <td className="px-3 py-2"><button onClick={() => router.push(`/dashboard/tasks/${wiId}`)} className="text-[10px] font-mono text-indigo-400 hover:underline">{wi.workItemNumber}</button></td>
                                      <td className="px-3 py-2 text-[var(--color-text)]">{wi.title}</td>
                                      <td className="px-3 py-2">{TYPE_ICONS[wi.type] || "📋"} {wi.type}</td>
                                      <td className="px-3 py-2"><span className={cn("px-2 py-0.5 rounded-full text-[9px] font-medium", stl.color)}>{stl.label}</span></td>
                                      <td className="px-3 py-2 text-center">{wi.storyPoints != null && wi.storyPoints > 0 ? <span className="text-[10px] font-bold text-indigo-400">{wi.storyPoints}</span> : <span className="text-[var(--color-text-muted)]">—</span>}</td>
                                      <td className="px-3 py-2 text-center">
                                        {s.status !== "Completed" && s.status !== "Cancelled" && (
                                          <button onClick={() => handleRemoveFromSprint(wiId, s.id)} className="text-[var(--color-text-muted)] hover:text-red-400" title="Sprint'ten çıkar"><XCircle className="w-3.5 h-3.5" /></button>
                                        )}
                                      </td>
                                    </tr>
                                  );
                                })}
                              </tbody>
                            </table>
                            <div className="px-3 py-1.5 border-t border-[var(--color-border)] bg-[var(--color-bg)] flex items-center justify-between">
                              <span className="text-[9px] text-[var(--color-text-muted)]">{sprintDetail.workItems.length} iş kalemi</span>
                              <span className="text-[9px] text-indigo-400 font-medium">{sprintDetail.workItems.filter(w => w.storyPoints).reduce((sum, w) => sum + (w.storyPoints || 0), 0)} SP</span>
                            </div>
                          </div>
                        )}
                      </>
                    ) : null}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
