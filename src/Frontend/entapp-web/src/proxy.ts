import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

/**
 * Development proxy — Keycloak yokken auth bypass.
 * Production'da next-auth middleware kullanılacak.
 *
 * Next.js 16'da middleware.ts → proxy.ts konvansiyonuna geçildi.
 * Bu dosya ağ seviyesinde routing, redirect ve rewrite işlemleri içindir.
 * Karmaşık auth/business logic Server Component veya Route Handler'larda yapılmalıdır.
 */
export function proxy(req: NextRequest) {
  const isLoginPage = req.nextUrl.pathname === "/login";
  const isApiRoute = req.nextUrl.pathname.startsWith("/api");
  const isStaticAsset = req.nextUrl.pathname.startsWith("/_next");

  // API ve static asset'leri atla
  if (isApiRoute || isStaticAsset) return NextResponse.next();

  // Dev mode — cookie ile basit auth kontrolü
  const isDevAuth = req.cookies.get("dev-auth")?.value === "true";

  // Login sayfasında ve auth varsa → dashboard'a yönlendir
  if (isLoginPage && isDevAuth) {
    return NextResponse.redirect(new URL("/dashboard", req.url));
  }

  // Korunan sayfalarda ve auth yoksa → login'e yönlendir
  if (!isDevAuth && !isLoginPage) {
    return NextResponse.redirect(new URL("/login", req.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
