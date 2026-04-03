"use client";

import { Layers } from "lucide-react";

export default function LoginPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-[var(--color-bg)]">
      <div className="w-full max-w-sm mx-4">
        {/* Logo */}
        <div className="flex flex-col items-center mb-8">
          <div className="w-14 h-14 rounded-2xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center shadow-xl shadow-indigo-500/30 mb-4">
            <Layers className="w-7 h-7 text-white" />
          </div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">EntApp Framework</h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">Yönetim paneline giriş yapın</p>
        </div>

        {/* Login Card */}
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6 shadow-xl">
          <form action="/api/auth/signin/keycloak" method="POST">
            <button
              type="submit"
              className="w-full flex items-center justify-center gap-3 h-11 rounded-lg
                         bg-indigo-500 hover:bg-indigo-600 text-white font-medium text-sm
                         transition-all duration-200 shadow-md shadow-indigo-500/20 
                         hover:shadow-lg hover:shadow-indigo-500/30"
            >
              <svg className="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z"/>
              </svg>
              Keycloak ile Giriş Yap
            </button>
          </form>

          <div className="mt-4 text-center">
            <p className="text-xs text-[var(--color-text-muted)]">
              Keycloak SSO ile güvenli oturum açma
            </p>
          </div>
        </div>

        <p className="text-center text-xs text-[var(--color-text-muted)] mt-6">
          © 2026 EntApp Framework. Tüm hakları saklıdır.
        </p>
      </div>
    </div>
  );
}
