"use client";

import { useState, useEffect, useCallback } from "react";
import {
  Link2, Plus, Loader2, Trash2, ArrowRight, ArrowLeft,
  Server, HardDrive, KeyRound, AppWindow, X, Search,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface CIRelationship {
  id: string;
  sourceCIId: string; sourceName: string; sourceCode: string; sourceType: string;
  targetCIId: string; targetName: string; targetCode: string; targetType: string;
  relationType: string; notes?: string; direction: string;
}

interface CISearchResult {
  id: string; name: string; code: string; type: string;
}

const RELATION_TYPES = [
  { value: "RunsOn", label: "Üzerinde Çalışır", desc: "Bu CI hedef üzerinde çalışır" },
  { value: "DependsOn", label: "Bağımlı", desc: "Bu CI hedefe bağımlıdır" },
  { value: "Hosts", label: "Barındırır", desc: "Bu CI hedefi barındırır" },
  { value: "ConnectsTo", label: "Bağlanır", desc: "Bu CI hedefe bağlanır" },
  { value: "ManagedBy", label: "Yönetici", desc: "Bu CI hedef tarafından yönetilir" },
  { value: "LicensedBy", label: "Lisanslı", desc: "Bu CI hedef lisansı kullanır" },
];

const TYPE_ICON: Record<string, React.ComponentType<{ className?: string }>> = {
  Application: AppWindow, Server: Server, Database: HardDrive, Licence: KeyRound,
};

const TYPE_COLOR: Record<string, { color: string; bg: string }> = {
  Application: { color: "text-cyan-400", bg: "bg-cyan-500/10 border-cyan-500/20" },
  Server: { color: "text-violet-400", bg: "bg-violet-500/10 border-violet-500/20" },
  Database: { color: "text-emerald-400", bg: "bg-emerald-500/10 border-emerald-500/20" },
  Licence: { color: "text-amber-400", bg: "bg-amber-500/10 border-amber-500/20" },
  CI: { color: "text-gray-400", bg: "bg-gray-500/10 border-gray-500/20" },
};

export default function CIRelationshipsPanel({ ciId, ciName, accentColor = "cyan" }: {
  ciId: string; ciName: string; accentColor?: string;
}) {
  const [relationships, setRelationships] = useState<CIRelationship[]>([]);
  const [loading, setLoading] = useState(true);
  const [showAdd, setShowAdd] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [searchResults, setSearchResults] = useState<CISearchResult[]>([]);
  const [searching, setSearching] = useState(false);
  const [selectedTarget, setSelectedTarget] = useState<CISearchResult | null>(null);
  const [relationType, setRelationType] = useState("DependsOn");
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const fetchRelationships = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetch(`/api/pm/ci/${ciId}/relationships`);
      if (res.ok) setRelationships(await res.json());
    } catch { /* */ }
    finally { setLoading(false); }
  }, [ciId]);

  useEffect(() => { fetchRelationships(); }, [fetchRelationships]);

  // Search all CI types
  const searchCIs = useCallback(async (q: string) => {
    if (q.length < 2) { setSearchResults([]); return; }
    setSearching(true);
    try {
      const results: CISearchResult[] = [];
      // Search applications, servers, databases, licences in parallel
      const [apps, srvs, dbs, lics] = await Promise.all([
        fetch(`/api/pm/applications`).then(r => r.ok ? r.json() : []),
        fetch(`/api/pm/servers`).then(r => r.ok ? r.json() : []),
        fetch(`/api/pm/databases`).then(r => r.ok ? r.json() : []),
        fetch(`/api/pm/licences`).then(r => r.ok ? r.json() : []),
      ]);
      const lq = q.toLowerCase();
      for (const a of (Array.isArray(apps) ? apps : [])) {
        if (a.id !== ciId && (a.name?.toLowerCase().includes(lq) || a.code?.toLowerCase().includes(lq)))
          results.push({ id: a.id, name: a.name, code: a.code, type: "Application" });
      }
      for (const s of (Array.isArray(srvs) ? srvs : [])) {
        if (s.id !== ciId && (s.name?.toLowerCase().includes(lq) || s.code?.toLowerCase().includes(lq)))
          results.push({ id: s.id, name: s.name, code: s.code, type: "Server" });
      }
      for (const d of (Array.isArray(dbs) ? dbs : [])) {
        if (d.id !== ciId && (d.name?.toLowerCase().includes(lq) || d.code?.toLowerCase().includes(lq)))
          results.push({ id: d.id, name: d.name, code: d.code, type: "Database" });
      }
      for (const l of (Array.isArray(lics) ? lics : [])) {
        if (l.id !== ciId && (l.name?.toLowerCase().includes(lq) || l.code?.toLowerCase().includes(lq)))
          results.push({ id: l.id, name: l.name, code: l.code, type: "Licence" });
      }
      setSearchResults(results.slice(0, 10));
    } catch { setSearchResults([]); }
    finally { setSearching(false); }
  }, [ciId]);

  useEffect(() => {
    const timer = setTimeout(() => { if (searchQuery) searchCIs(searchQuery); }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery, searchCIs]);

  const addRelationship = async () => {
    if (!selectedTarget) { setError("Hedef CI seçiniz."); return; }
    setSaving(true); setError("");
    try {
      const res = await fetch(`/api/pm/ci/${ciId}/relationships`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ targetCIId: selectedTarget.id, relationType, notes: notes || null }),
      });
      if (res.ok) {
        setShowAdd(false); setSelectedTarget(null); setNotes(""); setSearchQuery(""); setSearchResults([]);
        fetchRelationships();
      } else {
        const data = await res.json().catch(() => ({}));
        setError(data.detail || `Hata: ${res.status}`);
      }
    } catch { setError("Bağlantı hatası."); }
    finally { setSaving(false); }
  };

  const removeRelationship = async (relId: string) => {
    try {
      await fetch(`/api/pm/ci/relationships/${relId}`, { method: "DELETE" });
      fetchRelationships();
    } catch { /* */ }
  };

  const outgoing = relationships.filter(r => r.direction === "outgoing");
  const incoming = relationships.filter(r => r.direction === "incoming");

  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-semibold text-[var(--color-text)] flex items-center gap-2">
          <Link2 className={`w-4 h-4 text-${accentColor}-400`} /> İlişkiler
          <span className="text-xs font-normal text-[var(--color-text-muted)]">({relationships.length})</span>
        </h3>
        <button onClick={() => setShowAdd(true)}
          className={`flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-medium bg-${accentColor}-600 text-white hover:bg-${accentColor}-700 transition-colors`}>
          <Plus className="w-3 h-3" /> Ekle
        </button>
      </div>

      {loading ? (
        <div className="flex justify-center py-4"><Loader2 className={`w-4 h-4 text-${accentColor}-400 animate-spin`} /></div>
      ) : relationships.length === 0 ? (
        <p className="text-xs text-[var(--color-text-muted)] text-center py-4">Henüz ilişki tanımlanmamış.</p>
      ) : (
        <div className="space-y-3">
          {outgoing.length > 0 && (
            <div>
              <div className="text-[10px] uppercase tracking-wider text-[var(--color-text-muted)] mb-2 flex items-center gap-1">
                <ArrowRight className="w-3 h-3" /> Giden İlişkiler
              </div>
              <div className="space-y-1.5">
                {outgoing.map(rel => <RelRow key={rel.id} rel={rel} onRemove={removeRelationship} side="target" />)}
              </div>
            </div>
          )}
          {incoming.length > 0 && (
            <div>
              <div className="text-[10px] uppercase tracking-wider text-[var(--color-text-muted)] mb-2 flex items-center gap-1">
                <ArrowLeft className="w-3 h-3" /> Gelen İlişkiler
              </div>
              <div className="space-y-1.5">
                {incoming.map(rel => <RelRow key={rel.id} rel={rel} onRemove={removeRelationship} side="source" />)}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Add Relationship Modal */}
      {showAdd && (
        <>
          <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm" onClick={() => { setShowAdd(false); setSelectedTarget(null); setError(""); }} />
          <div className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50 w-full max-w-md bg-[var(--color-card-bg)] border border-[var(--color-border)] rounded-2xl shadow-2xl">
            <div className="flex items-center justify-between px-5 py-4 border-b border-[var(--color-border)]">
              <h4 className="text-sm font-semibold text-[var(--color-text)]">İlişki Ekle</h4>
              <button onClick={() => { setShowAdd(false); setSelectedTarget(null); setError(""); }} className="p-1.5 rounded-lg hover:bg-[var(--color-border)] transition-colors">
                <X className="w-4 h-4 text-[var(--color-text-muted)]" /></button>
            </div>
            <div className="px-5 py-4 space-y-4">
              {/* Search target CI */}
              <div>
                <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1.5">Hedef CI</label>
                {selectedTarget ? (
                  <div className="flex items-center gap-2 p-2.5 rounded-lg bg-[var(--color-bg)] border border-[var(--color-border)]">
                    <CITypeBadge type={selectedTarget.type} />
                    <span className="text-xs font-mono text-[var(--color-text-muted)]">{selectedTarget.code}</span>
                    <span className="text-sm text-[var(--color-text)] flex-1">{selectedTarget.name}</span>
                    <button onClick={() => { setSelectedTarget(null); setSearchQuery(""); }} className="p-1 rounded hover:bg-[var(--color-border)]">
                      <X className="w-3 h-3 text-[var(--color-text-muted)]" /></button>
                  </div>
                ) : (
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-[var(--color-text-muted)]" />
                    <input type="text" value={searchQuery} onChange={e => setSearchQuery(e.target.value)}
                      placeholder="CI adı veya kodu ile arayın..."
                      className="w-full pl-9 pr-4 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40" />
                    {searching && <Loader2 className="absolute right-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-cyan-400 animate-spin" />}
                    {searchResults.length > 0 && (
                      <div className="absolute z-10 w-full mt-1 bg-[var(--color-card-bg)] border border-[var(--color-border)] rounded-lg shadow-xl max-h-48 overflow-y-auto">
                        {searchResults.map(r => (
                          <button key={r.id} onClick={() => { setSelectedTarget(r); setSearchQuery(""); setSearchResults([]); }}
                            className="w-full text-left px-3 py-2 hover:bg-[var(--color-border)]/50 flex items-center gap-2 transition-colors">
                            <CITypeBadge type={r.type} />
                            <span className="text-[10px] font-mono text-[var(--color-text-muted)]">{r.code}</span>
                            <span className="text-xs text-[var(--color-text)] truncate">{r.name}</span>
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>
              {/* Relation type */}
              <div>
                <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1.5">İlişki Tipi</label>
                <select value={relationType} onChange={e => setRelationType(e.target.value)}
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                  {RELATION_TYPES.map(rt => <option key={rt.value} value={rt.value}>{rt.label} — {rt.desc}</option>)}
                </select>
              </div>
              {/* Notes */}
              <div>
                <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1.5">Notlar (opsiyonel)</label>
                <input type="text" value={notes} onChange={e => setNotes(e.target.value)} placeholder="İlişki hakkında not..."
                  className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40" />
              </div>
              {error && <div className="p-2.5 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-xs">{error}</div>}
            </div>
            <div className="px-5 py-3 border-t border-[var(--color-border)] flex justify-end gap-2">
              <button onClick={() => { setShowAdd(false); setSelectedTarget(null); setError(""); }}
                className="px-3 py-2 rounded-lg text-xs font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)]/50 transition-colors">İptal</button>
              <button onClick={addRelationship} disabled={saving || !selectedTarget}
                className="px-4 py-2 rounded-lg text-xs font-medium bg-cyan-600 text-white hover:bg-cyan-700 disabled:opacity-50 transition-colors flex items-center gap-1.5">
                {saving ? <Loader2 className="w-3 h-3 animate-spin" /> : <Plus className="w-3 h-3" />} Ekle
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

function CITypeBadge({ type }: { type: string }) {
  const Icon = TYPE_ICON[type] || Link2;
  const colors = TYPE_COLOR[type] || TYPE_COLOR.CI;
  return (
    <span className={cn("inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-medium border", colors.bg, colors.color)}>
      <Icon className="w-3 h-3" /> {type}
    </span>
  );
}

function RelRow({ rel, onRemove, side }: { rel: CIRelationship; onRemove: (id: string) => void; side: "source" | "target" }) {
  const name = side === "target" ? rel.targetName : rel.sourceName;
  const code = side === "target" ? rel.targetCode : rel.sourceCode;
  const type = side === "target" ? rel.targetType : rel.sourceType;
  const relLabel = RELATION_TYPES.find(rt => rt.value === rel.relationType)?.label || rel.relationType;

  return (
    <div className="flex items-center gap-2 p-2 rounded-lg bg-[var(--color-bg)] hover:bg-[var(--color-border)]/30 transition-colors group">
      <CITypeBadge type={type} />
      <span className="text-[10px] font-mono text-[var(--color-text-muted)]">{code}</span>
      <span className="text-xs text-[var(--color-text)] flex-1 truncate">{name}</span>
      <span className="text-[10px] text-[var(--color-text-muted)] bg-[var(--color-border)]/50 px-1.5 py-0.5 rounded">{relLabel}</span>
      <button onClick={() => onRemove(rel.id)}
        className="p-1 rounded opacity-0 group-hover:opacity-100 hover:bg-red-500/10 text-red-400 transition-all">
        <Trash2 className="w-3 h-3" />
      </button>
    </div>
  );
}
