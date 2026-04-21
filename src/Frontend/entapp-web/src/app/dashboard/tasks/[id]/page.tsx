"use client";

import { useState, useEffect } from "react";
import { useRouter, useParams } from "next/navigation";
import {
  ArrowLeft,
  Clock,
  CheckCircle2,
  XCircle,
  ArrowUpRight,
  Plus,
  Loader2,
  User,
  MessageSquare,
  Send,
  Tag,
  Calendar,
  ListTodo,
  CircleDot,
  CircleCheck,
  Circle,
  TicketIcon,
  FolderKanban,
  Unlink,
  AlertTriangle,
  Edit3,
  Save,
  X,
  Timer,
  Bug,
  Sparkles,
  TrendingUp,
  Layers,
  Hash,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useAuthStore } from "@/stores";

// ── Interfaces ──────────────────────────────────────────────

interface TaskDetail {
  id: string;
  workItemNumber: string;
  title: string;
  description?: string;
  status: string;
  priority: string;
  type: string;
  assigneeUserId?: string;
  reporterUserId?: string;
  parentTaskId?: string;
  dueDate?: string;
  estimatedHours: number;
  sortOrder: number;
  tags?: string;
  sourceModule?: string;
  sourceType?: string;
  sourceId?: string;
  projectId?: string;
  projectKey?: string;
  projectName?: string;
  createdAt: string;
  updatedAt?: string;
  subTasks: SubTaskData[];
}

interface SubTaskData {
  id: string;
  workItemNumber: string;
  title: string;
  status: string;
  priority: string;
  type: string;
  assigneeUserId?: string;
  dueDate?: string;
  estimatedHours: number;
  createdAt: string;
}

interface CommentData {
  id: { value: string } | string;
  authorUserId: string;
  content: string;
  createdAt: string;
}

interface TimeEntryData {
  id: { value: string } | string;
  userId: string;
  hours: number;
  workDate: string;
  description?: string;
}

// ── Config ──────────────────────────────────────────────────

const TASK_STATUS_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
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
  Task: { icon: ListTodo, color: "text-blue-400", label: "Görev" },
  Bug: { icon: Bug, color: "text-red-400", label: "Hata" },
  Feature: { icon: Sparkles, color: "text-violet-400", label: "Özellik" },
  Improvement: { icon: TrendingUp, color: "text-teal-400", label: "İyileştirme" },
  Epic: { icon: Layers, color: "text-orange-400", label: "Epik" },
};

const TASK_STATUS_OPTIONS = ["Backlog", "Todo", "InProgress", "InReview", "Done", "Cancelled"];
const TASK_PRIORITY_OPTIONS = ["Low", "Medium", "High", "Critical"];
const TASK_TYPE_OPTIONS = ["Task", "Bug", "Feature", "Improvement", "Epic"];

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

function extractId(id: { value: string } | string | undefined): string {
  if (!id) return "";
  if (typeof id === "string") return id;
  return id.value;
}

function formatDateTime(dateStr: string): string {
  return new Date(dateStr).toLocaleString("tr-TR", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });
}

function formatDate(dateStr?: string): string {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleDateString("tr-TR", {
    day: "2-digit", month: "2-digit", year: "numeric",
  });
}

// ── Component ───────────────────────────────────────────────

