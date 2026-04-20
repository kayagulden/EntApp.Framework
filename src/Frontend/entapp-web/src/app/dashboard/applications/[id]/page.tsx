"use client";

import { useState, useEffect, useCallback } from "react";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft, AppWindow, Calendar, Code2, Globe, ExternalLink,
  Loader2, CheckCircle2, RotateCcw, AlertTriangle, Archive,
  Clock, Edit3, Save, X, Shield, Monitor, ShoppingCart, Zap,
  ChevronRight, Tag, Link2, GitBranch, Ticket, FolderKanban,
} from "lucide-react";
import { cn } from "@/lib/utils";
import CIRelationshipsPanel from "@/components/ci-relationships-panel";

// ── Types ───────────────────────────────────────────────────

interface ApplicationDetail {
  id: string;
  name: string;
  code: string;
  description?: string;
  applicationType: string;
  status: string;
  criticality: string;
  ownerUserId?: string;
  techLeadUserId?: string;
  technologyStack?: string;
  repositoryUrl?: string;
  documentationUrl?: string;
  currentVersion?: string;
  createdAt: string;
  updatedAt?: string;
  projects?: CIProjectItem[];
}

interface CIProjectItem {
  projectId: string;
  projectKey: string;
  projectName: string;
  projectStatus: string;
  role: string;
  notes?: string;
}

interface RelatedTicket {
  id: { value: string } | string;
  number: string;
  title: string;
  status: string;
  priority: string;
  createdAt: string;
}

// ── Config ──────────────────────────────────────────────────

