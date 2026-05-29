/**
 * Loads Spanish (es-CO) CLDR data into Syncfusion's globalization layer so
 * EJ2 calendar components (DateRangePicker, DatePicker, Scheduler…) render
 * month names, weekday headers and number formats in Spanish.
 *
 * en-US ships built into ej2-base, so only the Spanish locale is loaded here.
 * Imported once from main.tsx, before React mounts.
 */
import { loadCldr } from "@syncfusion/ej2-base";

// Deep JSON imports from the cldr-data package (resolveJsonModule enabled).
import numberingSystems from "cldr-data/supplemental/numberingSystems.json";
import weekData from "cldr-data/supplemental/weekData.json";
import esGregorian from "cldr-data/main/es-CO/ca-gregorian.json";
import esNumbers from "cldr-data/main/es-CO/numbers.json";
import esTimeZoneNames from "cldr-data/main/es-CO/timeZoneNames.json";

loadCldr(numberingSystems, weekData, esGregorian, esNumbers, esTimeZoneNames);
