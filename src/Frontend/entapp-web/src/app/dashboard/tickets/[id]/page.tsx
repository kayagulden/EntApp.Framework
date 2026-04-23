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
  UserCheck,
  ListTodo,
  CircleDot,
  CircleCheck,
  Circle,
  ChevronDown,
  Zap,
  ShieldX,
  Monitor,
  FolderKanban,
  Save,
  Activity,
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
  childTicketCount?: number;
  completedChildTicketCount?: number;
  parentTicketId?: { value: string } | string;
  parentTicket?: { id?: { value: string } | string; number: string; title: string };
  workflowInstanceId?: string;
  category?: { name: string };
  department?: { name: string };
  serviceQueue?: { name: string; code: string; id?: { value: string } | string };
  comments?: CommentData[];
  statusHistory?: StatusHistoryData[];
  configurationItemId?: string;
  createdAt: string;
  resolvedAt?: string;
  projectId?: string;
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

interface ChildTicketItem {
  id: string;
  number: string;
  title: string;
  status: string;
  priority: string;
  channel: string;
  categoryName?: string;
  departmentName?: string;
  assigneeUserId?: string;
  serviceQueueName?: string;
  createdAt: string;
  resolvedAt?: string;
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

// STATUS_OPTIONS removed — transitions come from StateFlow API
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


  // Allowed transitions from StateFlow
  interface AllowedTransition { triggerName: string; label: string; toStateName: string; requiredRole?: string }
  const [allowedTransitions, setAllowedTransitions] = useState<AllowedTransition[]>([]);

  // Child ticket state
  const [childTickets, setChildTickets] = useState<ChildTicketItem[]>([]);
  const [childTicketsLoading, setChildTicketsLoading] = useState(false);
  const [showChildForm, setShowChildForm] = useState(false);
  const [childFormData, setChildFormData] = useState({ title: "", categoryId: "", departmentId: "", priority: "Medium", description: "" });
  const [childCreating, setChildCreating] = useState(false);

  // Department & Category for child ticket form
  const [childDepartments, setChildDepartments] = useState<{ id: string; name: string }[]>([]);
  const [childCategories, setChildCategories] = useState<{ id: string; name: string; departmentId: string }[]>([]);
  const [queueMembers, setQueueMembers] = useState<QueueMember[]>([]);
  const [reassigning, setReassigning] = useState(false);
  const [claiming, setClaiming] = useState(false);
  const [ciName, setCiName] = useState<string | null>(null);

  // Hierarchy tree
  interface HierarchyNode { id: string; number: string; title: string; status: string; children: HierarchyNode[] }
  const [hierarchyTree, setHierarchyTree] = useState<HierarchyNode | null>(null);

  // Projeye Aktar state
  interface ProjectOption { id: string; name: string; key: string; category: string; }
  const [showPromoteModal, setShowPromoteModal] = useState(false);
  const [projects, setProjects] = useState<ProjectOption[]>([]);
  const [promoteForm, setPromoteForm] = useState({ projectId: "", workItemType: "Feature", priority: "Medium", title: "" });
  const [promoting, setPromoting] = useState(false);

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

  // Fetch child tickets
  useEffect(() => {
    if (!ticketId) return;
    setChildTicketsLoading(true);
    fetch(`/api/req/tickets/${ticketId}/children`)
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setChildTickets(Array.isArray(data) ? data : []))
      .catch(() => setChildTickets([]))
      .finally(() => setChildTicketsLoading(false));
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


  // Fetch allowed transitions from StateFlow
  useEffect(() => {
    if (!ticketId) return;
    fetch(`/api/req/tickets/${ticketId}/allowed-transitions`)
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setAllowedTransitions(Array.isArray(data) ? data : []))
      .catch(() => setAllowedTransitions([]));
  }, [ticketId, ticket?.status]);

  // Fetch CI name for display
  useEffect(() => {
    if (!ticket?.configurationItemId) { setCiName(null); return; }
    fetch(`/api/pm/applications/${ticket.configurationItemId}`)
      .then((r) => (r.ok ? r.json() : null))
      .then((data) => setCiName(data?.name ?? null))
      .catch(() => setCiName(null));
  }, [ticket?.configurationItemId]);

