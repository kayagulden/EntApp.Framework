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
        source: "/api/v:version/:path*",
        destination: "http://localhost:5212/api/v:version/:path*",
      },
    ];
  },
};

export default nextConfig;
