"use client";

import { useState, useEffect, useCallback } from "react";
import { ChevronDown, ChevronRight, Plus, Trash2, Edit2, X, Save, Loader2 } from "lucide-react";
import type { FieldMetadata, DetailMetadata } from "@/types/dynamic";
import { cn } from "@/lib/utils";

interface DynamicDetailTableProps {
  /** Detail metadata (name, label, entity, fields) */
  detail: DetailMetadata;
  /** Master record ID */
  masterId: string;
  /** Master entity name */
  masterEntity: string;
}

interface DetailRow extends Record<string, unknown> {
  id: string;
}

/**
 * Master-detail inline table.
 * Master satırı seçildiğinde/genişletildiğinde alt entity kayıtlarını listeler.
 * Inline CRUD destekler.
 */
export function DynamicDetailTable({
  detail,
  masterId,
  masterEntity,
}: DynamicDetailTableProps) {
  const [expanded, setExpanded] = useState(false);
  const [items, setItems] = useState<DetailRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editData, setEditData] = useState<Record<string, unknown>>({});
  const [showAddRow, setShowAddRow] = useState(false);
  const [newRowData, setNewRowData] = useState<Record<string, unknown>>({});
  const [saving, setSaving] = useState(false);

  // Fields for detail table (exclude id, orderId/masterId, tenantId fields)
  const visibleFields = detail.fields.filter(
    (f) =>
      f.name !== "id" &&
      f.name !== "tenantId" &&
      !f.name.toLowerCase().endsWith("orderid") &&
      f.type !== "richtext" &&
      f.type !== "file"
  );

  const fetchDetails = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetch(
        `/api/crud/${masterEntity}/${masterId}/${detail.name}`
      );
      if (res.ok) {
        const data = await res.json();
        setItems(Array.isArray(data) ? data : data.items || []);
      }
    } catch {
      // silent
    } finally {
      setLoading(false);
    }
  }, [masterEntity, masterId, detail.name]);

  useEffect(() => {
    if (expanded && items.length === 0) {
      fetchDetails();
    }
  }, [expanded, fetchDetails]);

  const handleDelete = async (id: string) => {
    if (!confirm("Bu kaydı silmek istediğinizden emin misiniz?")) return;
    try {
      await fetch(`/api/crud/${detail.entity}/${id}`, { method: "DELETE" });
      setItems((prev) => prev.filter((i) => i.id !== id));
    } catch {
      // silent
    }
  };

  const handleSaveEdit = async () => {
    if (!editingId) return;
    setSaving(true);
    try {
      const res = await fetch(`/api/crud/${detail.entity}/${editingId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(editData),
      });
      if (res.ok) {
        await fetchDetails();
        setEditingId(null);
        setEditData({});
      }
    } catch {
      // silent
    } finally {
      setSaving(false);
    }
  };

  const handleAddRow = async () => {
    setSaving(true);
    try {
      // Inject foreign key
      const fkField = detail.fields.find((f) =>
        f.name.toLowerCase().endsWith("orderid")
      );
      const body = {
        ...newRowData,
        ...(fkField ? { [fkField.name]: masterId } : {}),
      };

      const res = await fetch(`/api/crud/${detail.entity}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      if (res.ok) {
        await fetchDetails();
        setShowAddRow(false);
        setNewRowData({});
      }
    } catch {
      // silent
    } finally {
      setSaving(false);
    }
  };

  const formatValue = (value: unknown, field: FieldMetadata): string => {
    if (value === null || value === undefined) return "—";
    switch (field.type) {
      case "boolean":
        return value ? "Evet" : "Hayır";
      case "date":
        return new Date(value as string).toLocaleDateString("tr-TR");
      case "money":
        return new Intl.NumberFormat("tr-TR", {
          style: "currency",
          currency: "TRY",
        }).format(value as number);
      case "decimal":
        return new Intl.NumberFormat("tr-TR", {
          minimumFractionDigits: 2,
        }).format(value as number);
      default:
        return String(value);
    }
  };

  const renderInput = (
    field: FieldMetadata,
    value: unknown,
    onChange: (name: string, val: unknown) => void
  ) => {
    const base =
      "px-2 py-1 rounded border border-[var(--color-border)] bg-[var(--color-bg)] text-xs text-[var(--color-text)] focus:ring-1 focus:ring-indigo-500/50 focus:outline-none w-full";

    switch (field.type) {
      case "number":
      case "decimal":
      case "money":
        return (
          <input
            type="number"
            step={field.type === "number" ? "1" : "0.01"}
            value={String(value ?? "")}
            onChange={(e) => onChange(field.name, Number(e.target.value))}
            className={base}
          />
        );
      case "boolean":
        return (
          <input
            type="checkbox"
            checked={!!value}
            onChange={(e) => onChange(field.name, e.target.checked)}
            className="h-4 w-4 accent-indigo-500"
          />
        );
      default:
        return (
          <input
            type="text"
            value={String(value ?? "")}
            onChange={(e) => onChange(field.name, e.target.value)}
            maxLength={field.maxLength || undefined}
            className={base}
          />
        );
    }
  };

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)]/30 overflow-hidden">
      {/* ── Header (Collapsible) ──────────────── */}
      <button
        onClick={() => setExpanded(!expanded)}
        className="w-full flex items-center justify-between px-4 py-2.5 text-sm font-medium text-[var(--color-text)] hover:bg-[var(--color-surface-hover)] transition-colors"
      >
        <div className="flex items-center gap-2">
          {expanded ? (
            <ChevronDown className="w-4 h-4 text-indigo-400" />
          ) : (
            <ChevronRight className="w-4 h-4 text-[var(--color-text-muted)]" />
          )}
          <span>{detail.label}</span>
          {items.length > 0 && (
            <span className="px-1.5 py-0.5 text-[10px] font-semibold rounded bg-indigo-500/10 text-indigo-400">
              {items.length}
            </span>
          )}
        </div>
        {expanded && (
          <button
            onClick={(e) => {
              e.stopPropagation();
              setShowAddRow(!showAddRow);
            }}
            className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-indigo-500/10 text-indigo-400 text-xs font-medium hover:bg-indigo-500/20 transition-colors"
          >
            <Plus className="w-3 h-3" />
            Ekle
          </button>
        )}
      </button>

      {/* ── Content ───────────────────────────── */}
      {expanded && (
        <div className="border-t border-[var(--color-border)]">
          {loading ? (
            <div className="flex items-center justify-center py-6">
              <Loader2 className="w-5 h-5 text-indigo-400 animate-spin" />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead>
                  <tr className="bg-[var(--color-bg)]/50 border-b border-[var(--color-border)]">
                    {visibleFields.map((f) => (
                      <th
                        key={f.name}
                        className="px-3 py-2 text-left font-semibold text-[var(--color-text-muted)] uppercase tracking-wider"
                      >
                        {f.label}
                      </th>
                    ))}
                    <th className="px-3 py-2 text-right font-semibold text-[var(--color-text-muted)] uppercase tracking-wider w-16">
                      İşlem
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[var(--color-border)]">
                  {/* Add new row */}
                  {showAddRow && (
                    <tr className="bg-indigo-500/5">
                      {visibleFields.map((f) => (
                        <td key={f.name} className="px-3 py-2">
                          {renderInput(f, newRowData[f.name], (name, val) =>
                            setNewRowData((prev) => ({ ...prev, [name]: val }))
                          )}
                        </td>
                      ))}
                      <td className="px-3 py-2 text-right">
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={handleAddRow}
                            disabled={saving}
                            className="p-1 rounded text-emerald-400 hover:bg-emerald-500/10 transition-colors"
                          >
                            {saving ? (
                              <Loader2 className="w-3.5 h-3.5 animate-spin" />
                            ) : (
                              <Save className="w-3.5 h-3.5" />
                            )}
                          </button>
                          <button
                            onClick={() => {
                              setShowAddRow(false);
                              setNewRowData({});
                            }}
                            className="p-1 rounded text-slate-400 hover:bg-slate-500/10 transition-colors"
                          >
                            <X className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  )}

                  {/* Existing rows */}
                  {items.length === 0 && !showAddRow ? (
                    <tr>
                      <td
                        colSpan={visibleFields.length + 1}
                        className="px-3 py-6 text-center text-[var(--color-text-muted)]"
                      >
                        Kayıt yok
                      </td>
                    </tr>
                  ) : (
                    items.map((row) => {
                      const isEditing = editingId === row.id;
                      return (
                        <tr
                          key={row.id}
                          className={cn(
                            "hover:bg-[var(--color-surface-hover)] transition-colors",
                            isEditing && "bg-amber-500/5"
                          )}
                        >
                          {visibleFields.map((f) => (
                            <td key={f.name} className="px-3 py-2 text-[var(--color-text)]">
                              {isEditing ? (
                                renderInput(f, editData[f.name], (name, val) =>
                                  setEditData((prev) => ({ ...prev, [name]: val }))
                                )
                              ) : (
                                formatValue(row[f.name], f)
                              )}
                            </td>
                          ))}
                          <td className="px-3 py-2 text-right">
                            {isEditing ? (
                              <div className="flex items-center justify-end gap-1">
                                <button
                                  onClick={handleSaveEdit}
                                  disabled={saving}
                                  className="p-1 rounded text-emerald-400 hover:bg-emerald-500/10 transition-colors"
                                >
                                  {saving ? (
                                    <Loader2 className="w-3.5 h-3.5 animate-spin" />
                                  ) : (
                                    <Save className="w-3.5 h-3.5" />
                                  )}
                                </button>
                                <button
                                  onClick={() => {
                                    setEditingId(null);
                                    setEditData({});
                                  }}
                                  className="p-1 rounded text-slate-400 hover:bg-slate-500/10 transition-colors"
                                >
                                  <X className="w-3.5 h-3.5" />
                                </button>
                              </div>
                            ) : (
                              <div className="flex items-center justify-end gap-1">
                                <button
                                  onClick={() => {
                                    setEditingId(row.id);
                                    const data: Record<string, unknown> = {};
                                    visibleFields.forEach((f) => {
                                      data[f.name] = row[f.name];
                                    });
                                    setEditData(data);
                                  }}
                                  className="p-1 rounded text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-border)] transition-colors"
                                >
                                  <Edit2 className="w-3.5 h-3.5" />
                                </button>
                                <button
                                  onClick={() => handleDelete(row.id)}
                                  className="p-1 rounded text-[var(--color-text-muted)] hover:text-red-400 hover:bg-red-500/10 transition-colors"
                                >
                                  <Trash2 className="w-3.5 h-3.5" />
                                </button>
                              </div>
                            )}
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
