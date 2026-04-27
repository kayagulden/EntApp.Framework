"use client";

import { useState, useCallback, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import dynamic from "next/dynamic";
import { WikiPageTreeView } from "@/components/wiki/WikiComponents";
import {
  ArrowLeft, BookOpen, Plus, Loader2, Save, Globe, Pencil,
  Archive, Eye, ChevronRight, Clock, User, Lock, Unlock,
  RotateCcw, History, FileText, Trash2,
} from "lucide-react";
import { cn } from "@/lib/utils";

// Dynamic import for RichTextEditor (SSR uyumlu)
const RichTextEditor = dynamic(
  () => import("@/components/shared/RichTextEditor").then(m => m.RichTextEditor),
  { ssr: false, loading: () => <div className="h-64 rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] animate-pulse" /> }
);

const RichTextViewer = dynamic(
  () => import("@/components/shared/RichTextEditor").then(m => m.RichTextViewer),
  { ssr: false }
);

// ── Types ────────────────────────────────────────────
interface WikiPageDetail {
  id: string;
  wikiSpaceId: string;
  spaceName: string;
  title: string;
  slug: string;
  contentJson: string;
  contentHtml: string;
  status: string;
  viewCount: number;
  lastEditedByUserId?: string;
  lockedByUserId?: string;
  lockedAt?: string;
  publishedAt?: string;
  sourceRequirementId?: string;
  sourceTicketId?: string;
  createdAt: string;
  updatedAt?: string;
  versionCount: number;
  breadcrumbs: { id: string; title: string; slug: string }[];
}

interface WikiPageVersion {
  id: string;
  versionNumber: number;
  changeNote?: string;
  authorUserId?: string;
  createdAt: string;
}

interface WikiSpace {
  id: string;
  name: string;
  slug: string;
  description?: string;
  iconEmoji?: string;
}

const STATUS_MAP: Record<string, { label: string; color: string; icon: React.ComponentType<{ className?: string }> }> = {
  Draft: { label: "Taslak", color: "bg-amber-500/20 text-amber-400 border-amber-500/30", icon: Pencil },
  Published: { label: "Yayında", color: "bg-emerald-500/20 text-emerald-400 border-emerald-500/30", icon: Globe },
  Archived: { label: "Arşiv", color: "bg-slate-500/20 text-slate-400 border-slate-500/30", icon: Archive },
};

// ── WikiSpaceDetail ─────────────────────────────────
export default function WikiSpaceDetailPage() {
  const params = useParams();
  const router = useRouter();
  const spaceId = params?.id as string;

  const [space, setSpace] = useState<WikiSpace | null>(null);
  const [selectedPageId, setSelectedPageId] = useState<string | null>(null);
  const [page, setPage] = useState<WikiPageDetail | null>(null);
  const [pageLoading, setPageLoading] = useState(false);
  const [editing, setEditing] = useState(false);
  const [editTitle, setEditTitle] = useState("");
  const [editJson, setEditJson] = useState("");
  const [editHtml, setEditHtml] = useState("");
  const [saving, setSaving] = useState(false);
  const [changeNote, setChangeNote] = useState("");

  // Create page state
  const [showCreate, setShowCreate] = useState(false);
  const [newTitle, setNewTitle] = useState("");
  const [newJson, setNewJson] = useState("");
  const [newHtml, setNewHtml] = useState("");
  const [creating, setCreating] = useState(false);

  // Version history
  const [showVersions, setShowVersions] = useState(false);
  const [versions, setVersions] = useState<WikiPageVersion[]>([]);
  const [versionsLoading, setVersionsLoading] = useState(false);

  // Fetch space info
  useEffect(() => {
    if (!spaceId) return;
    fetch(`/api/v1/wiki/spaces/${spaceId}`)
      .then(r => r.ok ? r.json() : null)
      .then(data => setSpace(data))
      .catch(() => {});
  }, [spaceId]);

  // Fetch page detail
  const fetchPage = useCallback(async (pageId: string) => {
    setPageLoading(true);
    try {
      const res = await fetch(`/api/v1/wiki/pages/${pageId}`);
      if (res.ok) {
        const data = await res.json();
        setPage(data);
        setEditing(false);
      }
    } catch { /* */ }
    finally { setPageLoading(false); }
  }, []);

  useEffect(() => {
    if (selectedPageId) fetchPage(selectedPageId);
    else setPage(null);
  }, [selectedPageId, fetchPage]);

  // Start editing
  const startEdit = () => {
    if (!page) return;
    setEditTitle(page.title);
    setEditJson(page.contentJson);
    setEditHtml(page.contentHtml);
    setChangeNote("");
    setEditing(true);
    // Lock page
    fetch(`/api/v1/wiki/pages/${page.id}/lock`, { method: "POST" }).catch(() => {});
  };

  // Save page
  const savePage = async () => {
    if (!page) return;
    setSaving(true);
    try {
      const res = await fetch(`/api/v1/wiki/pages/${page.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          title: editTitle,
          contentJson: editJson,
          contentHtml: editHtml,
          changeNote: changeNote || null,
        }),
      });
      if (res.ok) {
        // Unlock
        await fetch(`/api/v1/wiki/pages/${page.id}/unlock`, { method: "POST" }).catch(() => {});
        fetchPage(page.id);
      }
    } catch { /* */ }
    finally { setSaving(false); }
  };

  const cancelEdit = async () => {
    if (page) {
      await fetch(`/api/v1/wiki/pages/${page.id}/unlock`, { method: "POST" }).catch(() => {});
    }
    setEditing(false);
  };

  // Publish
  const publishPage = async () => {
    if (!page) return;
    try {
      await fetch(`/api/v1/wiki/pages/${page.id}/publish`, { method: "POST" });
      fetchPage(page.id);
    } catch { /* */ }
  };

  // Archive
  const archivePage = async () => {
    if (!page) return;
    try {
      await fetch(`/api/v1/wiki/pages/${page.id}/archive`, { method: "POST" });
      fetchPage(page.id);
    } catch { /* */ }
  };

  // Delete page
  const deletePage = async () => {
    if (!page || !confirm("Bu sayfa silinecek. Emin misiniz?")) return;
    try {
      await fetch(`/api/v1/wiki/pages/${page.id}`, { method: "DELETE" });
      setSelectedPageId(null);
      setPage(null);
    } catch { /* */ }
  };

  // Create page
  const createPage = async () => {
    if (!newTitle.trim()) return;
    setCreating(true);
    try {
      const res = await fetch("/api/v1/wiki/pages", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          spaceId,
          title: newTitle,
          contentJson: newJson || JSON.stringify({ type: "doc", content: [{ type: "paragraph" }] }),
          contentHtml: newHtml || "<p></p>",
          parentPageId: selectedPageId || null,
        }),
      });
      if (res.ok) {
        const data = await res.json();
        setShowCreate(false);
        setNewTitle("");
        setNewJson("");
        setNewHtml("");
        setSelectedPageId(data.id);
      }
    } catch { /* */ }
    finally { setCreating(false); }
  };

  // Fetch versions
  const fetchVersions = async () => {
    if (!page) return;
    setVersionsLoading(true);
    try {
      const res = await fetch(`/api/v1/wiki/pages/${page.id}/versions`);
      if (res.ok) setVersions(await res.json());
    } catch { /* */ }
    finally { setVersionsLoading(false); }
  };

  useEffect(() => {
    if (showVersions && page) fetchVersions();
  }, [showVersions, page?.id]);

  // Revert to version
  const revertToVersion = async (versionId: string) => {
    if (!page || !confirm("Bu versiyona geri dönülecek. Emin misiniz?")) return;
    try {
      await fetch(`/api/v1/wiki/pages/${page.id}/revert/${versionId}`, { method: "POST" });
      fetchPage(page.id);
      setShowVersions(false);
    } catch { /* */ }
  };

  const statusCfg = page ? (STATUS_MAP[page.status] || STATUS_MAP.Draft) : STATUS_MAP.Draft;

  return (
    <div className="space-y-4">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-[var(--color-text-muted)]">
        <button onClick={() => router.push("/dashboard/wiki")} className="flex items-center gap-1 hover:text-[var(--color-text)] transition-colors">
          <ArrowLeft className="w-4 h-4" /> Wiki
        </button>
        <ChevronRight className="w-3 h-3" />
        <span className="text-[var(--color-text)] font-medium flex items-center gap-1.5">
          <span>{space?.iconEmoji || "📚"}</span> {space?.name || "..."}
        </span>
      </div>

      {/* Main Layout — sidebar tree + content */}
      <div className="flex gap-5 min-h-[70vh]">
        {/* Left — Page Tree */}
        <div className="w-72 shrink-0 rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-3 space-y-3 self-start sticky top-20">
          <div className="flex items-center justify-between px-1">
            <h3 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Sayfalar</h3>
            <button
              onClick={() => setShowCreate(true)}
              className="p-1 rounded-md hover:bg-white/10 text-[var(--color-text-muted)] hover:text-indigo-400 transition-colors"
              title="Yeni sayfa"
            >
              <Plus className="w-4 h-4" />
            </button>
          </div>
          <WikiPageTreeView
            spaceId={spaceId}
            selectedPageId={selectedPageId || undefined}
            onSelectPage={setSelectedPageId}
          />
        </div>

        {/* Right — Content */}
        <div className="flex-1 min-w-0">
          {/* Create Form */}
          {showCreate && (
            <div className="rounded-xl border border-indigo-500/30 bg-indigo-500/5 p-5 space-y-4 mb-5 animate-in slide-in-from-top-2">
              <h3 className="text-sm font-semibold text-[var(--color-text)]">
                Yeni Sayfa {selectedPageId ? "(alt sayfa)" : ""}
              </h3>
              <div>
                <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Başlık *</label>
                <input
                  value={newTitle}
                  onChange={(e) => setNewTitle(e.target.value)}
                  placeholder="Sayfa başlığı..."
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                />
              </div>
              <RichTextEditor
                content={newJson}
                onUpdate={(json, html) => { setNewJson(json); setNewHtml(html); }}
                placeholder="Sayfa içeriğini yazın..."
                minHeight="200px"
              />
              <div className="flex justify-end gap-2">
                <button onClick={() => { setShowCreate(false); setNewTitle(""); }} className="px-4 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-white/5 transition-colors">
                  İptal
                </button>
                <button onClick={createPage} disabled={creating || !newTitle.trim()} className="px-4 py-2 rounded-lg text-sm font-medium bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors flex items-center gap-2">
                  {creating && <Loader2 className="w-3.5 h-3.5 animate-spin" />} Oluştur
                </button>
              </div>
            </div>
          )}

          {/* Page Content */}
          {pageLoading ? (
            <div className="flex items-center justify-center py-20">
              <Loader2 className="w-5 h-5 text-indigo-400 animate-spin" />
            </div>
          ) : page ? (
            <div className="space-y-4">
              {/* Page Header */}
              <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
                {/* Breadcrumbs */}
                {page.breadcrumbs.length > 0 && (
                  <div className="flex items-center gap-1.5 text-xs text-[var(--color-text-muted)] mb-3">
                    {page.breadcrumbs.map((bc, i) => (
                      <span key={bc.id} className="flex items-center gap-1.5">
                        {i > 0 && <ChevronRight className="w-3 h-3" />}
                        <button onClick={() => setSelectedPageId(bc.id)} className="hover:text-indigo-400 transition-colors">
                          {bc.title}
                        </button>
                      </span>
                    ))}
                    <ChevronRight className="w-3 h-3" />
                  </div>
                )}

                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    {editing ? (
                      <input
                        value={editTitle}
                        onChange={(e) => setEditTitle(e.target.value)}
                        className="text-xl font-bold bg-transparent border-b-2 border-indigo-500/40 text-[var(--color-text)] focus:outline-none w-full pb-1"
                      />
                    ) : (
                      <h1 className="text-xl font-bold text-[var(--color-text)]">{page.title}</h1>
                    )}
                    <div className="flex items-center gap-3 mt-2 text-xs text-[var(--color-text-muted)]">
                      <span className={cn("inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border", statusCfg.color)}>
                        <statusCfg.icon className="w-3 h-3" /> {statusCfg.label}
                      </span>
                      <span className="flex items-center gap-1"><Eye className="w-3 h-3" /> {page.viewCount} görüntülenme</span>
                      <span className="flex items-center gap-1"><History className="w-3 h-3" /> v{page.versionCount}</span>
                      <span className="flex items-center gap-1"><Clock className="w-3 h-3" /> {new Date(page.updatedAt || page.createdAt).toLocaleDateString("tr-TR")}</span>
                      {page.lockedByUserId && (
                        <span className="flex items-center gap-1 text-amber-400"><Lock className="w-3 h-3" /> Kilitli</span>
                      )}
                      {page.sourceRequirementId && (
                        <span className="flex items-center gap-1 text-violet-400">📋 Gereksinimden</span>
                      )}
                      {page.sourceTicketId && (
                        <span className="flex items-center gap-1 text-cyan-400">🎫 Talepten</span>
                      )}
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-1.5 shrink-0">
                    {!editing ? (
                      <>
                        <button onClick={startEdit} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-white/5 transition-colors">
                          <Pencil className="w-3.5 h-3.5" /> Düzenle
                        </button>
                        {page.status === "Draft" && (
                          <button onClick={publishPage} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium bg-emerald-600 text-white hover:bg-emerald-700 transition-colors">
                            <Globe className="w-3.5 h-3.5" /> Yayınla
                          </button>
                        )}
                        {page.status === "Published" && (
                          <button onClick={archivePage} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium border border-amber-500/30 text-amber-400 hover:bg-amber-500/10 transition-colors">
                            <Archive className="w-3.5 h-3.5" /> Arşivle
                          </button>
                        )}
                        <button onClick={() => setShowVersions(!showVersions)} className={cn("p-1.5 rounded-lg transition-colors", showVersions ? "bg-indigo-500/20 text-indigo-400" : "text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-white/5")}>
                          <History className="w-4 h-4" />
                        </button>
                        <button onClick={deletePage} className="p-1.5 rounded-lg text-[var(--color-text-muted)] hover:text-red-400 hover:bg-red-500/10 transition-colors">
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </>
                    ) : (
                      <>
                        <input
                          value={changeNote}
                          onChange={(e) => setChangeNote(e.target.value)}
                          placeholder="Değişiklik notu..."
                          className="px-2.5 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] w-48 focus:outline-none focus:ring-1 focus:ring-indigo-500/40"
                        />
                        <button onClick={cancelEdit} className="px-3 py-1.5 rounded-lg text-xs font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-white/5 transition-colors">
                          İptal
                        </button>
                        <button onClick={savePage} disabled={saving} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors">
                          {saving ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Save className="w-3.5 h-3.5" />} Kaydet
                        </button>
                      </>
                    )}
                  </div>
                </div>
              </div>

              {/* Version History Panel */}
              {showVersions && (
                <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-4 space-y-3 animate-in slide-in-from-top-2">
                  <h3 className="text-sm font-semibold text-[var(--color-text)] flex items-center gap-2">
                    <History className="w-4 h-4 text-indigo-400" /> Versiyon Geçmişi
                  </h3>
                  {versionsLoading ? (
                    <div className="flex justify-center py-4"><Loader2 className="w-4 h-4 text-indigo-400 animate-spin" /></div>
                  ) : versions.length > 0 ? (
                    <div className="space-y-1.5 max-h-64 overflow-y-auto">
                      {versions.map((v) => (
                        <div key={v.id} className="flex items-center justify-between px-3 py-2 rounded-lg bg-white/5 hover:bg-white/10 transition-colors group">
                          <div className="flex items-center gap-3">
                            <span className="text-xs font-mono text-indigo-400 bg-indigo-500/10 px-2 py-0.5 rounded">v{v.versionNumber}</span>
                            <span className="text-xs text-[var(--color-text-muted)]">{v.changeNote || "—"}</span>
                            <span className="text-[10px] text-[var(--color-text-muted)]">{new Date(v.createdAt).toLocaleString("tr-TR")}</span>
                          </div>
                          {v.versionNumber !== versions[0]?.versionNumber && (
                            <button
                              onClick={() => revertToVersion(v.id)}
                              className="opacity-0 group-hover:opacity-100 flex items-center gap-1 px-2 py-1 rounded text-[10px] font-medium text-amber-400 hover:bg-amber-500/10 transition-all"
                            >
                              <RotateCcw className="w-3 h-3" /> Geri dön
                            </button>
                          )}
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-xs text-[var(--color-text-muted)] text-center py-4">Versiyon bulunamadı</p>
                  )}
                </div>
              )}

              {/* Editor / Viewer */}
              {editing ? (
                <RichTextEditor
                  content={editJson}
                  onUpdate={(json, html) => { setEditJson(json); setEditHtml(html); }}
                  placeholder="Sayfa içeriğini düzenleyin..."
                  minHeight="400px"
                  maxHeight="800px"
                />
              ) : (
                <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)]">
                  <RichTextViewer content={page.contentJson} />
                </div>
              )}
            </div>
          ) : !showCreate ? (
            <div className="flex flex-col items-center justify-center py-24 text-[var(--color-text-muted)]">
              <FileText className="w-16 h-16 opacity-15 mb-4" />
              <p className="text-sm mb-1">Sol menüden bir sayfa seçin</p>
              <p className="text-xs opacity-60">veya yeni bir sayfa oluşturun</p>
              <button
                onClick={() => setShowCreate(true)}
                className="mt-4 flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium bg-indigo-600 text-white hover:bg-indigo-700 transition-colors"
              >
                <Plus className="w-4 h-4" /> Yeni Sayfa
              </button>
            </div>
          ) : null}
        </div>
      </div>
    </div>
  );
}
