"use client";

import { useState, useEffect } from "react";
import {
  listFlowDefinitions,
  getFlowDefinition,
  saveFlowDesign,
  type FlowDefinitionDto,
  type FlowDefinitionDetailDto,
  type StateDto,
  type TransitionDto,
} from "@/lib/api/state-flow";

const ACTION_TYPES = [
  { type: "SendNotification", label: "📨 Bildirim Gönder", fields: [
    { key: "channel", label: "Kanal", options: ["InApp", "Email", "Both"] },
    { key: "recipientRole", label: "Alıcı Rol", options: ["Assignee", "ProjectManager", "Reporter", "TeamLead"] },
    { key: "template", label: "Şablon", type: "text" as const },
  ]},
  { type: "AddComment", label: "💬 Yorum Ekle", fields: [
    { key: "content", label: "Yorum İçeriği", type: "textarea" as const },
  ]},
  { type: "AssignWorkItem", label: "👤 Atama Yap", fields: [
    { key: "role", label: "Hedef Rol", options: ["ProjectManager", "TeamLead", "Developer", "QA"] },
  ]},
  { type: "ChangeStatus", label: "🔄 Durum Değiştir", fields: [
    { key: "status", label: "Hedef Durum", type: "text" as const },
    { key: "targetEntityType", label: "Entity Tipi", type: "text" as const },
  ]},
];

interface AddRuleModalProps {
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
}

type TargetType = "state" | "transition";

