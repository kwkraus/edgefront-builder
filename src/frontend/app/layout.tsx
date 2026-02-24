import type { Metadata } from "next";
import "./globals.css";
import Providers from "@/components/providers";
import AppHeader from "@/components/app-header";

export const metadata: Metadata = {
  title: "EdgeFront Builder",
  description: "Manage webinar series and sessions with Teams integration",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="antialiased font-sans">
        <Providers>
          <AppHeader />
          <main className="mx-auto max-w-6xl px-4 py-8">{children}</main>
        </Providers>
      </body>
    </html>
  );
}