const TYPE_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  InHouse: { icon: Code2, color: "text-indigo-400", bg: "bg-indigo-500/10 border-indigo-500/20", label: "İç Geliştirme" },
  COTS: { icon: ShoppingCart, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Paket Yazılım" },
  Infrastructure: { icon: Monitor, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Altyapı" },
  Hybrid: { icon: Zap, color: "text-violet-400", bg: "bg-violet-500/10 border-violet-500/20", label: "Hibrit" },
};

const STATUS_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  Planned: { icon: Clock, color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Planlandı" },
  InDevelopment: { icon: RotateCcw, color: "text-cyan-400", bg: "bg-cyan-500/10 border-cyan-500/20", label: "Geliştiriliyor" },
  Active: { icon: CheckCircle2, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Aktif" },
  Deprecated: { icon: AlertTriangle, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Kullanımdan Kaldırılıyor" },
  Retired: { icon: Archive, color: "text-gray-400", bg: "bg-gray-500/10 border-gray-500/20", label: "Emekli" },
};

const CRITICALITY_CONFIG: Record<string, { color: string; bg: string; label: string }> = {
  Low: { color: "text-slate-400", bg: "bg-slate-500/10 border-slate-500/20", label: "Düşük" },
  Medium: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Orta" },
  High: { color: "text-orange-400", bg: "bg-orange-500/10 border-orange-500/20", label: "Yüksek" },
  Critical: { color: "text-red-400", bg: "bg-red-500/10 border-red-500/20", label: "Kritik" },
};

const STATUS_FLOW: Record<string, string[]> = {
  Planned: ["InDevelopment"],
  InDevelopment: ["Active"],
  Active: ["Deprecated"],
  Deprecated: ["Retired", "Active"],
  Retired: [],
};

function formatDate(dateStr?: string): string {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" });
}

// ── Component ───────────────────────────────────────────────

export default function ApplicationDetailPage() {
  const params = useParams();
  const router = useRouter();
  const appId = params?.id as string;

  const [app, setApp] = useState<ApplicationDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [editData, setEditData] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [relatedTickets, setRelatedTickets] = useState<RelatedTicket[]>([]);
  const [ticketsLoading, setTicketsLoading] = useState(false);

  const fetchApp = useCallback(async () => {
    if (!appId) return;
    setLoading(true);
    try {
      const res = await fetch(`/api/pm/applications/${appId}`);
      if (res.ok) setApp(await res.json());
    } catch { /* */ }
    finally { setLoading(false); }
  }, [appId]);

  useEffect(() => { fetchApp(); }, [fetchApp]);

  // Fetch related tickets
  useEffect(() => {
    if (!appId) return;
    setTicketsLoading(true);
    fetch(`/api/req/tickets?configurationItemId=${appId}&pageSize=10`)
      .then((r) => (r.ok ? r.json() : { items: [] }))
      .then((data) => setRelatedTickets(Array.isArray(data?.items) ? data.items : []))
      .catch(() => setRelatedTickets([]))
      .finally(() => setTicketsLoading(false));
  }, [appId]);

  const startEdit = () => {
    if (!app) return;
    setEditData({
      name: app.name,
      description: app.description || "",
      technologyStack: app.technologyStack || "",
      repositoryUrl: app.repositoryUrl || "",
      documentationUrl: app.documentationUrl || "",
      currentVersion: app.currentVersion || "",
      criticality: app.criticality,
    });
    setEditing(true);
  };

  const saveEdit = async () => {
    if (!app) return;
    setSaving(true);
    try {
      const res = await fetch(`/api/pm/applications/${app.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: editData.name || null,
          description: editData.description || null,
          technologyStack: editData.technologyStack || null,
          repositoryUrl: editData.repositoryUrl || null,
          documentationUrl: editData.documentationUrl || null,
          currentVersion: editData.currentVersion || null,
          criticality: editData.criticality || null,
        }),
      });
      if (res.ok) { setEditing(false); fetchApp(); }
    } catch { /* */ }
    finally { setSaving(false); }
  };

  const changeStatus = async (newStatus: string) => {
    if (!app) return;
    try {
      await fetch(`/api/pm/applications/${app.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status: newStatus }),
      });
      fetchApp();
    } catch { /* */ }
  };

  if (loading) return <div className="flex items-center justify-center py-20"><Loader2 className="w-6 h-6 text-cyan-400 animate-spin" /></div>;
  if (!app) return (
    <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]">
      <AppWindow className="w-12 h-12 opacity-30 mb-3" />
      <p className="text-sm">Uygulama bulunamadı</p>
      <button onClick={() => router.back()} className="mt-3 text-xs text-cyan-400 hover:text-cyan-300 transition-colors">← Geri</button>
    </div>
  );

  const typeCfg = TYPE_CONFIG[app.applicationType] || TYPE_CONFIG.InHouse;
  const statusCfg = STATUS_CONFIG[app.status] || STATUS_CONFIG.Planned;
  const critCfg = CRITICALITY_CONFIG[app.criticality] || CRITICALITY_CONFIG.Medium;
  const TypeIcon = typeCfg.icon;
  const StatusIcon = statusCfg.icon;
  const nextStatuses = STATUS_FLOW[app.status] || [];

  return (
    <div className="space-y-5">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-[var(--color-text-muted)]">
        <button onClick={() => router.push("/dashboard/applications")} className="flex items-center gap-1 hover:text-[var(--color-text)] transition-colors">
          <ArrowLeft className="w-4 h-4" /> Uygulamalar
        </button>
        <ChevronRight className="w-3 h-3" />
        <span className="text-[var(--color-text)] font-medium">{app.code}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between">
        <div className="flex items-start gap-4">
          <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-cyan-500 to-blue-600 flex items-center justify-center shadow-lg shadow-cyan-500/30 mt-0.5">
            <AppWindow className="w-6 h-6 text-white" />
          </div>
          <div>
            <div className="flex items-center gap-3 mb-1 flex-wrap">
              <span className="text-sm font-mono font-bold text-cyan-400 bg-cyan-500/10 px-2.5 py-1 rounded-md">{app.code}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", statusCfg.bg, statusCfg.color)}>
                <StatusIcon className="w-3.5 h-3.5" /> {statusCfg.label}
              </span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", typeCfg.bg, typeCfg.color)}>
                <TypeIcon className="w-3.5 h-3.5" /> {typeCfg.label}
              </span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", critCfg.bg, critCfg.color)}>
                <Shield className="w-3.5 h-3.5" /> {critCfg.label}
              </span>
              {app.currentVersion && (
                <span className="text-xs font-mono text-[var(--color-text-muted)] bg-[var(--color-border)] px-2 py-1 rounded">
                  v{app.currentVersion}
                </span>
              )}
            </div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">{app.name}</h1>
            {app.description && <p className="text-sm text-[var(--color-text-muted)] mt-1 max-w-2xl">{app.description}</p>}
          </div>
        </div>
        <div className="flex items-center gap-2">
          {nextStatuses.map(s => {
            const sCfg = STATUS_CONFIG[s] || STATUS_CONFIG.Planned;
            return (
              <button key={s} onClick={() => changeStatus(s)}
                className={cn("flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-medium border transition-all hover:opacity-80", sCfg.bg, sCfg.color)}>
                {sCfg.label}
              </button>
            );
          })}
          {!editing && (
            <button onClick={startEdit} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-border)]/50 transition-all">
              <Edit3 className="w-3.5 h-3.5" /> Düzenle
            </button>
          )}
        </div>
      </div>

      {/* Content */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        <div className="lg:col-span-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-6">
          {editing ? (
            <div className="space-y-4">
              <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3">Uygulama Bilgileri</h3>
              <div>
                <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Ad</label>
                <input type="text" value={editData.name || ""} onChange={e => setEditData(d => ({ ...d, name: e.target.value }))}
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40" />
              </div>
              <div>
                <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Açıklama</label>
                <textarea rows={3} value={editData.description || ""} onChange={e => setEditData(d => ({ ...d, description: e.target.value }))}
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40 resize-none" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Teknoloji Yığını</label>
                  <input type="text" value={editData.technologyStack || ""} onChange={e => setEditData(d => ({ ...d, technologyStack: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Kritiklik</label>
                  <select value={editData.criticality || ""} onChange={e => setEditData(d => ({ ...d, criticality: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    {Object.entries(CRITICALITY_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Repository URL</label>
                  <input type="url" value={editData.repositoryUrl || ""} onChange={e => setEditData(d => ({ ...d, repositoryUrl: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Sürüm</label>
                  <input type="text" value={editData.currentVersion || ""} onChange={e => setEditData(d => ({ ...d, currentVersion: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] font-mono" />
                </div>
              </div>
              <div>
                <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Dokümantasyon URL</label>
                <input type="url" value={editData.documentationUrl || ""} onChange={e => setEditData(d => ({ ...d, documentationUrl: e.target.value }))}
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" />
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button onClick={() => setEditing(false)} className="px-4 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50 transition-colors">
                  <X className="w-4 h-4 inline-block mr-1" />İptal
                </button>
                <button onClick={saveEdit} disabled={saving}
                  className="px-4 py-2 rounded-lg text-sm font-medium bg-cyan-600 text-white hover:bg-cyan-700 disabled:opacity-50 transition-colors flex items-center gap-2">
                  {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />} Kaydet
                </button>
              </div>
            </div>
          ) : (
            <div className="space-y-5">
              <h3 className="text-sm font-semibold text-[var(--color-text)] mb-4">Uygulama Bilgileri</h3>
              <div className="grid grid-cols-2 gap-y-4 gap-x-8">
                <InfoRow icon={AppWindow} label="Kod" value={app.code} mono />
                <InfoRow icon={Calendar} label="Oluşturma" value={formatDate(app.createdAt)} />
                <InfoRow icon={TypeIcon} label="Tip" value={typeCfg.label} badgeColors={typeCfg} />
                <InfoRow icon={Shield} label="Kritiklik" value={critCfg.label} badgeColors={critCfg} />
                {app.technologyStack && <InfoRow icon={Code2} label="Teknoloji" value={app.technologyStack} />}
                {app.currentVersion && <InfoRow icon={Tag} label="Sürüm" value={`v${app.currentVersion}`} mono />}
                {app.repositoryUrl && <InfoRow icon={GitBranch} label="Repository" value={app.repositoryUrl} link />}
                {app.documentationUrl && <InfoRow icon={Globe} label="Dokümantasyon" value={app.documentationUrl} link />}
              </div>
              {app.updatedAt && (
                <p className="text-[10px] text-[var(--color-text-muted)] pt-3 border-t border-[var(--color-border)]">
                  Son güncelleme: {formatDate(app.updatedAt)}
                </p>
              )}
            </div>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          <CIRelationshipsPanel ciId={appId} ciName={app.name} accentColor="cyan" />
          {/* İlgili Projeler */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3 flex items-center gap-2">
              <FolderKanban className="w-4 h-4 text-cyan-400" /> İlgili Projeler
            </h3>
            {(app.projects && app.projects.length > 0) ? (
              <ul className="space-y-2">
                {app.projects.map(p => (
                  <li key={p.projectId}>
                    <button
                      onClick={() => router.push(`/dashboard/projects/${p.projectId}`)}
                      className="w-full text-left p-2.5 rounded-lg bg-[var(--color-bg)] hover:bg-[var(--color-border)] transition-colors group"
                    >
                      <div className="flex items-center justify-between">
                        <span className="text-xs font-mono font-bold text-cyan-400">{p.projectKey}</span>
                        <span className={cn(
                          "text-[10px] px-1.5 py-0.5 rounded-full border font-medium",
                          p.projectStatus === "Active" ? "text-emerald-400 bg-emerald-500/10 border-emerald-500/20" : "text-blue-400 bg-blue-500/10 border-blue-500/20"
                        )}>{p.projectStatus}</span>
                      </div>
                      <p className="text-xs text-[var(--color-text)] mt-1 truncate">{p.projectName}</p>
                      <span className="text-[10px] text-[var(--color-text-muted)]">{p.role}</span>
                    </button>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-xs text-[var(--color-text-muted)]">İlgili proje bulunmuyor.</p>
            )}
          </div>

          {/* İlgili Talepler */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3 flex items-center gap-2">
              <Ticket className="w-4 h-4 text-indigo-400" /> İlgili Talepler
            </h3>
            {ticketsLoading ? (
              <div className="flex justify-center py-4"><Loader2 className="w-4 h-4 text-indigo-400 animate-spin" /></div>
            ) : relatedTickets.length > 0 ? (
              <ul className="space-y-2">
                {relatedTickets.map(t => {
                  const tid = typeof t.id === "object" ? t.id.value : t.id;
                  return (
                    <li key={tid}>
                      <button
                        onClick={() => router.push(`/dashboard/tickets/${tid}`)}
                        className="w-full text-left p-2.5 rounded-lg bg-[var(--color-bg)] hover:bg-[var(--color-border)] transition-colors"
                      >
                        <div className="flex items-center justify-between">
                          <span className="text-xs font-mono font-bold text-indigo-400">{t.number}</span>
                          <span className="text-[10px] text-[var(--color-text-muted)]">{t.status}</span>
                        </div>
                        <p className="text-xs text-[var(--color-text)] mt-1 truncate">{t.title}</p>
                      </button>
                    </li>
                  );
                })}
              </ul>
            ) : (
              <p className="text-xs text-[var(--color-text-muted)]">İlgili talep bulunmuyor.</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Sub-components ──────────────────────────────────────────

function InfoRow({ icon: Icon, label, value, mono, badgeColors, link }: {
  icon: React.ComponentType<{ className?: string }>; label: string; value: string;
  mono?: boolean; badgeColors?: { bg: string; color: string }; link?: boolean;
}) {
  return (
    <div className="flex items-start gap-2.5">
      <Icon className="w-4 h-4 text-[var(--color-text-muted)] mt-0.5 shrink-0" />
      <div className="min-w-0">
        <div className="text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider">{label}</div>
        {badgeColors ? (
          <span className={cn("inline-block mt-0.5 px-2 py-0.5 rounded-full text-xs font-medium border", badgeColors.bg, badgeColors.color)}>{value}</span>
        ) : link ? (
          <a href={value} target="_blank" rel="noopener noreferrer"
            className="text-sm text-cyan-400 hover:text-cyan-300 transition-colors truncate block max-w-[250px]" title={value}>
            {value.replace(/^https?:\/\//, "").split("/").slice(0, 3).join("/")} <ExternalLink className="w-3 h-3 inline-block ml-1" />
          </a>
        ) : (
          <div className={cn("text-sm text-[var(--color-text)]", mono && "font-mono font-bold text-cyan-400")}>{value}</div>
        )}
      </div>
    </div>
  );
}
