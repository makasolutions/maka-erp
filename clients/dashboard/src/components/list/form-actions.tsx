import { cn } from "@/lib/cn";

/**
 * FormActions — pie de acciones estandarizado para formularios/diálogos. Convención de
 * la casa: la **acción primaria va a la derecha**, la secundaria/Cancelar a la izquierda;
 * **todo botón lleva ícono** (lo provee quien lo usa). En móvil se apilan a ancho completo.
 *
 * Uso:
 *   <FormActions
 *     secondary={<Button variant="outline" onClick={cancel}><X/>Cancelar</Button>}
 *     primary={<Button onClick={save}><Check/>Guardar</Button>} />
 */
export function FormActions({
  primary,
  secondary,
  className,
}: {
  primary: React.ReactNode;
  secondary?: React.ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex flex-col-reverse gap-2 sm:flex-row sm:items-center sm:justify-end",
        className,
      )}
    >
      {secondary}
      {primary}
    </div>
  );
}
