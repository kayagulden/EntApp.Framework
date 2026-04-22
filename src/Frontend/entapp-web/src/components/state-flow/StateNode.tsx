"use client";

import { memo } from "react";
import { Handle, Position, type NodeProps } from "@xyflow/react";

export interface StateNodeData {
  name: string;
  label: string;
  color: string;
  icon: string | null;
  isInitial: boolean;
  isTerminal: boolean;
  isPaused: boolean;
  category: string;
  isSelected: boolean;
  isReadOnly: boolean;
}

function StateNodeComponent({ data }: NodeProps) {
  const d = data as unknown as StateNodeData;
  const borderColor = d.color || "#6b7280";
  const isSelected = d.isSelected;

  return (
    <div
      style={{
        minWidth: 160,
        maxWidth: 220,
        borderRadius: 10,
        border: `2px solid ${borderColor}`,
        background: "#1e1e2e",
        overflow: "hidden",
        boxShadow: isSelected
          ? `0 0 0 2px ${borderColor}, 0 4px 12px rgba(0,0,0,0.15)`
          : "0 2px 8px rgba(0,0,0,0.08)",
        cursor: "grab",
        fontFamily: "'Inter', sans-serif",
        transition: "box-shadow 0.2s ease",
      }}
    >
      {/* Incoming handle */}
      {!d.isInitial && (
        <Handle
          type="target"
          position={Position.Left}
          className="state-handle state-handle--target"
        />
      )}

      {/* Header */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          padding: "6px 10px",
          backgroundColor: borderColor,
          color: "#fff",
          fontSize: 11,
          fontWeight: 600,
          textTransform: "uppercase" as const,
          letterSpacing: "0.05em",
        }}
      >
        <div style={{ display: "flex", gap: 3 }}>
          {d.isInitial && <span title="Initial" style={{ fontSize: 9, opacity: 0.9 }}>▶</span>}
          {d.isTerminal && <span title="Terminal" style={{ fontSize: 9, opacity: 0.9 }}>■</span>}
          {d.isPaused && <span title="Paused" style={{ fontSize: 9, opacity: 0.9 }}>⏸</span>}
        </div>
        <span style={{ flex: 1, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" as const }}>
          {d.name}
        </span>
      </div>

      {/* Body */}
      <div style={{ padding: "8px 10px", display: "flex", flexDirection: "column" as const, gap: 2 }}>
        <span style={{ fontSize: 13, fontWeight: 500, color: "#e0e0e0" }}>{d.label}</span>
        <span
          style={{
            fontSize: 10,
            color: "#888",
            textTransform: "uppercase" as const,
            letterSpacing: "0.08em",
          }}
        >
          {d.category}
        </span>
      </div>

      {/* Outgoing handle */}
      {!d.isTerminal && (
        <Handle
          type="source"
          position={Position.Right}
          className="state-handle state-handle--source"
        />
      )}
    </div>
  );
}

export const StateNode = memo(StateNodeComponent);
