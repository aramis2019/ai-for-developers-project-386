import { Button, SimpleGrid, Stack, Text } from "@mantine/core";
import type { Slot } from "../../api/client";
import { formatDateLong, formatTime, timeZoneLabel } from "../../lib/datetime";

interface SlotPickerProps {
  /** Слоты выбранного дня, в порядке от сервера. */
  slots: Slot[];
  /** `start` выбранного слота — сравнение идёт по исходной UTC-строке. */
  selectedStart: string | null;
  onSelect: (slot: Slot) => void;
}

/**
 * Выбор времени внутри дня.
 *
 * Слот идентифицируется исходной строкой `start` из ответа API. Она же
 * уходит обратно в `POST /api/bookings` без пересборки из компонентов даты —
 * иначе легко сдвинуть время и получить `422 SLOT_NOT_ALIGNED`.
 */
export function SlotPicker({ slots, selectedStart, onSelect }: SlotPickerProps) {
  if (slots.length === 0) {
    return (
      <Text c="dimmed" size="sm">
        Выберите день в календаре.
      </Text>
    );
  }

  return (
    <Stack gap="xs">
      <Text fw={600}>{formatDateLong(slots[0]!.start)}</Text>
      <Text size="xs" c="dimmed">
        Время указано в вашем поясе ({timeZoneLabel()})
      </Text>

      <SimpleGrid cols={{ base: 3, sm: 4 }} spacing="xs" mt="xs">
        {slots.map((slot) => (
          <Button
            key={slot.start}
            variant={slot.start === selectedStart ? "filled" : "default"}
            onClick={() => onSelect(slot)}
          >
            {formatTime(slot.start)}
          </Button>
        ))}
      </SimpleGrid>
    </Stack>
  );
}
