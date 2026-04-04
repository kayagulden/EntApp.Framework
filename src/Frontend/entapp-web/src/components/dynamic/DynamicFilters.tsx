"use client";

import { useState, useCallback, useEffect } from "react";
import { Filter, X, ChevronDown, ChevronUp } from "lucide-react";
import { cn } from "@/lib/utils";
import type { FieldMetadata } from "@/types/dynamic";
import { DynamicLookup } from "./DynamicLookup";

export interface FilterValues {
  [fieldName: string]: string | boolean | null;
}

interface DynamicFiltersProps {
  fields: FieldMetadata[];
  values: FilterValues;
  onChange: (values: FilterValues) => void;
  onClear: () => void;
}

/**
 * Collapsible gelişmiş filtreleme paneli.
 * Searchable ve boolean alanlar için otomatik filter UI oluşturur.
 */
export function DynamicFilters({
  fields,
  values,
  onChange,
  onClear,
}: DynamicFiltersProps) {
  const [isOpen, setIsOpen] = useState(false);

  // Sadece filtrelenebilir alanları göster
  const filterableFields = fields.filter(
    (f) =>
      !f.readOnly &&
      !f.computed &&
      f.name !== "id" &&
      (f.searchable ||
        f.type === "boolean" ||
        f.type === "enum" ||
        f.type === "lookup")
  );

  const activeCount = Object.values(values).filter(
    (v) => v !== null && v !== ""
  ).length;

  const handleFieldChange = useCallback(
    (fieldName: string, val: string | boolean | null) => {
      onChange({ ...values, [fieldName]: val });
    },
    [values, onChange]
  );

  if (filterableFields.length === 0) return null;

  return (
    <div className="mb-4">
      {/* Toggle button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className={cn(
          "flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm transition-all duration-200",
          "border border-[var(--color-border)]",
          activeCount > 0
            ? "bg-indigo-500/10 border-indigo-500/30 text-indigo-400"
            : "text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:border-[var(--color-text-muted)]"
        )}
      >
        <Filter className="w-3.5 h-3.5" />
        Filtreler
        {activeCount > 0 && (
          <span className="flex items-center justify-center w-5 h-5 rounded-full bg-indigo-500 text-white text-[10px] font-bold">
            {activeCount}
          </span>
        )}
        {isOpen ? (
          <ChevronUp className="w-3.5 h-3.5 ml-auto" />
        ) : (
          <ChevronDown className="w-3.5 h-3.5 ml-auto" />
        )}
      </button>

      {/* Filter panel */}
      {isOpen && (
        <div
          className={cn(
            "mt-2 p-4 rounded-xl border border-[var(--color-border)]",
            "bg-[var(--color-card-bg)] animate-fade-in"
          )}
        >
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {filterableFields.map((field) => (
              <FilterField
                key={field.name}
                field={field}
                value={values[field.name] ?? null}
                onChange={(val) => handleFieldChange(field.name, val)}
              />
            ))}
          </div>

          {activeCount > 0 && (
            <div className="flex justify-end mt-3 pt-3 border-t border-[var(--color-border)]">
              <button
                onClick={onClear}
                className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-lg text-red-400 hover:bg-red-500/10 transition-colors"
              >
                <X className="w-3 h-3" />
                Filtreleri Temizle
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

// ── Individual Filter Field ────────────────────────

function FilterField({
  field,
  value,
  onChange,
}: {
  field: FieldMetadata;
  value: string | boolean | null;
  onChange: (val: string | boolean | null) => void;
}) {
  const [localValue, setLocalValue] = useState(value ?? "");

  useEffect(() => {
    setLocalValue(value ?? "");
  }, [value]);

  const baseClass = cn(
    "w-full rounded-lg border px-3 py-1.5 text-sm",
    "bg-[var(--color-input-bg)] border-[var(--color-border)]",
    "text-[var(--color-text)] placeholder:text-[var(--color-text-muted)]",
    "focus:outline-none focus:ring-1 focus:ring-indigo-500/40 focus:border-indigo-500"
  );

  const renderInput = () => {
    switch (field.type) {
      case "boolean":
        return (
          <select
            value={value === null ? "" : String(value)}
            onChange={(e) => {
              const v = e.target.value;
              if (v === "") onChange(null);
              else onChange(v === "true");
            }}
            className={baseClass}
          >
            <option value="">Tümü</option>
            <option value="true">Evet</option>
            <option value="false">Hayır</option>
          </select>
        );

      case "enum":
        return (
          <select
            value={(value as string) ?? ""}
            onChange={(e) => onChange(e.target.value || null)}
            className={baseClass}
          >
            <option value="">Tümü</option>
            {field.options?.map((opt) => (
              <option key={opt} value={opt}>
                {opt}
              </option>
            ))}
          </select>
        );

      case "lookup":
        return field.lookup ? (
          <DynamicLookup
            entityName={field.lookup.entity}
            value={(value as string) || null}
            onChange={(v) => onChange(v ?? null)}
            placeholder={`${field.label} filtrele...`}
          />
        ) : null;

      default:
        return (
          <input
            type="text"
            value={localValue as string}
            onChange={(e) => {
              setLocalValue(e.target.value);
            }}
            onBlur={() => {
              onChange((localValue as string) || null);
            }}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                onChange((localValue as string) || null);
              }
            }}
            placeholder={field.label}
            className={baseClass}
          />
        );
    }
  };

  return (
    <div className="space-y-1">
      <label className="block text-xs font-medium text-[var(--color-text-muted)]">
        {field.label}
      </label>
      {renderInput()}
    </div>
  );
}
