"use client";

import { memo } from "react";
import {
  BaseEdge,
  EdgeLabelRenderer,
  getBezierPath,
  type EdgeProps,
} from "@xyflow/react";

function TransitionEdgeComponent({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  data,
  selected,
}: EdgeProps) {
  const [edgePath, labelX, labelY] = getBezierPath({
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
  });

  const edgeData = data as { triggerName?: string; label?: string } | undefined;
  const label = edgeData?.label || edgeData?.triggerName || "";

  return (
    <>
      <BaseEdge
        id={id}
        path={edgePath}
        style={{
          stroke: selected ? "#8b5cf6" : "#6b7280",
          strokeWidth: selected ? 2.5 : 1.8,
          transition: "stroke 0.2s, stroke-width 0.2s",
        }}
        markerEnd="url(#arrowhead)"
      />
      {label && (
        <EdgeLabelRenderer>
          <div
            className="edge-label"
            style={{
              position: "absolute",
              transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)`,
              pointerEvents: "all",
              background: selected ? "#8b5cf6" : "var(--edge-label-bg, #2a2a3e)",
              color: "#fff",
              padding: "2px 8px",
              borderRadius: 6,
              fontSize: 11,
              fontWeight: 500,
              fontFamily: "'Inter', sans-serif",
              whiteSpace: "nowrap",
              border: `1px solid ${selected ? "#a78bfa" : "rgba(255,255,255,0.1)"}`,
              boxShadow: "0 2px 4px rgba(0,0,0,0.2)",
              transition: "background 0.2s, border-color 0.2s",
            }}
          >
            {label}
          </div>
        </EdgeLabelRenderer>
      )}
    </>
  );
}

export const TransitionEdge = memo(TransitionEdgeComponent);
