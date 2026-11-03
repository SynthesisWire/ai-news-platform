import type { Metadata } from "next";
import { headers } from "next/headers";
import { defaultLocale, isLocale } from "@/lib/i18n";
import { brandName, brandTitle } from "@/lib/brand";
import { siteOrigin } from "@/lib/urls";
import "./globals.css";

export const metadata: Metadata = {
  metadataBase: new URL(siteOrigin()),
  title: {
    default: brandTitle,
    template: `%s · ${brandName}`,
  },
  description: "AI research through claims, evidence, sources and limitations.",
};
export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const candidate = (await headers()).get("x-site-locale") || defaultLocale;
  const locale = isLocale(candidate) ? candidate : defaultLocale;
  return (
    <html lang={locale}>
      <body>
        <a className="skip-link" href="#main">
          {locale === "en" ? "Skip to content" : "Pular para o conteúdo"}
        </a>
        {children}
      </body>
    </html>
  );
}
