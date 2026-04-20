"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import {
  KeyRound, Plus, Search, Loader2, X, Users, DollarSign,
  AlertTriangle, CheckCircle2, Archive, Clock, Calendar,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface LicenceData {
  id: string; name: string; code: string; description?: string;
  licenceType: string; status: string; criticality: string;
  vendor?: string; productName?: string;
  maxUsers?: number; currentUsers?: number;
  expirationDate?: string; annualCost?: number; currency?: string;
  ownerUserId?: string; createdAt: string;
}

const LICENCE_TYPE_CONFIG: Record<string, { color: string; bg: string; label: string }> = {
  Perpetual: { color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Süresiz" },
  Subscription: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Abonelik" },
  OpenSource: { color: "text-violet-400", bg: "bg-violet-500/10 border-violet-500/20", label: "Açık Kaynak" },
  Trial: { color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Deneme" },
  Enterprise: { color: "text-rose-400", bg: "bg-rose-500/10 border-rose-500/20", label: "Kurumsal" },
};

const STATUS_CONFIG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  Planned: { icon: Clock, color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Planlandı" },
  Active: { icon: CheckCircle2, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Aktif" },
  Deprecated: { icon: AlertTriangle, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Süresi Doluyor" },
  Retired: { icon: Archive, color: "text-gray-400", bg: "bg-gray-500/10 border-gray-500/20", label: "Sonlandırıldı" },
};

const CRITICALITY_CONFIG: Record<string, { color: string; bg: string; label: string }> = {
  Low: { color: "text-slate-400", bg: "bg-slate-500/10 border-slate-500/20", label: "Düşük" },
  Medium: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Orta" },
  High: { color: "text-orange-400", bg: "bg-orange-500/10 border-orange-500/20", label: "Yüksek" },
  Critical: { color: "text-red-400", bg: "bg-red-500/10 border-red-500/20", label: "Kritik" },
};

export default function LicencesPage() {
  const router = useRouter();
  const [licences, setLicences] = useState<LicenceData[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [typeFilter, setTypeFilter] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [formData, setFormData] = useState<Record<string, string>>({});
  const [formSaving, setFormSaving] = useState(false);
  const [formError, setFormError] = useState("");

  const fetchLicences = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (statusFilter) params.set("status", statusFilter);
      if (typeFilter) params.set("licenceType", typeFilter);
      const res = await fetch(`/api/pm/licences?${params}`);
      if (res.ok) setLicences(await res.json());
    } catch { /* */ }
    finally { setLoading(false); }
  }, [statusFilter, typeFilter]);

  useEffect(() => { fetchLicences(); }, [fetchLicences]);

  const handleCreate = async () => {
    if (!formData.name?.trim() || !formData.code?.trim()) { setFormError("Ad ve kod zorunludur."); return; }
    setFormSaving(true); setFormError("");
    try {
      const res = await fetch("/api/pm/licences", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: formData.name?.trim(), code: formData.code?.trim(),
          description: formData.description?.trim() || null,
          licenceType: formData.licenceType || "Subscription",
          criticality: formData.criticality || "Medium",
          vendor: formData.vendor?.trim() || null,
          productName: formData.productName?.trim() || null,
          maxUsers: formData.maxUsers ? parseInt(formData.maxUsers) : null,
          annualCost: formData.annualCost ? parseFloat(formData.annualCost) : null,
          currency: formData.currency?.trim() || "TRY",
        })
      });
      if (res.ok) { setShowCreate(false); setFormData({}); fetchLicences(); }
      else { setFormError(`Hata: ${res.status}`); }
    } catch { setFormError("Bağlantı hatası."); }
    finally { setFormSaving(false); }
  };

  const filtered = searchTerm
    ? licences.filter(l => l.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        l.code.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (l.vendor?.toLowerCase().includes(searchTerm.toLowerCase())))
    : licences;

  const formatCurrency = (amount?: number, currency?: string) => {
    if (!amount) return null;
    return new Intl.NumberFormat("tr-TR", { style: "currency", currency: currency || "TRY" }).format(amount);
  };

  const isExpiringSoon = (date?: string) => {
    if (!date) return false;
    const diff = new Date(date).getTime() - Date.now();
    return diff > 0 && diff < 30 * 24 * 60 * 60 * 1000;
  };

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-amber-500 to-orange-600 flex items-center justify-center shadow-lg shadow-amber-500/25">
            <KeyRound className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">Lisanslar</h1>
            <p className="text-sm text-[var(--color-text-muted)] mt-0.5">{licences.length} lisans kayıtlı</p>
          </div>
        </div>
        <button onClick={() => { setShowCreate(true); setFormData({}); }}
          className="flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium bg-amber-600 text-white hover:bg-amber-700 shadow-lg shadow-amber-500/20 transition-all">
          <Plus className="w-4 h-4" /> Yeni Lisans
        </button>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
          <input type="text" placeholder="Lisans, üretici ara..." value={searchTerm} onChange={e => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-amber-500/40" />
        </div>
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
          <option value="">Tüm Durumlar</option>
          {Object.entries(STATUS_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
        </select>
        <select value={typeFilter} onChange={e => setTypeFilter(e.target.value)}
          className="px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
          <option value="">Tüm Tipler</option>
          {Object.entries(LICENCE_TYPE_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
        </select>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12"><Loader2 className="w-6 h-6 text-amber-400 animate-spin" /></div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-[var(--color-text-muted)]">
          <KeyRound className="w-12 h-12 opacity-30 mb-3" />
          <p className="text-sm">Henüz lisans kaydedilmemiş</p>
          <button onClick={() => { setShowCreate(true); setFormData({}); }}
            className="mt-3 text-xs text-amber-400 hover:text-amber-300 transition-colors">+ Yeni Lisans Ekle</button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map(lic => {
            const typeCfg = LICENCE_TYPE_CONFIG[lic.licenceType] || LICENCE_TYPE_CONFIG.Subscription;
            const statusCfg = STATUS_CONFIG[lic.status] || STATUS_CONFIG.Planned;
            const critCfg = CRITICALITY_CONFIG[lic.criticality] || CRITICALITY_CONFIG.Medium;
            const StatusIcon = statusCfg.icon;
            const expiring = isExpiringSoon(lic.expirationDate);
            return (
              <div key={lic.id} onClick={() => router.push(`/manage/cmdb/licences/${lic.id}`)}
                className={cn("group rounded-xl border bg-[var(--color-card-bg)] p-5 hover:shadow-lg transition-all duration-300 cursor-pointer",
                  expiring ? "border-amber-500/40 hover:shadow-amber-500/10" : "border-[var(--color-border)] hover:border-amber-500/40 hover:shadow-amber-500/5")}>
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-xs font-mono font-bold text-amber-400 bg-amber-500/10 px-2 py-1 rounded">{lic.code}</span>
                    <span className={cn("inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium border", typeCfg.bg, typeCfg.color)}>
                      {typeCfg.label}
                    </span>
                  </div>
                  <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", critCfg.bg, critCfg.color)}>
                    {critCfg.label}
                  </span>
                </div>
                <h3 className="text-base font-semibold text-[var(--color-text)] mb-1 group-hover:text-amber-400 transition-colors line-clamp-1">{lic.name}</h3>
                {lic.vendor && <p className="text-xs text-[var(--color-text-muted)] mb-2">{lic.vendor} {lic.productName && `• ${lic.productName}`}</p>}
                <div className="flex items-center gap-3 text-[10px] text-[var(--color-text-muted)] mb-3">
                  {lic.maxUsers && <span className="flex items-center gap-1"><Users className="w-3 h-3" /> {lic.currentUsers || 0}/{lic.maxUsers}</span>}
                  {lic.annualCost && <span className="flex items-center gap-1"><DollarSign className="w-3 h-3" /> {formatCurrency(lic.annualCost, lic.currency)}/yıl</span>}
                </div>
                <div className="flex items-center justify-between mt-auto pt-3 border-t border-[var(--color-border)]">
                  <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", statusCfg.bg, statusCfg.color)}>
                    <StatusIcon className="w-3 h-3" /> {statusCfg.label}
                  </span>
                  {lic.expirationDate && (
                    <span className={cn("inline-flex items-center gap-1 text-[10px]", expiring ? "text-amber-400" : "text-[var(--color-text-muted)]")}>
                      <Calendar className="w-3 h-3" /> {new Date(lic.expirationDate).toLocaleDateString("tr-TR")}
                    </span>
                  )}
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
                <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-amber-500 to-orange-600 shadow-lg shadow-amber-500/30 flex items-center justify-center">
                  <KeyRound className="w-5 h-5 text-white" /></div>
                <div><h2 className="text-lg font-semibold text-[var(--color-text)]">Yeni Lisans</h2>
                  <p className="text-xs text-[var(--color-text-muted)]">Lisans bilgilerini girin</p></div>
              </div>
              <button onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); }} className="p-2 rounded-lg hover:bg-[var(--color-border)] transition-colors">
                <X className="w-5 h-5 text-[var(--color-text-muted)]" /></button>
            </div>
            <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
              <div className="grid grid-cols-3 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Kod <span className="text-red-400">*</span></label>
                  <input type="text" value={formData.code || ""} onChange={e => setFormData(d => ({ ...d, code: e.target.value.toUpperCase() }))}
                    placeholder="ORALIC" maxLength={20}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-amber-500/40 font-mono" /></div>
                <div className="col-span-2"><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Lisans Adı <span className="text-red-400">*</span></label>
                  <input type="text" value={formData.name || ""} onChange={e => setFormData(d => ({ ...d, name: e.target.value }))}
                    placeholder="Oracle Database Enterprise"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-amber-500/40" /></div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Üretici</label>
                  <input type="text" value={formData.vendor || ""} onChange={e => setFormData(d => ({ ...d, vendor: e.target.value }))}
                    placeholder="Oracle"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-amber-500/40" /></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Ürün Adı</label>
                  <input type="text" value={formData.productName || ""} onChange={e => setFormData(d => ({ ...d, productName: e.target.value }))}
                    placeholder="Oracle DB EE"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-amber-500/40" /></div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Lisans Tipi</label>
                  <select value={formData.licenceType || "Subscription"} onChange={e => setFormData(d => ({ ...d, licenceType: e.target.value }))}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    {Object.entries(LICENCE_TYPE_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}</select></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Kritiklik</label>
                  <select value={formData.criticality || "Medium"} onChange={e => setFormData(d => ({ ...d, criticality: e.target.value }))}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    {Object.entries(CRITICALITY_CONFIG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}</select></div>
              </div>
              <div className="grid grid-cols-3 gap-4">
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Maks. Kullanıcı</label>
                  <input type="number" value={formData.maxUsers || ""} onChange={e => setFormData(d => ({ ...d, maxUsers: e.target.value }))}
                    placeholder="100"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-amber-500/40" /></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Yıllık Maliyet</label>
                  <input type="number" value={formData.annualCost || ""} onChange={e => setFormData(d => ({ ...d, annualCost: e.target.value }))}
                    placeholder="50000"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-amber-500/40" /></div>
                <div><label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">Para Birimi</label>
                  <select value={formData.currency || "TRY"} onChange={e => setFormData(d => ({ ...d, currency: e.target.value }))}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    <option value="TRY">TRY</option><option value="USD">USD</option><option value="EUR">EUR</option></select></div>
              </div>
              {formError && <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-sm">{formError}</div>}
            </div>
            <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-3">
              <button onClick={() => { setShowCreate(false); setFormData({}); setFormError(""); }}
                className="px-4 py-2.5 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50 transition-colors">İptal</button>
              <button onClick={handleCreate} disabled={formSaving}
                className="px-6 py-2.5 rounded-lg text-sm font-medium bg-amber-600 text-white hover:bg-amber-700 disabled:opacity-50 transition-colors flex items-center gap-2">
                {formSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />} Oluştur
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
