"use client";

import {
  Users,
  Shield,
  Building2,
  Activity,
  TrendingUp,
  Clock,
} from "lucide-react";
import { cn } from "@/lib/utils";

const stats = [
  {
    name: "Toplam Kullanıcı",
    value: "2,847",
    change: "+12.5%",
    trend: "up",
    icon: Users,
    color: "from-blue-500 to-cyan-500",
  },
  {
    name: "Aktif Oturum",
    value: "432",
    change: "+8.2%",
    trend: "up",
    icon: Activity,
    color: "from-emerald-500 to-green-500",
  },
  {
    name: "Tanımlı Rol",
    value: "12",
    change: "+2",
    trend: "up",
    icon: Shield,
    color: "from-violet-500 to-purple-500",
  },
  {
    name: "Organizasyon",
    value: "8",
    change: "0",
    trend: "neutral",
    icon: Building2,
    color: "from-amber-500 to-orange-500",
  },
];

const recentActivity = [
  {
    action: "Yeni kullanıcı oluşturuldu",
    user: "admin",
    target: "ahmet.yilmaz@company.com",
    time: "2 dakika önce",
  },
  {
    action: "Rol atandı",
    user: "admin",
    target: "Manager → mehmet.demir",
    time: "15 dakika önce",
  },
  {
    action: "Kullanıcı deaktif edildi",
    user: "admin",
    target: "eski.calisan@company.com",
    time: "1 saat önce",
  },
  {
    action: "Organizasyon oluşturuldu",
    user: "admin",
    target: "İstanbul Şubesi (IST)",
    time: "3 saat önce",
  },
  {
    action: "Yeni rol oluşturuldu",
    user: "admin",
    target: "Departman Yöneticisi",
    time: "1 gün önce",
  },
];

export default function DashboardPage() {
  return (
    <div className="space-y-8">
      {/* ── Page Title ────────────────────────────── */}
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">
          Dashboard
        </h1>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          EntApp Framework yönetim panelinize hoş geldiniz.
        </p>
      </div>

      {/* ── Stats Grid ────────────────────────────── */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        {stats.map((stat, index) => (
          <div
            key={stat.name}
            className={cn(
              "relative overflow-hidden rounded-xl p-5",
              "bg-[var(--color-surface)] border border-[var(--color-border)]",
              "hover:border-[var(--color-border-hover)] hover:shadow-lg",
              "transition-all duration-300 group animate-fade-in"
            )}
            style={{ animationDelay: `${index * 75}ms` }}
          >
            {/* Gradient orb */}
            <div
              className={cn(
                "absolute -right-4 -top-4 w-20 h-20 rounded-full opacity-10",
                "group-hover:opacity-20 transition-opacity duration-500",
                `bg-gradient-to-br ${stat.color}`
              )}
            />

            <div className="flex items-center justify-between relative">
              <div>
                <p className="text-xs font-medium uppercase tracking-wider text-[var(--color-text-muted)]">
                  {stat.name}
                </p>
                <p className="mt-2 text-3xl font-bold text-[var(--color-text)]">
                  {stat.value}
                </p>
              </div>
              <div
                className={cn(
                  "flex items-center justify-center w-11 h-11 rounded-xl",
                  `bg-gradient-to-br ${stat.color} shadow-lg`
                )}
              >
                <stat.icon className="w-5 h-5 text-white" />
              </div>
            </div>

            <div className="mt-3 flex items-center gap-1">
              <TrendingUp
                className={cn(
                  "w-3 h-3",
                  stat.trend === "up"
                    ? "text-emerald-500"
                    : "text-[var(--color-text-muted)]"
                )}
              />
              <span
                className={cn(
                  "text-xs font-medium",
                  stat.trend === "up"
                    ? "text-emerald-500"
                    : "text-[var(--color-text-muted)]"
                )}
              >
                {stat.change}
              </span>
              <span className="text-xs text-[var(--color-text-muted)]">
                son 30 gün
              </span>
            </div>
          </div>
        ))}
      </div>

      {/* ── Recent Activity ───────────────────────── */}
      <div
        className={cn(
          "rounded-xl overflow-hidden",
          "bg-[var(--color-surface)] border border-[var(--color-border)]"
        )}
      >
        <div className="px-6 py-4 border-b border-[var(--color-border)]">
          <h2 className="text-base font-semibold text-[var(--color-text)]">
            Son Aktiviteler
          </h2>
        </div>
        <div className="divide-y divide-[var(--color-border)]">
          {recentActivity.map((activity, index) => (
            <div
              key={index}
              className="flex items-center justify-between px-6 py-3.5 hover:bg-[var(--color-surface-hover)] transition-colors"
            >
              <div className="flex items-center gap-3 min-w-0">
                <div className="w-2 h-2 rounded-full bg-indigo-500 shrink-0" />
                <div className="min-w-0">
                  <p className="text-sm text-[var(--color-text)] truncate">
                    <span className="font-medium">{activity.action}</span>
                    {" — "}
                    <span className="text-[var(--color-text-secondary)]">
                      {activity.target}
                    </span>
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-1.5 text-xs text-[var(--color-text-muted)] shrink-0 ml-4">
                <Clock className="w-3 h-3" />
                {activity.time}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