export function AddRuleModal({ open, onClose, onSaved }: AddRuleModalProps) {
  const [step, setStep] = useState(1);
  const [flowList, setFlowList] = useState<FlowDefinitionDto[]>([]);
  const [selectedFlowId, setSelectedFlowId] = useState("");
  const [flowDetail, setFlowDetail] = useState<FlowDefinitionDetailDto | null>(null);
  const [targetType, setTargetType] = useState<TargetType>("state");
  const [selectedTargetId, setSelectedTargetId] = useState("");
  const [selectedActionType, setSelectedActionType] = useState("");
  const [params, setParams] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  // Load draft flows
  useEffect(() => {
    if (open) {
      listFlowDefinitions().then(list => {
        const drafts = list.filter(f => f.status === "Draft");
        setFlowList(drafts);
      });
      // Reset
      setStep(1); setSelectedFlowId(""); setFlowDetail(null);
      setTargetType("state"); setSelectedTargetId("");
      setSelectedActionType(""); setParams({}); setError("");
    }
  }, [open]);

  // Load flow detail when selected
  useEffect(() => {
    if (selectedFlowId) {
      getFlowDefinition(selectedFlowId).then(setFlowDetail).catch(() => setFlowDetail(null));
    }
  }, [selectedFlowId]);

  // Init params when action type changes
  useEffect(() => {
    const at = ACTION_TYPES.find(a => a.type === selectedActionType);
    if (at) {
      const p: Record<string, string> = {};
      at.fields.forEach(f => { p[f.key] = ""; });
      setParams(p);
    }
  }, [selectedActionType]);

  const targets = flowDetail ? (
    targetType === "state"
      ? flowDetail.states.map(s => ({ id: s.id, label: `${s.label || s.name} (${s.name})` }))
      : flowDetail.transitions.map(t => ({ id: t.id, label: `${t.fromStateName} → ${t.toStateName} (${t.triggerName})` }))
  ) : [];

  const canNext = () => {
    if (step === 1) return !!selectedFlowId;
    if (step === 2) return !!selectedTargetId;
    if (step === 3) return !!selectedActionType;
    return true;
  };

  const handleSave = async () => {
    if (!flowDetail) return;
    setSaving(true); setError("");
    try {
      const newAction = { type: selectedActionType, params };
      let updatedStates = [...flowDetail.states];
      let updatedTransitions = [...flowDetail.transitions];

      if (targetType === "state") {
        updatedStates = updatedStates.map(s => {
          if (s.id !== selectedTargetId) return s;
          const existing = s.onEntryActions ? JSON.parse(s.onEntryActions) : [];
          existing.push(newAction);
          return { ...s, onEntryActions: JSON.stringify(existing) };
        });
      } else {
        updatedTransitions = updatedTransitions.map(t => {
          if (t.id !== selectedTargetId) return t;
          const existing = t.onTransitionActions ? JSON.parse(t.onTransitionActions) : [];
          existing.push(newAction);
          return { ...t, onTransitionActions: JSON.stringify(existing) };
        });
      }

      await saveFlowDesign(flowDetail.id, updatedStates, updatedTransitions);
      onSaved();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Kaydetme başarısız");
    } finally {
      setSaving(false);
    }
  };

  if (!open) return null;

  const actionTemplate = ACTION_TYPES.find(a => a.type === selectedActionType);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm" onClick={onClose}>
      <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-2xl w-full max-w-lg shadow-2xl" onClick={e => e.stopPropagation()}>
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
          <h2 className="text-lg font-semibold text-[var(--color-text)]">⚡ Yeni Otomasyon Kuralı</h2>
          <button onClick={onClose} className="text-[var(--color-text-muted)] hover:text-[var(--color-text)] text-xl">&times;</button>
        </div>

        {/* Steps indicator */}
        <div className="flex px-6 pt-4 gap-2">
          {[1,2,3,4].map(s => (
            <div key={s} className={`h-1 flex-1 rounded-full transition-colors ${s <= step ? "bg-violet-500" : "bg-white/10"}`} />
          ))}
        </div>

        {/* Content */}
        <div className="px-6 py-5 min-h-[220px]">
          {/* Step 1: Select Flow */}
          {step === 1 && (
            <div className="space-y-3">
              <label className="block text-sm font-medium text-[var(--color-text)]">Akış Seçin</label>
              <p className="text-xs text-[var(--color-text-muted)]">Sadece Draft durumundaki akışlara kural eklenebilir.</p>
              {flowList.length === 0 ? (
                <p className="text-sm text-amber-400 mt-4">⚠ Draft durumunda akış bulunamadı. Önce bir akış oluşturun veya mevcut akışın yeni versiyonunu açın.</p>
              ) : (
                <select className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2.5 text-sm text-[var(--color-text)] focus:outline-none focus:border-violet-500" value={selectedFlowId} onChange={e => setSelectedFlowId(e.target.value)}>
                  <option value="">Akış seçin...</option>
                  {flowList.map(f => <option key={f.id} value={f.id}>{f.name} ({f.entityType} · v{f.version})</option>)}
                </select>
              )}
            </div>
          )}

          {/* Step 2: Select Target */}
          {step === 2 && flowDetail && (
            <div className="space-y-3">
              <label className="block text-sm font-medium text-[var(--color-text)]">Hedef Seçin</label>
              <div className="flex gap-2 mb-3">
                {(["state", "transition"] as TargetType[]).map(t => (
                  <button key={t} onClick={() => { setTargetType(t); setSelectedTargetId(""); }}
                    className={`flex-1 py-2 rounded-lg text-sm font-medium transition-all ${targetType === t ? "bg-violet-500/20 text-violet-400 border border-violet-500/40" : "bg-white/5 text-[var(--color-text-muted)] border border-transparent hover:bg-white/10"}`}>
                    {t === "state" ? "⚡ State Girişi" : "🔗 Geçiş"}
                  </button>
                ))}
              </div>
              <select className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2.5 text-sm text-[var(--color-text)] focus:outline-none focus:border-violet-500" value={selectedTargetId} onChange={e => setSelectedTargetId(e.target.value)}>
                <option value="">{targetType === "state" ? "State seçin..." : "Geçiş seçin..."}</option>
                {targets.map(t => <option key={t.id} value={t.id}>{t.label}</option>)}
              </select>
            </div>
          )}

          {/* Step 3: Select Action */}
          {step === 3 && (
            <div className="space-y-3">
              <label className="block text-sm font-medium text-[var(--color-text)]">Aksiyon Tipi</label>
              <div className="grid grid-cols-2 gap-2">
                {ACTION_TYPES.map(at => (
                  <button key={at.type} onClick={() => setSelectedActionType(at.type)}
                    className={`p-3 rounded-lg text-left text-sm transition-all border ${selectedActionType === at.type ? "bg-violet-500/15 border-violet-500/40 text-violet-300" : "bg-white/5 border-transparent text-[var(--color-text-muted)] hover:bg-white/10"}`}>
                    {at.label}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Step 4: Configure Params */}
          {step === 4 && actionTemplate && (
            <div className="space-y-3">
              <label className="block text-sm font-medium text-[var(--color-text)]">{actionTemplate.label} — Parametreler</label>
              {actionTemplate.fields.map(field => (
                <div key={field.key} className="space-y-1">
                  <label className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">{field.label}</label>
                  {"options" in field && field.options ? (
                    <select className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-violet-500" value={params[field.key] || ""} onChange={e => setParams(p => ({ ...p, [field.key]: e.target.value }))}>
                      <option value="">Seçiniz...</option>
                      {field.options.map(o => <option key={o} value={o}>{o}</option>)}
                    </select>
                  ) : field.type === "textarea" ? (
                    <textarea className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-violet-500 resize-none" rows={3} value={params[field.key] || ""} onChange={e => setParams(p => ({ ...p, [field.key]: e.target.value }))} />
                  ) : (
                    <input className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-violet-500" value={params[field.key] || ""} onChange={e => setParams(p => ({ ...p, [field.key]: e.target.value }))} />
                  )}
                </div>
              ))}
            </div>
          )}

          {error && <p className="text-sm text-red-400 mt-3">{error}</p>}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between px-6 py-4 border-t border-[var(--color-border)]">
          <button onClick={() => step > 1 ? setStep(step - 1) : onClose()}
            className="px-4 py-2 text-sm text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-colors">
            {step > 1 ? "← Geri" : "İptal"}
          </button>
          {step < 4 ? (
            <button onClick={() => setStep(step + 1)} disabled={!canNext()}
              className="px-5 py-2 rounded-lg text-sm font-medium bg-violet-600 text-white hover:bg-violet-500 disabled:opacity-40 disabled:cursor-not-allowed transition-all">
              İleri →
            </button>
          ) : (
            <button onClick={handleSave} disabled={saving}
              className="px-5 py-2 rounded-lg text-sm font-medium bg-gradient-to-r from-violet-600 to-purple-600 text-white hover:opacity-90 disabled:opacity-40 transition-all">
              {saving ? "Kaydediliyor..." : "✓ Kuralı Kaydet"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
