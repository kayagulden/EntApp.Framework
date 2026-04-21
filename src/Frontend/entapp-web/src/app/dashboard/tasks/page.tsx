"use client";

import { useState, useEffect, useCallback } from "react";
import { useAuthStore } from "@/stores";
import { useRouter } from "next/navigation";
import {
  ListTodo,
  Plus,
  Search,
  Clock,
  CheckCircle2,
  XCircle,
  ArrowUpRight,
  Loader2,
  User,
  X,
  Send,
  UserCheck,
  CircleDot,
  Circle,
  CircleCheck,
  Calendar,
  Tag,
  Inbox,
  TicketIcon,
  FolderKanban,
  Unlink,
  AlertTriangle,
  Bug,
  Sparkles,
  TrendingUp,
  Layers,
  ChevronDown,
} from "lucide-react";
import { cn } from "@/lib/utils";

// ── Interfaces ──────────────────────────────────────────────

interface TaskData {
  id: { value: string } | string;
  taskNumber: string;
  title: string;
  status: string;
  priority: string;
  type: string;
  assigneeUserId?: string;
  reporterUserId?: string;
  dueDate?: string;
  estimatedHours: number;
  projectKey?: string;
  sourceModule?: string;
  sourceType?: string;
  sourceId?: string;
  createdAt: string;
}

