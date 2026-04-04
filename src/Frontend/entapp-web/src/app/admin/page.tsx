"use client";

import { useEffect, useState } from "react";
import { apiClient } from "@/lib/api-client";

interface SystemInfo {
  framework: string;
  version: string;
  runtime: string;
  os: string;
  environment: string;
  processorCount: number;
  workingSet: number;
}

interface HealthEntry {
  name: string;
  status: string;
  duration: number;
}

interface ModuleInfo {
  name: string;
  schema: string;
  status: string;
}

export default function AdminDashboard() {
  const [systemInfo, setSystemInfo] = useState<SystemInfo | null>(null);
  const [health, setHealth] = useState<{ status: string; entries: HealthEntry[] } | null>(null);
  const [modules, setModules] = useState<{ totalModules: number; modules: ModuleInfo[] } | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchAll = async () => {
      try {
        const [infoRes, healthRes, modulesRes] = await Promise.allSettled([
          apiClient.get("/api/admin/system/info"),
          apiClient.get("/api/admin/system/health"),
          apiClient.get("/api/admin/system/modules"),
        ]);
        if (infoRes.status === "fulfilled") setSystemInfo(infoRes.value.data);
        if (healthRes.status === "fulfilled") setHealth(healthRes.value.data);
        if (modulesRes.status === "fulfilled") setModules(modulesRes.value.data);
      } finally {
        setLoading(false);
      }
    };
    fetchAll();
  }, []);

  const StatusBadge = ({ status }: { status: string }) => {
    const colors: Record<string, string> = {
      Healthy: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
      Active: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
      Degraded: "bg-amber-500/10 text-amber-400 border-amber-500/20",
      Unhealthy: "bg-red-500/10 text-red-400 border-red-500/20",
    };
    return (
      <span className={`px-2.5 py-0.5 rounded-full text-xs font-medium border ${colors[status] || "bg-slate-500/10 text-slate-400 border-slate-500/20"}`}>
        {status}
      </span>
    );
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
      {/* ── Page Header ──────────────── */}
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">Admin Dashboard</h1>
        <p className="text-sm text-[var(--color-text-muted)] mt-1">
          Sistem durumu ve genel bakış
        </p>
      </div>

      {/* ── Stats Grid ───────────────── */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="Toplam Modül"
          value={modules?.totalModules?.toString() ?? "—"}
          icon="📦"
          gradient="from-indigo-500 to-purple-600"
        />
        <StatCard
          title="Sistem Durumu"
          value={health?.status ?? "—"}
          icon="💚"
          gradient="from-emerald-500 to-teal-600"
        />
        <StatCard
          title="İşlemci"
          value={systemInfo ? `${systemInfo.processorCount} Core` : "—"}
          icon="⚡"
          gradient="from-amber-500 to-orange-600"
        />
        <StatCard
          title="Bellek"
          value={systemInfo ? `${systemInfo.workingSet} MB` : "—"}
          icon="🧠"
          gradient="from-rose-500 to-pink-600"
        />
      </div>

      {/* ── System Info Card ─────────── */}
      {systemInfo && (
        <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-6">
          <h2 className="text-lg font-semibold text-[var(--color-text)] mb-4">Sistem Bilgisi</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
            <InfoRow label="Framework" value={systemInfo.framework} />
            <InfoRow label="Versiyon" value={systemInfo.version} />
            <InfoRow label="Runtime" value={systemInfo.runtime} />
            <InfoRow label="OS" value={systemInfo.os} />
            <InfoRow label="Ortam" value={systemInfo.environment} />
            <InfoRow label="Bellek" value={`${systemInfo.workingSet} MB`} />
          </div>
        </div>
      )}

      {/* ── Health Checks ────────────── */}
      {health && (
        <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-[var(--color-text)]">Health Checks</h2>
            <StatusBadge status={health.status} />
          </div>
          <div className="space-y-2">
            {health.entries.map((e) => (
              <div key={e.name} className="flex items-center justify-between py-2 px-3 rounded-xl hover:bg-[var(--color-bg-secondary)] transition-colors">
                <div className="flex items-center gap-3">
                  <div className={`w-2 h-2 rounded-full ${e.status === "Healthy" ? "bg-emerald-400" : e.status === "Degraded" ? "bg-amber-400" : "bg-red-400"}`} />
                  <span className="text-sm font-medium text-[var(--color-text)]">{e.name}</span>
                </div>
                <div className="flex items-center gap-3">
                  <span className="text-xs text-[var(--color-text-muted)]">{e.duration.toFixed(0)}ms</span>
                  <StatusBadge status={e.status} />
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── Modules Grid ─────────────── */}
      {modules && (
        <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-6">
          <h2 className="text-lg font-semibold text-[var(--color-text)] mb-4">Yüklü Modüller ({modules.totalModules})</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            {modules.modules.map((m) => (
              <div key={m.name} className="flex items-center gap-3 p-3 rounded-xl bg-[var(--color-bg-secondary)] border border-[var(--color-border)] hover:border-amber-500/30 transition-colors">
                <div className="w-2 h-2 rounded-full bg-emerald-400" />
                <div>
                  <p className="text-sm font-medium text-[var(--color-text)]">{m.name}</p>
                  <p className="text-xs text-[var(--color-text-muted)]">{m.schema}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function StatCard({ title, value, icon, gradient }: { title: string; value: string; icon: string; gradient: string }) {
  return (
    <div className="relative overflow-hidden bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] p-5">
      <div className={`absolute top-0 right-0 w-24 h-24 bg-gradient-to-br ${gradient} opacity-5 rounded-full -translate-y-6 translate-x-6`} />
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-[var(--color-text-muted)]">{title}</p>
          <p className="text-2xl font-bold text-[var(--color-text)] mt-1">{value}</p>
        </div>
        <span className="text-2xl">{icon}</span>
      </div>
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between py-2 px-3 rounded-lg bg-[var(--color-bg-secondary)]">
      <span className="text-[var(--color-text-muted)]">{label}</span>
      <span className="font-mono text-[var(--color-text)]">{value}</span>
    </div>
  );
}
