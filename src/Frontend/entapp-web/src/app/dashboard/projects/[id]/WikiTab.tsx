"use client";

import { useState, useCallback, useEffect } from "react";
import dynamic from "next/dynamic";
import { WikiSpaceList } from "@/components/wiki/WikiComponents";
import { BookOpen, Plus, Search, Loader2 } from "lucide-react";

const RichTextViewer = dynamic(
  () => import("@/components/shared/RichTextEditor").then(m => m.RichTextViewer),
  { ssr: false }
);

// ── Types ────────────────────────────────────────────
interface WikiSearchResult {
  id: string;
  title: string;
  slug: string;
  spaceName: string;
  spaceSlug: string;
  excerpt: string;
  status: string;
  updatedAt: string;
}

// ── WikiTab (Project Dashboard) ─────────────────────
export default function WikiTab({ projectId }: { projectId: string }) {
  const [searchTerm, setSearchTerm] = useState("");
  const [searchResults, setSearchResults] = useState<WikiSearchResult[]>([]);
  const [searching, setSearching] = useState(false);

  const search = useCallback(async () => {
    if (!searchTerm.trim()) { setSearchResults([]); return; }
    setSearching(true);
    try {
      const params = new URLSearchParams({ q: searchTerm, projectId });
      const res = await fetch(`/api/v1/wiki/search?${params}`);
      if (res.ok) {
        const data = await res.json();
        setSearchResults(data.items || []);
      }
    } catch { /* */ }
    finally { setSearching(false); }
  }, [searchTerm, projectId]);

  useEffect(() => {
    const timer = setTimeout(search, 300);
    return () => clearTimeout(timer);
  }, [search]);

  return (
    <div className="space-y-5">
      {/* Search within project wiki */}
      <div className="relative max-w-lg">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
        <input
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          placeholder="Wiki sayfalarında ara..."
          className="w-full pl-10 pr-4 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
        />
        {searching && <Loader2 className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-indigo-400 animate-spin" />}
      </div>

      {/* Search Results */}
      {searchTerm && searchResults.length > 0 && (
        <div className="space-y-2">
          <h3 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase">Arama Sonuçları ({searchResults.length})</h3>
          <div className="space-y-2">
            {searchResults.map((r) => (
              <a
                key={r.id}
                href={`/dashboard/wiki/${r.spaceSlug}?page=${r.id}`}
                className="block rounded-lg border border-[var(--color-border)] bg-[var(--color-card-bg)] p-4 hover:border-indigo-500/40 transition-all"
              >
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-xs text-indigo-400 bg-indigo-500/10 px-2 py-0.5 rounded">{r.spaceName}</span>
                  <span className="text-xs text-[var(--color-text-muted)]">{new Date(r.updatedAt).toLocaleDateString("tr-TR")}</span>
                </div>
                <h4 className="text-sm font-medium text-[var(--color-text)]">{r.title}</h4>
                <p className="text-xs text-[var(--color-text-muted)] mt-1 line-clamp-2"
                  dangerouslySetInnerHTML={{ __html: r.excerpt }}
                />
              </a>
            ))}
          </div>
        </div>
      )}

      {searchTerm && searchResults.length === 0 && !searching && (
        <div className="text-center py-8 text-[var(--color-text-muted)]">
          <Search className="w-8 h-8 mx-auto opacity-20 mb-2" />
          <p className="text-xs">Sonuç bulunamadı</p>
        </div>
      )}

      {/* Space List for this project */}
      {!searchTerm && <WikiSpaceList projectId={projectId} />}
    </div>
  );
}
