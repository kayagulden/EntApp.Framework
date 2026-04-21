"use client";

import { useState, useEffect } from "react";
import { useRouter, useParams } from "next/navigation";
import {
  ArrowLeft,
  Clock,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  ArrowUpRight,
  Plus,
  Loader2,
  Building2,
  User,
  UserPlus,
  MessageSquare,
  Send,
  Tag,
  Inbox,
  GitBranch,
  Calendar,
  Hand,
  UserCheck,
  ListTodo,
  CircleDot,
  CircleCheck,
  Circle,
  ChevronDown,
  Zap,
  ShieldCheck,
  ShieldX,
  Monitor,
} from "lucide-react";
import { cn } from "@/lib/utils";

// ── Interfaces ──────────────────────────────────────────────

interface TicketDetail {
  id: string;
  number: string;
  title: string;
  description?: string;
  status: string;
  priority: string;
  channel: string;
  routingSource?: string;
  slaResponseDeadline?: string;
  slaResolutionDeadline?: string;
  slaResponseBreached: boolean;
  slaResolutionBreached: boolean;
  slaRespondedAt?: string;
  assigneeUserId?: string;
  reporterUserId: string;
  linkedTaskCount?: number;
  completedTaskCount?: number;
  workflowInstanceId?: string;
  category?: { name: string };
  department?: { name: string };
  serviceQueue?: { name: string; code: string; id?: { value: string } | string };
  comments?: CommentData[];
  statusHistory?: StatusHistoryData[];
  configurationItemId?: string;
  createdAt: string;
  resolvedAt?: string;
}

interface CommentData {
  id: { value: string } | string;
  content: string;
  authorUserId: string;
  isInternal: boolean;
  createdAt: string;
}

interface StatusHistoryData {
  id: { value: string } | string;
  fromStatus: string;
  toStatus: string;
  changedByUserId: string;
  reason?: string;
  changedAt: string;
}

