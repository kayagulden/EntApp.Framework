"use client";

import { useState, useEffect } from "react";
import { createEventRule } from "@/lib/api/state-flow";

type ConditionField = { key: string; label: string; options?: string[]; type?: "number" | "text" };
type ActionField = { key: string; label: string; options?: string[]; type?: "text" | "textarea" };

const TRIGGER_TYPES: { type: string; label: string; icon: string; description: string; conditionFields: ConditionField[] }[] = [
  { type: "SLAResponseBreached", label: "SLA Yanıt Süresi Aşımı", icon: "🕐", description: "SLA yanıt süresi aşıldığında tetiklenir", conditionFields: [
    { key: "priority", label: "Öncelik Filtresi", options: ["", "Critical", "Urgent", "High", "Medium", "Low"] },
  ]},
  { type: "SLAResolutionBreached", label: "SLA Çözüm Süresi Aşımı", icon: "⏰", description: "SLA çözüm süresi aşıldığında tetiklenir", conditionFields: [
    { key: "priority", label: "Öncelik Filtresi", options: ["", "Critical", "Urgent", "High", "Medium", "Low"] },
  ]},
  { type: "TicketIdleTimeout", label: "Ticket Bekleme Zaman Aşımı", icon: "💤", description: "Ticket belirli süre atanmadan beklediğinde", conditionFields: [
    { key: "idleMinutes", label: "Bekleme Süresi (dk)", type: "number" },
  ]},
  { type: "PriorityChanged", label: "Öncelik Değişikliği", icon: "🔺", description: "Entity önceliği değiştiğinde tetiklenir", conditionFields: [
    { key: "toPriority", label: "Yeni Öncelik", options: ["", "Critical", "Urgent", "High", "Medium", "Low"] },
  ]},
  { type: "AssignmentChanged", label: "Atama Değişikliği", icon: "👤", description: "Entity ataması değiştiğinde tetiklenir", conditionFields: [] },
  { type: "EntityCreated", label: "Entity Oluşturuldu", icon: "✨", description: "Yeni entity oluşturulduğunda tetiklenir", conditionFields: [] },
  { type: "EntityUpdated", label: "Entity Güncellendi", icon: "📝", description: "Entity güncellendiğinde tetiklenir", conditionFields: [] },
  { type: "CommentAdded", label: "Yorum Eklendi", icon: "💬", description: "Entity'ye yorum eklendiğinde tetiklenir", conditionFields: [] },
];

const ACTION_TYPES: { type: string; label: string; fields: ActionField[] }[] = [
  { type: "SendNotification", label: "📨 Bildirim Gönder", fields: [
    { key: "channel", label: "Kanal", options: ["InApp", "Email", "Both"] },
    { key: "recipientRole", label: "Alıcı Rol", options: ["Assignee", "ProjectManager", "Reporter", "TeamLead"] },
    { key: "template", label: "Şablon", type: "text" },
  ]},
  { type: "AddComment", label: "💬 Yorum Ekle", fields: [
    { key: "content", label: "Yorum İçeriği", type: "textarea" },
  ]},
  { type: "AssignWorkItem", label: "👤 Atama Yap", fields: [
    { key: "role", label: "Hedef Rol", options: ["ProjectManager", "TeamLead", "Developer", "QA"] },
  ]},
  { type: "ChangeStatus", label: "🔄 Durum Değiştir", fields: [
    { key: "status", label: "Hedef Durum", type: "text" },
  ]},
];

interface AddEventRuleModalProps {
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
}

