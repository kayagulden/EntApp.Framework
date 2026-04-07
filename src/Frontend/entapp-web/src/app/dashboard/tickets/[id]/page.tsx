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
  MessageSquare,
  Send,
  Tag,
  Inbox,
  GitBranch,
  Calendar,
  Hand,
  UserCheck,
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
  category?: { name: string };
  department?: { name: string };
  serviceQueue?: { name: string; code: string };
  comments?: CommentData[];
  statusHistory?: StatusHistoryData[];
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

// ── Config ──────────────────────────────────────────────────

const STATUS_CONFIG: Record<string, { color: string; bg: string; icon: React.ComponentType<{ className?: string }>; label: string }> = {
  New: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", icon: Plus, label: "Yeni" },
  Open: { color: "text-sky-400", bg: "bg-sky-500/10 border-sky-500/20", icon: ArrowUpRight, label: "Açık" },
  InProgress: { color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", icon: Clock, label: "İşlemde" },
  WaitingForInfo: { color: "text-purple-400", bg: "bg-purple-500/10 border-purple-500/20", icon: MessageSquare, label: "Bilgi Bekleniyor" },
  Escalated: { color: "text-red-400", bg: "bg-red-500/10 border-red-500/20", icon: AlertTriangle, label: "Eskalasyon" },
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

const CHANNEL_LABELS: Record<string, string> = {
  Portal: "Portal", Email: "E-posta", Phone: "Telefon",
  Chat: "Canlı Destek", Internal: "İç Talep",
};

const ROUTING_LABELS: Record<string, string> = {
  Manual: "Manuel", CategoryDefault: "Kategori", DepartmentDefault: "Departman",
  WorkflowRule: "İş Akışı", Unrouted: "Atanmamış",
};

const STATUS_OPTIONS = ["New", "Open", "InProgress", "WaitingForInfo", "Escalated", "Resolved", "Closed"];

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

  useEffect(() => {
    if (!ticketId) return;
    setLoading(true);
    fetch(`/api/req/tickets/${ticketId}`)
      .then((r) => (r.ok ? r.json() : null))
      .then((data) => { setTicket(data); setNewStatus(data?.status ?? ""); })
      .catch(() => setTicket(null))
      .finally(() => setLoading(false));
  }, [ticketId]);

  const handleComment = async () => {
    if (!commentText.trim()) return;
    setCommentSending(true);
    try {
      const res = await fetch(`/api/req/tickets/${ticketId}/comments`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ content: commentText.trim(), isInternal: false }),
      });
      if (res.ok) {
        setCommentText("");
        // Refresh
        const r = await fetch(`/api/req/tickets/${ticketId}`);
        if (r.ok) setTicket(await r.json());
      }
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
      const r = await fetch(`/api/req/tickets/${ticketId}`);
      if (r.ok) { const d = await r.json(); setTicket(d); setNewStatus(d.status); }
    } catch {
    } finally {
      setStatusChanging(false);
    }
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

          {/* Details Card */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3">Detaylar</h3>
            <div className="space-y-3">
              {[
                { icon: Building2, label: "Departman", value: ticket.department?.name ?? "—" },
                { icon: Tag, label: "Kategori", value: ticket.category?.name ?? "—" },
                { icon: Inbox, label: "Kuyruk", value: ticket.serviceQueue?.name ?? "Atanmamış" },
                { icon: GitBranch, label: "Yönlendirme", value: ROUTING_LABELS[ticket.routingSource ?? ""] ?? ticket.routingSource ?? "—" },
                { icon: MessageSquare, label: "Kanal", value: CHANNEL_LABELS[ticket.channel] ?? ticket.channel },
                { icon: Calendar, label: "Oluşturma", value: formatDateTime(ticket.createdAt) },
              ].map((item) => (
                <div key={item.label} className="flex items-center gap-2.5">
                  <item.icon className="w-3.5 h-3.5 text-[var(--color-text-muted)] shrink-0" />
                  <span className="text-xs text-[var(--color-text-muted)] w-20 shrink-0">{item.label}</span>
                  <span className="text-xs text-[var(--color-text)] truncate">{item.value}</span>
                </div>
              ))}
            </div>
          </div>

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
          </div>
        </div>
      </div>
    </div>
  );
}
