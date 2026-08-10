import path from "node:path";
import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Pin the workspace root to this app so Turbopack does not walk up and pick
  // up an unrelated lockfile outside the Git repository.
  turbopack: {
    root: path.resolve(__dirname),
  },
};

export default nextConfig;
