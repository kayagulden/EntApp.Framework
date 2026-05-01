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
  onTransitionActions: string | null;
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

// ── Automation ────────────────────────────────────────────────

export interface RuleExecutionLogDto {
  id: string;
  flowDefinitionId: string;
  entityType: string;
  targetEntityId: string;
  source: string;
  stateName: string;
  triggerName: string | null;
  actionType: string;
  actionParamsJson: string;
  success: boolean;
  errorMessage: string | null;
  durationMs: number;
  createdAt: string;
}

export interface ActionTypeInfo {
  type: string;
  label: string;
  description: string;
  paramFields: string[];
}

export async function listExecutionLogs(
  flowDefinitionId?: string,
  entityId?: string,
  limit = 50
) {
  const params = new URLSearchParams();
  if (flowDefinitionId) params.set("flowDefinitionId", flowDefinitionId);
  if (entityId) params.set("entityId", entityId);
  if (limit !== 50) params.set("limit", String(limit));
  const { data } = await apiClient.get<RuleExecutionLogDto[]>(`${BASE.replace('/flows', '')}/automation/logs?${params}`);
  return data;
}

export async function getActionTypes() {
  const { data } = await apiClient.get<ActionTypeInfo[]>(`${BASE.replace('/flows', '')}/automation/action-types`);
  return data;
}

// ── Event Automation Rules ───────────────────────────────────

export interface EventAutomationRuleDto {
  id: string;
  name: string;
  description: string | null;
  triggerType: string;
  triggerConditions: string;
  actionType: string;
  actionParams: string;
  entityType: string | null;
  isEnabled: boolean;
  priority: number;
  sortOrder: number;
  createdAt: string;
}

export interface TriggerTypeInfo {
  type: string;
  label: string;
  description: string;
}

const AUTO_BASE = `${BASE.replace('/flows', '')}/automation`;

export async function listEventRules(enabledOnly?: boolean) {
  const params = new URLSearchParams();
  if (enabledOnly !== undefined) params.set("enabledOnly", String(enabledOnly));
  const { data } = await apiClient.get<EventAutomationRuleDto[]>(`${AUTO_BASE}/event-rules?${params}`);
  return data;
}

export async function createEventRule(rule: {
  name: string; triggerType: string; actionType: string;
  description?: string; triggerConditions?: string;
  actionParams?: string; entityType?: string;
  priority?: number; sortOrder?: number;
}) {
  const { data } = await apiClient.post<{ id: string }>(`${AUTO_BASE}/event-rules`, rule);
  return data;
}

export async function updateEventRule(id: string, rule: {
  name: string; triggerType: string; actionType: string;
  description?: string; triggerConditions?: string;
  actionParams?: string; entityType?: string;
  priority?: number; sortOrder?: number;
}) {
  await apiClient.put(`${AUTO_BASE}/event-rules/${id}`, rule);
}

export async function toggleEventRule(id: string) {
  await apiClient.patch(`${AUTO_BASE}/event-rules/${id}/toggle`, {});
}

export async function deleteEventRule(id: string) {
  await apiClient.delete(`${AUTO_BASE}/event-rules/${id}`);
}

export async function getTriggerTypes() {
  const { data } = await apiClient.get<TriggerTypeInfo[]>(`${AUTO_BASE}/trigger-types`);
  return data;
}