interface TaskItem {
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

interface WorkflowAction {
  bookmarkId: string;
  activityType: string;
  label: string;
  outcomes: string[];
  ticketId?: string;
}

interface QueueMember {
  userId: string;
  role: string;
  displayName?: string;
}

// ── DEV_USERS (kullanıcı isim çözümleme) ────────────────────
const DEV_USERS = [
  { id: "868b6d11-0110-4182-9cc3-9a155f140fe4", fullName: "Ahmet Yılmaz" },
  { id: "b7dd400d-9aa8-4cc3-b973-a362e34ff39b", fullName: "Elif Demir" },
  { id: "dfbd1ff2-8ba9-424d-8e3b-00e5e35d8edc", fullName: "Mehmet Kaya" },
  { id: "84188840-d0dc-4080-845a-c0f25192ce22", fullName: "Ayşe Çelik" },
  { id: "96a07d00-94c7-4f08-bc30-80b32fbbb139", fullName: "Can Öztürk" },
];

function getUserName(userId?: string): string {
  if (!userId) return "Atanmamış";
  const found = DEV_USERS.find((u) => u.id === userId);
  return found ? found.fullName : userId.slice(0, 8);
}

// ── Config ──────────────────────────────────────────────────

const STATUS_CONFIG: Record<string, { color: string; bg: string; icon: React.ComponentType<{ className?: string }>; label: string }> = {
  New: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", icon: Plus, label: "Yeni" },
  Open: { color: "text-sky-400", bg: "bg-sky-500/10 border-sky-500/20", icon: ArrowUpRight, label: "Açık" },
  InProgress: { color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", icon: Clock, label: "İşlemde" },
  WaitingForInfo: { color: "text-purple-400", bg: "bg-purple-500/10 border-purple-500/20", icon: MessageSquare, label: "Bilgi Bekleniyor" },
  Escalated: { color: "text-red-400", bg: "bg-red-500/10 border-red-500/20", icon: AlertTriangle, label: "Eskalasyon" },
  AllTasksDone: { color: "text-teal-400", bg: "bg-teal-500/10 border-teal-500/20", icon: ListTodo, label: "Görevler Tamamlandı" },
  Resolved: { color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", icon: CheckCircle2, label: "Çözüldü" },
  Closed: { color: "text-slate-400", bg: "bg-slate-500/10 border-slate-500/20", icon: XCircle, label: "Kapalı" },
  Cancelled: { color: "text-gray-400", bg: "bg-gray-500/10 border-gray-500/20", icon: XCircle, label: "İptal" },
  Reopened: { color: "text-orange-400", bg: "bg-orange-500/10 border-orange-500/20", icon: ArrowUpRight, label: "Yeniden Açıldı" },
};

const PRIORITY_CONFIG: Record<string, { color: string; dot: string; label: string }> = {
  Low: { color: "text-slate-400", dot: "bg-slate-400", label: "Düşük" },
  Medium: { color: "text-blue-400", dot: "bg-blue-400", label: "Orta" },
  High: { color: "text-amber-400", dot: "bg-amber-400", label: "Yüksek" },
  Critical: { color: "text-red-400", dot: "bg-red-400", label: "Kritik" },
  Urgent: { color: "text-rose-500", dot: "bg-rose-500", label: "Acil" },
};

const TASK_STATUS_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; label: string }> = {
  Backlog: { icon: Circle, color: "text-slate-400", label: "Beklemede" },
  Todo: { icon: CircleDot, color: "text-blue-400", label: "Yapılacak" },
  InProgress: { icon: Clock, color: "text-amber-400", label: "İşlemde" },
  InReview: { icon: ArrowUpRight, color: "text-purple-400", label: "İnceleme" },
  Done: { icon: CircleCheck, color: "text-emerald-400", label: "Tamamlandı" },
  Cancelled: { icon: XCircle, color: "text-gray-400", label: "İptal" },
};

const CHANNEL_LABELS: Record<string, string> = {
  Portal: "Portal", Email: "E-posta", Phone: "Telefon",
  Chat: "Canlı Destek", Internal: "İç Talep",
};

const ROUTING_LABELS: Record<string, string> = {
  Manual: "Manuel", CategoryDefault: "Kategori", DepartmentDefault: "Departman",
  WorkflowRule: "İş Akışı", Unrouted: "Atanmamış",
};

const STATUS_OPTIONS = ["New", "Open", "InProgress", "WaitingForInfo", "Escalated", "AllTasksDone", "Resolved", "Closed"];
const TASK_STATUS_OPTIONS = ["Backlog", "Todo", "InProgress", "InReview", "Done", "Cancelled"];
const TASK_PRIORITY_OPTIONS = ["Low", "Medium", "High", "Critical"];

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

function slaTimeLeft(deadline?: string): string | null {
  if (!deadline) return null;
  const diff = new Date(deadline).getTime() - Date.now();
  if (diff <= 0) return "Aşıldı";
  const hours = Math.floor(diff / 3600000);
  const mins = Math.floor((diff % 3600000) / 60000);
  if (hours > 0) return `${hours}s ${mins}dk kaldı`;
  return `${mins}dk kaldı`;
}

// ── Component ───────────────────────────────────────────────

export default function TicketDetailPage() {
  const router = useRouter();
  const params = useParams();
  const ticketId = params.id as string;

  const [ticket, setTicket] = useState<TicketDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [commentText, setCommentText] = useState("");
  const [commentSending, setCommentSending] = useState(false);
  const [statusChanging, setStatusChanging] = useState(false);
  const [newStatus, setNewStatus] = useState("");

  // Workflow actions state
  const [workflowActions, setWorkflowActions] = useState<WorkflowAction[]>([]);
  const [actionsLoading, setActionsLoading] = useState(false);
  const [actionExecuting, setActionExecuting] = useState<string | null>(null);
  const [actionComment, setActionComment] = useState("");
  const [selectedOutcome, setSelectedOutcome] = useState("");

  // Task state
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [tasksLoading, setTasksLoading] = useState(false);
  const [showTaskForm, setShowTaskForm] = useState(false);
  const [taskFormData, setTaskFormData] = useState({ title: "", priority: "Medium", assigneeUserId: "", workItemType: "Task" });
  const [taskCreating, setTaskCreating] = useState(false);
  const [queueMembers, setQueueMembers] = useState<QueueMember[]>([]);
  const [reassigning, setReassigning] = useState(false);
  const [ciName, setCiName] = useState<string | null>(null);

  // Fetch ticket
  useEffect(() => {
    if (!ticketId) return;
    setLoading(true);
    fetch(`/api/req/tickets/${ticketId}`)
      .then((r) => (r.ok ? r.json() : null))
      .then((data) => { setTicket(data); setNewStatus(data?.status ?? ""); })
      .catch(() => setTicket(null))
      .finally(() => setLoading(false));
  }, [ticketId]);

  // Fetch tasks linked to this ticket
  useEffect(() => {
    if (!ticketId) return;
    setTasksLoading(true);
    fetch(`/api/pm/work-items/by-source?module=RequestManagement&type=Ticket&sourceId=${ticketId}`)
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setTasks(Array.isArray(data) ? data : []))
      .catch(() => setTasks([]))
      .finally(() => setTasksLoading(false));
  }, [ticketId]);

  // Fetch queue members for task assignment
  useEffect(() => {
    if (!ticket?.serviceQueue) return;
    const queueId = extractId(ticket.serviceQueue.id);
    if (!queueId) return;
    fetch(`/api/req/queues/${queueId}/members`)
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setQueueMembers(Array.isArray(data) ? data : []))
      .catch(() => setQueueMembers([]));
  }, [ticket?.serviceQueue]);

