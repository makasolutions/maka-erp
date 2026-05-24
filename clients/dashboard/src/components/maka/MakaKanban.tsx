/**
 * MakaKanban — Syncfusion KanbanComponent pre-configured for Maka ERP.
 *
 * - Locale: Spanish inline (no external file).
 * - Drag & drop enabled by default.
 * - Typed KanbanColumn with optional maxCount for WIP limits.
 * - onCardDrop callback fires after a card is moved between columns.
 *
 * NEVER use KanbanComponent directly in pages — always use MakaKanban.
 */
import {
  KanbanComponent,
  ColumnsDirective,
  ColumnDirective,
  Inject,
  type DragEventArgs,
} from "@syncfusion/ej2-react-kanban";
import { L10n } from "@syncfusion/ej2-base";
import { useTranslation } from "react-i18next";

// ── Spanish locale ────────────────────────────────────────────────────────────
L10n.load({
  es: {
    kanban: {
      items: "elementos",
      min: "Mín",
      max: "Máx",
      cardsSelected: "Tarjeta(s) seleccionada(s)",
      addTitle: "Agregar nueva tarjeta",
      editTitle: "Editar detalles de la tarjeta",
      deleteTitle: "Eliminar tarjeta",
      deleteContent: "¿Estás seguro de que quieres eliminar esta tarjeta?",
      save: "Guardar",
      delete: "Eliminar",
      cancel: "Cancelar",
      yes: "Sí",
      no: "No",
      close: "Cerrar",
      noCard: "Sin tarjetas para mostrar",
      unassigned: "Sin asignar",
    },
  },
});

// ── Types ─────────────────────────────────────────────────────────────────────

export interface KanbanColumn {
  /** Unique key that matches the card's status field */
  id: string;
  /** Header label shown at the top of the column */
  headerText: string;
  /** Maximum cards allowed (WIP limit); undefined = unlimited */
  maxCount?: number;
}

export interface MakaKanbanProps {
  /** Column definitions */
  columns: KanbanColumn[];
  /** Card data — each object must include a field matching keyField */
  dataSource: object[];
  /** Field that determines which column a card belongs to; default "status" */
  keyField?: string;
  /** Field used to display the card header text; default "title" */
  headerField?: string;
  /** Called after a card is dropped in a new column */
  onCardDrop?: (args: DragEventArgs) => void;
}

// ── Component ─────────────────────────────────────────────────────────────────

export function MakaKanban({
  columns,
  dataSource,
  keyField = "status",
  headerField = "title",
  onCardDrop,
}: MakaKanbanProps) {
  const { i18n } = useTranslation();
  return (
    <KanbanComponent
      dataSource={dataSource}
      keyField={keyField}
      locale={i18n.language}
      cardSettings={{
        contentField: "description",
        headerField: headerField,
        selectionType: "Single",
      }}
      dragStop={onCardDrop}
    >
      <ColumnsDirective>
        {columns.map((col) => (
          <ColumnDirective
            key={col.id}
            keyField={col.id}
            headerText={col.headerText}
            maxCount={col.maxCount}
          />
        ))}
      </ColumnsDirective>
      <Inject services={[]} />
    </KanbanComponent>
  );
}
