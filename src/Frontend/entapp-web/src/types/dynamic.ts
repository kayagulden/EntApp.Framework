// ── Dynamic UI Type Definitions ─────────────────────────
// Backend EntityMetadataDto karşılığı TypeScript tipleri

export interface EntityMetadata {
  entity: string;
  title: string;
  menuGroup?: string;
  isDetail: boolean;
  fields: FieldMetadata[];
  details?: DetailMetadata[];
  actions: EntityActions;
}

export interface FieldMetadata {
  name: string;
  label: string;
  type: FieldType;
  required: boolean;
  readOnly: boolean;
  searchable: boolean;
  maxLength: number;
  minLength: number;
  min?: number;
  max?: number;
  defaultValue?: string;
  computed?: string;
  options?: string[];
  lookup?: LookupInfo;
}

export type FieldType =
  | "string"
  | "text"
  | "number"
  | "decimal"
  | "money"
  | "date"
  | "datetime"
  | "boolean"
  | "enum"
  | "lookup"
  | "file"
  | "richtext";

export interface LookupInfo {
  entity: string;
  displayField: string;
  endpoint: string;
}

export interface DetailMetadata {
  name: string;
  label: string;
  entity: string;
  fields: FieldMetadata[];
}

export interface EntityActions {
  create: boolean;
  edit: boolean;
  delete: boolean;
  export: boolean;
}

export interface MenuGroup {
  name: string;
  items: MenuItem[];
}

export interface MenuItem {
  entity: string;
  title: string;
}

// ── Generic Paged Types ─────────────────────────────────

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PagedRequest {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
  search?: string;
}

export interface LookupDto {
  id: string;
  text: string;
  description?: string;
  isActive: boolean;
}
