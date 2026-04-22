import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: "/hubs/:path*",
        destination: "http://localhost:5212/hubs/:path*",
      },
      {
        source: "/api/admin/:path*",
        destination: "http://localhost:5212/api/admin/:path*",
      },
      {
        // AI workflow routes handled by API route handlers (longer timeout)
        source: "/api/workflows/ai/:path*",
        destination: "/api/workflows/ai/:path*",
      },
      {
        source: "/api/workflows/:path*",
        destination: "http://localhost:5212/api/workflows/:path*",
      },
      {
        source: "/api/ai/:path*",
        destination: "http://localhost:5212/api/ai/:path*",
      },
      {
        source: "/api/req/:path*",
        destination: "http://localhost:5212/api/req/:path*",
      },
      {
        source: "/api/pm/:path*",
        destination: "http://localhost:5212/api/pm/:path*",
      },
      {
        source: "/api/sf/:path*",
        destination: "http://localhost:5212/api/sf/:path*",
      },
      {
        source: "/api/wf/:path*",
        destination: "http://localhost:5212/api/wf/:path*",
      },
      {
        source: "/api/v:version/:path*",
        destination: "http://localhost:5212/api/v:version/:path*",
      },
    ];
  },
};

export default nextConfig;
