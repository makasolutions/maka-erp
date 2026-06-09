import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Camera, Fingerprint, UserCircle2 } from "lucide-react";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { useAuth } from "@/auth/use-auth";
import { getMyProfile, setProfileImage, updateMyProfile } from "@/api/identity";
import { ApiRequestError } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ImageInput } from "@/components/file/image-input";
import { SettingsSection } from "@/pages/settings/settings-layout";
import { rules, validateSchema } from "@/lib/validation/rules";

const PROFILE_KEY = ["identity", "me"] as const;

export function ProfileSettings() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const { t } = useTranslation("settings");

  const profileQuery = useQuery({
    queryKey: PROFILE_KEY,
    queryFn: getMyProfile,
  });

  const { t: tc } = useTranslation("common");
  const profile = profileQuery.data;
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [phone, setPhone] = useState("");
  const [showErrors, setShowErrors] = useState(false);
  const profileSchema = {
    firstName: [rules.max(50), rules.personName()],
    lastName: [rules.max(50), rules.personName()],
    phone: [rules.phone()],
  };
  const errs = showErrors ? validateSchema({ firstName, lastName, phone }, profileSchema, tc) : {} as Record<string, string>;

  // Seed form state from the fetched profile (falls back to the JWT-derived
  // user while the query is in flight so the form isn't empty on first paint).
  useEffect(() => {
    if (profile) {
      setFirstName(profile.firstName ?? "");
      setLastName(profile.lastName ?? "");
      setPhone(profile.phoneNumber ?? "");
    } else if (user) {
      setFirstName(user.name?.split(" ")[0] ?? "");
      setLastName(user.name?.split(" ").slice(1).join(" ") ?? "");
    }
  }, [profile, user]);

  const saveMutation = useMutation({
    mutationFn: () =>
      updateMyProfile({
        firstName: firstName.trim() || null,
        lastName: lastName.trim() || null,
        phoneNumber: phone.trim() || null,
      }),
    onSuccess: () => {
      toast.success(t("profile.profileSaved"));
      queryClient.invalidateQueries({ queryKey: PROFILE_KEY });
    },
    onError: (err: unknown) => {
      const message =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : t("profile.saveFailed");
      toast.error(t("profile.saveFailed"), { description: message });
    },
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (Object.keys(validateSchema({ firstName, lastName, phone }, profileSchema, tc)).length > 0) { setShowErrors(true); return; }
    saveMutation.mutate();
  };

  const onReset = () => {
    if (profile) {
      setFirstName(profile.firstName ?? "");
      setLastName(profile.lastName ?? "");
      setPhone(profile.phoneNumber ?? "");
    }
  };

  const saving = saveMutation.isPending;
  const dirty =
    (profile?.firstName ?? "") !== firstName ||
    (profile?.lastName ?? "") !== lastName ||
    (profile?.phoneNumber ?? "") !== phone;

  const imageMutation = useMutation({
    mutationFn: (url: string | null) => setProfileImage(url),
    onSuccess: () => {
      toast.success(t("profile.imageUpdated"));
      queryClient.invalidateQueries({ queryKey: PROFILE_KEY });
    },
    onError: (e: unknown) => {
      const message =
        e instanceof ApiRequestError
          ? (e.problem?.detail ?? e.problem?.title ?? e.message)
          : t("profile.saveFailed");
      toast.error(message);
    },
  });

  return (
    <form onSubmit={onSubmit} className="space-y-5 fsh-enter">
      <SettingsSection
        title={t("profile.photo")}
        icon={Camera}
        description={t("profile.photoDesc")}
      >
        <ImageInput
          value={profile?.imageUrl ?? ""}
          onChange={(next) => imageMutation.mutate(next.length > 0 ? next : null)}
          ownerType="User"
          ownerId={profile?.id ?? null}
          shape="circle"
        />
      </SettingsSection>

      <SettingsSection
        title={t("profile.identity")}
        icon={UserCircle2}
        description={t("profile.identityDesc")}
        footer={
          <div className="flex items-center justify-end gap-2">
            <Button
              type="button"
              variant="ghost"
              onClick={onReset}
              disabled={saving || !dirty}
              size="sm"
            >
              {t("profile.reset")}
            </Button>
            <Button type="submit" disabled={saving || !dirty} size="sm">
              {saving ? t("feedback.saving", { ns: "common" }) : t("profile.saveChanges")}
            </Button>
          </div>
        }
      >
        <div className="grid gap-5 sm:grid-cols-2">
          <Field id="first-name" label={t("profile.firstName")} error={errs.firstName}>
            <Input
              id="first-name"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              autoComplete="given-name"
              className="h-10 text-[13px]"
            />
          </Field>
          <Field id="last-name" label={t("profile.lastName")} error={errs.lastName}>
            <Input
              id="last-name"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              autoComplete="family-name"
              className="h-10 text-[13px]"
            />
          </Field>
          <Field id="email" label={t("profile.email")}>
            <Input
              id="email"
              type="email"
              value={profile?.email ?? user?.email ?? ""}
              readOnly
              disabled
              className="h-10 cursor-not-allowed bg-[var(--color-muted)] text-[13px]"
            />
            <p className="mt-1 text-[11px] text-[var(--color-muted-foreground)]">
              {t("profile.emailReadOnly")}
            </p>
          </Field>
          <Field id="phone" label={t("profile.phone")} error={errs.phone}>
            <Input
              id="phone"
              type="tel"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              autoComplete="tel"
              placeholder="+57 (300) 123-4567"
              className="h-10 text-[13px]"
            />
          </Field>
        </div>
      </SettingsSection>

      <SettingsSection
        title={t("profile.subjectId")}
        icon={Fingerprint}
        description={t("profile.subjectIdDesc")}
      >
        <code className="block w-full overflow-x-auto rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2 font-mono text-xs">
          {profile?.id ?? user?.id ?? "—"}
        </code>
      </SettingsSection>
    </form>
  );
}

function Field({
  id,
  label,
  error,
  children,
}: {
  id: string;
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div
      className={
        error
          ? "[&_input]:!border-[var(--color-destructive)] [&_textarea]:!border-[var(--color-destructive)]"
          : undefined
      }
    >
      <Label
        htmlFor={id}
        className="mb-1.5 block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
      >
        {label}
      </Label>
      {children}
      {error && (
        <p className="mt-1 text-[11.5px] font-medium text-[var(--color-destructive)]">{error}</p>
      )}
    </div>
  );
}
