"use client";
import { useState, useEffect, useCallback } from "react";
import { Plus, Edit3, Save, Trash2, Loader2, X, ChevronDown, ChevronRight, ClipboardList } from "lucide-react";
import { cn } from "@/lib/utils";

interface RequirementItem {
  id: string; key: string; title: string; type: string; priority: string; status: string;
  parentRequirementId?: string | null; childCount: number; workItemCount: number; sortOrder: number; createdAt: string;
}
interface RequirementDetail extends RequirementItem {
  description?: string; acceptanceCriteria?: string; sourceTicketId?: string | null;
  sourceTicketNumber?: string | null; externalDesignUrl?: string | null;
  children?: RequirementItem[];
  workItems?: { id: string; workItemNumber: string; title: string; status: string; type: string; priority: string; assigneeUserId?: string | null }[];
}

const REQ_TYPE_CONFIG: Record<string, { label: string; color: string; icon: string }> = {
  FeatureSpec: { label: "Feature Spec", color: "bg-purple-500/15 text-purple-400 border-purple-500/30", icon: "📋" },
  Functional: { label: "Fonksiyonel", color: "bg-blue-500/15 text-blue-400 border-blue-500/30", icon: "⚙️" },
  NonFunctional: { label: "Fonk. Dışı", color: "bg-orange-500/15 text-orange-400 border-orange-500/30", icon: "🔒" },
  Interface: { label: "Arayüz", color: "bg-cyan-500/15 text-cyan-400 border-cyan-500/30", icon: "🔌" },
  Constraint: { label: "Kısıt", color: "bg-red-500/15 text-red-400 border-red-500/30", icon: "⛔" },
};
const REQ_PRIORITY_CONFIG: Record<string, { label: string; color: string }> = {
  Must: { label: "Must", color: "bg-red-500/15 text-red-400" }, Should: { label: "Should", color: "bg-amber-500/15 text-amber-400" },
  Could: { label: "Could", color: "bg-green-500/15 text-green-400" }, WontHave: { label: "Won't", color: "bg-slate-500/15 text-slate-400" },
};
const REQ_STATUS_CONFIG: Record<string, { label: string; color: string; icon: string }> = {
  Draft: { label: "Taslak", color: "bg-slate-500/15 text-slate-400 border-slate-500/30", icon: "📝" },
  InReview: { label: "İnceleme", color: "bg-blue-500/15 text-blue-400 border-blue-500/30", icon: "🔍" },
  Approved: { label: "Onaylı", color: "bg-green-500/15 text-green-400 border-green-500/30", icon: "✅" },
  Implemented: { label: "İmplemente", color: "bg-purple-500/15 text-purple-400 border-purple-500/30", icon: "🚀" },
  Verified: { label: "Doğrulandı", color: "bg-emerald-500/15 text-emerald-400 border-emerald-500/30", icon: "🏆" },
};

function DesignPreview({ url }: { url: string }) {
  const [expanded, setExpanded] = useState(false);
  if (url.includes("figma.com")) {
    let embedUrl = url;
    try {
      const parsed = new URL(url.startsWith("http") ? url : "https://" + url);
      const path = parsed.pathname + parsed.search + parsed.hash;
      if (path.startsWith("/file/")) embedUrl = `https://embed.figma.com/design${path.slice(5)}`;
      else if (path.startsWith("/design/")) embedUrl = `https://embed.figma.com/design${path.slice(7)}`;
      else if (path.startsWith("/proto/")) embedUrl = `https://embed.figma.com/proto${path.slice(6)}`;
      else embedUrl = `https://embed.figma.com${path}`;
    } catch { embedUrl = url; }
    if (!embedUrl.startsWith("https://")) embedUrl = "https://" + embedUrl;
    return (
      <div className="relative group">
        <iframe src={embedUrl} className={cn("w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-secondary)] transition-all", expanded ? "h-[600px]" : "h-[300px]")} allowFullScreen />
        <div className="absolute top-2 right-2 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <button onClick={() => setExpanded(!expanded)} className="px-2 py-1 rounded bg-black/60 text-white text-[10px] hover:bg-black/80 backdrop-blur-sm">{expanded ? "Küçült" : "Büyüt"}</button>
        </div>
      </div>
    );
  }
  if (url.includes("balsamiq.cloud")) {
    let embedUrl = url;
    if (!embedUrl.startsWith("https://")) embedUrl = "https://" + embedUrl;
    return (
      <div className="relative group">
        <iframe src={embedUrl} className={cn("w-full rounded-lg border border-[var(--color-border)] bg-white transition-all", expanded ? "h-[600px]" : "h-[300px]")} allowFullScreen />
        <div className="absolute top-2 right-2 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <button onClick={() => setExpanded(!expanded)} className="px-2 py-1 rounded bg-black/60 text-white text-[10px] hover:bg-black/80 backdrop-blur-sm">{expanded ? "Küçült" : "Büyüt"}</button>
        </div>
      </div>
    );
  }
  return (
    <a href={url} target="_blank" rel="noopener noreferrer"
      className="flex items-center gap-2 px-3 py-2 rounded-lg bg-[var(--color-bg-secondary)] border border-[var(--color-border)] text-sm text-blue-400 hover:bg-[var(--color-bg-tertiary)] transition-colors">
      🎨 Harici Tasarım Aracı →
    </a>
  );
}

