import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { AlertCircle, ClipboardCheck, Copy, KeyRound, LogOut, MonitorSmartphone, ShieldCheck, ShieldOff, Smartphone, X } from "lucide-react";
import QRCode from "qrcode";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Field, FormActions } from "@/components/list";
import { CancelIcon } from "@/components/ui/icons";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  changePassword,
  disableTwoFactor,
  enrollTwoFactor,
  getMyProfile,
  verifyEnrollTwoFactor,
  type TwoFactorEnrollmentResponse,
} from "@/api/identity";
import {
  getMySessions,
  revokeAllOtherSessions,
  revokeSession,
  type UserSessionDto,
} from "@/api/sessions";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const PROFILE_KEY = ["identity", "me"] as const;

function formatTimestamp(iso: string | null | undefined, locale: string): string {
  if (!iso) return "—";
  return new Intl.DateTimeFormat(locale === "es" ? "es-CO" : "en-US", {
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(iso));
}

function describeDevice(
  s: UserSessionDto,
  unknownBrowser: string,
  unknownOs: string,
): string {
  const browser = s.browser ?? unknownBrowser;
  const version = s.browserVersion ? ` ${s.browserVersion}` : "";
  const os = s.operatingSystem ?? unknownOs;
  return `${browser}${version} · ${os}`;
}

function deviceIcon(s: UserSessionDto) {
  const isMobile = (s.deviceType ?? "").toLowerCase().includes("mobile");
  return isMobile ? Smartphone : MonitorSmartphone;
}

function apiErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof ApiRequestError) {
    return err.problem?.detail ?? err.problem?.title ?? err.message;
  }
  if (err instanceof Error) return err.message;
  return fallback;
}

// ─────────────────────────────────────────────────────────────────────────
// Page
// ─────────────────────────────────────────────────────────────────────────

