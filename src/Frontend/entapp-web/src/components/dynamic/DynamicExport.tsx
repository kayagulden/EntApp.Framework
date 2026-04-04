"use client";

import { X, FileSpreadsheet, FileText, Download, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import { apiClient } from "@/lib/api-client";
import { useState } from "react";

interface DynamicExportProps {
  entityName: string;
  title: string;
  isOpen: boolean;
  onClose: () => void;
}

const DYNAMIC_API_BASE = "/api/v1/dynamic";

/**
 * Export format seçim dialogu.
 * Excel veya CSV formatında dosya indirir.
 */
export function DynamicExport({
  entityName,
  title,
  isOpen,
  onClose,
}: DynamicExportProps) {
  const [isExporting, setIsExporting] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleExport = async (format: "xlsx" | "csv") => {
    setIsExporting(format);
    try {
      const response = await apiClient.get(
        `${DYNAMIC_API_BASE}/${entityName}/export?format=${format}`,
        { responseType: "blob" }
      );

      const blob = new Blob([response.data]);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `${entityName}.${format}`;
      a.click();
      URL.revokeObjectURL(url);
      onClose();
    } catch {
      // TODO: toast error
    } finally {
      setIsExporting(null);
    }
  };

  const handleDownloadTemplate = async () => {
    setIsExporting("template");
    try {
      const response = await apiClient.get(
        `${DYNAMIC_API_BASE}/${entityName}/import-template`,
        { responseType: "blob" }
      );

      const blob = new Blob([response.data]);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `${entityName}_template.xlsx`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      // TODO: toast error
    } finally {
      setIsExporting(null);
    }
  };

  return (
    <>
      <div
        className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm animate-fade-in"
        onClick={onClose}
      />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div
          className={cn(
            "w-full max-w-md rounded-2xl",
            "bg-[var(--color-card-bg)] border border-[var(--color-border)]",
            "shadow-2xl shadow-black/20 animate-fade-in"
          )}
        >
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
            <h2 className="text-lg font-semibold text-[var(--color-text)]">
              {title} — Dışa Aktar
            </h2>
            <button
              onClick={onClose}
              className="p-2 rounded-lg hover:bg-[var(--color-border)] transition-colors"
            >
              <X className="w-5 h-5 text-[var(--color-text-muted)]" />
            </button>
          </div>

          {/* Format options */}
          <div className="p-6 space-y-3">
            <button
              onClick={() => handleExport("xlsx")}
              disabled={isExporting !== null}
              className={cn(
                "flex items-center gap-4 w-full p-4 rounded-xl border",
                "border-[var(--color-border)] bg-[var(--color-bg)]",
                "hover:border-emerald-500/50 hover:bg-emerald-500/5",
                "transition-all duration-200 group"
              )}
            >
              <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-emerald-500/10">
                {isExporting === "xlsx" ? (
                  <Loader2 className="w-5 h-5 text-emerald-400 animate-spin" />
                ) : (
                  <FileSpreadsheet className="w-5 h-5 text-emerald-400" />
                )}
              </div>
              <div className="text-left">
                <p className="text-sm font-medium text-[var(--color-text)]">
                  Excel (.xlsx)
                </p>
                <p className="text-xs text-[var(--color-text-muted)]">
                  Formatlı tablo, filtreler korunur
                </p>
              </div>
            </button>

            <button
              onClick={() => handleExport("csv")}
              disabled={isExporting !== null}
              className={cn(
                "flex items-center gap-4 w-full p-4 rounded-xl border",
                "border-[var(--color-border)] bg-[var(--color-bg)]",
                "hover:border-blue-500/50 hover:bg-blue-500/5",
                "transition-all duration-200 group"
              )}
            >
              <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-blue-500/10">
                {isExporting === "csv" ? (
                  <Loader2 className="w-5 h-5 text-blue-400 animate-spin" />
                ) : (
                  <FileText className="w-5 h-5 text-blue-400" />
                )}
              </div>
              <div className="text-left">
                <p className="text-sm font-medium text-[var(--color-text)]">
                  CSV (.csv)
                </p>
                <p className="text-xs text-[var(--color-text-muted)]">
                  Sade metin, noktalı virgül ayraçlı
                </p>
              </div>
            </button>

            {/* Separator */}
            <div className="flex items-center gap-2 py-2">
              <div className="h-px flex-1 bg-[var(--color-border)]" />
              <span className="text-[10px] uppercase tracking-wider text-[var(--color-text-muted)]">
                Şablon
              </span>
              <div className="h-px flex-1 bg-[var(--color-border)]" />
            </div>

            <button
              onClick={handleDownloadTemplate}
              disabled={isExporting !== null}
              className={cn(
                "flex items-center gap-4 w-full p-4 rounded-xl border",
                "border-[var(--color-border)] bg-[var(--color-bg)]",
                "hover:border-indigo-500/50 hover:bg-indigo-500/5",
                "transition-all duration-200"
              )}
            >
              <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-indigo-500/10">
                {isExporting === "template" ? (
                  <Loader2 className="w-5 h-5 text-indigo-400 animate-spin" />
                ) : (
                  <Download className="w-5 h-5 text-indigo-400" />
                )}
              </div>
              <div className="text-left">
                <p className="text-sm font-medium text-[var(--color-text)]">
                  İçe Aktarma Şablonu
                </p>
                <p className="text-xs text-[var(--color-text-muted)]">
                  Boş Excel şablonu — doldurup import edin
                </p>
              </div>
            </button>
          </div>
        </div>
      </div>
    </>
  );
}
