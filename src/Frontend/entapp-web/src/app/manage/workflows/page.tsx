"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { WorkflowAiModal } from "@/components/manage/workflow-ai-modal";

// ── Types ────────────────────────────────────────────────────
interface WorkflowDefinition {
  definitionId: string;
  id: string;
  name: string;
  description?: string;
  version: number;
  isPublished: boolean;
  isLatest: boolean;
  createdAt: string;
  // Elsa-specific fields
  materializedName?: string;
}

// ── API ──────────────────────────────────────────────────────
const ELSA_API_BASE = "/elsa/api";

async function fetchWorkflowDefinitions(): Promise<WorkflowDefinition[]> {
  try {
    const res = await fetch(`${ELSA_API_BASE}/workflow-definitions`, {
      headers: {
        Authorization: "ApiKey 00000000-0000-0000-0000-000000000000",
      },
    });
    if (!res.ok) {
      console.warn("Elsa API not available:", res.status);
      return [];
    }
    const data = await res.json();
    return data.items ?? [];
  } catch {
    console.warn("Elsa API connection failed — is the backend running?");
    return [];
  }
}

async function createNewWorkflowDefinition(): Promise<string | null> {
  try {
    const res = await fetch(`${ELSA_API_BASE}/workflow-definitions`, {
      method: "POST",
      headers: {
        Authorization: "ApiKey 00000000-0000-0000-0000-000000000000",
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        model: {
          name: `Yeni Workflow ${new Date().toLocaleString("tr-TR")}`,
          description: "",
          root: {
            type: "Elsa.Flowchart",
            activities: [],
            connections: [],
          },
        },
        publish: false,
      }),
    });
    if (!res.ok) {
      console.error("Failed to create workflow:", res.status);
      return null;
    }
    const data = await res.json();
    return data.workflowDefinition?.definitionId ?? data.definitionId ?? null;
  } catch (err) {
    console.error("Failed to create workflow:", err);
    return null;
  }
}

async function deleteWorkflowDefinition(definitionId: string): Promise<boolean> {
  try {
    const res = await fetch(
      `${ELSA_API_BASE}/workflow-definitions/${definitionId}`,
      {
        method: "DELETE",
        headers: {
          Authorization: "ApiKey 00000000-0000-0000-0000-000000000000",
        },
      }
    );
    return res.ok || res.status === 204;
  } catch (err) {
    console.error("Failed to delete workflow:", err);
    return false;
  }
}

async function bulkDeleteWorkflowDefinitions(
  definitionIds: string[]
): Promise<boolean> {
  try {
    const res = await fetch(
      `${ELSA_API_BASE}/bulk-actions/delete/workflow-definitions/by-definition-id`,
      {
        method: "POST",
        headers: {
          Authorization: "ApiKey 00000000-0000-0000-0000-000000000000",
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ definitionIds }),
      }
    );
    return res.ok;
  } catch (err) {
    console.error("Failed to bulk delete workflows:", err);
    return false;
  }
}

