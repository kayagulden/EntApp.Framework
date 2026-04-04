"use client";

import { Plus, Search, RefreshCw, Download, Upload } from "lucide-react";
import { cn } from "@/lib/utils";

interface DynamicToolbarProps {
  title: string;
  searchValue: string;
  onSearchChange: (value: string) => void;
  onCreateClick: () => void;
  onRefresh: () => void;
  onExportClick: () => void;
  onImportClick: () => void;
  canCreate: boolean;
  isLoading?: boolean;
}

/**
 * Dynamic CRUD toolbar: başlık, arama, dışa aktar, içe aktar, yeni ekle.
 */
export function DynamicToolbar({
  title,
  searchValue,
  onSearchChange,
  onCreateClick,
  onRefresh,
  onExportClick,
  onImportClick,
  canCreate,
  isLoading = false,
}: DynamicToolbarProps) {
  return (
    <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 mb-6">
      {/* Title */}
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">
          {title}
        </h1>
      </div>

      {/* Actions */}
      <div className="flex items-center gap-2 w-full sm:w-auto flex-wrap">
        {/* Search */}
        <div className="relative flex-1 sm:flex-initial">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
          <input
            type="text"
            value={searchValue}
            onChange={(e) => onSearchChange(e.target.value)}
            placeholder="Ara..."
            className={cn(
              "w-full sm:w-56 pl-9 pr-3 py-2 rounded-lg text-sm",
              "bg-[var(--color-input-bg)] border border-[var(--color-border)]",
              "text-[var(--color-text)] placeholder:text-[var(--color-text-muted)]",
              "focus:outline-none focus:ring-2 focus:ring-indigo-500/40 focus:border-indigo-500",
              "transition-all duration-200"
            )}
          />
        </div>

        {/* Refresh */}
        <button
          onClick={onRefresh}
          disabled={isLoading}
          className={cn(
            "p-2 rounded-lg border border-[var(--color-border)]",
            "bg-[var(--color-input-bg)] text-[var(--color-text-muted)]",
            "hover:text-[var(--color-text)] hover:border-[var(--color-text-muted)]",
            "transition-all duration-200",
            isLoading && "animate-spin"
          )}
          title="Yenile"
        >
          <RefreshCw className="w-4 h-4" />
        </button>

        {/* Export */}
        <button
          onClick={onExportClick}
          className={cn(
            "flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm",
            "border border-[var(--color-border)]",
            "bg-[var(--color-input-bg)] text-[var(--color-text-muted)]",
            "hover:text-[var(--color-text)] hover:border-[var(--color-text-muted)]",
            "transition-all duration-200"
          )}
          title="Dışa Aktar"
        >
          <Download className="w-4 h-4" />
          <span className="hidden sm:inline">Dışa Aktar</span>
        </button>

        {/* Import */}
        <button
          onClick={onImportClick}
          className={cn(
            "flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm",
            "border border-[var(--color-border)]",
            "bg-[var(--color-input-bg)] text-[var(--color-text-muted)]",
            "hover:text-[var(--color-text)] hover:border-[var(--color-text-muted)]",
            "transition-all duration-200"
          )}
          title="İçe Aktar"
        >
          <Upload className="w-4 h-4" />
          <span className="hidden sm:inline">İçe Aktar</span>
        </button>

        {/* Create */}
        {canCreate && (
          <button
            onClick={onCreateClick}
            className={cn(
              "flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium",
              "bg-indigo-600 text-white",
              "hover:bg-indigo-700 active:bg-indigo-800",
              "shadow-md shadow-indigo-500/20",
              "transition-all duration-200"
            )}
          >
            <Plus className="w-4 h-4" />
            Yeni Ekle
          </button>
        )}
      </div>
    </div>
  );
}
