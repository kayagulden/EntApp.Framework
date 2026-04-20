"use client";

import { useState, useEffect, useCallback } from "react";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft, KeyRound, Calendar, Loader2, CheckCircle2, AlertTriangle, Archive,
  Clock, Edit3, Save, X, Shield, ChevronRight, Users, DollarSign, FolderKanban,
} from "lucide-react";
import { cn } from "@/lib/utils";
import CIRelationshipsPanel from "@/components/ci-relationships-panel";

interface LicenceDetail {
  id: string; name: string; code: string; description?: string;
  licenceType: string; status: string; criticality: string;
  vendor?: string; productName?: string; licenceKey?: string;
  maxUsers?: number; currentUsers?: number;
  expirationDate?: string; purchaseDate?: string; annualCost?: number; currency?: string;
  ownerUserId?: string; createdAt: string; updatedAt?: string; projects?: CIProjectItem[];
}
interface CIProjectItem { projectId: string; projectKey: string; projectName: string; projectStatus: string; role: string; }

const LICENCE_TYPE_CFG: Record<string, { color: string; bg: string; label: string }> = {
  Perpetual: { color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Süresiz" },
  Subscription: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Abonelik" },
  OpenSource: { color: "text-violet-400", bg: "bg-violet-500/10 border-violet-500/20", label: "Açık Kaynak" },
  Trial: { color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Deneme" },
  Enterprise: { color: "text-rose-400", bg: "bg-rose-500/10 border-rose-500/20", label: "Kurumsal" },
};
const STATUS_CFG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  Planned: { icon: Clock, color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Planlandı" },
  Active: { icon: CheckCircle2, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Aktif" },
  Deprecated: { icon: AlertTriangle, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Süresi Doluyor" },
  Retired: { icon: Archive, color: "text-gray-400", bg: "bg-gray-500/10 border-gray-500/20", label: "Sonlandırıldı" },
};
const CRIT_CFG: Record<string, { color: string; bg: string; label: string }> = {
  Low: { color: "text-slate-400", bg: "bg-slate-500/10 border-slate-500/20", label: "Düşük" },
  Medium: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Orta" },
  High: { color: "text-orange-400", bg: "bg-orange-500/10 border-orange-500/20", label: "Yüksek" },
  Critical: { color: "text-red-400", bg: "bg-red-500/10 border-red-500/20", label: "Kritik" },
};
const STATUS_FLOW: Record<string, string[]> = { Planned: ["Active"], Active: ["Deprecated"], Deprecated: ["Retired", "Active"], Retired: [] };
function fmtDate(d?: string) { return d ? new Date(d).toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" }) : "—"; }
function fmtCurrency(amount?: number, currency?: string) {
  if (!amount) return "—";
  return new Intl.NumberFormat("tr-TR", { style: "currency", currency: currency || "TRY" }).format(amount);
}

export default function LicenceDetailPage() {
  const params = useParams(); const router = useRouter();
  const licId = params?.id as string;
  const [lic, setLic] = useState<LicenceDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [editData, setEditData] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);

  const fetchLic = useCallback(async () => {
    if (!licId) return; setLoading(true);
    try { const r = await fetch(`/api/pm/licences/${licId}`); if (r.ok) setLic(await r.json()); } catch {}
    finally { setLoading(false); }
  }, [licId]);
  useEffect(() => { fetchLic(); }, [fetchLic]);

  const startEdit = () => {
    if (!lic) return;
    setEditData({ name: lic.name, description: lic.description || "", vendor: lic.vendor || "",
      productName: lic.productName || "", maxUsers: lic.maxUsers?.toString() || "",
      currentUsers: lic.currentUsers?.toString() || "", annualCost: lic.annualCost?.toString() || "",
      currency: lic.currency || "TRY", criticality: lic.criticality });
    setEditing(true);
  };

  const saveEdit = async () => {
    if (!lic) return; setSaving(true);
    try {
      const r = await fetch(`/api/pm/licences/${lic.id}`, { method: "PUT", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: editData.name || null, description: editData.description || null,
          vendor: editData.vendor || null, productName: editData.productName || null,
          maxUsers: editData.maxUsers ? parseInt(editData.maxUsers) : null,
          currentUsers: editData.currentUsers ? parseInt(editData.currentUsers) : null,
          annualCost: editData.annualCost ? parseFloat(editData.annualCost) : null,
          currency: editData.currency || null, criticality: editData.criticality || null }) });
      if (r.ok) { setEditing(false); fetchLic(); }
    } catch {} finally { setSaving(false); }
  };

  const changeStatus = async (s: string) => {
    if (!lic) return;
    try { await fetch(`/api/pm/licences/${lic.id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status: s }) }); fetchLic(); } catch {}
  };

  if (loading) return <div className="flex items-center justify-center py-20"><Loader2 className="w-6 h-6 text-amber-400 animate-spin" /></div>;
  if (!lic) return <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]"><KeyRound className="w-12 h-12 opacity-30 mb-3" /><p className="text-sm">Lisans bulunamadı</p><button onClick={() => router.back()} className="mt-3 text-xs text-amber-400">← Geri</button></div>;

  const typeCfg = LICENCE_TYPE_CFG[lic.licenceType] || LICENCE_TYPE_CFG.Subscription;
  const statusCfg = STATUS_CFG[lic.status] || STATUS_CFG.Planned;
  const critCfg = CRIT_CFG[lic.criticality] || CRIT_CFG.Medium;
  const StatusIcon = statusCfg.icon;
  const nextStatuses = STATUS_FLOW[lic.status] || [];
  const isExpiringSoon = lic.expirationDate && new Date(lic.expirationDate).getTime() - Date.now() < 30 * 24 * 60 * 60 * 1000 && new Date(lic.expirationDate).getTime() > Date.now();
  const isExpired = lic.expirationDate && new Date(lic.expirationDate).getTime() < Date.now();
  const usagePercent = (lic.maxUsers && lic.currentUsers) ? Math.round((lic.currentUsers / lic.maxUsers) * 100) : null;

  return (
    <div className="space-y-5">
      <div className="flex items-center gap-2 text-sm text-[var(--color-text-muted)]">
        <button onClick={() => router.push("/manage/cmdb/licences")} className="flex items-center gap-1 hover:text-[var(--color-text)] transition-colors"><ArrowLeft className="w-4 h-4" /> Lisanslar</button>
        <ChevronRight className="w-3 h-3" /><span className="text-[var(--color-text)] font-medium">{lic.code}</span>
      </div>

      <div className="flex items-start justify-between">
        <div className="flex items-start gap-4">
          <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-amber-500 to-orange-600 flex items-center justify-center shadow-lg shadow-amber-500/30 mt-0.5">
            <KeyRound className="w-6 h-6 text-white" />
          </div>
          <div>
            <div className="flex items-center gap-3 mb-1 flex-wrap">
              <span className="text-sm font-mono font-bold text-amber-400 bg-amber-500/10 px-2.5 py-1 rounded-md">{lic.code}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", statusCfg.bg, statusCfg.color)}><StatusIcon className="w-3.5 h-3.5" /> {statusCfg.label}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", typeCfg.bg, typeCfg.color)}>{typeCfg.label}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", critCfg.bg, critCfg.color)}><Shield className="w-3.5 h-3.5" /> {critCfg.label}</span>
            </div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">{lic.name}</h1>
            {lic.vendor && <p className="text-sm text-[var(--color-text-muted)] mt-1">{lic.vendor} {lic.productName && `• ${lic.productName}`}</p>}
          </div>
        </div>
        <div className="flex items-center gap-2">
          {nextStatuses.map(s => { const sc = STATUS_CFG[s] || STATUS_CFG.Planned; return (
            <button key={s} onClick={() => changeStatus(s)} className={cn("flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-medium border transition-all hover:opacity-80", sc.bg, sc.color)}>{sc.label}</button>
          ); })}
          {!editing && <button onClick={startEdit} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-border)]/50 transition-all"><Edit3 className="w-3.5 h-3.5" /> Düzenle</button>}
        </div>
      </div>

      {/* Expiry Warning */}
      {(isExpiringSoon || isExpired) && (
        <div className={cn("flex items-center gap-3 p-3 rounded-lg border",
          isExpired ? "bg-red-500/10 border-red-500/20 text-red-400" : "bg-amber-500/10 border-amber-500/20 text-amber-400")}>
          <AlertTriangle className="w-4 h-4 shrink-0" />
          <span className="text-sm font-medium">{isExpired ? "Bu lisansın süresi dolmuş!" : `Lisans süresi ${fmtDate(lic.expirationDate)} tarihinde doluyor.`}</span>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        <div className="lg:col-span-2 space-y-5">
          {/* Usage & Cost Cards */}
          <div className="grid grid-cols-2 gap-4">
            {usagePercent !== null && (
              <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
                <div className="flex items-center gap-2 mb-3"><Users className="w-4 h-4 text-blue-400" /><span className="text-xs font-semibold text-[var(--color-text)]">Kullanım</span></div>
                <div className="text-2xl font-bold text-[var(--color-text)]">{lic.currentUsers} <span className="text-sm font-normal text-[var(--color-text-muted)]">/ {lic.maxUsers}</span></div>
                <div className="mt-2 h-2 rounded-full bg-[var(--color-border)] overflow-hidden">
                  <div className={cn("h-full rounded-full transition-all", usagePercent > 90 ? "bg-red-500" : usagePercent > 70 ? "bg-amber-500" : "bg-emerald-500")}
                    style={{ width: `${Math.min(usagePercent, 100)}%` }} />
                </div>
                <div className="text-[10px] text-[var(--color-text-muted)] mt-1">{usagePercent}% kullanım</div>
              </div>
            )}
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
              <div className="flex items-center gap-2 mb-3"><DollarSign className="w-4 h-4 text-emerald-400" /><span className="text-xs font-semibold text-[var(--color-text)]">Maliyet</span></div>
              <div className="text-2xl font-bold text-[var(--color-text)]">{fmtCurrency(lic.annualCost, lic.currency)}</div>
              <div className="text-[10px] text-[var(--color-text-muted)] mt-1">Yıllık maliyet</div>
            </div>
          </div>

          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-6">
            {editing ? (
              <div className="space-y-4">
                <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3">Lisans Bilgileri</h3>
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Ad</label>
                  <input type="text" value={editData.name || ""} onChange={e => setEditData(d => ({ ...d, name: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-amber-500/40" /></div>
                <div className="grid grid-cols-2 gap-4">
                  <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Üretici</label>
                    <input type="text" value={editData.vendor || ""} onChange={e => setEditData(d => ({ ...d, vendor: e.target.value }))}
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
                  <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Ürün Adı</label>
                    <input type="text" value={editData.productName || ""} onChange={e => setEditData(d => ({ ...d, productName: e.target.value }))}
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
                </div>
                <div className="grid grid-cols-3 gap-4">
                  <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Maks. Kullanıcı</label>
                    <input type="number" value={editData.maxUsers || ""} onChange={e => setEditData(d => ({ ...d, maxUsers: e.target.value }))}
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
                  <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Mevcut Kullanıcı</label>
                    <input type="number" value={editData.currentUsers || ""} onChange={e => setEditData(d => ({ ...d, currentUsers: e.target.value }))}
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
                  <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Kritiklik</label>
                    <select value={editData.criticality || ""} onChange={e => setEditData(d => ({ ...d, criticality: e.target.value }))}
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                      {Object.entries(CRIT_CFG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}</select></div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Yıllık Maliyet</label>
                    <input type="number" value={editData.annualCost || ""} onChange={e => setEditData(d => ({ ...d, annualCost: e.target.value }))}
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
                  <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Para Birimi</label>
                    <select value={editData.currency || "TRY"} onChange={e => setEditData(d => ({ ...d, currency: e.target.value }))}
                      className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                      <option value="TRY">TRY</option><option value="USD">USD</option><option value="EUR">EUR</option></select></div>
                </div>
                <div className="flex justify-end gap-2 pt-2">
                  <button onClick={() => setEditing(false)} className="px-4 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50"><X className="w-4 h-4 inline-block mr-1" />İptal</button>
                  <button onClick={saveEdit} disabled={saving} className="px-4 py-2 rounded-lg text-sm font-medium bg-amber-600 text-white hover:bg-amber-700 disabled:opacity-50 flex items-center gap-2">
                    {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />} Kaydet</button>
                </div>
              </div>
            ) : (
              <div className="space-y-5">
                <h3 className="text-sm font-semibold text-[var(--color-text)] mb-4">Lisans Bilgileri</h3>
                <div className="grid grid-cols-2 gap-y-4 gap-x-8">
                  <InfoRow icon={KeyRound} label="Kod" value={lic.code} mono />
                  <InfoRow icon={Calendar} label="Oluşturma" value={fmtDate(lic.createdAt)} />
                  {lic.vendor && <InfoRow icon={KeyRound} label="Üretici" value={lic.vendor} />}
                  {lic.productName && <InfoRow icon={KeyRound} label="Ürün" value={lic.productName} />}
                  {lic.purchaseDate && <InfoRow icon={Calendar} label="Satın Alma" value={fmtDate(lic.purchaseDate)} />}
                  {lic.expirationDate && <InfoRow icon={Calendar} label="Bitiş Tarihi" value={fmtDate(lic.expirationDate)} />}
                </div>
                {lic.updatedAt && <p className="text-[10px] text-[var(--color-text-muted)] pt-3 border-t border-[var(--color-border)]">Son güncelleme: {fmtDate(lic.updatedAt)}</p>}
              </div>
            )}
          </div>
        </div>

        <div className="space-y-4">
          <CIRelationshipsPanel ciId={licId} ciName={lic.name} accentColor="amber" />
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3 flex items-center gap-2"><FolderKanban className="w-4 h-4 text-amber-400" /> İlgili Projeler</h3>
            {(lic.projects && lic.projects.length > 0) ? (
              <ul className="space-y-2">{lic.projects.map(p => (
                <li key={p.projectId}><button onClick={() => router.push(`/dashboard/projects/${p.projectId}`)}
                  className="w-full text-left p-2.5 rounded-lg bg-[var(--color-bg)] hover:bg-[var(--color-border)] transition-colors">
                  <span className="text-xs font-mono font-bold text-amber-400">{p.projectKey}</span>
                  <p className="text-xs text-[var(--color-text)] mt-1 truncate">{p.projectName}</p></button></li>
              ))}</ul>
            ) : <p className="text-xs text-[var(--color-text-muted)]">İlgili proje bulunmuyor.</p>}
          </div>
        </div>
      </div>
    </div>
  );
}

function InfoRow({ icon: Icon, label, value, mono }: { icon: React.ComponentType<{ className?: string }>; label: string; value: string; mono?: boolean; }) {
  return (<div className="flex items-start gap-2.5"><Icon className="w-4 h-4 text-[var(--color-text-muted)] mt-0.5 shrink-0" /><div className="min-w-0"><div className="text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider">{label}</div><div className={cn("text-sm text-[var(--color-text)]", mono && "font-mono font-bold text-amber-400")}>{value}</div></div></div>);
}
