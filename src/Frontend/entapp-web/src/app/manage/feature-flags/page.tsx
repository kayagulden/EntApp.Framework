"use client";

import { useState, useEffect } from "react";

interface FeatureFlag {
  name: string;
  displayName: string;
  isEnabled: boolean;
  description: string | null;
  isGlobal: boolean;
}

export default function ManageFeatureFlagsPage() {
  const [flags, setFlags] = useState<FeatureFlag[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch("/api/manage/feature-flags")
      .then((res) => (res.ok ? res.json() : []))
      .then(setFlags)
      .catch(() => setFlags([]))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">
          Feature Flags
        </h1>
        <p className="text-sm text-[var(--color-text-muted)] mt-1">
          Tenant&apos;ınız için aktif özellikleri yönetin.
        </p>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12">
          <div className="w-6 h-6 border-2 border-teal-500 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : flags.length === 0 ? (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-12 text-center">
          <p className="text-[var(--color-text-muted)]">
            Henüz tanımlanmış feature flag bulunmuyor.
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {flags.map((flag) => (
            <div
              key={flag.name}
              className="flex items-center justify-between p-4 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] hover:border-teal-500/20 transition-all"
            >
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-medium text-[var(--color-text)]">
                    {flag.displayName || flag.name}
                  </span>
                  {flag.isGlobal && (
                    <span className="px-1.5 py-0.5 text-[10px] font-semibold uppercase rounded bg-slate-500/10 text-slate-400">
                      Global
                    </span>
                  )}
                </div>
                {flag.description && (
                  <p className="text-xs text-[var(--color-text-muted)] mt-1">
                    {flag.description}
                  </p>
                )}
              </div>
              <div
                className={`w-10 h-6 rounded-full flex items-center px-1 transition-colors cursor-pointer ${
                  flag.isEnabled ? "bg-teal-500" : "bg-[var(--color-border)]"
                }`}
              >
                <div
                  className={`w-4 h-4 rounded-full bg-white shadow transition-transform ${
                    flag.isEnabled ? "translate-x-4" : "translate-x-0"
                  }`}
                />
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