export function SecuritySettings() {
  const { t, i18n } = useTranslation("settings");
  const queryClient = useQueryClient();

  const profileQuery = useQuery({ queryKey: PROFILE_KEY, queryFn: getMyProfile });
  const twoFactorEnabled = profileQuery.data?.twoFactorEnabled ?? false;

  const sessionsQuery = useQuery({
    queryKey: ["identity", "sessions", "me"],
    queryFn: getMySessions,
    staleTime: 30_000,
  });

  const sessions = useMemo(() => {
    const data = sessionsQuery.data ?? [];
    return [...data].sort((a, b) => {
      if (a.isCurrentSession && !b.isCurrentSession) return -1;
      if (!a.isCurrentSession && b.isCurrentSession) return 1;
      if (a.isActive && !b.isActive) return -1;
      if (!a.isActive && b.isActive) return 1;
      return new Date(b.lastActivityAt).getTime() - new Date(a.lastActivityAt).getTime();
    });
  }, [sessionsQuery.data]);

  const revokeOne = useMutation({
    mutationFn: (id: string) => revokeSession(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions", "me"] });
      toast.success(t("security.sessionsSection.revokeSuccess"));
    },
    onError: (err) =>
      toast.error(apiErrorMessage(err, t("security.sessionsSection.revokeErrorFallback"))),
  });

  const revokeAll = useMutation({
    mutationFn: () => revokeAllOtherSessions(),
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions", "me"] });
      toast.success(
        t("security.sessionsSection.revokeAllSuccess", { count: data.revokedCount }),
      );
    },
    onError: (err) =>
      toast.error(apiErrorMessage(err, t("security.sessionsSection.revokeAllErrorFallback"))),
  });

  const otherActiveCount = useMemo(
    () => sessions.filter((s) => s.isActive && !s.isCurrentSession).length,
    [sessions],
  );

  const sessionsError =
    sessionsQuery.error instanceof ApiRequestError
      ? sessionsQuery.error.problem?.detail ?? sessionsQuery.error.message
      : sessionsQuery.error
        ? t("security.sessionsSection.loadErrorFallback")
        : null;

  return (
    <div className="space-y-6 fsh-enter">
      <PasswordCard />
      <TwoFactorCard enabled={twoFactorEnabled} loading={profileQuery.isLoading} />

      {/* Active sessions — already wired to the backend. */}
      <Card>
        <CardHeader>
          <div className="flex items-start justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                {t("security.sessionsSection.title")}
                {!sessionsQuery.isLoading && (
                  <Badge variant="default">
                    {t("security.sessionsSection.active", {
                      count: sessions.filter((s) => s.isActive).length,
                    })}
                  </Badge>
                )}
              </CardTitle>
              <CardDescription>
                {t("security.sessionsSection.description")}
              </CardDescription>
            </div>
            <Button
              variant="outline"
              size="sm"
              disabled={otherActiveCount === 0 || revokeAll.isPending}
              onClick={() => revokeAll.mutate()}
            >
              <LogOut className="mr-1.5 h-3.5 w-3.5" />
              {t("security.sessionsSection.signOutEverywhere")}
            </Button>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {sessionsError && (
            <div className="border-t border-[var(--color-border)] px-6 py-4 text-sm text-[var(--color-destructive)] flex items-start gap-2">
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
              <span>{sessionsError}</span>
            </div>
          )}

          {sessionsQuery.isLoading ? (
            <SessionsSkeleton />
          ) : sessions.length === 0 ? (
            <div className="px-6 py-12 text-center">
              <p className="text-sm font-medium tracking-tight">
                {t("security.sessionsSection.noSessions")}
              </p>
              <p className="mt-1 text-xs text-[var(--color-muted-foreground)]">
                {t("security.sessionsSection.noSessionsDescription")}
              </p>
            </div>
          ) : (
            <ul>
              {sessions.map((s, idx) => {
                const Icon = deviceIcon(s);
                const isRevoking =
                  revokeOne.isPending && revokeOne.variables === s.id;
                const locale = i18n.resolvedLanguage ?? "en";
                return (
                  <li
                    key={s.id}
                    className={cn(
                      "fsh-enter group/row flex items-center justify-between gap-4",
                      "border-t border-[var(--color-border)] px-6 py-4 first:border-t-0",
                      "transition-colors",
                      !s.isActive && "opacity-60",
                    )}
                    style={{ animationDelay: `${50 * idx}ms` }}
                  >
                    <div className="flex items-center gap-3">
                      <span
                        aria-hidden
                        className={cn(
                          "grid h-9 w-9 shrink-0 place-items-center rounded-full ring-1 ring-inset",
                          s.isCurrentSession
                            ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)] ring-[var(--color-primary)]/30"
                            : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)] ring-[var(--color-border)]",
                        )}
                      >
                        <Icon className="h-4 w-4" />
                      </span>
                      <div className="space-y-0.5">
                        <div className="flex flex-wrap items-center gap-2 text-sm font-medium tracking-tight">
                          {describeDevice(
                            s,
                            t("security.sessionsSection.unknownBrowser"),
                            t("security.sessionsSection.unknownOs"),
                          )}
                          {s.isCurrentSession && (
                            <Badge variant="brand">
                              {t("security.sessionsSection.thisDevice")}
                            </Badge>
                          )}
                          {!s.isActive && (
                            <Badge variant="outline">
                              {t("security.sessionsSection.revoked")}
                            </Badge>
                          )}
                        </div>
                        <div className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
                          {s.ipAddress ?? t("security.sessionsSection.unknownIp")}
                          {" · "}
                          {t("security.sessionsSection.lastActivity")}{" "}
                          {formatTimestamp(s.lastActivityAt, locale)}
                          {" · "}
                          {t("security.sessionsSection.expires")}{" "}
                          {formatTimestamp(s.expiresAt, locale)}
                        </div>
                      </div>
                    </div>
                    {s.isActive && !s.isCurrentSession && (
                      <Button
                        variant="ghost"
                        size="sm"
                        disabled={isRevoking}
                        onClick={() => revokeOne.mutate(s.id)}
                      >
                        <LogOut className="mr-1.5 h-3.5 w-3.5" />
                        {isRevoking
                          ? t("security.sessionsSection.revoking")
                          : t("security.sessionsSection.revoke")}
                      </Button>
                    )}
                  </li>
                );
              })}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// Password card — Dialog-driven change-password flow
// ─────────────────────────────────────────────────────────────────────────

