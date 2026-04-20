"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import {
  Server, Plus, Search, Loader2, X, Monitor, Cloud, Box,
  AlertTriangle, CheckCircle2, RotateCcw, Archive, Clock,
  Cpu, MemoryStick, HardDrive, MapPin, Network,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface ServerData {
  id: string; name: string; code: string; description?: string;
  serverType: string; environment: string; status: string; criticality: string;
  operatingSystem?: string; ipAddress?: string; hostname?: string;
  cpuCores?: number; ramGB?: number; diskGB?: number; dataCenter?: string;
  ownerUserId?: string; adminUserId?: string; createdAt: string;
}

const SERVER_TYPE_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  Physical: { icon: Box, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Fiziksel" },
  Virtual: { icon: Monitor, color: "text-indigo-400", bg: "bg-indigo-500/10 border-indigo-500/20", label: "Sanal" },
  Cloud: { icon: Cloud, color: "text-cyan-400", bg: "bg-cyan-500/10 border-cyan-500/20", label: "Bulut" },
  Container: { icon: Box, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Konteyner" },
};

const ENV_CONFIG: Record<string, { color: string; bg: string; label: string }> = {
  Production: { color: "text-red-400", bg: "bg-red-500/10 border-red-500/20", label: "Prod" },
  Staging: { color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Staging" },
  Development: { color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Dev" },
  Test: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Test" },
  DR: { color: "text-violet-400", bg: "bg-violet-500/10 border-violet-500/20", label: "DR" },
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

export default function ServersPage() {
  const router = useRouter();
  const [servers, setServers] = useState<ServerData[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [typeFilter, setTypeFilter] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [formData, setFormData] = useState<Record<string, string>>({});
  const [formSaving, setFormSaving] = useState(false);
  const [formError, setFormError] = useState("");

  const fetchServers = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (statusFilter) params.set("status", statusFilter);
      if (typeFilter) params.set("serverType", typeFilter);
      const res = await fetch(`/api/pm/servers?${params}`);
      if (res.ok) setServers(await res.json());
    } catch { /* */ }
    finally { setLoading(false); }
  }, [statusFilter, typeFilter]);

  useEffect(() => { fetchServers(); }, [fetchServers]);

  const handleCreate = async () => {
    if (!formData.name?.trim() || !formData.code?.trim()) {
      setFormError("Ad ve kod zorunludur."); return;
    }
    setFormSaving(true); setFormError("");
    try {
      const res = await fetch("/api/pm/servers", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: formData.name?.trim(), code: formData.code?.trim(),
          description: formData.description?.trim() || null,
          serverType: formData.serverType || "Virtual",
          environment: formData.environment || "Production",
          criticality: formData.criticality || "Medium",
          operatingSystem: formData.operatingSystem?.trim() || null,
          ipAddress: formData.ipAddress?.trim() || null,
          hostname: formData.hostname?.trim() || null,
          cpuCores: formData.cpuCores ? parseInt(formData.cpuCores) : null,
          ramGB: formData.ramGB ? parseInt(formData.ramGB) : null,
          diskGB: formData.diskGB ? parseInt(formData.diskGB) : null,
          dataCenter: formData.dataCenter?.trim() || null,
        })
      });
      if (res.ok) { setShowCreate(false); setFormData({}); fetchServers(); }
      else { setFormError(`Hata: ${res.status}`); }
    } catch { setFormError("Bağlantı hatası."); }
    finally { setFormSaving(false); }
  };

  const filtered = searchTerm
    ? servers.filter(s => s.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        s.code.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (s.hostname?.toLowerCase().includes(searchTerm.toLowerCase())) ||
        (s.ipAddress?.includes(searchTerm)))
    : servers;

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-violet-500 to-purple-600 flex items-center justify-center shadow-lg shadow-violet-500/25">
            <Server className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">Sunucular</h1>
            <p className="text-sm text-[var(--color-text-muted)] mt-0.5">{servers.length} sunucu kayıtlı</p>
          </div>
        </div>
        <button onClick={() => { setShowCreate(true); setFormData({}); }}
          className="flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium bg-violet-600 text-white hover:bg-violet-700 shadow-lg shadow-violet-500/20 transition-all">
          <Plus className="w-4 h-4" /> Yeni Sunucu
        </button>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
          <input type="text" placeholder="Sunucu, IP, hostname ara..."
            value={searchTerm} onChange={e => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40" />
        </div>
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
          <option value="">Tüm Durumlar</option>
          {Object.entries(STATUS_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
        </select>
        <select value={typeFilter} onChange={e => setTypeFilter(e.target.value)}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
          <option value="">Tüm Tipler</option>
          {Object.entries(SERVER_TYPE_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
        </select>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12"><Loader2 className="w-6 h-6 text-violet-400 animate-spin" /></div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-[var(--color-text-muted)]">
          <Server className="w-12 h-12 opacity-30 mb-3" />
          <p className="text-sm">Henüz sunucu kaydedilmemiş</p>
          <button onClick={() => { setShowCreate(true); setFormData({}); }}
            className="mt-3 text-xs text-violet-400 hover:text-violet-300 transition-colors">+ Yeni Sunucu Ekle</button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map(srv => {
            const typeCfg = SERVER_TYPE_CONFIG[srv.serverType] || SERVER_TYPE_CONFIG.Virtual;
            const statusCfg = STATUS_CONFIG[srv.status] || STATUS_CONFIG.Planned;
            const critCfg = CRITICALITY_CONFIG[srv.criticality] || CRITICALITY_CONFIG.Medium;
            const envCfg = ENV_CONFIG[srv.environment] || ENV_CONFIG.Production;
            const TypeIcon = typeCfg.icon;
            const StatusIcon = statusCfg.icon;
            return (
              <div key={srv.id} onClick={() => router.push(`/dashboard/servers/${srv.id}`)}
                className="group rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5 hover:border-violet-500/40 hover:shadow-lg hover:shadow-violet-500/5 transition-all duration-300 cursor-pointer">
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-xs font-mono font-bold text-violet-400 bg-violet-500/10 px-2 py-1 rounded">{srv.code}</span>
                    <span className={cn("inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium border", typeCfg.bg, typeCfg.color)}>
                      <TypeIcon className="w-3 h-3" /> {typeCfg.label}
                    </span>
                  </div>
                  <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", envCfg.bg, envCfg.color)}>
                    {envCfg.label}
                  </span>
                </div>
                <h3 className="text-base font-semibold text-[var(--color-text)] mb-1 group-hover:text-violet-400 transition-colors line-clamp-1">{srv.name}</h3>
                {srv.description && <p className="text-xs text-[var(--color-text-muted)] mb-3 line-clamp-2">{srv.description}</p>}
                {/* Specs */}
                <div className="flex items-center gap-3 text-[10px] text-[var(--color-text-muted)] mb-3">
                  {srv.cpuCores && <span className="flex items-center gap-1"><Cpu className="w-3 h-3" /> {srv.cpuCores} vCPU</span>}
                  {srv.ramGB && <span className="flex items-center gap-1"><MemoryStick className="w-3 h-3" /> {srv.ramGB} GB</span>}
                  {srv.diskGB && <span className="flex items-center gap-1"><HardDrive className="w-3 h-3" /> {srv.diskGB} GB</span>}
                </div>
                {(srv.ipAddress || srv.hostname) && (
                  <p className="text-[10px] text-[var(--color-text-muted)] bg-[var(--color-border)]/50 px-2 py-1 rounded mb-3 line-clamp-1 font-mono">
                    {srv.hostname && <><Network className="w-3 h-3 inline mr-1" />{srv.hostname}</>}
                    {srv.hostname && srv.ipAddress && " • "}
                    {srv.ipAddress}
                  </p>
                )}
                <div className="flex items-center justify-between mt-auto pt-3 border-t border-[var(--color-border)]">
                  <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", statusCfg.bg, statusCfg.color)}>
                    <StatusIcon className="w-3 h-3" /> {statusCfg.label}
                  </span>
                  <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", critCfg.bg, critCfg.color)}>
                    {critCfg.label}
                  </span>
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
                <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-violet-500 to-purple-600 shadow-lg shadow-violet-500/30 flex items-center justify-center">
                  <Server className="w-5 h-5 text-white" />
                </div>
                <div><h2 className="text-lg font-semibold text-[var(--color-text)]">Yeni Sunucu</h2>
                  <p className="text-xs text-[var(--color-text-muted)]">Sunucu bilgilerini girin</p></div>
              </div>
              <button onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); }} className="p-2 rounded-lg hover:bg-[var(--color-border)] transition-colors">
                <X className="w-5 h-5 text-[var(--color-text-muted)]" /></button>
            </div>
            <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
              <div className="grid grid-cols-3 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Kod <span className="text-red-400">*</span></label>
                  <input type="text" value={formData.code || ""} onChange={e => setFormData(d => ({ ...d, code: e.target.value.toUpperCase() }))}
                    placeholder="APPSRV01" maxLength={20}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40 font-mono" /></div>
                <div className="col-span-2"><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Sunucu Adı <span className="text-red-400">*</span></label>
                  <input type="text" value={formData.name || ""} onChange={e => setFormData(d => ({ ...d, name: e.target.value }))}
                    placeholder="App Server 01"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40" /></div>
              </div>
              <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Açıklama</label>
                <textarea rows={2} value={formData.description || ""} onChange={e => setFormData(d => ({ ...d, description: e.target.value }))}
                  placeholder="Sunucu açıklaması..."
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40 resize-none" /></div>
              <div className="grid grid-cols-3 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Tip</label>
                  <select value={formData.serverType || "Virtual"} onChange={e => setFormData(d => ({ ...d, serverType: e.target.value }))}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    {Object.entries(SERVER_TYPE_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}</select></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Ortam</label>
                  <select value={formData.environment || "Production"} onChange={e => setFormData(d => ({ ...d, environment: e.target.value }))}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    {Object.entries(ENV_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}</select></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Kritiklik</label>
                  <select value={formData.criticality || "Medium"} onChange={e => setFormData(d => ({ ...d, criticality: e.target.value }))}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    {Object.entries(CRITICALITY_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}</select></div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Hostname</label>
                  <input type="text" value={formData.hostname || ""} onChange={e => setFormData(d => ({ ...d, hostname: e.target.value }))}
                    placeholder="appsrv01.local"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40 font-mono" /></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">IP Adresi</label>
                  <input type="text" value={formData.ipAddress || ""} onChange={e => setFormData(d => ({ ...d, ipAddress: e.target.value }))}
                    placeholder="10.0.1.10"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40 font-mono" /></div>
              </div>
              <div className="grid grid-cols-3 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">CPU (vCPU)</label>
                  <input type="number" value={formData.cpuCores || ""} onChange={e => setFormData(d => ({ ...d, cpuCores: e.target.value }))}
                    placeholder="8"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40" /></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">RAM (GB)</label>
                  <input type="number" value={formData.ramGB || ""} onChange={e => setFormData(d => ({ ...d, ramGB: e.target.value }))}
                    placeholder="32"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40" /></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Disk (GB)</label>
                  <input type="number" value={formData.diskGB || ""} onChange={e => setFormData(d => ({ ...d, diskGB: e.target.value }))}
                    placeholder="500"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40" /></div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">İşletim Sistemi</label>
                  <input type="text" value={formData.operatingSystem || ""} onChange={e => setFormData(d => ({ ...d, operatingSystem: e.target.value }))}
                    placeholder="Ubuntu 22.04 LTS"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40" /></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Veri Merkezi</label>
                  <input type="text" value={formData.dataCenter || ""} onChange={e => setFormData(d => ({ ...d, dataCenter: e.target.value }))}
                    placeholder="Ankara DC1"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-violet-500/40" /></div>
              </div>
              {formError && <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-sm">{formError}</div>}
            </div>
            <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-3">
              <button onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); }}
                className="px-4 py-2.5 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50 transition-colors">İptal</button>
              <button onClick={handleCreate} disabled={formSaving}
                className="px-6 py-2.5 rounded-lg text-sm font-medium bg-violet-600 text-white hover:bg-violet-700 disabled:opacity-50 transition-colors flex items-center gap-2">
                {formSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />} Oluştur
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
