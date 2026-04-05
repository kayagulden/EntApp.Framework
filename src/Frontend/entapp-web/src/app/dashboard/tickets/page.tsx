"use client";

import { useState, useEffect } from "react";
import {
  Ticket,
  Plus,
  Search,
  Filter,
  Clock,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  ArrowUpRight,
  ChevronDown,
  Loader2,
  Building2,
  User,
  MessageSquare,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface TicketData {
  id: string;
  number: string;
  title: string;
  description?: string;
  status: string;
  priority: string;
  channel: string;
  slaResponseDeadline?: string;
  slaResolutionDeadline?: string;
  slaResponseBreached: boolean;
  slaResolutionBreached: boolean;
  assigneeUserId?: string;
  reporterUserId: string;
  category?: { name: string };
  department?: { name: string };
  createdAt: string;
  resolvedAt?: string;
}

interface TicketListResult {
  items: TicketData[];
  totalCount: number;
}

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

const PRIORITY_CONFIG: Record<string, { color: string; label: string }> = {
  Low: { color: "text-slate-400", label: "Düşük" },
  Medium: { color: "text-blue-400", label: "Orta" },
  High: { color: "text-amber-400", label: "Yüksek" },
  Critical: { color: "text-red-400", label: "Kritik" },
  Urgent: { color: "text-rose-500", label: "Acil" },
};

export default function TicketsPage() {
  const [tickets, setTickets] = useState<TicketData[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [priorityFilter, setPriorityFilter] = useState<string>("");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const fetchTickets = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      params.set("page", page.toString());
      params.set("pageSize", pageSize.toString());
      if (statusFilter) params.set("status", statusFilter);
      if (priorityFilter) params.set("priority", priorityFilter);

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
  };

  useEffect(() => {
    fetchTickets();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, statusFilter, priorityFilter]);

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
    return diff > 0 && diff < 2 * 60 * 60 * 1000; // 2 saat
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">
            Talepler
          </h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">
            {totalCount} talep bulundu
          </p>
        </div>
        <button
          className={cn(
            "flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium",
            "bg-indigo-600 text-white",
            "hover:bg-indigo-700 active:bg-indigo-800",
            "shadow-lg shadow-indigo-500/20",
            "transition-all duration-200"
          )}
        >
          <Plus className="w-4 h-4" />
          Yeni Talep
        </button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: "Açık", value: tickets.filter((t) => ["New", "Open", "InProgress"].includes(t.status)).length, color: "text-blue-400", bg: "bg-blue-500/10" },
          { label: "SLA Risk", value: tickets.filter((t) => t.slaResolutionBreached || isSlaWarning(t.slaResolutionDeadline)).length, color: "text-red-400", bg: "bg-red-500/10" },
          { label: "Çözülen", value: tickets.filter((t) => t.status === "Resolved").length, color: "text-emerald-400", bg: "bg-emerald-500/10" },
          { label: "Toplam", value: totalCount, color: "text-slate-300", bg: "bg-slate-500/10" },
        ].map((stat) => (
          <div
            key={stat.label}
            className={cn(
              "p-4 rounded-xl border border-[var(--color-border)]",
              "bg-[var(--color-card-bg)]"
            )}
          >
            <p className="text-xs text-[var(--color-text-muted)] uppercase tracking-wider">
              {stat.label}
            </p>
            <p className={cn("text-2xl font-bold mt-1", stat.color)}>
              {stat.value}
            </p>
          </div>
        ))}
      </div>

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
          className={cn(
            "px-3 py-2 rounded-lg text-sm",
            "bg-[var(--color-input-bg)] border border-[var(--color-border)]",
            "text-[var(--color-text)]"
          )}
        >
          <option value="">Tüm Durumlar</option>
          {Object.entries(STATUS_CONFIG).map(([key, cfg]) => (
            <option key={key} value={key}>{cfg.label}</option>
          ))}
        </select>

        <select
          value={priorityFilter}
          onChange={(e) => { setPriorityFilter(e.target.value); setPage(1); }}
          className={cn(
            "px-3 py-2 rounded-lg text-sm",
            "bg-[var(--color-input-bg)] border border-[var(--color-border)]",
            "text-[var(--color-text)]"
          )}
        >
          <option value="">Tüm Öncelikler</option>
          {Object.entries(PRIORITY_CONFIG).map(([key, cfg]) => (
            <option key={key} value={key}>{cfg.label}</option>
          ))}
        </select>
      </div>

      {/* Table */}
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="w-6 h-6 text-indigo-400 animate-spin" />
          </div>
        ) : filteredTickets.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-[var(--color-text-muted)]">
            <Ticket className="w-12 h-12 opacity-30 mb-3" />
            <p className="text-sm">Henüz talep bulunmuyor</p>
            <p className="text-xs mt-1 opacity-60">Yeni bir talep oluşturun</p>
          </div>
        ) : (
          <table className="w-full">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg)]/50">
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Numara</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Başlık</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Durum</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Öncelik</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Departman</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Kategori</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">SLA</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Tarih</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {filteredTickets.map((ticket) => {
                const statusCfg = STATUS_CONFIG[ticket.status] || STATUS_CONFIG.New;
                const priorityCfg = PRIORITY_CONFIG[ticket.priority] || PRIORITY_CONFIG.Medium;
                const StatusIcon = statusCfg.icon;

                return (
                  <tr
                    key={ticket.id}
                    className="hover:bg-[var(--color-border)]/30 transition-colors cursor-pointer"
                  >
                    <td className="px-4 py-3">
                      <span className="text-sm font-mono font-medium text-indigo-400">
                        {ticket.number}
                      </span>
                    </td>
                    <td className="px-4 py-3">
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
                      <span className={cn("text-sm font-medium", priorityCfg.color)}>
                        {priorityCfg.label}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span className="text-sm text-[var(--color-text-muted)]">
                        {ticket.department?.name || "—"}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span className="text-sm text-[var(--color-text-muted)]">
                        {ticket.category?.name || "—"}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      {ticket.slaResolutionBreached ? (
                        <span className="inline-flex items-center gap-1 text-xs text-red-400">
                          <AlertTriangle className="w-3 h-3" /> İhlal
                        </span>
                      ) : isSlaWarning(ticket.slaResolutionDeadline) ? (
                        <span className="inline-flex items-center gap-1 text-xs text-amber-400">
                          <Clock className="w-3 h-3" /> Risk
                        </span>
                      ) : ticket.slaResolutionDeadline ? (
                        <span className="inline-flex items-center gap-1 text-xs text-emerald-400">
                          <CheckCircle2 className="w-3 h-3" /> Normal
                        </span>
                      ) : (
                        <span className="text-xs text-[var(--color-text-muted)]">—</span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <span className="text-xs text-[var(--color-text-muted)]">
                        {new Date(ticket.createdAt).toLocaleDateString("tr-TR")}
                      </span>
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
    </div>
  );
}
