"use client";

import { Settings, Globe, Bell, Palette } from "lucide-react";
import { useTheme } from "next-themes";
import { cn } from "@/lib/utils";

export default function SettingsPage() {
  const { theme, setTheme } = useTheme();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">Ayarlar</h1>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">Uygulama ayarlarını yapılandırın.</p>
      </div>

      <div className="space-y-4 max-w-2xl">
        {/* Theme */}
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
          <div className="flex items-center gap-3 mb-4">
            <Palette className="w-5 h-5 text-indigo-500" />
            <h3 className="font-semibold text-[var(--color-text)]">Tema</h3>
          </div>
          <div className="flex gap-3">
            {(["light", "dark", "system"] as const).map((t) => (
              <button
                key={t}
                onClick={() => setTheme(t)}
                className={cn(
                  "px-4 py-2 rounded-lg text-sm font-medium transition-all",
                  theme === t
                    ? "bg-indigo-500 text-white shadow-md shadow-indigo-500/20"
                    : "bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)] hover:bg-[var(--color-border)]"
                )}
              >
                {t === "light" ? "Açık" : t === "dark" ? "Koyu" : "Sistem"}
              </button>
            ))}
          </div>
        </div>

        {/* Language */}
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
          <div className="flex items-center gap-3 mb-4">
            <Globe className="w-5 h-5 text-emerald-500" />
            <h3 className="font-semibold text-[var(--color-text)]">Dil</h3>
          </div>
          <select className="px-4 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] text-sm text-[var(--color-text)] focus:ring-2 focus:ring-indigo-500/30 focus:outline-none">
            <option>Türkçe</option>
            <option>English</option>
          </select>
        </div>

        {/* Notifications */}
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
          <div className="flex items-center gap-3 mb-4">
            <Bell className="w-5 h-5 text-amber-500" />
            <h3 className="font-semibold text-[var(--color-text)]">Bildirimler</h3>
          </div>
          <label className="flex items-center gap-3 cursor-pointer">
            <div className="relative">
              <input type="checkbox" defaultChecked className="sr-only peer" />
              <div className="w-11 h-6 bg-[var(--color-border)] rounded-full peer peer-checked:bg-indigo-500 transition-colors" />
              <div className="absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow peer-checked:translate-x-5 transition-transform" />
            </div>
            <span className="text-sm text-[var(--color-text)]">E-posta bildirimleri</span>
          </label>
        </div>
      </div>
    </div>
  );
}
