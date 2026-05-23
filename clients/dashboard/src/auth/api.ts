import { apiFetch } from "@/lib/api-client";

export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
};

export function issueToken(input: {
  email: string;
  password: string;
  tenant: string;
}) {
  return apiFetch<TokenResponse>("/api/v1/identity/token/issue", {
    method: "POST",
    body: JSON.stringify({ email: input.email, password: input.password }),
    // X-FSH-App tells the API which app shell is requesting the token.
    // Root-tenant logins identify as "admin" so the API boundary check
    // (tenant=root + X-FSH-App=dashboard → 403) doesn't fire while the
    // dedicated admin app is still under construction.
    headers: {
      tenant: input.tenant,
      "X-FSH-App": input.tenant.toLowerCase() === "root" ? "admin" : "dashboard",
    },
    skipAuth: true,
  });
}
