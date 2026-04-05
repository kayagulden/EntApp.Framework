"use client";

import { useState, useCallback } from "react";
import { useDynamicMeta } from "@/lib/hooks/use-dynamic-meta";
import {
  useDynamicList,
  useDynamicCreate,
  useDynamicUpdate,
  useDynamicDelete,
} from "@/lib/hooks/use-dynamic-crud";
import { useSignalR, type EntityChangePayload } from "@/lib/hooks/use-signalr";
import { DynamicToolbar } from "./DynamicToolbar";
import { DynamicTable } from "./DynamicTable";
import { DynamicForm } from "./DynamicForm";
import { DynamicDetailTable } from "./DynamicDetailTable";
import { DynamicExport } from "./DynamicExport";
import { DynamicImport } from "./DynamicImport";
import { DynamicFilters, type FilterValues } from "./DynamicFilters";
import { AlertTriangle, Database } from "lucide-react";
import type { PagedRequest } from "@/types/dynamic";

interface DynamicPageProps {
  entityName: string;
}

/**
 * Dynamic CRUD orchestrator.
 * Metadata'yı fetch edip toolbar, table, form, export ve import'u koordine eder.
 */
export function DynamicPage({ entityName }: DynamicPageProps) {
  // ── State ────────────────────────────────────────────
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState<string | undefined>();
  const [sortDescending, setSortDescending] = useState(false);
  const [page, setPage] = useState(1);
  const [formOpen, setFormOpen] = useState(false);
  const [formMode, setFormMode] = useState<"create" | "edit">("create");
  const [editingRow, setEditingRow] = useState<Record<string, unknown> | null>(null);
  const [selectedRowId, setSelectedRowId] = useState<string | null>(null);
  const [exportOpen, setExportOpen] = useState(false);
  const [importOpen, setImportOpen] = useState(false);
  const [filterValues, setFilterValues] = useState<FilterValues>({});
  const [highlightId, setHighlightId] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  // ── Data Fetching ────────────────────────────────────
  const { data: metadata, isLoading: metaLoading, error: metaError } =
    useDynamicMeta(entityName);

  const queryParams: PagedRequest = {
    pageNumber: page,
    pageSize: 20,
    sortBy,
    sortDescending,
    search: search || undefined,
  };

  const {
    data: listData,
    isLoading: listLoading,
    refetch,
  } = useDynamicList(entityName, queryParams);

  const createMutation = useDynamicCreate(entityName);
  const updateMutation = useDynamicUpdate(entityName);
  const deleteMutation = useDynamicDelete(entityName);

  // ── SignalR Real-time ─────────────────────────────────
  const handleEntityChanged = useCallback(
    (payload: EntityChangePayload) => {
      if (payload.changeType === "Deleted") {
        // Fade-out animasyonu başlat, sonra refetch
        setDeletingId(payload.entityId);
        setTimeout(() => {
          refetch();
          setDeletingId(null);
        }, 500);
      } else {
        // Created/Updated → refetch + highlight
        refetch();
        setHighlightId(payload.entityId);
        setTimeout(() => setHighlightId(null), 2000);
      }
    },
    [refetch]
  );

  useSignalR({
    entityName,
    onEntityChanged: handleEntityChanged,
  });

  // ── Handlers ─────────────────────────────────────────
  const handleSortChange = useCallback(
    (field: string) => {
      if (sortBy === field) {
        setSortDescending(!sortDescending);
      } else {
        setSortBy(field);
        setSortDescending(false);
      }
      setPage(1);
    },
    [sortBy, sortDescending]
  );

  const handleCreateClick = useCallback(() => {
    setEditingRow(null);
    setFormMode("create");
    setFormOpen(true);
  }, []);

  const handleEditClick = useCallback((row: Record<string, unknown>) => {
    setEditingRow(row);
    setFormMode("edit");
    setFormOpen(true);
  }, []);

  const handleRowClick = useCallback((row: Record<string, unknown>) => {
    const id = String(row.id);
    setSelectedRowId((prev) => (prev === id ? null : id));
  }, []);

  const handleDeleteClick = useCallback(
    async (id: string) => {
      if (!confirm("Bu kaydı silmek istediğinizden emin misiniz?")) return;
      await deleteMutation.mutateAsync(id);
    },
    [deleteMutation]
  );

  const handleFormSubmit = useCallback(
    async (data: Record<string, unknown>) => {
      if (formMode === "create") {
        await createMutation.mutateAsync(data);
      } else if (editingRow) {
        await updateMutation.mutateAsync({
          id: String(editingRow.id),
          body: data,
        });
      }
      setFormOpen(false);
      setEditingRow(null);
    },
    [formMode, editingRow, createMutation, updateMutation]
  );

  const handleSearchChange = useCallback((value: string) => {
    setSearch(value);
    setPage(1);
  }, []);

  // ── Loading State ────────────────────────────────────
  if (metaLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="flex flex-col items-center gap-3">
          <Database className="w-8 h-8 text-indigo-400 animate-pulse" />
          <p className="text-sm text-[var(--color-text-muted)]">
            Metadata yükleniyor...
          </p>
        </div>
      </div>
    );
  }

  // ── Error State ──────────────────────────────────────
  if (metaError || !metadata) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="flex flex-col items-center gap-3 text-center">
          <AlertTriangle className="w-8 h-8 text-amber-400" />
          <p className="text-sm text-[var(--color-text-muted)]">
            &apos;{entityName}&apos; için metadata bulunamadı
          </p>
          <p className="text-xs text-[var(--color-text-muted)]">
            Backend&apos;de bu entity&apos;nin [DynamicEntity] attribute&apos;ü ile
            işaretlendiğinden emin olun
          </p>
        </div>
      </div>
    );
  }

  // ── Render ───────────────────────────────────────────
  return (
    <div>
      <DynamicToolbar
        title={metadata.title}
        searchValue={search}
        onSearchChange={handleSearchChange}
        onCreateClick={handleCreateClick}
        onRefresh={() => refetch()}
        onExportClick={() => setExportOpen(true)}
        onImportClick={() => setImportOpen(true)}
        canCreate={metadata.actions.create}
        isLoading={listLoading}
      />

      <DynamicFilters
        fields={metadata.fields}
        values={filterValues}
        onChange={(vals) => {
          setFilterValues(vals);
          setPage(1);
        }}
        onClear={() => {
          setFilterValues({});
          setPage(1);
        }}
      />

      <DynamicTable
        fields={metadata.fields}
        data={listData}
        isLoading={listLoading}
        sortBy={sortBy}
        sortDescending={sortDescending}
        onSortChange={handleSortChange}
        onPageChange={setPage}
        onEditClick={handleEditClick}
        onDeleteClick={handleDeleteClick}
        canEdit={metadata.actions.edit}
        canDelete={metadata.actions.delete}
        highlightId={highlightId}
        deletingId={deletingId}
        onRowClick={metadata.details && metadata.details.length > 0 ? handleRowClick : undefined}
        selectedRowId={selectedRowId}
      />

      {/* Master-Detail Tables */}
      {metadata.details && metadata.details.length > 0 && selectedRowId && (
        <div className="mt-4 space-y-3 pl-4 border-l-2 border-indigo-500/20">
          {metadata.details.map((detail) => (
            <DynamicDetailTable
              key={detail.name}
              detail={detail}
              masterId={selectedRowId}
              masterEntity={entityName}
            />
          ))}
        </div>
      )}

      <DynamicForm
        fields={metadata.fields}
        title={metadata.title}
        mode={formMode}
        initialData={editingRow ?? undefined}
        isOpen={formOpen}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
        onClose={() => {
          setFormOpen(false);
          setEditingRow(null);
        }}
        onSubmit={handleFormSubmit}
      />

      <DynamicExport
        entityName={entityName}
        title={metadata.title}
        isOpen={exportOpen}
        onClose={() => setExportOpen(false)}
      />

      <DynamicImport
        entityName={entityName}
        title={metadata.title}
        isOpen={importOpen}
        onClose={() => setImportOpen(false)}
        onComplete={() => refetch()}
      />
    </div>
  );
}
