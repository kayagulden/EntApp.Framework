"use client";

import { useStateFlowDesignerStore } from "@/stores/state-flow-store";

const CATEGORIES = ["Active", "Waiting", "Closed", "Cancelled"];
const COLORS = [
  { label: "Gray", value: "#6b7280" },
  { label: "Blue", value: "#3b82f6" },
  { label: "Green", value: "#22c55e" },
  { label: "Amber", value: "#f59e0b" },
  { label: "Red", value: "#ef4444" },
  { label: "Purple", value: "#8b5cf6" },
  { label: "Cyan", value: "#06b6d4" },
  { label: "Pink", value: "#ec4899" },
];

export function PropertiesPanel() {
  const { selectedElement, states, transitions, updateState, updateTransition, removeState, removeTransition, flowStatus } =
    useStateFlowDesignerStore();

  const isReadOnly = flowStatus !== "Draft";

  if (!selectedElement) {
    return (
      <div className="properties-panel properties-panel--empty">
        <div className="properties-panel__placeholder">
          <span className="properties-panel__icon">🎯</span>
          <p>Düzenlemek için bir state veya geçiş seçin</p>
        </div>
      </div>
    );
  }

  if (selectedElement.type === "state") {
    const state = states.find((s) => s.id === selectedElement.id);
    if (!state) return null;

    return (
      <div className="properties-panel">
        <h3 className="properties-panel__title">
          <span className="properties-panel__dot" style={{ backgroundColor: state.color }} />
          State Özellikleri
        </h3>

        <div className="properties-panel__group">
          <label className="prop-label">Ad (Name)</label>
          <input
            className="prop-input"
            value={state.name}
            disabled={isReadOnly}
            onChange={(e) => updateState(state.id, { name: e.target.value })}
          />
        </div>

        <div className="properties-panel__group">
          <label className="prop-label">Etiket (Label)</label>
          <input
            className="prop-input"
            value={state.label}
            disabled={isReadOnly}
            onChange={(e) => updateState(state.id, { label: e.target.value })}
          />
        </div>

        <div className="properties-panel__group">
          <label className="prop-label">Renk</label>
          <div className="color-picker">
            {COLORS.map((c) => (
              <button
                key={c.value}
                className={`color-swatch ${state.color === c.value ? "color-swatch--active" : ""}`}
                style={{ backgroundColor: c.value }}
                title={c.label}
                disabled={isReadOnly}
                onClick={() => updateState(state.id, { color: c.value })}
              />
            ))}
          </div>
        </div>

        <div className="properties-panel__group">
          <label className="prop-label">Kategori</label>
          <select
            className="prop-select"
            value={state.category}
            disabled={isReadOnly}
            onChange={(e) => updateState(state.id, { category: e.target.value })}
          >
            {CATEGORIES.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>

        <div className="properties-panel__flags">
          <label className="flag-checkbox">
            <input
              type="checkbox"
              checked={state.isInitial}
              disabled={isReadOnly}
              onChange={(e) => updateState(state.id, { isInitial: e.target.checked })}
            />
            <span className="flag-label">▶ Başlangıç (Initial)</span>
          </label>
          <label className="flag-checkbox">
            <input
              type="checkbox"
              checked={state.isTerminal}
              disabled={isReadOnly}
              onChange={(e) => updateState(state.id, { isTerminal: e.target.checked })}
            />
            <span className="flag-label">■ Bitiş (Terminal)</span>
          </label>
          <label className="flag-checkbox">
            <input
              type="checkbox"
              checked={state.isPaused}
              disabled={isReadOnly}
              onChange={(e) => updateState(state.id, { isPaused: e.target.checked })}
            />
            <span className="flag-label">⏸ Beklemede (Paused)</span>
          </label>
        </div>

        {!isReadOnly && (
          <button
            className="prop-btn prop-btn--danger"
            onClick={() => removeState(state.id)}
          >
            State&apos;i Sil
          </button>
        )}
      </div>
    );
  }

  // Transition selected
  const transition = transitions.find((t) => t.id === selectedElement.id);
  if (!transition) return null;

  return (
    <div className="properties-panel">
      <h3 className="properties-panel__title">
        <span className="properties-panel__dot" style={{ backgroundColor: "#8b5cf6" }} />
        Geçiş Özellikleri
      </h3>

      <div className="properties-panel__group">
        <label className="prop-label">Tetikleyici (Trigger)</label>
        <input
          className="prop-input"
          value={transition.triggerName}
          disabled={isReadOnly}
          onChange={(e) => updateTransition(transition.id, { triggerName: e.target.value })}
        />
      </div>

      <div className="properties-panel__group">
        <label className="prop-label">Etiket (Label)</label>
        <input
          className="prop-input"
          value={transition.label}
          disabled={isReadOnly}
          onChange={(e) => updateTransition(transition.id, { label: e.target.value })}
        />
      </div>

      <div className="properties-panel__info">
        <span>{transition.fromStateName}</span>
        <span className="properties-panel__arrow">→</span>
        <span>{transition.toStateName}</span>
      </div>

      <div className="properties-panel__group">
        <label className="prop-label">Gerekli Rol (Opsiyonel)</label>
        <input
          className="prop-input"
          value={transition.requiredRole || ""}
          placeholder="ör: Agent, Manager"
          disabled={isReadOnly}
          onChange={(e) =>
            updateTransition(transition.id, {
              requiredRole: e.target.value || null,
            })
          }
        />
      </div>

      {!isReadOnly && (
        <button
          className="prop-btn prop-btn--danger"
          onClick={() => removeTransition(transition.id)}
        >
          Geçişi Sil
        </button>
      )}
    </div>
  );
}
