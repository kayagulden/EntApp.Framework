"use client";
import { useState, useEffect, useCallback } from "react";
import { Loader2, X, Package, Shield, CheckCircle2, XCircle, Clock, FileText, Plus, Trash2, RefreshCw, Download, ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

const API = "/api/pm";

// ── Types ───────────────────────────────────────────────────
interface ReleaseDetail {
  id: string; key: string; version: string; title: string;
  status: string; type: string; description?: string;
  plannedDate?: string; actualDate?: string; codeFreezeDate?: string;
  releaseManagerId?: string; targetEnvironment?: string;
  sprintId?: string; milestoneId?: string;
  tags?: string; sortOrder: number;
  createdAt: string; updatedAt?: string;
  items?: ReleaseItemDto[]; goNoGoChecklist?: GoNoGoSummary; releaseNote?: ReleaseNoteDto;
}
interface ReleaseItemDto { id: string; workItemId: string; workItemNumber: string; workItemTitle: string; workItemType: string; workItemStatus: string; includedAt: string; includedBy: string; notes?: string; sortOrder: number; }
interface GoNoGoSummary { id: string; status: string; totalItems: number; approvedCount: number; rejectedCount: number; pendingCount: number; decisionBy?: string; decisionAt?: string; }
interface GoNoGoDetail { id: string; releaseId: string; status: string; decisionBy?: string; decisionAt?: string; decisionNotes?: string; createdAt: string; items: GoNoGoItemDto[]; }
interface GoNoGoItemDto { id: string; category: string; title: string; description?: string; status: string; reviewedBy?: string; reviewedAt?: string; notes?: string; sortOrder: number; isRequired: boolean; }
interface ReleaseNoteDto { id: string; releaseId: string; content: string; generatedAt: string; isManuallyEdited: boolean; publishedAt?: string; }

const STATUS_CFG: Record<string, { label: string; color: string; icon: string }> = {
  Planning: { label: "Planlama", color: "bg-slate-500/15 text-slate-400 border-slate-500/30", icon: "📋" },
  CodeFreeze: { label: "Code Freeze", color: "bg-blue-500/15 text-blue-400 border-blue-500/30", icon: "🧊" },
  Testing: { label: "Test", color: "bg-amber-500/15 text-amber-400 border-amber-500/30", icon: "🧪" },
  GoNoGo: { label: "Go/No-Go", color: "bg-purple-500/15 text-purple-400 border-purple-500/30", icon: "⚖️" },
  Staging: { label: "Staging", color: "bg-cyan-500/15 text-cyan-400 border-cyan-500/30", icon: "🚀" },
  Deployed: { label: "Deployed", color: "bg-emerald-500/15 text-emerald-400 border-emerald-500/30", icon: "✅" },
  Closed: { label: "Kapatıldı", color: "bg-teal-500/15 text-teal-400 border-teal-500/30", icon: "🏁" },
  Cancelled: { label: "İptal", color: "bg-gray-500/15 text-gray-400 border-gray-500/30", icon: "🚫" },
  Rollback: { label: "Rollback", color: "bg-red-500/15 text-red-400 border-red-500/30", icon: "⏪" },
};

const GNGO_STATUS: Record<string, { label: string; color: string; icon: React.ReactNode }> = {
  Pending: { label: "Bekliyor", color: "text-slate-400", icon: <Clock className="w-3.5 h-3.5" /> },
  Approved: { label: "Onaylandı", color: "text-emerald-400", icon: <CheckCircle2 className="w-3.5 h-3.5" /> },
  Rejected: { label: "Reddedildi", color: "text-red-400", icon: <XCircle className="w-3.5 h-3.5" /> },
  NotApplicable: { label: "N/A", color: "text-gray-400", icon: <span className="text-[10px]">—</span> },
};

const CATEGORY_LABELS: Record<string, string> = { Development: "Geliştirme", QA: "Kalite", Operations: "Operasyon", Security: "Güvenlik", Business: "İş Birimi", Legal: "Yasal" };

export default function ReleaseDetailPanel({ releaseId, onClose, onRefreshList }: { releaseId: string; onClose: () => void; onRefreshList: () => void; }) {
  const [release, setRelease] = useState<ReleaseDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<"items" | "gonogo" | "notes">("items");
  const [items, setItems] = useState<ReleaseItemDto[]>([]);
  const [gonogoDetail, setGonogoDetail] = useState<GoNoGoDetail | null>(null);
  const [releaseNote, setReleaseNote] = useState<ReleaseNoteDto | null>(null);
  const [noteContent, setNoteContent] = useState("");
  const [noteEditing, setNoteEditing] = useState(false);
  const [noteSaving, setNoteSaving] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);

  const fetchRelease = useCallback(async () => {
    setLoading(true);
    try { const res = await fetch(`${API}/releases/${releaseId}`); if (res.ok) setRelease(await res.json()); }
    catch { /* */ } finally { setLoading(false); }
  }, [releaseId]);

  const fetchItems = useCallback(async () => {
    try { const res = await fetch(`${API}/releases/${releaseId}/items`); if (res.ok) setItems(await res.json()); } catch { /* */ }
  }, [releaseId]);

  const fetchGoNoGo = useCallback(async () => {
    try { const res = await fetch(`${API}/releases/${releaseId}/go-no-go`); if (res.ok) setGonogoDetail(await res.json()); else setGonogoDetail(null); } catch { setGonogoDetail(null); }
  }, [releaseId]);

  const fetchReleaseNote = useCallback(async () => {
    try { const res = await fetch(`${API}/releases/${releaseId}/release-note`); if (res.ok) { const n = await res.json(); setReleaseNote(n); setNoteContent(n.content || ""); } else { setReleaseNote(null); setNoteContent(""); } } catch { setReleaseNote(null); }
  }, [releaseId]);

  useEffect(() => { fetchRelease(); fetchItems(); fetchGoNoGo(); fetchReleaseNote(); }, [fetchRelease, fetchItems, fetchGoNoGo, fetchReleaseNote]);

  const changeStatus = async (status: string) => {
    setActionLoading(true);
    try { await fetch(`${API}/releases/${releaseId}/status`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status }) }); await fetchRelease(); onRefreshList(); }
    catch { /* */ } finally { setActionLoading(false); }
  };

  const removeItem = async (workItemId: string) => {
    await fetch(`${API}/releases/${releaseId}/items/${workItemId}`, { method: "DELETE" }); await fetchItems(); onRefreshList();
  };

  const createChecklist = async () => {
    setActionLoading(true);
    try { await fetch(`${API}/releases/${releaseId}/go-no-go`, { method: "POST" }); await fetchGoNoGo(); } catch { /* */ } finally { setActionLoading(false); }
  };

  const updateGoNoGoItem = async (itemId: string, status: string) => {
    await fetch(`${API}/releases/${releaseId}/go-no-go/items/${itemId}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status, reviewedBy: "current-user" }) });
    await fetchGoNoGo();
  };

  const decideGoNoGo = async (status: string) => {
    setActionLoading(true);
    try { await fetch(`${API}/releases/${releaseId}/go-no-go/decide`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status, decisionBy: "current-user" }) }); await fetchGoNoGo(); } catch { /* */ } finally { setActionLoading(false); }
  };

  const generateNote = async () => {
    setActionLoading(true);
    try { await fetch(`${API}/releases/${releaseId}/release-note/generate`, { method: "POST" }); await fetchReleaseNote(); } catch { /* */ } finally { setActionLoading(false); }
  };

  const saveNote = async () => {
    setNoteSaving(true);
    try { await fetch(`${API}/releases/${releaseId}/release-note`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ content: noteContent }) }); setNoteEditing(false); await fetchReleaseNote(); } catch { /* */ } finally { setNoteSaving(false); }
  };

  if (loading) return <div className="flex items-center justify-center py-16"><Loader2 className="w-6 h-6 text-indigo-400 animate-spin" /></div>;
  if (!release) return <div className="text-center py-8 text-[var(--color-text-muted)] text-sm">Release bulunamadı</div>;

  const sc = STATUS_CFG[release.status] || STATUS_CFG.Planning;
  const fmtD = (d?: string) => d ? new Date(d).toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" }) : "—";
  const tabs = [
    { key: "items" as const, label: "İş Kalemleri", icon: <Package className="w-3.5 h-3.5" />, count: items.length },
    { key: "gonogo" as const, label: "Go/No-Go", icon: <Shield className="w-3.5 h-3.5" />, count: gonogoDetail?.items?.length },
    { key: "notes" as const, label: "Release Note", icon: <FileText className="w-3.5 h-3.5" /> },
  ];

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="text-xs font-mono font-bold text-indigo-400">{release.key}</span>
            <span className="text-lg font-bold text-[var(--color-text)]">{release.version}</span>
          </div>
          <p className="text-sm text-[var(--color-text-muted)]">{release.title}</p>
        </div>
        <button onClick={onClose} className="p-1.5 rounded-lg hover:bg-[var(--color-border)]"><X className="w-4 h-4 text-[var(--color-text-muted)]" /></button>
      </div>

      {/* Status Badge + Actions */}
      <div className="flex items-center gap-2 flex-wrap">
        <span className={cn("px-3 py-1 rounded-full text-xs font-semibold border", sc.color)}>{sc.icon} {sc.label}</span>
        <select value={release.status} onChange={e => changeStatus(e.target.value)} disabled={actionLoading}
          className="px-2 py-1 rounded-lg bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[10px] text-[var(--color-text)]">
          {Object.entries(STATUS_CFG).map(([k, v]) => <option key={k} value={k}>{v.icon} {v.label}</option>)}
        </select>
        {actionLoading && <Loader2 className="w-3.5 h-3.5 text-indigo-400 animate-spin" />}
      </div>

      {/* Info Grid */}
      <div className="grid grid-cols-2 gap-2 text-[10px]">
        <div className="px-3 py-2 rounded-lg bg-[var(--color-bg)] border border-[var(--color-border)]">
          <span className="text-[var(--color-text-muted)]">Planlanan</span><br/><span className="text-[var(--color-text)] font-medium">{fmtD(release.plannedDate)}</span>
        </div>
        <div className="px-3 py-2 rounded-lg bg-[var(--color-bg)] border border-[var(--color-border)]">
          <span className="text-[var(--color-text-muted)]">Code Freeze</span><br/><span className="text-[var(--color-text)] font-medium">{fmtD(release.codeFreezeDate)}</span>
        </div>
        {release.actualDate && <div className="px-3 py-2 rounded-lg bg-emerald-500/5 border border-emerald-500/20">
          <span className="text-emerald-400">Deploy Tarihi</span><br/><span className="text-emerald-300 font-medium">{fmtD(release.actualDate)}</span>
        </div>}
        {release.targetEnvironment && <div className="px-3 py-2 rounded-lg bg-[var(--color-bg)] border border-[var(--color-border)]">
          <span className="text-[var(--color-text-muted)]">Ortam</span><br/><span className="text-[var(--color-text)] font-medium">{release.targetEnvironment}</span>
        </div>}
      </div>

      {release.description && <div className="text-xs text-[var(--color-text-muted)] whitespace-pre-wrap bg-[var(--color-bg)] rounded-lg p-3 border border-[var(--color-border)]">{release.description}</div>}

      {/* Sub-Tabs */}
      <div className="flex border-b border-[var(--color-border)]">
        {tabs.map(t => (
          <button key={t.key} onClick={() => setActiveTab(t.key)}
            className={cn("flex items-center gap-1.5 px-3 py-2 text-[11px] font-medium border-b-2 transition-colors",
              activeTab === t.key ? "border-indigo-500 text-indigo-400" : "border-transparent text-[var(--color-text-muted)] hover:text-[var(--color-text)]")}>
            {t.icon} {t.label} {t.count != null && <span className="text-[9px] bg-[var(--color-bg)] px-1.5 py-0.5 rounded-full">{t.count}</span>}
          </button>
        ))}
      </div>

      {/* Items Tab */}
      {activeTab === "items" && (
        <div className="space-y-2">
          {items.length === 0 ? (
            <p className="text-center text-xs text-[var(--color-text-muted)] py-6">Henüz iş kalemi eklenmemiş</p>
          ) : items.map(item => (
            <div key={item.id} className="flex items-center gap-2 px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-card-bg)] group">
              <span className="text-[10px] font-mono text-indigo-400 w-16 shrink-0">{item.workItemNumber}</span>
              <span className="text-xs flex-1 truncate text-[var(--color-text)]">{item.workItemTitle}</span>
              <span className="text-[9px] text-[var(--color-text-muted)]">{item.workItemType}</span>
              <span className={cn("text-[9px] px-1.5 py-0.5 rounded-full",
                item.workItemStatus === "Done" ? "bg-emerald-500/15 text-emerald-400" : "bg-slate-500/15 text-slate-400")}>{item.workItemStatus}</span>
              <button onClick={() => removeItem(item.workItemId)} className="p-1 rounded hover:bg-red-500/10 text-[var(--color-text-muted)] hover:text-red-400 opacity-0 group-hover:opacity-100 transition-opacity">
                <Trash2 className="w-3 h-3" />
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Go/No-Go Tab */}
      {activeTab === "gonogo" && (
        <div className="space-y-3">
          {!gonogoDetail ? (
            <div className="text-center py-6">
              <Shield className="w-10 h-10 mx-auto text-[var(--color-text-muted)] opacity-20 mb-2" />
              <p className="text-xs text-[var(--color-text-muted)] mb-3">Henüz checklist oluşturulmamış</p>
              <button onClick={createChecklist} disabled={actionLoading}
                className="px-4 py-2 rounded-lg bg-purple-600 hover:bg-purple-500 text-white text-xs font-medium disabled:opacity-50">
                {actionLoading ? <Loader2 className="w-3.5 h-3.5 animate-spin inline mr-1" /> : <Plus className="w-3.5 h-3.5 inline mr-1" />} Checklist Oluştur
              </button>
            </div>
          ) : (
            <>
              {/* Summary Bar */}
              <div className="flex items-center gap-3 p-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)]">
                <div className="flex items-center gap-1 text-emerald-400 text-[11px]"><CheckCircle2 className="w-3.5 h-3.5" /> {gonogoDetail.items.filter(i => i.status === "Approved").length}</div>
                <div className="flex items-center gap-1 text-red-400 text-[11px]"><XCircle className="w-3.5 h-3.5" /> {gonogoDetail.items.filter(i => i.status === "Rejected").length}</div>
                <div className="flex items-center gap-1 text-slate-400 text-[11px]"><Clock className="w-3.5 h-3.5" /> {gonogoDetail.items.filter(i => i.status === "Pending").length}</div>
                <div className="flex-1" />
                <span className={cn("px-2 py-0.5 rounded-full text-[9px] font-semibold border",
                  gonogoDetail.status === "Approved" ? "bg-emerald-500/15 text-emerald-400 border-emerald-500/30" :
                  gonogoDetail.status === "Rejected" ? "bg-red-500/15 text-red-400 border-red-500/30" :
                  "bg-slate-500/15 text-slate-400 border-slate-500/30"
                )}>{gonogoDetail.status}</span>
              </div>

              {/* Items */}
              {gonogoDetail.items.map(item => {
                const gs = GNGO_STATUS[item.status] || GNGO_STATUS.Pending;
                return (
                  <div key={item.id} className="flex items-center gap-2 px-3 py-2.5 rounded-lg border border-[var(--color-border)] bg-[var(--color-card-bg)]">
                    <div className={cn("shrink-0", gs.color)}>{gs.icon}</div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="text-[9px] px-1.5 py-0.5 rounded bg-[var(--color-bg)] text-[var(--color-text-muted)]">{CATEGORY_LABELS[item.category] || item.category}</span>
                        <span className="text-xs font-medium text-[var(--color-text)] truncate">{item.title}</span>
                        {item.isRequired && <span className="text-[8px] text-red-400">*</span>}
                      </div>
                      {item.description && <p className="text-[10px] text-[var(--color-text-muted)] mt-0.5 truncate">{item.description}</p>}
                    </div>
                    <select value={item.status} onChange={e => updateGoNoGoItem(item.id, e.target.value)}
                      className="text-[10px] px-1.5 py-1 rounded border border-[var(--color-border)] bg-[var(--color-input-bg)] text-[var(--color-text)]">
                      <option value="Pending">Bekliyor</option><option value="Approved">Onaylandı</option>
                      <option value="Rejected">Reddedildi</option><option value="NotApplicable">N/A</option>
                    </select>
                  </div>
                );
              })}

              {/* Decision Buttons */}
              {gonogoDetail.status !== "Approved" && gonogoDetail.status !== "Rejected" && (
                <div className="flex gap-2 pt-2">
                  <button onClick={() => decideGoNoGo("Approved")} disabled={actionLoading}
                    className="flex-1 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-medium disabled:opacity-50">✅ Go — Onayla</button>
                  <button onClick={() => decideGoNoGo("Rejected")} disabled={actionLoading}
                    className="flex-1 py-2 rounded-lg bg-red-600 hover:bg-red-500 text-white text-xs font-medium disabled:opacity-50">❌ No-Go — Reddet</button>
                </div>
              )}
            </>
          )}
        </div>
      )}

      {/* Release Note Tab */}
      {activeTab === "notes" && (
        <div className="space-y-3">
          {!releaseNote ? (
            <div className="text-center py-6">
              <FileText className="w-10 h-10 mx-auto text-[var(--color-text-muted)] opacity-20 mb-2" />
              <p className="text-xs text-[var(--color-text-muted)] mb-3">Henüz release note oluşturulmamış</p>
              <button onClick={generateNote} disabled={actionLoading}
                className="px-4 py-2 rounded-lg bg-teal-600 hover:bg-teal-500 text-white text-xs font-medium disabled:opacity-50">
                {actionLoading ? <Loader2 className="w-3.5 h-3.5 animate-spin inline mr-1" /> : <RefreshCw className="w-3.5 h-3.5 inline mr-1" />} Otomatik Oluştur
              </button>
            </div>
          ) : (
            <>
              <div className="flex items-center justify-between">
                <div className="text-[10px] text-[var(--color-text-muted)]">
                  Oluşturma: {new Date(releaseNote.generatedAt).toLocaleDateString("tr-TR")}
                  {releaseNote.isManuallyEdited && <span className="ml-2 text-amber-400">• Manuel düzenlendi</span>}
                </div>
                <div className="flex gap-1">
                  <button onClick={generateNote} disabled={actionLoading} className="p-1.5 rounded-lg hover:bg-[var(--color-border)] text-[var(--color-text-muted)]" title="Yeniden oluştur"><RefreshCw className="w-3.5 h-3.5" /></button>
                  <button onClick={() => setNoteEditing(!noteEditing)} className={cn("p-1.5 rounded-lg hover:bg-[var(--color-border)]", noteEditing ? "text-indigo-400" : "text-[var(--color-text-muted)]")} title="Düzenle"><FileText className="w-3.5 h-3.5" /></button>
                </div>
              </div>
              {noteEditing ? (
                <div className="space-y-2">
                  <textarea value={noteContent} onChange={e => setNoteContent(e.target.value)} rows={12}
                    className="w-full px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] font-mono resize-none focus:ring-2 focus:ring-indigo-500/40 focus:outline-none" />
                  <div className="flex gap-2">
                    <button onClick={saveNote} disabled={noteSaving} className="px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-medium disabled:opacity-50">
                      {noteSaving ? <Loader2 className="w-3 h-3 animate-spin inline mr-1" /> : null} Kaydet
                    </button>
                    <button onClick={() => { setNoteEditing(false); setNoteContent(releaseNote.content); }} className="px-3 py-1.5 rounded-lg border border-[var(--color-border)] text-xs text-[var(--color-text-muted)]">İptal</button>
                  </div>
                </div>
              ) : (
                <div className="text-xs text-[var(--color-text)] whitespace-pre-wrap bg-[var(--color-bg)] rounded-lg p-4 border border-[var(--color-border)] leading-relaxed font-mono max-h-[400px] overflow-y-auto">
                  {releaseNote.content || "İçerik boş"}
                </div>
              )}
            </>
          )}
        </div>
      )}
    </div>
  );
}
