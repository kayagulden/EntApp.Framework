"use client";

import { useState, useCallback, useEffect } from "react";
import { useRouter } from "next/navigation";
import {
  BookOpen, Plus, Search, ChevronRight, ChevronDown,
  FileText, Loader2, FolderOpen, Globe, Eye, Archive,
  MoreHorizontal, Pencil, Trash2, ArrowUpDown, ExternalLink,
} from "lucide-react";
import { cn } from "@/lib/utils";

// ── Types ────────────────────────────────────────────
interface WikiSpace {
  id: string;
  name: string;
  slug: string;
  description?: string;
  projectId?: string;
  iconEmoji?: string;
  pageCount: number;
  isActive: boolean;
}

interface WikiPageTree {
  id: string;
  title: string;
  slug: string;
  status: string;
  parentPageId?: string;
  sortOrder: number;
  childCount: number;
  children: WikiPageTree[];
}

// ── Status Badges ───────────────────────────────────
const PAGE_STATUS: Record<string, { label: string; color: string; icon: React.ComponentType<{ className?: string }> }> = {
  Draft: { label: "Taslak", color: "bg-amber-500/20 text-amber-400", icon: Pencil },
  Published: { label: "Yayında", color: "bg-emerald-500/20 text-emerald-400", icon: Globe },
  Archived: { label: "Arşiv", color: "bg-slate-500/20 text-slate-400", icon: Archive },
};

