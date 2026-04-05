"use client";

import { useState, useEffect } from "react";

interface TenantStats {
  activeUsers: number;
  totalUsers: number;
  plan: string;
  storageUsed: string;
}

export default function ManageDashboard() {
  const [stats] = useState<TenantStats>({
    activeUsers: 12,
    totalUsers: 25,
    plan: "Professional",
    storageUsed: "2.4 GB / 10 GB",
  });

  const cards = [
    {
      label: "Aktif Kullanıcılar",
      value: `${stats.activeUsers} / ${stats.totalUsers}`,
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
        </svg>
      ),
      color: "text-teal-400",
      bg: "bg-teal-500/10",
    },
    {
      label: "Plan",
      value: stats.plan,
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09z" />
        </svg>
      ),
      color: "text-cyan-400",
      bg: "bg-cyan-500/10",
    },
    {
      label: "Depolama",
      value: stats.storageUsed,
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M20.25 6.375c0 2.278-3.694 4.125-8.25 4.125S3.75 8.653 3.75 6.375m16.5 0c0-2.278-3.694-4.125-8.25-4.125S3.75 4.097 3.75 6.375m16.5 0v11.25c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125V6.375" />
        </svg>
      ),
      color: "text-emerald-400",
      bg: "bg-emerald-500/10",
    },
    {
      label: "Roller",
      value: "4 aktif",
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
        </svg>
      ),
      color: "text-violet-400",
      bg: "bg-violet-500/10",
    },
  ];

  return (
    <div className="space-y-6">
      {/* Title */}
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">
          Tenant Yönetimi
        </h1>
        <p className="text-sm text-[var(--color-text-muted)] mt-1">
          Tenant&apos;ınızın kullanıcılarını, rollerini ve ayarlarını buradan yönetin.
        </p>
      </div>

      {/* Stat Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
        {cards.map((card) => (
          <div
            key={card.label}
            className="relative overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5 transition-all duration-200 hover:shadow-lg hover:shadow-teal-500/5"
          >
            <div className="flex items-start justify-between">
              <div>
                <p className="text-sm text-[var(--color-text-muted)]">
                  {card.label}
                </p>
                <p className="mt-2 text-2xl font-bold text-[var(--color-text)]">
                  {card.value}
                </p>
              </div>
              <div className={`${card.bg} p-2.5 rounded-xl ${card.color}`}>
                {card.icon}
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Quick Actions */}
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6">
        <h2 className="text-lg font-semibold text-[var(--color-text)] mb-4">
          Hızlı İşlemler
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <a
            href="/manage/users"
            className="flex items-center gap-3 p-4 rounded-lg border border-[var(--color-border)] hover:bg-teal-500/5 hover:border-teal-500/20 transition-all"
          >
            <div className="w-10 h-10 rounded-lg bg-teal-500/10 flex items-center justify-center text-teal-400">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M19 7.5v3m0 0v3m0-3h3m-3 0h-3m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM4 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 0110.374 21c-2.331 0-4.512-.645-6.374-1.766z" />
              </svg>
            </div>
            <div>
              <p className="text-sm font-medium text-[var(--color-text)]">
                Kullanıcı Ekle
              </p>
              <p className="text-xs text-[var(--color-text-muted)]">
                Yeni kullanıcı oluştur
              </p>
            </div>
          </a>
          <a
            href="/manage/roles"
            className="flex items-center gap-3 p-4 rounded-lg border border-[var(--color-border)] hover:bg-teal-500/5 hover:border-teal-500/20 transition-all"
          >
            <div className="w-10 h-10 rounded-lg bg-cyan-500/10 flex items-center justify-center text-cyan-400">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
              </svg>
            </div>
            <div>
              <p className="text-sm font-medium text-[var(--color-text)]">
                Rol Yönetimi
              </p>
              <p className="text-xs text-[var(--color-text-muted)]">
                Rolleri düzenle
              </p>
            </div>
          </a>
          <a
            href="/manage/settings"
            className="flex items-center gap-3 p-4 rounded-lg border border-[var(--color-border)] hover:bg-teal-500/5 hover:border-teal-500/20 transition-all"
          >
            <div className="w-10 h-10 rounded-lg bg-emerald-500/10 flex items-center justify-center text-emerald-400">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87a6.52 6.52 0 01.22.128c.331.183.581.495.644.869l.214 1.281" />
              </svg>
            </div>
            <div>
              <p className="text-sm font-medium text-[var(--color-text)]">
                Tenant Ayarları
              </p>
              <p className="text-xs text-[var(--color-text-muted)]">
                Konfigürasyon
              </p>
            </div>
          </a>
        </div>
      </div>
    </div>
  );
}
