"use client";

import { useEffect, useState } from "react";
import { apiClient } from "@/lib/api-client";

interface HealthEntry {
  name: string;
  status: string;
  duration: number;
  description: string | null;
  error: string | null;
  tags: string[];
}

interface ModuleInfo {
  name: string;
  schema: string;
  status: string;
}

interface SystemInfo {
  framework: string;
  version: string;
  runtime: string;
  os: string;
  architecture: string;
  environment: string;
  machineName: string;
  processorCount: number;
  startTime: string;
  workingSet: number;
}

interface CacheStatus {
  provider: string;
  note: string;
  status: string;
}

export default function SystemPage() {
  const [health, setHealth] = useState<{ status: string; duration: number; entries: HealthEntry[] } | null>(null);
  const [modules, setModules] = useState<{ totalModules: number; modules: ModuleInfo[] } | null>(null);
  const [systemInfo, setSystemInfo] = useState<SystemInfo | null>(null);
  const [cacheStatus, setCacheStatus] = useState<CacheStatus | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchAll = async () => {
      try {
        const [healthRes, modulesRes, infoRes, cacheRes] = await Promise.allSettled([
          apiClient.get("/api/admin/system/health"),
          apiClient.get("/api/admin/system/modules"),
          apiClient.get("/api/admin/system/info"),
          apiClient.get("/api/admin/cache/status"),
        ]);
        if (healthRes.status === "fulfilled") setHealth(healthRes.value.data);
        if (modulesRes.status === "fulfilled") setModules(modulesRes.value.data);
        if (infoRes.status === "fulfilled") setSystemInfo(infoRes.value.data);
        if (cacheRes.status === "fulfilled") setCacheStatus(cacheRes.value.data);
      } finally {
        setLoading(false);
      }
    };
    fetchAll();
  }, []);

  const refreshHealth = async () => {
    const res = await apiClient.get("/api/admin/system/health");
    setHealth(res.data);
  };

  const statusIcon = (status: string) => {
    if (status === "Healthy" || status === "Active") return "🟢";
    if (status === "Degraded") return "🟡";
    return "🔴";
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="w-8 h-8 border-2 border-amber-400 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">System Health</h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">Sistem durumu ve modül bilgileri</p>
        </div>
        <button onClick={refreshHealth}
          className="px-4 py-2 bg-gradient-to-r from-amber-500 to-orange-500 text-white text-sm font-medium rounded-xl hover:shadow-lg hover:shadow-amber-500/25 transition-all">
          🔄 Yenile
        </button>
      </div>

      {/* ── Overall Health ────────────── */}
      {health && (
        <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-6">
          <div className="flex items-center justify-between mb-6">
            <div className="flex items-center gap-3">
              <span className="text-3xl">{statusIcon(health.status)}</span>
              <div>
                <h2 className="text-xl font-bold text-[var(--color-text)]">{health.status}</h2>
                <p className="text-sm text-[var(--color-text-muted)]">
                  Toplam süre: {health.duration.toFixed(0)}ms
                </p>
              </div>
            </div>
          </div>

          <div className="space-y-3">
            {health.entries.map((e) => (
              <div key={e.name} className="flex items-center justify-between p-4 rounded-xl bg-[var(--color-bg-secondary)] border border-[var(--color-border)]">
                <div className="flex items-center gap-3">
                  <span>{statusIcon(e.status)}</span>
                  <div>
                    <p className="font-medium text-[var(--color-text)]">{e.name}</p>
                    {e.description && <p className="text-xs text-[var(--color-text-muted)]">{e.description}</p>}
                    {e.error && <p className="text-xs text-red-400 mt-1">{e.error}</p>}
                    {e.tags.length > 0 && (
                      <div className="flex gap-1 mt-1">
                        {e.tags.map(t => (
                          <span key={t} className="text-[10px] px-1.5 py-0.5 rounded bg-[var(--color-bg-tertiary)] text-[var(--color-text-muted)]">
                            {t}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
                <span className="text-sm font-mono text-[var(--color-text-muted)]">{e.duration.toFixed(0)}ms</span>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* ── System Info ────────────── */}
        {systemInfo && (
          <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-6">
            <h2 className="text-lg font-semibold text-[var(--color-text)] mb-4">Sistem Bilgisi</h2>
            <div className="space-y-2">
              {[
                ["Framework", systemInfo.framework],
                ["Versiyon", systemInfo.version],
                ["Runtime", systemInfo.runtime],
                ["OS", systemInfo.os],
                ["Mimari", systemInfo.architecture],
                ["Ortam", systemInfo.environment],
                ["Makine", systemInfo.machineName],
                ["İşlemci", `${systemInfo.processorCount} Core`],
                ["Bellek", `${systemInfo.workingSet} MB`],
                ["Başlangıç", new Date(systemInfo.startTime).toLocaleString("tr-TR")],
              ].map(([label, value]) => (
                <div key={label} className="flex items-center justify-between py-2 px-3 rounded-lg hover:bg-[var(--color-bg-secondary)] transition-colors">
                  <span className="text-sm text-[var(--color-text-muted)]">{label}</span>
                  <span className="text-sm font-mono text-[var(--color-text)]">{value}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* ── Cache Status ───────────── */}
        <div className="space-y-6">
          {cacheStatus && (
            <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-6">
              <h2 className="text-lg font-semibold text-[var(--color-text)] mb-4">Cache Durumu</h2>
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-[var(--color-text-muted)]">Provider</span>
                  <span className="text-sm font-medium text-[var(--color-text)]">{cacheStatus.provider}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-[var(--color-text-muted)]">Durum</span>
                  <span className="flex items-center gap-1.5">
                    <span className="w-2 h-2 rounded-full bg-emerald-400" />
                    <span className="text-sm font-medium text-emerald-400">{cacheStatus.status}</span>
                  </span>
                </div>
                <p className="text-xs text-[var(--color-text-muted)] pt-2 border-t border-[var(--color-border)]">
                  {cacheStatus.note}
                </p>
              </div>
            </div>
          )}

          {/* ── Modules ─────────────── */}
          {modules && (
            <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-6">
              <h2 className="text-lg font-semibold text-[var(--color-text)] mb-4">
                Modüller ({modules.totalModules})
              </h2>
              <div className="space-y-2">
                {modules.modules.map((m) => (
                  <div key={m.name} className="flex items-center justify-between py-2 px-3 rounded-lg hover:bg-[var(--color-bg-secondary)] transition-colors">
                    <div className="flex items-center gap-2">
                      <span className="w-2 h-2 rounded-full bg-emerald-400" />
                      <span className="text-sm font-medium text-[var(--color-text)]">{m.name}</span>
                    </div>
                    <span className="text-xs font-mono text-[var(--color-text-muted)]">{m.schema}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
