"use client";

import { useState, useMemo } from "react";
import type { StateDto, TransitionDto } from "@/lib/api/state-flow";

// ── Action Types ──────────────────────────────────────────────

interface ActionDefinition {
  type: string;
  params: Record<string, string>;
}

const ACTION_TYPES = [
  {
    type: "SendNotification",
    label: "📨 Bildirim Gönder",
    description: "InApp veya Email bildirim gönderir",
    fields: [
      { key: "channel", label: "Kanal", options: ["InApp", "Email", "Both"], default: "InApp" },
      { key: "recipientRole", label: "Alıcı", options: ["Assignee", "ProjectManager", "Reporter", "TeamLead"], default: "Assignee" },
      { key: "template", label: "Şablon", type: "text" as const, default: "state_transition" },
    ],
  },
  {
    type: "AddComment",
    label: "💬 Yorum Ekle",
    description: "Otomatik yorum ekler",
    fields: [
      { key: "content", label: "Yorum İçeriği", type: "textarea" as const, default: "" },
    ],
  },
  {
    type: "AssignWorkItem",
    label: "👤 Atama Yap",
    description: "Belirtilen kişiye atar",
    fields: [
      { key: "role", label: "Hedef Rol", options: ["ProjectManager", "TeamLead", "Developer", "QA"], default: "" },
    ],
  },
  {
    type: "ChangeStatus",
    label: "🔄 Durum Değiştir",
    description: "İlişkili entity durumunu değiştirir",
    fields: [
      { key: "status", label: "Hedef Durum", type: "text" as const, default: "" },
      { key: "targetEntityType", label: "Entity Tipi", type: "text" as const, default: "" },
    ],
  },
];

// ── Helpers ────────────────────────────────────────────────────

function parseActions(json: string | null): ActionDefinition[] {
  if (!json) return [];
  try {
    return JSON.parse(json);
  } catch {
    return [];
  }
}

function serializeActions(actions: ActionDefinition[]): string | null {
  if (actions.length === 0) return null;
  return JSON.stringify(actions);
}

// ── Component Props ───────────────────────────────────────────

interface ActionsEditorProps {
  actionsJson: string | null;
  onChange: (json: string | null) => void;
  source: "OnEntry" | "OnTransition";
  isReadOnly: boolean;
}

