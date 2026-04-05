"use client";

import { useState, useEffect } from "react";

interface Prompt {
  id: string;
  key: string;
  version: number;
  title: string;
  templateContent: string;
  category: string | null;
  isActive: boolean;
}

export default function ManagePromptsPage() {
  const [prompts, setPrompts] = useState<Prompt[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch("/api/ai/prompts")
      .then((r) => (r.ok ? r.json() : { items: [] }))
      .then((d) => setPrompts(d.items || []))
      .catch(() => setPrompts([]))
      .finally(() => setLoading(false));
  }, []);

  const categoryColors: Record<string, string> = {
    system: "bg-violet-500/10 text-violet-400",
    chat: "bg-blue-500/10 text-blue-400",
    analysis: "bg-emerald-500/10 text-emerald-400",
    generation: "bg-amber-500/10 text-amber-400",
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">Prompt Şablonları</h1>
        <p className="text-sm text-[var(--color-text-muted)] mt-1">
          Tenant&apos;ınız için mevcut AI prompt şablonlarını görüntüleyin.
          Override&apos;lar yakında eklenecektir.
        </p>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12">
          <div className="w-6 h-6 border-2 border-teal-500 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : prompts.length === 0 ? (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-12 text-center">
          <p className="text-[var(--color-text-muted)]">Henüz prompt şablonu tanımlanmamış.</p>
          <p className="text-xs text-[var(--color-text-muted)] mt-2">Prompt şablonları Admin Panel üzerinden oluşturulur.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {prompts.map((p) => (
            <div key={p.id} className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-4 hover:border-teal-500/20 transition-all">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="font-semibold text-[var(--color-text)]">{p.title}</h3>
                  <code className="text-xs text-[var(--color-text-muted)]">{p.key}</code>
                </div>
                <div className="flex items-center gap-2">
                  {p.category && (
                    <span className={`px-2 py-0.5 rounded text-[10px] font-semibold uppercase ${categoryColors[p.category] || "bg-slate-500/10 text-slate-400"}`}>
                      {p.category}
                    </span>
                  )}
                  <span className="px-2 py-0.5 rounded text-[10px] font-medium bg-slate-500/10 text-slate-400">
                    v{p.version}
                  </span>
                  <span className={`px-2 py-0.5 rounded text-[10px] font-medium ${
                    p.isActive ? "bg-emerald-500/10 text-emerald-400" : "bg-red-500/10 text-red-400"
                  }`}>
                    {p.isActive ? "Aktif" : "Pasif"}
                  </span>
                </div>
              </div>
              <pre className="mt-3 text-xs text-[var(--color-text-muted)] bg-[var(--color-bg-secondary)] rounded-lg p-3 overflow-hidden max-h-20 font-mono">
                {p.templateContent}
              </pre>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
