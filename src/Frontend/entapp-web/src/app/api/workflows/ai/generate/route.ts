import { NextRequest, NextResponse } from "next/server";

export const maxDuration = 120; // Vercel/Next.js function timeout: 2 dakika

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 90_000); // 90s timeout

    const res = await fetch("http://localhost:5212/api/workflows/ai/generate", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
      signal: controller.signal,
    });

    clearTimeout(timeout);

    const data = await res.json();
    return NextResponse.json(data, { status: res.status });
  } catch (err) {
    const message =
      err instanceof Error ? err.message : "Workflow oluşturulamadı";
    return NextResponse.json(
      { error: message },
      { status: 500 }
    );
  }
}
