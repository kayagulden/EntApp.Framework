"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import {
  AppWindow, Plus, Search, Loader2, X, Monitor, ShoppingCart,
  Building2, Tag, Shield, Code2, Globe, ExternalLink,
  AlertTriangle, CheckCircle2, RotateCcw, Archive, Clock,
  Zap,
} from "lucide-react";
import { cn } from "@/lib/utils";

// ── Types ───────────────────────────────────────────────────

interface ApplicationData {
  id: string;
  name: string;
  code: string;
  description?: string;
  applicationType: string;
  status: string;
  criticality: string;
  ownerUserId?: string;
  techLeadUserId?: string;
  technologyStack?: string;
  currentVersion?: string;
  createdAt: string;
}

// ── Config ──────────────────────────────────────────────────

const TYPE_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  InHouse: { icon: Code2, color: "text-indigo-400", bg: "bg-indigo-500/10 border-indigo-500/20", label: "İç Geliştirme" },
  COTS: { icon: ShoppingCart, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Paket Yazılım" },
  Infrastructure: { icon: Monitor, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Altyapı" },
  Hybrid: { icon: Zap, color: "text-violet-400", bg: "bg-violet-500/10 border-violet-500/20", label: "Hibrit" },
};

const STATUS_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  Planned: { icon: Clock, color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Planlandı" },
  InDevelopment: { icon: RotateCcw, color: "text-cyan-400", bg: "bg-cyan-500/10 border-cyan-500/20", label: "Geliştiriliyor" },
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

// ── Component ───────────────────────────────────────────────

export default function ApplicationsPage() {
  const router = useRouter();
  const [applications, setApplications] = useState<ApplicationData[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [typeFilter, setTypeFilter] = useState("");

  // Create modal
  const [showCreate, setShowCreate] = useState(false);
  const [formData, setFormData] = useState<Record<string, string>>({});
  const [formSaving, setFormSaving] = useState(false);
  const [formError, setFormError] = useState("");

  const fetchApplications = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (statusFilter) params.set("status", statusFilter);
      if (typeFilter) params.set("applicationType", typeFilter);
      const res = await fetch(`/api/pm/applications?${params}`);
      if (res.ok) setApplications(await res.json());
    } catch { /* */ }
    finally { setLoading(false); }
  }, [statusFilter, typeFilter]);

  useEffect(() => { fetchApplications(); }, [fetchApplications]);

  const handleCreate = async () => {
    if (!formData.name?.trim() || !formData.code?.trim()) {
      setFormError("Ad ve kod zorunludur."); return;
    }
    setFormSaving(true); setFormError("");
    try {
      const res = await fetch("/api/pm/applications", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: formData.name?.trim(),
          code: formData.code?.trim(),
          description: formData.description?.trim() || null,
          applicationType: formData.applicationType || "InHouse",
          criticality: formData.criticality || "Medium",
          technologyStack: formData.technologyStack?.trim() || null,
          repositoryUrl: formData.repositoryUrl?.trim() || null,
          documentationUrl: formData.documentationUrl?.trim() || null,
          currentVersion: formData.currentVersion?.trim() || null,
        })
      });
      if (res.ok) {
        setShowCreate(false); setFormData({}); setFormError("");
        fetchApplications();
      } else { setFormError(`Hata: ${res.status}`); }
    } catch { setFormError("Bağlantı hatası."); }
    finally { setFormSaving(false); }
  };

  const filtered = searchTerm
    ? applications.filter(a => a.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        a.code.toLowerCase().includes(searchTerm.toLowerCase()))
    : applications;

  return (
    <div className="space-y-5">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-cyan-500 to-blue-600 flex items-center justify-center shadow-lg shadow-cyan-500/25">
            <AppWindow className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">Uygulamalar</h1>
            <p className="text-sm text-[var(--color-text-muted)] mt-0.5">
              {applications.length} uygulama kayıtlı
            </p>
          </div>
        </div>
        <button onClick={() => { setShowCreate(true); setFormData({}); }}
          className="flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium bg-cyan-600 text-white hover:bg-cyan-700 shadow-lg shadow-cyan-500/20 transition-all">
          <Plus className="w-4 h-4" /> Yeni Uygulama
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
          <input type="text" placeholder="Uygulama ara..."
            value={searchTerm} onChange={e => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40" />
        </div>
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
          <option value="">Tüm Durumlar</option>
          {Object.entries(STATUS_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
        </select>
        <select value={typeFilter} onChange={e => setTypeFilter(e.target.value)}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
          <option value="">Tüm Tipler</option>
          {Object.entries(TYPE_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
        </select>
      </div>

      {/* Grid */}
      {loading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="w-6 h-6 text-cyan-400 animate-spin" />
        </div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-[var(--color-text-muted)]">
          <AppWindow className="w-12 h-12 opacity-30 mb-3" />
          <p className="text-sm">Henüz uygulama kaydedilmemiş</p>
          <button onClick={() => { setShowCreate(true); setFormData({}); }}
            className="mt-3 text-xs text-cyan-400 hover:text-cyan-300 transition-colors">+ Yeni Uygulama Ekle</button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map(app => {
            const typeCfg = TYPE_CONFIG[app.applicationType] || TYPE_CONFIG.InHouse;
            const statusCfg = STATUS_CONFIG[app.status] || STATUS_CONFIG.Planned;
            const critCfg = CRITICALITY_CONFIG[app.criticality] || CRITICALITY_CONFIG.Medium;
            const TypeIcon = typeCfg.icon;
            const StatusIcon = statusCfg.icon;
            return (
              <div key={app.id} onClick={() => router.push(`/manage/cmdb/applications/${app.id}`)}
                className="group rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5 hover:border-cyan-500/40 hover:shadow-lg hover:shadow-cyan-500/5 transition-all duration-300 cursor-pointer">
                {/* Top */}
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-xs font-mono font-bold text-cyan-400 bg-cyan-500/10 px-2 py-1 rounded">{app.code}</span>
                    <span className={cn("inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium border", typeCfg.bg, typeCfg.color)}>
                      <TypeIcon className="w-3 h-3" /> {typeCfg.label}
                    </span>
                  </div>
                  <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", critCfg.bg, critCfg.color)}>
                    {critCfg.label}
                  </span>
                </div>
                {/* Title */}
                <h3 className="text-base font-semibold text-[var(--color-text)] mb-1 group-hover:text-cyan-400 transition-colors line-clamp-1">{app.name}</h3>
                {app.description && (
                  <p className="text-xs text-[var(--color-text-muted)] mb-3 line-clamp-2">{app.description}</p>
                )}
                {/* Tech stack badge */}
                {app.technologyStack && (
                  <p className="text-[10px] text-[var(--color-text-muted)] bg-[var(--color-border)]/50 px-2 py-1 rounded mb-3 line-clamp-1 font-mono">
                    {app.technologyStack}
                  </p>
                )}
                {/* Meta */}
                <div className="flex items-center justify-between mt-auto pt-3 border-t border-[var(--color-border)]">
                  <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", statusCfg.bg, statusCfg.color)}>
                    <StatusIcon className="w-3 h-3" /> {statusCfg.label}
                  </span>
                  {app.currentVersion && (
                    <span className="text-[10px] font-mono text-[var(--color-text-muted)] bg-[var(--color-border)]/50 px-2 py-0.5 rounded">
                      v{app.currentVersion}
                    </span>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* ═══════════ Create Modal ═══════════ */}
      {showCreate && (
        <>
          <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm" onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); }} />
          <div className="fixed right-0 top-0 z-50 h-full w-full max-w-lg bg-[var(--color-card-bg)] border-l border-[var(--color-border)] shadow-2xl flex flex-col animate-slide-in-right">
            {/* Header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-cyan-500 to-blue-600 shadow-lg shadow-cyan-500/30 flex items-center justify-center">
                  <AppWindow className="w-5 h-5 text-white" />
                </div>
                <div>
                  <h2 className="text-lg font-semibold text-[var(--color-text)]">Yeni Uygulama</h2>
                  <p className="text-xs text-[var(--color-text-muted)]">Uygulama bilgilerini girin</p>
                </div>
              </div>
              <button onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); }} className="p-2 rounded-lg hover:bg-[var(--color-border)] transition-colors">
                <X className="w-5 h-5 text-[var(--color-text-muted)]" />
              </button>
            </div>

            {/* Body */}
            <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Kod <span className="text-red-400">*</span></label>
                  <input type="text" value={formData.code || ""} onChange={e => setFormData(d => ({ ...d, code: e.target.value.toUpperCase() }))}
                    placeholder="PORTAL" maxLength={20}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40 font-mono" />
                </div>
                <div className="col-span-2">
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Uygulama Adı <span className="text-red-400">*</span></label>
                  <input type="text" value={formData.name || ""} onChange={e => setFormData(d => ({ ...d, name: e.target.value }))}
                    placeholder="Müşteri Portalı"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40" />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Açıklama</label>
                <textarea rows={3} value={formData.description || ""} onChange={e => setFormData(d => ({ ...d, description: e.target.value }))}
                  placeholder="Uygulamanın amacı ve kapsamı..."
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40 resize-none" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Uygulama Tipi</label>
                  <select value={formData.applicationType || "InHouse"} onChange={e => setFormData(d => ({ ...d, applicationType: e.target.value }))}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40">
                    {Object.entries(TYPE_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Kritiklik</label>
                  <select value={formData.criticality || "Medium"} onChange={e => setFormData(d => ({ ...d, criticality: e.target.value }))}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40">
                    {Object.entries(CRITICALITY_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
                  </select>
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Teknoloji Yığını</label>
                <input type="text" value={formData.technologyStack || ""} onChange={e => setFormData(d => ({ ...d, technologyStack: e.target.value }))}
                  placeholder=".NET 9, React, PostgreSQL"
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Repository URL</label>
                  <input type="url" value={formData.repositoryUrl || ""} onChange={e => setFormData(d => ({ ...d, repositoryUrl: e.target.value }))}
                    placeholder="https://github.com/..."
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Mevcut Sürüm</label>
                  <input type="text" value={formData.currentVersion || ""} onChange={e => setFormData(d => ({ ...d, currentVersion: e.target.value }))}
                    placeholder="1.0.0"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40 font-mono" />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Dokümantasyon URL</label>
                <input type="url" value={formData.documentationUrl || ""} onChange={e => setFormData(d => ({ ...d, documentationUrl: e.target.value }))}
                  placeholder="https://wiki.example.com/..."
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40" />
              </div>
              {formError && (
                <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-sm">{formError}</div>
              )}
            </div>

            {/* Footer */}
            <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-3">
              <button onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); }}
                className="px-4 py-2.5 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50 transition-colors">İptal</button>
              <button onClick={handleCreate} disabled={formSaving}
                className="px-6 py-2.5 rounded-lg text-sm font-medium bg-cyan-600 text-white hover:bg-cyan-700 disabled:opacity-50 transition-colors flex items-center gap-2">
                {formSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
                Oluştur
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
