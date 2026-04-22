"use client";

import { useCallback, useEffect, useMemo, useRef } from "react";
import {
  ReactFlow,
  Background,
  Controls,
  MiniMap,
  useNodesState,
  useEdgesState,
  addEdge,
  useReactFlow,
  ReactFlowProvider,
  type Node,
  type Edge,
  type Connection,
  type OnNodesChange,
  type OnEdgesChange,
  MarkerType,
  Panel,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import "./state-flow-designer.css";
import dagre from "dagre";

import { useStateFlowDesignerStore } from "@/stores/state-flow-store";
import { StateNode, type StateNodeData } from "./StateNode";
import { TransitionEdge } from "./TransitionEdge";
import { PropertiesPanel } from "./PropertiesPanel";
import type { StateDto, TransitionDto } from "@/lib/api/state-flow";
import { saveFlowDesign, publishFlow } from "@/lib/api/state-flow";

const nodeTypes = { stateNode: StateNode };
const edgeTypes = { transitionEdge: TransitionEdge };

// ── Dagre auto-layout ────────────────────────────────────────
function autoLayout(nodes: Node[], edges: Edge[]): Node[] {
  const g = new dagre.graphlib.Graph();
  g.setDefaultEdgeLabel(() => ({}));
  g.setGraph({ rankdir: "LR", nodesep: 60, ranksep: 120 });

  nodes.forEach((n) => g.setNode(n.id, { width: 180, height: 80 }));
  edges.forEach((e) => g.setEdge(e.source, e.target));
  dagre.layout(g);

  return nodes.map((n) => {
    const pos = g.node(n.id);
    return { ...n, position: { x: pos.x - 90, y: pos.y - 40 } };
  });
}

function generateId() {
  return crypto.randomUUID();
}

interface DesignerProps {
  flowId: string;
}

function DesignerInner({ flowId }: DesignerProps) {
  const store = useStateFlowDesignerStore();
  const isReadOnly = store.flowStatus !== "Draft";
  const { fitView } = useReactFlow();
  const prevStateCountRef = useRef(store.states.length);

  // ── Convert store → React Flow nodes ──────────────────────
  const flowNodes = useMemo((): Node[] => {
    return store.states.map((s) => ({
      id: s.id,
      type: "stateNode",
      position: { x: s.positionX, y: s.positionY },
      data: {
        name: s.name,
        label: s.label,
        color: s.color,
        icon: s.icon,
        isInitial: s.isInitial,
        isTerminal: s.isTerminal,
        isPaused: s.isPaused,
        category: s.category,
        isSelected: store.selectedElement?.type === "state" && store.selectedElement.id === s.id,
        isReadOnly,
      } satisfies StateNodeData,
      draggable: !isReadOnly,
    }));
  }, [store.states, store.selectedElement, isReadOnly]);

  // ── Convert store → React Flow edges ──────────────────────
  const flowEdges = useMemo((): Edge[] => {
    return store.transitions.map((t) => {
      const sourceNode = store.states.find((s) => s.name === t.fromStateName);
      const targetNode = store.states.find((s) => s.name === t.toStateName);
      return {
        id: t.id,
        source: sourceNode?.id || "",
        target: targetNode?.id || "",
        type: "transitionEdge",
        data: { triggerName: t.triggerName, label: t.label },
        markerEnd: { type: MarkerType.ArrowClosed, color: "#6b7280" },
      };
    });
  }, [store.transitions, store.states]);

  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);

  // ── Sync store → React Flow (always) ──────────────────────
  useEffect(() => {
    setNodes(flowNodes);
    setEdges(flowEdges);
  }, [flowNodes, flowEdges, setNodes, setEdges]);

  // ── fitView when state count changes ──────────────────────
  useEffect(() => {
    if (store.states.length !== prevStateCountRef.current) {
      prevStateCountRef.current = store.states.length;
      // Small delay to let React Flow process new nodes
      setTimeout(() => fitView({ padding: 0.3, duration: 300 }), 50);
    }
  }, [store.states.length, fitView]);

  // ── Node drag → update store positions ────────────────────
  const handleNodesChange: OnNodesChange = useCallback(
    (changes) => {
      onNodesChange(changes);
      changes.forEach((change) => {
        if (change.type === "position" && change.dragging === false && change.position) {
          store.updateState(change.id, {
            positionX: change.position.x,
            positionY: change.position.y,
          });
        }
      });
    },
    [onNodesChange, store]
  );

  const handleEdgesChange: OnEdgesChange = useCallback(
    (changes) => onEdgesChange(changes),
    [onEdgesChange]
  );

  // ── New connection → create transition ────────────────────
  const onConnect = useCallback(
    (connection: Connection) => {
      if (isReadOnly) return;
      const sourceState = store.states.find((s) => s.id === connection.source);
      const targetState = store.states.find((s) => s.id === connection.target);
      if (!sourceState || !targetState) return;

      const newTransition: TransitionDto = {
        id: generateId(),
        fromStateName: sourceState.name,
        toStateName: targetState.name,
        triggerName: `${sourceState.name}To${targetState.name}`,
        label: `${sourceState.label} → ${targetState.label}`,
        requiredRole: null,
        guardExpression: null,
        sortOrder: store.transitions.length,
      };

      store.addTransition(newTransition);
      setEdges((eds) =>
        addEdge(
          {
            ...connection,
            id: newTransition.id,
            type: "transitionEdge",
            data: { triggerName: newTransition.triggerName, label: newTransition.label },
            markerEnd: { type: MarkerType.ArrowClosed, color: "#6b7280" },
          },
          eds
        )
      );
    },
    [isReadOnly, store, setEdges]
  );

  // ── Selection ─────────────────────────────────────────────
  const onNodeClick = useCallback(
    (_: React.MouseEvent, node: Node) => {
      store.setSelectedElement({ type: "state", id: node.id });
    },
    [store]
  );

  const onEdgeClick = useCallback(
    (_: React.MouseEvent, edge: Edge) => {
      store.setSelectedElement({ type: "transition", id: edge.id });
    },
    [store]
  );

  const onPaneClick = useCallback(() => {
    store.setSelectedElement(null);
  }, [store]);

  // ── Toolbar actions ───────────────────────────────────────
  const handleAddState = useCallback(() => {
    const count = store.states.length;
    const newState: StateDto = {
      id: generateId(),
      name: `State${count + 1}`,
      label: `State ${count + 1}`,
      color: "#6b7280",
      icon: null,
      isInitial: count === 0,
      isTerminal: false,
      isPaused: false,
      category: "Active",
      positionX: 100 + count * 220,
      positionY: 200,
      sortOrder: count,
      onEntryActions: null,
    };
    store.addState(newState);
  }, [store]);

  const handleAutoLayout = useCallback(() => {
    const layouted = autoLayout(nodes, edges);
    setNodes(layouted);
    layouted.forEach((n) => {
      store.updateState(n.id, {
        positionX: n.position.x,
        positionY: n.position.y,
      });
    });
    setTimeout(() => fitView({ padding: 0.3, duration: 300 }), 50);
  }, [nodes, edges, setNodes, store, fitView]);

  const handleSave = useCallback(async () => {
    if (!store.flowId) return;
    store.setSaving(true);
    try {
      await saveFlowDesign(store.flowId, store.states, store.transitions);
      store.setDirty(false);
    } catch (err) {
      console.error("Save failed:", err);
      alert("Kaydetme başarısız!");
    } finally {
      store.setSaving(false);
    }
  }, [store]);

  const handlePublish = useCallback(async () => {
    if (!store.flowId) return;
    if (!confirm("Bu akışı yayınlamak istediğinizden emin misiniz?")) return;
    try {
      await saveFlowDesign(store.flowId, store.states, store.transitions);
      await publishFlow(store.flowId);
      store.setDirty(false);
      window.location.reload();
    } catch (err) {
      console.error("Publish failed:", err);
      alert("Yayınlama başarısız! " + (err instanceof Error ? err.message : ""));
    }
  }, [store]);

  return (
    <div className="designer-container">
      <div className="designer-canvas">
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={handleNodesChange}
          onEdgesChange={handleEdgesChange}
          onConnect={onConnect}
          onNodeClick={onNodeClick}
          onEdgeClick={onEdgeClick}
          onPaneClick={onPaneClick}
          nodeTypes={nodeTypes}
          edgeTypes={edgeTypes}
          fitView
          fitViewOptions={{ padding: 0.3 }}
          defaultEdgeOptions={{
            type: "transitionEdge",
            markerEnd: { type: MarkerType.ArrowClosed, color: "#6b7280" },
          }}
          proOptions={{ hideAttribution: true }}
        >
          <Background color="#333" gap={20} size={1} />
          <Controls showInteractive={false} />
          <MiniMap
            nodeColor={(n) => {
              const d = n.data as unknown as StateNodeData;
              return d?.color || "#6b7280";
            }}
            maskColor="rgba(0,0,0,0.6)"
            style={{ background: "#1a1a2e" }}
          />

          {/* Toolbar Panel */}
          <Panel position="top-left" className="designer-toolbar">
            <div className="toolbar-group">
              <span className="toolbar-badge" data-status={store.flowStatus}>
                {store.flowStatus} v{store.flowVersion}
              </span>
            </div>
            {!isReadOnly && (
              <div className="toolbar-group">
                <button className="toolbar-btn" onClick={handleAddState} title="Yeni State Ekle">
                  ＋ State
                </button>
                <button className="toolbar-btn" onClick={handleAutoLayout} title="Otomatik Düzenle">
                  ⟳ Layout
                </button>
                <button
                  className="toolbar-btn toolbar-btn--primary"
                  onClick={handleSave}
                  disabled={store.isSaving || !store.isDirty}
                  title="Kaydet"
                >
                  {store.isSaving ? "⏳" : "💾"} Kaydet
                </button>
                <button
                  className="toolbar-btn toolbar-btn--publish"
                  onClick={handlePublish}
                  title="Yayınla"
                >
                  🚀 Yayınla
                </button>
              </div>
            )}
          </Panel>

          {/* Custom arrow marker */}
          <svg style={{ position: "absolute", top: 0, left: 0 }}>
            <defs>
              <marker
                id="arrowhead"
                viewBox="0 0 10 10"
                refX="8"
                refY="5"
                markerWidth="8"
                markerHeight="8"
                orient="auto-start-reverse"
              >
                <path d="M 0 0 L 10 5 L 0 10 z" fill="#6b7280" />
              </marker>
            </defs>
          </svg>
        </ReactFlow>
      </div>
      <div className="designer-sidebar">
        <PropertiesPanel />
      </div>
    </div>
  );
}

// Wrapper with ReactFlowProvider (required for useReactFlow)
export function StateFlowDesigner({ flowId }: DesignerProps) {
  return (
    <ReactFlowProvider>
      <DesignerInner flowId={flowId} />
    </ReactFlowProvider>
  );
}
