import path from "node:path";
import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Emits .next/standalone with a self-contained server and only the node_modules it
  // actually uses, so the Docker runtime stage does not need a second npm install.
  output: "standalone",

  // Pin the workspace root to this app so Turbopack does not walk up and pick
  // up an unrelated lockfile outside the Git repository.
  turbopack: {
    root: path.resolve(__dirname),
  },
};

export default nextConfig;
