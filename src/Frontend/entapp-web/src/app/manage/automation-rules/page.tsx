"use client";

import { useState, useEffect, useMemo } from "react";
import {
  listFlowDefinitions,
  getFlowDefinition,
  listExecutionLogs,
  getActionTypes,
  type FlowDefinitionDetailDto,
  type RuleExecutionLogDto,
  type ActionTypeInfo,
} from "@/lib/api/state-flow";

// ── Helpers ──────────────────────────────────────────────────

interface ActionSummary {
  flowId: string;
  flowName: string;
  entityType: string;
  source: "OnEntry" | "OnTransition";
  location: string; // state name or "FromState → ToState"
  actionType: string;
  actionLabel: string;
  params: Record<string, string>;
}

function parseActionsFromFlow(flow: FlowDefinitionDetailDto): ActionSummary[] {
  const actions: ActionSummary[] = [];

  // Parse OnEntryActions from states
  for (const state of flow.states) {
    if (state.onEntryActions) {
      try {
        const entries = JSON.parse(state.onEntryActions);
        for (const entry of entries) {
          actions.push({
            flowId: flow.id,
            flowName: flow.name,
            entityType: flow.entityType,
            source: "OnEntry",
            location: state.label || state.name,
            actionType: entry.type,
            actionLabel: entry.type,
            params: entry.params || {},
          });
        }
      } catch { /* ignore parse errors */ }
    }
  }

  // Parse OnTransitionActions from transitions
  for (const transition of flow.transitions) {
    if (transition.onTransitionActions) {
      try {
        const entries = JSON.parse(transition.onTransitionActions);
        for (const entry of entries) {
          actions.push({
            flowId: flow.id,
            flowName: flow.name,
            entityType: flow.entityType,
            source: "OnTransition",
            location: `${transition.fromStateName} → ${transition.toStateName}`,
            actionType: entry.type,
            actionLabel: entry.type,
            params: entry.params || {},
          });
        }
      } catch { /* ignore parse errors */ }
    }
  }

  return actions;
}

// ── Action Type Label Map ────────────────────────────────────
const ACTION_ICONS: Record<string, string> = {
  SendNotification: "📨",
  AddComment: "💬",
  AssignWorkItem: "👤",
  ChangeStatus: "🔄",
};

const ACTION_LABELS: Record<string, string> = {
  SendNotification: "Bildirim Gönder",
  AddComment: "Yorum Ekle",
  AssignWorkItem: "Atama Yap",
  ChangeStatus: "Durum Değiştir",
};

// ── Tab Type ─────────────────────────────────────────────────
type TabId = "rules" | "logs" | "action-types";

