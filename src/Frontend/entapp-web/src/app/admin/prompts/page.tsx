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
  createdAt: string;
  updatedAt: string | null;
}

interface PromptListResponse {
  items: Prompt[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export default function AdminPromptsPage() {
  const [data, setData] = useState<PromptListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedPrompt, setSelectedPrompt] = useState<Prompt | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [testResult, setTestResult] = useState<string | null>(null);
  const [testVars, setTestVars] = useState("{}");

  // Create form
  const [newKey, setNewKey] = useState("");
  const [newTitle, setNewTitle] = useState("");
  const [newCategory, setNewCategory] = useState("");
  const [newContent, setNewContent] = useState("");

  const fetchPrompts = () => {
    setLoading(true);
    fetch("/api/ai/prompts")
      .then((r) => (r.ok ? r.json() : { items: [], totalCount: 0, pageNumber: 1, pageSize: 20 }))
      .then(setData)
      .catch(() => setData({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20 }))
      .finally(() => setLoading(false));
  };

  useEffect(fetchPrompts, []);

  const handleCreate = async () => {
    if (!newKey || !newContent) return;
    await fetch("/api/ai/prompts", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        key: newKey,
        title: newTitle || newKey,
        category: newCategory || null,
        templateContent: newContent,
      }),
    });
    setShowCreate(false);
    setNewKey(""); setNewTitle(""); setNewCategory(""); setNewContent("");
    fetchPrompts();
  };

  const handleTest = async () => {
    if (!selectedPrompt) return;
    try {
      const vars = JSON.parse(testVars);
      const res = await fetch("/api/ai/prompts/render", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ key: selectedPrompt.key, variables: vars }),
      });
      const result = await res.json();
      setTestResult(res.ok ? result.rendered : result.error);
    } catch (e) {
      setTestResult("JSON parse hatası");
    }
  };

  const categoryColors: Record<string, string> = {
    system: "bg-violet-500/10 text-violet-400",
    chat: "bg-blue-500/10 text-blue-400",
    analysis: "bg-emerald-500/10 text-emerald-400",
    generation: "bg-amber-500/10 text-amber-400",
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Prompt Şablonları</h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">
            AI prompt şablonlarını oluşturun, versiyonlayın ve test edin.
          </p>
        </div>
        <button
          onClick={() => setShowCreate(!showCreate)}
          className="flex items-center gap-2 px-4 py-2.5 rounded-lg bg-amber-500 hover:bg-amber-600 text-white text-sm font-medium transition-colors shadow-md shadow-amber-500/20"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          Yeni Prompt
        </button>
      </div>

      {/* Create Form */}
      {showCreate && (
        <div className="rounded-xl border border-amber-500/20 bg-[var(--color-surface)] p-5 space-y-4">
          <h3 className="font-semibold text-[var(--color-text)]">Yeni Prompt Oluştur</h3>
          <div className="grid grid-cols-3 gap-3">
            <input value={newKey} onChange={(e) => setNewKey(e.target.value)} placeholder="Key (benzersiz)" className="px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-sm text-[var(--color-text)] focus:ring-2 focus:ring-amber-500/30 focus:outline-none" />
            <input value={newTitle} onChange={(e) => setNewTitle(e.target.value)} placeholder="Başlık" className="px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-sm text-[var(--color-text)] focus:ring-2 focus:ring-amber-500/30 focus:outline-none" />
            <input value={newCategory} onChange={(e) => setNewCategory(e.target.value)} placeholder="Kategori (opsiyonel)" className="px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-sm text-[var(--color-text)] focus:ring-2 focus:ring-amber-500/30 focus:outline-none" />
          </div>
          <textarea value={newContent} onChange={(e) => setNewContent(e.target.value)} placeholder={"Scriban şablon içeriği...\n{{ variable }} syntax kullanabilirsiniz"} rows={5} className="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-sm text-[var(--color-text)] font-mono focus:ring-2 focus:ring-amber-500/30 focus:outline-none" />
          <div className="flex gap-2">
            <button onClick={handleCreate} className="px-4 py-2 rounded-lg bg-amber-500 hover:bg-amber-600 text-white text-sm font-medium transition-colors">Oluştur</button>
            <button onClick={() => setShowCreate(false)} className="px-4 py-2 rounded-lg border border-[var(--color-border)] text-[var(--color-text-muted)] text-sm hover:bg-[var(--color-surface-hover)] transition-colors">İptal</button>
          </div>
        </div>
      )}

      {/* Prompt List */}
      {loading ? (
        <div className="flex items-center justify-center py-12">
          <div className="w-6 h-6 border-2 border-amber-500 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-12 text-center">
          <p className="text-[var(--color-text-muted)]">Henüz prompt şablonu tanımlanmamış.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {data.items.map((p) => (
            <div
              key={p.id}
              onClick={() => { setSelectedPrompt(p); setTestResult(null); }}
              className={`rounded-xl border p-4 cursor-pointer transition-all ${
                selectedPrompt?.id === p.id
                  ? "border-amber-500/40 bg-amber-500/5"
                  : "border-[var(--color-border)] bg-[var(--color-surface)] hover:border-amber-500/20"
              }`}
            >
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
                </div>
              </div>
              <pre className="mt-3 text-xs text-[var(--color-text-muted)] bg-[var(--color-bg-secondary)] rounded-lg p-3 overflow-hidden max-h-24 font-mono">
                {p.templateContent}
              </pre>
            </div>
          ))}
        </div>
      )}

      {/* Test Panel */}
      {selectedPrompt && (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5 space-y-4">
          <h3 className="font-semibold text-[var(--color-text)]">
            Test: <code className="text-amber-400">{selectedPrompt.key}</code>
          </h3>
          <div>
            <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Değişkenler (JSON)</label>
            <textarea value={testVars} onChange={(e) => setTestVars(e.target.value)} rows={3} className="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-sm text-[var(--color-text)] font-mono focus:ring-2 focus:ring-amber-500/30 focus:outline-none" />
          </div>
          <button onClick={handleTest} className="px-4 py-2 rounded-lg bg-teal-500 hover:bg-teal-600 text-white text-sm font-medium transition-colors">
            Render / Test
          </button>
          {testResult && (
            <div className="mt-2">
              <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Sonuç</label>
              <pre className="px-3 py-2 rounded-lg bg-[var(--color-bg-secondary)] text-sm text-[var(--color-text)] font-mono whitespace-pre-wrap">
                {testResult}
              </pre>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