interface TaskListResult {
  items: TaskData[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

interface MyQueueData {
  queueId: string;
  name: string;
  code: string;
  role: string;
  ticketCount: number;
  unassignedCount: number;
  members?: QueueMemberData[];
}

interface QueueMemberData {
  userId: string;
  role: string;
  displayName?: string;
}

type TabKey = "my-tasks" | "my-created" | "queue-tasks";

// ── Helpers ─────────────────────────────────────────────────

function extractId(id: { value: string } | string | undefined): string {
  if (!id) return "";
  if (typeof id === "string") return id;
  return id.value;
}

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return "Az önce";
  if (mins < 60) return `${mins}dk`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}s`;
  const days = Math.floor(hours / 24);
  return `${days}g`;
}

function formatDate(dateStr?: string): string {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

// ── Config ──────────────────────────────────────────────────

const TASK_STATUS_CONFIG: Record<
  string,
  { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }
> = {
  Backlog: { icon: Circle, color: "text-slate-400", bg: "bg-slate-500/10 border-slate-500/20", label: "Beklemede" },
  Todo: { icon: CircleDot, color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Yapılacak" },
  InProgress: { icon: Clock, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "İşlemde" },
  InReview: { icon: ArrowUpRight, color: "text-purple-400", bg: "bg-purple-500/10 border-purple-500/20", label: "İnceleme" },
  Done: { icon: CircleCheck, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Tamamlandı" },
  Cancelled: { icon: XCircle, color: "text-gray-400", bg: "bg-gray-500/10 border-gray-500/20", label: "İptal" },
};

const PRIORITY_CONFIG: Record<string, { color: string; dot: string; label: string }> = {
  Low: { color: "text-slate-400", dot: "bg-slate-400", label: "Düşük" },
  Medium: { color: "text-blue-400", dot: "bg-blue-400", label: "Orta" },
  High: { color: "text-amber-400", dot: "bg-amber-400", label: "Yüksek" },
  Critical: { color: "text-red-400", dot: "bg-red-400", label: "Kritik" },
};

const TYPE_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; label: string }> = {
  Task: { icon: ListTodo, color: "text-blue-400", label: "Görev (Task)" },
  Bug: { icon: Bug, color: "text-red-400", label: "Hata" },
  Feature: { icon: Sparkles, color: "text-violet-400", label: "Özellik" },
  Improvement: { icon: TrendingUp, color: "text-teal-400", label: "İyileştirme" },
  Epic: { icon: Layers, color: "text-orange-400", label: "Epik" },
};

const TASK_STATUS_OPTIONS = ["Backlog", "Todo", "InProgress", "InReview", "Done", "Cancelled"];
const TASK_PRIORITY_OPTIONS = ["Low", "Medium", "High", "Critical"];
const TASK_TYPE_OPTIONS = ["Task", "Bug", "Feature", "Improvement", "Epic"];
const SOURCE_FILTER_OPTIONS = [
  { value: "", label: "Tüm Kaynaklar" },
  { value: "independent", label: "Bağımsız" },
  { value: "ticket", label: "Talep Bağlı" },
  { value: "project", label: "Proje Bağlı" },
];

const TABS: { key: TabKey; label: string; icon: React.ComponentType<{ className?: string }> }[] = [
  { key: "my-tasks", label: "Üzerimdeki", icon: UserCheck },
  { key: "my-created", label: "Oluşturduklarım", icon: ListTodo },
  { key: "queue-tasks", label: "Diğer İş Kalemleri", icon: Inbox },
];

const DEV_USERS = [
  { id: "868b6d11-0110-4182-9cc3-9a155f140fe4", fullName: "Ahmet Yılmaz" },
  { id: "b7dd400d-9aa8-4cc3-b973-a362e34ff39b", fullName: "Elif Demir" },
  { id: "dfbd1ff2-8ba9-424d-8e3b-00e5e35d8edc", fullName: "Mehmet Kaya" },
  { id: "84188840-d0dc-4080-845a-c0f25192ce22", fullName: "Ayşe Çelik" },
  { id: "96a07d00-94c7-4f08-bc30-80b32fbbb139", fullName: "Can Öztürk" },
];

function getUserName(userId?: string): string {
  if (!userId) return "—";
  const found = DEV_USERS.find((u) => u.id === userId);
  return found ? found.fullName : userId.slice(0, 8);
}

// ── Component ───────────────────────────────────────────────

export default function TasksPage() {
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<TabKey>("my-tasks");
  const [tasks, setTasks] = useState<TaskData[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [priorityFilter, setPriorityFilter] = useState<string>("");
  const [typeFilter, setTypeFilter] = useState<string>("");
  const [sourceFilter, setSourceFilter] = useState<string>("");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const authUser = useAuthStore((s) => s.user);
  const currentUserId = authUser?.id ?? "";
  const [myQueues, setMyQueues] = useState<MyQueueData[]>([]);
  const [queueMembers, setQueueMembers] = useState<QueueMemberData[]>([]);

  // Tab counts
  const [myTasksCount, setMyTasksCount] = useState(0);
  const [myCreatedCount, setMyCreatedCount] = useState(0);
  const [queueTasksCount, setQueueTasksCount] = useState(0);

  // Create form
  const [showCreate, setShowCreate] = useState(false);
  const [formTitle, setFormTitle] = useState("");
  const [formDescription, setFormDescription] = useState("");
  const [formPriority, setFormPriority] = useState("Medium");
  const [formType, setFormType] = useState("Task");
  const [formAssignee, setFormAssignee] = useState("");
  const [formDueDate, setFormDueDate] = useState("");
  const [formEstimatedHours, setFormEstimatedHours] = useState("");
  const [formTags, setFormTags] = useState("");
  const [formSaving, setFormSaving] = useState(false);
  const [formError, setFormError] = useState("");

  // Fetch my queues
  useEffect(() => {
    if (!currentUserId) return;
    fetch(`/api/req/my-queues/${currentUserId}`)
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => {
        const list = Array.isArray(data) ? data : [];
        setMyQueues(list);
      })
      .catch(() => setMyQueues([]));
  }, [currentUserId]);

  // Fetch queue members (for all queues)
  useEffect(() => {
    if (myQueues.length === 0) return;
    const uniqueQueueIds = myQueues.map((q) => q.queueId);
    Promise.all(
      uniqueQueueIds.map((qid) =>
        fetch(`/api/req/queues/${qid}/members`)
          .then((r) => (r.ok ? r.json() : []))
          .catch(() => [])
      )
    ).then((results) => {
      const allMembers: QueueMemberData[] = [];
      const seen = new Set<string>();
      results.flat().forEach((m: QueueMemberData) => {
        if (!seen.has(m.userId)) {
          seen.add(m.userId);
          allMembers.push(m);
        }
      });
      setQueueMembers(allMembers);
    });
  }, [myQueues]);

  // Fetch tasks
  const fetchTasks = useCallback(async () => {
    if (!currentUserId) return;
    setLoading(true);
    try {
      const params = new URLSearchParams();
      params.set("page", page.toString());
      params.set("pageSize", pageSize.toString());
      if (statusFilter) params.set("status", statusFilter);
      if (priorityFilter) params.set("priority", priorityFilter);
      if (typeFilter) params.set("type", typeFilter);
      if (sourceFilter) params.set("sourceFilter", sourceFilter);

      if (activeTab === "my-tasks") {
        params.set("assignee", currentUserId);
      } else if (activeTab === "my-created") {
        params.set("reporterUserId", currentUserId);
      } else if (activeTab === "queue-tasks") {
        // Lead: tüm member görevleri, Member: sadece kendisi
        const isLead = myQueues.some((q) => q.role === "Lead");
        if (isLead) {
          const memberIds = queueMembers.map((m) => m.userId).join(",");
          if (memberIds) params.set("assigneeUserIds", memberIds);
        } else {
          params.set("assignee", currentUserId);
        }
      }

      const res = await fetch(`/api/pm/work-items?${params}`);
      if (res.ok) {
        const data: TaskListResult = await res.json();
        setTasks(data.items ?? []);
        setTotalCount(data.totalCount ?? 0);
      }
    } catch (err) {
      console.error("Failed to fetch tasks:", err);
    } finally {
      setLoading(false);
    }
  }, [currentUserId, activeTab, page, statusFilter, priorityFilter, typeFilter, sourceFilter, myQueues, queueMembers]);

  useEffect(() => {
    fetchTasks();
  }, [fetchTasks]);

  // Fetch tab counts
  useEffect(() => {
    if (!currentUserId) return;
    fetch(`/api/pm/work-items?assignee=${currentUserId}&pageSize=1`)
      .then((r) => (r.ok ? r.json() : { totalCount: 0 }))
      .then((d) => setMyTasksCount(d.totalCount ?? 0))
      .catch(() => {});
    fetch(`/api/pm/work-items?reporterUserId=${currentUserId}&pageSize=1`)
      .then((r) => (r.ok ? r.json() : { totalCount: 0 }))
      .then((d) => setMyCreatedCount(d.totalCount ?? 0))
      .catch(() => {});
    // Queue tasks count
    if (myQueues.length > 0) {
      const isLead = myQueues.some((q) => q.role === "Lead");
      const memberIds = isLead
        ? queueMembers.map((m) => m.userId).join(",")
        : currentUserId;
      const qParam = isLead ? `assigneeUserIds=${memberIds}` : `assignee=${memberIds}`;
      fetch(`/api/pm/work-items?${qParam}&pageSize=1`)
        .then((r) => (r.ok ? r.json() : { totalCount: 0 }))
        .then((d) => setQueueTasksCount(d.totalCount ?? 0))
        .catch(() => {});
    }
  }, [currentUserId, tasks, myQueues, queueMembers]);

  // Create task
  const handleCreate = async () => {
    if (!formTitle.trim()) {
      setFormError("Başlık zorunludur.");
      return;
    }
    setFormSaving(true);
    setFormError("");
    try {
      const res = await fetch("/api/pm/work-items", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          title: formTitle.trim(),
          description: formDescription.trim() || null,
          priority: formPriority,
          type: formType,
          assigneeUserId: formAssignee || null,
          reporterUserId: currentUserId,
          dueDate: formDueDate || null,
          estimatedHours: formEstimatedHours ? parseFloat(formEstimatedHours) : 0,
          tags: formTags.trim() || null,
        }),
      });
      if (res.ok) {
        setShowCreate(false);
        resetForm();
        await fetchTasks();
      } else {
        const errText = await res.text();
        setFormError(`Hata: ${res.status} — ${errText.substring(0, 200)}`);
      }
    } catch {
      setFormError("Bağlantı hatası oluştu.");
    } finally {
      setFormSaving(false);
    }
  };

  const resetForm = () => {
    setFormTitle("");
    setFormDescription("");
    setFormPriority("Medium");
    setFormType("Task");
    setFormAssignee("");
    setFormDueDate("");
    setFormEstimatedHours("");
    setFormTags("");
    setFormError("");
  };

  // Quick status change
  const handleStatusChange = async (taskId: string, newStatus: string) => {
    try {
      await fetch(`/api/pm/work-items/${taskId}/move`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status: newStatus }),
      });
      await fetchTasks();
    } catch {
      /* silently fail */
    }
  };

  // Search filter
  const filteredTasks = searchTerm
    ? tasks.filter(
        (t) =>
          t.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
          t.workItemNumber.toLowerCase().includes(searchTerm.toLowerCase())
      )
    : tasks;

  const tabCounts: Record<TabKey, number> = {
    "my-tasks": myTasksCount,
    "my-created": myCreatedCount,
    "queue-tasks": queueTasksCount,
  };

  function getSourceBadge(task: TaskData) {
    if (task.sourceModule === "RequestManagement") {
      return (
        <span className="inline-flex items-center gap-1 text-xs text-violet-400 bg-violet-500/10 px-2 py-0.5 rounded-full">
          <TicketIcon className="w-3 h-3" />
          Talep
        </span>
      );
    }
    if (task.projectKey) {
      return (
        <span className="inline-flex items-center gap-1 text-xs text-cyan-400 bg-cyan-500/10 px-2 py-0.5 rounded-full">
          <FolderKanban className="w-3 h-3" />
          {task.projectKey}
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1 text-xs text-slate-400 bg-slate-500/10 px-2 py-0.5 rounded-full">
        <Unlink className="w-3 h-3" />
        Bağımsız
      </span>
    );
  }

  const isOverdue = (dueDate?: string) => {
    if (!dueDate) return false;
    return new Date(dueDate).getTime() < Date.now();
  };

  return (
    <div className="space-y-5">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-teal-500 to-cyan-600 flex items-center justify-center shadow-lg shadow-teal-500/25">
            <ListTodo className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">İş Kalemleri</h1>
            <p className="text-sm text-[var(--color-text-muted)] mt-0.5">
              {totalCount} görev
            </p>
          </div>
        </div>
        <button
          onClick={() => setShowCreate(true)}
          className={cn(
            "flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium",
            "bg-teal-600 text-white hover:bg-teal-700 active:bg-teal-800",
            "shadow-lg shadow-teal-500/20 transition-all duration-200"
          )}
        >
          <Plus className="w-4 h-4" />
          Yeni Görev
        </button>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-1 p-1 rounded-xl bg-[var(--color-bg)]/80 border border-[var(--color-border)] w-fit">
        {TABS.map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.key;
          const count = tabCounts[tab.key];
          if (tab.key === "queue-tasks" && myQueues.length === 0) return null;
          return (
            <button
              key={tab.key}
              onClick={() => {
                setActiveTab(tab.key);
                setPage(1);
                setStatusFilter("");
                setPriorityFilter("");
                setTypeFilter("");
                setSourceFilter("");
              }}
              className={cn(
                "flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200",
                isActive
                  ? "bg-[var(--color-card-bg)] text-[var(--color-text)] shadow-sm border border-[var(--color-border)]"
                  : "text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-card-bg)]/50"
              )}
            >
              <Icon className="w-4 h-4" />
              {tab.label}
              {count > 0 && (
                <span
                  className={cn(
                    "inline-flex items-center justify-center min-w-[20px] h-5 px-1.5 rounded-full text-[11px] font-semibold",
                    isActive
                      ? "bg-teal-500/20 text-teal-400"
                      : "bg-[var(--color-border)] text-[var(--color-text-muted)]"
                  )}
                >
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
          <input
            type="text"
            placeholder="İş kalemi ara (numara veya başlık)..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className={cn(
              "w-full pl-10 pr-4 py-2 rounded-lg text-sm",
              "bg-[var(--color-input-bg)] border border-[var(--color-border)]",
              "text-[var(--color-text)] placeholder:text-[var(--color-text-muted)]",
              "focus:outline-none focus:ring-2 focus:ring-teal-500/40"
            )}
          />
        </div>
        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]"
        >
          <option value="">Tüm Durumlar</option>
          {TASK_STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>{TASK_STATUS_CONFIG[s]?.label ?? s}</option>
          ))}
        </select>
        <select
          value={priorityFilter}
          onChange={(e) => { setPriorityFilter(e.target.value); setPage(1); }}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]"
        >
          <option value="">Tüm Öncelikler</option>
          {TASK_PRIORITY_OPTIONS.map((p) => (
            <option key={p} value={p}>{PRIORITY_CONFIG[p]?.label ?? p}</option>
          ))}
        </select>
        <select
          value={typeFilter}
          onChange={(e) => { setTypeFilter(e.target.value); setPage(1); }}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]"
        >
          <option value="">Tüm Tipler</option>
          {TASK_TYPE_OPTIONS.map((t) => (
            <option key={t} value={t}>{TYPE_CONFIG[t]?.label ?? t}</option>
          ))}
        </select>
        <select
          value={sourceFilter}
          onChange={(e) => { setSourceFilter(e.target.value); setPage(1); }}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]"
        >
          {SOURCE_FILTER_OPTIONS.map((sf) => (
            <option key={sf.value} value={sf.value}>{sf.label}</option>
          ))}
        </select>
      </div>

      {/* Task Table */}
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="w-6 h-6 text-teal-400 animate-spin" />
          </div>
        ) : filteredTasks.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-[var(--color-text-muted)]">
            <ListTodo className="w-12 h-12 opacity-30 mb-3" />
            <p className="text-sm">
              {activeTab === "my-tasks" && "Üzerinize atanmış iş kalemi yok"}
              {activeTab === "my-created" && "Henüz iş kalemi oluşturmadınız"}
              {activeTab === "queue-tasks" && "Diğer iş kalemleri bulunamadı"}
            </p>
            <button
              onClick={() => setShowCreate(true)}
              className="mt-3 text-xs text-teal-400 hover:text-teal-300 transition-colors"
            >
              + Yeni İş Kalemi Oluştur
            </button>
          </div>
        ) : (
          <table className="w-full">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg)]/50">
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Numara</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Başlık</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Durum</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Öncelik</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Tip</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Atanan</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Kaynak</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Bitiş</th>
                <th className="text-right px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Durum Değiştir</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {filteredTasks.map((task) => {
                const statusCfg = TASK_STATUS_CONFIG[task.status] || TASK_STATUS_CONFIG.Backlog;
                const priorityCfg = PRIORITY_CONFIG[task.priority] || PRIORITY_CONFIG.Medium;
                const typeCfg = TYPE_CONFIG[task.type] || TYPE_CONFIG.Task;
                const StatusIcon = statusCfg.icon;
                const TypeIcon = typeCfg.icon;
                const overdue = isOverdue(task.dueDate) && task.status !== "Done" && task.status !== "Cancelled";

                return (
                  <tr
                    key={extractId(task.id)}
                    onClick={() => router.push(`/dashboard/tasks/${extractId(task.id)}`)}
                    className="hover:bg-[var(--color-border)]/30 transition-colors cursor-pointer group"
                  >
                    <td className="px-4 py-3">
                      <span className="text-sm font-mono font-medium text-teal-400 group-hover:text-teal-300 transition-colors">
                        {task.workItemNumber}
                      </span>
                    </td>
                    <td className="px-4 py-3 max-w-[280px]">
                      <span className="text-sm text-[var(--color-text)] line-clamp-1">
                        {task.title}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span className={cn(
                        "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border",
                        statusCfg.bg, statusCfg.color
                      )}>
                        <StatusIcon className="w-3 h-3" />
                        {statusCfg.label}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span className="flex items-center gap-1.5">
                        <span className={cn("w-2 h-2 rounded-full", priorityCfg.dot)} />
                        <span className={cn("text-sm font-medium", priorityCfg.color)}>
                          {priorityCfg.label}
                        </span>
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span className={cn("inline-flex items-center gap-1 text-xs", typeCfg.color)}>
                        <TypeIcon className="w-3 h-3" />
                        {typeCfg.label}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span className="text-sm text-[var(--color-text-muted)] flex items-center gap-1">
                        <User className="w-3 h-3" />
                        {getUserName(task.assigneeUserId)}
                      </span>
                    </td>
                    <td className="px-4 py-3">{getSourceBadge(task)}</td>
                    <td className="px-4 py-3">
                      {task.dueDate ? (
                        <span className={cn(
                          "inline-flex items-center gap-1 text-xs",
                          overdue ? "text-red-400" : "text-[var(--color-text-muted)]"
                        )}>
                          {overdue && <AlertTriangle className="w-3 h-3" />}
                          <Calendar className="w-3 h-3" />
                          {formatDate(task.dueDate)}
                        </span>
                      ) : (
                        <span className="text-xs text-[var(--color-text-muted)]">—</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-right" onClick={(e) => e.stopPropagation()}>
                      <select
                        value={task.status}
                        onChange={(e) => handleStatusChange(extractId(task.id), e.target.value)}
                        className="opacity-0 group-hover:opacity-100 focus:opacity-100 px-2 py-1 rounded text-[10px] bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] transition-opacity cursor-pointer"
                      >
                        {TASK_STATUS_OPTIONS.map((s) => (
                          <option key={s} value={s}>{TASK_STATUS_CONFIG[s]?.label ?? s}</option>
                        ))}
                      </select>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}

        {/* Pagination */}
        {totalCount > pageSize && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-[var(--color-border)]">
            <span className="text-xs text-[var(--color-text-muted)]">
              Sayfa {page} / {Math.ceil(totalCount / pageSize)}
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-3 py-1.5 rounded text-xs border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)] disabled:opacity-30 transition-colors"
              >
                Önceki
              </button>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={page >= Math.ceil(totalCount / pageSize)}
                className="px-3 py-1.5 rounded text-xs border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)] disabled:opacity-30 transition-colors"
              >
                Sonraki
              </button>
            </div>
          </div>
        )}
      </div>

      {/* ═══════════ Create Task Panel ═══════════ */}
      {showCreate && (
        <>
          <div
            className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm"
            onClick={() => { setShowCreate(false); resetForm(); }}
          />
          <div className="fixed right-0 top-0 z-50 h-full w-full max-w-lg bg-[var(--color-card-bg)] border-l border-[var(--color-border)] shadow-2xl flex flex-col animate-slide-in-right">
            {/* Panel Header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-teal-500 to-cyan-600 flex items-center justify-center shadow-lg shadow-teal-500/30">
                  <ListTodo className="w-5 h-5 text-white" />
                </div>
                <div>
                  <h2 className="text-lg font-semibold text-[var(--color-text)]">Yeni İş Kalemi</h2>
                  <p className="text-xs text-[var(--color-text-muted)]">İş kalemi bilgilerini girin</p>
                </div>
              </div>
              <button
                onClick={() => { setShowCreate(false); resetForm(); }}
                className="p-2 rounded-lg hover:bg-[var(--color-border)] transition-colors"
              >
                <X className="w-5 h-5 text-[var(--color-text-muted)]" />
              </button>
            </div>

            {/* Panel Body */}
            <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">
                  Başlık <span className="text-red-400">*</span>
                </label>
                <input
                  type="text"
                  value={formTitle}
                  onChange={(e) => setFormTitle(e.target.value)}
                  placeholder="İş kalemi başlığı..."
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Tip</label>
                  <select
                    value={formType}
                    onChange={(e) => setFormType(e.target.value)}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                  >
                    {TASK_TYPE_OPTIONS.map((t) => (
                      <option key={t} value={t}>{TYPE_CONFIG[t]?.label ?? t}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Öncelik</label>
                  <select
                    value={formPriority}
                    onChange={(e) => setFormPriority(e.target.value)}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                  >
                    {TASK_PRIORITY_OPTIONS.map((p) => (
                      <option key={p} value={p}>{PRIORITY_CONFIG[p]?.label ?? p}</option>
                    ))}
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Atanan Kişi</label>
                <select
                  value={formAssignee}
                  onChange={(e) => setFormAssignee(e.target.value)}
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                >
                  <option value="">Atama yapılmadı</option>
                  {queueMembers.length > 0
                    ? queueMembers.map((m) => (
                        <option key={m.userId} value={m.userId}>
                          {m.displayName || getUserName(m.userId)}
                        </option>
                      ))
                    : DEV_USERS.map((u) => (
                        <option key={u.id} value={u.id}>{u.fullName}</option>
                      ))
                  }
                </select>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Bitiş Tarihi</label>
                  <input
                    type="date"
                    value={formDueDate}
                    onChange={(e) => setFormDueDate(e.target.value)}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Tahmini Süre (saat)</label>
                  <input
                    type="number"
                    step="0.5"
                    value={formEstimatedHours}
                    onChange={(e) => setFormEstimatedHours(e.target.value)}
                    placeholder="0"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Etiketler</label>
                <input
                  type="text"
                  value={formTags}
                  onChange={(e) => setFormTags(e.target.value)}
                  placeholder="frontend, api, acil (virgülle ayırın)"
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Açıklama</label>
                <textarea
                  value={formDescription}
                  onChange={(e) => setFormDescription(e.target.value)}
                  placeholder="Detaylı açıklama..."
                  rows={4}
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] resize-none focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                />
              </div>

              {formError && (
                <div className="px-3 py-2 rounded-lg bg-red-500/10 border border-red-500/20 text-sm text-red-400">
                  {formError}
                </div>
              )}
            </div>

            {/* Panel Footer */}
            <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-3">
              <button
                onClick={() => { setShowCreate(false); resetForm(); }}
                className="px-4 py-2.5 rounded-lg text-sm font-medium text-[var(--color-text-muted)] hover:bg-[var(--color-border)] transition-colors"
              >
                İptal
              </button>
              <button
                onClick={handleCreate}
                disabled={formSaving}
                className={cn(
                  "flex items-center gap-2 px-5 py-2.5 rounded-lg text-sm font-medium",
                  "bg-teal-600 text-white hover:bg-teal-700",
                  "shadow-lg shadow-teal-500/20 transition-all duration-200",
                  "disabled:opacity-50 disabled:cursor-not-allowed"
                )}
              >
                {formSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
                Oluştur
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
