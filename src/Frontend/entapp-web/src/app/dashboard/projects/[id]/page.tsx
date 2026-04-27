"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import dynamic from "next/dynamic";
import TestScenariosTab from "./TestScenariosTab";
import TestPlansTab from "./TestPlansTab";
import MilestonesTab from "./MilestonesTab";
import SprintsTab from "./SprintsTab";
import BoardColumnSettings from "./BoardColumnSettings";
import RequirementsTab from "./RequirementsTab";
import ReleasesTab from "./ReleasesTab";
import WikiTab from "./WikiTab";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import {
  ArrowLeft, FolderKanban, Calendar, BarChart3, User, GitBranch,
  Loader2, CheckCircle2, PauseCircle, RotateCcw, AlertCircle,
  Briefcase, Edit3, Save, X, ListTodo, Layout, Milestone, Archive,
  ChevronRight, ChevronDown, Monitor, ShoppingCart, Building2, Tag, AppWindow, Plus, Trash2,
  Table2, TreePine, Filter, Search, RefreshCw, Timer, Play, Square, XCircle,
  Settings, GripVertical, ClipboardList, FlaskConical, TestTube2, Rocket, BookOpen,
} from "lucide-react";
import { cn } from "@/lib/utils";

// ── Types ───────────────────────────────────────────

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

// ── Config ──────────────────────────────────────────

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
type TabKey = "overview" | "workitems" | "board" | "sprints" | "metrics" | "milestones" | "requirements" | "releases" | "test-scenarios" | "test-plans" | "wiki" | "settings";
const ALL_TABS: { key: TabKey; label: string; icon: React.ComponentType<{ className?: string }>; categories: string[]; disabled?: boolean }[] = [
  { key: "overview", label: "Genel Bakış", icon: FolderKanban, categories: ["all"] },
  { key: "workitems", label: "Backlog", icon: ListTodo, categories: ["all"] },
  { key: "board", label: "Board", icon: Layout, categories: ["all"] },
  { key: "sprints", label: "Sprintler", icon: Timer, categories: ["all"] },
  { key: "metrics", label: "Metrikler", icon: BarChart3, categories: ["all"] },
  { key: "milestones", label: "Milestones", icon: Milestone, categories: ["all"] },
  { key: "requirements", label: "Gereksinimler", icon: ClipboardList, categories: ["all"] },
  { key: "test-scenarios", label: "Test Senaryoları", icon: FlaskConical, categories: ["all"] },
  { key: "test-plans", label: "Test Planları", icon: TestTube2, categories: ["all"] },
  { key: "releases", label: "Releases", icon: Rocket, categories: ["all"] },
  { key: "wiki", label: "Wiki", icon: BookOpen, categories: ["all"] },
  { key: "settings", label: "Ayarlar", icon: Settings, categories: ["all"] },
];

interface BacklogWorkItem {
  id: { value: string } | string;
  workItemNumber: string;
  title: string;
  description?: string;
  status: string;
  priority: string;
  type: string;
  assigneeUserId?: string;
  reporterUserId?: string;
  parentTaskId?: string | null;
  dueDate?: string;
  estimatedHours?: number;
  storyPoints?: number | null;
  acceptanceCriteria?: string;
  hierarchyLevel?: number;
  sprintId?: string | null;
  sortOrder?: number;
  tags?: string;
  createdAt: string;
  children?: BacklogWorkItem[];
  wsjfScore?: number | null;
}

const WORK_ITEM_TYPES = [
  { value: "Epic", label: "Epic", icon: "🏔️" },
  { value: "Feature", label: "Feature", icon: "⭐" },
  { value: "UserStory", label: "User Story", icon: "📖" },
  { value: "Task", label: "Task", icon: "✅" },
  { value: "Bug", label: "Bug", icon: "🐛" },
  { value: "TechDebt", label: "Tech Debt", icon: "🔧" },
  { value: "Spike", label: "Spike", icon: "💡" },
  { value: "Improvement", label: "Improvement", icon: "a" },
];

const STATUS_LABELS: Record<string,{label:string;color:string}> = {
  Backlog: { label: "Backlog", color: "bg-slate-500/20 text-slate-400" },
  Todo: { label: "Yapılacak", color: "bg-blue-500/20 text-blue-400" },
  InProgress: { label: "Devam Ediyor", color: "bg-amber-500/20 text-amber-400" },
  InReview: { label: "İncelemede", color: "bg-purple-500/20 text-purple-400" },
  Done: { label: "Tamamlandı", color: "bg-emerald-500/20 text-emerald-400" },
  Cancelled: { label: "İptal", color: "bg-red-500/20 text-red-400" },
};

interface BoardColumnData { id: string; name: string; order: number; mappedStatus: string; wipLimit?: number | null; }
interface BoardWorkItem { id: string; workItemNumber: string; title: string; type: string; status: string; priority: string; storyPoints?: number; assigneeUserId?: string; }
interface VelocityData { sprintId: string; sprintName: string; plannedPoints: number; completedPoints: number; startDate: string; endDate: string; }
interface MetricsSummary {
  totalStoryPoints: number; completedStoryPoints: number;
  totalWorkItems: number; activeWorkItems: number;
  averageVelocity: number;
  byType: Record<string, number>; byStatus: Record<string, number>; byPriority: Record<string, number>;
  averageLeadTimeDays: number | null; averageCycleTimeDays: number | null;
}
interface BurndownData {
  plannedPoints: number; startDate: string; endDate: string;
  dataPoints: { date: string; remainingPoints: number; completedPoints: number; totalItems: number; completedItems: number }[];
}
interface SprintOption { id: string; name: string; status: string; }

// Recharts (SSR uyumlu dynamic import)
const RechartsLine = dynamic(() => import("recharts").then(m => {
  const { ResponsiveContainer, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ReferenceLine } = m;
  return function BurndownChart({ data, ideal }: { data: { date: string; remaining: number; completed: number }[]; ideal: { date: string; ideal: number }[] }) {
    const merged = ideal.map((p, i) => ({ ...p, ...data.find(d => d.date === p.date), remaining: data.find(d => d.date === p.date)?.remaining ?? null }));
    return (
      <ResponsiveContainer width="100%" height={320}>
        <LineChart data={merged} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.06)" />
          <XAxis dataKey="date" tick={{ fontSize: 10, fill: "#94a3b8" }} />
          <YAxis tick={{ fontSize: 10, fill: "#94a3b8" }} />
          <Tooltip contentStyle={{ background: "#1e293b", border: "1px solid #334155", borderRadius: 8, fontSize: 12 }} />
          <Legend wrapperStyle={{ fontSize: 11 }} />
          <Line type="monotone" dataKey="ideal" stroke="#6366f1" strokeDasharray="5 5" name="İdeal" dot={false} strokeWidth={2} />
          <Line type="monotone" dataKey="remaining" stroke="#f59e0b" name="Kalan SP" strokeWidth={2} dot={{ r: 3 }} connectNulls />
        </LineChart>
      </ResponsiveContainer>
    );
  };
}), { ssr: false, loading: () => <div className="h-80 flex items-center justify-center"><Loader2 className="w-5 h-5 animate-spin text-indigo-400" /></div> });

