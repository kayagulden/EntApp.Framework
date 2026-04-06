"use client";

import { useState, useEffect, useCallback } from "react";
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
  X,
  Save,
  Send,
} from "lucide-react";
import { cn } from "@/lib/utils";

// ── Interfaces ──────────────────────────────────────────────

interface TicketData {
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
  id: { value: string } | string;
  name: string;
  code: string;
}

interface CategoryOption {
  id: { value: string } | string;
  name: string;
  code: string;
  departmentId: { value: string } | string;
}

// ── Helpers ─────────────────────────────────────────────────

function extractId(id: { value: string } | string | undefined): string {
  if (!id) return "";
  if (typeof id === "string") return id;
  return id.value;
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

const PRIORITY_CONFIG: Record<string, { color: string; label: string }> = {
  Low: { color: "text-slate-400", label: "Düşük" },
  Medium: { color: "text-blue-400", label: "Orta" },
  High: { color: "text-amber-400", label: "Yüksek" },
  Critical: { color: "text-red-400", label: "Kritik" },
  Urgent: { color: "text-rose-500", label: "Acil" },
};

const CHANNEL_OPTIONS = [
  { value: "Portal", label: "Portal" },
  { value: "Email", label: "E-posta" },
  { value: "Phone", label: "Telefon" },
  { value: "Chat", label: "Canlı Destek" },
  { value: "Internal", label: "İç Talep" },
];

const ROUTING_LABELS: Record<string, string> = {
  Manual: "Manuel",
  CategoryDefault: "Kategori",
  DepartmentDefault: "Departman",
  WorkflowRule: "İş Akışı",
  Unrouted: "Atanmamış",
};

// ── Component ───────────────────────────────────────────────

export default function TicketsPage() {
  const [tickets, setTickets] = useState<TicketData[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [priorityFilter, setPriorityFilter] = useState<string>("");
  const [page, setPage] = useState(1);
  const pageSize = 20;

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
  const [formSaving, setFormSaving] = useState(false);
  const [formError, setFormError] = useState("");

  // ── Fetch tickets ──
  const fetchTickets = useCallback(async () => {
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
  }, [page, statusFilter, priorityFilter]);

  useEffect(() => {
    fetchTickets();
  }, [fetchTickets]);

  // ── Fetch departments & categories for form ──
  useEffect(() => {
    if (!showCreate) return;
    fetch("/api/req/departments")
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setDepartments(Array.isArray(data) ? data : []))
      .catch(() => setDepartments([]));

    fetch("/api/req/categories")
      .then((r) => (r.ok ? r.json() : []))
      .then((data) => setCategories(Array.isArray(data) ? data : []))
      .catch(() => setCategories([]));
  }, [showCreate]);

  // ── Filtered categories by selected department ──
  const filteredCategories = formDepartmentId
    ? categories.filter((c) => extractId(c.departmentId) === formDepartmentId)
    : categories;

  // Reset category when department changes
  useEffect(() => {
    setFormCategoryId("");
  }, [formDepartmentId]);

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
          priority: formPriority,
          channel: formChannel,
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
    } catch (err) {
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
    setFormError("");
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
          onClick={() => setShowCreate(true)}
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
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Kuyruk</th>
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
                    key={ticket.number}
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
                      {ticket.serviceQueue ? (
                        <span className="inline-flex items-center gap-1 text-xs text-cyan-400 bg-cyan-500/10 px-2 py-0.5 rounded-full">
                          {ticket.serviceQueue.name}
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-xs text-amber-400 bg-amber-500/10 px-2 py-0.5 rounded-full">
                          Atanmamış
                        </span>
                      )}
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
              {/* Title */}
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

              {/* Department + Category row */}
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
                      <option key={extractId(d.id)} value={extractId(d.id)}>
                        {d.name}
                      </option>
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
                    <option value="">
                      {formDepartmentId ? "Kategori seçin" : "Önce departman seçin"}
                    </option>
                    {filteredCategories.map((c) => (
                      <option key={extractId(c.id)} value={extractId(c.id)}>
                        {c.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Priority + Channel row */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">
                    Öncelik
                  </label>
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
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">
                    Kanal
                  </label>
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

              {/* Description */}
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">
                  Açıklama
                </label>
                <textarea
                  value={formDescription}
                  onChange={(e) => setFormDescription(e.target.value)}
                  rows={5}
                  placeholder="Talebinizi detaylı açıklayın..."
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] resize-y focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                />
              </div>

              {/* Routing info hint */}
              <div className="flex items-start gap-2 p-3 rounded-lg bg-indigo-500/5 border border-indigo-500/10">
                <Building2 className="w-4 h-4 text-indigo-400 mt-0.5 shrink-0" />
                <p className="text-xs text-[var(--color-text-muted)]">
                  Talep, seçilen kategorinin varsayılan kuyruğuna otomatik yönlendirilecektir.
                  Kuyruk atanmamışsa departman varsayılan kuyruğu kullanılır.
                </p>
              </div>

              {/* Error */}
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
                  "disabled:opacity-40 disabled:cursor-not-allowed",
                  "transition-all"
                )}
              >
                {formSaving ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <Send className="w-4 h-4" />
                )}
                Talep Oluştur
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