function PasswordCard() {
  const { t } = useTranslation("settings");
  const [open, setOpen] = useState(false);

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>{t("security.password.title")}</CardTitle>
          <CardDescription>{t("security.password.description")}</CardDescription>
        </CardHeader>
        <CardContent className="flex items-center justify-between gap-4 px-6 pb-5 pt-1">
          <div className="text-sm text-[var(--color-muted-foreground)]">
            {t("security.password.hint")}
          </div>
          <Button variant="outline" size="sm" onClick={() => setOpen(true)}>
            <KeyRound className="size-4" />{t("security.password.change")}
          </Button>
        </CardContent>
      </Card>

      <ChangePasswordDialog open={open} onOpenChange={setOpen} />
    </>
  );
}

function ChangePasswordDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (next: boolean) => void;
}) {
  const { t } = useTranslation("settings");
  const { t: tcommon } = useTranslation("common");
  const [current, setCurrent] = useState("");
  const [next, setNext] = useState("");
  const [confirm, setConfirm] = useState("");
  const [localError, setLocalError] = useState<string | null>(null);

  // Reset state every time the dialog opens so stale values don't bleed.
  useEffect(() => {
    if (open) {
      setCurrent("");
      setNext("");
      setConfirm("");
      setLocalError(null);
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () =>
      changePassword({
        password: current,
        newPassword: next,
        confirmNewPassword: confirm,
      }),
    onSuccess: () => {
      toast.success(t("security.password.changed"), {
        description: t("security.password.changedDescription"),
      });
      onOpenChange(false);
    },
    onError: (err) =>
      setLocalError(apiErrorMessage(err, t("security.password.errorFallback"))),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setLocalError(null);

    if (next.length < 8) {
      setLocalError(t("security.password.errorMinLength"));
      return;
    }
    if (!/(?=.*[A-Za-z])(?=.*\d)/.test(next)) {
      setLocalError(tcommon("validation.passwordWeak"));
      return;
    }
    if (next !== confirm) {
      setLocalError(t("security.password.errorMismatch"));
      return;
    }
    if (next === current) {
      setLocalError(t("security.password.errorSameAsCurrent"));
      return;
    }
    mutation.mutate();
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("security.password.change")}</DialogTitle>
          <DialogDescription>
            {t("security.password.dialogDescription")}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} noValidate>
          <DialogBody className="space-y-4">
            <Field id="cp-current" label={t("security.currentPassword")} required>
              <Input
                id="cp-current"
                type="password"
                autoComplete="current-password"
                value={current}
                onChange={(e) => setCurrent(e.target.value)}
                required
                autoFocus
              />
            </Field>
            <Field id="cp-next" label={t("security.newPassword")} required>
              <Input
                id="cp-next"
                type="password"
                autoComplete="new-password"
                value={next}
                onChange={(e) => setNext(e.target.value)}
                required
                minLength={8}
              />
            </Field>
            <Field id="cp-confirm" label={t("security.confirmPassword")} required>
              <Input
                id="cp-confirm"
                type="password"
                autoComplete="new-password"
                value={confirm}
                onChange={(e) => setConfirm(e.target.value)}
                required
                minLength={8}
              />
            </Field>

            {localError && (
              <div
                role="alert"
                className="flex items-start gap-2 rounded-md border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.40)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)] px-3 py-2 text-sm text-[var(--color-destructive)]"
              >
                <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
                <span className="leading-snug">{localError}</span>
              </div>
            )}
          </DialogBody>

          <DialogFooter>
            <FormActions
              secondary={
                <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={mutation.isPending}>
                  <CancelIcon className="size-4" />{tcommon("actions.cancel")}
                </Button>
              }
              primary={
                <Button type="submit" disabled={mutation.isPending || !current || !next || !confirm}>
                  <KeyRound className="size-4" />
                  {mutation.isPending ? t("security.password.updating") : t("security.password.update")}
                </Button>
              }
            />
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// Two-factor card — real enroll/verify/disable flow
// ─────────────────────────────────────────────────────────────────────────

function TwoFactorCard({ enabled, loading }: { enabled: boolean; loading: boolean }) {
  const { t } = useTranslation("settings");
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          {t("security.twoFactor")}
          {loading ? (
            <Skeleton className="h-5 w-16 rounded-full" />
          ) : enabled ? (
            <Badge variant="success">{t("security.twoFactorSection.enabled")}</Badge>
          ) : (
            <Badge variant="warning">{t("security.twoFactorSection.disabled")}</Badge>
          )}
        </CardTitle>
        <CardDescription>
          {t("security.twoFactorSection.description")}
        </CardDescription>
      </CardHeader>
      <CardContent className="px-6 pb-5 pt-1">
        {loading ? (
          <Skeleton className="h-9 w-40" />
        ) : enabled ? (
          <TwoFactorDisable />
        ) : (
          <TwoFactorEnroll />
        )}
      </CardContent>
    </Card>
  );
}

function TwoFactorEnroll() {
  const { t } = useTranslation("settings");
  const queryClient = useQueryClient();
  const [enrollment, setEnrollment] = useState<TwoFactorEnrollmentResponse | null>(null);
  const [code, setCode] = useState("");
  const [qrSvg, setQrSvg] = useState<string | null>(null);
  const [copiedKey, setCopiedKey] = useState(false);

  const beginMutation = useMutation({
    mutationFn: enrollTwoFactor,
    onSuccess: (data) => setEnrollment(data),
    onError: (err) =>
      toast.error(t("security.twoFactorSection.enrollFailed"), {
        description: apiErrorMessage(err, t("security.twoFactorSection.enrollFailedFallback")),
      }),
  });

  const verifyMutation = useMutation({
    mutationFn: (otp: string) => verifyEnrollTwoFactor(otp),
    onSuccess: (data) => {
      if (data.success) {
        toast.success(t("security.twoFactorSection.enabledSuccess"), {
          description: t("security.twoFactorSection.enabledDescription"),
        });
        setEnrollment(null);
        setCode("");
        setQrSvg(null);
        void queryClient.invalidateQueries({ queryKey: PROFILE_KEY });
      } else {
        toast.error(t("security.twoFactorSection.verifyFailed"), {
          description: t("security.twoFactorSection.verifyMismatch"),
        });
      }
    },
    onError: (err) =>
      toast.error(t("security.twoFactorSection.verifyFailed"), {
        description: apiErrorMessage(err, t("security.twoFactorSection.verifyFailedFallback")),
      }),
  });

  // Render the QR as inline SVG when the otpauth URI changes. Keeps the
  // image source-of-truth in JS without an extra <canvas>, and lets us
  // theme it via currentColor so it tracks dark/light mode.
  useEffect(() => {
    if (!enrollment) {
      setQrSvg(null);
      return;
    }
    let cancelled = false;
    void QRCode.toString(enrollment.authenticatorUri, {
      type: "svg",
      margin: 1,
      width: 200,
      errorCorrectionLevel: "M",
    }).then((svg) => {
      if (cancelled) return;
      const themed = svg
        .replace(/fill="#ffffff"/gi, 'fill="transparent"')
        .replace(/fill="#FFFFFF"/gi, 'fill="transparent"')
        .replace(/fill="#000000"/gi, 'fill="currentColor"')
        .replace(/fill="#000"/gi, 'fill="currentColor"');
      setQrSvg(themed);
    });
    return () => {
      cancelled = true;
    };
  }, [enrollment]);

  const copyKey = async () => {
    if (!enrollment) return;
    try {
      await navigator.clipboard.writeText(enrollment.sharedKey);
      setCopiedKey(true);
      window.setTimeout(() => setCopiedKey(false), 1500);
    } catch {
      /* clipboard unavailable — silently noop */
    }
  };

  if (!enrollment) {
    return (
      <div className="flex flex-wrap items-center gap-3">
        <Button
          onClick={() => beginMutation.mutate()}
          disabled={beginMutation.isPending}
        >
          <ShieldCheck className="mr-1.5 h-3.5 w-3.5" />
          {beginMutation.isPending
            ? t("security.twoFactorSection.generating")
            : t("security.twoFactorSection.enable")}
        </Button>
        <span className="text-xs text-[var(--color-muted-foreground)]">
          {t("security.twoFactorSection.enableHint")}
        </span>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div className="grid gap-5 sm:grid-cols-[14rem_1fr] sm:items-start">
        <div className="grid h-52 w-52 place-items-center rounded-md border border-[var(--color-border)] bg-[var(--color-card)] p-2 text-[var(--color-foreground)]">
          {qrSvg ? (
            <div
              aria-label={t("security.twoFactorSection.qrCodeLabel")}
              role="img"
              className="h-full w-full [&_svg]:h-full [&_svg]:w-full"
              dangerouslySetInnerHTML={{ __html: qrSvg }}
            />
          ) : (
            <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
              {t("security.twoFactorSection.rendering")}
            </span>
          )}
        </div>
        <div className="space-y-3">
          <div>
            <div className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
              {t("security.twoFactorSection.cantScan")}
            </div>
            <div className="mt-1 flex items-center gap-2">
              <code className="break-all rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-2 py-1 font-mono text-[11px]">
                {enrollment.sharedKey}
              </code>
              <button
                type="button"
                onClick={copyKey}
                className="inline-flex h-7 items-center gap-1 rounded-md px-2 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
              >
                {copiedKey ? (
                  <>
                    <ClipboardCheck className="h-3 w-3" />
                    {" "}{t("security.twoFactorSection.copied")}
                  </>
                ) : (
                  <>
                    <Copy className="h-3 w-3" />
                    {" "}{t("security.twoFactorSection.copy")}
                  </>
                )}
              </button>
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="totp-code">{t("security.twoFactorSection.codeLabel")}</Label>
            <Input
              id="totp-code"
              inputMode="numeric"
              autoComplete="one-time-code"
              placeholder="123 456"
              maxLength={8}
              value={code}
              onChange={(e) => setCode(e.target.value.replace(/\s/g, ""))}
              className={cn(
                "font-mono text-lg tracking-[0.4em]",
                code.length >= 6 && "border-[var(--color-primary)]",
              )}
            />
          </div>

          <div className="flex flex-wrap items-center gap-2 pt-1">
            <Button
              onClick={() => verifyMutation.mutate(code)}
              disabled={code.length < 6 || verifyMutation.isPending}
            >
              <ShieldCheck className="size-4" />{verifyMutation.isPending
                ? t("security.twoFactorSection.verifying")
                : t("security.twoFactorSection.confirmEnable")}
            </Button>
            <Button
              variant="ghost"
              onClick={() => {
                setEnrollment(null);
                setCode("");
              }}
              disabled={verifyMutation.isPending}
            >
              <X className="size-4" />{t("common:actions.cancel", "Cancel")}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

function TwoFactorDisable() {
  const { t } = useTranslation("settings");
  const queryClient = useQueryClient();
  const [password, setPassword] = useState("");

  const mutation = useMutation({
    mutationFn: (pw: string) => disableTwoFactor(pw),
    onSuccess: (data) => {
      if (data.success) {
        toast.success(t("security.twoFactorSection.disabledSuccess"));
        setPassword("");
        void queryClient.invalidateQueries({ queryKey: PROFILE_KEY });
      } else {
        toast.error(t("security.twoFactorSection.disableFailed"), {
          description: t("security.twoFactorSection.disableVerifyFailed"),
        });
      }
    },
    onError: (err) =>
      toast.error(t("security.twoFactorSection.disableFailed"), {
        description: apiErrorMessage(
          err,
          t("security.twoFactorSection.disableFailedFallback"),
        ),
      }),
  });

  return (
    <div className="space-y-3">
      <p className="text-sm text-[var(--color-muted-foreground)]">
        {t("security.twoFactorSection.disableTitle")}
      </p>
      <div className="grid gap-3 sm:grid-cols-[1fr_auto] sm:items-end">
        <div className="space-y-1.5">
          <Label htmlFor="disable-pw">{t("security.currentPassword")}</Label>
          <Input
            id="disable-pw"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="font-mono"
          />
        </div>
        <Button
          type="button"
          variant="destructive"
          onClick={() => mutation.mutate(password)}
          disabled={password.length === 0 || mutation.isPending}
        >
          <ShieldOff className="mr-1 h-3.5 w-3.5" />
          {mutation.isPending
            ? t("security.twoFactorSection.disabling")
            : t("security.twoFactorSection.disable")}
        </Button>
      </div>
    </div>
  );
}

function SessionsSkeleton() {
  return (
    <ul>
      {[0, 1].map((i) => (
        <li
          key={i}
          className="flex items-center justify-between gap-4 border-t border-[var(--color-border)] px-6 py-4 first:border-t-0"
        >
          <div className="flex items-center gap-3">
            <Skeleton className="h-9 w-9 rounded-full" />
            <div className="space-y-1.5">
              <Skeleton className="h-4 w-48" />
              <Skeleton className="h-3 w-64" />
            </div>
          </div>
          <Skeleton className="h-8 w-20" />
        </li>
      ))}
    </ul>
  );
}
