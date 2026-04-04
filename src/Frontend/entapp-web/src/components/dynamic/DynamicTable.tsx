"use client";

import { useState } from "react";
import {
  ChevronUp,
  ChevronDown,
  ChevronsUpDown,
  ChevronLeft,
  ChevronRight,
  Edit2,
  Trash2,
  MoreHorizontal,
} from "lucide-react";
import type { FieldMetadata, PagedResult } from "@/types/dynamic";
import { cn } from "@/lib/utils";

interface DynamicTableProps {
  fields: FieldMetadata[];
  data: PagedResult<Record<string, unknown>> | undefined;
  isLoading: boolean;
  sortBy: string | undefined;
  sortDescending: boolean;
  onSortChange: (field: string) => void;
  onPageChange: (page: number) => void;
  onEditClick: (row: Record<string, unknown>) => void;
  onDeleteClick: (id: string) => void;
  canEdit: boolean;
  canDelete: boolean;
  /** SignalR: yeni/güncellenen satır ID → highlight animasyonu */
  highlightId?: string | null;
  /** SignalR: silinen satır ID → fade-out animasyonu */
  deletingId?: string | null;
}

/**
 * Metadata-driven table: kolonlar metadata'dan otomatik üretilir.
 * Sort, pagination, ve row actions destekler.
 */