export default function TaskDetailPage() {
  const router = useRouter();
  const params = useParams();
  const taskId = params.id as string;
  const authUser = useAuthStore((s) => s.user);
  const currentUserId = authUser?.id ?? "";

  const [task, setTask] = useState<TaskDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [comments, setComments] = useState<CommentData[]>([]);
  const [timeEntries, setTimeEntries] = useState<TimeEntryData[]>([]);

  // Comment state
  const [commentText, setCommentText] = useState("");
  const [commentSending, setCommentSending] = useState(false);

  // Status & assignment
  const [newStatus, setNewStatus] = useState("");
  const [statusChanging, setStatusChanging] = useState(false);

  // Edit mode
  const [editing, setEditing] = useState(false);
  const [editTitle, setEditTitle] = useState("");
  const [editDescription, setEditDescription] = useState("");
  const [editPriority, setEditPriority] = useState("");
  const [editType, setEditType] = useState("");
  const [editDueDate, setEditDueDate] = useState("");
  const [editEstimatedHours, setEditEstimatedHours] = useState("");
  const [editTags, setEditTags] = useState("");
  const [editAssignee, setEditAssignee] = useState("");
  const [editSaving, setEditSaving] = useState(false);

  // Time entry form
  const [showTimeEntry, setShowTimeEntry] = useState(false);
  const [teHours, setTeHours] = useState("");
  const [teDate, setTeDate] = useState(new Date().toISOString().slice(0, 10));
  const [teDescription, setTeDescription] = useState("");
  const [teSaving, setTeSaving] = useState(false);

  // Fetch task detail
  useEffect(() => {
    if (!taskId) return;
    setLoading(true);
    fetch(`/api/pm/work-items/${taskId}`)
      .then((r) => (r.ok ? r.json() : null))
      .then((data) => {
        setTask(data);
        setNewStatus(data?.status ?? "");
      })
      .catch(() => setTask(null))
      .finally(() => setLoading(false));
  }, [taskId]);

  // Fetch comments
  useEffect(() => {
    if (!taskId) return;
    fetch(`/api/pm/comments/${taskId}`)
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setComments(Array.isArray(data) ? data : []))
      .catch(() => setComments([]));
  }, [taskId]);

  // Fetch time entries
  useEffect(() => {
    if (!taskId) return;
    fetch(`/api/pm/time-entries?taskId=${taskId}`)
      .then((r) => (r.ok ? r.json() : { items: [] }))
      .then((data) => setTimeEntries(data.items ?? []))
      .catch(() => setTimeEntries([]));
  }, [taskId]);

  const refreshAll = async () => {
    const [taskRes, commentsRes, teRes] = await Promise.all([
      fetch(`/api/pm/work-items/${taskId}`),
      fetch(`/api/pm/comments/${taskId}`),
      fetch(`/api/pm/time-entries?taskId=${taskId}`),
    ]);
    if (taskRes.ok) { const d = await taskRes.json(); setTask(d); setNewStatus(d.status); }
    if (commentsRes.ok) { setComments(await commentsRes.json()); }
    if (teRes.ok) { const d = await teRes.json(); setTimeEntries(d.items ?? []); }
  };

  // Status change
  const handleStatusChange = async () => {
    if (!newStatus || newStatus === task?.status) return;
    setStatusChanging(true);
    try {
      await fetch(`/api/pm/work-items/${taskId}/move`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status: newStatus }),
      });
      await refreshAll();
    } catch { /* */ } finally { setStatusChanging(false); }
  };

  // Comment
  const handleComment = async () => {
    if (!commentText.trim()) return;
    setCommentSending(true);
    try {
      await fetch("/api/pm/comments", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ taskId, authorUserId: currentUserId, content: commentText.trim() }),
      });
      setCommentText("");
      await refreshAll();
    } catch { /* */ } finally { setCommentSending(false); }
  };

  // Edit
  const startEditing = () => {
    if (!task) return;
    setEditTitle(task.title);
    setEditDescription(task.description ?? "");
    setEditPriority(task.priority);
    setEditType(task.type);
    setEditDueDate(task.dueDate ? task.dueDate.slice(0, 10) : "");
    setEditEstimatedHours(task.estimatedHours.toString());
    setEditTags(task.tags ?? "");
    setEditAssignee(task.assigneeUserId ?? "");
    setEditing(true);
  };

  const handleSaveEdit = async () => {
    setEditSaving(true);
    try {
      const res = await fetch(`/api/pm/work-items/${taskId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          title: editTitle.trim() || null,
          description: editDescription.trim(),
          priority: editPriority,
          type: editType,
          dueDate: editDueDate || null,
          estimatedHours: parseFloat(editEstimatedHours) || 0,
          tags: editTags.trim() || null,
          assigneeUserId: editAssignee || null,
        }),
      });
      if (res.ok) {
        setEditing(false);
        await refreshAll();
      }
    } catch { /* */ } finally { setEditSaving(false); }
  };

  // Time entry
  const handleTimeEntry = async () => {
    if (!teHours || parseFloat(teHours) <= 0) return;
    setTeSaving(true);
    try {
      await fetch("/api/pm/time-entries", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          taskId,
          userId: currentUserId,
          hours: parseFloat(teHours),
          workDate: teDate,
          description: teDescription.trim() || null,
        }),
      });
      setTeHours("");
      setTeDescription("");
      setShowTimeEntry(false);
      await refreshAll();
    } catch { /* */ } finally { setTeSaving(false); }
  };

  // Sub-task status change
  const handleSubTaskStatus = async (subTaskId: string, newSts: string) => {
    try {
      await fetch(`/api/pm/work-items/${subTaskId}/move`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status: newSts }),
      });
      await refreshAll();
    } catch { /* */ }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Loader2 className="w-8 h-8 text-teal-400 animate-spin" />
      </div>
    );
  }

  if (!task) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-[var(--color-text-muted)]">
        <XCircle className="w-12 h-12 opacity-30 mb-3" />
        <p>İş kalemi bulunamadı</p>
        <button onClick={() => router.back()} className="mt-4 text-sm text-teal-400 hover:text-teal-300">
          ← Geri Dön
        </button>
      </div>
    );
  }

  const statusCfg = TASK_STATUS_CONFIG[task.status] || TASK_STATUS_CONFIG.Backlog;
  const priorityCfg = PRIORITY_CONFIG[task.priority] || PRIORITY_CONFIG.Medium;
  const typeCfg = TYPE_CONFIG[task.type] || TYPE_CONFIG.Task;
  const StatusIcon = statusCfg.icon;
  const TypeIcon = typeCfg.icon;

  const completedSubTasks = task.subTasks.filter((t) => t.status === "Done" || t.status === "Cancelled").length;
  const subTaskProgress = task.subTasks.length > 0 ? Math.round((completedSubTasks / task.subTasks.length) * 100) : 0;

  const totalLoggedHours = timeEntries.reduce((sum, te) => sum + te.hours, 0);
  const isOverdue = task.dueDate && new Date(task.dueDate).getTime() < Date.now() && task.status !== "Done" && task.status !== "Cancelled";

  function getSourceLink() {
    if (task!.sourceModule === "RequestManagement" && task!.sourceId) {
      return (
        <button
          onClick={() => router.push(`/dashboard/tickets/${task!.sourceId}`)}
          className="inline-flex items-center gap-1.5 text-xs text-violet-400 bg-violet-500/10 px-2.5 py-1 rounded-full hover:bg-violet-500/20 transition-colors"
        >
          <TicketIcon className="w-3 h-3" />
          Talebe Git
        </button>
      );
    }
    if (task!.projectKey) {
      return (
        <span className="inline-flex items-center gap-1.5 text-xs text-cyan-400 bg-cyan-500/10 px-2.5 py-1 rounded-full">
          <FolderKanban className="w-3 h-3" />
          {task!.projectKey} — {task!.projectName}
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1.5 text-xs text-slate-400 bg-slate-500/10 px-2.5 py-1 rounded-full">
        <Unlink className="w-3 h-3" />
        Bağımsız Görev
      </span>
    );
  }

  return (
    <div className="space-y-6">
      {/* Back + Header */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => router.push("/dashboard/tasks")}
          className="p-2 rounded-lg hover:bg-[var(--color-border)] transition-colors"
        >
          <ArrowLeft className="w-5 h-5 text-[var(--color-text-muted)]" />
        </button>
        <div className="flex-1">
          <div className="flex items-center gap-3 flex-wrap">
            <span className="text-sm font-mono font-bold text-teal-400">{task.workItemNumber}</span>
            <span className={cn(
              "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border",
              statusCfg.bg, statusCfg.color
            )}>
              <StatusIcon className="w-3 h-3" />
              {statusCfg.label}
            </span>
            <span className="flex items-center gap-1">
              <span className={cn("w-2 h-2 rounded-full", priorityCfg.dot)} />
              <span className={cn("text-xs font-medium", priorityCfg.color)}>{priorityCfg.label}</span>
            </span>
            <span className={cn("inline-flex items-center gap-1 text-xs", typeCfg.color)}>
              <TypeIcon className="w-3 h-3" />
              {typeCfg.label}
            </span>
            {getSourceLink()}
          </div>
          {!editing ? (
            <h1 className="text-xl font-bold text-[var(--color-text)] mt-1">{task.title}</h1>
          ) : (
            <input
              type="text"
              value={editTitle}
              onChange={(e) => setEditTitle(e.target.value)}
              className="mt-1 w-full text-xl font-bold px-3 py-1 rounded-lg bg-[var(--color-input-bg)] border border-teal-500/40 text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-teal-500/40"
            />
          )}
        </div>
        {!editing ? (
          <button
            onClick={startEditing}
            className="flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium text-teal-400 border border-teal-500/20 hover:bg-teal-500/10 transition-all"
          >
            <Edit3 className="w-4 h-4" />
            Düzenle
          </button>
        ) : (
          <div className="flex gap-2">
            <button
              onClick={handleSaveEdit}
              disabled={editSaving}
              className="flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium bg-teal-600 text-white hover:bg-teal-700 disabled:opacity-50 transition-all"
            >
              {editSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
              Kaydet
            </button>
            <button
              onClick={() => setEditing(false)}
              className="flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium text-[var(--color-text-muted)] hover:bg-[var(--color-border)] transition-all"
            >
              <X className="w-4 h-4" />
              İptal
            </button>
          </div>
        )}
      </div>

      {/* Main content grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left: main content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Description */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3">Açıklama</h3>
            {!editing ? (
              <p className="text-sm text-[var(--color-text-muted)] whitespace-pre-wrap leading-relaxed">
                {task.description || "Açıklama eklenmemiş."}
              </p>
            ) : (
              <textarea
                value={editDescription}
                onChange={(e) => setEditDescription(e.target.value)}
                rows={5}
                className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] resize-none focus:outline-none focus:ring-2 focus:ring-teal-500/40"
              />
            )}
          </div>

          {/* Sub Tasks */}
          {(task.subTasks.length > 0 || !editing) && (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)]">
              <div className="px-5 py-4 border-b border-[var(--color-border)] flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <ListTodo className="w-4 h-4 text-teal-400" />
                  <h3 className="text-sm font-semibold text-[var(--color-text)]">
                    Alt İş Kalemleri {task.subTasks.length > 0 && <span className="text-[var(--color-text-muted)] font-normal">({completedSubTasks}/{task.subTasks.length})</span>}
                  </h3>
                  {task.subTasks.length > 0 && (
                    <div className="flex items-center gap-2">
                      <div className="w-24 h-1.5 bg-[var(--color-border)] rounded-full overflow-hidden">
                        <div
                          className={cn("h-full rounded-full transition-all duration-500", subTaskProgress === 100 ? "bg-emerald-500" : "bg-teal-500")}
                          style={{ width: `${subTaskProgress}%` }}
                        />
                      </div>
                      <span className="text-xs text-[var(--color-text-muted)]">{subTaskProgress}%</span>
                    </div>
                  )}
                </div>
              </div>
              <div className="divide-y divide-[var(--color-border)]">
                {task.subTasks.length === 0 ? (
                  <div className="px-5 py-8 text-center text-sm text-[var(--color-text-muted)]">
                    <ListTodo className="w-8 h-8 mx-auto mb-2 opacity-20" />
                    Alt iş kalemi yok
                  </div>
                ) : (
                  task.subTasks.map((sub) => {
                    const sCfg = TASK_STATUS_CONFIG[sub.status] || TASK_STATUS_CONFIG.Backlog;
                    const pCfg = PRIORITY_CONFIG[sub.priority] || PRIORITY_CONFIG.Medium;
                    const SIcon = sCfg.icon;
                    return (
                      <div key={sub.id} className="px-5 py-3 flex items-center gap-3 hover:bg-[var(--color-input-bg)]/30 transition-colors group cursor-pointer"
                        onClick={() => router.push(`/dashboard/tasks/${sub.id}`)}
                      >
                        <SIcon className={cn("w-4 h-4 shrink-0", sCfg.color)} />
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-mono text-[var(--color-text-muted)]">{sub.workItemNumber}</span>
                            <span className="text-sm text-[var(--color-text)] truncate">{sub.title}</span>
                          </div>
                          <div className="flex items-center gap-3 mt-0.5">
                            <span className={cn("text-[10px] font-medium", pCfg.color)}>
                              <span className={cn("inline-block w-1.5 h-1.5 rounded-full mr-1", pCfg.dot)} />
                              {pCfg.label}
                            </span>
                            {sub.assigneeUserId && (
                              <span className="text-[10px] text-[var(--color-text-muted)] flex items-center gap-0.5">
                                <User className="w-2.5 h-2.5" />
                                {getUserName(sub.assigneeUserId)}
                              </span>
                            )}
                          </div>
                        </div>
                        <select
                          value={sub.status}
                          onChange={(e) => { e.stopPropagation(); handleSubTaskStatus(sub.id, e.target.value); }}
                          onClick={(e) => e.stopPropagation()}
                          className="opacity-0 group-hover:opacity-100 focus:opacity-100 px-2 py-1 rounded text-[10px] bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] transition-opacity cursor-pointer"
                        >
                          {TASK_STATUS_OPTIONS.map((s) => (
                            <option key={s} value={s}>{TASK_STATUS_CONFIG[s]?.label ?? s}</option>
                          ))}
                        </select>
                      </div>
                    );
                  })
                )}
              </div>
            </div>
          )}

          {/* Comments */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)]">
            <div className="px-5 py-4 border-b border-[var(--color-border)]">
              <h3 className="text-sm font-semibold text-[var(--color-text)]">
                Yorumlar ({comments.length})
              </h3>
            </div>
            {/* Add comment */}
            <div className="px-5 py-4 border-b border-[var(--color-border)]">
              <div className="flex gap-3">
                <div className="w-8 h-8 rounded-full bg-teal-500/20 flex items-center justify-center shrink-0">
                  <User className="w-4 h-4 text-teal-400" />
                </div>
                <div className="flex-1">
                  <textarea
                    value={commentText}
                    onChange={(e) => setCommentText(e.target.value)}
                    placeholder="Yorum yazın..."
                    rows={2}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] resize-none focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                  />
                  <div className="flex justify-end mt-2">
                    <button
                      onClick={handleComment}
                      disabled={commentSending || !commentText.trim()}
                      className={cn(
                        "flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium",
                        "bg-teal-600 text-white hover:bg-teal-700",
                        "disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                      )}
                    >
                      {commentSending ? <Loader2 className="w-3 h-3 animate-spin" /> : <Send className="w-3 h-3" />}
                      Gönder
                    </button>
                  </div>
                </div>
              </div>
            </div>
            <div className="divide-y divide-[var(--color-border)]">
              {comments.length === 0 ? (
                <div className="px-5 py-8 text-center text-sm text-[var(--color-text-muted)]">
                  Henüz yorum yok
                </div>
              ) : (
                comments.map((c, i) => (
                  <div key={i} className="px-5 py-3 flex gap-3">
                    <div className="w-8 h-8 rounded-full bg-blue-500/15 flex items-center justify-center shrink-0">
                      <MessageSquare className="w-3.5 h-3.5 text-blue-400" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="text-xs font-medium text-[var(--color-text)]">{getUserName(c.authorUserId)}</span>
                        <span className="text-[10px] text-[var(--color-text-muted)]">{formatDateTime(c.createdAt)}</span>
                      </div>
                      <p className="text-sm text-[var(--color-text-muted)]">{c.content}</p>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          {/* Time Entries */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)]">
            <div className="px-5 py-4 border-b border-[var(--color-border)] flex items-center justify-between">
              <div className="flex items-center gap-3">
                <Timer className="w-4 h-4 text-indigo-400" />
                <h3 className="text-sm font-semibold text-[var(--color-text)]">
                  Zaman Girişleri
                  <span className="text-[var(--color-text-muted)] font-normal ml-1">
                    ({totalLoggedHours.toFixed(1)}s{task.estimatedHours > 0 ? ` / ${task.estimatedHours}s` : ""})
                  </span>
                </h3>
              </div>
              <button
                onClick={() => setShowTimeEntry(!showTimeEntry)}
                className="flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-medium bg-indigo-600 text-white hover:bg-indigo-700 transition-all"
              >
                <Plus className="w-3 h-3" />
                Süre Ekle
              </button>
            </div>
            {showTimeEntry && (
              <div className="px-5 py-4 border-b border-[var(--color-border)] bg-[var(--color-input-bg)]/30">
                <div className="flex gap-2 items-end">
                  <div className="flex-1">
                    <label className="text-[10px] text-[var(--color-text-muted)] mb-1 block">Süre (saat)</label>
                    <input type="number" step="0.25" value={teHours} onChange={(e) => setTeHours(e.target.value)}
                      className="w-full px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none" />
                  </div>
                  <div className="flex-1">
                    <label className="text-[10px] text-[var(--color-text-muted)] mb-1 block">Tarih</label>
                    <input type="date" value={teDate} onChange={(e) => setTeDate(e.target.value)}
                      className="w-full px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none" />
                  </div>
                  <div className="flex-1">
                    <label className="text-[10px] text-[var(--color-text-muted)] mb-1 block">Açıklama</label>
                    <input type="text" value={teDescription} onChange={(e) => setTeDescription(e.target.value)} placeholder="Opsiyonel"
                      className="w-full px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none" />
                  </div>
                  <button onClick={handleTimeEntry} disabled={teSaving || !teHours}
                    className="px-3 py-1.5 rounded-lg text-xs font-medium bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-40 transition-all">
                    {teSaving ? <Loader2 className="w-3 h-3 animate-spin" /> : "Kaydet"}
                  </button>
                </div>
              </div>
            )}
            <div className="divide-y divide-[var(--color-border)]">
              {timeEntries.length === 0 ? (
                <div className="px-5 py-6 text-center text-sm text-[var(--color-text-muted)]">
                  Henüz zaman girişi yok
                </div>
              ) : (
                timeEntries.map((te, i) => (
                  <div key={i} className="px-5 py-2.5 flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <span className="text-xs text-[var(--color-text-muted)]">{formatDate(te.workDate)}</span>
                      <span className="text-xs text-[var(--color-text)]">{getUserName(te.userId)}</span>
                      {te.description && <span className="text-xs text-[var(--color-text-muted)]">— {te.description}</span>}
                    </div>
                    <span className="text-xs font-medium text-indigo-400">{te.hours}s</span>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>

        {/* Right sidebar */}
        <div className="space-y-4">
          {/* Details Card */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3">Detaylar</h3>
            <div className="space-y-3">
              {!editing ? (
                <>
                  {[
                    { icon: User, label: "Atanan", value: getUserName(task.assigneeUserId) },
                    { icon: User, label: "Raporlayan", value: getUserName(task.reporterUserId) },
                    { icon: Calendar, label: "Bitiş Tarihi", value: task.dueDate ? formatDate(task.dueDate) : "—", warning: isOverdue },
                    { icon: Timer, label: "Tahmini", value: task.estimatedHours > 0 ? `${task.estimatedHours} saat` : "—" },
                    { icon: Timer, label: "Harcanan", value: totalLoggedHours > 0 ? `${totalLoggedHours.toFixed(1)} saat` : "—" },
                    { icon: Hash, label: "Oluşturma", value: formatDateTime(task.createdAt) },
                  ].map((item) => (
                    <div key={item.label} className="flex items-center gap-2.5">
                      <item.icon className={cn("w-3.5 h-3.5 shrink-0", item.warning ? "text-red-400" : "text-[var(--color-text-muted)]")} />
                      <span className="text-xs text-[var(--color-text-muted)] w-20 shrink-0">{item.label}</span>
                      <span className={cn("text-xs truncate", item.warning ? "text-red-400 font-medium" : "text-[var(--color-text)]")}>
                        {item.warning && <AlertTriangle className="w-3 h-3 inline mr-1" />}
                        {item.value}
                      </span>
                    </div>
                  ))}
                  {task.tags && (
                    <div className="flex items-start gap-2.5">
                      <Tag className="w-3.5 h-3.5 text-[var(--color-text-muted)] shrink-0 mt-0.5" />
                      <span className="text-xs text-[var(--color-text-muted)] w-20 shrink-0">Etiketler</span>
                      <div className="flex flex-wrap gap-1">
                        {task.tags.split(",").map((tag, i) => (
                          <span key={i} className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] bg-[var(--color-border)] text-[var(--color-text-muted)]">
                            {tag.trim()}
                          </span>
                        ))}
                      </div>
                    </div>
                  )}
                </>
              ) : (
                <div className="space-y-3">
                  <div>
                    <label className="text-[10px] text-[var(--color-text-muted)] mb-1 block">Öncelik</label>
                    <select value={editPriority} onChange={(e) => setEditPriority(e.target.value)}
                      className="w-full px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                      {TASK_PRIORITY_OPTIONS.map((p) => <option key={p} value={p}>{PRIORITY_CONFIG[p]?.label ?? p}</option>)}
                    </select>
                  </div>
                  <div>
                    <label className="text-[10px] text-[var(--color-text-muted)] mb-1 block">Tip</label>
                    <select value={editType} onChange={(e) => setEditType(e.target.value)}
                      className="w-full px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                      {TASK_TYPE_OPTIONS.map((t) => <option key={t} value={t}>{TYPE_CONFIG[t]?.label ?? t}</option>)}
                    </select>
                  </div>
                  <div>
                    <label className="text-[10px] text-[var(--color-text-muted)] mb-1 block">Atanan Kişi</label>
                    <select value={editAssignee} onChange={(e) => setEditAssignee(e.target.value)}
                      className="w-full px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                      <option value="">Atanmamış</option>
                      {DEV_USERS.map((u) => <option key={u.id} value={u.id}>{u.fullName}</option>)}
                    </select>
                  </div>
                  <div>
                    <label className="text-[10px] text-[var(--color-text-muted)] mb-1 block">Bitiş Tarihi</label>
                    <input type="date" value={editDueDate} onChange={(e) => setEditDueDate(e.target.value)}
                      className="w-full px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" />
                  </div>
                  <div>
                    <label className="text-[10px] text-[var(--color-text-muted)] mb-1 block">Tahmini Süre (saat)</label>
                    <input type="number" step="0.5" value={editEstimatedHours} onChange={(e) => setEditEstimatedHours(e.target.value)}
                      className="w-full px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" />
                  </div>
                  <div>
                    <label className="text-[10px] text-[var(--color-text-muted)] mb-1 block">Etiketler</label>
                    <input type="text" value={editTags} onChange={(e) => setEditTags(e.target.value)} placeholder="frontend, api"
                      className="w-full px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)]" />
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Time Progress Card */}
          {task.estimatedHours > 0 && (
            <div className={cn(
              "rounded-xl border p-5",
              totalLoggedHours > task.estimatedHours
                ? "border-red-500/20 bg-red-500/5"
                : "border-indigo-500/20 bg-indigo-500/5"
            )}>
              <h3 className={cn(
                "text-xs font-semibold uppercase tracking-wider mb-3 flex items-center gap-1.5",
                totalLoggedHours > task.estimatedHours ? "text-red-400" : "text-indigo-400"
              )}>
                <Timer className="w-3.5 h-3.5" />
                Süre Takibi
              </h3>
              <div className="space-y-2">
                <div className="flex justify-between text-xs">
                  <span className="text-[var(--color-text-muted)]">{totalLoggedHours.toFixed(1)} / {task.estimatedHours}s</span>
                  <span className={cn("font-medium", totalLoggedHours > task.estimatedHours ? "text-red-400" : "text-indigo-400")}>
                    {Math.round((totalLoggedHours / task.estimatedHours) * 100)}%
                  </span>
                </div>
                <div className="w-full h-2 bg-[var(--color-border)] rounded-full overflow-hidden">
                  <div
                    className={cn(
                      "h-full rounded-full transition-all duration-700 ease-out",
                      totalLoggedHours > task.estimatedHours
                        ? "bg-gradient-to-r from-red-600 to-red-400"
                        : "bg-gradient-to-r from-indigo-600 to-indigo-400"
                    )}
                    style={{ width: `${Math.min(100, (totalLoggedHours / task.estimatedHours) * 100)}%` }}
                  />
                </div>
              </div>
            </div>
          )}

          {/* Actions Card */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3">Aksiyonlar</h3>
            <div className="space-y-3">
              <div>
                <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Durum Değiştir</label>
                <div className="flex gap-2">
                  <select
                    value={newStatus}
                    onChange={(e) => setNewStatus(e.target.value)}
                    className="flex-1 px-2 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none"
                  >
                    {TASK_STATUS_OPTIONS.map((s) => (
                      <option key={s} value={s}>{TASK_STATUS_CONFIG[s]?.label ?? s}</option>
                    ))}
                  </select>
                  <button
                    onClick={handleStatusChange}
                    disabled={statusChanging || newStatus === task.status}
                    className={cn(
                      "px-3 py-1.5 rounded-lg text-xs font-medium",
                      "bg-teal-600 text-white hover:bg-teal-700",
                      "disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                    )}
                  >
                    {statusChanging ? <Loader2 className="w-3 h-3 animate-spin" /> : "Uygula"}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
