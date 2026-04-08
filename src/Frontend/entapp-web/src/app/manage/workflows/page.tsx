"use client";

import { useState, useEffect } from "react";
import Link from "next/link";

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
        Authorization: "ApiKey AJE1MTM0",
        "Content-Type": "application/json",
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

// ── Component ────────────────────────────────────────────────
export default function WorkflowsPage() {
  const [definitions, setDefinitions] = useState<WorkflowDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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
          <a
            href="http://localhost:5280"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-teal-500 to-cyan-500 text-white shadow-lg shadow-teal-500/25 hover:shadow-teal-500/40 hover:brightness-110 transition-all duration-200"
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
                strokeWidth={2}
                d="M12 4v16m8-8H4"
              />
            </svg>
            Yeni Workflow
          </a>
        </div>
      </div>

      {/* ── Error Banner ────────────────────── */}
      {error && (
        <div className="rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-300">
          <span className="font-semibold">Hata:</span> {error}
        </div>
      )}

      {/* ── Table ───────────────────────────── */}
      <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden shadow-xl shadow-black/10">
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
              Elsa Designer&apos;ı açarak görsel olarak yeni bir iş akışı
              tasarlayın. Custom activity&apos;ler (Ticket Management)
              designer&apos;da hazır olarak mevcut.
            </p>
            <a
              href="http://localhost:5280"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-teal-500 to-cyan-500 text-white shadow-lg shadow-teal-500/25 hover:shadow-teal-500/40 transition-all"
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
                  strokeWidth={2}
                  d="M13.5 6H5.25A2.25 2.25 0 003 8.25v10.5A2.25 2.25 0 005.25 21h10.5A2.25 2.25 0 0018 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25"
                />
              </svg>
              Elsa Designer Aç
            </a>
          </div>
        ) : (
          <table className="w-full">
            <thead>
              <tr className="border-b border-[var(--color-border)]">
                <th className="text-left px-6 py-4 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Workflow Adı
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Açıklama
                </th>
                <th className="text-center px-6 py-4 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Versiyon
                </th>
                <th className="text-center px-6 py-4 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Durum
                </th>
                <th className="text-right px-6 py-4 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Aksiyonlar
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {definitions.map((def) => (
                <tr
                  key={def.definitionId}
                  className="hover:bg-[var(--color-surface-elevated)] transition-colors"
                >
                  <td className="px-6 py-4">
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
                  <td className="px-6 py-4 text-sm text-[var(--color-text-muted)] max-w-xs truncate">
                    {def.description || "—"}
                  </td>
                  <td className="px-6 py-4 text-center">
                    <span className="inline-flex items-center justify-center w-7 h-7 rounded-lg bg-[var(--color-surface-elevated)] text-xs font-semibold text-[var(--color-text-muted)]">
                      v{def.version}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-center">
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
                  <td className="px-6 py-4 text-right">
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
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-indigo-500/20 to-purple-500/10 flex items-center justify-center flex-shrink-0">
            <svg
              className="w-5 h-5 text-indigo-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={1.5}
                d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z"
              />
            </svg>
          </div>
          <div>
            <h3 className="font-semibold text-[var(--color-text)] mb-1">
              Custom Activities
            </h3>
            <p className="text-sm text-[var(--color-text-muted)] leading-relaxed">
              Elsa Designer&apos;da{" "}
              <span className="font-medium text-teal-400">
                &quot;Ticket Management&quot;
              </span>{" "}
              kategorisinde 8 özel activity mevcuttur: Change Status, Assign
              Ticket, Wait for Approval, Send Notification, Check SLA, Route to
              Queue, Add Comment ve Get Ticket Details. Bu activity&apos;ler
              sürükle-bırak ile workflow akışlarınıza eklenebilir.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
