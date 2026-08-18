import { Indicator, Paper } from "@mantine/core";
import { DatePicker } from "@mantine/dates";
import { dayKeyToDate, localDayKey } from "../../lib/datetime";

interface SlotCalendarProps {
  /** Начало окна записи, UTC ISO. Пришло из `SlotsPage.window.from`. */
  windowFrom: string;
  /** Конец окна записи, UTC ISO. Пришло из `SlotsPage.window.to`. */
  windowTo: string;
  /** Локальные дни `YYYY-MM-DD`, в которых есть свободные слоты. */
  availableDays: string[];
  /** Выбранный день `YYYY-MM-DD` или `null`. */
  selectedDay: string | null;
  onSelect: (dayKey: string | null) => void;
}

/**
 * Календарь окна записи.
 *
 * Границы берутся из ответа API (`window.from`/`window.to`), а не вычисляются
 * от «сегодня». Это важно: окно скользящее и считается сервером на момент
 * запроса — клиент не должен догадываться о его границах сам.
 *
 * Дни без свободных слотов отключены: в контракте нет способа забронировать
 * время вне выданной сетки.
 */
export function SlotCalendar({
  windowFrom,
  windowTo,
  availableDays,
  selectedDay,
  onSelect,
}: SlotCalendarProps) {
  const available = new Set(availableDays);

  return (
    <Paper withBorder p="md" radius="md">
      <DatePicker
        value={selectedDay ? dayKeyToDate(selectedDay) : null}
        onChange={(date) => onSelect(date ? localDayKey(date) : null)}
        minDate={new Date(windowFrom)}
        maxDate={new Date(windowTo)}
        getDayProps={(date) => ({
          disabled: !available.has(localDayKey(date)),
        })}
        renderDay={(date) => {
          const key = localDayKey(date);
          return (
            <Indicator size={6} color="blue" offset={-4} disabled={!available.has(key)}>
              <div>{date.getDate()}</div>
            </Indicator>
          );
        }}
        size="md"
      />
    </Paper>
  );
}
