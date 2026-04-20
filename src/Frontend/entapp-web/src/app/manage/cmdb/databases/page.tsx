"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import {
  HardDrive, Plus, Search, Loader2, X, Database,
  AlertTriangle, CheckCircle2, Archive, Clock,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface DatabaseData {
  id: string; name: string; code: string; description?: string;
  databaseEngine: string; status: string; criticality: string;
  version?: string; port?: number; sizeGB?: number;
  backupSchedule?: string;
  ownerUserId?: string; adminUserId?: string; createdAt: string;
}

const ENGINE_CONFIG: Record<string, { color: string; bg: string; label: string }> = {
  PostgreSQL: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "PostgreSQL" },
  SQLServer: { color: "text-red-400", bg: "bg-red-500/10 border-red-500/20", label: "SQL Server" },
  Oracle: { color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Oracle" },
  MySQL: { color: "text-cyan-400", bg: "bg-cyan-500/10 border-cyan-500/20", label: "MySQL" },
  MongoDB: { color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "MongoDB" },
  Redis: { color: "text-rose-400", bg: "bg-rose-500/10 border-rose-500/20", label: "Redis" },
  Elasticsearch: { color: "text-yellow-400", bg: "bg-yellow-500/10 border-yellow-500/20", label: "Elasticsearch" },
};

