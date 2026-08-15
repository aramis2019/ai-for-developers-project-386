import createClient from "openapi-fetch";
import type { components, paths } from "./schema";

/**
 * Типизированный HTTP-клиент. Типы `paths` и `components` целиком выведены из
 * contracts/dist/openapi.yaml — руками здесь ничего не описывается.
 *
 * Перегенерация после изменения контракта: `npm run api:types`.
 */
export const api = createClient<paths>({
  baseUrl: import.meta.env.VITE_API_URL ?? "http://127.0.0.1:4010",
  headers: { "Content-Type": "application/json" },
});

/** Тело ошибки, единое для всех эндпоинтов. */
export type ApiError = components["schemas"]["ErrorBody"];

/** Машиночитаемый код ошибки: ветвиться нужно по нему, а не по `message`. */
export type ApiErrorCode = ApiError["code"];

export type EventType = components["schemas"]["PublicEventType"];
export type Slot = components["schemas"]["Slot"];
export type SlotsPage = components["schemas"]["SlotsPage"];
export type Booking = components["schemas"]["Booking"];
export type BookingCreate = components["schemas"]["BookingCreate"];

/**
 * Сообщения для пользователя по коду ошибки.
 *
 * Ключевое различие, зафиксированное в контракте (см. ADR 0002):
 * `SLOT_ALREADY_BOOKED` — это гонка, ввод гостя был корректен, нужно
 * перезапросить слоты. Остальные `422` — проблема во вводе, перезапрос не поможет.
 */
export const errorMessages: Record<string, string> = {
  SLOT_ALREADY_BOOKED: "Это время только что заняли. Выберите другой слот.",
  SLOT_OUT_OF_WINDOW: "Записаться можно только на ближайшие 14 дней.",
  SLOT_NOT_ALIGNED: "Некорректное время начала встречи.",
  SLOT_OUTSIDE_WORKING_HOURS: "Встреча не помещается в рабочие часы.",
  EVENT_TYPE_NOT_FOUND: "Этот вид встречи больше недоступен.",
  EVENT_TYPE_ALREADY_EXISTS: "Тип события с таким идентификатором уже существует.",
  VALIDATION_FAILED: "Проверьте введённые данные.",
  BAD_REQUEST: "Некорректный запрос.",
};

/** Нужно ли перезапросить список слотов после ошибки. */
export function shouldRefetchSlots(error: ApiError): boolean {
  return error.code === "SLOT_ALREADY_BOOKED";
}

export function describeError(error: ApiError | undefined): string {
  if (!error) return "Не удалось выполнить запрос.";
  return errorMessages[error.code] ?? error.message;
}