// ── WikiSpaceList ───────────────────────────────────
export function WikiSpaceList({ projectId }: { projectId?: string }) {
  const router = useRouter();
  const [spaces, setSpaces] = useState<WikiSpace[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [newSpace, setNewSpace] = useState({ name: "", slug: "", description: "", iconEmoji: "📚" });
  const [creating, setCreating] = useState(false);

  const fetchSpaces = useCallback(async () => {
    setLoading(true);
    try {
      const params = projectId ? `?projectId=${projectId}` : "";
      const res = await fetch(`/api/v1/wiki/spaces${params}`);
      if (res.ok) setSpaces(await res.json());
    } catch { /* */ }
    finally { setLoading(false); }
  }, [projectId]);

  useEffect(() => { fetchSpaces(); }, [fetchSpaces]);

  const createSpace = async () => {
    if (!newSpace.name.trim()) return;
    setCreating(true);
    try {
      const slug = newSpace.slug || newSpace.name.toLowerCase()
        .replace(/ş/g, "s").replace(/ç/g, "c").replace(/ğ/g, "g")
        .replace(/ü/g, "u").replace(/ö/g, "o").replace(/ı/g, "i")
        .replace(/[^a-z0-9\s-]/g, "").replace(/\s+/g, "-").replace(/-+/g, "-").trim();
      const res = await fetch("/api/v1/wiki/spaces", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ...newSpace, slug, projectId }),
      });
      if (res.ok) {
        setShowCreate(false);
        setNewSpace({ name: "", slug: "", description: "", iconEmoji: "📚" });
        fetchSpaces();
      }
    } catch { /* */ }
    finally { setCreating(false); }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-16">
        <Loader2 className="w-5 h-5 text-indigo-400 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-emerald-500 to-teal-600 flex items-center justify-center shadow-lg shadow-emerald-500/20">
            <BookOpen className="w-5 h-5 text-white" />
          </div>
          <div>
            <h2 className="text-lg font-bold text-[var(--color-text)]">Wiki Alanları</h2>
            <p className="text-xs text-[var(--color-text-muted)]">{spaces.length} alan</p>
          </div>
        </div>
        <button
          onClick={() => setShowCreate(true)}
          className="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium bg-indigo-600 text-white hover:bg-indigo-700 transition-colors shadow-lg shadow-indigo-500/20"
        >
          <Plus className="w-4 h-4" /> Yeni Alan
        </button>
      </div>

      {/* Create form */}
      {showCreate && (
        <div className="rounded-xl border border-indigo-500/30 bg-indigo-500/5 p-5 space-y-3 animate-in slide-in-from-top-2">
          <h3 className="text-sm font-semibold text-[var(--color-text)]">Yeni Wiki Alanı</h3>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Alan Adı *</label>
              <input
                value={newSpace.name}
                onChange={(e) => setNewSpace(s => ({ ...s, name: e.target.value }))}
                placeholder="Örn: Teknik Dokümantasyon"
                className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Emoji</label>
              <input
                value={newSpace.iconEmoji}
                onChange={(e) => setNewSpace(s => ({ ...s, iconEmoji: e.target.value }))}
                className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
              />
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Açıklama</label>
            <textarea
              value={newSpace.description}
              onChange={(e) => setNewSpace(s => ({ ...s, description: e.target.value }))}
              rows={2}
              className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40 resize-none"
            />
          </div>
          <div className="flex justify-end gap-2">
            <button onClick={() => setShowCreate(false)} className="px-4 py-2 rounded-lg text-sm font-medium border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-white/5 transition-colors">
              İptal
            </button>
            <button onClick={createSpace} disabled={creating || !newSpace.name.trim()} className="px-4 py-2 rounded-lg text-sm font-medium bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors flex items-center gap-2">
              {creating && <Loader2 className="w-3.5 h-3.5 animate-spin" />} Oluştur
            </button>
          </div>
        </div>
      )}

      {/* Space Cards */}
      {spaces.length === 0 && !showCreate ? (
        <div className="flex flex-col items-center justify-center py-16 text-[var(--color-text-muted)]">
          <BookOpen className="w-12 h-12 opacity-20 mb-3" />
          <p className="text-sm">Henüz wiki alanı oluşturulmamış</p>
          <button onClick={() => setShowCreate(true)} className="mt-3 text-xs text-indigo-400 hover:text-indigo-300 transition-colors">
            İlk alanı oluştur →
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {spaces.map((space) => (
            <button
              key={space.id}
              onClick={() => router.push(`/dashboard/wiki/${space.id}`)}
              className="group text-left rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5 hover:border-indigo-500/40 hover:shadow-lg hover:shadow-indigo-500/5 transition-all duration-200"
            >
              <div className="flex items-start justify-between mb-3">
                <span className="text-2xl">{space.iconEmoji || "📚"}</span>
                <span className="text-xs text-[var(--color-text-muted)] bg-white/5 px-2 py-1 rounded-full">
                  {space.pageCount} sayfa
                </span>
              </div>
              <h3 className="text-sm font-semibold text-[var(--color-text)] group-hover:text-indigo-400 transition-colors mb-1">
                {space.name}
              </h3>
              {space.description && (
                <p className="text-xs text-[var(--color-text-muted)] line-clamp-2">{space.description}</p>
              )}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

// ── WikiPageTree ────────────────────────────────────
export function WikiPageTreeView({
  spaceId,
  selectedPageId,
  onSelectPage,
}: {
  spaceId: string;
  selectedPageId?: string;
  onSelectPage: (pageId: string) => void;
}) {
  const [tree, setTree] = useState<WikiPageTree[]>([]);
  const [loading, setLoading] = useState(true);
  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const [searchTerm, setSearchTerm] = useState("");

  const fetchTree = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetch(`/api/v1/wiki/spaces/${spaceId}/tree`);
      if (res.ok) {
        const data = await res.json();
        setTree(Array.isArray(data) ? data : []);
        // Auto-expand first level
        if (data?.length) {
          setExpanded(new Set(data.map((p: WikiPageTree) => p.id)));
        }
      }
    } catch { /* */ }
    finally { setLoading(false); }
  }, [spaceId]);

  useEffect(() => { fetchTree(); }, [fetchTree]);

  const toggleExpand = (id: string) => {
    setExpanded(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  };

  const flatFilter = useCallback((pages: WikiPageTree[]): WikiPageTree[] => {
    if (!searchTerm) return pages;
    const term = searchTerm.toLowerCase();
    return pages.reduce<WikiPageTree[]>((acc, page) => {
      const matchSelf = page.title.toLowerCase().includes(term);
      const matchedChildren = flatFilter(page.children);
      if (matchSelf || matchedChildren.length > 0) {
        acc.push({ ...page, children: matchedChildren });
      }
      return acc;
    }, []);
  }, [searchTerm]);

  const filteredTree = flatFilter(tree);

  const renderNode = (node: WikiPageTree, depth: number = 0) => {
    const hasChildren = node.children.length > 0;
    const isExpanded = expanded.has(node.id);
    const isSelected = node.id === selectedPageId;
    const statusCfg = PAGE_STATUS[node.status] || PAGE_STATUS.Draft;

    return (
      <div key={node.id}>
        <button
          onClick={() => onSelectPage(node.id)}
          className={cn(
            "w-full flex items-center gap-2 px-2 py-1.5 rounded-lg text-sm transition-all duration-150 group",
            isSelected
              ? "bg-indigo-500/15 text-indigo-400 border border-indigo-500/30"
              : "text-[var(--color-text-muted)] hover:bg-white/5 hover:text-[var(--color-text)] border border-transparent",
          )}
          style={{ paddingLeft: `${depth * 16 + 8}px` }}
        >
          {hasChildren ? (
            <button
              onClick={(e) => { e.stopPropagation(); toggleExpand(node.id); }}
              className="p-0.5 rounded hover:bg-white/10 transition-colors shrink-0"
            >
              {isExpanded ? <ChevronDown className="w-3.5 h-3.5" /> : <ChevronRight className="w-3.5 h-3.5" />}
            </button>
          ) : (
            <FileText className="w-3.5 h-3.5 shrink-0 opacity-40" />
          )}
          <span className="truncate flex-1 text-left">{node.title}</span>
          <span className={cn("text-[9px] px-1.5 py-0.5 rounded-full shrink-0 opacity-0 group-hover:opacity-100 transition-opacity", statusCfg.color)}>
            {statusCfg.label}
          </span>
        </button>
        {hasChildren && isExpanded && (
          <div className="animate-in slide-in-from-top-1 duration-150">
            {node.children.map(child => renderNode(child, depth + 1))}
          </div>
        )}
      </div>
    );
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="w-4 h-4 text-indigo-400 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {/* Search */}
      <div className="relative">
        <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-[var(--color-text-muted)]" />
        <input
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          placeholder="Sayfa ara..."
          className="w-full pl-8 pr-3 py-1.5 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-1 focus:ring-indigo-500/40"
        />
      </div>

      {/* Tree */}
      <div className="space-y-0.5">
        {filteredTree.length > 0 ? (
          filteredTree.map(node => renderNode(node))
        ) : (
          <div className="text-center py-8 text-[var(--color-text-muted)]">
            <FileText className="w-8 h-8 mx-auto opacity-20 mb-2" />
            <p className="text-xs">{searchTerm ? "Sonuç bulunamadı" : "Henüz sayfa yok"}</p>
          </div>
        )}
      </div>
    </div>
  );
}

export default WikiSpaceList;
