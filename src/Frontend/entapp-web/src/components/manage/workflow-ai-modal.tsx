"use client";

import { useState, useRef, useEffect } from "react";

// ── Types ─────────────────────────────────────────────────
interface ActivitySummary {
  type: string;
  displayName: string;
  description: string;
}

interface GenerateResult {
  definitionId: string;
  name: string;
  description: string | null;
  activityCount: number;
  message: string;
}

interface DescribeResult {
  definitionId: string;
  name: string;
  description: string;
  activities: ActivitySummary[];
}

interface ChatMessage {
  role: "user" | "assistant" | "system";
  content: string;
  timestamp: Date;
  result?: GenerateResult;
}

type ModalMode = "generate" | "describe";

interface WorkflowAiModalProps {
  isOpen: boolean;
  onClose: () => void;
  mode: ModalMode;
  /** describe modunda hangi workflow */
  definitionId?: string;
  /** Workflow oluşturulduktan sonra yönlendirme */
  onWorkflowCreated?: (definitionId: string) => void;
}

// ── API ────────────────────────────────────────────────────
const API_BASE = "/api/workflows/ai";

async function generateWorkflow(
  prompt: string,
  name?: string
): Promise<GenerateResult> {
  const res = await fetch(`${API_BASE}/generate`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ prompt, name }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(
      err.error || `Workflow oluşturulamadı (${res.status})`
    );
  }
  return res.json();
}

async function describeWorkflow(
  definitionId: string
): Promise<DescribeResult> {
  const res = await fetch(`${API_BASE}/describe`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ definitionId }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(
      err.error || `Workflow tarif edilemedi (${res.status})`
    );
  }
  return res.json();
}

