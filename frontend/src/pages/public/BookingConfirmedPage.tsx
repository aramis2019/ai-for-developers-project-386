import { Alert, Button, Card, Group, Stack, Text, Title } from "@mantine/core";
import { Link, Navigate, useLocation } from "react-router-dom";
import type { Booking } from "../../api/client";
import { formatDateLong, formatDuration, formatRange, timeZoneLabel } from "../../lib/datetime";

/**
 * `/book/:eventTypeId/done` — подтверждение записи.
 *
 * Бронь передаётся через `navigate(state)`: контракт не даёт операции
 * «получить бронь по id», поэтому перезагрузить страницу по прямой ссылке
 * нельзя. При заходе без state возвращаем гостя к списку.
 */
export function BookingConfirmedPage() {
  const location = useLocation();
  const booking = (location.state as { booking?: Booking } | null)?.booking;

  if (!booking) return <Navigate to="/" replace />;

  return (
    <Stack gap="lg" maw={560}>
      <div>
        <Title order={1}>Вы записаны</Title>
        <Text c="dimmed" mt={4}>
          Подтверждение отправлено на {booking.guest.email}
        </Text>
      </div>

      <Card withBorder radius="md" padding="lg">
        <Stack gap="xs">
          <Group justify="space-between">
            <Text fw={600}>{booking.eventTypeTitle}</Text>
            <Text size="sm" c="dimmed">
              {formatDuration(booking.durationMinutes)}
            </Text>
          </Group>

          <Text>{formatDateLong(booking.start)}</Text>
          <Text size="xl" fw={700}>
            {formatRange(booking.start, booking.end)}
          </Text>
          <Text size="xs" c="dimmed">
            Время в вашем поясе ({timeZoneLabel()})
          </Text>

          {booking.guest.note && (
            <Text size="sm" c="dimmed" mt="xs">
              Комментарий: {booking.guest.note}
            </Text>
          )}
        </Stack>
      </Card>

      {/*
        Отмены и переноса брони в контракте текущей версии нет
        (contracts/docs/domain.md, раздел 5) — кнопок для них тоже быть не должно.
      */}
      <Alert variant="light" color="gray">
        <Text size="sm">
          Чтобы отменить или перенести встречу, напишите владельцу календаря — в текущей
          версии сервиса это делается вручную.
        </Text>
      </Alert>

      <Button component={Link} to="/" variant="default">
        Записаться ещё раз
      </Button>
    </Stack>
  );
}
