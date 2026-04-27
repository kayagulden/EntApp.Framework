"use client";

import { WikiSpaceList } from "@/components/wiki/WikiComponents";

export default function WikiPage() {
  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">Bilgi Bankası</h1>
        <p className="text-sm text-[var(--color-text-muted)] mt-1">
          Kurumsal wiki alanları ve dokümantasyon
        </p>
      </div>

      {/* Space List */}
      <WikiSpaceList />
    </div>
  );
}
