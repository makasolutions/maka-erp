/**
 * MakaRichTextEditor — Syncfusion RichTextEditor pre-configured for Maka ERP.
 *
 * - Locale: es
 * - HTML output (value/onChange are HTML strings)
 * - Maka design tokens via CSS override (see maka-rte.css), incl. dark mode
 * - Sensible toolbar for product descriptions / technical specs
 *
 * NEVER use RichTextEditorComponent directly — always use MakaRichTextEditor.
 */
import { useEffect, useRef } from "react";
import {
  RichTextEditorComponent,
  Inject,
  Toolbar,
  Link,
  Image,
  HtmlEditor,
  QuickToolbar,
  Table,
  Count,
  type ToolbarSettingsModel,
} from "@syncfusion/ej2-react-richtexteditor";
import "./maka-rte.css";

export type MakaRichTextEditorProps = {
  value: string;
  onChange: (html: string) => void;
  placeholder?: string;
  /** Editor body height in px. Default 220. */
  height?: number;
  /** Show the character counter. Default true. */
  showCharCount?: boolean;
  maxLength?: number;
  disabled?: boolean;
  id?: string;
};

const TOOLBAR: ToolbarSettingsModel = {
  items: [
    "Bold", "Italic", "Underline", "StrikeThrough", "|",
    "Formats", "Alignments", "OrderedList", "UnorderedList", "|",
    "CreateLink", "Image", "CreateTable", "|",
    "ClearFormat", "SourceCode", "|", "Undo", "Redo",
  ],
};

export function MakaRichTextEditor({
  value, onChange, placeholder, height = 220, showCharCount = true, maxLength, disabled, id,
}: MakaRichTextEditorProps) {
  const ref = useRef<RichTextEditorComponent>(null);

  // Keep the editor in sync when the external value changes (e.g. async load),
  // without clobbering the caret while the user is typing.
  useEffect(() => {
    const rte = ref.current;
    if (rte && value !== rte.value && document.activeElement?.closest(".e-richtexteditor") == null) {
      rte.value = value ?? "";
    }
  }, [value]);

  return (
    <div className="maka-rte">
      <RichTextEditorComponent
        id={id}
        ref={ref}
        value={value || undefined}
        locale="es"
        height={height}
        placeholder={placeholder}
        enabled={!disabled}
        showCharCount={showCharCount}
        maxLength={maxLength ?? -1}
        toolbarSettings={TOOLBAR}
        change={(e: { value: string | null }) => onChange(e.value ?? "")}
        blur={() => { if (ref.current) onChange(ref.current.value ?? ""); }}
      >
        <Inject services={[Toolbar, Link, Image, HtmlEditor, QuickToolbar, Table, Count]} />
      </RichTextEditorComponent>
    </div>
  );
}
