import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "./client";
import type { Booking, BookingCreate, EventType, EventTypeCreate } from "./client";
import type { ApiError } from "./errors";

/**
 * Хуки react-query поверх сгенерированного клиента — по одному на каждую
 * из семи операций контракта.
 *
 * Соглашение: `queryFn` бросает `ApiError` (тело `ErrorBody`), а не `Error`.
 * Поэтому мутации параметризованы явно — иначе react-query выведет `Error`
 * и в компоненте потеряется поле `code`, по которому ветвится UI.
 */

export const queryKeys = {
  eventTypes: ["event-types"] as const,
  slots: (eventTypeId: string) => ["slots", eventTypeId] as const,
  adminProfile: ["admin", "profile"] as const,
  adminEventTypes: ["admin", "event-types"] as const,
  adminBookings: ["admin", "bookings"] as const,
};

// ---------------------------------------------------------------------------
// Публичная часть — гость
// ---------------------------------------------------------------------------

/** `GET /api/event-types` — виды брони для страницы выбора. */
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

/**
 * `GET /api/event-types/{eventTypeId}/slots` — свободные слоты на 14 суток.
 *
 * Ответ живёт недолго: слот может занять другой гость. Держим короткий
 * `staleTime` и не кешируем агрессивно.
 */
export function useSlots(eventTypeId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.slots(eventTypeId ?? ""),
    enabled: Boolean(eventTypeId),
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

/**
 * `POST /api/bookings` — создание брони.
 *
 * После любого исхода список слотов инвалидируется: при успехе слот занят нами,
 * при `409` — кем-то другим. В обоих случаях сетка устарела.
 */
export function useCreateBooking() {
  const queryClient = useQueryClient();

  return useMutation<Booking, ApiError, BookingCreate>({
    mutationFn: async (body) => {
      const { data, error } = await api.POST("/api/bookings", { body });
      if (error) throw error;
      return data;
    },
    onSettled: (_data, _error, variables) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.slots(variables.eventTypeId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.adminBookings });
    },
  });
}

// ---------------------------------------------------------------------------
// Админская часть — владелец календаря
// ---------------------------------------------------------------------------

/** `GET /api/admin/profile` — рабочие часы, шаг сетки, окно записи. */
export function useOwnerProfile() {
  return useQuery({
    queryKey: queryKeys.adminProfile,
    staleTime: 5 * 60_000,
    queryFn: async () => {
      const { data, error } = await api.GET("/api/admin/profile");
      if (error) throw error;
      return data;
    },
  });
}

/** `GET /api/admin/event-types` — типы событий владельца. */
export function useAdminEventTypes() {
  return useQuery({
    queryKey: queryKeys.adminEventTypes,
    queryFn: async () => {
      const { data, error } = await api.GET("/api/admin/event-types");
      if (error) throw error;
      return data.items;
    },
  });
}

/**
 * `POST /api/admin/event-types` — создание типа события.
 *
 * Идентификатор задаёт владелец, поэтому `409 EVENT_TYPE_ALREADY_EXISTS`
 * — штатный исход, который вешается ошибкой на поле `id`.
 */
export function useCreateEventType() {
  const queryClient = useQueryClient();

  return useMutation<EventType, ApiError, EventTypeCreate>({
    mutationFn: async (body) => {
      const { data, error } = await api.POST("/api/admin/event-types", { body });
      if (error) throw error;
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.adminEventTypes });
      // Новый тип сразу появляется на публичной странице.
      void queryClient.invalidateQueries({ queryKey: queryKeys.eventTypes });
    },
  });
}

/** `GET /api/admin/bookings` — предстоящие встречи всех типов событий. */
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
