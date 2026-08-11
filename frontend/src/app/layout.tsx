import type { Metadata, Viewport } from "next";
import { Fira_Sans, Fira_Code } from "next/font/google";
import { AuthProvider } from "@/lib/auth/AuthContext";
import { AppNav } from "@/components/layout/AppNav";
import "./globals.css";

/** Fira Sans for UI text, Fira Code for marks and identifiers — a dashboard pairing. */
const firaSans = Fira_Sans({
  variable: "--font-fira-sans",
  weight: ["300", "400", "500", "600", "700"],
  subsets: ["latin"],
  display: "swap",
});

const firaCode = Fira_Code({
  variable: "--font-fira-code",
  weight: ["400", "500"],
  subsets: ["latin"],
  display: "swap",
});

export const metadata: Metadata = {
  title: "Assignment & Submission Management System",
  description: "Role-based assignment and submission management for schools/colleges.",
};

/** `themeColor` per scheme keeps the mobile browser chrome from fighting the dark theme. */
export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#f0fdfa" },
    { media: "(prefers-color-scheme: dark)", color: "#071a19" },
  ],
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      className={`${firaSans.variable} ${firaCode.variable} h-full antialiased`}
    >
      <body className="flex min-h-full flex-col bg-background text-foreground">
        <AuthProvider>
          <a
            href="#main-content"
            className="sr-only rounded-lg bg-primary px-4 py-2 text-sm font-medium text-on-primary focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-50"
          >
            Skip to content
          </a>
          <AppNav />
          {children}
        </AuthProvider>
      </body>
    </html>
  );
}
