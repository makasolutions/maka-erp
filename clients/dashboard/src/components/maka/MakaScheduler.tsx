/**
 * MakaScheduler — Syncfusion ScheduleComponent pre-configured for Maka ERP.
 *
 * - Timezone: America/Bogota (UTC-5).
 * - Default view: Week; available views: Day, Week, Month, Agenda.
 * - Locale: Spanish — CLDR calendar data loaded from cldr-data package,
 *   UI labels loaded via L10n.load() (both required for full ES support).
 * - 12-hour format (h:mm a) to match Colombian convention.
 * - onEventClick and onSlotClick callbacks.
 *
 * NEVER use ScheduleComponent directly in pages — always use MakaScheduler.
 */
import {
  ScheduleComponent,
  Day,
  Week,
  Month,
  Agenda,
  Inject,
  type EventClickArgs,
  type CellClickEventArgs,
  type View,
} from "@syncfusion/ej2-react-schedule";
import { L10n, loadCldr } from "@syncfusion/ej2-base";
// CLDR data for the "es" locale — required by Syncfusion's Internationalization
// layer for date/time formatting (day names, month names, AM/PM, patterns).
// L10n.load() covers UI strings; loadCldr() covers calendar/number data.
// Vite resolves these JSON imports at build time (resolveJsonModule: true).
import numberingSystems from "cldr-data/supplemental/numberingSystems.json";
import gregorian from "cldr-data/main/es/ca-gregorian.json";
import cldrNumbers from "cldr-data/main/es/numbers.json";
import timeZoneNames from "cldr-data/main/es/timeZoneNames.json";

loadCldr(numberingSystems, gregorian, cldrNumbers, timeZoneNames);

// ── Spanish UI strings ────────────────────────────────────────────────────────
L10n.load({
  es: {
    schedule: {
      day: "Día",
      week: "Semana",
      workWeek: "Semana laboral",
      month: "Mes",
      agenda: "Agenda",
      weekAgenda: "Agenda semanal",
      workWeekAgenda: "Agenda semana laboral",
      monthAgenda: "Agenda mensual",
      today: "Hoy",
      noEvents: "Sin eventos",
      emptyContainer: "No hay eventos programados para este día.",
      allDay: "Todo el día",
      start: "Inicio",
      end: "Fin",
      more: "más",
      close: "Cerrar",
      cancel: "Cancelar",
      noTitle: "(Sin título)",
      delete: "Eliminar",
      deleteEvent: "Eliminar evento",
      deleteMultipleEvent: "Eliminar múltiples eventos",
      selectedItems: "Elementos seleccionados",
      deleteSeries: "Eliminar serie completa",
      edit: "Editar",
      editSeries: "Editar serie completa",
      editEvent: "Editar evento",
      createEvent: "Crear",
      subject: "Asunto",
      addTitle: "Agregar título",
      moreDetails: "Más detalles",
      save: "Guardar",
      editContent: "¿Cómo deseas cambiar la cita de la serie?",
      deleteContent: "¿Seguro que deseas eliminar este evento?",
      deleteMultipleContent: "¿Seguro que deseas eliminar los eventos seleccionados?",
      newEvent: "Nuevo evento",
      title: "Título",
      location: "Ubicación",
      description: "Descripción",
      timezone: "Zona horaria",
      startTimezone: "Zona horaria de inicio",
      endTimezone: "Zona horaria de fin",
      repeat: "Repetir",
      saveButton: "Guardar",
      cancelButton: "Cancelar",
      deleteButton: "Eliminar",
      recurrence: "Recurrencia",
      wrongPattern: "El patrón de recurrencia no es válido.",
      recurringAlert:
        "Las fechas de la cita se solapan con su propia recurrencia. Elige otra.",
      removeAlert: "¿Deseas eliminar este evento de la serie?",
      editRecurrence: "Editar recurrencia",
      repeats: "Se repite",
      alert: "Alerta",
      startEndError: "La fecha de fin seleccionada es anterior a la de inicio.",
      invalidDateError: "El valor de fecha ingresado no es válido.",
      blockAlert:
        "No se pueden programar eventos en el rango de tiempo bloqueado.",
      ok: "Aceptar",
      yes: "Sí",
      no: "No",
      occurrence: "Ocurrencia",
      series: "Serie",
      previous: "Anterior",
      next: "Siguiente",
      expandAllDaySection: "Expandir",
      collapseAllDaySection: "Contraer",
      timelineDay: "Línea de tiempo del día",
      timelineWeek: "Línea de tiempo de la semana",
      timelineWorkWeek: "Línea de tiempo de la semana laboral",
      timelineMonth: "Línea de tiempo del mes",
      timelineYear: "Línea de tiempo del año",
      editFollowingEvent: "Eventos siguientes",
      deleteTitle: "Eliminar evento",
      editTitle: "Editar evento",
      beginFrom: "Comienza desde",
      endAt: "Termina en",
    },
    recurrenceeditor: {
      none: "Ninguna",
      daily: "Diaria",
      weekly: "Semanal",
      monthly: "Mensual",
      month: "Mes",
      yearly: "Anual",
      never: "Nunca",
      until: "Hasta",
      count: "Cantidad",
      first: "Primera",
      second: "Segunda",
      third: "Tercera",
      fourth: "Cuarta",
      last: "Última",
      repeat: "Repetir",
      repeatEvery: "Repetir cada",
      on: "El",
      end: "Fin",
      onDay: "Día",
      days: "Días",
      weeks: "Semanas",
      months: "Meses",
      years: "Años",
      every: "cada",
      summaryTimes: "veces",
      summaryOn: "el",
      summaryUntil: "hasta",
      summaryPeriod: "período(s)",
      summaryDay: "día(s)",
      summaryWeek: "semana(s)",
      summaryMonth: "mes(es)",
      summaryYear: "año(s)",
      monthWeek: "Semana del mes",
      startDay: "Día de inicio",
      interval: "Intervalo",
      repeatInterval: "Intervalo de repetición",
      of: "de",
    },
  },
});

// ── Types ─────────────────────────────────────────────────────────────────────

export interface MakaSchedulerProps {
  /** Event data array — objects must have Subject, StartTime, EndTime */
  events: object[];
  /** Called when the user clicks an existing event */
  onEventClick?: (args: EventClickArgs) => void;
  /** Called when the user clicks an empty time slot */
  onSlotClick?: (args: CellClickEventArgs) => void;
  /** Container height; default "550px" */
  height?: string;
  /** Initial view; default "Week" */
  defaultView?: View;
}

// ── Component ─────────────────────────────────────────────────────────────────

export function MakaScheduler({
  events,
  onEventClick,
  onSlotClick,
  height = "550px",
  defaultView = "Week",
}: MakaSchedulerProps) {
  return (
    <ScheduleComponent
      height={height}
      locale="es"
      timezone="America/Bogota"
      currentView={defaultView}
      timeFormat="h:mm a"
      dateFormat="dd/MM/yyyy"
      selectedDate={new Date()}
      eventSettings={{ dataSource: events }}
      eventClick={onEventClick}
      cellClick={onSlotClick}
      views={["Day", "Week", "Month", "Agenda"]}
    >
      <Inject services={[Day, Week, Month, Agenda]} />
    </ScheduleComponent>
  );
}