// ── Component ──────────────────────────────────────────────
export function WorkflowAiModal({
  isOpen,
  onClose,
  mode: initialMode,
  definitionId,
  onWorkflowCreated,
}: WorkflowAiModalProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [mode, setMode] = useState<ModalMode>(initialMode);
  const [describeLoaded, setDescribeLoaded] = useState(false);
  const chatEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

  // Scroll to bottom on new messages
  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  // Focus input on open
  useEffect(() => {
    if (isOpen) {
      setTimeout(() => inputRef.current?.focus(), 100);
    }
  }, [isOpen]);

  // Reset state when modal opens
  useEffect(() => {
    if (isOpen) {
      setMessages([]);
      setInput("");
      setDescribeLoaded(false);
      setMode(initialMode);

      // Auto-describe if in describe mode
      if (initialMode === "describe" && definitionId) {
        handleDescribe(definitionId);
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, initialMode, definitionId]);

  async function handleDescribe(defId: string) {
    setLoading(true);
    setMessages((prev) => [
      ...prev,
      {
        role: "system",
        content: "Workflow tarifi yükleniyor...",
        timestamp: new Date(),
      },
    ]);

    try {
      const result = await describeWorkflow(defId);
      setMessages([
        {
          role: "assistant",
          content: result.description,
          timestamp: new Date(),
        },
      ]);
      setDescribeLoaded(true);
      // Pre-fill input with description for modification
      setInput(result.description);
    } catch (err) {
      setMessages([
        {
          role: "system",
          content: `Hata: ${err instanceof Error ? err.message : "Bilinmeyen hata"}`,
          timestamp: new Date(),
        },
      ]);
    } finally {
      setLoading(false);
    }
  }

  async function handleGenerate() {
    if (!input.trim() || loading) return;

    const userMessage = input.trim();
    setInput("");
    setMessages((prev) => [
      ...prev,
      { role: "user", content: userMessage, timestamp: new Date() },
    ]);
    setLoading(true);

    try {
      const result = await generateWorkflow(userMessage);
      setMessages((prev) => [
        ...prev,
        {
          role: "assistant",
          content: `✅ **${result.name}** oluşturuldu!\n\n${result.description || ""}\n\n📊 ${result.activityCount} activity içeriyor.`,
          timestamp: new Date(),
          result,
        },
      ]);
    } catch (err) {
      setMessages((prev) => [
        ...prev,
        {
          role: "assistant",
          content: `❌ ${err instanceof Error ? err.message : "Workflow oluşturulamadı."}`,
          timestamp: new Date(),
        },
      ]);
    } finally {
      setLoading(false);
    }
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleGenerate();
    }
  }

  function handleOpenDesigner(defId: string) {
    onWorkflowCreated?.(defId);
    onClose();
  }

  if (!isOpen) return null;

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm transition-opacity"
        onClick={onClose}
      />

      {/* Modal */}
      <div className="fixed inset-y-0 right-0 z-50 w-full max-w-2xl flex flex-col bg-[var(--color-surface)] border-l border-[var(--color-border)] shadow-2xl shadow-black/30">
        {/* ── Header ──────────────────────── */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)] bg-gradient-to-r from-[var(--color-surface)] to-[var(--color-surface-elevated)]">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-violet-500/20 to-purple-500/20 flex items-center justify-center">
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
                  d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09zM18.259 8.715L18 9.75l-.259-1.035a3.375 3.375 0 00-2.455-2.456L14.25 6l1.036-.259a3.375 3.375 0 002.455-2.456L18 2.25l.259 1.035a3.375 3.375 0 002.455 2.456L21.75 6l-1.036.259a3.375 3.375 0 00-2.455 2.456zM16.894 20.567L16.5 21.75l-.394-1.183a2.25 2.25 0 00-1.423-1.423L13.5 18.75l1.183-.394a2.25 2.25 0 001.423-1.423l.394-1.183.394 1.183a2.25 2.25 0 001.423 1.423l1.183.394-1.183.394a2.25 2.25 0 00-1.423 1.423z"
                />
              </svg>
            </div>
            <div>
              <h2 className="text-lg font-bold text-[var(--color-text)]">
                {mode === "generate"
                  ? "AI ile Workflow Oluştur"
                  : "AI Workflow Tarifi"}
              </h2>
              <p className="text-xs text-[var(--color-text-muted)]">
                {mode === "generate"
                  ? "Workflow'u doğal dilde tarif edin, AI oluştursun"
                  : "Mevcut workflow'un AI tarafından oluşturulan tarifi"}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="w-8 h-8 rounded-lg flex items-center justify-center text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-surface-elevated)] transition-colors"
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
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>

        {/* ── Chat Area ───────────────────── */}
        <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">
          {/* Welcome message */}
          {messages.length === 0 && mode === "generate" && (
            <div className="flex flex-col items-center justify-center h-full text-center gap-4 py-12">
              <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-violet-500/20 to-purple-500/10 flex items-center justify-center">
                <svg
                  className="w-8 h-8 text-violet-400"
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
                <h3 className="text-lg font-semibold text-[var(--color-text)] mb-2">
                  Workflow&apos;unuzu Tarif Edin
                </h3>
                <p className="text-sm text-[var(--color-text-muted)] max-w-md leading-relaxed">
                  İş akışınızı doğal dilde anlatın. AI, Elsa Designer&apos;da
                  kullanılabilecek bir workflow oluşturacaktır.
                </p>
              </div>
              {/* Examples */}
              <div className="w-full max-w-md space-y-2 mt-4">
                <p className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Örnek Tarifler
                </p>
                {examplePrompts.map((example, i) => (
                  <button
                    key={i}
                    onClick={() => setInput(example)}
                    className="w-full text-left px-4 py-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-elevated)] hover:border-violet-500/30 hover:bg-violet-500/5 text-sm text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-all duration-200"
                  >
                    <span className="text-violet-400 mr-2">→</span>
                    {example}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Messages */}
          {messages.map((msg, i) => (
            <div
              key={i}
              className={`flex ${msg.role === "user" ? "justify-end" : "justify-start"}`}
            >
              <div
                className={`max-w-[85%] rounded-2xl px-4 py-3 ${
                  msg.role === "user"
                    ? "bg-gradient-to-r from-violet-600 to-purple-600 text-white"
                    : msg.role === "system"
                      ? "bg-amber-500/10 border border-amber-500/20 text-amber-300"
                      : "bg-[var(--color-surface-elevated)] border border-[var(--color-border)] text-[var(--color-text)]"
                }`}
              >
                <div className="text-sm whitespace-pre-wrap leading-relaxed">
                  {msg.content}
                </div>
                {/* Action buttons for generated workflow */}
                {msg.result && (
                  <div className="flex items-center gap-2 mt-3 pt-3 border-t border-white/10">
                    <button
                      onClick={() =>
                        handleOpenDesigner(msg.result!.definitionId)
                      }
                      className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold bg-white/20 hover:bg-white/30 text-white transition-colors"
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
                      Designer&apos;da Aç
                    </button>
                  </div>
                )}
              </div>
            </div>
          ))}

          {/* Loading indicator */}
          {loading && (
            <div className="flex justify-start">
              <div className="bg-[var(--color-surface-elevated)] border border-[var(--color-border)] rounded-2xl px-4 py-3">
                <div className="flex items-center gap-2">
                  <div className="flex gap-1">
                    <div className="w-2 h-2 rounded-full bg-violet-400 animate-bounce [animation-delay:0ms]" />
                    <div className="w-2 h-2 rounded-full bg-violet-400 animate-bounce [animation-delay:150ms]" />
                    <div className="w-2 h-2 rounded-full bg-violet-400 animate-bounce [animation-delay:300ms]" />
                  </div>
                  <span className="text-xs text-[var(--color-text-muted)]">
                    AI düşünüyor...
                  </span>
                </div>
              </div>
            </div>
          )}

          {/* Describe mode: action to create from description */}
          {describeLoaded && mode === "describe" && !loading && (
            <div className="flex justify-center py-4">
              <button
                onClick={() => {
                  setMode("generate");
                  setMessages([]);
                  setDescribeLoaded(false);
                }}
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
                    strokeWidth={2}
                    d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182"
                  />
                </svg>
                Bu tarifi düzenleyip yeni workflow oluştur
              </button>
            </div>
          )}

          <div ref={chatEndRef} />
        </div>

        {/* ── Input Area ──────────────────── */}
        <div className="px-6 py-4 border-t border-[var(--color-border)] bg-[var(--color-surface)]">
          <div className="flex gap-2">
            <div className="flex-1 relative">
              <textarea
                ref={inputRef}
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder={
                  mode === "generate"
                    ? "Workflow'u tarif edin... (Shift+Enter: yeni satır)"
                    : "Tarifi düzenleyin ve yeni workflow oluşturun..."
                }
                rows={3}
                className="w-full px-4 py-3 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-violet-500/50 focus:ring-2 focus:ring-violet-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] resize-none outline-none transition-all duration-200"
                disabled={loading}
              />
            </div>
            <button
              onClick={handleGenerate}
              disabled={!input.trim() || loading}
              className="self-end px-4 py-3 rounded-xl bg-gradient-to-r from-violet-500 to-purple-500 text-white shadow-lg shadow-violet-500/25 hover:shadow-violet-500/40 hover:brightness-110 transition-all duration-200 disabled:opacity-40 disabled:cursor-not-allowed"
            >
              {loading ? (
                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                <svg
                  className="w-5 h-5"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5"
                  />
                </svg>
              )}
            </button>
          </div>
          <p className="text-[10px] text-[var(--color-text-muted)] mt-2 text-center">
            AI, 8 custom activity (Change Status, Route to Queue, Wait for
            Approval vb.) kullanarak Elsa workflow&apos;u oluşturur.
          </p>
        </div>
      </div>
    </>
  );
}

// ── Example prompts ────────────────────────────────────────
const examplePrompts = [
  "Destek talebi oluşturulsun, durumu Open yapılsın, kuyruğa atılsın ve bir agent üzerine alması beklensin",
  "Ticket oluşturulunca önce SLA kontrol et. SLA ihlali varsa eskalasyon yap, yoksa kuyruğa yönlendir",
  "Çok aşamalı onay: talep oluştur → yönetici onayı → IT onayı → tamamla",
];
