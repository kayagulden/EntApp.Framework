"use client";

import { Layers } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("admin@entapp.dev");
  const [password, setPassword] = useState("admin123");
  const [loading, setLoading] = useState(false);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    // Dev mode — cookie set edip dashboard'a yönlendir
    document.cookie = "dev-auth=true; path=/; max-age=86400";
    
    // Kısa bir bekleme ile UX hissi
    await new Promise(r => setTimeout(r, 500));
    router.push("/dashboard");
  };

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
          <form onSubmit={handleLogin} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">
                E-posta
              </label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full h-10 px-3 rounded-lg border border-[var(--color-border)] 
                           bg-[var(--color-bg)] text-[var(--color-text)] text-sm
                           focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-indigo-500
                           transition-all duration-200"
                placeholder="admin@entapp.dev"
              />
            </div>
            
            <div>
              <label className="block text-sm font-medium text-[var(--color-text)] mb-1.5">
                Şifre
              </label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full h-10 px-3 rounded-lg border border-[var(--color-border)] 
                           bg-[var(--color-bg)] text-[var(--color-text)] text-sm
                           focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-indigo-500
                           transition-all duration-200"
                placeholder="••••••••"
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full flex items-center justify-center gap-2 h-11 rounded-lg
                         bg-indigo-500 hover:bg-indigo-600 text-white font-medium text-sm
                         transition-all duration-200 shadow-md shadow-indigo-500/20 
                         hover:shadow-lg hover:shadow-indigo-500/30 disabled:opacity-60"
            >
              {loading ? (
                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                "Giriş Yap"
              )}
            </button>
          </form>

          <div className="mt-4 text-center">
            <p className="text-xs text-[var(--color-text-muted)]">
              Dev Mode — herhangi bir bilgi ile giriş yapabilirsiniz
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
