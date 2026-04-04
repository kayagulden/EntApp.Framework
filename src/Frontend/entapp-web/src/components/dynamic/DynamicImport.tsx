"use client";

import { useState, useCallback, useRef } from "react";
import {
  X,
  Upload,
  ArrowRight,
  ArrowLeft,
  Check,
  AlertCircle,
  Loader2,
  FileUp,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { apiClient } from "@/lib/api-client";

const DYNAMIC_API_BASE = "/api/v1/dynamic";

interface ImportPreview {
  fileHeaders: string[];
  entityFields: { name: string; label: string; type: string; required: boolean }[];
  suggestedMapping: Record<number, string>;
  previewRows: string[][];
  totalRowCount: number;
}

interface ImportResult {
  successCount: number;
  errorCount: number;
  totalCount: number;
  errors: { rowNumber: number; field: string; message: string }[];
}

interface DynamicImportProps {
  entityName: string;
  title: string;
  isOpen: boolean;
  onClose: () => void;
  onComplete: () => void;
}

/**
 * 3 adımlı import wizard:
 * 1. Dosya yükleme (drag & drop)
 * 2. Kolon eşleştirme
 * 3. Sonuç
 */
export function DynamicImport({
  entityName,
  title,
  isOpen,
  onClose,
  onComplete,
}: DynamicImportProps) {
  const [step, setStep] = useState(1);
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<ImportPreview | null>(null);
  const [mapping, setMapping] = useState<Record<number, string>>({});
  const [result, setResult] = useState<ImportResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [dragOver, setDragOver] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const reset = useCallback(() => {
    setStep(1);
    setFile(null);
    setPreview(null);
    setMapping({});
    setResult(null);
    setLoading(false);
  }, []);

  const handleClose = () => {
    reset();
    onClose();
  };

  // ── Step 1: File Upload ────────────────────────
  const handleFileSelect = async (selectedFile: File) => {
    setFile(selectedFile);
    setLoading(true);
    try {
      const formData = new FormData();
      formData.append("file", selectedFile);

      const { data } = await apiClient.post<ImportPreview>(
        `${DYNAMIC_API_BASE}/${entityName}/import/preview`,
        formData,
        { headers: { "Content-Type": "multipart/form-data" } }
      );

      setPreview(data);
      setMapping(data.suggestedMapping);
      setStep(2);
    } catch {
      // TODO: toast error
    } finally {
      setLoading(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    const droppedFile = e.dataTransfer.files[0];
    if (droppedFile) handleFileSelect(droppedFile);
  };

  // ── Step 2 → 3: Execute Import ────────────────
  const handleImport = async () => {
    if (!file) return;
    setLoading(true);
    try {
      const formData = new FormData();
      formData.append("file", file);

      const mappingStr = JSON.stringify(mapping);
      const { data } = await apiClient.post<ImportResult>(
        `${DYNAMIC_API_BASE}/${entityName}/import?mapping=${encodeURIComponent(mappingStr)}`,
        formData,
        { headers: { "Content-Type": "multipart/form-data" } }
      );

      setResult(data);
      setStep(3);
      if (data.successCount > 0) onComplete();
    } catch {
      // TODO: toast error
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <>
      <div
        className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm animate-fade-in"
        onClick={handleClose}
      />
      <div
        className={cn(
          "fixed right-0 top-0 z-50 h-full w-full max-w-2xl",
          "bg-[var(--color-card-bg)] border-l border-[var(--color-border)]",
          "shadow-2xl shadow-black/20 flex flex-col animate-slide-in-right"
        )}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
          <div>
            <h2 className="text-lg font-semibold text-[var(--color-text)]">
              {title} — İçe Aktar
            </h2>
            <div className="flex items-center gap-2 mt-1">
              {[1, 2, 3].map((s) => (
                <div
                  key={s}
                  className={cn(
                    "h-1.5 rounded-full transition-all duration-300",
                    s <= step ? "bg-indigo-500 w-8" : "bg-[var(--color-border)] w-4"
                  )}
                />
              ))}
              <span className="text-xs text-[var(--color-text-muted)] ml-2">
                {step === 1 ? "Dosya Yükle" : step === 2 ? "Kolon Eşleştir" : "Sonuç"}
              </span>
            </div>
          </div>
          <button onClick={handleClose} className="p-2 rounded-lg hover:bg-[var(--color-border)] transition-colors">
            <X className="w-5 h-5 text-[var(--color-text-muted)]" />
          </button>
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto p-6">
          {/* Step 1: File Upload */}
          {step === 1 && (
            <div
              onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
              onDragLeave={() => setDragOver(false)}
              onDrop={handleDrop}
              onClick={() => inputRef.current?.click()}
              className={cn(
                "flex flex-col items-center justify-center gap-4 p-12 rounded-2xl",
                "border-2 border-dashed cursor-pointer transition-all duration-200",
                dragOver
                  ? "border-indigo-500 bg-indigo-500/5"
                  : "border-[var(--color-border)] hover:border-indigo-500/50 hover:bg-indigo-500/5"
              )}
            >
              {loading ? (
                <Loader2 className="w-12 h-12 text-indigo-400 animate-spin" />
              ) : (
                <FileUp className="w-12 h-12 text-[var(--color-text-muted)]" />
              )}
              <div className="text-center">
                <p className="text-sm font-medium text-[var(--color-text)]">
                  {loading ? "Dosya analiz ediliyor..." : "Excel veya CSV dosyası sürükleyin"}
                </p>
                <p className="text-xs text-[var(--color-text-muted)] mt-1">
                  veya tıklayarak seçin (.xlsx, .csv)
                </p>
              </div>
              <input
                ref={inputRef}
                type="file"
                accept=".xlsx,.xls,.csv"
                onChange={(e) => {
                  const f = e.target.files?.[0];
                  if (f) handleFileSelect(f);
                }}
                className="hidden"
              />
            </div>
          )}

          {/* Step 2: Column Mapping */}
          {step === 2 && preview && (
            <div className="space-y-4">
              <p className="text-sm text-[var(--color-text-muted)]">
                {preview.totalRowCount} satır bulundu. Kolon eşleştirmesini kontrol edin:
              </p>

              <div className="rounded-xl border border-[var(--color-border)] overflow-hidden">
                <table className="w-full">
                  <thead>
                    <tr className="bg-[var(--color-bg)]/50 border-b border-[var(--color-border)]">
                      <th className="px-4 py-2.5 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase">
                        Dosya Kolonu
                      </th>
                      <th className="px-4 py-2.5 text-center text-xs text-[var(--color-text-muted)]">→</th>
                      <th className="px-4 py-2.5 text-left text-xs font-semibold text-[var(--color-text-muted)] uppercase">
                        Entity Alanı
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[var(--color-border)]">
                    {preview.fileHeaders.map((header, idx) => (
                      <tr key={idx}>
                        <td className="px-4 py-2.5 text-sm text-[var(--color-text)]">{header}</td>
                        <td className="px-4 py-2.5 text-center">
                          <ArrowRight className="w-4 h-4 text-[var(--color-text-muted)] mx-auto" />
                        </td>
                        <td className="px-4 py-2.5">
                          <select
                            value={mapping[idx] ?? ""}
                            onChange={(e) => setMapping((prev) => ({ ...prev, [idx]: e.target.value }))}
                            className={cn(
                              "w-full rounded-lg border px-2 py-1.5 text-sm",
                              "bg-[var(--color-input-bg)] border-[var(--color-border)]",
                              "text-[var(--color-text)]",
                              "focus:ring-2 focus:ring-indigo-500/40 focus:border-indigo-500"
                            )}
                          >
                            <option value="">— Atla —</option>
                            {preview.entityFields.map((field) => (
                              <option key={field.name} value={field.name}>
                                {field.label} {field.required ? "*" : ""}
                              </option>
                            ))}
                          </select>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Preview rows */}
              {preview.previewRows.length > 0 && (
                <div>
                  <p className="text-xs text-[var(--color-text-muted)] mb-2">
                    Önizleme (ilk {preview.previewRows.length} satır):
                  </p>
                  <div className="rounded-lg border border-[var(--color-border)] overflow-x-auto">
                    <table className="w-full text-xs">
                      <thead>
                        <tr className="bg-[var(--color-bg)]/30">
                          {preview.fileHeaders.map((h, i) => (
                            <th key={i} className="px-3 py-1.5 text-left text-[var(--color-text-muted)] whitespace-nowrap">
                              {h}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-[var(--color-border)]">
                        {preview.previewRows.map((row, ri) => (
                          <tr key={ri}>
                            {row.map((cell, ci) => (
                              <td key={ci} className="px-3 py-1.5 text-[var(--color-text)] whitespace-nowrap">
                                {cell || "—"}
                              </td>
                            ))}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Step 3: Result */}
          {step === 3 && result && (
            <div className="space-y-6">
              {/* Summary */}
              <div className="flex gap-4">
                <div className="flex-1 p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-center">
                  <p className="text-2xl font-bold text-emerald-400">{result.successCount}</p>
                  <p className="text-xs text-emerald-400/70 mt-1">Başarılı</p>
                </div>
                <div className="flex-1 p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-center">
                  <p className="text-2xl font-bold text-red-400">{result.errorCount}</p>
                  <p className="text-xs text-red-400/70 mt-1">Hatalı</p>
                </div>
                <div className="flex-1 p-4 rounded-xl bg-slate-500/10 border border-slate-500/20 text-center">
                  <p className="text-2xl font-bold text-[var(--color-text)]">{result.totalCount}</p>
                  <p className="text-xs text-[var(--color-text-muted)] mt-1">Toplam</p>
                </div>
              </div>

              {/* Errors */}
              {result.errors.length > 0 && (
                <div>
                  <p className="text-sm font-medium text-red-400 mb-2 flex items-center gap-2">
                    <AlertCircle className="w-4 h-4" />
                    Hatalar
                  </p>
                  <div className="max-h-60 overflow-y-auto rounded-lg border border-red-500/20">
                    {result.errors.map((err, i) => (
                      <div
                        key={i}
                        className="px-4 py-2 text-xs border-b border-red-500/10 last:border-0"
                      >
                        <span className="text-red-400 font-mono">Satır {err.rowNumber}</span>
                        {err.field && (
                          <span className="text-[var(--color-text-muted)]"> [{err.field}]</span>
                        )}
                        <span className="text-[var(--color-text)]"> — {err.message}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {result.successCount > 0 && result.errorCount === 0 && (
                <div className="flex flex-col items-center gap-2 py-4">
                  <div className="w-12 h-12 rounded-full bg-emerald-500/10 flex items-center justify-center">
                    <Check className="w-6 h-6 text-emerald-400" />
                  </div>
                  <p className="text-sm text-emerald-400 font-medium">
                    Tüm kayıtlar başarıyla aktarıldı!
                  </p>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between px-6 py-4 border-t border-[var(--color-border)] bg-[var(--color-bg)]/50">
          <div>
            {step === 2 && (
              <button
                onClick={() => { setStep(1); setFile(null); setPreview(null); }}
                className="flex items-center gap-2 px-4 py-2 rounded-lg text-sm text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-colors"
              >
                <ArrowLeft className="w-4 h-4" />
                Geri
              </button>
            )}
          </div>
          <div className="flex gap-3">
            <button
              onClick={handleClose}
              className={cn(
                "px-4 py-2 rounded-lg text-sm font-medium",
                "border border-[var(--color-border)] text-[var(--color-text-muted)]",
                "hover:text-[var(--color-text)] hover:bg-[var(--color-border)]",
                "transition-all duration-200"
              )}
            >
              {step === 3 ? "Kapat" : "İptal"}
            </button>
            {step === 2 && (
              <button
                onClick={handleImport}
                disabled={loading}
                className={cn(
                  "flex items-center gap-2 px-5 py-2 rounded-lg text-sm font-medium",
                  "bg-indigo-600 text-white hover:bg-indigo-700",
                  "shadow-md shadow-indigo-500/20 transition-all duration-200",
                  "disabled:opacity-60"
                )}
              >
                {loading ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <Upload className="w-4 h-4" />
                )}
                İçe Aktar
              </button>
            )}
          </div>
        </div>
      </div>
    </>
  );
}
