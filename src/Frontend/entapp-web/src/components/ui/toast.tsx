"use client";

import * as React from "react";
import { cn } from "@/lib/utils";
import { X, CheckCircle, AlertCircle, Info, AlertTriangle } from "lucide-react";

type ToastVariant = "default" | "success" | "error" | "warning" | "info";

interface Toast {
  id: string;
  title: string;
  description?: string;
  variant: ToastVariant;
}

interface ToastContextType {
  toasts: Toast[];
  toast: (props: Omit<Toast, "id">) => void;
  dismiss: (id: string) => void;
}

const ToastContext = React.createContext<ToastContextType>({
  toasts: [],
  toast: () => {},
  dismiss: () => {},
});

export function useToast() {
  return React.useContext(ToastContext);
}

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = React.useState<Toast[]>([]);

  const toast = React.useCallback((props: Omit<Toast, "id">) => {
    const id = Math.random().toString(36).slice(2);
    setToasts((prev) => [...prev, { ...props, id }]);
    setTimeout(() => setToasts((prev) => prev.filter((t) => t.id !== id)), 5000);
  }, []);

  const dismiss = React.useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  return (
    <ToastContext.Provider value={{ toasts, toast, dismiss }}>
      {children}
      <ToastContainer />
    </ToastContext.Provider>
  );
}

const variantConfig: Record<ToastVariant, { icon: React.ElementType; color: string; bg: string }> = {
  default: { icon: Info, color: "text-[var(--color-text)]", bg: "bg-[var(--color-surface)]" },
  success: { icon: CheckCircle, color: "text-emerald-500", bg: "bg-[var(--color-surface)]" },
  error: { icon: AlertCircle, color: "text-red-500", bg: "bg-[var(--color-surface)]" },
  warning: { icon: AlertTriangle, color: "text-amber-500", bg: "bg-[var(--color-surface)]" },
  info: { icon: Info, color: "text-blue-500", bg: "bg-[var(--color-surface)]" },
};

function ToastContainer() {
  const { toasts, dismiss } = useToast();

  return (
    <div className="fixed bottom-4 right-4 z-[100] flex flex-col gap-2 max-w-sm">
      {toasts.map((t) => {
        const config = variantConfig[t.variant];
        return (
          <div
            key={t.id}
            className={cn(
              "flex items-start gap-3 p-4 rounded-xl border border-[var(--color-border)] shadow-lg animate-slide-in",
              config.bg
            )}
          >
            <config.icon className={cn("w-5 h-5 shrink-0 mt-0.5", config.color)} />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-[var(--color-text)]">{t.title}</p>
              {t.description && (
                <p className="text-xs text-[var(--color-text-muted)] mt-0.5">{t.description}</p>
              )}
            </div>
            <button onClick={() => dismiss(t.id)} className="text-[var(--color-text-muted)] hover:text-[var(--color-text)]">
              <X className="w-4 h-4" />
            </button>
          </div>
        );
      })}
    </div>
  );
}
