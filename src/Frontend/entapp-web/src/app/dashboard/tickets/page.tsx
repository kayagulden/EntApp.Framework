"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import { useAuthStore } from "@/stores";
import { useRouter } from "next/navigation";
import {
  Ticket,
  Plus,
  Search,
  Clock,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  ArrowUpRight,
  Loader2,
  Building2,
  User,
  Users,
  MessageSquare,
  X,
  Send,
  Inbox,
  ClipboardList,
  UserCheck,
  Hand,
  Monitor,
} from "lucide-react";
import { cn } from "@/lib/utils";

// ── Interfaces ──────────────────────────────────────────────

interface TicketData {
  id: { value: string } | string;
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
  assigneeUserId?: string;
  reporterUserId: string;
  category?: { name: string };
  department?: { name: string };
  serviceQueue?: { name: string; code: string };
  createdAt: string;
  resolvedAt?: string;
}

interface TicketListResult {
  items: TicketData[];
  totalCount: number;
}

interface DepartmentOption {
  id: string;
  name: string;
  code: string;
}

interface CategoryOption {
  id: { value: string } | string;
  name: string;
  code: string;
  departmentId: { value: string } | string;
}

interface MyQueueData {
  queueId: string;
  name: string;
  code: string;
  description?: string;
  departmentName?: string;
  role: string;
  ticketCount: number;
  unassignedCount: number;
}

interface ApplicationOption {
  id: string;
  name: string;
  code: string;
}

type TabKey = "my-requests" | "my-assignments" | "queue-pool";

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

function slaTimeLeft(deadline?: string): string | null {
  if (!deadline) return null;
  const diff = new Date(deadline).getTime() - Date.now();
  if (diff <= 0) return "Aşıldı";
  const hours = Math.floor(diff / 3600000);
  const mins = Math.floor((diff % 3600000) / 60000);
  if (hours > 0) return `${hours}s ${mins}dk`;
  return `${mins}dk`;
}

// ── Config ──────────────────────────────────────────────────