// ── Component ────────────────────────────────────────────────
export default function WorkflowsPage() {
  const router = useRouter();
  const [definitions, setDefinitions] = useState<WorkflowDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [deleting, setDeleting] = useState<string | null>(null); // definitionId being deleted
  const [confirmDelete, setConfirmDelete] = useState<WorkflowDefinition | null>(null);
  const [confirmBulkDelete, setConfirmBulkDelete] = useState(false);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  // AI Modal state
  const [aiModalOpen, setAiModalOpen] = useState(false);
  const [aiModalMode, setAiModalMode] = useState<"generate" | "describe">(
    "generate"
  );
  const [aiDescribeId, setAiDescribeId] = useState<string | undefined>();

  useEffect(() => {
    loadDefinitions();
  }, []);

  async function loadDefinitions() {
    setLoading(true);
    try {
      const items = await fetchWorkflowDefinitions();
      setDefinitions(items);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load");
    } finally {
      setLoading(false);
    }
  }

  async function handleCreateNew() {
    setCreating(true);
    try {
      const defId = await createNewWorkflowDefinition();
      if (defId) {
        router.push(`/manage/workflows/${defId}`);
      } else {
        setError("Workflow oluşturulamadı. Backend çalışıyor mu?");
      }
    } finally {
      setCreating(false);
    }
  }

  function handleOpenAiGenerate() {
    setAiModalMode("generate");
    setAiDescribeId(undefined);
    setAiModalOpen(true);
  }

  function handleOpenAiDescribe(defId: string) {
    setAiModalMode("describe");
    setAiDescribeId(defId);
    setAiModalOpen(true);
  }

  function handleAiWorkflowCreated(defId: string) {
    router.push(`/manage/workflows/${defId}`);
  }

  async function handleDelete(def: WorkflowDefinition) {
    setDeleting(def.definitionId);
    setConfirmDelete(null);
    const success = await deleteWorkflowDefinition(def.definitionId);
    if (success) {
      setDefinitions((prev) =>
        prev.filter((d) => d.definitionId !== def.definitionId)
      );
      setSelectedIds((prev) => {
        const next = new Set(prev);
        next.delete(def.definitionId);
        return next;
      });
    } else {
      setError("Workflow silinemedi.");
    }
    setDeleting(null);
  }

  async function handleBulkDelete() {
    setConfirmBulkDelete(false);
    if (selectedIds.size === 0) return;
    setDeleting("bulk");
    const success = await bulkDeleteWorkflowDefinitions(
      Array.from(selectedIds)
    );
    if (success) {
      setDefinitions((prev) =>
        prev.filter((d) => !selectedIds.has(d.definitionId))
      );
      setSelectedIds(new Set());
    } else {
      setError("Toplu silme başarısız oldu.");
    }
    setDeleting(null);
    loadDefinitions(); // refresh
  }

  function toggleSelect(defId: string) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(defId)) next.delete(defId);
      else next.add(defId);
      return next;
    });
  }

  function toggleSelectAll() {
    if (selectedIds.size === definitions.length) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(definitions.map((d) => d.definitionId)));
    }
  }

  return (
    <div className="space-y-6">
      {/* ── Header ──────────────────────────── */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">
            İş Akışları
          </h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">
            Workflow tanımlarını yönetin ve Elsa Designer ile görsel akış
            oluşturun.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={loadDefinitions}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium bg-[var(--color-surface-elevated)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] border border-[var(--color-border)] hover:border-[var(--color-border-strong)] transition-all duration-200"
          >
            <svg
              className="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={1.5}
                d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182"
              />
            </svg>
            Yenile
          </button>
          {/* Toplu Sil */}
          {selectedIds.size > 0 && (
            <button
              onClick={() => setConfirmBulkDelete(true)}
              disabled={deleting === "bulk"}
              className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium bg-red-500/10 text-red-400 border border-red-500/30 hover:bg-red-500/20 hover:border-red-500/50 transition-all duration-200 disabled:opacity-50"
            >
              {deleting === "bulk" ? (
                <div className="w-4 h-4 border-2 border-red-400/30 border-t-red-400 rounded-full animate-spin" />
              ) : (
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                </svg>
              )}
              {selectedIds.size} Seçili Sil
            </button>
          )}
          {/* AI ile Oluştur button */}
          <button
            onClick={handleOpenAiGenerate}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-violet-500 to-purple-500 text-white shadow-lg shadow-violet-500/25 hover:shadow-violet-500/40 hover:brightness-110 transition-all duration-200"
          >
            <svg
              className="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={1.5}
                d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09z"
              />
            </svg>
            AI ile Oluştur
          </button>
          <button
            onClick={handleCreateNew}
            disabled={creating}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-teal-500 to-cyan-500 text-white shadow-lg shadow-teal-500/25 hover:shadow-teal-500/40 hover:brightness-110 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {creating ? (
              <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
            ) : (
              <svg
                className="w-4 h-4"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 4v16m8-8H4"
                />
              </svg>
            )}
            {creating ? "Oluşturuluyor..." : "Boş Workflow"}
          </button>
        </div>
      </div>

      {/* ── Error Banner ────────────────────── */}
      {error && (
        <div className="rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-300">
          <span className="font-semibold">Hata:</span> {error}
        </div>
      )}

      {/* ── Table ───────────────────────────── */}
      <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden shadow-xl shadow-black/10 overflow-x-auto">
        {loading ? (
          <div className="flex items-center justify-center py-20">
            <div className="flex items-center gap-3 text-[var(--color-text-muted)]">
              <div className="w-5 h-5 border-2 border-teal-400/30 border-t-teal-400 rounded-full animate-spin" />
              <span className="text-sm">Workflow tanımları yükleniyor...</span>
            </div>
          </div>
        ) : definitions.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-20 text-center">
            <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-teal-500/20 to-cyan-500/10 flex items-center justify-center mb-4">
              <svg
                className="w-8 h-8 text-teal-400"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={1.5}
                  d="M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75z"
                />
              </svg>
            </div>
            <h3 className="text-lg font-semibold text-[var(--color-text)] mb-1">
              Henüz Workflow Tanımlanmamış
            </h3>
            <p className="text-sm text-[var(--color-text-muted)] max-w-sm mb-4">
              AI ile doğal dilde tarif ederek veya Elsa Designer ile görsel
              olarak yeni bir iş akışı tasarlayın.
            </p>
            <div className="flex items-center gap-3">
              <button
                onClick={handleOpenAiGenerate}
                className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-violet-500 to-purple-500 text-white shadow-lg shadow-violet-500/25 hover:shadow-violet-500/40 transition-all"
              >
                <svg
                  className="w-4 h-4"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={1.5}
                    d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09z"
                  />
                </svg>
                AI ile Oluştur
              </button>
              <button
                onClick={handleCreateNew}
                disabled={creating}
                className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-teal-500 to-cyan-500 text-white shadow-lg shadow-teal-500/25 hover:shadow-teal-500/40 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {creating ? (
                  <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                ) : (
                  <svg
                    className="w-4 h-4"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 4v16m8-8H4"
                    />
                  </svg>
                )}
                {creating ? "Oluşturuluyor..." : "Boş Workflow Oluştur"}
              </button>
            </div>
          </div>
        ) : (
          <table className="w-full min-w-[700px]">
            <thead>
              <tr className="border-b border-[var(--color-border)]">
                <th className="w-10 px-3 py-3">
                  <input
                    type="checkbox"
                    checked={selectedIds.size === definitions.length && definitions.length > 0}
                    onChange={toggleSelectAll}
                    className="w-4 h-4 rounded border-[var(--color-border)] bg-[var(--color-surface-elevated)] accent-violet-500 cursor-pointer"
                  />
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Workflow Adı
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider hidden lg:table-cell">
                  Açıklama
                </th>
                <th className="text-center px-3 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Ver.
                </th>
                <th className="text-center px-3 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Durum
                </th>
                <th className="text-right px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Aksiyonlar
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {definitions.map((def) => (
                <tr
                  key={def.definitionId}
                  className={`hover:bg-[var(--color-surface-elevated)] transition-colors ${selectedIds.has(def.definitionId) ? 'bg-violet-500/5' : ''} ${deleting === def.definitionId ? 'opacity-50' : ''}`}
                >
                  <td className="w-10 px-3 py-3">
                    <input
                      type="checkbox"
                      checked={selectedIds.has(def.definitionId)}
                      onChange={() => toggleSelect(def.definitionId)}
                      className="w-4 h-4 rounded border-[var(--color-border)] bg-[var(--color-surface-elevated)] accent-violet-500 cursor-pointer"
                    />
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-teal-500/20 to-cyan-500/10 flex items-center justify-center flex-shrink-0">
                        <svg
                          className="w-4 h-4 text-teal-400"
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={1.5}
                            d="M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75z"
                          />
                        </svg>
                      </div>
                      <span className="font-medium text-[var(--color-text)]">
                        {def.name || def.materializedName || "İsimsiz"}
                      </span>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-sm text-[var(--color-text-muted)] max-w-[200px] truncate hidden lg:table-cell">
                    {def.description || "—"}
                  </td>
                  <td className="px-3 py-3 text-center">
                    <span className="inline-flex items-center justify-center w-7 h-7 rounded-lg bg-[var(--color-surface-elevated)] text-xs font-semibold text-[var(--color-text-muted)]">
                      v{def.version}
                    </span>
                  </td>
                  <td className="px-3 py-3 text-center">
                    {def.isPublished ? (
                      <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-500/15 text-emerald-400">
                        <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
                        Aktif
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-500/15 text-amber-400">
                        <span className="w-1.5 h-1.5 rounded-full bg-amber-400" />
                        Taslak
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex items-center justify-end gap-1">
                      {/* AI ile Tarif Et */}
                      <button
                        onClick={() =>
                          handleOpenAiDescribe(def.definitionId)
                        }
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-violet-400 hover:text-violet-300 hover:bg-violet-500/10 transition-colors"
                        title="AI ile Tarif Et"
                      >
                        <svg
                          className="w-3.5 h-3.5"
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09z"
                          />
                        </svg>
                        AI Tarif
                      </button>
                      {/* Düzenle */}
                      <Link
                        href={`/manage/workflows/${def.definitionId}`}
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-teal-400 hover:text-teal-300 hover:bg-teal-500/10 transition-colors"
                      >
                        <svg
                          className="w-3.5 h-3.5"
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M13.5 6H5.25A2.25 2.25 0 003 8.25v10.5A2.25 2.25 0 005.25 21h10.5A2.25 2.25 0 0018 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25"
                          />
                        </svg>
                        Düzenle
                      </Link>
                      {/* Sil */}
                      <button
                        onClick={() => setConfirmDelete(def)}
                        disabled={deleting === def.definitionId}
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-red-400 hover:text-red-300 hover:bg-red-500/10 transition-colors disabled:opacity-50"
                        title="Sil"
                      >
                        {deleting === def.definitionId ? (
                          <div className="w-3.5 h-3.5 border-2 border-red-400/30 border-t-red-400 rounded-full animate-spin" />
                        ) : (
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                          </svg>
                        )}
                        Sil
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* ── Info Card ───────────────────────── */}
      <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6">
        <div className="flex items-start gap-4">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-violet-500/20 to-purple-500/10 flex items-center justify-center flex-shrink-0">
            <svg
              className="w-5 h-5 text-violet-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={1.5}
                d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09z"
              />
            </svg>
          </div>
          <div>
            <h3 className="font-semibold text-[var(--color-text)] mb-1">
              AI Workflow Asistanı
            </h3>
            <p className="text-sm text-[var(--color-text-muted)] leading-relaxed">
              <span className="font-medium text-violet-400">
                &quot;AI ile Oluştur&quot;
              </span>{" "}
              butonuna tıklayarak iş akışınızı doğal dilde tarif edebilirsiniz.
              AI, 8 özel activity (Change Status, Route to Queue, Wait for
              Approval vb.) kullanarak Elsa workflow&apos;u oluşturur. Mevcut
              workflow&apos;ları{" "}
              <span className="font-medium text-violet-400">
                &quot;AI Tarif&quot;
              </span>{" "}
              ile analiz edip, tarifi düzenleyerek yeni varyasyonlar
              üretebilirsiniz.
            </p>
          </div>
        </div>
      </div>

      {/* ── AI Modal ──────────────────────── */}
      <WorkflowAiModal
        isOpen={aiModalOpen}
        onClose={() => setAiModalOpen(false)}
        mode={aiModalMode}
        definitionId={aiDescribeId}
        onWorkflowCreated={handleAiWorkflowCreated}
      />

      {/* ── Delete Confirmation Dialog ───── */}
      {confirmDelete && (
        <>
          <div
            className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm"
            onClick={() => setConfirmDelete(null)}
          />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-2xl shadow-2xl shadow-black/30 max-w-md w-full p-6">
              <div className="flex items-center gap-3 mb-4">
                <div className="w-10 h-10 rounded-xl bg-red-500/10 flex items-center justify-center flex-shrink-0">
                  <svg className="w-5 h-5 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                  </svg>
                </div>
                <div>
                  <h3 className="text-lg font-bold text-[var(--color-text)]">Workflow Sil</h3>
                  <p className="text-sm text-[var(--color-text-muted)]">Bu işlem geri alınamaz</p>
                </div>
              </div>
              <p className="text-sm text-[var(--color-text-muted)] mb-6">
                <span className="font-semibold text-[var(--color-text)]">&quot;{confirmDelete.name || confirmDelete.materializedName || "İsimsiz"}&quot;</span> workflow&apos;unu ve tüm versiyonlarını silmek istediğinizden emin misiniz?
              </p>
              <div className="flex items-center justify-end gap-3">
                <button
                  onClick={() => setConfirmDelete(null)}
                  className="px-4 py-2 rounded-xl text-sm font-medium text-[var(--color-text-muted)] hover:text-[var(--color-text)] bg-[var(--color-surface-elevated)] border border-[var(--color-border)] hover:border-[var(--color-border-strong)] transition-all"
                >
                  Vazgeç
                </button>
                <button
                  onClick={() => handleDelete(confirmDelete)}
                  className="px-4 py-2 rounded-xl text-sm font-semibold bg-red-500 text-white hover:bg-red-600 shadow-lg shadow-red-500/25 transition-all"
                >
                  Sil
                </button>
              </div>
            </div>
          </div>
        </>
      )}

      {/* ── Bulk Delete Confirmation ──────── */}
      {confirmBulkDelete && (
        <>
          <div
            className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm"
            onClick={() => setConfirmBulkDelete(false)}
          />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-2xl shadow-2xl shadow-black/30 max-w-md w-full p-6">
              <div className="flex items-center gap-3 mb-4">
                <div className="w-10 h-10 rounded-xl bg-red-500/10 flex items-center justify-center flex-shrink-0">
                  <svg className="w-5 h-5 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                  </svg>
                </div>
                <div>
                  <h3 className="text-lg font-bold text-[var(--color-text)]">Toplu Silme</h3>
                  <p className="text-sm text-[var(--color-text-muted)]">Bu işlem geri alınamaz</p>
                </div>
              </div>
              <p className="text-sm text-[var(--color-text-muted)] mb-6">
                Seçili <span className="font-semibold text-[var(--color-text)]">{selectedIds.size} workflow</span>&apos;u ve tüm versiyonlarını silmek istediğinizden emin misiniz?
              </p>
              <div className="flex items-center justify-end gap-3">
                <button
                  onClick={() => setConfirmBulkDelete(false)}
                  className="px-4 py-2 rounded-xl text-sm font-medium text-[var(--color-text-muted)] hover:text-[var(--color-text)] bg-[var(--color-surface-elevated)] border border-[var(--color-border)] hover:border-[var(--color-border-strong)] transition-all"
                >
                  Vazgeç
                </button>
                <button
                  onClick={handleBulkDelete}
                  className="px-4 py-2 rounded-xl text-sm font-semibold bg-red-500 text-white hover:bg-red-600 shadow-lg shadow-red-500/25 transition-all"
                >
                  {selectedIds.size} Workflow Sil
                </button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