  // Fetch workflow actions when ticket has a workflow
  useEffect(() => {
    if (!ticket?.workflowInstanceId) { setWorkflowActions([]); return; }
    setActionsLoading(true);
    fetch(`/api/wf/instance/${ticket.workflowInstanceId}/actions`)
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setWorkflowActions(Array.isArray(data) ? data : []))
      .catch(() => setWorkflowActions([]))
      .finally(() => setActionsLoading(false));
  }, [ticket?.workflowInstanceId]);

  // Fetch CI name for display
  useEffect(() => {
    if (!ticket?.configurationItemId) { setCiName(null); return; }
    fetch(`/api/pm/applications/${ticket.configurationItemId}`)
      .then((r) => (r.ok ? r.json() : null))
      .then((data) => setCiName(data?.name ?? null))
      .catch(() => setCiName(null));
  }, [ticket?.configurationItemId]);

  const refreshAll = async () => {
    const [ticketRes, tasksRes] = await Promise.all([
      fetch(`/api/req/tickets/${ticketId}`),
      fetch(`/api/pm/work-items/by-source?module=RequestManagement&type=Ticket&sourceId=${ticketId}`),
    ]);
    let ticketData = null;
    if (ticketRes.ok) { ticketData = await ticketRes.json(); setTicket(ticketData); setNewStatus(ticketData.status); }
    if (tasksRes.ok) { setTasks(await tasksRes.json()); }
    // Refresh workflow actions
    if (ticketData?.workflowInstanceId) {
      try {
        const actRes = await fetch(`/api/wf/instance/${ticketData.workflowInstanceId}/actions`);
        if (actRes.ok) setWorkflowActions(await actRes.json());
      } catch { /* silently fail */ }
    } else {
      setWorkflowActions([]);
    }
  };

  const handleComment = async () => {
    if (!commentText.trim()) return;
    setCommentSending(true);
    try {
      const res = await fetch(`/api/req/tickets/${ticketId}/comments`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ content: commentText.trim(), isInternal: false }),
      });
      if (res.ok) { setCommentText(""); await refreshAll(); }
    } catch {
    } finally {
      setCommentSending(false);
    }
  };

  const handleStatusChange = async () => {
    if (!newStatus || newStatus === ticket?.status) return;
    setStatusChanging(true);
    try {
      await fetch(`/api/req/tickets/${ticketId}/status`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ newStatus, reason: null }),
      });
      await refreshAll();
    } catch {
    } finally {
      setStatusChanging(false);
    }
  };

  const handleWorkflowAction = async (bookmarkId: string, decision: string) => {
    if (!ticket?.workflowInstanceId) return;
    setActionExecuting(bookmarkId + decision);
    try {
      await fetch(`/api/wf/instance/${ticket.workflowInstanceId}/actions/${bookmarkId}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ decision, comment: actionComment.trim() || null }),
      });
      setActionComment("");
      // Backend workflow dispatch asenkron — bookmark işlenene kadar bekle
      await new Promise((r) => setTimeout(r, 2500));
      await refreshAll();
    } catch {
    } finally {
      setActionExecuting(null);
    }
  };

  const handleClaimSelf = async (action: WorkflowAction) => {
    if (!ticket?.workflowInstanceId) return;
    setActionExecuting(action.bookmarkId + "ClaimSelf");
    try {
      // DEV: Gerçek kuyruk üyesinin userId'sini kullan (seed'de oluşturulan).
      // Hardcoded DEV_USERS ID'leri veritabanındaki gerçek ID'lerle eşleşmez!
      // Production'da backend ICurrentUser üzerinden JWT'den alacak.
      const claimerId = queueMembers.length > 0
        ? queueMembers[0].userId
        : DEV_USERS[0].id; // fallback — kuyruk üyeleri yüklenemezse

      const res = await fetch(`/api/wf/instance/${ticket.workflowInstanceId}/actions/${action.bookmarkId}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          decision: "ClaimSelf",
          ticketId: action.ticketId || ticketId,
          claimerUserId: claimerId,
        }),
      });
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        console.error("ClaimSelf failed:", res.status, err);
      }
      // Backend workflow dispatch asenkron çalışır — bookmark resume edilene kadar
      // kısa bir bekleme gerekir, yoksa eski bookmark hala görünür.
      await new Promise((r) => setTimeout(r, 1500));
      await refreshAll();
    } catch {
    } finally {
      setActionExecuting(null);
    }
  };

  const handleReassign = async (newAssigneeUserId: string) => {
    if (!ticket || !ticketId || reassigning) return;
    setReassigning(true);
    try {
      const res = await fetch(`/api/req/tickets/${ticketId}/assign`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ assigneeUserId: newAssigneeUserId }),
      });
      if (!res.ok) {
        console.error("Reassign failed:", res.status);
      }
      await refreshAll();
    } catch {
    } finally {
      setReassigning(false);
    }
  };

  const handleCreateTask = async () => {
    if (!taskFormData.title.trim()) return;
    setTaskCreating(true);
    try {
      const res = await fetch("/api/pm/work-items/from-source", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sourceModule: "RequestManagement",
          sourceType: "Ticket",
          sourceId: ticketId,
          title: taskFormData.title.trim(),
          priority: taskFormData.priority,
          workItemType: taskFormData.workItemType || "Task",
          assigneeUserId: taskFormData.assigneeUserId || null,
        }),
      });
      if (res.ok) {
        setTaskFormData({ title: "", priority: "Medium", assigneeUserId: "", workItemType: "Task" });
        setShowTaskForm(false);
        await refreshAll();
      }
    } catch {
    } finally {
      setTaskCreating(false);
    }
  };

  const handleTaskStatusChange = async (taskId: string, newTaskStatus: string) => {
    try {
      await fetch(`/api/pm/work-items/${taskId}/move`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status: newTaskStatus }),
      });
      await refreshAll();
    } catch { /* silently fail */ }
  };

  const handleTaskAssign = async (taskId: string, userId: string) => {
    try {
      await fetch(`/api/pm/work-items/${taskId}/assign`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: userId || null }),
      });
      await refreshAll();
    } catch { /* silently fail */ }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Loader2 className="w-8 h-8 text-indigo-400 animate-spin" />
      </div>
    );
  }

  if (!ticket) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-[var(--color-text-muted)]">
        <XCircle className="w-12 h-12 opacity-30 mb-3" />
        <p>Talep bulunamadı</p>
        <button onClick={() => router.back()} className="mt-4 text-sm text-indigo-400 hover:text-indigo-300">
          ← Geri Dön
        </button>
      </div>
    );
  }

  const statusCfg = STATUS_CONFIG[ticket.status] || STATUS_CONFIG.New;
  const priorityCfg = PRIORITY_CONFIG[ticket.priority] || PRIORITY_CONFIG.Medium;
  const StatusIcon = statusCfg.icon;

  const completedTaskCount = tasks.filter((t) => t.status === "Done" || t.status === "Cancelled").length;
  const taskProgress = tasks.length > 0 ? Math.round((completedTaskCount / tasks.length) * 100) : 0;

  // Merge comments and status history into activity feed
  const activities: { type: "comment" | "status"; time: string; data: any }[] = [
    ...(ticket.comments ?? []).map((c) => ({ type: "comment" as const, time: c.createdAt, data: c })),
    ...(ticket.statusHistory ?? []).map((s) => ({ type: "status" as const, time: s.changedAt, data: s })),
  ].sort((a, b) => new Date(b.time).getTime() - new Date(a.time).getTime());

  return (
    <div className="space-y-6">
      {/* Back + Header */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => router.push("/dashboard/tickets")}
          className="p-2 rounded-lg hover:bg-[var(--color-border)] transition-colors"
        >
          <ArrowLeft className="w-5 h-5 text-[var(--color-text-muted)]" />
        </button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <span className="text-sm font-mono font-bold text-indigo-400">{ticket.number}</span>
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
          </div>
          <h1 className="text-xl font-bold text-[var(--color-text)] mt-1">{ticket.title}</h1>
        </div>
      </div>

      {/* Main content grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left: main content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Description */}
          {ticket.description && (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
              <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3">Açıklama</h3>
              <p className="text-sm text-[var(--color-text-muted)] whitespace-pre-wrap leading-relaxed">
                {ticket.description}
              </p>
            </div>
          )}

          {/* ══════════ GÖREVLER BÖLÜMÜ ══════════ */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)]">
            <div className="px-5 py-4 border-b border-[var(--color-border)] flex items-center justify-between">
              <div className="flex items-center gap-3">
                <ListTodo className="w-4 h-4 text-teal-400" />
                <h3 className="text-sm font-semibold text-[var(--color-text)]">
                  İş Kalemleri {tasks.length > 0 && <span className="text-[var(--color-text-muted)] font-normal">({completedTaskCount}/{tasks.length})</span>}
                </h3>
                {tasks.length > 0 && (
                  <div className="flex items-center gap-2">
                    <div className="w-24 h-1.5 bg-[var(--color-border)] rounded-full overflow-hidden">
                      <div
                        className={cn(
                          "h-full rounded-full transition-all duration-500",
                          taskProgress === 100 ? "bg-emerald-500" : "bg-teal-500"
                        )}
                        style={{ width: `${taskProgress}%` }}
                      />
                    </div>
                    <span className="text-xs text-[var(--color-text-muted)]">{taskProgress}%</span>
                  </div>
                )}
              </div>
              <button
                onClick={() => setShowTaskForm(!showTaskForm)}
                className="flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-medium bg-teal-600 text-white hover:bg-teal-700 transition-all"
              >
                <Plus className="w-3 h-3" />
                İş Kalemi Ekle
              </button>
            </div>

            {/* Task creation form */}
            {showTaskForm && (
              <div className="px-5 py-4 border-b border-[var(--color-border)] bg-[var(--color-input-bg)]/30">
                <div className="space-y-3">
                  <input
                    type="text"
                    value={taskFormData.title}
                    onChange={(e) => setTaskFormData({ ...taskFormData, title: e.target.value })}
                    placeholder="İş kalemi başlığı..."
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                  />
                  <div className="flex gap-2">
                    <select
                      value={taskFormData.workItemType}
                      onChange={(e) => setTaskFormData({ ...taskFormData, workItemType: e.target.value })}
                      className="flex-1 px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none"
                    >
                      <option value="Task">📋 Görev</option>
                      <option value="UserStory">📖 Kullanıcı Hikayesi</option>
                      <option value="Feature">🏗 Özellik</option>
                      <option value="Epic">🎯 Epic</option>
                      <option value="Bug">🐛 Hata</option>
                      <option value="TechDebt">🔧 Teknik Borç</option>
                      <option value="Spike">🔬 Araştırma</option>
                    </select>
                    <select
                      value={taskFormData.priority}
                      onChange={(e) => setTaskFormData({ ...taskFormData, priority: e.target.value })}
                      className="flex-1 px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none"
                    >
                      {TASK_PRIORITY_OPTIONS.map((p) => (
                        <option key={p} value={p}>{PRIORITY_CONFIG[p]?.label ?? p}</option>
                      ))}
                    </select>
                    <select
                      value={taskFormData.assigneeUserId}
                      onChange={(e) => setTaskFormData({ ...taskFormData, assigneeUserId: e.target.value })}
                      className="flex-1 px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none"
                    >
                      <option value="">Atama yapılmadı</option>
                      {queueMembers.map((m) => (
                        <option key={m.userId} value={m.userId}>
                          {m.displayName || m.userId.slice(0, 8)}
                        </option>
                      ))}
                    </select>
                    <button
                      onClick={handleCreateTask}
                      disabled={taskCreating || !taskFormData.title.trim()}
                      className={cn(
                        "px-3 py-1.5 rounded-lg text-xs font-medium",
                        "bg-teal-600 text-white hover:bg-teal-700",
                        "disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                      )}
                    >
                      {taskCreating ? <Loader2 className="w-3 h-3 animate-spin" /> : "Oluştur"}
                    </button>
                    <button
                      onClick={() => setShowTaskForm(false)}
                      className="px-2.5 py-1.5 rounded-lg text-xs text-[var(--color-text-muted)] hover:bg-[var(--color-border)] transition-all"
                    >
                      İptal
                    </button>
                  </div>
                </div>
              </div>
            )}

            {/* Task list */}
            <div className="divide-y divide-[var(--color-border)]">
              {tasksLoading ? (
                <div className="px-5 py-8 text-center">
                  <Loader2 className="w-5 h-5 text-teal-400 animate-spin mx-auto" />
                </div>
              ) : tasks.length === 0 ? (
                <div className="px-5 py-8 text-center text-sm text-[var(--color-text-muted)]">
                  <ListTodo className="w-8 h-8 mx-auto mb-2 opacity-20" />
                  Bu talebe henüz iş kalemi eklenmedi
                </div>
              ) : (
                tasks.map((task) => {
                  const tsCfg = TASK_STATUS_CONFIG[task.status] || TASK_STATUS_CONFIG.Backlog;
                  const tpCfg = PRIORITY_CONFIG[task.priority] || PRIORITY_CONFIG.Medium;
                  const TsIcon = tsCfg.icon;
                  return (
                    <div key={task.id} className="px-5 py-3 flex items-center gap-3 hover:bg-[var(--color-input-bg)]/30 transition-colors group">
                      <TsIcon className={cn("w-4 h-4 shrink-0", tsCfg.color)} />
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="text-xs">{({"Task":"📋","Bug":"🐛","Feature":"🏗","Improvement":"⚡","Epic":"🎯","UserStory":"📖","TechDebt":"🔧","Spike":"🔬"} as Record<string,string>)[task.type] ?? "📋"}</span>
                          <span className="text-xs font-mono text-[var(--color-text-muted)]">{task.workItemNumber}</span>
                          <span className="text-sm text-[var(--color-text)] truncate">{task.title}</span>
                        </div>
                        <div className="flex items-center gap-3 mt-0.5">
                          <span className={cn("text-[10px] font-medium", tpCfg.color)}>
                            <span className={cn("inline-block w-1.5 h-1.5 rounded-full mr-1", tpCfg.dot)} />
                            {tpCfg.label}
                          </span>
                          {task.dueDate && (
                            <span className="text-[10px] text-[var(--color-text-muted)] flex items-center gap-0.5">
                              <Calendar className="w-2.5 h-2.5" />
                              {new Date(task.dueDate).toLocaleDateString("tr-TR")}
                            </span>
                          )}
                        </div>
                      </div>
                      {/* Assignee dropdown */}
                      <select
                        value={task.assigneeUserId ?? ""}
                        onChange={(e) => { e.stopPropagation(); handleTaskAssign(task.id, e.target.value); }}
                        className={cn(
                          "px-2 py-1 rounded text-[10px] border transition-colors cursor-pointer min-w-[110px]",
                          "bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text)]",
                          !task.assigneeUserId && "border-amber-500/40 text-amber-400"
                        )}
                      >
                        <option value="">Atanmamış</option>
                        {(queueMembers.length > 0 ? queueMembers : DEV_USERS.map(u => ({ userId: u.id, displayName: u.fullName }))).map((m) => (
                          <option key={m.userId} value={m.userId}>
                            {("displayName" in m && m.displayName) || getUserName(m.userId)}
                          </option>
                        ))}
                      </select>
                      {/* Quick status change dropdown */}
                      <select
                        value={task.status}
                        onChange={(e) => handleTaskStatusChange(task.id, e.target.value)}
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

          {/* Activity Feed */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)]">
            <div className="px-5 py-4 border-b border-[var(--color-border)]">
              <h3 className="text-sm font-semibold text-[var(--color-text)]">
                Aktivite ({activities.length})
              </h3>
            </div>

            {/* Add comment */}
            <div className="px-5 py-4 border-b border-[var(--color-border)]">
              <div className="flex gap-3">
                <div className="w-8 h-8 rounded-full bg-indigo-500/20 flex items-center justify-center shrink-0">
                  <User className="w-4 h-4 text-indigo-400" />
                </div>
                <div className="flex-1">
                  <textarea
                    value={commentText}
                    onChange={(e) => setCommentText(e.target.value)}
                    placeholder="Yorum yazın..."
                    rows={2}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] resize-none focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                  />
                  <div className="flex justify-end mt-2">
                    <button
                      onClick={handleComment}
                      disabled={commentSending || !commentText.trim()}
                      className={cn(
                        "flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium",
                        "bg-indigo-600 text-white hover:bg-indigo-700",
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

            {/* Activity items */}
            <div className="divide-y divide-[var(--color-border)]">
              {activities.length === 0 ? (
                <div className="px-5 py-8 text-center text-sm text-[var(--color-text-muted)]">
                  Henüz aktivite yok
                </div>
              ) : (
                activities.map((act, i) => (
                  <div key={i} className="px-5 py-3 flex gap-3">
                    <div className={cn(
                      "w-8 h-8 rounded-full flex items-center justify-center shrink-0",
                      act.type === "comment" ? "bg-blue-500/15" : "bg-amber-500/15"
                    )}>
                      {act.type === "comment" ? (
                        <MessageSquare className="w-3.5 h-3.5 text-blue-400" />
                      ) : (
                        <GitBranch className="w-3.5 h-3.5 text-amber-400" />
                      )}
                    </div>
                    <div className="flex-1 min-w-0">
                      {act.type === "comment" ? (
                        <>
                          <p className="text-sm text-[var(--color-text)]">{act.data.content}</p>
                          <p className="text-xs text-[var(--color-text-muted)] mt-1">
                            {formatDateTime(act.data.createdAt)}
                            {act.data.isInternal && (
                              <span className="ml-2 px-1.5 py-0.5 rounded bg-amber-500/10 text-amber-400 text-[10px]">İç Not</span>
                            )}
                          </p>
                        </>
                      ) : (
                        <>
                          <p className="text-sm text-[var(--color-text-muted)]">
                            Durum değişti:{" "}
                            <span className={STATUS_CONFIG[act.data.fromStatus]?.color ?? "text-slate-400"}>
                              {STATUS_CONFIG[act.data.fromStatus]?.label ?? act.data.fromStatus}
                            </span>
                            {" → "}
                            <span className={STATUS_CONFIG[act.data.toStatus]?.color ?? "text-slate-400"}>
                              {STATUS_CONFIG[act.data.toStatus]?.label ?? act.data.toStatus}
                            </span>
                          </p>
                          {act.data.reason && (
                            <p className="text-xs text-[var(--color-text-muted)] mt-0.5">Neden: {act.data.reason}</p>
                          )}
                          <p className="text-xs text-[var(--color-text-muted)] mt-1">
                            {formatDateTime(act.data.changedAt)}
                          </p>
                        </>
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>

        {/* Right sidebar */}
        <div className="space-y-4">
          {/* SLA Card */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3">SLA</h3>
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-xs text-[var(--color-text-muted)]">Yanıt</span>
                {ticket.slaResponseBreached ? (
                  <span className="text-xs text-red-400 flex items-center gap-1"><AlertTriangle className="w-3 h-3" /> İhlal</span>
                ) : ticket.slaRespondedAt ? (
                  <span className="text-xs text-emerald-400 flex items-center gap-1"><CheckCircle2 className="w-3 h-3" /> Yanıtlandı</span>
                ) : ticket.slaResponseDeadline ? (
                  <span className="text-xs text-amber-400 flex items-center gap-1"><Clock className="w-3 h-3" /> {slaTimeLeft(ticket.slaResponseDeadline)}</span>
                ) : (
                  <span className="text-xs text-[var(--color-text-muted)]">—</span>
                )}
              </div>
              <div className="flex items-center justify-between">
                <span className="text-xs text-[var(--color-text-muted)]">Çözüm</span>
                {ticket.slaResolutionBreached ? (
                  <span className="text-xs text-red-400 flex items-center gap-1"><AlertTriangle className="w-3 h-3" /> İhlal</span>
                ) : ticket.resolvedAt ? (
                  <span className="text-xs text-emerald-400 flex items-center gap-1"><CheckCircle2 className="w-3 h-3" /> Çözüldü</span>
                ) : ticket.slaResolutionDeadline ? (
                  <span className="text-xs text-amber-400 flex items-center gap-1"><Clock className="w-3 h-3" /> {slaTimeLeft(ticket.slaResolutionDeadline)}</span>
                ) : (
                  <span className="text-xs text-[var(--color-text-muted)]">—</span>
                )}
              </div>
            </div>
          </div>

          {/* Task Progress Card */}
          {tasks.length > 0 && (
            <div className="rounded-xl border border-teal-500/20 bg-teal-500/5 p-5">
              <h3 className="text-xs font-semibold text-teal-400 uppercase tracking-wider mb-3 flex items-center gap-1.5">
                <ListTodo className="w-3.5 h-3.5" />
                Görev İlerlemesi
              </h3>
              <div className="space-y-2">
                <div className="flex justify-between text-xs">
                  <span className="text-[var(--color-text-muted)]">{completedTaskCount} / {tasks.length} tamamlandı</span>
                  <span className={cn("font-medium", taskProgress === 100 ? "text-emerald-400" : "text-teal-400")}>{taskProgress}%</span>
                </div>
                <div className="w-full h-2 bg-[var(--color-border)] rounded-full overflow-hidden">
                  <div
                    className={cn(
                      "h-full rounded-full transition-all duration-700 ease-out",
                      taskProgress === 100 ? "bg-gradient-to-r from-emerald-500 to-emerald-400" : "bg-gradient-to-r from-teal-600 to-teal-400"
                    )}
                    style={{ width: `${taskProgress}%` }}
                  />
                </div>
              </div>
            </div>
          )}

          {/* Details Card */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3">Detaylar</h3>
            <div className="space-y-3">
              {[
                { icon: Building2, label: "Departman", value: ticket.department?.name ?? "—" },
                { icon: Tag, label: "Kategori", value: ticket.category?.name ?? "—" },
                { icon: Inbox, label: "Kuyruk", value: ticket.serviceQueue?.name ?? "Atanmamış" },
                { icon: User, label: "Atanan", value: getUserName(ticket.assigneeUserId) },
                { icon: GitBranch, label: "Yönlendirme", value: ROUTING_LABELS[ticket.routingSource ?? ""] ?? ticket.routingSource ?? "—" },
                { icon: MessageSquare, label: "Kanal", value: CHANNEL_LABELS[ticket.channel] ?? ticket.channel },
                { icon: Monitor, label: "Uygulama", value: ciName ?? (ticket.configurationItemId ? ticket.configurationItemId.slice(0, 8) + "..." : "—") },
                { icon: Calendar, label: "Oluşturma", value: formatDateTime(ticket.createdAt) },
              ]
              .filter((item) => item.value !== "—" || item.label !== "Uygulama")
              .map((item) => (
                <div key={item.label} className="flex items-center gap-2.5">
                  <item.icon className="w-3.5 h-3.5 text-[var(--color-text-muted)] shrink-0" />
                  <span className="text-xs text-[var(--color-text-muted)] w-20 shrink-0">{item.label}</span>
                  {item.label === "Uygulama" && ticket.configurationItemId ? (
                    <button
                      onClick={() => router.push(`/dashboard/applications/${ticket.configurationItemId}`)}
                      className="text-xs text-indigo-400 hover:text-indigo-300 transition-colors truncate"
                    >
                      {item.value}
                    </button>
                  ) : (
                    <span className="text-xs text-[var(--color-text)] truncate">{item.value}</span>
                  )}
                </div>
              ))}

              {/* Başkasına Ata — sadece kuyruk üyeleri varsa ve ticket atanmışsa */}
              {ticket.assigneeUserId && queueMembers.length > 1 && (
                <div className="pt-2 mt-2 border-t border-[var(--color-border)]">
                  <div className="flex items-center gap-2 mb-1.5">
                    <UserPlus className="w-3.5 h-3.5 text-[var(--color-text-muted)]" />
                    <span className="text-xs text-[var(--color-text-muted)]">Başkasına Ata</span>
                  </div>
                  <div className="flex gap-2">
                    <select
                      id="reassign-select"
                      defaultValue=""
                      disabled={reassigning}
                      onChange={(e) => {
                        if (e.target.value) handleReassign(e.target.value);
                        e.target.value = "";
                      }}
                      className="flex-1 px-2 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-teal-500/40 disabled:opacity-40"
                    >
                      <option value="">Kişi seçin...</option>
                      {queueMembers
                        .filter((m) => m.userId !== ticket.assigneeUserId)
                        .map((m) => (
                          <option key={m.userId} value={m.userId}>
                            {m.displayName || getUserName(m.userId)}
                          </option>
                        ))}
                    </select>
                    {reassigning && <Loader2 className="w-4 h-4 text-teal-400 animate-spin shrink-0 self-center" />}
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Actions Card — Workflow-driven or static fallback */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3 flex items-center gap-1.5">
              <Zap className="w-3.5 h-3.5" />
              Aksiyonlar
            </h3>

            {ticket.workflowInstanceId ? (
              /* ── Workflow-driven dynamic actions ── */
              <div className="space-y-3">
                {actionsLoading ? (
                  <div className="flex items-center gap-2 py-3">
                    <Loader2 className="w-4 h-4 text-teal-400 animate-spin" />
                    <span className="text-xs text-[var(--color-text-muted)]">Aksiyonlar yükleniyor...</span>
                  </div>
                ) : workflowActions.length > 0 ? (
                  workflowActions.map((action) => {
                    // ── WaitForAssignment: "Üzerime Al" butonu ──
                    const isAssignment = action.activityType === "WaitForAssignmentActivity" ||
                      action.activityType === "EntApp.WaitForAssignmentActivity" ||
                      action.outcomes.includes("ClaimSelf");

                    if (isAssignment) {
                      const executing = actionExecuting !== null;
                      return (
                        <div key={action.bookmarkId} className="space-y-2">
                          <div className="flex items-center gap-2">
                            <Hand className="w-3.5 h-3.5 text-amber-400" />
                            <span className="text-xs font-semibold text-[var(--color-text)]">
                              {action.label}
                            </span>
                          </div>
                          <p className="text-[10px] text-[var(--color-text-muted)]">
                            Bu talebi üzerinize alarak işleme başlayabilirsiniz.
                          </p>
                          <button
                            onClick={() => handleClaimSelf(action)}
                            disabled={executing}
                            className="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg text-xs font-semibold bg-amber-600 text-white hover:bg-amber-700 shadow-lg shadow-amber-500/20 transition-all duration-200 disabled:opacity-40 disabled:cursor-not-allowed"
                          >
                            {executing ? (
                              <Loader2 className="w-3.5 h-3.5 animate-spin" />
                            ) : (
                              <Hand className="w-3.5 h-3.5" />
                            )}
                            Üzerime Al
                          </button>
                        </div>
                      );
                    }

                    // ── Generic: combo + uygula butonu ──
                    return (
                    <div key={action.bookmarkId} className="space-y-2">
                      {/* Action label */}
                      <div className="flex items-center gap-2">
                        <ShieldCheck className="w-3.5 h-3.5 text-violet-400" />
                        <span className="text-xs font-semibold text-[var(--color-text)]">
                          {action.label}
                        </span>
                      </div>

                      {/* Inline comment textarea */}
                      <textarea
                        value={actionComment}
                        onChange={(e) => setActionComment(e.target.value)}
                        placeholder="Yorum (opsiyonel)..."
                        rows={2}
                        className="w-full px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] resize-none focus:outline-none focus:ring-2 focus:ring-violet-500/40"
                      />

                      {/* Status label mapping */}
                      {(() => {
                        const statusLabels: Record<string, string> = {
                          approved: "Onayla", rejected: "Reddet",
                          resolved: "Çözüldü", cancelled: "İptal",
                          escalated: "Eskale Et", closed: "Kapat",
                          reopened: "Yeniden Aç", inprogress: "İşleme Al",
                          waitingforinfo: "Bilgi İste", open: "Aç",
                          returntopool: "Havuza Bırak",
                        };
                        const getLabel = (o: string) => statusLabels[o.toLowerCase()] || o;
                        const currentOutcome = selectedOutcome || action.outcomes[0] || "";
                        const executing = actionExecuting !== null;

                        return (
                          <div className="flex gap-2">
                            <select
                              value={currentOutcome}
                              onChange={(e) => setSelectedOutcome(e.target.value)}
                              disabled={executing}
                              className="flex-1 px-2.5 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-violet-500/40 disabled:opacity-40"
                            >
                              {action.outcomes.map((o) => (
                                <option key={o} value={o}>{getLabel(o)}</option>
                              ))}
                            </select>
                            <button
                              onClick={() => handleWorkflowAction(action.bookmarkId, currentOutcome)}
                              disabled={executing}
                              className="px-4 py-2 rounded-lg text-xs font-semibold bg-violet-600 text-white hover:bg-violet-700 shadow-lg shadow-violet-500/20 transition-all duration-200 disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-1.5"
                            >
                              {executing ? (
                                <Loader2 className="w-3 h-3 animate-spin" />
                              ) : (
                                <ShieldCheck className="w-3.5 h-3.5" />
                              )}
                              Uygula
                            </button>
                          </div>
                        );
                      })()}
                    </div>
                    );
                  })
                ) : (
                  /* No active bookmarks — workflow is running but no user action needed */
                  <div className="py-3 text-center">
                    <div className="w-8 h-8 mx-auto mb-2 rounded-full bg-violet-500/10 flex items-center justify-center">
                      <Zap className="w-4 h-4 text-violet-400" />
                    </div>
                    <p className="text-xs text-[var(--color-text-muted)]">
                      Workflow çalışıyor — şu an aksiyonunuz beklenmiyor
                    </p>
                  </div>
                )}

                {/* Manual override — collapsible */}
                <details className="mt-2">
                  <summary className="text-[10px] text-[var(--color-text-muted)] cursor-pointer hover:text-[var(--color-text)] transition-colors">
                    Manuel durum değiştir
                  </summary>
                  <div className="mt-2 flex gap-2">
                    <select
                      value={newStatus}
                      onChange={(e) => setNewStatus(e.target.value)}
                      className="flex-1 px-2 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none"
                    >
                      {STATUS_OPTIONS.map((s) => (
                        <option key={s} value={s}>{STATUS_CONFIG[s]?.label ?? s}</option>
                      ))}
                    </select>
                    <button
                      onClick={handleStatusChange}
                      disabled={statusChanging || newStatus === ticket.status}
                      className={cn(
                        "px-3 py-1.5 rounded-lg text-xs font-medium",
                        "bg-indigo-600 text-white hover:bg-indigo-700",
                        "disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                      )}
                    >
                      {statusChanging ? <Loader2 className="w-3 h-3 animate-spin" /> : "Uygula"}
                    </button>
                  </div>
                </details>
              </div>
            ) : (
              /* ── Static fallback — no workflow ── */
              <div className="space-y-3">
                <div>
                  <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Durum Değiştir</label>
                  <div className="flex gap-2">
                    <select
                      value={newStatus}
                      onChange={(e) => setNewStatus(e.target.value)}
                      className="flex-1 px-2 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none"
                    >
                      {STATUS_OPTIONS.map((s) => (
                        <option key={s} value={s}>{STATUS_CONFIG[s]?.label ?? s}</option>
                      ))}
                    </select>
                    <button
                      onClick={handleStatusChange}
                      disabled={statusChanging || newStatus === ticket.status}
                      className={cn(
                        "px-3 py-1.5 rounded-lg text-xs font-medium",
                        "bg-indigo-600 text-white hover:bg-indigo-700",
                        "disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                      )}
                    >
                      {statusChanging ? <Loader2 className="w-3 h-3 animate-spin" /> : "Uygula"}
                    </button>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