  // Fetch departments & categories when child form opens
  useEffect(() => {
    if (!showChildForm) return;
    fetch("/api/v1/org/departments")
      .then((r) => (r.ok ? r.json() : { value: [] }))
      .then((data) => {
        const list = Array.isArray(data) ? data : (data.value ?? []);
        setChildDepartments(list.map((d: any) => ({ id: extractId(d.id), name: d.name })));
      })
      .catch(() => setChildDepartments([]));
    fetch("/api/req/categories")
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => {
        const list = Array.isArray(data) ? data : [];
        setChildCategories(list.map((c: any) => ({
          id: extractId(c.id),
          name: c.name,
          departmentId: extractId(c.departmentId),
        })));
      })
      .catch(() => setChildCategories([]));
  }, [showChildForm]);

  // Fetch hierarchy tree
  useEffect(() => {
    if (!ticketId) return;
    fetch(`/api/req/tickets/${ticketId}/hierarchy`)
      .then((r) => (r.ok ? r.json() : null))
      .then((data) => setHierarchyTree(data))
      .catch(() => setHierarchyTree(null));
  }, [ticketId]);

  const refreshAll = async () => {
    const [ticketRes, childRes, hierRes] = await Promise.all([
      fetch(`/api/req/tickets/${ticketId}`),
      fetch(`/api/req/tickets/${ticketId}/children`),
      fetch(`/api/req/tickets/${ticketId}/hierarchy`),
    ]);
    let ticketData = null;
    if (ticketRes.ok) { ticketData = await ticketRes.json(); setTicket(ticketData); setNewStatus(ticketData.status); }
    if (childRes.ok) { setChildTickets(await childRes.json()); }
    if (hierRes.ok) { setHierarchyTree(await hierRes.json()); }
    // Refresh allowed transitions
    try {
      const transRes = await fetch(`/api/req/tickets/${ticketId}/allowed-transitions`);
      if (transRes.ok) setAllowedTransitions(await transRes.json());
    } catch { /* */ }
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

  const handleClaimSelf = async () => {
    if (!ticket || !ticketId || claiming) return;
    setClaiming(true);
    try {
      // Kuyruk üyelerinden ilkini claimer olarak kullan (dev modda)
      const claimerId = queueMembers.length > 0
        ? queueMembers[0].userId
        : DEV_USERS[0].id;
      const res = await fetch(`/api/req/tickets/${ticketId}/claim`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ claimerUserId: claimerId }),
      });
      if (!res.ok) {
        const err = await res.text();
        alert(err || "Üzerine alma başarısız.");
      }
      await refreshAll();
    } catch {
    } finally {
      setClaiming(false);
    }
  };

  const handleCreateChildTicket = async () => {
    if (!childFormData.title.trim() || !childFormData.categoryId || !childFormData.departmentId) return;
    setChildCreating(true);
    try {
      const res = await fetch(`/api/req/tickets/${ticketId}/children`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          title: childFormData.title.trim(),
          categoryId: childFormData.categoryId,
          departmentId: childFormData.departmentId,
          priority: childFormData.priority,
          description: childFormData.description || null,
          channel: "Internal",
        }),
      });
      if (res.ok) {
        setChildFormData({ title: "", categoryId: "", departmentId: "", priority: "Medium", description: "" });
        setShowChildForm(false);
        await refreshAll();
      } else {
        const err = await res.text();
        alert(err || "Alt talep oluşturulamadı.");
      }
    } catch {
    } finally {
      setChildCreating(false);
    }
  };

  const openPromoteModal = async () => {
    setPromoteForm({ projectId: "", workItemType: "Feature", priority: ticket?.priority || "Medium", title: ticket?.title || "" });
    setShowPromoteModal(true);
    try {
      const res = await fetch("/api/pm/projects");
      if (res.ok) {
        const data = await res.json();
        setProjects((data.items || data || []).map((p: any) => ({ id: p.id, name: p.name, key: p.key, category: p.category })));
      }
    } catch { /* */ }
  };

  const handlePromoteToProject = async () => {
    if (!promoteForm.projectId || !promoteForm.title.trim()) return;
    setPromoting(true);
    try {
      const res = await fetch("/api/pm/work-items/promote-ticket", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          ticketId: ticketId,
          projectId: promoteForm.projectId,
          title: promoteForm.title.trim(),
          description: ticket?.description || null,
          priority: promoteForm.priority,
          workItemType: promoteForm.workItemType,
        }),
      });
      if (res.ok) {
        setShowPromoteModal(false);
        await refreshAll();
      }
    } catch { /* */ }
    finally { setPromoting(false); }
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

  const completedChildCount = childTickets.filter((t) => t.status === "Resolved" || t.status === "Closed" || t.status === "Cancelled").length;
  const childProgress = childTickets.length > 0 ? Math.round((completedChildCount / childTickets.length) * 100) : 0;
  const isAlreadyPromoted = !!ticket.projectId;

  // Hierarchy limits
  const MAX_DEPTH = 3;
  const MAX_CHILDREN = 10;
  const currentDepth = (() => {
    if (!hierarchyTree) return 0;
    const findDepth = (node: HierarchyNode, id: string, depth: number): number => {
      if (node.id === id) return depth;
      for (const child of node.children) {
        const found = findDepth(child, id, depth + 1);
        if (found >= 0) return found;
      }
      return -1;
    };
    return findDepth(hierarchyTree, extractId(ticket.id), 0);
  })();
  const canAddChild = currentDepth < MAX_DEPTH - 1 && childTickets.length < MAX_CHILDREN;

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
          onClick={() => router.push(
            ticket.parentTicketId
              ? `/dashboard/tickets/${extractId(ticket.parentTicketId)}`
              : "/dashboard/tickets"
          )}
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
            {childTickets.length > 0 && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium bg-teal-500/10 text-teal-400 border border-teal-500/20">
                <ListTodo className="w-3 h-3" />
                {childTickets.length} alt talep
              </span>
            )}
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

          {/* ══════════ PROJEYE AKTAR MODAL ══════════ */}
          {showPromoteModal && (
            <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
              <div className="w-full max-w-lg mx-4 rounded-2xl border border-violet-500/30 bg-[var(--color-card-bg)] shadow-2xl shadow-violet-500/10">
                <div className="px-6 py-4 border-b border-[var(--color-border)] flex items-center gap-3">
                  <div className="w-9 h-9 rounded-xl bg-violet-500/15 flex items-center justify-center">
                    <FolderKanban className="w-5 h-5 text-violet-400" />
                  </div>
                  <div>
                    <h3 className="text-sm font-semibold text-[var(--color-text)]">Projeye Aktar</h3>
                    <p className="text-[10px] text-[var(--color-text-muted)]">Talebi bir projenin backlog&apos;una iş kalemi olarak ekle</p>
                  </div>
                </div>
                <div className="px-6 py-5 space-y-4">
                  {/* Proje Seçimi */}
                  <div>
                    <label className="block text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider mb-1.5">Hedef Proje *</label>
                    <select
                      value={promoteForm.projectId}
                      onChange={e => setPromoteForm({ ...promoteForm, projectId: e.target.value })}
                      className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-violet-500/40"
                    >
                      <option value="">Proje seçin...</option>
                      {projects.map(p => (
                        <option key={p.id} value={p.id}>{p.key} — {p.name}</option>
                      ))}
                    </select>
                  </div>
                  {/* Başlık */}
                  <div>
                    <label className="block text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider mb-1.5">İş Kalemi Başlığı *</label>
                    <input
                      type="text"
                      value={promoteForm.title}
                      onChange={e => setPromoteForm({ ...promoteForm, title: e.target.value })}
                      className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40"
                    />
                  </div>
                  {/* Tip + Öncelik */}
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="block text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider mb-1.5">Tip</label>
                      <select
                        value={promoteForm.workItemType}
                        onChange={e => setPromoteForm({ ...promoteForm, workItemType: e.target.value })}
                        className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-violet-500/40"
                      >
                        <option value="Feature">🏗 Feature</option>
                        <option value="Epic">🎯 Epic</option>
                        <option value="UserStory">📖 User Story</option>
                        <option value="Bug">🐛 Bug</option>
                        <option value="Task">📋 Task</option>
                        <option value="TechDebt">🔧 Teknik Borç</option>
                        <option value="Spike">🔬 Araştırma</option>
                      </select>
                    </div>
                    <div>
                      <label className="block text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider mb-1.5">Öncelik</label>
                      <select
                        value={promoteForm.priority}
                        onChange={e => setPromoteForm({ ...promoteForm, priority: e.target.value })}
                        className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-violet-500/40"
                      >
                        <option value="Low">Düşük</option>
                        <option value="Medium">Orta</option>
                        <option value="High">Yüksek</option>
                        <option value="Critical">Kritik</option>
                      </select>
                    </div>
                  </div>
                  {/* Source info */}
                  <div className="rounded-lg bg-violet-500/5 border border-violet-500/20 px-3 py-2">
                    <p className="text-[10px] text-violet-300">
                      <span className="font-medium">Kaynak:</span> {ticket.number} — {ticket.title}
                    </p>
                    <p className="text-[10px] text-[var(--color-text-muted)] mt-0.5">
                      Talep açık kalacak, iş kalemi tamamlandığında takip edilebilir.
                    </p>
                  </div>
                </div>
                <div className="px-6 py-4 border-t border-[var(--color-border)] flex items-center justify-end gap-2">
                  <button
                    onClick={() => setShowPromoteModal(false)}
                    className="px-4 py-2 rounded-lg text-xs text-[var(--color-text-muted)] hover:bg-[var(--color-border)] transition-colors"
                  >
                    İptal
                  </button>
                  <button
                    onClick={handlePromoteToProject}
                    disabled={promoting || !promoteForm.projectId || !promoteForm.title.trim()}
                    className="flex items-center gap-1.5 px-5 py-2 rounded-lg text-xs font-medium bg-violet-600 text-white hover:bg-violet-700 disabled:opacity-50 transition-colors shadow-lg shadow-violet-500/20"
                  >
                    {promoting ? <Loader2 className="w-3 h-3 animate-spin" /> : <FolderKanban className="w-3 h-3" />}
                    Projeye Aktar
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* ══════════ ALT TALEPLER BÖLÜMÜ ══════════ */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)]">
            <div className="px-5 py-4 border-b border-[var(--color-border)] flex items-center justify-between">
              <div className="flex items-center gap-3">
                <ListTodo className="w-4 h-4 text-teal-400" />
                <h3 className="text-sm font-semibold text-[var(--color-text)]">
                  Alt Talepler {childTickets.length > 0 && <span className="text-[var(--color-text-muted)] font-normal">({completedChildCount}/{childTickets.length})</span>}
                </h3>
                {childTickets.length > 0 && (
                  <div className="flex items-center gap-2">
                    <div className="w-24 h-1.5 bg-[var(--color-border)] rounded-full overflow-hidden">
                      <div
                        className={cn(
                          "h-full rounded-full transition-all duration-500",
                          childProgress === 100 ? "bg-emerald-500" : "bg-teal-500"
                        )}
                        style={{ width: `${childProgress}%` }}
                      />
                    </div>
                    <span className="text-xs text-[var(--color-text-muted)]">{childProgress}%</span>
                  </div>
                )}
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={openPromoteModal}
                  className="flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-medium bg-violet-600 text-white hover:bg-violet-700 transition-all"
                >
                  <FolderKanban className="w-3 h-3" />
                  Projeye Aktar
                </button>
                <button
                  onClick={() => setShowChildForm(!showChildForm)}
                  disabled={!canAddChild}
                  title={!canAddChild
                    ? currentDepth >= MAX_DEPTH - 1
                      ? `Maksimum hiyerarşi derinliğine (${MAX_DEPTH} seviye) ulaşıldı`
                      : `Maksimum alt talep sayısına (${MAX_CHILDREN}) ulaşıldı`
                    : undefined}
                  className={cn(
                    "flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-medium transition-all",
                    canAddChild
                      ? "bg-teal-600 text-white hover:bg-teal-700"
                      : "bg-[var(--color-border)] text-[var(--color-text-muted)] cursor-not-allowed opacity-50"
                  )}
                >
                  <Plus className="w-3 h-3" />
                  Alt Talep Ekle
                </button>
              </div>
            </div>

            {/* Child ticket creation form */}
            {showChildForm && (
              <div className="px-5 py-4 border-b border-[var(--color-border)] bg-[var(--color-input-bg)]/30">
                <div className="space-y-3">
                  <input
                    type="text"
                    value={childFormData.title}
                    onChange={(e) => setChildFormData({ ...childFormData, title: e.target.value })}
                    placeholder="Alt talep başlığı..."
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                  />
                  <textarea
                    value={childFormData.description}
                    onChange={(e) => setChildFormData({ ...childFormData, description: e.target.value })}
                    placeholder="Açıklama (opsiyonel)..."
                    rows={2}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] resize-none focus:outline-none focus:ring-2 focus:ring-teal-500/40"
                  />
                  <div className="grid grid-cols-3 gap-2">
                    <select
                      value={childFormData.departmentId}
                      onChange={(e) => setChildFormData({ ...childFormData, departmentId: e.target.value, categoryId: "" })}
                      className="px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none"
                    >
                      <option value="">Departman seçin</option>
                      {childDepartments.map((d) => (
                        <option key={d.id} value={d.id}>{d.name}</option>
                      ))}
                    </select>
                    <select
                      value={childFormData.categoryId}
                      onChange={(e) => setChildFormData({ ...childFormData, categoryId: e.target.value })}
                      disabled={!childFormData.departmentId}
                      className={cn(
                        "px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none",
                        !childFormData.departmentId && "opacity-40 cursor-not-allowed"
                      )}
                    >
                      <option value="">{childFormData.departmentId ? "Kategori seçin" : "Önce departman seçin"}</option>
                      {childCategories
                        .filter((c) => c.departmentId === childFormData.departmentId)
                        .map((c) => (
                          <option key={c.id} value={c.id}>{c.name}</option>
                        ))}
                    </select>
                    <select
                      value={childFormData.priority}
                      onChange={(e) => setChildFormData({ ...childFormData, priority: e.target.value })}
                      className="px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none"
                    >
                      <option value="Low">Düşük</option>
                      <option value="Medium">Orta</option>
                      <option value="High">Yüksek</option>
                      <option value="Critical">Kritik</option>
                    </select>
                  </div>
                  <div className="flex justify-end gap-2">
                    <button
                      onClick={() => setShowChildForm(false)}
                      className="px-2.5 py-1.5 rounded-lg text-xs text-[var(--color-text-muted)] hover:bg-[var(--color-border)] transition-all"
                    >
                      İptal
                    </button>
                    <button
                      onClick={handleCreateChildTicket}
                      disabled={childCreating || !childFormData.title.trim() || !childFormData.categoryId || !childFormData.departmentId}
                      className={cn(
                        "px-4 py-1.5 rounded-lg text-xs font-medium",
                        "bg-teal-600 text-white hover:bg-teal-700",
                        "disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                      )}
                    >
                      {childCreating ? <Loader2 className="w-3 h-3 animate-spin" /> : "Alt Talep Oluştur"}
                    </button>
                  </div>
                </div>
              </div>
            )}

            {/* Child ticket list */}
            <div className="divide-y divide-[var(--color-border)]">
              {childTicketsLoading ? (
                <div className="px-5 py-8 text-center">
                  <Loader2 className="w-5 h-5 text-teal-400 animate-spin mx-auto" />
                </div>
              ) : childTickets.length === 0 ? (
                <div className="px-5 py-8 text-center text-sm text-[var(--color-text-muted)]">
                  <ListTodo className="w-8 h-8 mx-auto mb-2 opacity-20" />
                  Bu talebe henüz alt talep eklenmedi
                </div>
              ) : (
                childTickets.map((child) => {
                  const childStatusCfg = STATUS_CONFIG[child.status] || STATUS_CONFIG.New;
                  const childPriorityCfg = PRIORITY_CONFIG[child.priority] || PRIORITY_CONFIG.Medium;
                  const ChildStatusIcon = childStatusCfg.icon;
                  return (
                    <a
                      key={child.id}
                      href={`/dashboard/tickets/${child.id}`}
                      className="px-5 py-3 flex items-center gap-3 hover:bg-[var(--color-input-bg)]/30 transition-colors group block"
                    >
                      <ChildStatusIcon className={cn("w-4 h-4 shrink-0", childStatusCfg.color)} />
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-mono text-[var(--color-text-muted)]">{child.number}</span>
                          <span className="text-sm text-[var(--color-text)] truncate">{child.title}</span>
                        </div>
                        <div className="flex items-center gap-3 mt-0.5">
                          <span className={cn("text-[10px] font-medium", childPriorityCfg.color)}>
                            <span className={cn("inline-block w-1.5 h-1.5 rounded-full mr-1", childPriorityCfg.dot)} />
                            {childPriorityCfg.label}
                          </span>
                          {child.departmentName && (
                            <span className="text-[10px] text-[var(--color-text-muted)]">
                              🏢 {child.departmentName}
                            </span>
                          )}
                          {child.serviceQueueName && (
                            <span className="text-[10px] text-[var(--color-text-muted)]">
                              📮 {child.serviceQueueName}
                            </span>
                          )}
                          {child.assigneeUserId && (
                            <span className="text-[10px] text-[var(--color-text-muted)]">
                              👤 {getUserName(child.assigneeUserId)}
                            </span>
                          )}
                        </div>
                      </div>
                      <span className={cn(
                        "px-2 py-0.5 rounded-full text-[10px] font-medium border border-current/20",
                        childStatusCfg.bg, childStatusCfg.color
                      )}>
                        {childStatusCfg.label}
                      </span>
                    </a>
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

          {/* Child Ticket Progress Card */}
          {childTickets.length > 0 && (
            <div className="rounded-xl border border-teal-500/20 bg-teal-500/5 p-5">
              <h3 className="text-xs font-semibold text-teal-400 uppercase tracking-wider mb-3 flex items-center gap-1.5">
                <ListTodo className="w-3.5 h-3.5" />
                Alt Talep İlerlemesi
              </h3>
              <div className="space-y-2">
                <div className="flex justify-between text-xs">
                  <span className="text-[var(--color-text-muted)]">{completedChildCount} / {childTickets.length} tamamlandı</span>
                  <span className={cn("font-medium", childProgress === 100 ? "text-emerald-400" : "text-teal-400")}>{childProgress}%</span>
                </div>
                <div className="w-full h-2 bg-[var(--color-border)] rounded-full overflow-hidden">
                  <div
                    className={cn(
                      "h-full rounded-full transition-all duration-700 ease-out",
                      childProgress === 100 ? "bg-gradient-to-r from-emerald-500 to-emerald-400" : "bg-gradient-to-r from-teal-600 to-teal-400"
                    )}
                    style={{ width: `${childProgress}%` }}
                  />
                </div>
              </div>
            </div>
          )}

          {/* ══════════ Ticket Hierarchy Tree (API-driven) ══════════ */}
          {hierarchyTree && (() => {
            const currentId = extractId(ticket.id);

            const renderNode = (node: HierarchyNode, depth: number, isLast: boolean, parentLines: boolean[]): React.ReactNode => {
              const isCurrent = node.id === currentId;
              const statusCfgNode = STATUS_CONFIG[node.status];
              const NodeIcon = statusCfgNode?.icon || CircleDot;
              return (
                <div key={node.id}>
                  <div className="flex items-center gap-0">
                    {/* Tree indent lines */}
                    {depth > 0 && (
                      <div className="flex items-center" style={{ width: `${(depth - 1) * 16}px` }}>
                        {parentLines.slice(0, -1).map((showLine, i) => (
                          <div key={i} className="w-4 h-6 relative flex-shrink-0">
                            {showLine && <div className="absolute left-[7px] top-0 bottom-0 w-px bg-[var(--color-border)]/50" />}
                          </div>
                        ))}
                      </div>
                    )}
                    {depth > 0 && (
                      <div className="w-4 h-6 relative flex-shrink-0">
                        <div className={cn("absolute left-[7px] top-0 w-px bg-[var(--color-border)]/50", isLast ? "h-3" : "h-full")} />
                        <div className="absolute left-[7px] top-3 w-[9px] h-px bg-[var(--color-border)]/50" />
                      </div>
                    )}
                    {/* Node content */}
                    {isCurrent ? (
                      <div className="flex items-center gap-1.5 flex-1 min-w-0 px-1.5 py-0.5 rounded bg-indigo-500/15 border border-indigo-500/25">
                        <NodeIcon className="w-3 h-3 text-indigo-400 flex-shrink-0" />
                        <span className="text-[11px] font-mono font-bold text-indigo-400 flex-shrink-0">{node.number}</span>
                        <span className="text-[11px] text-indigo-300 truncate">{node.title}</span>
                      </div>
                    ) : (
                      <a
                        href={`/dashboard/tickets/${node.id}`}
                        className="flex items-center gap-1.5 flex-1 min-w-0 px-1.5 py-0.5 rounded hover:bg-[var(--color-border)]/40 transition-colors"
                      >
                        <NodeIcon className="w-3 h-3 text-[var(--color-text-muted)] flex-shrink-0" />
                        <span className="text-[11px] font-mono text-[var(--color-text-muted)] flex-shrink-0">{node.number}</span>
                        <span className="text-[11px] text-[var(--color-text-muted)] truncate">{node.title}</span>
                      </a>
                    )}
                  </div>
                  {node.children.map((child, idx) =>
                    renderNode(child, depth + 1, idx === node.children.length - 1, [...parentLines, !isLast])
                  )}
                </div>
              );
            };

            return (
              <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-4">
                <h3 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-2.5 flex items-center gap-1.5">
                  <GitBranch className="w-3.5 h-3.5" />
                  Talep Hiyerarşisi
                </h3>
                <div className="space-y-0">
                  {renderNode(hierarchyTree, 0, true, [])}
                </div>
              </div>
            );
          })()}

          {/* Details Card */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3">Detaylar</h3>
            <div className="space-y-3">
              {/* Durum satırı — renkli badge ile */}
              <div className="flex items-center gap-2.5">
                <Activity className="w-3.5 h-3.5 text-[var(--color-text-muted)] shrink-0" />
                <span className="text-xs text-[var(--color-text-muted)] w-20 shrink-0">Durum</span>
                <span className={cn(
                  "inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold",
                  STATUS_CONFIG[ticket.status]?.color ?? "bg-gray-500/10 text-gray-400"
                )}>
                  {STATUS_CONFIG[ticket.status]?.icon && (() => {
                    const Icon = STATUS_CONFIG[ticket.status].icon;
                    return <Icon className="w-3 h-3" />;
                  })()}
                  {STATUS_CONFIG[ticket.status]?.label ?? ticket.status}
                </span>
              </div>
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

            </div>
          </div>

          {/* Actions Card — StateFlow-driven */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3 flex items-center gap-1.5">
              <Zap className="w-3.5 h-3.5" />
              Aksiyonlar
            </h3>

            <div className="space-y-3">
              {/* WaitForAssignment → Üzerime Al, diğer durumlar → Durum Değiştir */}
              {ticket.status === "WaitForAssignment" ? (
                <div>
                  <p className="text-xs text-[var(--color-text-muted)] mb-2">
                    Bu talep atama bekliyor. Üzerinize alarak işleme başlayabilirsiniz.
                  </p>
                  <button
                    onClick={handleClaimSelf}
                    disabled={claiming}
                    className="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg text-xs font-semibold bg-amber-600 text-white hover:bg-amber-700 shadow-lg shadow-amber-500/20 transition-all duration-200 disabled:opacity-40 disabled:cursor-not-allowed"
                  >
                    {claiming ? (
                      <Loader2 className="w-3.5 h-3.5 animate-spin" />
                    ) : (
                      <UserCheck className="w-3.5 h-3.5" />
                    )}
                    Üzerime Al
                  </button>
                </div>
              ) : (
                <div>
                  <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Durum Geçişi</label>
                  {allowedTransitions.length > 0 ? (
                    <div className="flex gap-2">
                      <select
                        value={newStatus}
                        onChange={(e) => setNewStatus(e.target.value)}
                        className="flex-1 px-2 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none"
                      >
                        <option value={ticket.status}>İşlem seçin...</option>
                        {allowedTransitions.map((t) => (
                          <option key={t.triggerName} value={t.toStateName}>{t.label || t.triggerName}</option>
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
                  ) : (
                    <p className="text-xs text-[var(--color-text-muted)] italic">Bu durumdan geçiş tanımlı değil</p>
                  )}
                </div>
              )}

              {/* Başkasına Ata — kuyruk üyeleri varsa her zaman göster (dispatcher) */}
              {queueMembers.length > 0 && (
                <div className="pt-2 border-t border-[var(--color-border)]">
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
        </div>
      </div>
    </div>
  );
}

