"use client";

import { useState, useEffect, useCallback } from "react";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft, HardDrive, Calendar, Loader2, CheckCircle2, AlertTriangle, Archive,
  Clock, Edit3, Save, X, Shield, ChevronRight, Database, FolderKanban,
} from "lucide-react";
import { cn } from "@/lib/utils";
import CIRelationshipsPanel from "@/components/ci-relationships-panel";

interface DatabaseDetail {
  id: string; name: string; code: string; description?: string;
  databaseEngine: string; status: string; criticality: string;
  version?: string; port?: number; sizeGB?: number;
  connectionString?: string; backupSchedule?: string;
  ownerUserId?: string; adminUserId?: string;
  createdAt: string; updatedAt?: string; projects?: CIProjectItem[];
}
interface CIProjectItem { projectId: string; projectKey: string; projectName: string; projectStatus: string; role: string; }

const ENGINE_CFG: Record<string, { color: string; bg: string; label: string }> = {
  PostgreSQL: { color: "text-blue-400", bg: "bg-blue-500/10 border-blue-500/20", label: "PostgreSQL" },
  SQLServer: { color: "text-red-400", bg: "bg-red-500/10 border-red-500/20", label: "SQL Server" },
  Oracle: { color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20", label: "Oracle" },
  MySQL: { color: "text-cyan-400", bg: "bg-cyan-500/10 border-cyan-500/20", label: "MySQL" },
  MongoDB: { color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20", label: "MongoDB" },
  Redis: { color: "text-rose-400", bg: "bg-rose-500/10 border-rose-500/20", label: "Redis" },
  Elasticsearch: { color: "text-yellow-400", bg: "bg-yellow-500/10 border-yellow-500/20", label: "Elasticsearch" },
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

export default function DatabaseDetailPage() {
  const params = useParams(); const router = useRouter();
  const dbId = params?.id as string;
  const [db, setDb] = useState<DatabaseDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [editData, setEditData] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);

  const fetchDb = useCallback(async () => {
    if (!dbId) return; setLoading(true);
    try { const r = await fetch(`/api/pm/databases/${dbId}`); if (r.ok) setDb(await r.json()); } catch {}
    finally { setLoading(false); }
  }, [dbId]);
  useEffect(() => { fetchDb(); }, [fetchDb]);

  const startEdit = () => {
    if (!db) return;
    setEditData({ name: db.name, description: db.description || "", version: db.version || "",
      port: db.port?.toString() || "", backupSchedule: db.backupSchedule || "", criticality: db.criticality });
    setEditing(true);
  };

  const saveEdit = async () => {
    if (!db) return; setSaving(true);
    try {
      const r = await fetch(`/api/pm/databases/${db.id}`, { method: "PUT", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: editData.name || null, description: editData.description || null,
          version: editData.version || null, port: editData.port ? parseInt(editData.port) : null,
          backupSchedule: editData.backupSchedule || null, criticality: editData.criticality || null }) });
      if (r.ok) { setEditing(false); fetchDb(); }
    } catch {} finally { setSaving(false); }
  };

  const changeStatus = async (s: string) => {
    if (!db) return;
    try { await fetch(`/api/pm/databases/${db.id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status: s }) }); fetchDb(); } catch {}
  };

  if (loading) return <div className="flex items-center justify-center py-20"><Loader2 className="w-6 h-6 text-emerald-400 animate-spin" /></div>;
  if (!db) return <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]"><HardDrive className="w-12 h-12 opacity-30 mb-3" /><p className="text-sm">Veritabanı bulunamadı</p><button onClick={() => router.back()} className="mt-3 text-xs text-emerald-400">← Geri</button></div>;

  const engCfg = ENGINE_CFG[db.databaseEngine] || ENGINE_CFG.PostgreSQL;
  const statusCfg = STATUS_CFG[db.status] || STATUS_CFG.Planned;
  const critCfg = CRIT_CFG[db.criticality] || CRIT_CFG.Medium;
  const StatusIcon = statusCfg.icon;
  const nextStatuses = STATUS_FLOW[db.status] || [];

  return (
    <div className="space-y-5">
      <div className="flex items-center gap-2 text-sm text-[var(--color-text-muted)]">
        <button onClick={() => router.push("/dashboard/databases")} className="flex items-center gap-1 hover:text-[var(--color-text)] transition-colors"><ArrowLeft className="w-4 h-4" /> Veritabanları</button>
        <ChevronRight className="w-3 h-3" /><span className="text-[var(--color-text)] font-medium">{db.code}</span>
      </div>

      <div className="flex items-start justify-between">
        <div className="flex items-start gap-4">
          <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-emerald-500 to-teal-600 flex items-center justify-center shadow-lg shadow-emerald-500/30 mt-0.5">
            <HardDrive className="w-6 h-6 text-white" />
          </div>
          <div>
            <div className="flex items-center gap-3 mb-1 flex-wrap">
              <span className="text-sm font-mono font-bold text-emerald-400 bg-emerald-500/10 px-2.5 py-1 rounded-md">{db.code}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", statusCfg.bg, statusCfg.color)}><StatusIcon className="w-3.5 h-3.5" /> {statusCfg.label}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", engCfg.bg, engCfg.color)}><Database className="w-3.5 h-3.5" /> {engCfg.label}</span>
              <span className={cn("inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border", critCfg.bg, critCfg.color)}><Shield className="w-3.5 h-3.5" /> {critCfg.label}</span>
              {db.version && <span className="text-xs font-mono text-[var(--color-text-muted)] bg-[var(--color-border)] px-2 py-1 rounded">v{db.version}</span>}
            </div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">{db.name}</h1>
            {db.description && <p className="text-sm text-[var(--color-text-muted)] mt-1 max-w-2xl">{db.description}</p>}
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
              <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3">Veritabanı Bilgileri</h3>
              <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Ad</label>
                <input type="text" value={editData.name || ""} onChange={e => setEditData(d => ({ ...d, name: e.target.value }))}
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-emerald-500/40" /></div>
              <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Açıklama</label>
                <textarea rows={2} value={editData.description || ""} onChange={e => setEditData(d => ({ ...d, description: e.target.value }))}
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-emerald-500/40 resize-none" /></div>
              <div className="grid grid-cols-2 gap-4">
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Sürüm</label>
                  <input type="text" value={editData.version || ""} onChange={e => setEditData(d => ({ ...d, version: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] font-mono" /></div>
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Port</label>
                  <input type="number" value={editData.port || ""} onChange={e => setEditData(d => ({ ...d, port: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Yedekleme Planı</label>
                  <input type="text" value={editData.backupSchedule || ""} onChange={e => setEditData(d => ({ ...d, backupSchedule: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]" /></div>
                <div><label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Kritiklik</label>
                  <select value={editData.criticality || ""} onChange={e => setEditData(d => ({ ...d, criticality: e.target.value }))}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                    {Object.entries(CRIT_CFG).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}</select></div>
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button onClick={() => setEditing(false)} className="px-4 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50"><X className="w-4 h-4 inline-block mr-1" />İptal</button>
                <button onClick={saveEdit} disabled={saving} className="px-4 py-2 rounded-lg text-sm font-medium bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50 flex items-center gap-2">
                  {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />} Kaydet</button>
              </div>
            </div>
          ) : (
            <div className="space-y-5">
              <h3 className="text-sm font-semibold text-[var(--color-text)] mb-4">Veritabanı Bilgileri</h3>
              <div className="grid grid-cols-2 gap-y-4 gap-x-8">
                <InfoRow icon={HardDrive} label="Kod" value={db.code} mono accent="emerald" />
                <InfoRow icon={Calendar} label="Oluşturma" value={fmtDate(db.createdAt)} accent="emerald" />
                <InfoRow icon={Database} label="Motor" value={engCfg.label} accent="emerald" />
                {db.version && <InfoRow icon={Database} label="Sürüm" value={`v${db.version}`} mono accent="emerald" />}
                {db.port && <InfoRow icon={Database} label="Port" value={db.port.toString()} mono accent="emerald" />}
                {db.sizeGB && <InfoRow icon={HardDrive} label="Boyut" value={`${db.sizeGB} GB`} accent="emerald" />}
                {db.backupSchedule && <InfoRow icon={Clock} label="Yedekleme" value={db.backupSchedule} accent="emerald" />}
              </div>
              {db.updatedAt && <p className="text-[10px] text-[var(--color-text-muted)] pt-3 border-t border-[var(--color-border)]">Son güncelleme: {fmtDate(db.updatedAt)}</p>}
            </div>
          )}
        </div>

        <div className="space-y-4">
          <CIRelationshipsPanel ciId={dbId} ciName={db.name} accentColor="emerald" />
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
            <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3 flex items-center gap-2"><FolderKanban className="w-4 h-4 text-emerald-400" /> İlgili Projeler</h3>
            {(db.projects && db.projects.length > 0) ? (
              <ul className="space-y-2">{db.projects.map(p => (
                <li key={p.projectId}><button onClick={() => router.push(`/dashboard/projects/${p.projectId}`)}
                  className="w-full text-left p-2.5 rounded-lg bg-[var(--color-bg)] hover:bg-[var(--color-border)] transition-colors">
                  <span className="text-xs font-mono font-bold text-emerald-400">{p.projectKey}</span>
                  <p className="text-xs text-[var(--color-text)] mt-1 truncate">{p.projectName}</p></button></li>
              ))}</ul>
            ) : <p className="text-xs text-[var(--color-text-muted)]">İlgili proje bulunmuyor.</p>}
          </div>
        </div>
      </div>
    </div>
  );
}

function InfoRow({ icon: Icon, label, value, mono, accent = "emerald" }: { icon: React.ComponentType<{ className?: string }>; label: string; value: string; mono?: boolean; accent?: string; }) {
  return (<div className="flex items-start gap-2.5"><Icon className="w-4 h-4 text-[var(--color-text-muted)] mt-0.5 shrink-0" /><div className="min-w-0"><div className="text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider">{label}</div><div className={cn("text-sm text-[var(--color-text)]", mono && `font-mono font-bold text-${accent}-400`)}>{value}</div></div></div>);
}
