"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import {
  FolderKanban, Plus, Search, Loader2, X, Layers, Calendar,
  GitBranch, RotateCcw, CheckCircle2, PauseCircle, AlertCircle,
  Archive, Briefcase, BarChart3, User, Monitor, ShoppingCart,
  Building2, Tag,
} from "lucide-react";
import { cn } from "@/lib/utils";

// ── Types ───────────────────────────────────────────────────

interface TemplateData {
  id: string;
  name: string;
  description?: string;
  icon?: string;
  methodology: string;
  category: string;
  estimationMode: string;
  isBuiltIn: boolean;
  sortOrder: number;
}

interface PortfolioData {
  id: string;
  name: string;
  code: string;
  description?: string;
  status: string;
  ownerUserId?: string;
  projectCount: number;
  createdAt: string;
}

interface ProjectData {
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
  taskCount: number;
  createdAt: string;
}

type TabKey = "projects" | "portfolios";

// ── Config ──────────────────────────────────────────────────

const STATUS_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  Planning: { icon: RotateCcw, color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Planlama" },
  Active: { icon: CheckCircle2, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Aktif" },
  OnHold: { icon: PauseCircle, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Beklemede" },
  Completed: { icon: CheckCircle2, color: "text-teal-400", bg: "bg-teal-500/10 border-teal-500/20", label: "Tamamlandı" },
  Cancelled: { icon: AlertCircle, color: "text-gray-400", bg: "bg-gray-500/10 border-gray-500/20", label: "İptal" },
  Archived: { icon: Archive, color: "text-slate-400", bg: "bg-slate-500/10 border-slate-500/20", label: "Arşiv" },
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

const TABS: { key: TabKey; label: string; icon: React.ComponentType<{ className?: string }> }[] = [
  { key: "projects", label: "Projeler", icon: FolderKanban },
  { key: "portfolios", label: "Portfolyolar", icon: Briefcase },
];

function formatDate(dateStr?: string): string {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleDateString("tr-TR", { day: "2-digit", month: "2-digit", year: "numeric" });
}

// ── Component ───────────────────────────────────────────────

export default function ProjectsPage() {
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<TabKey>("projects");
  const [projects, setProjects] = useState<ProjectData[]>([]);
  const [portfolios, setPortfolios] = useState<PortfolioData[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [methodologyFilter, setMethodologyFilter] = useState("");
  const [portfolioFilter, setPortfolioFilter] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");

  // Templates
  const [templates, setTemplates] = useState<TemplateData[]>([]);

  // Create modal
  const [showCreate, setShowCreate] = useState(false);
  const [createType, setCreateType] = useState<"project" | "portfolio">("project");
  const [createStep, setCreateStep] = useState<"template" | "form">("template");
  const [selectedTemplate, setSelectedTemplate] = useState<TemplateData | null>(null);
  const [formData, setFormData] = useState<Record<string, string>>({});
  const [formSaving, setFormSaving] = useState(false);
  const [formError, setFormError] = useState("");

  // Fetch
  const fetchProjects = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (statusFilter) params.set("status", statusFilter);
      if (portfolioFilter) params.set("portfolioId", portfolioFilter);
      const res = await fetch(`/api/pm/projects?${params}`);
      if (res.ok) setProjects(await res.json());
    } catch { /* */
    } finally { setLoading(false); }
  }, [statusFilter, portfolioFilter]);

  const fetchPortfolios = useCallback(async () => {
    try {
      const res = await fetch("/api/pm/portfolios");
      if (res.ok) setPortfolios(await res.json());
    } catch { /* */ }
  }, []);

  const fetchTemplates = useCallback(async () => {
    try {
      const res = await fetch("/api/pm/project-templates");
      if (res.ok) setTemplates(await res.json());
    } catch { /* */ }
  }, []);

  useEffect(() => { fetchProjects(); fetchPortfolios(); fetchTemplates(); }, [fetchProjects, fetchPortfolios, fetchTemplates]);

  // Create handlers
  const openProjectCreate = () => {
    setCreateType("project"); setCreateStep("template"); setSelectedTemplate(null);
    setFormData({}); setFormError(""); setShowCreate(true);
  };

  const selectTemplate = (t: TemplateData) => {
    setSelectedTemplate(t);
    setFormData(d => ({ ...d, methodology: t.methodology, category: t.category }));
    setCreateStep("form");
  };

  const handleCreate = async () => {
    if (createType === "project" && (!formData.key?.trim() || !formData.name?.trim())) {
      setFormError("Anahtar ve ad zorunludur."); return;
    }
    if (createType === "portfolio" && (!formData.name?.trim() || !formData.code?.trim())) {
      setFormError("Ad ve kod zorunludur."); return;
    }
    setFormSaving(true); setFormError("");
    try {
      let url: string; let body: Record<string, unknown>;
      if (createType === "portfolio") {
        url = "/api/pm/portfolios";
        body = { name: formData.name?.trim(), code: formData.code?.trim(), description: formData.description?.trim() || null };
      } else if (selectedTemplate) {
        url = "/api/pm/projects/from-template";
        body = {
          templateId: selectedTemplate.id, key: formData.key?.trim(), name: formData.name?.trim(),
          description: formData.description?.trim() || null,
          portfolioId: formData.portfolioId || null,
          startDate: formData.startDate || null, targetEndDate: formData.targetEndDate || null,
        };
      } else {
        url = "/api/pm/projects";
        body = {
          key: formData.key?.trim(), name: formData.name?.trim(),
          description: formData.description?.trim() || null,
          methodology: formData.methodology || "Kanban", category: formData.category || "General",
          portfolioId: formData.portfolioId || null,
          startDate: formData.startDate || null, targetEndDate: formData.targetEndDate || null,
        };
      }
      const res = await fetch(url, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      if (res.ok) {
        setShowCreate(false); setFormData({}); setFormError(""); setSelectedTemplate(null);
        fetchProjects(); fetchPortfolios();
      } else { setFormError(`Hata: ${res.status}`); }
    } catch { setFormError("Bağlantı hatası."); }
    finally { setFormSaving(false); }
  };

  // Filters
  const filteredProjects = searchTerm
    ? projects.filter(p => p.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        p.key.toLowerCase().includes(searchTerm.toLowerCase()))
    : projects;

  const filteredPortfolios = searchTerm
    ? portfolios.filter(p => p.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        p.code.toLowerCase().includes(searchTerm.toLowerCase()))
    : portfolios;

  return (
    <div className="space-y-5">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center shadow-lg shadow-indigo-500/25">
            <FolderKanban className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">Proje Yönetimi</h1>
            <p className="text-sm text-[var(--color-text-muted)] mt-0.5">
              {projects.length} proje · {portfolios.length} portfolyo
            </p>
          </div>
        </div>
        <div className="flex gap-2">
          <button onClick={() => { setCreateType("portfolio"); setShowCreate(true); setFormData({}); }}
            className="flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text)] hover:bg-[var(--color-border)]/50 transition-all">
            <Briefcase className="w-4 h-4" /> Yeni Portfolyo
          </button>
          <button onClick={openProjectCreate}
            className="flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium bg-indigo-600 text-white hover:bg-indigo-700 shadow-lg shadow-indigo-500/20 transition-all">
            <Plus className="w-4 h-4" /> Yeni Proje
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-1 p-1 rounded-xl bg-[var(--color-bg)]/80 border border-[var(--color-border)] w-fit">
        {TABS.map(tab => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.key;
          const count = tab.key === "projects" ? projects.length : portfolios.length;
          return (
            <button key={tab.key} onClick={() => setActiveTab(tab.key)}
              className={cn(
                "flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200",
                isActive
                  ? "bg-[var(--color-card-bg)] text-[var(--color-text)] shadow-sm border border-[var(--color-border)]"
                  : "text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-card-bg)]/50"
              )}>
              <Icon className="w-4 h-4" />
              {tab.label}
              {count > 0 && (
                <span className={cn("inline-flex items-center justify-center min-w-[20px] h-5 px-1.5 rounded-full text-[11px] font-semibold",
                  isActive ? "bg-indigo-500/20 text-indigo-400" : "bg-[var(--color-border)] text-[var(--color-text-muted)]")}>
                  {count}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
          <input type="text" placeholder={activeTab === "projects" ? "Proje ara..." : "Portfolyo ara..."}
            value={searchTerm} onChange={e => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
        </div>
        {activeTab === "projects" && (
          <>
            <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
              className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
              <option value="">Tüm Durumlar</option>
              {["Planning", "Active", "OnHold", "Completed", "Cancelled"].map(s => (
                <option key={s} value={s}>{STATUS_CONFIG[s]?.label ?? s}</option>
              ))}
            </select>
            <select value={methodologyFilter} onChange={e => setMethodologyFilter(e.target.value)}
              className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
              <option value="">Tüm Metodolojiler</option>
              {["Kanban", "Scrum", "ScrumBan", "Waterfall"].map(m => (
                <option key={m} value={m}>{METHODOLOGY_CONFIG[m]?.label ?? m}</option>
              ))}
            </select>
            <select value={categoryFilter} onChange={e => setCategoryFilter(e.target.value)}
              className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
              <option value="">Tüm Kategoriler</option>
              {Object.entries(CATEGORY_CONFIG).map(([k, v]) => (
                <option key={k} value={k}>{v.label}</option>
              ))}
            </select>
            {portfolios.length > 0 && (
              <select value={portfolioFilter} onChange={e => setPortfolioFilter(e.target.value)}
                className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                <option value="">Tüm Portfolyolar</option>
                {portfolios.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
              </select>
            )}
          </>
        )}
      </div>

      {/* Content */}
      {loading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="w-6 h-6 text-indigo-400 animate-spin" />
        </div>
      ) : activeTab === "projects" ? (
        /* ── Project Grid ─────────────────────────── */
        filteredProjects.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-[var(--color-text-muted)]">
            <FolderKanban className="w-12 h-12 opacity-30 mb-3" />
            <p className="text-sm">Henüz proje oluşturulmamış</p>
            <button onClick={openProjectCreate}
              className="mt-3 text-xs text-indigo-400 hover:text-indigo-300 transition-colors">+ Yeni Proje Oluştur</button>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {(methodologyFilter || categoryFilter
              ? filteredProjects.filter(p =>
                  (!methodologyFilter || p.methodology === methodologyFilter) &&
                  (!categoryFilter || p.category === categoryFilter))
              : filteredProjects).map(project => {
              const statusCfg = STATUS_CONFIG[project.status] || STATUS_CONFIG.Planning;
              const methodCfg = METHODOLOGY_CONFIG[project.methodology] || METHODOLOGY_CONFIG.Kanban;
              const catCfg = CATEGORY_CONFIG[project.category] || CATEGORY_CONFIG.General;
              const StatusIcon = statusCfg.icon;
              const CatIcon = catCfg.icon;
              return (
                <div key={project.id} onClick={() => router.push(`/dashboard/projects/${project.id}`)}
                  className="group rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5 hover:border-indigo-500/40 hover:shadow-lg hover:shadow-indigo-500/5 transition-all duration-300 cursor-pointer">
                  <div className="flex items-start justify-between mb-3">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-xs font-mono font-bold text-indigo-400 bg-indigo-500/10 px-2 py-1 rounded">{project.key}</span>
                      <span className={cn("inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium border", catCfg.bg, catCfg.color)}>
                        <CatIcon className="w-3 h-3" /> {catCfg.label}
                      </span>
                      {project.portfolioName && (
                        <span className="text-[10px] font-medium text-violet-400 bg-violet-500/10 px-1.5 py-0.5 rounded-full">{project.portfolioName}</span>
                      )}
                    </div>
                    <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", methodCfg.bg, methodCfg.color)}>
                      {methodCfg.label}
                    </span>
                  </div>
                  {/* Title */}
                  <h3 className="text-base font-semibold text-[var(--color-text)] mb-1 group-hover:text-indigo-400 transition-colors line-clamp-1">{project.name}</h3>
                  {project.description && (
                    <p className="text-xs text-[var(--color-text-muted)] mb-3 line-clamp-2">{project.description}</p>
                  )}
                  {/* Meta */}
                  <div className="flex items-center justify-between mt-4 pt-3 border-t border-[var(--color-border)]">
                    <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", statusCfg.bg, statusCfg.color)}>
                      <StatusIcon className="w-3 h-3" /> {statusCfg.label}
                    </span>
                    <div className="flex items-center gap-3 text-xs text-[var(--color-text-muted)]">
                      <span className="flex items-center gap-1"><BarChart3 className="w-3 h-3" />{project.taskCount}</span>
                      {project.targetEndDate && (
                        <span className="flex items-center gap-1"><Calendar className="w-3 h-3" />{formatDate(project.targetEndDate)}</span>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )
      ) : (
        /* ── Portfolio List ───────────────────────── */
        filteredPortfolios.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-[var(--color-text-muted)]">
            <Briefcase className="w-12 h-12 opacity-30 mb-3" />
            <p className="text-sm">Henüz portfolyo oluşturulmamış</p>
            <button onClick={() => { setCreateType("portfolio"); setShowCreate(true); setFormData({}); }}
              className="mt-3 text-xs text-indigo-400 hover:text-indigo-300 transition-colors">+ Yeni Portfolyo Oluştur</button>
          </div>
        ) : (
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] overflow-hidden">
            <table className="w-full">
              <thead>
                <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg)]/50">
                  <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Kod</th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Ad</th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Açıklama</th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Durum</th>
                  <th className="text-center px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Projeler</th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Oluşturma</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--color-border)]">
                {filteredPortfolios.map(pf => {
                  const statusCfg = STATUS_CONFIG[pf.status] || STATUS_CONFIG.Active;
                  const StatusIcon = statusCfg.icon;
                  return (
                    <tr key={pf.id} className="hover:bg-[var(--color-border)]/30 transition-colors">
                      <td className="px-4 py-3">
                        <span className="text-sm font-mono font-bold text-violet-400">{pf.code}</span>
                      </td>
                      <td className="px-4 py-3">
                        <span className="text-sm font-medium text-[var(--color-text)]">{pf.name}</span>
                      </td>
                      <td className="px-4 py-3 max-w-[250px]">
                        <span className="text-xs text-[var(--color-text-muted)] line-clamp-1">{pf.description || "—"}</span>
                      </td>
                      <td className="px-4 py-3">
                        <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", statusCfg.bg, statusCfg.color)}>
                          <StatusIcon className="w-3 h-3" /> {statusCfg.label}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-center">
                        <span className="inline-flex items-center justify-center w-7 h-7 rounded-full bg-indigo-500/10 text-indigo-400 text-xs font-bold">{pf.projectCount}</span>
                      </td>
                      <td className="px-4 py-3">
                        <span className="text-xs text-[var(--color-text-muted)]">{formatDate(pf.createdAt)}</span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )
      )}

      {/* ═══════════ Create Modal ═══════════ */}
      {showCreate && (
        <>
          <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm" onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); setSelectedTemplate(null); }} />
          <div className="fixed right-0 top-0 z-50 h-full w-full max-w-lg bg-[var(--color-card-bg)] border-l border-[var(--color-border)] shadow-2xl flex flex-col animate-slide-in-right">
            {/* Header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
              <div className="flex items-center gap-3">
                <div className={cn("w-10 h-10 rounded-xl flex items-center justify-center shadow-lg",
                  createType === "project" ? "bg-gradient-to-br from-indigo-500 to-violet-600 shadow-indigo-500/30" : "bg-gradient-to-br from-violet-500 to-purple-600 shadow-violet-500/30")}>
                  {createType === "project" ? <FolderKanban className="w-5 h-5 text-white" /> : <Briefcase className="w-5 h-5 text-white" />}
                </div>
                <div>
                  <h2 className="text-lg font-semibold text-[var(--color-text)]">{createType === "project" ? (createStep === "template" ? "Şablon Seçin" : "Proje Bilgileri") : "Yeni Portfolyo"}</h2>
                  <p className="text-xs text-[var(--color-text-muted)]">{createType === "project" ? (createStep === "template" ? "Bir proje şablonu seçerek başlayın" : (selectedTemplate ? `Şablon: ${selectedTemplate.icon} ${selectedTemplate.name}` : "Proje bilgilerini girin")) : "Portfolyo bilgilerini girin"}</p>
                </div>
              </div>
              <button onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); setSelectedTemplate(null); }} className="p-2 rounded-lg hover:bg-[var(--color-border)] transition-colors">
                <X className="w-5 h-5 text-[var(--color-text-muted)]" />
              </button>
            </div>

            {/* Body */}
            <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
              {/* ── Template Selection Step ── */}
              {createType === "project" && createStep === "template" ? (
                <div className="space-y-3">
                  {templates.map(t => {
                    const methCfg = METHODOLOGY_CONFIG[t.methodology] || METHODOLOGY_CONFIG.Kanban;
                    const catCfg = CATEGORY_CONFIG[t.category] || CATEGORY_CONFIG.General;
                    return (
                      <button key={t.id} onClick={() => selectTemplate(t)}
                        className="w-full text-left p-4 rounded-xl border border-[var(--color-border)] bg-[var(--color-bg)]/50 hover:border-indigo-500/50 hover:bg-indigo-500/5 hover:shadow-lg hover:shadow-indigo-500/10 transition-all duration-300 group">
                        <div className="flex items-start gap-3">
                          <span className="text-2xl mt-0.5">{t.icon || "📁"}</span>
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2 mb-1">
                              <h3 className="text-sm font-semibold text-[var(--color-text)] group-hover:text-indigo-400 transition-colors">{t.name}</h3>
                              <span className={cn("px-1.5 py-0.5 rounded-full text-[9px] font-medium border", methCfg.bg, methCfg.color)}>{methCfg.label}</span>
                              <span className={cn("px-1.5 py-0.5 rounded-full text-[9px] font-medium border", catCfg.bg, catCfg.color)}>{catCfg.label}</span>
                            </div>
                            {t.description && <p className="text-xs text-[var(--color-text-muted)] line-clamp-2">{t.description}</p>}
                          </div>
                          <GitBranch className="w-4 h-4 text-[var(--color-text-muted)] opacity-0 group-hover:opacity-100 transition-opacity mt-1" />
                        </div>
                      </button>
                    );
                  })}
                </div>
              ) : createType === "project" ? (
                <>
                  {selectedTemplate && (
                    <button onClick={() => setCreateStep("template")}
                      className="flex items-center gap-2 text-xs text-indigo-400 hover:text-indigo-300 transition-colors mb-2">
                      <RotateCcw className="w-3 h-3" /> Şablonu değiştir
                    </button>
                  )}
                  <div className="grid grid-cols-3 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Anahtar <span className="text-red-400">*</span></label>
                      <input type="text" value={formData.key || ""} onChange={e => setFormData(d => ({ ...d, key: e.target.value.toUpperCase() }))}
                        placeholder="PRJ" maxLength={10}
                        className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40 font-mono" />
                    </div>
                    <div className="col-span-2">
                      <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Proje Adı <span className="text-red-400">*</span></label>
                      <input type="text" value={formData.name || ""} onChange={e => setFormData(d => ({ ...d, name: e.target.value }))}
                        placeholder="Proje adı..."
                        className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Açıklama</label>
                    <textarea rows={3} value={formData.description || ""} onChange={e => setFormData(d => ({ ...d, description: e.target.value }))}
                      placeholder="Projenin amacı ve kapsamı..."
                      className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40 resize-none" />
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Metodoloji</label>
                      <select value={formData.methodology || "Kanban"} onChange={e => setFormData(d => ({ ...d, methodology: e.target.value }))}
                        className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40">
                        <option value="Kanban">Kanban</option>
                        <option value="Scrum">Scrum</option>
                        <option value="ScrumBan">ScrumBan</option>
                        <option value="Waterfall">Waterfall</option>
                      </select>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Kategori</label>
                      <select value={formData.category || "General"} onChange={e => setFormData(d => ({ ...d, category: e.target.value }))}
                        className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40">
                        {Object.entries(CATEGORY_CONFIG).map(([k, v]) => (
                          <option key={k} value={k}>{v.label}</option>
                        ))}
                      </select>
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Portfolyo</label>
                      <select value={formData.portfolioId || ""} onChange={e => setFormData(d => ({ ...d, portfolioId: e.target.value }))}
                        className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40">
                        <option value="">Portfolyo yok</option>
                        {portfolios.map(p => <option key={p.id} value={p.id}>{p.name} ({p.code})</option>)}
                      </select>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Başlangıç Tarihi</label>
                      <input type="date" value={formData.startDate || ""} onChange={e => setFormData(d => ({ ...d, startDate: e.target.value }))}
                        className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Hedef Bitiş</label>
                      <input type="date" value={formData.targetEndDate || ""} onChange={e => setFormData(d => ({ ...d, targetEndDate: e.target.value }))}
                        className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
                    </div>
                  </div>
                </>
              ) : (
                <>
                  <div className="grid grid-cols-3 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Kod <span className="text-red-400">*</span></label>
                      <input type="text" value={formData.code || ""} onChange={e => setFormData(d => ({ ...d, code: e.target.value.toUpperCase() }))}
                        placeholder="DT" maxLength={20}
                        className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40 font-mono" />
                    </div>
                    <div className="col-span-2">
                      <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Portfolyo Adı <span className="text-red-400">*</span></label>
                      <input type="text" value={formData.name || ""} onChange={e => setFormData(d => ({ ...d, name: e.target.value }))}
                        placeholder="Portfolyo adı..."
                        className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Açıklama</label>
                    <textarea rows={3} value={formData.description || ""} onChange={e => setFormData(d => ({ ...d, description: e.target.value }))}
                      placeholder="Portfolyonun stratejik amacı..."
                      className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40 resize-none" />
                  </div>
                </>
              )}
              {formError && (
                <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-sm">{formError}</div>
              )}
            </div>

            {/* Footer — template step'te gizle */}
            {!(createType === "project" && createStep === "template") && (
            <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-3">
              <button onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); setSelectedTemplate(null); }}
                className="px-4 py-2.5 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50 transition-colors">İptal</button>
              <button onClick={handleCreate} disabled={formSaving}
                className="px-6 py-2.5 rounded-lg text-sm font-medium bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors flex items-center gap-2">
                {formSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
                Oluştur
              </button>
            </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