export function DynamicTable({
  fields,
  data,
  isLoading,
  sortBy,
  sortDescending,
  onSortChange,
  onPageChange,
  onEditClick,
  onDeleteClick,
  canEdit,
  canDelete,
  highlightId,
  deletingId,
}: DynamicTableProps) {
  const [activeMenu, setActiveMenu] = useState<string | null>(null);

  // Tabloda gösterilecek alanlar (readOnly/computed dahil, navigation hariç)
  const visibleFields = fields.filter(
    (f) => f.type !== "richtext" && f.type !== "file"
  );

  const formatValue = (value: unknown, field: FieldMetadata): string => {
    if (value === null || value === undefined) return "—";

    switch (field.type) {
      case "boolean":
        return value ? "Evet" : "Hayır";
      case "date":
        return new Date(value as string).toLocaleDateString("tr-TR");
      case "datetime":
        return new Date(value as string).toLocaleString("tr-TR");
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

  const SortIcon = ({ field }: { field: string }) => {
    if (sortBy !== field)
      return <ChevronsUpDown className="w-3.5 h-3.5 opacity-30" />;
    return sortDescending ? (
      <ChevronDown className="w-3.5 h-3.5 text-indigo-400" />
    ) : (
      <ChevronUp className="w-3.5 h-3.5 text-indigo-400" />
    );
  };

  if (isLoading && !data) {
    return (
      <div className="rounded-xl border border-[var(--color-border)] overflow-hidden">
        <div className="animate-pulse p-8 space-y-3">
          {[...Array(5)].map((_, i) => (
            <div
              key={i}
              className="h-10 bg-[var(--color-border)] rounded-lg"
              style={{ opacity: 1 - i * 0.15 }}
            />
          ))}
        </div>
      </div>
    );
  }

  const items = data?.items ?? [];
  const hasActions = canEdit || canDelete;

  return (
    <div className="space-y-4">
      {/* Table */}
      <div className="rounded-xl border border-[var(--color-border)] overflow-hidden bg-[var(--color-card-bg)]">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg)]/50">
                {visibleFields.map((field) => (
                  <th
                    key={field.name}
                    onClick={() => onSortChange(field.name)}
                    className={cn(
                      "px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider",
                      "text-[var(--color-text-muted)] cursor-pointer select-none",
                      "hover:text-[var(--color-text)] transition-colors duration-200"
                    )}
                  >
                    <div className="flex items-center gap-1.5">
                      {field.label}
                      <SortIcon field={field.name} />
                    </div>
                  </th>
                ))}
                {hasActions && (
                  <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-[var(--color-text-muted)] w-20">
                    İşlem
                  </th>
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {items.length === 0 ? (
                <tr>
                  <td
                    colSpan={visibleFields.length + (hasActions ? 1 : 0)}
                    className="px-4 py-12 text-center text-[var(--color-text-muted)] text-sm"
                  >
                    Kayıt bulunamadı
                  </td>
                </tr>
              ) : (
                items.map((row, idx) => {
                  const rowId = String(row.id ?? idx);
                  return (
                    <tr
                      key={rowId}
                      className={cn(
                        "transition-all duration-300",
                        "hover:bg-[var(--color-bg)]/30",
                        idx % 2 === 0 ? "bg-transparent" : "bg-[var(--color-bg)]/10",
                        highlightId === rowId && "animate-row-highlight",
                        deletingId === rowId && "animate-row-delete"
                      )}
                    >
                      {visibleFields.map((field) => (
                        <td
                          key={field.name}
                          className="px-4 py-3 text-sm text-[var(--color-text)] whitespace-nowrap"
                        >
                          {field.type === "boolean" ? (
                            <span
                              className={cn(
                                "inline-flex px-2 py-0.5 rounded-full text-xs font-medium",
                                row[field.name]
                                  ? "bg-emerald-500/10 text-emerald-400"
                                  : "bg-slate-500/10 text-slate-400"
                              )}
                            >
                              {row[field.name] ? "Aktif" : "Pasif"}
                            </span>
                          ) : field.type === "enum" ? (
                            <span className="inline-flex px-2 py-0.5 rounded-full text-xs font-medium bg-indigo-500/10 text-indigo-400">
                              {formatValue(row[field.name], field)}
                            </span>
                          ) : (
                            formatValue(row[field.name], field)
                          )}
                        </td>
                      ))}
                      {hasActions && (
                        <td className="px-4 py-3 text-right relative">
                          <button
                            onClick={() =>
                              setActiveMenu(
                                activeMenu === rowId ? null : rowId
                              )
                            }
                            className="p-1.5 rounded-lg hover:bg-[var(--color-border)] transition-colors"
                          >
                            <MoreHorizontal className="w-4 h-4 text-[var(--color-text-muted)]" />
                          </button>
                          {activeMenu === rowId && (
                            <>
                              <div
                                className="fixed inset-0 z-40"
                                onClick={() => setActiveMenu(null)}
                              />
                              <div className="absolute right-4 top-full z-50 mt-1 w-36 rounded-lg border border-[var(--color-border)] bg-[var(--color-card-bg)] shadow-xl py-1 animate-fade-in">
                                {canEdit && (
                                  <button
                                    onClick={() => {
                                      onEditClick(row);
                                      setActiveMenu(null);
                                    }}
                                    className="flex items-center gap-2 w-full px-3 py-2 text-sm text-[var(--color-text)] hover:bg-[var(--color-bg)] transition-colors"
                                  >
                                    <Edit2 className="w-3.5 h-3.5" />
                                    Düzenle
                                  </button>
                                )}
                                {canDelete && (
                                  <button
                                    onClick={() => {
                                      onDeleteClick(rowId);
                                      setActiveMenu(null);
                                    }}
                                    className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-400 hover:bg-red-500/10 transition-colors"
                                  >
                                    <Trash2 className="w-3.5 h-3.5" />
                                    Sil
                                  </button>
                                )}
                              </div>
                            </>
                          )}
                        </td>
                      )}
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between px-1">
          <p className="text-sm text-[var(--color-text-muted)]">
            Toplam {data.totalCount} kayıt — Sayfa {data.pageNumber} /{" "}
            {data.totalPages}
          </p>
          <div className="flex items-center gap-2">
            <button
              onClick={() => onPageChange(data.pageNumber - 1)}
              disabled={!data.hasPreviousPage}
              className={cn(
                "p-2 rounded-lg border border-[var(--color-border)]",
                "transition-all duration-200",
                data.hasPreviousPage
                  ? "hover:bg-[var(--color-border)] text-[var(--color-text)]"
                  : "opacity-30 cursor-not-allowed text-[var(--color-text-muted)]"
              )}
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <button
              onClick={() => onPageChange(data.pageNumber + 1)}
              disabled={!data.hasNextPage}
              className={cn(
                "p-2 rounded-lg border border-[var(--color-border)]",
                "transition-all duration-200",
                data.hasNextPage
                  ? "hover:bg-[var(--color-border)] text-[var(--color-text)]"
                  : "opacity-30 cursor-not-allowed text-[var(--color-text-muted)]"
              )}
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
