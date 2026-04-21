"use client";

import { useState, useEffect, useCallback } from "react";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft, FolderKanban, Calendar, BarChart3, User, GitBranch,
  Loader2, CheckCircle2, PauseCircle, RotateCcw, AlertCircle,
  Briefcase, Edit3, Save, X, ListTodo, Layout, Milestone, Archive,
  ChevronRight, Monitor, ShoppingCart, Building2, Tag, AppWindow, Plus, Trash2,
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
  deliverables?: DeliverableItem[];
}

interface DeliverableItem {
  id: string;
  configurationItemId: string;
  ciName: string;
  ciCode: string;
  ciType: string;
  role: string;
  notes?: string;
}

interface ApplicationOption {
  id: string;
  name: string;
  code: string;
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
type TabKey = "overview" | "backlog" | "board" | "metrics" | "tasks" | "milestones";
const ALL_TABS: { key: TabKey; label: string; icon: React.ComponentType<{ className?: string }>; categories: string[]; disabled?: boolean }[] = [
  { key: "overview", label: "Genel Bakış", icon: FolderKanban, categories: ["all"] },
  { key: "board", label: "Board", icon: Layout, categories: ["all"] },
  { key: "metrics", label: "Metrikler", icon: BarChart3, categories: ["all"] },
  { key: "backlog", label: "Backlog", icon: ListTodo, categories: ["SoftwareDevelopment"], disabled: true },
  { key: "tasks", label: "İş Kalemleri", icon: ListTodo, categories: ["all"], disabled: true },
  { key: "milestones", label: "Milestones", icon: Milestone, categories: ["Infrastructure", "Procurement", "Business", "SoftwareDevelopment"], disabled: true },
];

interface BoardColumnData { id: string; name: string; order: number; mappedStatus: string; wipLimit?: number | null; }
interface BoardWorkItem { id: string; workItemNumber: string; title: string; type: string; status: string; priority: string; storyPoints?: number; assigneeUserId?: string; }
interface VelocityData { sprintId: string; sprintName: string; plannedPoints: number; completedPoints: number; startDate: string; endDate: string; }

const TYPE_ICONS: Record<string,string> = { Task:"📋", Bug:"🐛", Feature:"🏗", Improvement:"⚡", Epic:"🎯", UserStory:"📖", TechDebt:"🔧", Spike:"🔬" };
const PRIORITY_COLORS: Record<string,string> = { Critical:"bg-red-500", High:"bg-orange-500", Medium:"bg-yellow-500", Low:"bg-blue-500", None:"bg-gray-500" };

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

  // Deliverables
  const [applications, setApplications] = useState<ApplicationOption[]>([]);
  const [addingDeliverable, setAddingDeliverable] = useState(false);
  const [selectedAppId, setSelectedAppId] = useState("");
  const [selectedRole, setSelectedRole] = useState("Primary");

  // Board state
  const [boardColumns, setBoardColumns] = useState<BoardColumnData[]>([]);
  const [boardItems, setBoardItems] = useState<BoardWorkItem[]>([]);
  const [boardLoading, setBoardLoading] = useState(false);

  // Metrics state
  const [velocity, setVelocity] = useState<VelocityData[]>([]);
  const [metricsLoading, setMetricsLoading] = useState(false);

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

  // Fetch board data
  useEffect(() => {
    if (activeTab !== "board" || !projectId) return;
    setBoardLoading(true);
    Promise.all([
      fetch(`/api/pm/projects/${projectId}/board-columns`).then(r => r.ok ? r.json() : []),
      fetch(`/api/pm/work-items/board/${projectId}`).then(r => r.ok ? r.json() : {}),
    ]).then(([cols, boardData]) => {
      setBoardColumns(Array.isArray(cols) ? cols : []);
      const items: BoardWorkItem[] = [];
      if (boardData && typeof boardData === "object") {
        Object.values(boardData).forEach((arr: unknown) => {
          if (Array.isArray(arr)) items.push(...arr);
        });
      }
      setBoardItems(items);
    }).catch(() => {}).finally(() => setBoardLoading(false));
  }, [activeTab, projectId]);

  // Fetch metrics data
  useEffect(() => {
    if (activeTab !== "metrics" || !projectId) return;
    setMetricsLoading(true);
    fetch(`/api/pm/projects/${projectId}/velocity`)
      .then(r => r.ok ? r.json() : [])
      .then(data => setVelocity(Array.isArray(data) ? data : []))
      .catch(() => setVelocity([]))
      .finally(() => setMetricsLoading(false));
  }, [activeTab, projectId]);

