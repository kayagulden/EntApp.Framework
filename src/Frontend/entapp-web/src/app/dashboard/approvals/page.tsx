"use client";

import { useState, useEffect } from "react";
import {
  Clock,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  ChevronRight,
  CalendarClock,
  User,
  Loader2,
  Inbox,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface PendingStep {
  id: string;
  instanceId: string;
  stepOrder: number;
  stepName: string;
  assignedUserId: string | null;
  dueDate: string | null;
  createdAt: string;
}

// TODO: Gerçek userId auth context'ten alınacak (Keycloak entegrasyonu sonrası)
const PLACEHOLDER_USER_ID = "00000000-0000-0000-0000-000000000001";

export default function ApprovalsPage() {
  const [steps, setSteps] = useState<PendingStep[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  const fetchPending = async () => {
    try {
      setLoading(true);
      const res = await fetch(
        `/api/workflows/pending/${PLACEHOLDER_USER_ID}`
      );
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setSteps(data);
      setError(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Beklenmeyen hata");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPending();
  }, []);

  const handleAction = async (
    instanceId: string,
    stepId: string,
    action: "approve" | "reject"
  ) => {
    setActionLoading(stepId);
    try {
      const res = await fetch(`/api/workflows/${instanceId}/${action}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          stepId,
          userId: PLACEHOLDER_USER_ID,
          comment: action === "approve" ? "Onaylandı" : "Reddedildi",
        }),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      await fetchPending(); // Listeyi yenile
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "İşlem başarısız");
    } finally {
      setActionLoading(null);
    }
  };

  const formatDate = (dateStr: string | null) => {
    if (!dateStr) return "—";
    const d = new Date(dateStr);
    return d.toLocaleDateString("tr-TR", {
      day: "2-digit",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const isDueSoon = (dueDate: string | null) => {
    if (!dueDate) return false;
    const diff = new Date(dueDate).getTime() - Date.now();
    return diff > 0 && diff < 24 * 60 * 60 * 1000; // 24 saat içinde
  };

  const isOverdue = (dueDate: string | null) => {
    if (!dueDate) return false;
    return new Date(dueDate).getTime() < Date.now();
  };

  return (
    <div className="space-y-6">
      {/* ── Header ─────────────────────────────── */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">
            Bekleyen Onaylarım
          </h1>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">
            Size atanmış onay bekleyen workflow adımları
          </p>
        </div>
        <button
          onClick={fetchPending}
          disabled={loading}
          className={cn(
            "flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium",
            "bg-[var(--color-surface)] border border-[var(--color-border)]",
            "hover:bg-[var(--color-surface-hover)] transition-colors",
            "text-[var(--color-text)]"
          )}
        >
          {loading ? (
            <Loader2 className="w-4 h-4 animate-spin" />
          ) : (
            <Clock className="w-4 h-4" />
          )}
          Yenile
        </button>
      </div>

      {/* ── Error ──────────────────────────────── */}
      {error && (
        <div className="flex items-center gap-3 p-4 rounded-xl bg-red-500/10 border border-red-500/20">
          <AlertTriangle className="w-5 h-5 text-red-500 shrink-0" />
          <p className="text-sm text-red-500">{error}</p>
        </div>
      )}

      {/* ── Loading ────────────────────────────── */}
      {loading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 className="w-8 h-8 animate-spin text-[var(--color-text-muted)]" />
        </div>
      )}

      {/* ── Empty State ────────────────────────── */}
      {!loading && steps.length === 0 && !error && (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <div className="w-16 h-16 rounded-2xl bg-emerald-500/10 flex items-center justify-center mb-4">
            <Inbox className="w-8 h-8 text-emerald-500" />
          </div>
          <h3 className="text-lg font-semibold text-[var(--color-text)]">
            Bekleyen onay yok
          </h3>
          <p className="mt-1 text-sm text-[var(--color-text-muted)] max-w-md">
            Şu anda size atanmış herhangi bir onay bekleyen workflow adımı
            bulunmuyor.
          </p>
        </div>
      )}

      {/* ── Pending Steps List ─────────────────── */}
      {!loading && steps.length > 0 && (
        <div className="space-y-3">
          {steps.map((step, index) => (
            <div
              key={step.id}
              className={cn(
                "rounded-xl p-5",
                "bg-[var(--color-surface)] border",
                isOverdue(step.dueDate)
                  ? "border-red-500/30 bg-red-500/5"
                  : isDueSoon(step.dueDate)
                    ? "border-amber-500/30 bg-amber-500/5"
                    : "border-[var(--color-border)]",
                "hover:shadow-lg transition-all duration-300 animate-fade-in"
              )}
              style={{ animationDelay: `${index * 50}ms` }}
            >
              <div className="flex items-start justify-between gap-4">
                {/* Sol: Bilgiler */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-2">
                    <span className="inline-flex items-center justify-center w-7 h-7 rounded-lg bg-indigo-500/10 text-indigo-500 text-xs font-bold">
                      {step.stepOrder}
                    </span>
                    <h3 className="text-base font-semibold text-[var(--color-text)] truncate">
                      {step.stepName}
                    </h3>
                    {isOverdue(step.dueDate) && (
                      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-red-500/10 text-red-500">
                        <AlertTriangle className="w-3 h-3" />
                        GECİKMİŞ
                      </span>
                    )}
                    {isDueSoon(step.dueDate) && !isOverdue(step.dueDate) && (
                      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-amber-500/10 text-amber-500">
                        <Clock className="w-3 h-3" />
                        YAKINDA
                      </span>
                    )}
                  </div>

                  <div className="flex items-center gap-4 text-xs text-[var(--color-text-muted)]">
                    <span className="flex items-center gap-1">
                      <ChevronRight className="w-3 h-3" />
                      Workflow: {step.instanceId.slice(0, 8)}...
                    </span>
                    <span className="flex items-center gap-1">
                      <CalendarClock className="w-3 h-3" />
                      Oluşturma: {formatDate(step.createdAt)}
                    </span>
                    {step.dueDate && (
                      <span
                        className={cn(
                          "flex items-center gap-1",
                          isOverdue(step.dueDate)
                            ? "text-red-500 font-medium"
                            : ""
                        )}
                      >
                        <Clock className="w-3 h-3" />
                        Vade: {formatDate(step.dueDate)}
                      </span>
                    )}
                    {step.assignedUserId && (
                      <span className="flex items-center gap-1">
                        <User className="w-3 h-3" />
                        {step.assignedUserId.slice(0, 8)}...
                      </span>
                    )}
                  </div>
                </div>

                {/* Sağ: Aksiyon Butonları */}
                <div className="flex items-center gap-2 shrink-0">
                  <button
                    onClick={() =>
                      handleAction(step.instanceId, step.id, "reject")
                    }
                    disabled={actionLoading === step.id}
                    className={cn(
                      "flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium",
                      "border border-red-500/30 text-red-500",
                      "hover:bg-red-500/10 transition-colors",
                      "disabled:opacity-50 disabled:cursor-not-allowed"
                    )}
                  >
                    {actionLoading === step.id ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <XCircle className="w-4 h-4" />
                    )}
                    Reddet
                  </button>
                  <button
                    onClick={() =>
                      handleAction(step.instanceId, step.id, "approve")
                    }
                    disabled={actionLoading === step.id}
                    className={cn(
                      "flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-medium",
                      "bg-emerald-500 text-white",
                      "hover:bg-emerald-600 transition-colors",
                      "disabled:opacity-50 disabled:cursor-not-allowed",
                      "shadow-sm"
                    )}
                  >
                    {actionLoading === step.id ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <CheckCircle2 className="w-4 h-4" />
                    )}
                    Onayla
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
