import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Anchor,
  Badge,
  Breadcrumbs,
  Grid,
  Group,
  Paper,
  Stack,
  Text,
  Title,
} from "@mantine/core";
import { notifications } from "@mantine/notifications";
import { Link, useNavigate, useParams } from "react-router-dom";
import type { Guest, Slot } from "../../api/client";
import { useCreateBooking, useEventTypes, useSlots } from "../../api/queries";
import {
  describeError,
  isEventTypeGone,
  isSlotConflict,
  isStaleSlot,
  toFieldErrors,
} from "../../api/errors";
import { QueryState } from "../../components/QueryState";
import { GuestForm } from "../../features/booking/GuestForm";
import { SlotCalendar } from "../../features/booking/SlotCalendar";
import { SlotPicker } from "../../features/booking/SlotPicker";
import { formatDuration, formatRange, groupSlotsByLocalDay } from "../../lib/datetime";

/**
 * `/book/:eventTypeId` — выбор слота и создание брони.
 *
 * Контракт отдаёт все свободные слоты на 14 суток одним ответом: ни пагинации,
 * ни фильтров по дате в `GET /slots` нет. Поэтому группировка по дням —
 * задача клиента.
 */
export function BookingPage() {
  const { eventTypeId } = useParams<{ eventTypeId: string }>();
  const navigate = useNavigate();

  const slotsQuery = useSlots(eventTypeId);
  const eventTypesQuery = useEventTypes();
  const createBooking = useCreateBooking();

  const [selectedDay, setSelectedDay] = useState<string | null>(null);
  const [selectedSlot, setSelectedSlot] = useState<Slot | null>(null);
  const [serverErrors, setServerErrors] = useState<Record<string, string>>({});

  const eventType = eventTypesQuery.data?.find((item) => item.id === eventTypeId);

  const days = useMemo(
    () => groupSlotsByLocalDay(slotsQuery.data?.slots ?? []),
    [slotsQuery.data],
  );

  // Первый доступный день выбирается сам — иначе гость видит пустую колонку.
  useEffect(() => {
    if (!selectedDay && days.length > 0) setSelectedDay(days[0]!.dayKey);
  }, [days, selectedDay]);

  // Слоты обновились, а выбранный исчез — снимаем выбор, чтобы не отправить
  // бронь на время, которого уже нет в сетке.
  useEffect(() => {
    if (!selectedSlot) return;
    const stillFree = slotsQuery.data?.slots.some((slot) => slot.start === selectedSlot.start);
    if (!stillFree) setSelectedSlot(null);
  }, [slotsQuery.data, selectedSlot]);

  // Тип события исчез между загрузкой списка и открытием страницы.
  useEffect(() => {
    if (isEventTypeGone(slotsQuery.error)) {
      notifications.show({
        color: "red",
        title: "Встреча недоступна",
        message: describeError(slotsQuery.error),
      });
      navigate("/", { replace: true });
    }
  }, [slotsQuery.error, navigate]);

  const daySlots = days.find((day) => day.dayKey === selectedDay)?.slots ?? [];

  function handleSubmit(guest: Guest) {
    if (!eventTypeId || !selectedSlot) return;
    setServerErrors({});

    createBooking.mutate(
      { eventTypeId, start: selectedSlot.start, guest },
      {
        onSuccess: (booking) => {
          navigate(`/book/${eventTypeId}/done`, { state: { booking } });
        },
        onError: (error) => {
          // 409 — гонка: ввод был корректен, состояние сервера изменилось.
          // Сбрасываем выбор и подтягиваем свежую сетку (ADR 0002).
          if (isSlotConflict(error) || isStaleSlot(error)) {
            setSelectedSlot(null);
            void slotsQuery.refetch();
            notifications.show({
              color: "orange",
              title: "Выберите другое время",
              message: describeError(error),
            });
            return;
          }

          if (isEventTypeGone(error)) {
            notifications.show({
              color: "red",
              title: "Встреча недоступна",
              message: describeError(error),
            });
            navigate("/", { replace: true });
            return;
          }

          // 422 по данным формы — раскладываем по полям, если сервер прислал details.
          const fields = toFieldErrors(error);
          setServerErrors(fields);
          if (Object.keys(fields).length === 0) {
            notifications.show({
              color: "red",
              title: "Не удалось записаться",
              message: describeError(error),
            });
          }
        },
      },
    );
  }

  return (
    <Stack gap="lg">
      <Breadcrumbs>
        <Anchor component={Link} to="/" size="sm">
          Виды встреч
        </Anchor>
        <Text size="sm" c="dimmed">
          {eventType?.title ?? eventTypeId}
        </Text>
      </Breadcrumbs>

      <div>
        <Group gap="sm" align="baseline">
          <Title order={1}>{eventType?.title ?? "Запись на встречу"}</Title>
          {slotsQuery.data && (
            <Badge variant="light">{formatDuration(slotsQuery.data.durationMinutes)}</Badge>
          )}
        </Group>
        {eventType?.description && (
          <Text c="dimmed" mt={4}>
            {eventType.description}
          </Text>
        )}
      </div>

      <QueryState
        isPending={slotsQuery.isPending}
        error={isEventTypeGone(slotsQuery.error) ? null : slotsQuery.error}
        isEmpty={slotsQuery.data?.slots.length === 0}
        emptyTitle="Свободного времени нет"
        emptyText="На ближайшие 14 дней всё занято. Загляните позже."
      >
        {slotsQuery.data && (
          <Grid gutter="lg">
            <Grid.Col span={{ base: 12, sm: 6 }}>
              <SlotCalendar
                windowFrom={slotsQuery.data.window.from}
                windowTo={slotsQuery.data.window.to}
                availableDays={days.map((day) => day.dayKey)}
                selectedDay={selectedDay}
                onSelect={(day) => {
                  setSelectedDay(day);
                  setSelectedSlot(null);
                }}
              />
            </Grid.Col>

            <Grid.Col span={{ base: 12, sm: 6 }}>
              <Stack gap="lg">
                <SlotPicker
                  slots={daySlots}
                  selectedStart={selectedSlot?.start ?? null}
                  onSelect={setSelectedSlot}
                />

                {selectedSlot ? (
                  <Paper withBorder p="md" radius="md">
                    <Stack gap="sm">
                      <Alert variant="light" color="blue" p="xs">
                        <Text size="sm">
                          Выбрано: {formatRange(selectedSlot.start, selectedSlot.end)}
                        </Text>
                      </Alert>

                      <GuestForm
                        disabled={createBooking.isPending}
                        submitting={createBooking.isPending}
                        onSubmit={handleSubmit}
                        serverErrors={serverErrors}
                      />
                    </Stack>
                  </Paper>
                ) : (
                  daySlots.length > 0 && (
                    <Text size="sm" c="dimmed">
                      Выберите время, чтобы заполнить контактные данные.
                    </Text>
                  )
                )}
              </Stack>
            </Grid.Col>
          </Grid>
        )}
      </QueryState>
    </Stack>
  );
}