  // Fetch apps for deliverable dropdown
  useEffect(() => {
    fetch("/api/pm/applications")
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setApplications(Array.isArray(data) ? data : []))
      .catch(() => setApplications([]));
  }, []);

  const addDeliverable = async () => {
    if (!selectedAppId || !projectId) return;
    setAddingDeliverable(true);
    try {
      await fetch(`/api/pm/projects/${projectId}/deliverables`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ configurationItemId: selectedAppId, role: selectedRole }),
      });
      setSelectedAppId("");
      setSelectedRole("Primary");
      fetchProject();
    } catch { /* */ }
    finally { setAddingDeliverable(false); }
  };

  const removeDeliverable = async (ciId: string) => {
    if (!projectId) return;
    try {
      await fetch(`/api/pm/projects/${projectId}/deliverables/${ciId}`, { method: "DELETE" });
      fetchProject();
    } catch { /* */ }
  };

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

            {/* Teslim Edilebilirler */}
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
              <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3 flex items-center gap-2">
                <AppWindow className="w-4 h-4 text-cyan-400" /> Teslim Edilebilirler
              </h3>
              {(project.deliverables && project.deliverables.length > 0) && (
                <ul className="space-y-2 mb-3">
                  {project.deliverables.map(d => (
                    <li key={d.id} className="flex items-center justify-between p-2.5 rounded-lg bg-[var(--color-bg)] group">
                      <button
                        onClick={() => router.push(`/dashboard/applications/${d.configurationItemId}`)}
                        className="text-left flex-1 min-w-0"
                      >
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-mono font-bold text-cyan-400">{d.ciCode}</span>
                          <span className="text-[10px] text-[var(--color-text-muted)]">{d.role}</span>
                        </div>
                        <p className="text-xs text-[var(--color-text)] truncate">{d.ciName}</p>
                      </button>
                      <button
                        onClick={() => removeDeliverable(d.configurationItemId)}
                        className="p-1 rounded text-red-400/50 hover:text-red-400 hover:bg-red-500/10 transition-colors opacity-0 group-hover:opacity-100"
                        title="Kaldır"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </li>
                  ))}
                </ul>
              )}
              {/* Add deliverable form */}
              <div className="flex items-center gap-2">
                <select
                  value={selectedAppId}
                  onChange={(e) => setSelectedAppId(e.target.value)}
                  className="flex-1 px-2 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none"
                >
                  <option value="">Uygulama seçin...</option>
                  {applications
                    .filter(a => !project.deliverables?.some(d => d.configurationItemId === a.id))
                    .map(a => <option key={a.id} value={a.id}>{a.name} ({a.code})</option>)}
                </select>
                <select
                  value={selectedRole}
                  onChange={(e) => setSelectedRole(e.target.value)}
                  className="w-24 px-2 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none"
                >
                  <option value="Primary">Primary</option>
                  <option value="Secondary">Secondary</option>
                  <option value="Supporting">Supporting</option>
                </select>
                <button
                  onClick={addDeliverable}
                  disabled={!selectedAppId || addingDeliverable}
                  className="p-1.5 rounded-lg bg-cyan-600 text-white hover:bg-cyan-700 disabled:opacity-40 transition-colors"
                >
                  {addingDeliverable ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Plus className="w-3.5 h-3.5" />}
                </button>
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

      {/* Board Tab */}
      {activeTab === "board" && (
        <div className="space-y-4">
          {boardLoading ? (
            <div className="flex items-center justify-center py-12"><Loader2 className="w-6 h-6 text-indigo-400 animate-spin" /></div>
          ) : boardColumns.length === 0 ? (
            <div className="text-center py-12 text-[var(--color-text-muted)]">
              <Layout className="w-10 h-10 mx-auto opacity-30 mb-2" />
              <p className="text-sm">Board kolonları henüz oluşturulmadı</p>
            </div>
          ) : (
            <div className="flex gap-3 overflow-x-auto pb-4" style={{ minHeight: 400 }}>
              {boardColumns.map(col => {
                const colItems = boardItems.filter(item => item.status === col.mappedStatus);
                const isOverWip = col.wipLimit != null && colItems.length > col.wipLimit;
                return (
                  <div key={col.id} className={cn(
                    "flex-shrink-0 w-72 rounded-xl border bg-[var(--color-card-bg)] flex flex-col",
                    isOverWip ? "border-red-500/50" : "border-[var(--color-border)]"
                  )}>
                    <div className={cn(
                      "px-4 py-3 border-b flex items-center justify-between rounded-t-xl",
                      isOverWip ? "border-red-500/30 bg-red-500/5" : "border-[var(--color-border)]"
                    )}>
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-semibold text-[var(--color-text)]">{col.name}</span>
                        <span className={cn(
                          "text-xs font-mono px-1.5 py-0.5 rounded-full",
                          isOverWip ? "bg-red-500/20 text-red-400" : "bg-[var(--color-border)] text-[var(--color-text-muted)]"
                        )}>{colItems.length}{col.wipLimit != null ? `/${col.wipLimit}` : ""}</span>
                      </div>
                    </div>
                    <div className="p-2 flex-1 space-y-2 overflow-y-auto" style={{ maxHeight: 500 }}>
                      {colItems.map(item => (
                        <div key={item.id}
                          onClick={() => router.push(`/dashboard/tasks/${item.id}`)}
                          className="p-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] hover:border-indigo-500/30 hover:bg-indigo-500/5 transition-all cursor-pointer group">
                          <div className="flex items-center gap-1.5 mb-1">
                            <span className="text-xs">{TYPE_ICONS[item.type] ?? "📋"}</span>
                            <span className="text-[10px] font-mono text-[var(--color-text-muted)]">{item.workItemNumber}</span>
                            {item.storyPoints != null && item.storyPoints > 0 && (
                              <span className="ml-auto text-[10px] font-bold text-indigo-400 bg-indigo-500/10 px-1.5 py-0.5 rounded-full">{item.storyPoints} SP</span>
                            )}
                          </div>
                          <p className="text-xs text-[var(--color-text)] line-clamp-2">{item.title}</p>
                          <div className="flex items-center gap-1.5 mt-1.5">
                            <span className={cn("w-1.5 h-1.5 rounded-full", PRIORITY_COLORS[item.priority] ?? "bg-gray-500")} />
                            <span className="text-[9px] text-[var(--color-text-muted)]">{item.priority}</span>
                          </div>
                        </div>
                      ))}
                      {colItems.length === 0 && (
                        <div className="text-center py-6 text-[10px] text-[var(--color-text-muted)] opacity-50">Boş</div>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* Metrics Tab */}
      {activeTab === "metrics" && (
        <div className="space-y-6">
          {metricsLoading ? (
            <div className="flex items-center justify-center py-12"><Loader2 className="w-6 h-6 text-indigo-400 animate-spin" /></div>
          ) : velocity.length === 0 ? (
            <div className="text-center py-12 text-[var(--color-text-muted)]">
              <BarChart3 className="w-10 h-10 mx-auto opacity-30 mb-2" />
              <p className="text-sm">Tamamlanmış sprint bulunamadı</p>
              <p className="text-[11px] mt-1">Sprint tamamlandığında velocity verileri burada görünecek</p>
            </div>
          ) : (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-6">
              <h3 className="text-sm font-semibold text-[var(--color-text)] mb-4 flex items-center gap-2">
                <BarChart3 className="w-4 h-4 text-indigo-400" /> Velocity Chart
              </h3>
              <div className="flex items-end gap-3 h-48">
                {velocity.map(v => {
                  const maxPts = Math.max(...velocity.map(x => Math.max(x.plannedPoints, x.completedPoints)), 1);
                  const pctPlanned = (v.plannedPoints / maxPts) * 100;
                  const pctCompleted = (v.completedPoints / maxPts) * 100;
                  return (
                    <div key={v.sprintId} className="flex-1 flex flex-col items-center gap-1">
                      <div className="flex items-end gap-1 w-full h-40">
                        <div className="flex-1 rounded-t-md bg-indigo-500/20 border border-indigo-500/30 transition-all" style={{ height: `${pctPlanned}%` }} title={`Planlanan: ${v.plannedPoints} SP`} />
                        <div className="flex-1 rounded-t-md bg-emerald-500/40 border border-emerald-500/50 transition-all" style={{ height: `${pctCompleted}%` }} title={`Tamamlanan: ${v.completedPoints} SP`} />
                      </div>
                      <span className="text-[9px] text-[var(--color-text-muted)] text-center truncate w-full">{v.sprintName}</span>
                      <span className="text-[10px] font-mono text-emerald-400">{v.completedPoints} SP</span>
                    </div>
                  );
                })}
              </div>
              <div className="flex items-center gap-4 mt-4 pt-3 border-t border-[var(--color-border)]">
                <div className="flex items-center gap-1.5 text-[10px] text-[var(--color-text-muted)]">
                  <span className="w-3 h-3 rounded bg-indigo-500/20 border border-indigo-500/30" /> Planlanan
                </div>
                <div className="flex items-center gap-1.5 text-[10px] text-[var(--color-text-muted)]">
                  <span className="w-3 h-3 rounded bg-emerald-500/40 border border-emerald-500/50" /> Tamamlanan
                </div>
                <span className="ml-auto text-[10px] text-[var(--color-text-muted)]">
                  Ort. Velocity: {Math.round(velocity.reduce((s,v) => s + v.completedPoints, 0) / velocity.length)} SP
                </span>
              </div>
            </div>
          )}
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
