"use client";

import { Sidebar } from "@/components/sidebar";
import { Header } from "@/components/header";
import { useUiStore } from "@/stores";
import { cn } from "@/lib/utils";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { sidebarCollapsed } = useUiStore();

  return (
    <div className="min-h-screen bg-[var(--color-bg)]">
      <Sidebar />
      <Header />

      <main
        className={cn(
          "pt-16 min-h-screen transition-all duration-300",
          sidebarCollapsed ? "ml-[68px]" : "ml-64"
        )}
      >
        <div className="p-6 animate-fade-in">{children}</div>
      </main>
    </div>
  );
}
