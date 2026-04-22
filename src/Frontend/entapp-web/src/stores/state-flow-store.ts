import { create } from "zustand";
import type { StateDto, TransitionDto } from "@/lib/api/state-flow";

// ── Types ────────────────────────────────────────────────────

export type SelectedElement =
  | { type: "state"; id: string }
  | { type: "transition"; id: string }
  | null;

interface StateFlowDesignerState {
  // Flow meta
  flowId: string | null;
  flowName: string;
  flowStatus: string;
  flowVersion: number;
  entityType: string;

  // Canvas data
  states: StateDto[];
  transitions: TransitionDto[];

  // Selection
  selectedElement: SelectedElement;

  // Dirty flag
  isDirty: boolean;
  isSaving: boolean;

  // Actions
  setFlow: (flowId: string, name: string, status: string, version: number, entityType: string) => void;
  setStates: (states: StateDto[]) => void;
  setTransitions: (transitions: TransitionDto[]) => void;
  setSelectedElement: (element: SelectedElement) => void;
  updateState: (id: string, updates: Partial<StateDto>) => void;
  updateTransition: (id: string, updates: Partial<TransitionDto>) => void;
  addState: (state: StateDto) => void;
  removeState: (id: string) => void;
  addTransition: (transition: TransitionDto) => void;
  removeTransition: (id: string) => void;
  setDirty: (dirty: boolean) => void;
  setSaving: (saving: boolean) => void;
  reset: () => void;
}

const initialState = {
  flowId: null as string | null,
  flowName: "",
  flowStatus: "Draft",
  flowVersion: 1,
  entityType: "",
  states: [] as StateDto[],
  transitions: [] as TransitionDto[],
  selectedElement: null as SelectedElement,
  isDirty: false,
  isSaving: false,
};

export const useStateFlowDesignerStore = create<StateFlowDesignerState>((set) => ({
  ...initialState,

  setFlow: (flowId, name, status, version, entityType) =>
    set({ flowId, flowName: name, flowStatus: status, flowVersion: version, entityType }),

  setStates: (states) => set({ states }),
  setTransitions: (transitions) => set({ transitions }),
  setSelectedElement: (element) => set({ selectedElement: element }),

  updateState: (id, updates) =>
    set((s) => ({
      states: s.states.map((st) => (st.id === id ? { ...st, ...updates } : st)),
      isDirty: true,
    })),

  updateTransition: (id, updates) =>
    set((s) => ({
      transitions: s.transitions.map((t) => (t.id === id ? { ...t, ...updates } : t)),
      isDirty: true,
    })),

  addState: (state) =>
    set((s) => ({ states: [...s.states, state], isDirty: true })),

  removeState: (id) =>
    set((s) => ({
      states: s.states.filter((st) => st.id !== id),
      transitions: s.transitions.filter(
        (t) =>
          t.fromStateName !== s.states.find((st) => st.id === id)?.name &&
          t.toStateName !== s.states.find((st) => st.id === id)?.name
      ),
      isDirty: true,
      selectedElement: s.selectedElement?.type === "state" && s.selectedElement.id === id ? null : s.selectedElement,
    })),

  addTransition: (transition) =>
    set((s) => ({ transitions: [...s.transitions, transition], isDirty: true })),

  removeTransition: (id) =>
    set((s) => ({
      transitions: s.transitions.filter((t) => t.id !== id),
      isDirty: true,
      selectedElement: s.selectedElement?.type === "transition" && s.selectedElement.id === id ? null : s.selectedElement,
    })),

  setDirty: (dirty) => set({ isDirty: dirty }),
  setSaving: (saving) => set({ isSaving: saving }),

  reset: () => set(initialState),
}));