const STATUS_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  Planned: { icon: Clock, color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Planlandı" },
  Active: { icon: CheckCircle2, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Aktif" },
  Deprecated: { icon: AlertTriangle, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Kullanımdan Kaldırılıyor" },
  Retired: { icon: Archive, color: "text-gray-400", bg: "bg-gray-500/10 border-gray-500/20", label: "Emekli" },
};

const CRITICALITY_CONFIG: Record<string, { color: string; bg: string; label: string }> = {
  Low: { color: "text-slate-400", bg: "bg-slate-500/10 border-slate-500/20", label: "Düşük" },
  Medium: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Orta" },
  High: { color: "text-orange-400", bg: "bg-orange-500/10 border-orange-500/20", label: "Yüksek" },
  Critical: { color: "text-red-400", bg: "bg-red-500/10 border-red-500/20", label: "Kritik" },
};

export default function DatabasesPage() {
  const router = useRouter();
  const [databases, setDatabases] = useState<DatabaseData[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [engineFilter, setEngineFilter] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [formData, setFormData] = useState<Record<string, string>>({});
  const [formSaving, setFormSaving] = useState(false);
  const [formError, setFormError] = useState("");

  const fetchDatabases = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (statusFilter) params.set("status", statusFilter);
      if (engineFilter) params.set("databaseEngine", engineFilter);
      const res = await fetch(`/api/pm/databases?${params}`);
      if (res.ok) setDatabases(await res.json());
    } catch { /* */ }
    finally { setLoading(false); }
  }, [statusFilter, engineFilter]);

  useEffect(() => { fetchDatabases(); }, [fetchDatabases]);

  const handleCreate = async () => {
    if (!formData.name?.trim() || !formData.code?.trim()) { setFormError("Ad ve kod zorunludur."); return; }
    setFormSaving(true); setFormError("");
    try {
      const res = await fetch("/api/pm/databases", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: formData.name?.trim(), code: formData.code?.trim(),
          description: formData.description?.trim() || null,
          databaseEngine: formData.databaseEngine || "PostgreSQL",
          criticality: formData.criticality || "Medium",
          version: formData.version?.trim() || null,
          port: formData.port ? parseInt(formData.port) : null,
          backupSchedule: formData.backupSchedule?.trim() || null,
        })
      });
      if (res.ok) { setShowCreate(false); setFormData({}); fetchDatabases(); }
      else { setFormError(`Hata: ${res.status}`); }
    } catch { setFormError("Bağlantı hatası."); }
    finally { setFormSaving(false); }
  };

  const filtered = searchTerm
    ? databases.filter(d => d.name.toLowerCase().includes(searchTerm.toLowerCase()) || d.code.toLowerCase().includes(searchTerm.toLowerCase()))
    : databases;

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-emerald-500 to-teal-600 flex items-center justify-center shadow-lg shadow-emerald-500/25">
            <HardDrive className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">Veritabanları</h1>
            <p className="text-sm text-[var(--color-text-muted)] mt-0.5">{databases.length} veritabanı kayıtlı</p>
          </div>
        </div>
        <button onClick={() => { setShowCreate(true); setFormData({}); }}
          className="flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium bg-emerald-600 text-white hover:bg-emerald-700 shadow-lg shadow-emerald-500/20 transition-all">
          <Plus className="w-4 h-4" /> Yeni Veritabanı
        </button>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
          <input type="text" placeholder="Veritabanı ara..." value={searchTerm} onChange={e => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-emerald-500/40" />
        </div>
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
          <option value="">Tüm Durumlar</option>
          {Object.entries(STATUS_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
        </select>
        <select value={engineFilter} onChange={e => setEngineFilter(e.target.value)}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
          <option value="">Tüm Motorlar</option>
          {Object.entries(ENGINE_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
        </select>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12"><Loader2 className="w-6 h-6 text-emerald-400 animate-spin" /></div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-[var(--color-text-muted)]">
          <HardDrive className="w-12 h-12 opacity-30 mb-3" />
          <p className="text-sm">Henüz veritabanı kaydedilmemiş</p>
          <button onClick={() => { setShowCreate(true); setFormData({}); }}
            className="mt-3 text-xs text-emerald-400 hover:text-emerald-300 transition-colors">+ Yeni Veritabanı Ekle</button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map(db => {
            const engCfg = ENGINE_CONFIG[db.databaseEngine] || ENGINE_CONFIG.PostgreSQL;
            const statusCfg = STATUS_CONFIG[db.status] || STATUS_CONFIG.Planned;
            const critCfg = CRITICALITY_CONFIG[db.criticality] || CRITICALITY_CONFIG.Medium;
            const StatusIcon = statusCfg.icon;
            return (
              <div key={db.id} onClick={() => router.push(`/manage/cmdb/databases/${db.id}`)}
                className="group rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5 hover:border-emerald-500/40 hover:shadow-lg hover:shadow-emerald-500/5 transition-all duration-300 cursor-pointer">
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-xs font-mono font-bold text-emerald-400 bg-emerald-500/10 px-2 py-1 rounded">{db.code}</span>
                    <span className={cn("inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium border", engCfg.bg, engCfg.color)}>
                      <Database className="w-3 h-3" /> {engCfg.label}
                    </span>
                  </div>
                  <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", critCfg.bg, critCfg.color)}>
                    {critCfg.label}
                  </span>
                </div>
                <h3 className="text-base font-semibold text-[var(--color-text)] mb-1 group-hover:text-emerald-400 transition-colors line-clamp-1">{db.name}</h3>
                {db.description && <p className="text-xs text-[var(--color-text-muted)] mb-3 line-clamp-2">{db.description}</p>}
                <div className="flex items-center gap-3 text-[10px] text-[var(--color-text-muted)] mb-3">
                  {db.version && <span className="font-mono">v{db.version}</span>}
                  {db.port && <span>Port: {db.port}</span>}
                  {db.sizeGB && <span>{db.sizeGB} GB</span>}
                </div>
                <div className="flex items-center justify-between mt-auto pt-3 border-t border-[var(--color-border)]">
                  <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", statusCfg.bg, statusCfg.color)}>
                    <StatusIcon className="w-3 h-3" /> {statusCfg.label}
                  </span>
                  {db.backupSchedule && <span className="text-[10px] text-[var(--color-text-muted)]">🔄 {db.backupSchedule}</span>}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {showCreate && (
        <>
          <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm" onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); }} />
          <div className="fixed right-0 top-0 z-50 h-full w-full max-w-lg bg-[var(--color-card-bg)] border-l border-[var(--color-border)] shadow-2xl flex flex-col animate-slide-in-right">
            <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-emerald-500 to-teal-600 shadow-lg shadow-emerald-500/30 flex items-center justify-center">
                  <HardDrive className="w-5 h-5 text-white" /></div>
                <div><h2 className="text-lg font-semibold text-[var(--color-text)]">Yeni Veritabanı</h2>
                  <p className="text-xs text-[var(--color-text-muted)]">Veritabanı bilgilerini girin</p></div>
              </div>
              <button onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); }} className="p-2 rounded-lg hover:bg-[var(--color-border)] transition-colors">
                <X className="w-5 h-5 text-[var(--color-text-muted)]" /></button>
            </div>
            <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
              <div className="grid grid-cols-3 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Kod <span className="text-red-400">*</span></label>
                  <input type="text" value={formData.code || ""} onChange={e => setFormData(d => ({ ...d, code: e.target.value.toUpperCase() }))}
                    placeholder="PGPROD" maxLength={20}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-emerald-500/40 font-mono" /></div>
                <div className="col-span-2"><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Veritabanı Adı <span className="text-red-400">*</span></label>
                  <input type="text" value={formData.name || ""} onChange={e => setFormData(d => ({ ...d, name: e.target.value }))}
                    placeholder="PostgreSQL Production"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-emerald-500/40" /></div>
              </div>
              <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Açıklama</label>
                <textarea rows={2} value={formData.description || ""} onChange={e => setFormData(d => ({ ...d, description: e.target.value }))}
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-emerald-500/40 resize-none" /></div>
              <div className="grid grid-cols-2 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Motor</label>
                  <select value={formData.databaseEngine || "PostgreSQL"} onChange={e => setFormData(d => ({ ...d, databaseEngine: e.target.value }))}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    {Object.entries(ENGINE_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}</select></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Kritiklik</label>
                  <select value={formData.criticality || "Medium"} onChange={e => setFormData(d => ({ ...d, criticality: e.target.value }))}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    {Object.entries(CRITICALITY_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}</select></div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Sürüm</label>
                  <input type="text" value={formData.version || ""} onChange={e => setFormData(d => ({ ...d, version: e.target.value }))}
                    placeholder="16.2"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-emerald-500/40 font-mono" /></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Port</label>
                  <input type="number" value={formData.port || ""} onChange={e => setFormData(d => ({ ...d, port: e.target.value }))}
                    placeholder="5432"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-emerald-500/40" /></div>
              </div>
              <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Yedekleme Planı</label>
                <input type="text" value={formData.backupSchedule || ""} onChange={e => setFormData(d => ({ ...d, backupSchedule: e.target.value }))}
                  placeholder="Her gün 02:00"
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-emerald-500/40" /></div>
              {formError && <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-sm">{formError}</div>}
            </div>
            <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-3">
              <button onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); }}
                className="px-4 py-2.5 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50 transition-colors">İptal</button>
              <button onClick={handleCreate} disabled={formSaving}
                className="px-6 py-2.5 rounded-lg text-sm font-medium bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50 transition-colors flex items-center gap-2">
                {formSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />} Oluştur
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
