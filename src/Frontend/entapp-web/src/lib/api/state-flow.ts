import { apiClient } from "@/lib/api-client";

// ── Types ────────────────────────────────────────────────────

export interface StateDto {
  id: string;
  name: string;
  label: string;
  color: string;
  icon: string | null;
  isInitial: boolean;
  isTerminal: boolean;
  isPaused: boolean;
  category: string;
  positionX: number;
  positionY: number;
  sortOrder: number;
  onEntryActions: string | null;
}

export interface TransitionDto {
  id: string;
  fromStateName: string;
  toStateName: string;
  triggerName: string;
  label: string;
  requiredRole: string | null;
  guardExpression: string | null;
  sortOrder: number;
}

export interface FlowDefinitionDto {
  id: string;
  entityType: string;
  key: string;
  name: string;
  description: string | null;
  version: number;
  status: string;
  publishedAt: string | null;
  isGlobalTemplate: boolean;
  sourceTemplateId: string | null;
  stateCount: number;
  transitionCount: number;
  createdAt: string;
}

export interface FlowDefinitionDetailDto {
  id: string;
  entityType: string;
  key: string;
  name: string;
  description: string | null;
  version: number;
  status: string;
  publishedAt: string | null;
  isGlobalTemplate: boolean;
  sourceTemplateId: string | null;
  createdAt: string;
  states: StateDto[];
  transitions: TransitionDto[];
}

export interface TriggerInfo {
  triggerName: string;
  label: string;
  toStateName: string;
  requiredRole: string | null;
}

// ── API Functions ────────────────────────────────────────────

const BASE = "/api/sf";

export async function listFlowDefinitions(entityType?: string, includeArchived = false) {
  const params = new URLSearchParams();
  if (entityType) params.set("entityType", entityType);
  if (includeArchived) params.set("includeArchived", "true");
  const { data } = await apiClient.get<FlowDefinitionDto[]>(`${BASE}/flows?${params}`);
  return data;
}

export async function getFlowDefinition(id: string) {
  const { data } = await apiClient.get<FlowDefinitionDetailDto>(`${BASE}/flows/${id}`);
  return data;
}

export async function getPublishedFlow(entityType: string) {
  const { data } = await apiClient.get<FlowDefinitionDetailDto>(`${BASE}/flows/published/${entityType}`);
  return data;
}

export async function createFlowDefinition(req: {
  entityType: string;
  key: string;
  name: string;
  description?: string;
  isGlobalTemplate?: boolean;
}) {
  const { data } = await apiClient.post<{ id: string }>(`${BASE}/flows`, req);
  return data.id;
}

export async function updateFlowDefinition(id: string, req: { name: string; description?: string }) {
  await apiClient.put(`${BASE}/flows/${id}`, req);
}

export async function deleteFlowDefinition(id: string) {
  await apiClient.delete(`${BASE}/flows/${id}`);
}

export async function publishFlow(id: string) {
  await apiClient.post(`${BASE}/flows/${id}/publish`);
}

export async function archiveFlow(id: string) {
  await apiClient.post(`${BASE}/flows/${id}/archive`);
}

export async function createNewVersion(id: string) {
  const { data } = await apiClient.post<{ id: string }>(`${BASE}/flows/${id}/new-version`);
  return data.id;
}

export async function cloneFromTemplate(id: string, customName?: string) {
  const { data } = await apiClient.post<{ id: string }>(`${BASE}/flows/${id}/clone`, { customName });
  return data.id;
}

export async function saveFlowDesign(
  id: string,
  states: StateDto[],
  transitions: TransitionDto[]
) {
  await apiClient.put(`${BASE}/flows/${id}/design`, { states, transitions });
}

export async function getAllowedTriggers(
  entityType: string,
  currentState: string,
  flowDefinitionId: string
) {
  const { data } = await apiClient.post<TriggerInfo[]>(`${BASE}/engine/allowed-triggers`, {
    entityType,
    currentState,
    flowDefinitionId,
  });
  return data;
}

export async function fireTransition(
  entityType: string,
  currentState: string,
  trigger: string,
  flowDefinitionId: string
) {
  const { data } = await apiClient.post<{ newState: string }>(`${BASE}/engine/fire`, {
    entityType,
    currentState,
    trigger,
    flowDefinitionId,
  });
  return data.newState;
}
