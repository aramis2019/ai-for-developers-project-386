import type { components } from "./schema";

/**
 * Единое тело ошибки, общее для всех эндпоинтов контракта.
 * Ветвиться нужно по `code`, а не по HTTP-статусу и не по тексту `message`
 * — см. contracts/docs/adr/0002-error-status-codes.md.
 */
export type ApiError = components["schemas"]["ErrorBody"];

/** Тексты для пользователя по машиночитаемому коду. */
const messages: Record<string, string> = {
  SLOT_ALREADY_BOOKED: "Это время только что заняли. Выберите другой слот.",
  SLOT_OUT_OF_WINDOW: "Записаться можно только на ближайшие 14 дней.",
  SLOT_NOT_ALIGNED: "Некорректное время начала встречи.",
  SLOT_OUTSIDE_WORKING_HOURS: "Встреча не помещается в рабочие часы.",
  EVENT_TYPE_NOT_FOUND: "Этот вид встречи больше недоступен.",
  EVENT_TYPE_ALREADY_EXISTS: "Тип события с таким идентификатором уже существует.",
  VALIDATION_FAILED: "Проверьте введённые данные.",
  BAD_REQUEST: "Некорректный запрос.",
};

/** Похоже ли значение на тело ошибки контракта. */
export function isApiError(value: unknown): value is ApiError {
  return (
    typeof value === "object" &&
    value !== null &&
    "code" in value &&
    "message" in value &&
    typeof (value as ApiError).message === "string"
  );
}

/** Текст для пользователя. Незнакомый код — показываем `message` сервера. */
export function describeError(error: unknown): string {
  if (!isApiError(error)) return "Не удалось выполнить запрос. Проверьте соединение.";
  return messages[error.code] ?? error.message;
}

/**
 * Гонка за слот: ввод гостя был корректен, состояние сервера изменилось.
 * Клиент обязан перезапросить слоты, а не просить исправить форму.
 */
export function isSlotConflict(error: unknown): boolean {
  return isApiError(error) && error.code === "SLOT_ALREADY_BOOKED";
}

/**
 * Рассинхрон UI со спекой: слот не выровнен, вне окна или вне рабочих часов.
 * Ввод формы тут ни при чём — надо обновить сетку слотов.
 */
export function isStaleSlot(error: unknown): boolean {
  return (
    isApiError(error) &&
    (error.code === "SLOT_OUT_OF_WINDOW" ||
      error.code === "SLOT_NOT_ALIGNED" ||
      error.code === "SLOT_OUTSIDE_WORKING_HOURS")
  );
}

/** Тип события исчез — гостя нужно вернуть к списку. */
export function isEventTypeGone(error: unknown): boolean {
  return isApiError(error) && error.code === "EVENT_TYPE_NOT_FOUND";
}

/**
 * Разбор `details` в ошибки на поля формы.
 *
 * Контракт описывает `details` как свободный `Record<unknown>`: конкретная
 * форма — на совести сервера. Поддерживаются два распространённых варианта:
 *
 *   { "guest.email": "неверный формат" }
 *   { "fields": { "guest.email": "неверный формат" } }
 *
 * Всё, что не разобралось, вернётся пустым объектом — тогда показывается
 * общий текст ошибки.
 */
export function toFieldErrors(error: unknown): Record<string, string> {
  if (!isApiError(error) || !error.details) return {};

  const source =
    typeof error.details["fields"] === "object" && error.details["fields"] !== null
      ? (error.details["fields"] as Record<string, unknown>)
      : (error.details as Record<string, unknown>);

  const result: Record<string, string> = {};
  for (const [field, value] of Object.entries(source)) {
    if (typeof value === "string") result[field] = value;
    else if (Array.isArray(value) && typeof value[0] === "string") result[field] = value[0];
  }
  return result;
}