export function ActionsEditor({ actionsJson, onChange, source, isReadOnly }: ActionsEditorProps) {
  const [expandedIdx, setExpandedIdx] = useState<number | null>(null);
  const [showAdd, setShowAdd] = useState(false);
  const actions = useMemo(() => parseActions(actionsJson), [actionsJson]);

  const addAction = (type: string) => {
    const template = ACTION_TYPES.find((a) => a.type === type);
    if (!template) return;
    const params: Record<string, string> = {};
    template.fields.forEach((f) => {
      params[f.key] = f.default || "";
    });
    const newActions = [...actions, { type, params }];
    onChange(serializeActions(newActions));
    setShowAdd(false);
    setExpandedIdx(newActions.length - 1);
  };

  const removeAction = (idx: number) => {
    const newActions = actions.filter((_, i) => i !== idx);
    onChange(serializeActions(newActions));
    setExpandedIdx(null);
  };

  const updateActionParam = (idx: number, key: string, value: string) => {
    const newActions = actions.map((a, i) =>
      i === idx ? { ...a, params: { ...a.params, [key]: value } } : a
    );
    onChange(serializeActions(newActions));
  };

  const moveAction = (idx: number, dir: -1 | 1) => {
    const target = idx + dir;
    if (target < 0 || target >= actions.length) return;
    const newActions = [...actions];
    [newActions[idx], newActions[target]] = [newActions[target], newActions[idx]];
    onChange(serializeActions(newActions));
    setExpandedIdx(target);
  };

  const sourceLabel = source === "OnEntry" ? "State Giriş Aksiyonları" : "Geçiş Aksiyonları";
  const sourceIcon = source === "OnEntry" ? "⚡" : "🔗";

  return (
    <div className="actions-editor">
      <div className="actions-editor__header">
        <span className="actions-editor__title">
          {sourceIcon} {sourceLabel}
        </span>
        <span className="actions-editor__count">{actions.length}</span>
      </div>

      {actions.length === 0 && !showAdd && (
        <div className="actions-editor__empty">
          <p>Henüz aksiyon tanımlanmadı</p>
          {!isReadOnly && (
            <button className="actions-editor__add-btn" onClick={() => setShowAdd(true)}>
              + Aksiyon Ekle
            </button>
          )}
        </div>
      )}

      {actions.length > 0 && (
        <div className="actions-editor__list">
          {actions.map((action, idx) => {
            const template = ACTION_TYPES.find((a) => a.type === action.type);
            const isExpanded = expandedIdx === idx;
            return (
              <div
                key={idx}
                className={`action-item ${isExpanded ? "action-item--expanded" : ""}`}
              >
                <div
                  className="action-item__header"
                  onClick={() => setExpandedIdx(isExpanded ? null : idx)}
                >
                  <span className="action-item__label">
                    {template?.label || action.type}
                  </span>
                  <div className="action-item__controls">
                    {!isReadOnly && (
                      <>
                        <button
                          className="action-item__move"
                          onClick={(e) => { e.stopPropagation(); moveAction(idx, -1); }}
                          disabled={idx === 0}
                          title="Yukarı taşı"
                        >
                          ▲
                        </button>
                        <button
                          className="action-item__move"
                          onClick={(e) => { e.stopPropagation(); moveAction(idx, 1); }}
                          disabled={idx === actions.length - 1}
                          title="Aşağı taşı"
                        >
                          ▼
                        </button>
                      </>
                    )}
                    <span className="action-item__chevron">{isExpanded ? "▾" : "▸"}</span>
                  </div>
                </div>

                {isExpanded && template && (
                  <div className="action-item__body">
                    <p className="action-item__desc">{template.description}</p>
                    {template.fields.map((field) => (
                      <div key={field.key} className="action-item__field">
                        <label className="prop-label">{field.label}</label>
                        {"options" in field && field.options ? (
                          <select
                            className="prop-select"
                            value={action.params[field.key] || ""}
                            disabled={isReadOnly}
                            onChange={(e) => updateActionParam(idx, field.key, e.target.value)}
                          >
                            <option value="">Seçiniz...</option>
                            {field.options.map((opt) => (
                              <option key={opt} value={opt}>{opt}</option>
                            ))}
                          </select>
                        ) : field.type === "textarea" ? (
                          <textarea
                            className="prop-input action-item__textarea"
                            value={action.params[field.key] || ""}
                            placeholder="Otomatik yorum içeriği..."
                            disabled={isReadOnly}
                            rows={3}
                            onChange={(e) => updateActionParam(idx, field.key, e.target.value)}
                          />
                        ) : (
                          <input
                            className="prop-input"
                            value={action.params[field.key] || ""}
                            disabled={isReadOnly}
                            onChange={(e) => updateActionParam(idx, field.key, e.target.value)}
                          />
                        )}
                      </div>
                    ))}
                    {!isReadOnly && (
                      <button
                        className="prop-btn prop-btn--danger action-item__remove"
                        onClick={() => removeAction(idx)}
                      >
                        Aksiyonu Kaldır
                      </button>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Add action dropdown */}
      {showAdd && (
        <div className="actions-editor__dropdown">
          {ACTION_TYPES.map((at) => (
            <button
              key={at.type}
              className="actions-editor__dropdown-item"
              onClick={() => addAction(at.type)}
            >
              <span className="actions-editor__dropdown-label">{at.label}</span>
              <span className="actions-editor__dropdown-desc">{at.description}</span>
            </button>
          ))}
          <button
            className="actions-editor__dropdown-cancel"
            onClick={() => setShowAdd(false)}
          >
            İptal
          </button>
        </div>
      )}

      {actions.length > 0 && !isReadOnly && !showAdd && (
        <button className="actions-editor__add-btn actions-editor__add-btn--small" onClick={() => setShowAdd(true)}>
          + Aksiyon Ekle
        </button>
      )}
    </div>
  );
}
