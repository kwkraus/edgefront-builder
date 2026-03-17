import type { Metadata } from "next";
import "./globals.css";
import Providers from "@/components/providers";
import AppHeader from "@/components/app-header";

export const metadata: Metadata = {
  title: "EdgeFront Builder",
  description: "Manage local webinar series, session imports, and analytics",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" suppressHydrationWarning data-color-mode="light" data-light-theme="light" data-dark-theme="dark">
      <head>
        {/* Apply saved color mode before first paint to avoid flash */}
        <script
          dangerouslySetInnerHTML={{
            __html: `(function(){try{var m=localStorage.getItem('color-mode');if(m==='dark'||m==='light'){document.documentElement.setAttribute('data-color-mode',m)}}catch(e){}})()`,
          }}
        />
      </head>
      <body>
        <Providers>
          <AppHeader />
          <main className="mx-auto max-w-6xl px-4 py-8">{children}</main>
        </Providers>
      </body>
    </html>
  );
}
