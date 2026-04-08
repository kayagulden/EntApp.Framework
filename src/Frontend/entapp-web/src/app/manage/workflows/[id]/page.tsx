"use client";

import { useParams } from "next/navigation";
import Link from "next/link";
import { useState, useEffect } from "react";

interface WorkflowDefinition {
  definitionId: string;
  name: string;
  description?: string;
  version: number;
  isPublished: boolean;
}

const ELSA_STUDIO_URL = "http://localhost:5280";

export default function WorkflowDesignerPage() {
  const params = useParams();
  const definitionId = params.id as string;
  const [definition, setDefinition] = useState<WorkflowDefinition | null>(null);
  const [loading, setLoading] = useState(true);
  const [studioReady, setStudioReady] = useState(false);

  useEffect(() => {
    // Workflow definition bilgisini al
    fetch(`/elsa/api/workflow-definitions/${definitionId}`, {
      headers: {
        Authorization: "ApiKey AJE1MTM0",
      },
    })
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        setDefinition(data);
        setLoading(false);
      })
      .catch(() => setLoading(false));
  }, [definitionId]);

  return (
    <div className="space-y-4">
      {/* ── Header ──────────────────────────── */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link
            href="/manage/workflows"
            className="inline-flex items-center justify-center w-10 h-10 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] hover:border-[var(--color-border-strong)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-all duration-200"
          >
            <svg
              className="w-5 h-5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={1.5}
                d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18"
              />
            </svg>
          </Link>
          <div>
            <h1 className="text-xl font-bold text-[var(--color-text)]">
              {loading
                ? "Yükleniyor..."
                : definition?.name || "Workflow Designer"}
            </h1>
            <div className="flex items-center gap-3 mt-1">
              {definition && (
                <>
                  <span className="text-xs text-[var(--color-text-muted)]">
                    v{definition.version}
                  </span>
                  {definition.isPublished ? (
                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-500/15 text-emerald-400">
                      <span className="w-1 h-1 rounded-full bg-emerald-400" />
                      Aktif
                    </span>
                  ) : (
                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-amber-500/15 text-amber-400">
                      <span className="w-1 h-1 rounded-full bg-amber-400" />
                      Taslak
                    </span>
                  )}
                </>
              )}
              <span className="text-[10px] font-mono text-[var(--color-text-muted)] opacity-50">
                {definitionId}
              </span>
            </div>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <a
            href={`${ELSA_STUDIO_URL}/workflow-definitions/${definitionId}/edit`}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium bg-gradient-to-r from-teal-500 to-cyan-500 text-white shadow-lg shadow-teal-500/25 hover:shadow-teal-500/40 hover:brightness-110 transition-all duration-200"
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
            Tam Ekran Aç
          </a>
        </div>
      </div>

      {/* ── Designer iframe ─────────────────── */}
      <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden shadow-xl shadow-black/10 relative">
        {!studioReady && (
          <div className="absolute inset-0 flex items-center justify-center bg-[var(--color-surface)] z-10">
            <div className="flex flex-col items-center gap-4">
              <div className="w-12 h-12 border-3 border-teal-400/30 border-t-teal-400 rounded-full animate-spin" />
              <div className="text-center">
                <p className="text-sm font-medium text-[var(--color-text)]">
                  Elsa Studio yükleniyor...
                </p>
                <p className="text-xs text-[var(--color-text-muted)] mt-1">
                  Blazor WASM başlatılıyor, lütfen bekleyin
                </p>
              </div>
            </div>
          </div>
        )}
        <iframe
          src={`${ELSA_STUDIO_URL}/workflow-definitions/${definitionId}/edit`}
          className="w-full border-0"
          style={{ height: "calc(100vh - 200px)", minHeight: "600px" }}
          onLoad={() => setStudioReady(true)}
          title="Elsa Studio — Workflow Designer"
          sandbox="allow-same-origin allow-scripts allow-forms allow-popups allow-modals"
        />
      </div>
    </div>
  );
}
