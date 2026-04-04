import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: "/hubs/:path*",
        destination: "http://localhost:5212/hubs/:path*",
      },
      {
        source: "/api/v:version/:path*",
        destination: "http://localhost:5212/api/v:version/:path*",
      },
    ];
  },
};

export default nextConfig;