const STATUS_CONFIG: Record<string, { color: string; icon: React.ComponentType<{ className?: string }>; label: string }> = {
  New: { color: "bg-blue-500/10 text-blue-400 border-blue-500/20", icon: Plus, label: "Yeni" },
  Open: { color: "bg-sky-500/10 text-sky-400 border-sky-500/20", icon: ArrowUpRight, label: "Açık" },
  InProgress: { color: "bg-amber-500/10 text-amber-400 border-amber-500/20", icon: Clock, label: "İşlemde" },
  WaitingForInfo: { color: "bg-purple-500/10 text-purple-400 border-purple-500/20", icon: MessageSquare, label: "Bilgi Bekleniyor" },
  Escalated: { color: "bg-red-500/10 text-red-400 border-red-500/20", icon: AlertTriangle, label: "Eskalasyon" },
  Resolved: { color: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20", icon: CheckCircle2, label: "Çözüldü" },
  Closed: { color: "bg-slate-500/10 text-slate-400 border-slate-500/20", icon: XCircle, label: "Kapalı" },
  Cancelled: { color: "bg-gray-500/10 text-gray-400 border-gray-500/20", icon: XCircle, label: "İptal" },
  Reopened: { color: "bg-orange-500/10 text-orange-400 border-orange-500/20", icon: ArrowUpRight, label: "Yeniden Açıldı" },
};

const PRIORITY_CONFIG: Record<string, { color: string; dot: string; label: string }> = {
  Low: { color: "text-slate-400", dot: "bg-slate-400", label: "Düşük" },
  Medium: { color: "text-blue-400", dot: "bg-blue-400", label: "Orta" },
  High: { color: "text-amber-400", dot: "bg-amber-400", label: "Yüksek" },
  Critical: { color: "text-red-400", dot: "bg-red-400", label: "Kritik" },
  Urgent: { color: "text-rose-500", dot: "bg-rose-500", label: "Acil" },
};

const CHANNEL_OPTIONS = [
  { value: "Portal", label: "Portal" },
  { value: "Email", label: "E-posta" },
  { value: "Phone", label: "Telefon" },
  { value: "Chat", label: "Canlı Destek" },
  { value: "Internal", label: "İç Talep" },
];

const TABS: { key: TabKey; label: string; icon: React.ComponentType<{ className?: string }> }[] = [
  { key: "my-requests", label: "Taleplerim", icon: ClipboardList },
  { key: "my-assignments", label: "Üzerimde", icon: UserCheck },
  { key: "queue-pool", label: "Kuyruk Havuzu", icon: Inbox },
];

// ── Component ───────────────────────────────────────────────

export default function TicketsPage() {
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<TabKey>("my-requests");
  const [tickets, setTickets] = useState<TicketData[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [priorityFilter, setPriorityFilter] = useState<string>("");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  // ── Global auth user ──
  const authUser = useAuthStore((s) => s.user);
  const currentUserId = authUser?.id ?? "";
  const [myQueues, setMyQueues] = useState<MyQueueData[]>([]);
  const [selectedQueueFilter, setSelectedQueueFilter] = useState<string>("all");

  // ── Tab counts ──
  const [myRequestsCount, setMyRequestsCount] = useState(0);
  const [myAssignmentsCount, setMyAssignmentsCount] = useState(0);
  const [queuePoolCount, setQueuePoolCount] = useState(0);

  // ── Create form state ──
  const [showCreate, setShowCreate] = useState(false);
  const [departments, setDepartments] = useState<DepartmentOption[]>([]);
  const [categories, setCategories] = useState<CategoryOption[]>([]);
  const [formTitle, setFormTitle] = useState("");
  const [formDescription, setFormDescription] = useState("");
  const [formDepartmentId, setFormDepartmentId] = useState("");
  const [formCategoryId, setFormCategoryId] = useState("");
  const [formPriority, setFormPriority] = useState("Medium");
  const [formChannel, setFormChannel] = useState("Portal");
  const [formApplicationId, setFormApplicationId] = useState("");
  const [formSaving, setFormSaving] = useState(false);
  const [formError, setFormError] = useState("");
  const [applications, setApplications] = useState<ApplicationOption[]>([]);

  // ── Claiming state ──
  const [claimingId, setClaimingId] = useState<string | null>(null);

  // Reset page when auth user changes
  useEffect(() => {
    setPage(1);
    setSelectedQueueFilter("all");
  }, [currentUserId]);

  // ── Fetch my queues ──
  useEffect(() => {
    if (!currentUserId) return;
    fetch(`/api/req/my-queues/${currentUserId}`)
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => {
        const list = Array.isArray(data) ? data : [];
        setMyQueues(list);
        setQueuePoolCount(list.reduce((sum: number, q: MyQueueData) => sum + q.unassignedCount, 0));
      })
      .catch(() => setMyQueues([]));
  }, [currentUserId]);

  // ── Fetch tickets ──
  const fetchTickets = useCallback(async () => {
    if (!currentUserId) return;
    setLoading(true);
    try {
      const params = new URLSearchParams();
      params.set("page", page.toString());
      params.set("pageSize", pageSize.toString());
      if (statusFilter) params.set("status", statusFilter);
      if (priorityFilter) params.set("priority", priorityFilter);

      if (activeTab === "my-requests") {
        params.set("reporterUserId", currentUserId);
      } else if (activeTab === "my-assignments") {
        params.set("assigneeUserId", currentUserId);
      } else if (activeTab === "queue-pool") {
        const queueIds = selectedQueueFilter === "all"
          ? myQueues.map((q) => q.queueId).join(",")
          : selectedQueueFilter;
        if (queueIds) params.set("queueIds", queueIds);
        params.set("unassignedOnly", "true");
      }

      const res = await fetch(`/api/req/tickets?${params}`);
      if (res.ok) {
        const data: TicketListResult = await res.json();
        setTickets(data.items ?? []);
        setTotalCount(data.totalCount ?? 0);
      }
    } catch (err) {
      console.error("Failed to fetch tickets:", err);
    } finally {
      setLoading(false);
    }
  }, [currentUserId, activeTab, page, statusFilter, priorityFilter, myQueues, selectedQueueFilter]);

  useEffect(() => {
    fetchTickets();
  }, [fetchTickets]);

  // ── Fetch tab counts ──
  useEffect(() => {
    if (!currentUserId) return;
    // My requests count
    fetch(`/api/req/tickets?reporterUserId=${currentUserId}&pageSize=1`)
      .then((r) => r.ok ? r.json() : { totalCount: 0 })
      .then((d) => setMyRequestsCount(d.totalCount ?? 0))
      .catch(() => {});
    // My assignments count
    fetch(`/api/req/tickets?assigneeUserId=${currentUserId}&pageSize=1`)
      .then((r) => r.ok ? r.json() : { totalCount: 0 })
      .then((d) => setMyAssignmentsCount(d.totalCount ?? 0))
      .catch(() => {});
  }, [currentUserId, tickets]);

  // ── Fetch departments & categories for form ──
  useEffect(() => {
    if (!showCreate) return;
    fetch("/api/v1/org/departments")
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setDepartments(Array.isArray(data) ? data : []))
      .catch(() => setDepartments([]));
    fetch("/api/req/categories")
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setCategories(Array.isArray(data) ? data : []))
      .catch(() => setCategories([]));
    fetch("/api/pm/applications")
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setApplications(Array.isArray(data) ? data : []))
      .catch(() => setApplications([]));
  }, [showCreate]);

  const filteredCategories = formDepartmentId
    ? categories.filter((c) => extractId(c.departmentId) === formDepartmentId)
    : categories;

  useEffect(() => { setFormCategoryId(""); }, [formDepartmentId]);

  // ── Create ticket ──
  const handleCreate = async () => {
    if (!formTitle.trim() || !formDepartmentId || !formCategoryId) {
      setFormError("Başlık, departman ve kategori zorunludur.");
      return;
    }
    setFormSaving(true);
    setFormError("");
    try {
      const res = await fetch("/api/req/tickets", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          title: formTitle.trim(),
          categoryId: formCategoryId,
          departmentId: formDepartmentId,
          description: formDescription.trim() || null,
          priority: ({ Low: 0, Medium: 1, High: 2, Critical: 3, Urgent: 4 } as Record<string, number>)[formPriority] ?? 1,
          channel: ({ Portal: 0, Email: 1, Phone: 2, Chat: 3, Internal: 4 } as Record<string, number>)[formChannel] ?? 0,
          reporterUserId: currentUserId,
          configurationItemId: formApplicationId || null,
        }),
      });
      if (res.ok) {
        setShowCreate(false);
        resetForm();
        await fetchTickets();
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
    setFormDepartmentId("");
    setFormCategoryId("");
    setFormPriority("Medium");
    setFormChannel("Portal");
    setFormApplicationId("");
    setFormError("");
  };

  // ── Claim ticket ──
  const handleClaim = async (ticketId: string) => {
    if (!currentUserId) return;
    setClaimingId(ticketId);
    try {
      const res = await fetch(`/api/req/tickets/${ticketId}/claim`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ claimerUserId: currentUserId }),
      });
      if (res.ok) {
        await fetchTickets();
      } else {
        const errText = await res.text();
        alert(`Claim hatası: ${errText.substring(0, 200)}`);
      }
    } catch {
      alert("Bağlantı hatası");
    } finally {
      setClaimingId(null);
    }
  };

  // ── Search filter ──
  const filteredTickets = searchTerm
    ? tickets.filter(
        (t) =>
          t.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
          t.number.toLowerCase().includes(searchTerm.toLowerCase())
      )
    : tickets;

  const isSlaWarning = (deadline?: string) => {
    if (!deadline) return false;
    const diff = new Date(deadline).getTime() - Date.now();
    return diff > 0 && diff < 2 * 60 * 60 * 1000;
  };

  const tabCounts: Record<TabKey, number> = {
    "my-requests": myRequestsCount,
    "my-assignments": myAssignmentsCount,
    "queue-pool": queuePoolCount,
  };

  const currentUserRole = (queueId: string) => {
    const q = myQueues.find((q) => q.queueId === queueId);
    return q?.role ?? "Member";
  };

  return (
    <div className="space-y-5">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">Talepler</h1>
            <p className="text-sm text-[var(--color-text-muted)] mt-0.5">
              {totalCount} talep
            </p>
          </div>
        </div>
        <button
          onClick={() => setShowCreate(true)}
          className={cn(
            "flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium",
            "bg-indigo-600 text-white hover:bg-indigo-700 active:bg-indigo-800",
            "shadow-lg shadow-indigo-500/20 transition-all duration-200"
          )}
        >
          <Plus className="w-4 h-4" />
          Yeni Talep
        </button>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-1 p-1 rounded-xl bg-[var(--color-bg)]/80 border border-[var(--color-border)] w-fit">
        {TABS.map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.key;
          const count = tabCounts[tab.key];
          // Hide queue tab if user has no queues
          if (tab.key === "queue-pool" && myQueues.length === 0) return null;
          return (
            <button
              key={tab.key}
              onClick={() => { setActiveTab(tab.key); setPage(1); setStatusFilter(""); setPriorityFilter(""); }}
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
                <span className={cn(
                  "inline-flex items-center justify-center min-w-[20px] h-5 px-1.5 rounded-full text-[11px] font-semibold",
                  isActive
                    ? "bg-indigo-500/20 text-indigo-400"
                    : "bg-[var(--color-border)] text-[var(--color-text-muted)]"
                )}>
                  {count}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {/* Queue selector (only for queue-pool tab) */}
      {activeTab === "queue-pool" && myQueues.length > 0 && (
        <div className="flex items-center gap-2 flex-wrap">
          <button
            onClick={() => { setSelectedQueueFilter("all"); setPage(1); }}
            className={cn(
              "px-3 py-1.5 rounded-lg text-xs font-medium border transition-all",
              selectedQueueFilter === "all"
                ? "bg-indigo-500/10 border-indigo-500/30 text-indigo-400"
                : "border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-card-bg)]"
            )}
          >
            Tümü ({myQueues.reduce((s, q) => s + q.unassignedCount, 0)})
          </button>
          {myQueues.map((q) => (
            <button
              key={q.queueId}
              onClick={() => { setSelectedQueueFilter(q.queueId); setPage(1); }}
              className={cn(
                "px-3 py-1.5 rounded-lg text-xs font-medium border transition-all",
                selectedQueueFilter === q.queueId
                  ? "bg-cyan-500/10 border-cyan-500/30 text-cyan-400"
                  : "border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-card-bg)]"
              )}
            >
              <span className="flex items-center gap-1.5">
                {q.name}
                <span className="opacity-60">({q.unassignedCount})</span>
                <span className={cn(
                  "text-[10px] px-1 py-0.5 rounded font-mono uppercase",
                  q.role === "Lead" ? "bg-purple-500/15 text-purple-400" :
                  q.role === "Dispatcher" ? "bg-amber-500/15 text-amber-400" :
                  "bg-slate-500/15 text-slate-400"
                )}>{q.role}</span>
              </span>
            </button>
          ))}
        </div>
      )}

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
          <input
            type="text"
            placeholder="Talep ara (numara veya başlık)..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className={cn(
              "w-full pl-10 pr-4 py-2 rounded-lg text-sm",
              "bg-[var(--color-input-bg)] border border-[var(--color-border)]",
              "text-[var(--color-text)] placeholder:text-[var(--color-text-muted)]",
              "focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
            )}
          />
        </div>
        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]"
        >
          <option value="">Tüm Durumlar</option>
          {Object.entries(STATUS_CONFIG).map(([key, cfg]) => (
            <option key={key} value={key}>{cfg.label}</option>
          ))}
        </select>
        <select
          value={priorityFilter}
          onChange={(e) => { setPriorityFilter(e.target.value); setPage(1); }}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]"
        >
          <option value="">Tüm Öncelikler</option>
          {Object.entries(PRIORITY_CONFIG).map(([key, cfg]) => (
            <option key={key} value={key}>{cfg.label}</option>
          ))}
        </select>
      </div>

      {/* Ticket Table */}
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="w-6 h-6 text-indigo-400 animate-spin" />
          </div>
        ) : filteredTickets.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-[var(--color-text-muted)]">
            <Ticket className="w-12 h-12 opacity-30 mb-3" />
            <p className="text-sm">
              {activeTab === "my-requests" && "Henüz talep oluşturmadınız"}
              {activeTab === "my-assignments" && "Üzerinize atanmış talep yok"}
              {activeTab === "queue-pool" && "Kuyruk havuzunda bekleyen talep yok"}
            </p>
            {activeTab === "my-requests" && (
              <button
                onClick={() => setShowCreate(true)}
                className="mt-3 text-xs text-indigo-400 hover:text-indigo-300 transition-colors"
              >
                + Yeni Talep Oluştur
              </button>
            )}
          </div>
        ) : (
          <table className="w-full">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg)]/50">
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Numara</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Başlık</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Durum</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Öncelik</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  {activeTab === "queue-pool" ? "Kuyruk" : "Departman"}
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">SLA</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Tarih</th>
                {activeTab === "queue-pool" && (
                  <th className="text-right px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Aksiyon</th>
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {filteredTickets.map((ticket) => {
                const statusCfg = STATUS_CONFIG[ticket.status] || STATUS_CONFIG.New;
                const priorityCfg = PRIORITY_CONFIG[ticket.priority] || PRIORITY_CONFIG.Medium;
                const StatusIcon = statusCfg.icon;
                const queueRole = ticket.serviceQueue
                  ? currentUserRole(ticket.serviceQueue.code === ticket.serviceQueue.code ? (myQueues.find((q) => q.name === ticket.serviceQueue!.name)?.queueId ?? "") : "")
                  : "Member";

                return (
                  <tr
                    key={extractId(ticket.id)}
                    onClick={() => router.push(`/dashboard/tickets/${extractId(ticket.id)}`)}
                    className="hover:bg-[var(--color-border)]/30 transition-colors cursor-pointer group"
                  >
                    <td className="px-4 py-3">
                      <span className="text-sm font-mono font-medium text-indigo-400 group-hover:text-indigo-300 transition-colors">
                        {ticket.number}
                      </span>
                    </td>
                    <td className="px-4 py-3 max-w-[280px]">
                      <span className="text-sm text-[var(--color-text)] line-clamp-1">
                        {ticket.title}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span className={cn(
                        "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border",
                        statusCfg.color
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
                      {activeTab === "queue-pool" ? (
                        ticket.serviceQueue ? (
                          <span className="inline-flex items-center gap-1 text-xs text-cyan-400 bg-cyan-500/10 px-2 py-0.5 rounded-full">
                            {ticket.serviceQueue.name}
                          </span>
                        ) : <span className="text-xs text-[var(--color-text-muted)]">—</span>
                      ) : (
                        <span className="text-sm text-[var(--color-text-muted)]">
                          {ticket.department?.name || "—"}
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      {ticket.slaResolutionBreached ? (
                        <span className="inline-flex items-center gap-1 text-xs text-red-400">
                          <AlertTriangle className="w-3 h-3" /> İhlal
                        </span>
                      ) : isSlaWarning(ticket.slaResolutionDeadline) ? (
                        <span className="inline-flex items-center gap-1 text-xs text-amber-400 animate-pulse">
                          <Clock className="w-3 h-3" /> {slaTimeLeft(ticket.slaResolutionDeadline)}
                        </span>
                      ) : ticket.slaResolutionDeadline ? (
                        <span className="inline-flex items-center gap-1 text-xs text-emerald-400">
                          <CheckCircle2 className="w-3 h-3" /> {slaTimeLeft(ticket.slaResolutionDeadline)}
                        </span>
                      ) : (
                        <span className="text-xs text-[var(--color-text-muted)]">—</span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <span className="text-xs text-[var(--color-text-muted)]">
                        {timeAgo(ticket.createdAt)}
                      </span>
                    </td>
                    {activeTab === "queue-pool" && (
                      <td className="px-4 py-3 text-right" onClick={(e) => e.stopPropagation()}>
                        <div className="flex items-center gap-1.5 justify-end">
                          <button
                            onClick={() => handleClaim(extractId(ticket.id))}
                            disabled={claimingId === extractId(ticket.id)}
                            className={cn(
                              "inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-medium",
                              "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20",
                              "hover:bg-emerald-500/20 transition-all",
                              "disabled:opacity-40 disabled:cursor-not-allowed"
                            )}
                          >
                            {claimingId === ticket.id ? (
                              <Loader2 className="w-3 h-3 animate-spin" />
                            ) : (
                              <Hand className="w-3 h-3" />
                            )}
                            Üzerime Al
                          </button>
                        </div>
                      </td>
                    )}
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

      {/* ═══════════ Create Ticket Panel ═══════════ */}
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
                <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center shadow-lg shadow-indigo-500/30">
                  <Send className="w-5 h-5 text-white" />
                </div>
                <div>
                  <h2 className="text-lg font-semibold text-[var(--color-text)]">Yeni Talep</h2>
                  <p className="text-xs text-[var(--color-text-muted)]">Talep bilgilerini girin</p>
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
                  placeholder="Talebinizi kısaca özetleyin..."
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">
                    Departman <span className="text-red-400">*</span>
                  </label>
                  <select
                    value={formDepartmentId}
                    onChange={(e) => setFormDepartmentId(e.target.value)}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                  >
                    <option value="">Departman seçin</option>
                    {departments.map((d) => (
                      <option key={extractId(d.id)} value={extractId(d.id)}>{d.name}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">
                    Kategori <span className="text-red-400">*</span>
                  </label>
                  <select
                    value={formCategoryId}
                    onChange={(e) => setFormCategoryId(e.target.value)}
                    disabled={!formDepartmentId}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40 disabled:opacity-40"
                  >
                    <option value="">{formDepartmentId ? "Kategori seçin" : "Önce departman seçin"}</option>
                    {filteredCategories.map((c) => (
                      <option key={extractId(c.id)} value={extractId(c.id)}>{c.name}</option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Öncelik</label>
                  <select
                    value={formPriority}
                    onChange={(e) => setFormPriority(e.target.value)}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                  >
                    {Object.entries(PRIORITY_CONFIG).map(([key, cfg]) => (
                      <option key={key} value={key}>{cfg.label}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Kanal</label>
                  <select
                    value={formChannel}
                    onChange={(e) => setFormChannel(e.target.value)}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                  >
                    {CHANNEL_OPTIONS.map((ch) => (
                      <option key={ch.value} value={ch.value}>{ch.label}</option>
                    ))}
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Açıklama</label>
                <textarea
                  value={formDescription}
                  onChange={(e) => setFormDescription(e.target.value)}
                  rows={5}
                  placeholder="Talebinizi detaylı açıklayın..."
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] resize-y focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                />
              </div>

              {/* İlgili Uygulama (opsiyonel) */}
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">
                  <div className="flex items-center gap-1.5">
                    <Monitor className="w-3.5 h-3.5 text-[var(--color-text-muted)]" />
                    İlgili Uygulama
                    <span className="text-[10px] text-[var(--color-text-muted)] font-normal">(opsiyonel)</span>
                  </div>
                </label>
                <select
                  value={formApplicationId}
                  onChange={(e) => setFormApplicationId(e.target.value)}
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                >
                  <option value="">Uygulama seçin (opsiyonel)</option>
                  {applications.map((a) => (
                    <option key={a.id} value={a.id}>{a.name} ({a.code})</option>
                  ))}
                </select>
              </div>

              <div className="flex items-start gap-2 p-3 rounded-lg bg-indigo-500/5 border border-indigo-500/10">
                <Building2 className="w-4 h-4 text-indigo-400 mt-0.5 shrink-0" />
                <p className="text-xs text-[var(--color-text-muted)]">
                Talep, kategoriye bağlı iş akışı ile otomatik olarak doğru kuyruğa yönlendirilecektir.
                </p>
              </div>

              {formError && (
                <div className="flex items-start gap-2 p-3 rounded-lg bg-red-500/10 border border-red-500/20">
                  <AlertTriangle className="w-4 h-4 text-red-400 mt-0.5 shrink-0" />
                  <p className="text-xs text-red-400">{formError}</p>
                </div>
              )}
            </div>

            {/* Panel Footer */}
            <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-[var(--color-border)]">
              <button
                onClick={() => { setShowCreate(false); resetForm(); }}
                className="px-4 py-2.5 rounded-lg text-sm border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)] transition-colors"
              >
                İptal
              </button>
              <button
                onClick={handleCreate}
                disabled={formSaving || !formTitle.trim() || !formDepartmentId || !formCategoryId}
                className={cn(
                  "flex items-center gap-2 px-5 py-2.5 rounded-lg text-sm font-medium",
                  "bg-indigo-600 text-white hover:bg-indigo-700",
                  "shadow-md shadow-indigo-500/20",
                  "disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                )}
              >
                {formSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
                Talep Oluştur
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