export default function RequirementsTab({ projectId, projectKey }: { projectId: string; projectKey: string }) {
  const [requirements, setRequirements] = useState<RequirementItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedReq, setSelectedReq] = useState<RequirementDetail | null>(null);
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());
  const [childrenMap, setChildrenMap] = useState<Record<string, RequirementItem[]>>({});
  const [showForm, setShowForm] = useState(false);
  const [formParentId, setFormParentId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState({ title: "", type: "Functional", priority: "Must", description: "", acceptanceCriteria: "", externalDesignUrl: "" });
  const API = `http://localhost:5212`;

  const fetchRequirements = useCallback(async () => {
    setLoading(true);
    try { const res = await fetch(`${API}/api/pm/projects/${projectId}/requirements`); if (res.ok) setRequirements(await res.json()); }
    finally { setLoading(false); }
  }, [projectId]);

  useEffect(() => { fetchRequirements(); }, [fetchRequirements]);

  const fetchChildren = async (parentId: string) => {
    const res = await fetch(`${API}/api/pm/projects/${projectId}/requirements?parentId=${parentId}`);
    if (res.ok) { const data = await res.json(); setChildrenMap(prev => ({ ...prev, [parentId]: data })); }
  };

  const fetchDetail = async (id: string) => { const res = await fetch(`${API}/api/pm/requirements/${id}`); if (res.ok) setSelectedReq(await res.json()); };

  const toggleExpand = (id: string) => {
    const next = new Set(expandedIds);
    if (next.has(id)) next.delete(id); else { next.add(id); fetchChildren(id); }
    setExpandedIds(next);
  };

  const handleCreate = async () => {
    setSaving(true);
    try {
      const body: Record<string, unknown> = { title: form.title, type: form.type, priority: form.priority };
      if (form.description) body.description = form.description;
      if (form.acceptanceCriteria) body.acceptanceCriteria = form.acceptanceCriteria;
      if (form.externalDesignUrl) body.externalDesignUrl = form.externalDesignUrl;
      if (formParentId) body.parentRequirementId = formParentId;
      const res = await fetch(`${API}/api/pm/projects/${projectId}/requirements`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      if (res.ok) { setShowForm(false); setForm({ title: "", type: "Functional", priority: "Must", description: "", acceptanceCriteria: "", externalDesignUrl: "" }); setFormParentId(null); fetchRequirements(); if (formParentId) fetchChildren(formParentId); }
    } finally { setSaving(false); }
  };

  const handleUpdate = async () => {
    if (!editingId) return; setSaving(true);
    try {
      const body: Record<string, unknown> = {};
      if (form.title) body.title = form.title; if (form.type) body.type = form.type; if (form.priority) body.priority = form.priority;
      if (form.description) body.description = form.description; if (form.acceptanceCriteria) body.acceptanceCriteria = form.acceptanceCriteria;
      if (form.externalDesignUrl) body.externalDesignUrl = form.externalDesignUrl;
      const res = await fetch(`${API}/api/pm/requirements/${editingId}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      if (res.ok) { setEditingId(null); setShowForm(false); fetchRequirements(); if (selectedReq?.id === editingId) fetchDetail(editingId); }
    } finally { setSaving(false); }
  };

  const handleDelete = async (id: string) => {
    if (!confirm("Bu gereksinim ve alt gereksinimleri silinecek. Emin misiniz?")) return;
    await fetch(`${API}/api/pm/requirements/${id}`, { method: "DELETE" }); if (selectedReq?.id === id) setSelectedReq(null); fetchRequirements();
  };

  const handleStatusChange = async (id: string, status: string) => {
    await fetch(`${API}/api/pm/requirements/${id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status }) });
    fetchRequirements(); if (selectedReq?.id === id) fetchDetail(id);
  };

  const startEdit = (req: RequirementDetail) => {
    setEditingId(req.id);
    setForm({ title: req.title, type: req.type, priority: req.priority, description: req.description || "", acceptanceCriteria: req.acceptanceCriteria || "", externalDesignUrl: req.externalDesignUrl || "" });
    setShowForm(true);
  };

  const startAddChild = (parentId: string) => {
    setFormParentId(parentId); setEditingId(null);
    setForm({ title: "", type: "Functional", priority: "Must", description: "", acceptanceCriteria: "", externalDesignUrl: "" }); setShowForm(true);
  };

  const renderReqRow = (req: RequirementItem, indent: number = 0) => {
    const typeConf = REQ_TYPE_CONFIG[req.type] || REQ_TYPE_CONFIG.Functional;
    const priConf = REQ_PRIORITY_CONFIG[req.priority] || REQ_PRIORITY_CONFIG.Must;
    const statusConf = REQ_STATUS_CONFIG[req.status] || REQ_STATUS_CONFIG.Draft;
    const isExpanded = expandedIds.has(req.id);
    const hasChildren = req.childCount > 0;
    const isSelected = selectedReq?.id === req.id;
    return (
      <div key={req.id}>
        <div className={cn("group flex items-center gap-2 px-3 py-2.5 rounded-lg cursor-pointer transition-all border border-transparent",
          isSelected ? "bg-[var(--color-primary)]/10 border-[var(--color-primary)]/30" : "hover:bg-[var(--color-bg-secondary)]")}
          style={{ paddingLeft: `${12 + indent * 24}px` }} onClick={() => fetchDetail(req.id)}>
          <button onClick={(e) => { e.stopPropagation(); if (hasChildren) toggleExpand(req.id); }}
            className={cn("w-5 h-5 flex items-center justify-center rounded transition-colors", hasChildren ? "hover:bg-[var(--color-bg-tertiary)]" : "invisible")}>
            {hasChildren && (isExpanded ? <ChevronDown className="w-3.5 h-3.5" /> : <ChevronRight className="w-3.5 h-3.5" />)}
          </button>
          <span className={cn("px-2 py-0.5 rounded text-[10px] font-semibold border", typeConf.color)}>{typeConf.icon} {typeConf.label}</span>
          <span className="text-xs font-mono text-[var(--color-text-muted)] w-20 shrink-0">{req.key}</span>
          <span className="flex-1 text-sm font-medium truncate">{req.title}</span>
          <span className={cn("px-1.5 py-0.5 rounded text-[10px] font-bold", priConf.color)}>{priConf.label}</span>
          <span className={cn("px-2 py-0.5 rounded text-[10px] font-semibold border", statusConf.color)}>{statusConf.icon} {statusConf.label}</span>
          {req.childCount > 0 && <span className="text-[10px] text-[var(--color-text-muted)] bg-[var(--color-bg-tertiary)] px-1.5 py-0.5 rounded">{req.childCount} alt</span>}
          {req.workItemCount > 0 && <span className="text-[10px] text-blue-400 bg-blue-500/10 px-1.5 py-0.5 rounded">{req.workItemCount} iş</span>}
          <div className="opacity-0 group-hover:opacity-100 flex gap-1 transition-opacity">
            <button onClick={(e) => { e.stopPropagation(); startAddChild(req.id); }} className="p-1 rounded hover:bg-[var(--color-bg-tertiary)]" title="Alt gereksinim ekle"><Plus className="w-3.5 h-3.5 text-green-400" /></button>
            <button onClick={(e) => { e.stopPropagation(); handleDelete(req.id); }} className="p-1 rounded hover:bg-red-500/10" title="Sil"><Trash2 className="w-3.5 h-3.5 text-red-400" /></button>
          </div>
        </div>
        {isExpanded && childrenMap[req.id]?.map(child => renderReqRow(child, indent + 1))}
      </div>
    );
  };

  return (
    <div className="flex gap-4 h-[calc(100vh-280px)]">
      <div className="flex-1 flex flex-col min-w-0">
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-lg font-semibold">Gereksinimler</h3>
          <button onClick={() => { setFormParentId(null); setEditingId(null); setForm({ title: "", type: "FeatureSpec", priority: "Must", description: "", acceptanceCriteria: "", externalDesignUrl: "" }); setShowForm(true); }}
            className="px-3 py-1.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:bg-[var(--color-primary-hover)] transition-colors flex items-center gap-1.5">
            <Plus className="w-4 h-4" /> Yeni Gereksinim
          </button>
        </div>
        {showForm && (
          <div className="mb-4 p-4 rounded-xl bg-[var(--color-bg-secondary)] border border-[var(--color-border)]">
            <div className="flex items-center justify-between mb-3">
              <h4 className="text-sm font-semibold">{editingId ? "Gereksinim Düzenle" : formParentId ? "Alt Gereksinim Ekle" : "Yeni Gereksinim"}</h4>
              <button onClick={() => { setShowForm(false); setEditingId(null); }} className="p-1 rounded hover:bg-[var(--color-bg-tertiary)]"><X className="w-4 h-4" /></button>
            </div>
            <div className="grid grid-cols-3 gap-3 mb-3">
              <input placeholder="Başlık *" value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} className="col-span-3 px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm" />
              <select value={form.type} onChange={e => setForm(f => ({ ...f, type: e.target.value }))} className="px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm">
                <option value="FeatureSpec">Feature Spec</option><option value="Functional">Fonksiyonel</option><option value="NonFunctional">Fonk. Dışı</option><option value="Interface">Arayüz</option><option value="Constraint">Kısıt</option>
              </select>
              <select value={form.priority} onChange={e => setForm(f => ({ ...f, priority: e.target.value }))} className="px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm">
                <option value="Must">Must Have</option><option value="Should">Should Have</option><option value="Could">Could Have</option><option value="WontHave">Won&apos;t Have</option>
              </select>
              <input placeholder="Tasarım URL (Figma/Miro)" value={form.externalDesignUrl} onChange={e => setForm(f => ({ ...f, externalDesignUrl: e.target.value }))} className="px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm" />
            </div>
            <textarea placeholder="Açıklama (Markdown)" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} rows={3} className="w-full px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm mb-3 resize-none" />
            <textarea placeholder="Kabul Kriterleri" value={form.acceptanceCriteria} onChange={e => setForm(f => ({ ...f, acceptanceCriteria: e.target.value }))} rows={2} className="w-full px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm mb-3 resize-none" />
            <button onClick={editingId ? handleUpdate : handleCreate} disabled={saving || !form.title.trim()}
              className="px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:bg-[var(--color-primary-hover)] disabled:opacity-50 transition-colors flex items-center gap-2">
              {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}{editingId ? "Güncelle" : "Oluştur"}
            </button>
          </div>
        )}
        <div className="flex-1 overflow-y-auto space-y-1">
          {loading ? <div className="flex items-center justify-center py-12"><Loader2 className="w-6 h-6 animate-spin text-[var(--color-primary)]" /></div>
          : requirements.length === 0 ? (
            <div className="text-center py-12 text-[var(--color-text-muted)]">
              <ClipboardList className="w-12 h-12 mx-auto mb-3 opacity-30" /><p className="text-sm">Henüz gereksinim yok</p>
            </div>
          ) : requirements.map(req => renderReqRow(req))}
        </div>
      </div>
      {selectedReq && (
        <div className="w-[420px] shrink-0 border-l border-[var(--color-border)] pl-4 overflow-y-auto">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <span className="text-xs font-mono text-[var(--color-text-muted)] bg-[var(--color-bg-tertiary)] px-2 py-1 rounded">{selectedReq.key}</span>
              <span className={cn("px-2 py-0.5 rounded text-[10px] font-semibold border", (REQ_TYPE_CONFIG[selectedReq.type] || REQ_TYPE_CONFIG.Functional).color)}>
                {(REQ_TYPE_CONFIG[selectedReq.type] || REQ_TYPE_CONFIG.Functional).label}
              </span>
            </div>
            <div className="flex gap-1">
              <button onClick={() => startEdit(selectedReq)} className="p-1.5 rounded-lg hover:bg-[var(--color-bg-secondary)]"><Edit3 className="w-4 h-4" /></button>
              <button onClick={() => setSelectedReq(null)} className="p-1.5 rounded-lg hover:bg-[var(--color-bg-secondary)]"><X className="w-4 h-4" /></button>
            </div>
          </div>
          <h3 className="text-lg font-semibold mb-3">{selectedReq.title}</h3>
          <div className="flex gap-2 mb-4">
            <select value={selectedReq.status} onChange={e => handleStatusChange(selectedReq.id, e.target.value)}
              className="px-2 py-1 rounded-lg bg-[var(--color-bg-secondary)] border border-[var(--color-border)] text-xs">
              <option value="Draft">📝 Taslak</option><option value="InReview">🔍 İnceleme</option><option value="Approved">✅ Onaylı</option><option value="Implemented">🚀 İmplemente</option><option value="Verified">🏆 Doğrulandı</option>
            </select>
            <span className={cn("px-2 py-1 rounded text-xs font-bold", (REQ_PRIORITY_CONFIG[selectedReq.priority] || REQ_PRIORITY_CONFIG.Must).color)}>
              {(REQ_PRIORITY_CONFIG[selectedReq.priority] || REQ_PRIORITY_CONFIG.Must).label}
            </span>
          </div>
          {selectedReq.sourceTicketNumber && (
            <div className="mb-3 px-3 py-2 rounded-lg bg-amber-500/10 border border-amber-500/20 text-xs"><span className="text-amber-400 font-medium">Kaynak Talep:</span> {selectedReq.sourceTicketNumber}</div>
          )}
          {selectedReq.externalDesignUrl && (
            <div className="mb-4">
              <div className="flex items-center justify-between mb-2">
                <h4 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Tasarım</h4>
                <a href={selectedReq.externalDesignUrl} target="_blank" rel="noopener noreferrer" className="text-[10px] text-blue-400 hover:underline">Yeni sekmede aç →</a>
              </div>
              <DesignPreview url={selectedReq.externalDesignUrl} />
            </div>
          )}
          {selectedReq.description && (
            <div className="mb-4"><h4 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-2">Açıklama</h4>
              <div className="text-sm leading-relaxed whitespace-pre-wrap bg-[var(--color-bg-secondary)] rounded-lg p-3 border border-[var(--color-border)]">{selectedReq.description}</div>
            </div>
          )}
          {selectedReq.acceptanceCriteria && (
            <div className="mb-4"><h4 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-2">Kabul Kriterleri</h4>
              <div className="text-sm leading-relaxed whitespace-pre-wrap bg-green-500/5 rounded-lg p-3 border border-green-500/20">{selectedReq.acceptanceCriteria}</div>
            </div>
          )}
          {selectedReq.children && selectedReq.children.length > 0 && (
            <div className="mb-4"><h4 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-2">Alt Gereksinimler ({selectedReq.children.length})</h4>
              <div className="space-y-1">{selectedReq.children.map(child => {
                const cStatus = REQ_STATUS_CONFIG[child.status] || REQ_STATUS_CONFIG.Draft;
                return (
                  <div key={child.id} onClick={() => fetchDetail(child.id)} className="flex items-center gap-2 px-2 py-1.5 rounded-lg hover:bg-[var(--color-bg-secondary)] cursor-pointer">
                    <span className="text-[10px] font-mono text-[var(--color-text-muted)]">{child.key}</span>
                    <span className="text-xs flex-1 truncate">{child.title}</span>
                    <span className={cn("px-1.5 py-0.5 rounded text-[9px] border", cStatus.color)}>{cStatus.icon}</span>
                  </div>
                );
              })}</div>
            </div>
          )}
          {selectedReq.workItems && selectedReq.workItems.length > 0 && (
            <div className="mb-4"><h4 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-2">Bağlı İş Kalemleri ({selectedReq.workItems.length})</h4>
              <div className="space-y-1">{selectedReq.workItems.map(wi => (
                <div key={wi.id} className="flex items-center gap-2 px-2 py-1.5 rounded-lg bg-[var(--color-bg-secondary)]">
                  <span className="text-[10px] font-mono text-blue-400">{wi.workItemNumber}</span>
                  <span className="text-xs flex-1 truncate">{wi.title}</span>
                  <span className="text-[9px] text-[var(--color-text-muted)]">{wi.status}</span>
                </div>
              ))}</div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
