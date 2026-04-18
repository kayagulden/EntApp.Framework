"use client";

import { useState, useEffect, useCallback } from "react";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft, FolderKanban, Calendar, BarChart3, User, GitBranch,
  Loader2, CheckCircle2, PauseCircle, RotateCcw, AlertCircle,
  Briefcase, Edit3, Save, X, ListTodo, Layout, Milestone, Archive,
  ChevronRight, Monitor, ShoppingCart, Building2, Tag,
} from "lucide-react";
import { cn } from "@/lib/utils";

// ── Types ───────────────────────────────────────────────────

interface ProjectDetail {
  id: string;
  key: string;
  name: string;
  description?: string;
  status: string;
  methodology: string;
  category: string;
  startDate?: string;
  endDate?: string;
  targetEndDate?: string;
  managerUserId?: string;
  ownerUserId?: string;
  portfolioId?: string;
  portfolioName?: string;
  portfolioCode?: string;
  taskCount: number;
  taskSequence: number;
  createdAt: string;
  updatedAt?: string;
}

interface PortfolioOption { id: string; name: string; code: string; }

// ── Config ──────────────────────────────────────────────────

const STATUS_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  Planning: { icon: RotateCcw, color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Planlama" },
  Active: { icon: CheckCircle2, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Aktif" },
  OnHold: { icon: PauseCircle, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Beklemede" },
  Completed: { icon: CheckCircle2, color: "text-teal-400", bg: "bg-teal-500/10 border-teal-500/20", label: "Tamamlandı" },
  Cancelled: { icon: AlertCircle, color: "text-gray-400", bg: "bg-gray-500/10 border-gray-500/20", label: "İptal" },
};

const METHODOLOGY_CONFIG: Record<string, { color: string; bg: string; label: string }> = {
  Kanban: { color: "text-cyan-400", bg: "bg-cyan-500/10 border-cyan-500/20", label: "Kanban" },
  Scrum: { color: "text-violet-400", bg: "bg-violet-500/10 border-violet-500/20", label: "Scrum" },
  ScrumBan: { color: "text-orange-400", bg: "bg-orange-500/10 border-orange-500/20", label: "ScrumBan" },
  Waterfall: { color: "text-sky-400", bg: "bg-sky-500/10 border-sky-500/20", label: "Waterfall" },
};

const CATEGORY_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  General: { icon: Tag, color: "text-slate-400", bg: "bg-slate-500/10 border-slate-500/20", label: "Genel" },
  SoftwareDevelopment: { icon: FolderKanban, color: "text-indigo-400", bg: "bg-indigo-500/10 border-indigo-500/20", label: "Yazılım Geliştirme" },
  Infrastructure: { icon: Monitor, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Altyapı" },
  Procurement: { icon: ShoppingCart, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Tedarik" },
  Business: { icon: Building2, color: "text-rose-400", bg: "bg-rose-500/10 border-rose-500/20", label: "İş / Organizasyonel" },
};

const STATUS_FLOW: Record<string, string[]> = {
  Planning: ["Active", "Cancelled"],
  Active: ["OnHold", "Completed", "Cancelled"],
  OnHold: ["Active", "Cancelled"],
  Completed: [],
  Cancelled: [],
};

function formatDate(dateStr?: string): string {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" });
}

function formatDateShort(dateStr?: string): string {
  if (!dateStr) return "";
  return new Date(dateStr).toISOString().split("T")[0];
}

// Tab config — kategori bazlı filtreleme
type TabKey = "overview" | "backlog" | "board" | "tasks" | "milestones";
const ALL_TABS: { key: TabKey; label: string; icon: React.ComponentType<{ className?: string }>; categories: string[]; disabled?: boolean }[] = [
  { key: "overview", label: "Genel Bakış", icon: FolderKanban, categories: ["all"] },
  { key: "backlog", label: "Backlog", icon: ListTodo, categories: ["SoftwareDevelopment"], disabled: true },
  { key: "board", label: "Board", icon: Layout, categories: ["SoftwareDevelopment", "General"], disabled: true },
  { key: "tasks", label: "Görevler", icon: BarChart3, categories: ["all"], disabled: true },
  { key: "milestones", label: "Milestones", icon: Milestone, categories: ["Infrastructure", "Procurement", "Business", "SoftwareDevelopment"], disabled: true },
];

// ── Component ───────────────────────────────────────────────

export default function ProjectDetailPage() {
  const params = useParams();
  const router = useRouter();
  const projectId = params?.id as string;

  const [project, setProject] = useState<ProjectDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<TabKey>("overview");
  const [portfolios, setPortfolios] = useState<PortfolioOption[]>([]);

  // Edit mode
  const [editing, setEditing] = useState(false);
  const [editData, setEditData] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);

  const fetchProject = useCallback(async () => {
    if (!projectId) return;
    setLoading(true);
    try {
      const res = await fetch(`/api/pm/projects/${projectId}`);
      if (res.ok) setProject(await res.json());
    } catch { /* */ }
    finally { setLoading(false); }
  }, [projectId]);

  const fetchPortfolios = useCallback(async () => {
    try {
      const res = await fetch("/api/pm/portfolios");
      if (res.ok) setPortfolios(await res.json());
    } catch { /* */ }
  }, []);

  useEffect(() => { fetchProject(); fetchPortfolios(); }, [fetchProject, fetchPortfolios]);

  // Start editing
  const startEdit = () => {
    if (!project) return;
    setEditData({
      name: project.name,
      description: project.description || "",
      startDate: formatDateShort(project.startDate),
      targetEndDate: formatDateShort(project.targetEndDate),
      methodology: project.methodology,
      portfolioId: project.portfolioId || "",
    });
    setEditing(true);
  };

  // Save edit
  const saveEdit = async () => {
    if (!project) return;
    setSaving(true);
    try {
      const res = await fetch(`/api/pm/projects/${project.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: editData.name || null,
          description: editData.description || null,
          startDate: editData.startDate || null,
          targetEndDate: editData.targetEndDate || null,
          methodology: editData.methodology || null,
          portfolioId: editData.portfolioId || null,
        }),
      });
      if (res.ok) { setEditing(false); fetchProject(); }
    } catch { /* */ }
    finally { setSaving(false); }
  };

  // Status change
  const changeStatus = async (newStatus: string) => {
    if (!project) return;
    try {
      await fetch(`/api/pm/projects/${project.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status: newStatus }),
      });
      fetchProject();
    } catch { /* */ }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="w-6 h-6 text-indigo-400 animate-spin" />
      </div>
    );
  }

  if (!project) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]">
        <FolderKanban className="w-12 h-12 opacity-30 mb-3" />
        <p className="text-sm">Proje bulunamadı</p>
        <button onClick={() => router.back()} className="mt-3 text-xs text-indigo-400 hover:text-indigo-300 transition-colors">← Geri</button>
      </div>
    );
  }

  const statusCfg = STATUS_CONFIG[project.status] || STATUS_CONFIG.Planning;
  const methodCfg = METHODOLOGY_CONFIG[project.methodology] || METHODOLOGY_CONFIG.Kanban;
  const catCfg = CATEGORY_CONFIG[project.category] || CATEGORY_CONFIG.General;
  const StatusIcon = statusCfg.icon;
  const CatIcon = catCfg.icon;
  const nextStatuses = STATUS_FLOW[project.status] || [];

  // Kategori bazlı tab filtreleme
  const visibleTabs = ALL_TABS.filter(t =>
    t.categories.includes("all") || t.categories.includes(project.category)
  );

  return (
    <div className="space-y-5">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-[var(--color-text-muted)]">
        <button onClick={() => router.push("/dashboard/projects")}
          className="flex items-center gap-1 hover:text-[var(--color-text)] transition-colors">
          <ArrowLeft className="w-4 h-4" /> Projeler
        </button>
        <ChevronRight className="w-3 h-3" />
        <span className="text-[var(--color-text)] font-medium">{project.key}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between">
        <div className="flex items-start gap-4">
          <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center shadow-lg shadow-indigo-500/30 mt-0.5">
            <FolderKanban className="w-6 h-6 text-white" />
          </div>
          <div>
            <div className="flex items-center gap-3 mb-1">
              <span className="text-sm font-mono font-bold text-indigo-400 bg-indigo-500/10 px-2.5 py-1 rounded-md">{project.key}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", statusCfg.bg, statusCfg.color)}>
                <StatusIcon className="w-3.5 h-3.5" /> {statusCfg.label}
              </span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", methodCfg.bg, methodCfg.color)}>
                {methodCfg.label}
              </span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", catCfg.bg, catCfg.color)}>
                <CatIcon className="w-3.5 h-3.5" /> {catCfg.label}
              </span>
              {project.portfolioName && (
                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border border-violet-500/20 bg-violet-500/10 text-violet-400">
                  <Briefcase className="w-3 h-3" /> {project.portfolioName}
                </span>
              )}
            </div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">{project.name}</h1>
            {project.description && (
              <p className="text-sm text-[var(--color-text-muted)] mt-1 max-w-2xl">{project.description}</p>
            )}
          </div>
        </div>
        <div className="flex items-center gap-2">
          {nextStatuses.map(s => {
            const sCfg = STATUS_CONFIG[s] || STATUS_CONFIG.Planning;
            return (
              <button key={s} onClick={() => changeStatus(s)}
                className={cn("flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-medium border transition-all hover:opacity-80", sCfg.bg, sCfg.color)}>
                {sCfg.label}
              </button>
            );
          })}
          {!editing && (
            <button onClick={startEdit}
              className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-border)]/50 transition-all">
              <Edit3 className="w-3.5 h-3.5" /> Düzenle
            </button>
          )}
        </div>
      </div>

      {/* Tabs — kategori bazlı */}
      <div className="flex items-center gap-1 border-b border-[var(--color-border)]">
        {visibleTabs.map(tab => {
          const Icon = tab.icon;
          return (
            <button key={tab.key}
              disabled={tab.disabled}
              onClick={() => !tab.disabled && setActiveTab(tab.key)}
              className={cn(
                "flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-all",
                tab.disabled
                  ? "opacity-30 cursor-not-allowed border-transparent text-[var(--color-text-muted)]"
                  : activeTab === tab.key
                    ? "border-indigo-500 text-indigo-400"
                    : "border-transparent text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:border-[var(--color-border)]"
              )}>
              <Icon className="w-4 h-4" /> {tab.label}
              {tab.disabled && <span className="text-[9px] font-semibold px-1.5 py-0.5 rounded-full bg-[var(--color-border)] text-[var(--color-text-muted)]">Yakında</span>}
            </button>
          );
        })}
      </div>

      {/* Tab Content */}
      {activeTab === "overview" && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
          {/* Info Card */}
          <div className="lg:col-span-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-6">
            {editing ? (
              /* ── Edit Mode ─── */
              <div className="space-y-4">
                <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3">Proje Bilgileri</h3>
                <div>
                  <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Proje Adı</label>
                  <input type="text" value={editData.name || ""} onChange={e => setEditData(d => ({ ...d, name: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Açıklama</label>
                  <textarea rows={3} value={editData.description || ""} onChange={e => setEditData(d => ({ ...d, description: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40 resize-none" />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Metodoloji</label>
                    <select value={editData.methodology || ""} onChange={e => setEditData(d => ({ ...d, methodology: e.target.value }))}
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                      <option value="Kanban">Kanban</option><option value="Scrum">Scrum</option><option value="ScrumBan">ScrumBan</option><option value="Waterfall">Waterfall</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Portfolyo</label>
                    <select value={editData.portfolioId || ""} onChange={e => setEditData(d => ({ ...d, portfolioId: e.target.value }))}
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                      <option value="">Portfolyo yok</option>
                      {portfolios.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
                    </select>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Başlangıç Tarihi</label>
                    <input type="date" value={editData.startDate || ""} onChange={e => setEditData(d => ({ ...d, startDate: e.target.value }))}
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Hedef Bitiş</label>
                    <input type="date" value={editData.targetEndDate || ""} onChange={e => setEditData(d => ({ ...d, targetEndDate: e.target.value }))}
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" />
                  </div>
                </div>
                <div className="flex justify-end gap-2 pt-2">
                  <button onClick={() => setEditing(false)} className="px-4 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50 transition-colors">
                    <X className="w-4 h-4 inline-block mr-1" />İptal
                  </button>
                  <button onClick={saveEdit} disabled={saving}
                    className="px-4 py-2 rounded-lg text-sm font-medium bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors flex items-center gap-2">
                    {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />} Kaydet
                  </button>
                </div>
              </div>
            ) : (
              /* ── View Mode ─── */
              <div className="space-y-5">
                <h3 className="text-sm font-semibold text-[var(--color-text)] mb-4">Proje Bilgileri</h3>
                <div className="grid grid-cols-2 gap-y-4 gap-x-8">
                  <InfoRow icon={FolderKanban} label="Anahtar" value={project.key} mono />
                  <InfoRow icon={Calendar} label="Oluşturma" value={formatDate(project.createdAt)} />
                  <InfoRow icon={Calendar} label="Başlangıç" value={formatDate(project.startDate)} />
                  <InfoRow icon={Calendar} label="Hedef Bitiş" value={formatDate(project.targetEndDate)} />
                  <InfoRow icon={GitBranch} label="Metodoloji" value={methodCfg.label} badgeColors={methodCfg} />
                  <InfoRow icon={CatIcon} label="Kategori" value={catCfg.label} badgeColors={catCfg} />
                  <InfoRow icon={Briefcase} label="Portfolyo" value={project.portfolioName || "—"} />
                  <InfoRow icon={BarChart3} label="Toplam Görev" value={String(project.taskCount)} />
                  <InfoRow icon={BarChart3} label="Görev Sayacı" value={`#${project.taskSequence}`} />
                </div>
                {project.updatedAt && (
                  <p className="text-[10px] text-[var(--color-text-muted)] pt-3 border-t border-[var(--color-border)]">
                    Son güncelleme: {formatDate(project.updatedAt)}
                  </p>
                )}
              </div>
            )}
          </div>

          {/* Stats Sidebar */}
          <div className="space-y-4">
            {/* Quick Stats */}
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5 space-y-4">
              <h3 className="text-sm font-semibold text-[var(--color-text)]">Hızlı İstatistikler</h3>
              <div className="grid grid-cols-2 gap-3">
                <StatBox icon={BarChart3} label="Görevler" value={project.taskCount} color="indigo" />
                <StatBox icon={GitBranch} label="Sıra" value={project.taskSequence} color="violet" />
              </div>
            </div>

            {/* Future tabs info */}
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
              <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3">Yakında</h3>
              <ul className="space-y-3">
                {[
                  { icon: ListTodo, label: "Backlog Yönetimi", desc: "Epic → Story → Task hiyerarşisi" },
                  { icon: Layout, label: "Kanban Board", desc: "Drag & drop görev yönetimi" },
                  { icon: Milestone, label: "Milestones", desc: "Proje kilometre taşları" },
                ].map(item => (
                  <li key={item.label} className="flex items-start gap-3">
                    <div className="w-8 h-8 rounded-lg bg-[var(--color-border)] flex items-center justify-center mt-0.5 shrink-0">
                      <item.icon className="w-4 h-4 text-[var(--color-text-muted)]" />
                    </div>
                    <div>
                      <span className="text-sm font-medium text-[var(--color-text)]">{item.label}</span>
                      <p className="text-[11px] text-[var(--color-text-muted)]">{item.desc}</p>
                    </div>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Sub-components ──────────────────────────────────────────

function InfoRow({ icon: Icon, label, value, mono, badgeColors }: {
  icon: React.ComponentType<{ className?: string }>; label: string; value: string;
  mono?: boolean; badgeColors?: { bg: string; color: string };
}) {
  return (
    <div className="flex items-start gap-2.5">
      <Icon className="w-4 h-4 text-[var(--color-text-muted)] mt-0.5 shrink-0" />
      <div>
        <div className="text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider">{label}</div>
        {badgeColors ? (
          <span className={cn("inline-block mt-0.5 px-2 py-0.5 rounded-full text-xs font-medium border", badgeColors.bg, badgeColors.color)}>{value}</span>
        ) : (
          <div className={cn("text-sm text-[var(--color-text)]", mono && "font-mono font-bold text-indigo-400")}>{value}</div>
        )}
      </div>
    </div>
  );
}

function StatBox({ icon: Icon, label, value, color }: {
  icon: React.ComponentType<{ className?: string }>; label: string; value: number; color: string;
}) {
  return (
    <div className={cn("rounded-lg border p-3",
      color === "indigo" ? "border-indigo-500/20 bg-indigo-500/5" : "border-violet-500/20 bg-violet-500/5")}>
      <div className="flex items-center gap-2 mb-1">
        <Icon className={cn("w-3.5 h-3.5", color === "indigo" ? "text-indigo-400" : "text-violet-400")} />
        <span className="text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider">{label}</span>
      </div>
      <div className={cn("text-2xl font-bold", color === "indigo" ? "text-indigo-400" : "text-violet-400")}>{value}</div>
    </div>
  );
}
