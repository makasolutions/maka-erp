import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Check, Copy, Mail, MessageCircle, Share2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

/**
 * ShareMenu — copy a public, friendly URL or share it to social networks.
 * Uses the native Web Share API when available (mobile), else a dropdown.
 */
export function ShareMenu({ url, title }: { url: string; title: string }) {
  const { t } = useTranslation("catalog");
  const [open, setOpen] = useState(false);
  const [copied, setCopied] = useState(false);

  const enc = encodeURIComponent;
  const links = [
    { key: "whatsapp", label: t("sheet.whatsapp"), href: `https://wa.me/?text=${enc(`${title} ${url}`)}`, icon: MessageCircle },
    { key: "facebook", label: t("sheet.facebook"), href: `https://www.facebook.com/sharer/sharer.php?u=${enc(url)}`, icon: Share2 },
    { key: "x", label: t("sheet.x"), href: `https://twitter.com/intent/tweet?text=${enc(title)}&url=${enc(url)}`, icon: Share2 },
    { key: "email", label: t("sheet.email"), href: `mailto:?subject=${enc(title)}&body=${enc(url)}`, icon: Mail },
  ];

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      toast.success(t("sheet.copied"));
      setTimeout(() => setCopied(false), 1500);
    } catch {
      toast.error("—");
    }
  };

  const nativeShare = async () => {
    if (typeof navigator.share === "function") {
      try { await navigator.share({ title, url }); } catch { /* user cancelled */ }
    }
    setOpen(false);
  };

  return (
    <div className="relative">
      <Button variant="outline" onClick={() => setOpen((v) => !v)} className="h-9 gap-1.5">
        <Share2 className="size-4" />{t("sheet.share")}
      </Button>

      {open && (
        <>
          <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} aria-hidden />
          <div className="absolute right-0 z-20 mt-1 w-56 overflow-hidden rounded-lg border border-[var(--color-border)] bg-[var(--color-popover)] p-1 shadow-lg">
            <div className="px-2 py-1.5 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
              {t("sheet.shareVia")}
            </div>
            <button type="button" onClick={() => { void copy(); setOpen(false); }}
              className="flex w-full items-center gap-2 rounded-md px-2 py-2 text-left text-[13px] text-[var(--color-foreground)] hover:bg-[var(--color-muted)]">
              {copied ? <Check className="size-4 text-[var(--color-success)]" /> : <Copy className="size-4" />}
              {t("sheet.copyUrl")}
            </button>
            {typeof navigator.share === "function" && (
              <button type="button" onClick={() => void nativeShare()}
                className="flex w-full items-center gap-2 rounded-md px-2 py-2 text-left text-[13px] text-[var(--color-foreground)] hover:bg-[var(--color-muted)]">
                <Share2 className="size-4" />{t("sheet.share")}…
              </button>
            )}
            {links.map((l) => {
              const Icon = l.icon;
              return (
                <a key={l.key} href={l.href} target="_blank" rel="noreferrer" onClick={() => setOpen(false)}
                  className="flex w-full items-center gap-2 rounded-md px-2 py-2 text-[13px] text-[var(--color-foreground)] hover:bg-[var(--color-muted)]">
                  <Icon className="size-4" />{l.label}
                </a>
              );
            })}
            <div className={cn("truncate px-2 pb-1 pt-1.5 text-[10.5px] text-[var(--color-muted-foreground)]")} title={url}>
              {t("sheet.publicHint")}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
