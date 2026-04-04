"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import { Search, ChevronDown, X, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import { apiClient } from "@/lib/api-client";

const DYNAMIC_API_BASE = "/api/v1/dynamic";

interface LookupOption {
  id: string;
  displayText: string;
}

interface DynamicLookupProps {
  /** Lookup hedef entity adı (ör: "Country") */
  entityName: string;
  /** Seçilen ID */
  value: string | null;
  /** Seçim değiştiğinde */
  onChange: (value: string | null) => void;
  /** Placeholder metin */
  placeholder?: string;
  disabled?: boolean;
  error?: string;
}

/**
 * Async arama destekli lookup combobox.
 * /api/v1/dynamic/{entity}/lookup endpoint'ini kullanır.
 */
export function DynamicLookup({
  entityName,
  value,
  onChange,
  placeholder = "Seçiniz...",
  disabled = false,
  error,
}: DynamicLookupProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [options, setOptions] = useState<LookupOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedLabel, setSelectedLabel] = useState<string>("");
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout>>(undefined);

  // Seçili ID'nin label'ını çöz
  useEffect(() => {
    if (!value) {
      setSelectedLabel("");
      return;
    }

    // Options'ta zaten var mı?
    const found = options.find((o) => o.id === value);
    if (found) {
      setSelectedLabel(found.displayText);
      return;
    }

    // API'den çek
    apiClient
      .get(`${DYNAMIC_API_BASE}/${entityName}/${value}`)
      .then((res) => {
        const name = res.data?.name ?? res.data?.code ?? res.data?.displayText ?? value;
        setSelectedLabel(String(name));
      })
      .catch(() => setSelectedLabel(value));
  }, [value, entityName]); // eslint-disable-line react-hooks/exhaustive-deps

  // Arama debounce
  const fetchOptions = useCallback(
    async (searchTerm: string) => {
      setLoading(true);
      try {
        const { data } = await apiClient.get<LookupOption[]>(
          `${DYNAMIC_API_BASE}/${entityName}/lookup`,
          { params: { search: searchTerm || undefined, take: 20 } }
        );
        setOptions(data);
      } catch {
        setOptions([]);
      } finally {
        setLoading(false);
      }
    },
    [entityName]
  );

  useEffect(() => {
    if (!isOpen) return;

    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      fetchOptions(search);
    }, 300);

    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, [search, isOpen, fetchOptions]);

  // Dropdown dışına tıklanınca kapat
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  const handleSelect = (option: LookupOption) => {
    onChange(option.id);
    setSelectedLabel(option.displayText);
    setIsOpen(false);
    setSearch("");
  };

  const handleClear = (e: React.MouseEvent) => {
    e.stopPropagation();
    onChange(null);
    setSelectedLabel("");
    setSearch("");
  };

  return (
    <div ref={containerRef} className="relative">
      {/* Trigger */}
      <button
        type="button"
        onClick={() => {
          if (!disabled) {
            setIsOpen(!isOpen);
            setTimeout(() => inputRef.current?.focus(), 50);
          }
        }}
        disabled={disabled}
        className={cn(
          "w-full flex items-center justify-between gap-2 rounded-lg border px-3 py-2 text-sm",
          "bg-[var(--color-input-bg)] border-[var(--color-border)]",
          "text-[var(--color-text)] transition-colors duration-200",
          "focus:outline-none focus:ring-2 focus:ring-indigo-500/40 focus:border-indigo-500",
          disabled && "opacity-60 cursor-not-allowed",
          error && "border-red-500 focus:ring-red-500/40",
          isOpen && "ring-2 ring-indigo-500/40 border-indigo-500"
        )}
      >
        <span className={cn(!selectedLabel && "text-[var(--color-text-muted)]")}>
          {selectedLabel || placeholder}
        </span>
        <div className="flex items-center gap-1">
          {value && !disabled && (
            <X
              className="w-3.5 h-3.5 text-[var(--color-text-muted)] hover:text-[var(--color-text)]"
              onClick={handleClear}
            />
          )}
          <ChevronDown
            className={cn(
              "w-4 h-4 text-[var(--color-text-muted)] transition-transform duration-200",
              isOpen && "rotate-180"
            )}
          />
        </div>
      </button>

      {/* Dropdown */}
      {isOpen && (
        <div className="absolute z-50 w-full mt-1 rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] shadow-xl animate-fade-in overflow-hidden">
          {/* Search input */}
          <div className="p-2 border-b border-[var(--color-border)]">
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-[var(--color-text-muted)]" />
              <input
                ref={inputRef}
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Ara..."
                className={cn(
                  "w-full pl-8 pr-3 py-1.5 rounded-lg text-sm",
                  "bg-[var(--color-bg)] border border-[var(--color-border)]",
                  "text-[var(--color-text)] placeholder:text-[var(--color-text-muted)]",
                  "focus:outline-none focus:ring-1 focus:ring-indigo-500/40"
                )}
              />
            </div>
          </div>

          {/* Options */}
          <div className="max-h-48 overflow-y-auto">
            {loading ? (
              <div className="flex items-center justify-center py-4">
                <Loader2 className="w-4 h-4 text-indigo-400 animate-spin" />
              </div>
            ) : options.length === 0 ? (
              <div className="py-4 text-center text-xs text-[var(--color-text-muted)]">
                Sonuç bulunamadı
              </div>
            ) : (
              options.map((opt) => (
                <button
                  key={opt.id}
                  type="button"
                  onClick={() => handleSelect(opt)}
                  className={cn(
                    "w-full text-left px-3 py-2 text-sm transition-colors duration-100",
                    opt.id === value
                      ? "bg-indigo-500/10 text-indigo-400"
                      : "text-[var(--color-text)] hover:bg-[var(--color-bg)]"
                  )}
                >
                  {opt.displayText}
                </button>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
