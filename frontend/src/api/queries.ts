import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "./client";
import type { BookingCreate } from "./client";

/**
 * Хуки react-query поверх сгенерированного клиента.
 * Пути и имена полей проверяются компилятором: опечатка — ошибка сборки.
 */

export const queryKeys = {
  eventTypes: ["event-types"] as const,
  slots: (eventTypeId: string) => ["slots", eventTypeId] as const,
  adminBookings: ["admin", "bookings"] as const,
  adminEventTypes: ["admin", "event-types"] as const,
  adminProfile: ["admin", "profile"] as const,
};

/** Виды брони для публичной страницы. */
export function useEventTypes() {
  return useQuery({
    queryKey: queryKeys.eventTypes,
    queryFn: async () => {
      const { data, error } = await api.GET("/api/event-types");
      if (error) throw error;
      return data.items;
    },
  });
}

/** Свободные слоты выбранного типа события на ближайшие 14 дней. */
export function useSlots(eventTypeId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.slots(eventTypeId ?? ""),
    enabled: Boolean(eventTypeId),
    // Слоты устаревают: их может занять другой гость.
    staleTime: 30_000,
    queryFn: async () => {
      const { data, error } = await api.GET("/api/event-types/{eventTypeId}/slots", {
        params: { path: { eventTypeId: eventTypeId! } },
      });
      if (error) throw error;
      return data;
    },
  });
}

/** Создание бронирования. При 409 список слотов инвалидируется автоматически. */
export function useCreateBooking() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (body: BookingCreate) => {
      const { data, error } = await api.POST("/api/bookings", { body });
      if (error) throw error;
      return data;
    },
    onSettled: (_data, _error, variables) => {
      void queryClient.invalidateQueries({
        queryKey: queryKeys.slots(variables.eventTypeId),
      });
    },
  });
}

/** Предстоящие встречи всех типов событий — админская страница. */
export function useUpcomingBookings() {
  return useQuery({
    queryKey: queryKeys.adminBookings,
    queryFn: async () => {
      const { data, error } = await api.GET("/api/admin/bookings");
      if (error) throw error;
      return data.items;
    },
  });
}