export default function AutomationRulesPage() {
  const [activeTab, setActiveTab] = useState<TabId>("rules");
  const [flows, setFlows] = useState<FlowDefinitionDetailDto[]>([]);
  const [logs, setLogs] = useState<RuleExecutionLogDto[]>([]);
  const [actionTypes, setActionTypes] = useState<ActionTypeInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [logFilter, setLogFilter] = useState<{ flowId?: string; limit: number }>({ limit: 50 });

  // Load data
  useEffect(() => {
    (async () => {
      setLoading(true);
      try {
        const [flowList, logsData, atData] = await Promise.all([
          listFlowDefinitions(),
          listExecutionLogs(undefined, undefined, logFilter.limit),
          getActionTypes(),
        ]);

        // Fetch details for each flow to get states/transitions with actions
        const flowDetails = await Promise.all(
          flowList.map(f => getFlowDefinition(f.id).catch(() => null))
        );

        setFlows(flowDetails.filter((f): f is FlowDefinitionDetailDto => f !== null));
        setLogs(logsData);
        setActionTypes(atData);
      } catch (err) {
        console.error("Automation Rules load error:", err);
      } finally {
        setLoading(false);
      }
    })();
  }, [logFilter]);

  // Parse all actions from all flows
  const allActions = useMemo(() => {
    return flows.flatMap(parseActionsFromFlow);
  }, [flows]);

  // Stats
  const stats = useMemo(() => ({
    totalRules: allActions.length,
    totalFlows: new Set(allActions.map(a => a.flowId)).size,
    totalExecutions: logs.length,
    successRate: logs.length > 0
      ? Math.round((logs.filter(l => l.success).length / logs.length) * 100)
      : 100,
  }), [allActions, logs]);

  const tabs: { id: TabId; label: string; icon: string; count?: number }[] = [
    { id: "rules", label: "Tanımlı Kurallar", icon: "⚡", count: allActions.length },
    { id: "logs", label: "Çalışma Geçmişi", icon: "📋", count: logs.length },
    { id: "action-types", label: "Aksiyon Tipleri", icon: "🧩", count: actionTypes.length },
  ];

  return (
    <div className="space-y-6">
      {/* ── Header ─────────────────────────────────────── */}
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">
          ⚡ Otomasyon Kuralları
        </h1>
        <p className="text-sm text-[var(--color-text-muted)] mt-1">
          StateFlow akışlarına bağlı otomasyon kurallarını görüntüleyin ve yönetin.
        </p>
      </div>

      {/* ── Stats Cards ────────────────────────────────── */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
        {[
          { label: "Toplam Kural", value: stats.totalRules, color: "violet", icon: "⚡" },
          { label: "Akış Sayısı", value: stats.totalFlows, color: "cyan", icon: "🔗" },
          { label: "Çalışma Sayısı", value: stats.totalExecutions, color: "teal", icon: "📋" },
          { label: "Başarı Oranı", value: `%${stats.successRate}`, color: "emerald", icon: "✅" },
        ].map((card) => (
          <div
            key={card.label}
            className="relative overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5 transition-all duration-200 hover:shadow-lg"
          >
            <div className="flex items-start justify-between">
              <div>
                <p className="text-sm text-[var(--color-text-muted)]">{card.label}</p>
                <p className="mt-2 text-2xl font-bold text-[var(--color-text)]">{card.value}</p>
              </div>
              <div className={`bg-${card.color}-500/10 p-2.5 rounded-xl text-xl`}>
                {card.icon}
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* ── Tabs ───────────────────────────────────────── */}
      <div className="flex gap-1 border-b border-[var(--color-border)]">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`
              flex items-center gap-2 px-4 py-3 text-sm font-medium transition-all duration-200 border-b-2 -mb-px
              ${activeTab === tab.id
                ? "border-violet-500 text-violet-400"
                : "border-transparent text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:border-white/20"
              }
            `}
          >
            <span>{tab.icon}</span>
            <span>{tab.label}</span>
            {tab.count !== undefined && (
              <span className={`
                text-xs px-1.5 py-0.5 rounded-full font-semibold
                ${activeTab === tab.id ? "bg-violet-500/20 text-violet-300" : "bg-white/5 text-[var(--color-text-muted)]"}
              `}>
                {tab.count}
              </span>
            )}
          </button>
        ))}
      </div>

      {/* ── Content ────────────────────────────────────── */}
      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="flex items-center gap-3 text-[var(--color-text-muted)]">
            <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            Yükleniyor...
          </div>
        </div>
      ) : (
        <>
          {/* ── TAB: Rules ─────────────────────────────── */}
          {activeTab === "rules" && (
            <div className="space-y-3">
              {allActions.length === 0 ? (
                <div className="text-center py-16 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)]">
                  <p className="text-4xl mb-3">⚡</p>
                  <p className="text-[var(--color-text-muted)] text-sm">
                    Henüz otomasyon kuralı tanımlanmamış.
                  </p>
                  <p className="text-[var(--color-text-muted)] text-xs mt-1">
                    StateFlow Designer&apos;da state veya transition üzerine aksiyon ekleyebilirsiniz.
                  </p>
                  <a
                    href="/manage/state-flows"
                    className="inline-block mt-4 text-sm text-violet-400 hover:text-violet-300 transition-colors"
                  >
                    → Durum Akışlarına Git
                  </a>
                </div>
              ) : (
                <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="bg-white/[0.02] border-b border-[var(--color-border)]">
                        <th className="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Akış</th>
                        <th className="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Kaynak</th>
                        <th className="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Konum</th>
                        <th className="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Aksiyon</th>
                        <th className="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Parametreler</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-[var(--color-border)]">
                      {allActions.map((action, idx) => (
                        <tr key={idx} className="hover:bg-white/[0.02] transition-colors">
                          <td className="px-4 py-3">
                            <a href={`/manage/state-flows/${action.flowId}`} className="text-violet-400 hover:text-violet-300 font-medium transition-colors">
                              {action.flowName}
                            </a>
                            <p className="text-xs text-[var(--color-text-muted)]">{action.entityType}</p>
                          </td>
                          <td className="px-4 py-3">
                            <span className={`
                              inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium
                              ${action.source === "OnEntry"
                                ? "bg-amber-500/10 text-amber-400"
                                : "bg-cyan-500/10 text-cyan-400"
                              }
                            `}>
                              {action.source === "OnEntry" ? "⚡ Giriş" : "🔗 Geçiş"}
                            </span>
                          </td>
                          <td className="px-4 py-3 text-[var(--color-text)]">{action.location}</td>
                          <td className="px-4 py-3">
                            <span className="inline-flex items-center gap-1.5">
                              <span>{ACTION_ICONS[action.actionType] || "⚙️"}</span>
                              <span className="text-[var(--color-text)]">{ACTION_LABELS[action.actionType] || action.actionType}</span>
                            </span>
                          </td>
                          <td className="px-4 py-3 text-xs text-[var(--color-text-muted)] font-mono">
                            {Object.entries(action.params).filter(([,v]) => v).map(([k, v]) => (
                              <span key={k} className="inline-block mr-2 bg-white/5 px-1.5 py-0.5 rounded">
                                {k}: {v}
                              </span>
                            ))}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {/* ── TAB: Logs ──────────────────────────────── */}
          {activeTab === "logs" && (
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <select
                    className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-violet-500"
                    value={logFilter.limit}
                    onChange={(e) => setLogFilter(f => ({ ...f, limit: Number(e.target.value) }))}
                  >
                    <option value={25}>Son 25</option>
                    <option value={50}>Son 50</option>
                    <option value={100}>Son 100</option>
                  </select>
                </div>
              </div>

              {logs.length === 0 ? (
                <div className="text-center py-16 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)]">
                  <p className="text-4xl mb-3">📋</p>
                  <p className="text-[var(--color-text-muted)] text-sm">Henüz çalışma kaydı yok.</p>
                  <p className="text-[var(--color-text-muted)] text-xs mt-1">
                    StateFlow üzerinden durum geçişi yapıldığında kayıtlar burada görünecek.
                  </p>
                </div>
              ) : (
                <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="bg-white/[0.02] border-b border-[var(--color-border)]">
                        <th className="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Tarih</th>
                        <th className="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Kaynak</th>
                        <th className="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">State</th>
                        <th className="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Aksiyon</th>
                        <th className="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Süre</th>
                        <th className="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Durum</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-[var(--color-border)]">
                      {logs.map((log) => (
                        <tr key={log.id} className="hover:bg-white/[0.02] transition-colors">
                          <td className="px-4 py-3 text-[var(--color-text-muted)] text-xs whitespace-nowrap">
                            {new Date(log.createdAt).toLocaleString("tr-TR")}
                          </td>
                          <td className="px-4 py-3">
                            <span className={`
                              inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium
                              ${log.source === "OnEntry" ? "bg-amber-500/10 text-amber-400" : "bg-cyan-500/10 text-cyan-400"}
                            `}>
                              {log.source}
                            </span>
                          </td>
                          <td className="px-4 py-3 text-[var(--color-text)]">
                            {log.stateName}
                            {log.triggerName && (
                              <span className="text-xs text-[var(--color-text-muted)] ml-1">
                                ({log.triggerName})
                              </span>
                            )}
                          </td>
                          <td className="px-4 py-3">
                            <span className="inline-flex items-center gap-1.5">
                              <span>{ACTION_ICONS[log.actionType] || "⚙️"}</span>
                              <span className="text-[var(--color-text)]">{ACTION_LABELS[log.actionType] || log.actionType}</span>
                            </span>
                          </td>
                          <td className="px-4 py-3 text-[var(--color-text-muted)] text-xs">
                            {log.durationMs}ms
                          </td>
                          <td className="px-4 py-3">
                            {log.success ? (
                              <span className="inline-flex items-center gap-1 text-xs text-emerald-400">
                                <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
                                Başarılı
                              </span>
                            ) : (
                              <span className="inline-flex items-center gap-1 text-xs text-red-400" title={log.errorMessage || ""}>
                                <span className="w-1.5 h-1.5 rounded-full bg-red-400" />
                                Hata
                              </span>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {/* ── TAB: Action Types ──────────────────────── */}
          {activeTab === "action-types" && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {actionTypes.map((at) => (
                <div
                  key={at.type}
                  className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5 hover:border-violet-500/30 transition-all duration-200"
                >
                  <div className="flex items-start gap-3">
                    <div className="text-2xl">{ACTION_ICONS[at.type] || "⚙️"}</div>
                    <div className="flex-1 min-w-0">
                      <h3 className="text-sm font-semibold text-[var(--color-text)]">{at.label}</h3>
                      <p className="text-xs text-[var(--color-text-muted)] mt-1">{at.description}</p>
                      <div className="mt-3 flex flex-wrap gap-1.5">
                        {at.paramFields.map((field) => {
                          const [key, type] = field.split(": ");
                          return (
                            <span
                              key={field}
                              className="inline-flex items-center gap-1 bg-white/5 border border-white/10 rounded-md px-2 py-0.5 text-xs"
                            >
                              <span className="text-violet-400 font-medium">{key}</span>
                              <span className="text-[var(--color-text-muted)]">{type}</span>
                            </span>
                          );
                        })}
                      </div>
                      <p className="mt-2 text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider font-semibold">
                        Tip: <code className="text-violet-400 bg-violet-500/10 px-1 py-0.5 rounded">{at.type}</code>
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
}
