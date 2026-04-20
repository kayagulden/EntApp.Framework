"use client";

import { useState, useEffect, useCallback } from "react";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft, Server, Calendar, Loader2, CheckCircle2, AlertTriangle, Archive,
  Clock, Edit3, Save, X, Shield, ChevronRight, Cpu, MemoryStick, HardDrive,
  Network, MapPin, Monitor, Cloud, Box, FolderKanban,
} from "lucide-react";
import { cn } from "@/lib/utils";
import CIRelationshipsPanel from "@/components/ci-relationships-panel";

interface ServerDetail {
  id: string; name: string; code: string; description?: string;
  serverType: string; environment: string; status: string; criticality: string;
  operatingSystem?: string; ipAddress?: string; hostname?: string;
  cpuCores?: number; ramGB?: number; diskGB?: number; dataCenter?: string;
  ownerUserId?: string; adminUserId?: string;
  createdAt: string; updatedAt?: string; projects?: CIProjectItem[];
}
interface CIProjectItem { projectId: string; projectKey: string; projectName: string; projectStatus: string; role: string; }

const SERVER_TYPE_CFG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  Physical: { icon: Box, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Fiziksel" },
  Virtual: { icon: Monitor, color: "text-indigo-400", bg: "bg-indigo-500/10 border-indigo-500/20", label: "Sanal" },
  Cloud: { icon: Cloud, color: "text-cyan-400", bg: "bg-cyan-500/10 border-cyan-500/20", label: "Bulut" },
  Container: { icon: Box, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Konteyner" },
};
const ENV_CFG: Record<string, { color: string; bg: string; label: string }> = {
  Production: { color: "text-red-400", bg: "bg-red-500/10 border-red-500/20", label: "Production" },
  Staging: { color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Staging" },
  Development: { color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Development" },
  Test: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Test" },
  DR: { color: "text-violet-400", bg: "bg-violet-500/10 border-violet-500/20", label: "DR" },
};
const STATUS_CFG: Record<string, { icon: React.ComponentType<{ className?: string }>; color: string; bg: string; label: string }> = {
  Planned: { icon: Clock, color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Planlandı" },
  Active: { icon: CheckCircle2, color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "Aktif" },
  Deprecated: { icon: AlertTriangle, color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Kullanımdan Kaldırılıyor" },
  Retired: { icon: Archive, color: "text-gray-400", bg: "bg-gray-500/10 border-gray-500/20", label: "Emekli" },
};
const CRIT_CFG: Record<string, { color: string; bg: string; label: string }> = {
  Low: { color: "text-slate-400", bg: "bg-slate-500/10 border-slate-500/20", label: "Düşük" },
  Medium: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "Orta" },
  High: { color: "text-orange-400", bg: "bg-orange-500/10 border-orange-500/20", label: "Yüksek" },
  Critical: { color: "text-red-400", bg: "bg-red-500/10 border-red-500/20", label: "Kritik" },
};
const STATUS_FLOW: Record<string, string[]> = { Planned: ["Active"], Active: ["Deprecated"], Deprecated: ["Retired", "Active"], Retired: [] };
function fmtDate(d?: string) { return d ? new Date(d).toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" }) : "—"; }

export default function ServerDetailPage() {
  const params = useParams(); const router = useRouter();
  const srvId = params?.id as string;
  const [srv, setSrv] = useState<ServerDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [editData, setEditData] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);

  const fetchSrv = useCallback(async () => {
    if (!srvId) return; setLoading(true);
    try { const r = await fetch(`/api/pm/servers/${srvId}`); if (r.ok) setSrv(await r.json()); } catch {}
    finally { setLoading(false); }
  }, [srvId]);
  useEffect(() => { fetchSrv(); }, [fetchSrv]);

  const startEdit = () => {
    if (!srv) return;
    setEditData({ name: srv.name, description: srv.description || "", hostname: srv.hostname || "", ipAddress: srv.ipAddress || "",
      operatingSystem: srv.operatingSystem || "", dataCenter: srv.dataCenter || "", criticality: srv.criticality,
      cpuCores: srv.cpuCores?.toString() || "", ramGB: srv.ramGB?.toString() || "", diskGB: srv.diskGB?.toString() || "" });
    setEditing(true);
  };

  const saveEdit = async () => {
    if (!srv) return; setSaving(true);
    try {
      const r = await fetch(`/api/pm/servers/${srv.id}`, { method: "PUT", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: editData.name || null, description: editData.description || null, hostname: editData.hostname || null,
          ipAddress: editData.ipAddress || null, operatingSystem: editData.operatingSystem || null, dataCenter: editData.dataCenter || null,
          criticality: editData.criticality || null, cpuCores: editData.cpuCores ? parseInt(editData.cpuCores) : null,
          ramGB: editData.ramGB ? parseInt(editData.ramGB) : null, diskGB: editData.diskGB ? parseInt(editData.diskGB) : null }) });
      if (r.ok) { setEditing(false); fetchSrv(); }
    } catch {} finally { setSaving(false); }
  };

  const changeStatus = async (s: string) => {
    if (!srv) return;
    try { await fetch(`/api/pm/servers/${srv.id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status: s }) }); fetchSrv(); } catch {}
  };

  if (loading) return <div className="flex items-center justify-center py-20"><Loader2 className="w-6 h-6 text-violet-400 animate-spin" /></div>;
  if (!srv) return <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]"><Server className="w-12 h-12 opacity-30 mb-3" /><p className="text-sm">Sunucu bulunamadı</p><button onClick={() => router.back()} className="mt-3 text-xs text-violet-400">← Geri</button></div>;

  const typeCfg = SERVER_TYPE_CFG[srv.serverType] || SERVER_TYPE_CFG.Virtual;
  const envCfg = ENV_CFG[srv.environment] || ENV_CFG.Production;
  const statusCfg = STATUS_CFG[srv.status] || STATUS_CFG.Planned;
  const critCfg = CRIT_CFG[srv.criticality] || CRIT_CFG.Medium;
  const TypeIcon = typeCfg.icon; const StatusIcon = statusCfg.icon;
  const nextStatuses = STATUS_FLOW[srv.status] || [];

  return (
    <div className="space-y-5">
      <div className="flex items-center gap-2 text-sm text-[var(--color-text-muted)]">
        <button onClick={() => router.push("/manage/cmdb/servers")} className="flex items-center gap-1 hover:text-[var(--color-text)] transition-colors"><ArrowLeft className="w-4 h-4" /> Sunucular</button>
        <ChevronRight className="w-3 h-3" /><span className="text-[var(--color-text)] font-medium">{srv.code}</span>
      </div>

      <div className="flex items-start justify-between">
        <div className="flex items-start gap-4">
          <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-violet-500 to-purple-600 flex items-center justify-center shadow-lg shadow-violet-500/30 mt-0.5">
            <Server className="w-6 h-6 text-white" />
          </div>
          <div>
            <div className="flex items-center gap-3 mb-1 flex-wrap">
              <span className="text-sm font-mono font-bold text-violet-400 bg-violet-500/10 px-2.5 py-1 rounded-md">{srv.code}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", statusCfg.bg, statusCfg.color)}><StatusIcon className="w-3.5 h-3.5" /> {statusCfg.label}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", typeCfg.bg, typeCfg.color)}><TypeIcon className="w-3.5 h-3.5" /> {typeCfg.label}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", envCfg.bg, envCfg.color)}>{envCfg.label}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", critCfg.bg, critCfg.color)}><Shield className="w-3.5 h-3.5" /> {critCfg.label}</span>
            </div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">{srv.name}</h1>
            {srv.description && <p className="text-sm text-[var(--color-text-muted)] mt-1 max-w-2xl">{srv.description}</p>}
          </div>
        </div>
        <div className="flex items-center gap-2">
          {nextStatuses.map(s => { const sc = STATUS_CFG[s] || STATUS_CFG.Planned; return (
            <button key={s} onClick={() => changeStatus(s)} className={cn("flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-medium border transition-all hover:opacity-80", sc.bg, sc.color)}>{sc.label}</button>
          ); })}
          {!editing && <button onClick={startEdit} className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-border)]/50 transition-all"><Edit3 className="w-3.5 h-3.5" /> Düzenle</button>}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        <div className="lg:col-span-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-6">
          {editing ? (
            <div className="space-y-4">
              <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3">Sunucu Bilgileri</h3>
              <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Ad</label>
                <input type="text" value={editData.name || ""} onChange={e => setEditData(d => ({ ...d, name: e.target.value }))}
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-violet-500/40" /></div>
              <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Açıklama</label>
                <textarea rows={2} value={editData.description || ""} onChange={e => setEditData(d => ({ ...d, description: e.target.value }))}
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-violet-500/40 resize-none" /></div>
              <div className="grid grid-cols-2 gap-4">
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Hostname</label>
                  <input type="text" value={editData.hostname || ""} onChange={e => setEditData(d => ({ ...d, hostname: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] font-mono" /></div>
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">IP Adresi</label>
                  <input type="text" value={editData.ipAddress || ""} onChange={e => setEditData(d => ({ ...d, ipAddress: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] font-mono" /></div>
              </div>
              <div className="grid grid-cols-3 gap-4">
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">CPU (vCPU)</label>
                  <input type="number" value={editData.cpuCores || ""} onChange={e => setEditData(d => ({ ...d, cpuCores: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">RAM (GB)</label>
                  <input type="number" value={editData.ramGB || ""} onChange={e => setEditData(d => ({ ...d, ramGB: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Disk (GB)</label>
                  <input type="number" value={editData.diskGB || ""} onChange={e => setEditData(d => ({ ...d, diskGB: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">İşletim Sistemi</label>
                  <input type="text" value={editData.operatingSystem || ""} onChange={e => setEditData(d => ({ ...d, operatingSystem: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Kritiklik</label>
                  <select value={editData.criticality || ""} onChange={e => setEditData(d => ({ ...d, criticality: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    {Object.entries(CRIT_CFG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}</select></div>
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button onClick={() => setEditing(false)} className="px-4 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50 transition-colors"><X className="w-4 h-4 inline-block mr-1" />İptal</button>
                <button onClick={saveEdit} disabled={saving} className="px-4 py-2 rounded-lg text-sm font-medium bg-violet-600 text-white hover:bg-violet-700 disabled:opacity-50 transition-colors flex items-center gap-2">
                  {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />} Kaydet</button>
              </div>
            </div>
          ) : (
            <div className="space-y-5">
              <h3 className="text-sm font-semibold text-[var(--color-text)] mb-4">Sunucu Bilgileri</h3>
              <div className="grid grid-cols-2 gap-y-4 gap-x-8">
                <InfoRow icon={Server} label="Kod" value={srv.code} mono />
                <InfoRow icon={Calendar} label="Oluşturma" value={fmtDate(srv.createdAt)} />
                {srv.hostname && <InfoRow icon={Network} label="Hostname" value={srv.hostname} mono />}
                {srv.ipAddress && <InfoRow icon={Network} label="IP Adresi" value={srv.ipAddress} mono />}
                {srv.operatingSystem && <InfoRow icon={Monitor} label="İşletim Sistemi" value={srv.operatingSystem} />}
                {srv.dataCenter && <InfoRow icon={MapPin} label="Veri Merkezi" value={srv.dataCenter} />}
              </div>
              {(srv.cpuCores || srv.ramGB || srv.diskGB) && (
                <div className="pt-4 border-t border-[var(--color-border)]">
                  <h4 className="text-xs font-semibold text-[var(--color-text-muted)] mb-3 uppercase tracking-wider">Donanım Kaynakları</h4>
                  <div className="grid grid-cols-3 gap-4">
                    {srv.cpuCores && <ResourceCard icon={Cpu} label="CPU" value={`${srv.cpuCores} vCPU`} color="violet" />}
                    {srv.ramGB && <ResourceCard icon={MemoryStick} label="RAM" value={`${srv.ramGB} GB`} color="cyan" />}
                    {srv.diskGB && <ResourceCard icon={HardDrive} label="Disk" value={`${srv.diskGB} GB`} color="emerald" />}
                  </div>
                </div>
              )}
              {srv.updatedAt && <p className="text-[10px] text-[var(--color-text-muted)] pt-3 border-t border-[var(--color-border)]">Son güncelleme: {fmtDate(srv.updatedAt)}</p>}
            </div>
          )}
        </div>

        <div className="space-y-4">
          <CIRelationshipsPanel ciId={srvId} ciName={srv.name} accentColor="violet" />
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3 flex items-center gap-2"><FolderKanban className="w-4 h-4 text-violet-400" /> İlgili Projeler</h3>
            {(srv.projects && srv.projects.length > 0) ? (
              <ul className="space-y-2">{srv.projects.map(p => (
                <li key={p.projectId}><button onClick={() => router.push(`/dashboard/projects/${p.projectId}`)}
                  className="w-full text-left p-2.5 rounded-lg bg-[var(--color-bg)] hover:bg-[var(--color-border)] transition-colors">
                  <div className="flex items-center justify-between"><span className="text-xs font-mono font-bold text-violet-400">{p.projectKey}</span></div>
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
  return (<div className="flex items-start gap-2.5"><Icon className="w-4 h-4 text-[var(--color-text-muted)] mt-0.5 shrink-0" /><div className="min-w-0"><div className="text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider">{label}</div><div className={cn("text-sm text-[var(--color-text)]", mono && "font-mono font-bold text-violet-400")}>{value}</div></div></div>);
}

function ResourceCard({ icon: Icon, label, value, color }: { icon: React.ComponentType<{ className?: string }>; label: string; value: string; color: string; }) {
  return (
    <div className={`rounded-lg border border-${color}-500/20 bg-${color}-500/5 p-3 text-center`}>
      <Icon className={`w-5 h-5 text-${color}-400 mx-auto mb-1`} />
      <div className="text-lg font-bold text-[var(--color-text)]">{value}</div>
      <div className="text-[10px] text-[var(--color-text-muted)] uppercase">{label}</div>
    </div>
  );
}
