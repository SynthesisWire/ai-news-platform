import { NextResponse, type NextRequest } from "next/server";
import { defaultLocale, isLocale } from "./lib/i18n";

export function proxy(request: NextRequest) {
  const candidate = request.nextUrl.pathname.split("/")[1];
  const headers = new Headers(request.headers);
  // Always overwrite incoming values. This request-only header sets SSR document language.
  headers.set("x-site-locale", isLocale(candidate) ? candidate : defaultLocale);
  return NextResponse.next({ request: { headers } });
}
export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico|icon.svg).*)"],
};