export function AddEventRuleModal({ open, onClose, onSaved }: AddEventRuleModalProps) {
  const [step, setStep] = useState(1);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [selectedTrigger, setSelectedTrigger] = useState("");
  const [triggerConditions, setTriggerConditions] = useState<Record<string, string>>({});
  const [entityType, setEntityType] = useState("");
  const [selectedAction, setSelectedAction] = useState("");
  const [actionParams, setActionParams] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (open) {
      setStep(1); setName(""); setDescription(""); setSelectedTrigger("");
      setTriggerConditions({}); setEntityType(""); setSelectedAction("");
      setActionParams({}); setError("");
    }
  }, [open]);

  // Init condition fields when trigger changes
  useEffect(() => {
    const t = TRIGGER_TYPES.find(t => t.type === selectedTrigger);
    if (t) {
      const c: Record<string, string> = {};
      t.conditionFields.forEach(f => { c[f.key] = ""; });
      setTriggerConditions(c);
    }
  }, [selectedTrigger]);

  // Init action params when action changes
  useEffect(() => {
    const a = ACTION_TYPES.find(a => a.type === selectedAction);
    if (a) {
      const p: Record<string, string> = {};
      a.fields.forEach(f => { p[f.key] = ""; });
      setActionParams(p);
    }
  }, [selectedAction]);

  const canNext = () => {
    if (step === 1) return !!selectedTrigger;
    if (step === 2) return !!selectedAction;
    if (step === 3) return name.trim().length > 0;
    return true;
  };

  const handleSave = async () => {
    setSaving(true); setError("");
    try {
      // Clean empty values
      const cleanConditions = Object.fromEntries(Object.entries(triggerConditions).filter(([,v]) => v));
      const cleanParams = Object.fromEntries(Object.entries(actionParams).filter(([,v]) => v));

      await createEventRule({
        name: name.trim(),
        description: description.trim() || undefined,
        triggerType: selectedTrigger,
        triggerConditions: JSON.stringify(cleanConditions),
        actionType: selectedAction,
        actionParams: JSON.stringify(cleanParams),
        entityType: entityType || undefined,
      });
      onSaved();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Kaydetme başarısız");
    } finally {
      setSaving(false);
    }
  };

  if (!open) return null;

  const triggerTemplate = TRIGGER_TYPES.find(t => t.type === selectedTrigger);
  const actionTemplate = ACTION_TYPES.find(a => a.type === selectedAction);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm" onClick={onClose}>
      <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-2xl w-full max-w-lg shadow-2xl" onClick={e => e.stopPropagation()}>
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
          <h2 className="text-lg font-semibold text-[var(--color-text)]">🎯 Yeni Event Kuralı</h2>
          <button onClick={onClose} className="text-[var(--color-text-muted)] hover:text-[var(--color-text)] text-xl">&times;</button>
        </div>

        {/* Steps */}
        <div className="flex px-6 pt-4 gap-2">
          {[1,2,3].map(s => (
            <div key={s} className={`h-1 flex-1 rounded-full transition-colors ${s <= step ? "bg-emerald-500" : "bg-white/10"}`} />
          ))}
        </div>

        {/* Content */}
        <div className="px-6 py-5 min-h-[260px]">
          {/* Step 1: Select Trigger */}
          {step === 1 && (
            <div className="space-y-3">
              <label className="block text-sm font-medium text-[var(--color-text)]">Tetikleyici Seçin</label>
              <p className="text-xs text-[var(--color-text-muted)]">Kuralın ne zaman çalışacağını belirleyen olay.</p>
              <div className="grid grid-cols-2 gap-2 max-h-[240px] overflow-y-auto pr-1">
                {TRIGGER_TYPES.map(t => (
                  <button key={t.type} onClick={() => setSelectedTrigger(t.type)}
                    className={`p-3 rounded-lg text-left text-sm transition-all border ${selectedTrigger === t.type ? "bg-emerald-500/15 border-emerald-500/40 text-emerald-300" : "bg-white/5 border-transparent text-[var(--color-text-muted)] hover:bg-white/10"}`}>
                    <span className="text-base mr-1">{t.icon}</span>
                    <span className="font-medium">{t.label}</span>
                    <p className="text-[10px] mt-1 opacity-70">{t.description}</p>
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Step 2: Select Action */}
          {step === 2 && (
            <div className="space-y-3">
              <label className="block text-sm font-medium text-[var(--color-text)]">Aksiyon Seçin</label>
              <p className="text-xs text-[var(--color-text-muted)]">Tetiklendiğinde ne yapılacak.</p>
              <div className="grid grid-cols-2 gap-2">
                {ACTION_TYPES.map(a => (
                  <button key={a.type} onClick={() => setSelectedAction(a.type)}
                    className={`p-3 rounded-lg text-left text-sm transition-all border ${selectedAction === a.type ? "bg-emerald-500/15 border-emerald-500/40 text-emerald-300" : "bg-white/5 border-transparent text-[var(--color-text-muted)] hover:bg-white/10"}`}>
                    {a.label}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Step 3: Configure */}
          {step === 3 && (
            <div className="space-y-4 max-h-[320px] overflow-y-auto pr-1">
              <div className="space-y-1">
                <label className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Kural Adı *</label>
                <input className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-emerald-500" value={name} onChange={e => setName(e.target.value)} placeholder="Örn: SLA yanıt süresi aşımı bildirimi" />
              </div>
              <div className="space-y-1">
                <label className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Açıklama</label>
                <textarea className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-emerald-500 resize-none" rows={2} value={description} onChange={e => setDescription(e.target.value)} />
              </div>
              <div className="space-y-1">
                <label className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Entity Tipi (opsiyonel)</label>
                <select className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-emerald-500" value={entityType} onChange={e => setEntityType(e.target.value)}>
                  <option value="">Tümü</option>
                  <option value="Ticket">Ticket</option>
                  <option value="WorkItem">WorkItem</option>
                </select>
              </div>

              {/* Trigger Conditions */}
              {triggerTemplate && triggerTemplate.conditionFields.length > 0 && (
                <div className="border-t border-[var(--color-border)] pt-3">
                  <p className="text-xs font-semibold text-amber-400 mb-2">{triggerTemplate.icon} Tetikleyici Koşulları</p>
                  {triggerTemplate.conditionFields.map(f => (
                    <div key={f.key} className="space-y-1 mb-2">
                      <label className="text-xs text-[var(--color-text-muted)]">{f.label}</label>
                      {"options" in f && f.options ? (
                        <select className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-emerald-500" value={triggerConditions[f.key] || ""} onChange={e => setTriggerConditions(p => ({ ...p, [f.key]: e.target.value }))}>
                          {f.options.map(o => <option key={o} value={o}>{o || "Tümü"}</option>)}
                        </select>
                      ) : (
                        <input type={f.type === "number" ? "number" : "text"} className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-emerald-500" value={triggerConditions[f.key] || ""} onChange={e => setTriggerConditions(p => ({ ...p, [f.key]: e.target.value }))} />
                      )}
                    </div>
                  ))}
                </div>
              )}

              {/* Action Params */}
              {actionTemplate && (
                <div className="border-t border-[var(--color-border)] pt-3">
                  <p className="text-xs font-semibold text-violet-400 mb-2">{actionTemplate.label} Parametreleri</p>
                  {actionTemplate.fields.map(f => (
                    <div key={f.key} className="space-y-1 mb-2">
                      <label className="text-xs text-[var(--color-text-muted)]">{f.label}</label>
                      {"options" in f && f.options ? (
                        <select className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-emerald-500" value={actionParams[f.key] || ""} onChange={e => setActionParams(p => ({ ...p, [f.key]: e.target.value }))}>
                          <option value="">Seçiniz...</option>
                          {f.options.map(o => <option key={o} value={o}>{o}</option>)}
                        </select>
                      ) : f.type === "textarea" ? (
                        <textarea className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-emerald-500 resize-none" rows={2} value={actionParams[f.key] || ""} onChange={e => setActionParams(p => ({ ...p, [f.key]: e.target.value }))} />
                      ) : (
                        <input className="w-full bg-black/30 border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm text-[var(--color-text)] focus:outline-none focus:border-emerald-500" value={actionParams[f.key] || ""} onChange={e => setActionParams(p => ({ ...p, [f.key]: e.target.value }))} />
                      )}
                    </div>
                  ))}
                </div>
              )}
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
          {step < 3 ? (
            <button onClick={() => setStep(step + 1)} disabled={!canNext()}
              className="px-5 py-2 rounded-lg text-sm font-medium bg-emerald-600 text-white hover:bg-emerald-500 disabled:opacity-40 disabled:cursor-not-allowed transition-all">
              İleri →
            </button>
          ) : (
            <button onClick={handleSave} disabled={saving || !canNext()}
              className="px-5 py-2 rounded-lg text-sm font-medium bg-gradient-to-r from-emerald-600 to-teal-600 text-white hover:opacity-90 disabled:opacity-40 transition-all">
              {saving ? "Kaydediliyor..." : "✓ Kuralı Kaydet"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