const RechartsBar = dynamic(() => import("recharts").then(m => {
  const { ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ReferenceLine } = m;
  return function VelocityChart({ data, avg }: { data: { name: string; planned: number; completed: number }[]; avg: number }) {
    return (
      <ResponsiveContainer width="100%" height={280}>
        <BarChart data={data} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.06)" />
          <XAxis dataKey="name" tick={{ fontSize: 10, fill: "#94a3b8" }} />
          <YAxis tick={{ fontSize: 10, fill: "#94a3b8" }} />
          <Tooltip contentStyle={{ background: "#1e293b", border: "1px solid #334155", borderRadius: 8, fontSize: 12 }} />
          <Legend wrapperStyle={{ fontSize: 11 }} />
          <ReferenceLine y={avg} stroke="#a78bfa" strokeDasharray="3 3" label={{ value: `Ort: ${avg.toFixed(0)}`, fill: "#a78bfa", fontSize: 10 }} />
          <Bar dataKey="planned" fill="#6366f1" name="Planlanan" radius={[4, 4, 0, 0]} opacity={0.6} />
          <Bar dataKey="completed" fill="#10b981" name="Tamamlanan" radius={[4, 4, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    );
  };
}), { ssr: false, loading: () => <div className="h-72 flex items-center justify-center"><Loader2 className="w-5 h-5 animate-spin text-indigo-400" /></div> });

const TYPE_ICONS: Record<string,string> = { Task:"✅", Bug:"🐛", Feature:"⭐", UserStory:"📖", Epic:"🏔️", TechDebt:"🔧", Spike:"💡", SubTask:"📌" };
const PRIORITY_COLORS: Record<string,string> = { Critical:"bg-red-500", High:"bg-orange-500", Medium:"bg-yellow-500", Low:"bg-blue-500", None:"bg-gray-500" };

// ── Component ───────────────────────────────────────

export default function ProjectDetailPage() {
  const params = useParams();
  const router = useRouter();
  const projectId = params?.id as string;

  const [project, setProject] = useState<ProjectDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const searchParams = useSearchParams();
  const tabFromUrl = searchParams.get("tab") as TabKey | null;
  const [activeTab, setActiveTabState] = useState<TabKey>(tabFromUrl || "overview");
  const setActiveTab = (tab: TabKey) => {
    setActiveTabState(tab);
    const url = new URL(window.location.href);
    if (tab === "overview") url.searchParams.delete("tab");
    else url.searchParams.set("tab", tab);
    window.history.replaceState({}, "", url.toString());
  };
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
  // Board filters & DnD
  const [boardSearch, setBoardSearch] = useState("");
  const [boardTypeFilter, setBoardTypeFilter] = useState("");
  const [boardSwimlane, setBoardSwimlane] = useState<"none" | "assignee" | "priority" | "type">("none");
  const [boardIncludeCompleted, setBoardIncludeCompleted] = useState(false);
  const [draggedItem, setDraggedItem] = useState<BoardWorkItem | null>(null);
  const [dragOverColumn, setDragOverColumn] = useState<string | null>(null);
  const [boardSprints, setBoardSprints] = useState<{id:string;name:string;status:string}[]>([]);
  const [boardSprintFilter, setBoardSprintFilter] = useState("");

  // Metrics state
  const [velocity, setVelocity] = useState<VelocityData[]>([]);
  const [metricsSummary, setMetricsSummary] = useState<MetricsSummary | null>(null);
  const [burndown, setBurndown] = useState<BurndownData | null>(null);
  const [metricsSprints, setMetricsSprints] = useState<SprintOption[]>([]);
  const [selectedSprintId, setSelectedSprintId] = useState("");
  const [metricsLoading, setMetricsLoading] = useState(false);
  const [burndownLoading, setBurndownLoading] = useState(false);

  // WorkItems state
  const [workItems, setWorkItems] = useState<BacklogWorkItem[]>([]);
  const [workItemsLoading, setWorkItemsLoading] = useState(false);
  const [workItemView, setWorkItemView] = useState<"table" | "tree">("table");
  const [wiTypeFilter, setWiTypeFilter] = useState("");
  const [wiStatusFilter, setWiStatusFilter] = useState("");
  const [wiSearch, setWiSearch] = useState("");
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [creating, setCreating] = useState(false);
  const [newItem, setNewItem] = useState({ title: "", type: "Task", priority: "Medium", description: "", storyPoints: "" });
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set());

  // Sprints state (for backlog inline dropdown)
  const [backlogSprints, setBacklogSprints] = useState<{id:string;name:string;status:string}[]>([]);

  // Milestones — now self-contained in MilestonesTab

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
  const fetchBoardData = useCallback(() => {
    if (!projectId) return;
    setBoardLoading(true);
    const params = new URLSearchParams();
    if (boardIncludeCompleted) params.set("includeCompleted", "true");
    if (boardSprintFilter) params.set("sprintId", boardSprintFilter);
    const qs = params.toString();
    Promise.all([
      fetch(`/api/pm/projects/${projectId}/board-columns`).then(r => r.ok ? r.json() : []),
      fetch(`/api/pm/work-items/board/${projectId}${qs ? `?${qs}` : ""}`).then(r => r.ok ? r.json() : {}),
      fetch(`/api/pm/projects/${projectId}/sprints`).then(r => r.ok ? r.json() : []),
    ]).then(([cols, boardData, sprintsData]) => {
      setBoardColumns(Array.isArray(cols) ? cols : []);
      const items: BoardWorkItem[] = [];
      if (boardData && typeof boardData === "object") {
        Object.values(boardData).forEach((arr: unknown) => {
          if (Array.isArray(arr)) items.push(...arr);
        });
      }
      setBoardItems(items);
      setBoardSprints(Array.isArray(sprintsData) ? sprintsData : []);
    }).catch(() => {}).finally(() => setBoardLoading(false));
  }, [projectId, boardIncludeCompleted, boardSprintFilter]);

  useEffect(() => {
    if (activeTab !== "board") return;
    fetchBoardData();
  }, [activeTab, fetchBoardData]);

  // Board DnD handler
  const handleBoardDrop = async (item: BoardWorkItem, targetStatus: string) => {
    if (item.status === targetStatus) return;
    const prevItems = [...boardItems];
    // Optimistic update
    setBoardItems(prev => prev.map(i => i.id === item.id ? { ...i, status: targetStatus } : i));
    setDraggedItem(null);
    setDragOverColumn(null);
    try {
      const res = await fetch(`/api/pm/work-items/${item.id}/move`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status: targetStatus }),
      });
      if (!res.ok) throw new Error();
    } catch {
      setBoardItems(prevItems); // Rollback
    }
  };

  // Board client-side filter
  const filteredBoardItems = boardItems.filter(item => {
    if (boardSearch && !item.title.toLowerCase().includes(boardSearch.toLowerCase()) &&
        !item.workItemNumber.toLowerCase().includes(boardSearch.toLowerCase())) return false;
    if (boardTypeFilter && item.type !== boardTypeFilter) return false;
    return true;
  });

  // Swimlane grouping
  const swimlaneGroups = (() => {
    if (boardSwimlane === "none") return new Map([["all", filteredBoardItems]]);
    const map = new Map<string, BoardWorkItem[]>();
    filteredBoardItems.forEach(item => {
      let key = "";
      if (boardSwimlane === "assignee") key = item.assigneeUserId || "unassigned";
      else if (boardSwimlane === "priority") key = item.priority;
      else if (boardSwimlane === "type") key = item.type;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(item);
    });
    // "Atanmamıx" en sona
    if (boardSwimlane === "assignee" && map.has("unassigned")) {
      const u = map.get("unassigned")!;
      map.delete("unassigned");
      map.set("unassigned", u);
    }
    return map;
  })();

  const getSwimlaneLabel = (key: string) => {
    if (key === "all") return "";
    if (key === "unassigned") return "Atanmamıx";
    if (boardSwimlane === "priority") return ({ Critical:"🔴 Kritik", High:"🟠 Yüksek", Medium:"🟡 Orta", Low:"🟢 Düşük" } as Record<string,string>)[key] || key;
    if (boardSwimlane === "type") return `${TYPE_ICONS[key] ?? "✅"} ${key}`;
    return key.slice(0, 8) + "..."; // userId kısalt
  };

  // Fetch metrics data
  useEffect(() => {
    if (activeTab !== "metrics" || !projectId) return;
    setMetricsLoading(true);
    Promise.all([
      fetch(`/api/pm/projects/${projectId}/velocity`).then(r => r.ok ? r.json() : []),
      fetch(`/api/pm/projects/${projectId}/metrics-summary`).then(r => r.ok ? r.json() : null),
      fetch(`/api/pm/projects/${projectId}/sprints`).then(r => r.ok ? r.json() : []),
    ]).then(([vel, summary, sprints]) => {
      setVelocity(Array.isArray(vel) ? vel : []);
      setMetricsSummary(summary);
      const sprintList = Array.isArray(sprints) ? sprints : [];
      setMetricsSprints(sprintList);
      // Auto-select first active or latest sprint
      if (!selectedSprintId && sprintList.length > 0) {
        const active = sprintList.find((s: SprintOption) => s.status === "Active");
        setSelectedSprintId(active ? active.id : sprintList[sprintList.length - 1].id);
      }
    }).catch(() => {})
    .finally(() => setMetricsLoading(false));
  }, [activeTab, projectId]);

  // Fetch burndown when sprint selected
  useEffect(() => {
    if (!selectedSprintId) return;
    setBurndownLoading(true);
    fetch(`/api/pm/sprints/${selectedSprintId}/burndown`)
      .then(r => r.ok ? r.json() : null)
      .then(data => setBurndown(data))
      .catch(() => setBurndown(null))
      .finally(() => setBurndownLoading(false));
  }, [selectedSprintId]);

  // Fetch work items
  const fetchWorkItems = useCallback(async () => {
    if (!projectId) return;
    setWorkItemsLoading(true);
    try {
      const params = new URLSearchParams({ view: workItemView });
      if (wiTypeFilter) params.set("type", wiTypeFilter);
      if (wiStatusFilter) params.set("status", wiStatusFilter);
      const res = await fetch(`/api/pm/projects/${projectId}/backlog?${params}`);
      if (res.ok) {
        const data = await res.json();
        setWorkItems(Array.isArray(data) ? data : []);
      }
    } catch { /* */ }
    finally { setWorkItemsLoading(false); }
  }, [projectId, workItemView, wiTypeFilter, wiStatusFilter]);

  useEffect(() => {
    if (activeTab === "workitems") {
      fetchWorkItems();
      // Fetch sprints for inline assignment dropdown
      fetch(`/api/pm/projects/${projectId}/sprints`)
        .then(r => r.ok ? r.json() : [])
        .then(data => setBacklogSprints(Array.isArray(data) ? data : []))
        .catch(() => {});
    }
  }, [activeTab, fetchWorkItems, projectId]);

  const handleSprintAssign = async (workItemId: string, sprintId: string | null) => {
    try {
      await fetch(`/api/pm/work-items/${workItemId}/sprint`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sprintId: sprintId || null }),
      });
      fetchWorkItems();
    } catch { /* */ }
  };

  const createWorkItem = async () => {
    if (!newItem.title.trim() || !projectId) return;
    setCreating(true);
    try {
      const res = await fetch("/api/pm/work-items", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          projectId,
          title: newItem.title,
          type: newItem.type,
          priority: newItem.priority,
          description: newItem.description || null,
          storyPoints: newItem.storyPoints ? parseInt(newItem.storyPoints) : undefined,
        }),
      });
      if (res.ok) {
        setNewItem({ title: "", type: "Task", priority: "Medium", description: "", storyPoints: "" });
        setShowCreateForm(false);
        fetchWorkItems();
        fetchProject();
      }
    } catch { /* */ }
    finally { setCreating(false); }
  };

  const getItemId = (item: BacklogWorkItem): string => {
    if (typeof item.id === "string") return item.id;
    return item.id?.value ?? "";
  };

  const toggleNode = (id: string) => {
    setExpandedNodes(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  };

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
        <button onClick={() => router.back()} className="mt-3 text-xs text-indigo-400 hover:text-indigo-300 transition-colors">  Geri</button>
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

      {/* Tabs  kategori bazlı */}
      <div className="flex flex-wrap items-center gap-1 border-b border-[var(--color-border)]">
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
              /* ── Edit Mode ── */
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
              /* ── View Mode ── */
              <div className="space-y-5">
                <h3 className="text-sm font-semibold text-[var(--color-text)] mb-4">Proje Bilgileri</h3>
                <div className="grid grid-cols-2 gap-y-4 gap-x-8">
                  <InfoRow icon={FolderKanban} label="Anahtar" value={project.key} mono />
                  <InfoRow icon={Calendar} label="Oluşturma" value={formatDate(project.createdAt)} />
                  <InfoRow icon={Calendar} label="Başlangıç" value={formatDate(project.startDate)} />
                  <InfoRow icon={Calendar} label="Hedef Bitiş" value={formatDate(project.targetEndDate)} />
                  <InfoRow icon={GitBranch} label="Metodoloji" value={methodCfg.label} badgeColors={methodCfg} />
                  <InfoRow icon={CatIcon} label="Kategori" value={catCfg.label} badgeColors={catCfg} />
                  <InfoRow icon={Briefcase} label="Portfolyo" value={project.portfolioName || ""} />
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

            {/* Tamamlanan & Yakında */}
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
              <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3">Özellikler</h3>
              <ul className="space-y-3">
                {[
                  { icon: ListTodo, label: "İş Kalemleri", desc: "Tablo & ağaç görünüm", done: true },
                  { icon: Layout, label: "Kanban Board", desc: "Sürükle-bırak, swimlane, filtre", done: true },
                  { icon: Milestone, label: "Milestones", desc: "Timeline görünümü", done: true },
                  { icon: BarChart3, label: "Metrikler", desc: "Velocity, Burndown", done: true },
                  { icon: GitBranch, label: "Ağaç Görünüm", desc: "Hiyerarşik drag & drop sıralama", done: false },
                ].map(item => (
                  <li key={item.label} className="flex items-start gap-3">
                    <div className={cn("w-8 h-8 rounded-lg flex items-center justify-center mt-0.5 shrink-0",
                      item.done ? "bg-emerald-500/10" : "bg-[var(--color-border)]")}>
                      <item.icon className={cn("w-4 h-4", item.done ? "text-emerald-400" : "text-[var(--color-text-muted)]")} />
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

      {/* WorkItems Tab */}
      {activeTab === "workitems" && (
        <div className="space-y-4">
          {/* Toolbar */}
          <div className="flex items-center gap-3 flex-wrap">
            {/* View toggle */}
            <div className="flex items-center rounded-lg border border-[var(--color-border)] overflow-hidden">
              <button onClick={() => setWorkItemView("table")}
                className={cn("flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium transition-colors",
                  workItemView === "table" ? "bg-indigo-500/20 text-indigo-400" : "text-[var(--color-text-muted)] hover:text-[var(--color-text)]")}>
                <Table2 className="w-3.5 h-3.5" /> Tablo
              </button>
              <button onClick={() => setWorkItemView("tree")}
                className={cn("flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium transition-colors border-l border-[var(--color-border)]",
                  workItemView === "tree" ? "bg-indigo-500/20 text-indigo-400" : "text-[var(--color-text-muted)] hover:text-[var(--color-text)]")}>
                <TreePine className="w-3.5 h-3.5" /> Ağaç
              </button>
            </div>

            {/* Search */}
            <div className="relative flex-1 min-w-[200px] max-w-xs">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-[var(--color-text-muted)]" />
              <input type="text" placeholder="Ara..." value={wiSearch} onChange={e => setWiSearch(e.target.value)}
                className="w-full pl-8 pr-3 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
            </div>

            {/* Filters */}
            <select value={wiTypeFilter} onChange={e => setWiTypeFilter(e.target.value)}
              className="px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
              <option value="">Tüm Tipler</option>
              {WORK_ITEM_TYPES.map(t => <option key={t.value} value={t.value}>{t.icon} {t.label}</option>)}
            </select>

            <select value={wiStatusFilter} onChange={e => setWiStatusFilter(e.target.value)}
              className="px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
              <option value="">Tüm Durumlar</option>
              {Object.entries(STATUS_LABELS).map(([k,v]) => <option key={k} value={k}>{v.label}</option>)}
            </select>

            {/* Spacer + Actions */}
            <div className="flex-1" />
            <button onClick={() => fetchWorkItems()}
              className="p-1.5 rounded-lg border border-[var(--color-border)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-border)]/50 transition-colors">
              <RefreshCw className="w-3.5 h-3.5" />
            </button>
            <button onClick={() => setShowCreateForm(!showCreateForm)}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium bg-indigo-600 text-white hover:bg-indigo-700 transition-colors">
              <Plus className="w-3.5 h-3.5" /> Yeni İş Kalemi
            </button>
          </div>

          {/* Create Form */}
          {showCreateForm && (
            <div className="rounded-xl border border-indigo-500/30 bg-indigo-500/5 p-4 space-y-3">
              <h4 className="text-sm font-semibold text-[var(--color-text)] flex items-center gap-2">
                <Plus className="w-4 h-4 text-indigo-400" /> Yeni İş Kalemi Oluştur
              </h4>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
                <div className="lg:col-span-2">
                  <label className="block text-[10px] font-medium text-[var(--color-text-muted)] mb-1">Baxlık *</label>
                  <input type="text" value={newItem.title} onChange={e => setNewItem(d => ({ ...d, title: e.target.value }))}
                    placeholder="İş kalemi baxlıxı..." autoFocus
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
                </div>
                <div>
                  <label className="block text-[10px] font-medium text-[var(--color-text-muted)] mb-1">Tip</label>
                  <select value={newItem.type} onChange={e => setNewItem(d => ({ ...d, type: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    {WORK_ITEM_TYPES.map(t => <option key={t.value} value={t.value}>{t.icon} {t.label}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-[10px] font-medium text-[var(--color-text-muted)] mb-1">ncelik</label>
                  <select value={newItem.priority} onChange={e => setNewItem(d => ({ ...d, priority: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    <option value="Low">Düşük</option><option value="Medium">Orta</option>
                    <option value="High">Yüksek</option><option value="Critical">Kritik</option>
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div>
                  <label className="block text-[10px] font-medium text-[var(--color-text-muted)] mb-1">Açıklama</label>
                  <textarea rows={2} value={newItem.description} onChange={e => setNewItem(d => ({ ...d, description: e.target.value }))}
                    placeholder="Detaylar..."
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40 resize-none" />
                </div>
                <div className="flex items-end gap-3">
                  <div className="w-24">
                    <label className="block text-[10px] font-medium text-[var(--color-text-muted)] mb-1">Story Points</label>
                    <input type="number" min="0" max="100" value={newItem.storyPoints} onChange={e => setNewItem(d => ({ ...d, storyPoints: e.target.value }))}
                      placeholder="—"
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
                  </div>
                  <div className="flex gap-2 pb-0.5">
                    <button onClick={createWorkItem} disabled={creating || !newItem.title.trim()}
                      className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-medium bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors">
                      {creating ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />} Oluştur
                    </button>
                    <button onClick={() => setShowCreateForm(false)}
                      className="px-3 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50 transition-colors">
                      <X className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Content */}
          {workItemsLoading ? (
            <div className="flex items-center justify-center py-12"><Loader2 className="w-6 h-6 text-indigo-400 animate-spin" /></div>
          ) : workItems.length === 0 ? (
            <div className="text-center py-16 text-[var(--color-text-muted)]">
              <ListTodo className="w-12 h-12 mx-auto opacity-20 mb-3" />
              <p className="text-sm font-medium">Henüz ix kalemi yok</p>
              <p className="text-[11px] mt-1">"Yeni İş Kalemi" ile başlayın</p>
            </div>
          ) : workItemView === "table" ? (
            /* ── Table View ── */
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] overflow-hidden">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg)]">
                    <th className="text-left px-4 py-2.5 text-[10px] font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Numara</th>
                    <th className="text-left px-4 py-2.5 text-[10px] font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Baxlık</th>
                    <th className="text-left px-4 py-2.5 text-[10px] font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Tip</th>
                    <th className="text-left px-4 py-2.5 text-[10px] font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Durum</th>
                    <th className="text-left px-4 py-2.5 text-[10px] font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">ncelik</th>
                    <th className="text-center px-4 py-2.5 text-[10px] font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">SP</th>
                    <th className="text-center px-4 py-2.5 text-[10px] font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">WSJF</th>
                    <th className="text-left px-4 py-2.5 text-[10px] font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Sprint</th>
                  </tr>
                </thead>
                <tbody>
                  {workItems
                    .filter(item => !wiSearch || item.title.toLowerCase().includes(wiSearch.toLowerCase()) || item.workItemNumber.toLowerCase().includes(wiSearch.toLowerCase()))
                    .map(item => {
                    const itemId = getItemId(item);
                    const stl = STATUS_LABELS[item.status] || { label: item.status, color: "bg-gray-500/20 text-gray-400" };
                    return (
                      <tr key={itemId}
                        onClick={() => router.push(`/dashboard/tasks/${itemId}`)}
                        className="border-b border-[var(--color-border)] hover:bg-indigo-500/5 transition-colors cursor-pointer group">
                        <td className="px-4 py-2.5">
                          <span className="text-xs font-mono text-[var(--color-text-muted)] group-hover:text-indigo-400 transition-colors">{item.workItemNumber}</span>
                        </td>
                        <td className="px-4 py-2.5">
                          <span className="text-xs text-[var(--color-text)]">{item.title}</span>
                        </td>
                        <td className="px-4 py-2.5">
                          <span className="text-xs">{TYPE_ICONS[item.type] ?? "x9"} {item.type}</span>
                        </td>
                        <td className="px-4 py-2.5">
                          <span className={cn("inline-flex px-2 py-0.5 rounded-full text-[10px] font-medium", stl.color)}>{stl.label}</span>
                        </td>
                        <td className="px-4 py-2.5">
                          <div className="flex items-center gap-1.5">
                            <span className={cn("w-2 h-2 rounded-full", PRIORITY_COLORS[item.priority] ?? "bg-gray-500")} />
                            <span className="text-xs text-[var(--color-text-muted)]">{item.priority}</span>
                          </div>
                        </td>
                        <td className="px-4 py-2.5 text-center">
                          {item.storyPoints != null && item.storyPoints > 0 ? (
                            <span className="text-[10px] font-bold text-indigo-400 bg-indigo-500/10 px-1.5 py-0.5 rounded-full">{item.storyPoints}</span>
                          ) : <span className="text-[10px] text-[var(--color-text-muted)]"></span>}
                        </td>
                        <td className="px-4 py-2.5 text-center">
                          {item.wsjfScore != null && item.wsjfScore > 0 ? (
                            <span className="text-[10px] font-bold text-amber-400 bg-amber-500/10 px-1.5 py-0.5 rounded-full">{item.wsjfScore.toFixed(1)}</span>
                          ) : <span className="text-[10px] text-[var(--color-text-muted)]"></span>}
                        </td>
                        <td className="px-4 py-2.5" onClick={e => e.stopPropagation()}>
                          <select
                            className="text-[10px] rounded border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-1.5 py-0.5 w-24 truncate"
                            value={item.sprintId || ""}
                            onChange={e => handleSprintAssign(itemId, e.target.value || null)}
                          >
                            <option value="">—</option>
                            {backlogSprints.filter(s => s.status !== "Completed" && s.status !== "Cancelled").map(s => (
                              <option key={s.id} value={s.id}>{s.name}</option>
                            ))}
                          </select>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
              <div className="px-4 py-2 border-t border-[var(--color-border)] bg-[var(--color-bg)]">
                <span className="text-[10px] text-[var(--color-text-muted)]">Toplam: {workItems.length} ix kalemi</span>
              </div>
            </div>
          ) : (
            /* ── Tree View ── */
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-4 space-y-1">
              {workItems
                .filter(item => !wiSearch || item.title.toLowerCase().includes(wiSearch.toLowerCase()) || item.workItemNumber.toLowerCase().includes(wiSearch.toLowerCase()))
                .map(item => <TreeNode key={getItemId(item)} item={item} depth={0} getItemId={getItemId} expandedNodes={expandedNodes} toggleNode={toggleNode} router={router} />)}
              {workItems.length > 0 && (
                <div className="pt-2 border-t border-[var(--color-border)] mt-3">
                  <span className="text-[10px] text-[var(--color-text-muted)]">Toplam: {workItems.length} kök ix kalemi</span>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* Board Tab */}
      {activeTab === "board" && (
        <div className="space-y-4">
          {/* Board Toolbar */}
          <div className="flex items-center gap-3 flex-wrap">
            {/* Search */}
            <div className="relative flex-1 min-w-[180px] max-w-xs">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-[var(--color-text-muted)]" />
              <input type="text" placeholder="Kart ara..." value={boardSearch} onChange={e => setBoardSearch(e.target.value)}
                className="w-full pl-8 pr-3 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40" />
            </div>

            {/* Type filter */}
            <select value={boardTypeFilter} onChange={e => setBoardTypeFilter(e.target.value)}
              className="px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
              <option value="">Tüm Tipler</option>
              {WORK_ITEM_TYPES.map(t => <option key={t.value} value={t.value}>{t.icon} {t.label}</option>)}
            </select>

            {/* Sprint filter */}
            {boardSprints.length > 0 && (
              <select value={boardSprintFilter} onChange={e => setBoardSprintFilter(e.target.value)}
                className="px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                <option value="">Tüm Sprintler</option>
                {boardSprints.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            )}

            {/* Swimlane */}
            <select value={boardSwimlane} onChange={e => setBoardSwimlane(e.target.value as typeof boardSwimlane)}
              className="px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
              <option value="none">Swimlane: Kapalı</option>
              <option value="assignee">👤 Kişiye Göre</option>
              <option value="priority">x ncelixe Göre</option>
              <option value="type">x9 Tipe Göre</option>
            </select>

            <div className="flex-1" />

            {/* Include completed toggle */}
            <label className="flex items-center gap-1.5 cursor-pointer select-none">
              <input type="checkbox" checked={boardIncludeCompleted} onChange={e => setBoardIncludeCompleted(e.target.checked)}
                className="w-3.5 h-3.5 rounded border-[var(--color-border)] accent-indigo-500" />
              <span className="text-[10px] text-[var(--color-text-muted)]">Tamamlananlar</span>
            </label>

            {/* Refresh */}
            <button onClick={fetchBoardData}
              className="p-1.5 rounded-lg border border-[var(--color-border)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-border)]/50 transition-colors">
              <RefreshCw className="w-3.5 h-3.5" />
            </button>
          </div>

          {boardLoading ? (
            <div className="flex items-center justify-center py-12"><Loader2 className="w-6 h-6 text-indigo-400 animate-spin" /></div>
          ) : boardColumns.length === 0 ? (
            <div className="text-center py-12 text-[var(--color-text-muted)]">
              <Layout className="w-10 h-10 mx-auto opacity-30 mb-2" />
              <p className="text-sm">Board kolonları henüz oluşturulmadı</p>
            </div>
          ) : (
            <div className="space-y-6">
              {Array.from(swimlaneGroups.entries()).map(([laneKey, laneItems]) => (
                <div key={laneKey}>
                  {/* Swimlane header */}
                  {boardSwimlane !== "none" && (
                    <div className="flex items-center gap-2 mb-2 pb-1.5 border-b border-[var(--color-border)]">
                      <span className="text-xs font-semibold text-[var(--color-text)]">{getSwimlaneLabel(laneKey)}</span>
                      <span className="text-[10px] text-[var(--color-text-muted)] bg-[var(--color-border)] px-1.5 py-0.5 rounded-full">{laneItems.length}</span>
                    </div>
                  )}

                  {/* Columns */}
                  <div className="flex gap-3 overflow-x-auto pb-2" style={{ minHeight: boardSwimlane === "none" ? 400 : 200 }}>
                    {boardColumns.map(col => {
                      const colItems = laneItems.filter(item => item.status === col.mappedStatus);
                      const allColItems = boardItems.filter(item => item.status === col.mappedStatus);
                      const isOverWip = col.wipLimit != null && allColItems.length > col.wipLimit;
                      const isDragOver = dragOverColumn === `${laneKey}-${col.mappedStatus}`;
                      return (
                        <div key={col.id}
                          onDragOver={e => { e.preventDefault(); setDragOverColumn(`${laneKey}-${col.mappedStatus}`); }}
                          onDragLeave={() => setDragOverColumn(null)}
                          onDrop={e => { e.preventDefault(); if (draggedItem) handleBoardDrop(draggedItem, col.mappedStatus); }}
                          className={cn(
                            "flex-shrink-0 w-72 rounded-xl border bg-[var(--color-card-bg)] flex flex-col transition-all duration-200",
                            isDragOver ? "border-indigo-500 ring-2 ring-indigo-500/30 bg-indigo-500/5" :
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
                          <div className="p-2 flex-1 space-y-2 overflow-y-auto" style={{ maxHeight: boardSwimlane === "none" ? 500 : 300 }}>
                            {colItems.map(item => (
                              <div key={item.id}
                                draggable
                                onDragStart={() => setDraggedItem(item)}
                                onDragEnd={() => { setDraggedItem(null); setDragOverColumn(null); }}
                                onClick={() => router.push(`/dashboard/tasks/${item.id}`)}
                                className={cn(
                                  "p-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] hover:border-indigo-500/30 hover:bg-indigo-500/5 transition-all cursor-grab active:cursor-grabbing group",
                                  draggedItem?.id === item.id && "opacity-40 scale-95"
                                )}>
                                <div className="flex items-center gap-1.5 mb-1">
                                  <span className="text-xs">{TYPE_ICONS[item.type] ?? "x9"}</span>
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
                              <div className={cn(
                                "text-center py-6 text-[10px] text-[var(--color-text-muted)] rounded-lg transition-colors",
                                isDragOver ? "bg-indigo-500/10 text-indigo-400" : "opacity-50"
                              )}>
                                {isDragOver ? "Buraya bırak" : "Box"}
                              </div>
                            )}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Metrics Tab */}
      {activeTab === "metrics" && (
        <div className="space-y-6">
          {metricsLoading ? (
            <div className="flex items-center justify-center py-12"><Loader2 className="w-6 h-6 text-indigo-400 animate-spin" /></div>
          ) : (
            <>
              {/*  zet Kartları  */}
              <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                {[
                  { label: "Toplam SP", value: metricsSummary?.totalStoryPoints ?? 0, icon: "a", color: "from-indigo-500/20 to-indigo-600/5 border-indigo-500/20" },
                  { label: "Tamamlanan SP", value: metricsSummary?.completedStoryPoints ?? 0, icon: "S&", color: "from-emerald-500/20 to-emerald-600/5 border-emerald-500/20" },
                  { label: "Ort. Velocity", value: `${(metricsSummary?.averageVelocity ?? 0).toFixed(1)} SP`, icon: "xa", color: "from-violet-500/20 to-violet-600/5 border-violet-500/20" },
                  { label: "Aktif İş Kalemi", value: metricsSummary?.activeWorkItems ?? 0, icon: "x", color: "from-amber-500/20 to-amber-600/5 border-amber-500/20" },
                ].map(card => (
                  <div key={card.label} className={`rounded-xl border bg-gradient-to-br ${card.color} p-5 flex items-start gap-3`}>
                    <span className="text-2xl">{card.icon}</span>
                    <div>
                      <p className="text-[11px] uppercase tracking-wider text-[var(--color-text-muted)] mb-1">{card.label}</p>
                      <p className="text-2xl font-bold text-[var(--color-text)]">{card.value}</p>
                    </div>
                  </div>
                ))}
              </div>

              {/*  Kanban Metrikleri (Lead Time & Cycle Time)  */}
              {(metricsSummary?.averageLeadTimeDays !== null || metricsSummary?.averageCycleTimeDays !== null) && (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div className="rounded-xl border border-cyan-500/20 bg-gradient-to-br from-cyan-500/10 to-transparent p-5">
                    <div className="flex items-center gap-2 mb-2">
                      <span className="text-lg">x`</span>
                      <span className="text-xs font-semibold uppercase tracking-wider text-cyan-400">Ort. Lead Time</span>
                    </div>
                    <p className="text-3xl font-bold text-[var(--color-text)]">
                      {metricsSummary?.averageLeadTimeDays != null ? `${metricsSummary.averageLeadTimeDays.toFixed(1)} gün` : ""}
                    </p>
                    <p className="text-[10px] text-[var(--color-text-muted)] mt-1">Oluşturma   Tamamlanma arası ortalama süre</p>
                  </div>
                  <div className="rounded-xl border border-teal-500/20 bg-gradient-to-br from-teal-500/10 to-transparent p-5">
                    <div className="flex items-center gap-2 mb-2">
                      <span className="text-lg">⏱️</span>
                      <span className="text-xs font-semibold uppercase tracking-wider text-teal-400">Ort. Cycle Time</span>
                    </div>
                    <p className="text-3xl font-bold text-[var(--color-text)]">
                      {metricsSummary?.averageCycleTimeDays != null ? `${metricsSummary.averageCycleTimeDays.toFixed(1)} gün` : ""}
                    </p>
                    <p className="text-[10px] text-[var(--color-text-muted)] mt-1">İlk başlama   Tamamlanma arası ortalama süre</p>
                  </div>
                </div>
              )}

              {/* ── Velocity Chart ── */}
              <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-6">
                <h3 className="text-sm font-semibold text-[var(--color-text)] mb-4 flex items-center gap-2">
                  <BarChart3 className="w-4 h-4 text-indigo-400" /> Velocity Chart
                </h3>
                {velocity.length === 0 ? (
                  <div className="text-center py-10 text-[var(--color-text-muted)]">
                    <BarChart3 className="w-8 h-8 mx-auto opacity-20 mb-2" />
                    <p className="text-xs">Tamamlanmıx sprint bulunamadı</p>
                    <p className="text-[10px] mt-1">Sprint tamamlandıxında velocity verileri burada görünecek</p>
                  </div>
                ) : (
                  <RechartsBar
                    data={velocity.map(v => ({ name: v.sprintName, planned: v.plannedPoints, completed: v.completedPoints }))}
                    avg={velocity.length > 0 ? velocity.reduce((s, v) => s + v.completedPoints, 0) / velocity.length : 0}
                  />
                )}
              </div>

              {/* ── Burndown Chart ── */}
              <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-6">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-sm font-semibold text-[var(--color-text)] flex items-center gap-2">
                    <GitBranch className="w-4 h-4 text-amber-400" /> Burndown Chart
                  </h3>
                  <select
                    className="text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-3 py-1.5"
                    value={selectedSprintId}
                    onChange={e => setSelectedSprintId(e.target.value)}
                  >
                    <option value="">Sprint seçin...</option>
                    {metricsSprints.map(s => (
                      <option key={s.id} value={s.id}>{s.name} ({s.status === "Active" ? "Aktif" : s.status === "Completed" ? "Tamamlandı" : s.status})</option>
                    ))}
                  </select>
                </div>
                {burndownLoading ? (
                  <div className="flex items-center justify-center py-10"><Loader2 className="w-5 h-5 animate-spin text-amber-400" /></div>
                ) : !burndown || burndown.dataPoints.length === 0 ? (
                  <div className="text-center py-10 text-[var(--color-text-muted)]">
                    <GitBranch className="w-8 h-8 mx-auto opacity-20 mb-2" />
                    <p className="text-xs">{selectedSprintId ? "Bu sprint için burndown verisi yok" : "Sprint seçerek burndown grafixini görüntüleyin"}</p>
                  </div>
                ) : (() => {
                  const start = new Date(burndown.startDate);
                  const end = new Date(burndown.endDate);
                  const totalDays = Math.max(1, Math.ceil((end.getTime() - start.getTime()) / 86400000));
                  const idealLine = Array.from({ length: totalDays + 1 }, (_, i) => {
                    const d = new Date(start); d.setDate(d.getDate() + i);
                    return { date: d.toISOString().split("T")[0], ideal: Math.round(burndown.plannedPoints * (1 - i / totalDays)) };
                  });
                  const actual = burndown.dataPoints.map(p => ({
                    date: new Date(p.date).toISOString().split("T")[0],
                    remaining: p.remainingPoints, completed: p.completedPoints,
                  }));
                  return <RechartsLine data={actual} ideal={idealLine} />;
                })()}
              </div>

              {/*  Dağılım Grafikleri  */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {/* Tip Dağılımı */}
                <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-muted)] mb-4">Tip Dağılımı</h4>
                  {metricsSummary && Object.entries(metricsSummary.byType).sort((a, b) => b[1] - a[1]).map(([type, count]) => {
                    const total = metricsSummary.totalWorkItems || 1;
                    const pct = (count / total) * 100;
                    return (
                      <div key={type} className="mb-3">
                        <div className="flex justify-between text-[11px] mb-1">
                          <span className="text-[var(--color-text)]">{TYPE_ICONS[type] || "x9"} {type}</span>
                          <span className="text-[var(--color-text-muted)]">{count} ({pct.toFixed(0)}%)</span>
                        </div>
                        <div className="h-2 rounded-full bg-[var(--color-bg)] overflow-hidden">
                          <div className="h-full rounded-full bg-indigo-500 transition-all" style={{ width: `${pct}%` }} />
                        </div>
                      </div>
                    );
                  })}
                </div>
                {/* Durum Dağılımı */}
                <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-muted)] mb-4">Durum Dağılımı</h4>
                  {metricsSummary && Object.entries(metricsSummary.byStatus).sort((a, b) => b[1] - a[1]).map(([status, count]) => {
                    const total = metricsSummary.totalWorkItems || 1;
                    const pct = (count / total) * 100;
                    const statusColors: Record<string, string> = { Backlog: "bg-slate-500", InProgress: "bg-amber-500", Done: "bg-emerald-500", Cancelled: "bg-red-500", InReview: "bg-purple-500", Testing: "bg-cyan-500" };
                    return (
                      <div key={status} className="mb-3">
                        <div className="flex justify-between text-[11px] mb-1">
                          <span className="text-[var(--color-text)]">{STATUS_LABELS[status]?.label || status}</span>
                          <span className="text-[var(--color-text-muted)]">{count} ({pct.toFixed(0)}%)</span>
                        </div>
                        <div className="h-2 rounded-full bg-[var(--color-bg)] overflow-hidden">
                          <div className={`h-full rounded-full ${statusColors[status] || "bg-gray-500"} transition-all`} style={{ width: `${pct}%` }} />
                        </div>
                      </div>
                    );
                  })}
                </div>
                {/* ncelik Dağılımı */}
                <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
                  <h4 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-muted)] mb-4">ncelik Dağılımı</h4>
                  {metricsSummary && Object.entries(metricsSummary.byPriority).sort((a, b) => b[1] - a[1]).map(([priority, count]) => {
                    const total = metricsSummary.totalWorkItems || 1;
                    const pct = (count / total) * 100;
                    return (
                      <div key={priority} className="mb-3">
                        <div className="flex justify-between text-[11px] mb-1">
                          <span className="text-[var(--color-text)] flex items-center gap-1.5">
                            <span className={`w-2 h-2 rounded-full ${PRIORITY_COLORS[priority] || "bg-gray-500"}`} />
                            {priority}
                          </span>
                          <span className="text-[var(--color-text-muted)]">{count} ({pct.toFixed(0)}%)</span>
                        </div>
                        <div className="h-2 rounded-full bg-[var(--color-bg)] overflow-hidden">
                          <div className={`h-full rounded-full ${PRIORITY_COLORS[priority] || "bg-gray-500"} transition-all`} style={{ width: `${pct}%` }} />
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </>
          )}
        </div>
      )}

      {/* Sprints Tab */}
      {activeTab === "sprints" && (
        <SprintsTab projectId={projectId as string} />
      )}

      {/* Milestones Tab */}
      {activeTab === "milestones" && (
        <MilestonesTab projectId={projectId as string} />
      )}

      {/* Requirements Tab */}
      {activeTab === "requirements" && (
        <RequirementsTab projectId={projectId as string} projectKey={project?.key || ""} />
      )}

      {/* Test Senaryoları Tab */}
      {activeTab === "test-scenarios" && (
        <TestScenariosTab projectId={projectId as string} />
      )}

      {/* Test Planları Tab */}
      {activeTab === "test-plans" && (
        <TestPlansTab projectId={projectId as string} />
      )}

      {/* Settings Tab */}
      {activeTab === "settings" && (
        <BoardColumnSettings projectId={projectId as string} />
      )}

      {/* Releases Tab */}
      {activeTab === "releases" && (
        <ReleasesTab projectId={projectId as string} />
      )}

      {/* Wiki Tab */}
      {activeTab === "wiki" && (
        <WikiTab projectId={projectId as string} />
      )}
    </div>
  );
}

// ── Sub-components ──────────────────────────────────

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

function TreeNode({ item, depth, getItemId, expandedNodes, toggleNode, router }: {
  item: BacklogWorkItem; depth: number;
  getItemId: (item: BacklogWorkItem) => string;
  expandedNodes: Set<string>;
  toggleNode: (id: string) => void;
  router: ReturnType<typeof import("next/navigation").useRouter>;
}) {
  const itemId = getItemId(item);
  const hasChildren = item.children && item.children.length > 0;
  const isExpanded = expandedNodes.has(itemId);
  const stl = STATUS_LABELS[item.status] || { label: item.status, color: "bg-gray-500/20 text-gray-400" };

  const DEPTH_COLORS = [
    "border-l-violet-500/40",    // Epic
    "border-l-blue-500/40",      // Feature
    "border-l-emerald-500/40",   // Story
    "border-l-amber-500/40",     // Task
  ];

  return (
    <div>
      <div
        className={cn(
          "flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-indigo-500/5 transition-colors group",
          depth > 0 && "ml-5 border-l-2",
          depth > 0 && (DEPTH_COLORS[depth] || "border-l-gray-500/30")
        )}
      >
        {/* Expand/Collapse */}
        <button
          onClick={() => hasChildren && toggleNode(itemId)}
          className={cn("w-5 h-5 flex items-center justify-center rounded shrink-0 transition-colors",
            hasChildren ? "hover:bg-[var(--color-border)] text-[var(--color-text-muted)]" : "opacity-0")}
        >
          {hasChildren && (isExpanded ? <ChevronDown className="w-3.5 h-3.5" /> : <ChevronRight className="w-3.5 h-3.5" />)}
        </button>

        {/* Type icon */}
        <span className="text-sm shrink-0">{TYPE_ICONS[item.type] ?? "x9"}</span>

        {/* Number */}
        <span className="text-[10px] font-mono text-[var(--color-text-muted)] shrink-0">{item.workItemNumber}</span>

        {/* Title  clickable */}
        <button
          onClick={() => router.push(`/dashboard/tasks/${itemId}`)}
          className="text-xs text-[var(--color-text)] hover:text-indigo-400 transition-colors truncate text-left flex-1 min-w-0"
        >
          {item.title}
        </button>

        {/* Status badge */}
        <span className={cn("inline-flex px-2 py-0.5 rounded-full text-[9px] font-medium shrink-0", stl.color)}>
          {stl.label}
        </span>

        {/* Priority dot */}
        <span className={cn("w-2 h-2 rounded-full shrink-0", PRIORITY_COLORS[item.priority] ?? "bg-gray-500")} title={item.priority} />

        {/* SP */}
        {item.storyPoints != null && item.storyPoints > 0 ? (
          <span className="text-[9px] font-bold text-indigo-400 bg-indigo-500/10 px-1.5 py-0.5 rounded-full shrink-0">{item.storyPoints} SP</span>
        ) : null}
      </div>

      {/* Children */}
      {hasChildren && isExpanded && (
        <div className="space-y-0.5">
          {item.children!.map(child => (
            <TreeNode key={getItemId(child)} item={child} depth={depth + 1}
              getItemId={getItemId} expandedNodes={expandedNodes} toggleNode={toggleNode} router={router} />
          ))}
        </div>
      )}
    </div>
  );
}

