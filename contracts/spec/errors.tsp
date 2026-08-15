import "@typespec/http";

using TypeSpec.Http;

namespace Meetly;

/** Машиночитаемый код ошибки. Клиент принимает решения по `code`, не по тексту. */
union ErrorCode {
  /** Тело или параметры запроса не прошли синтаксическую проверку. */
  BAD_REQUEST: "BAD_REQUEST",

  /** Тип события с указанным `eventTypeId` не существует. */
  EVENT_TYPE_NOT_FOUND: "EVENT_TYPE_NOT_FOUND",

  /** Тип события с таким `id` уже создан владельцем. */
  EVENT_TYPE_ALREADY_EXISTS: "EVENT_TYPE_ALREADY_EXISTS",

  /** Запрошенное время пересекается с уже существующим бронированием. */
  SLOT_ALREADY_BOOKED: "SLOT_ALREADY_BOOKED",

  /** Слот целиком не попадает в окно записи (14 суток от текущего момента). */
  SLOT_OUT_OF_WINDOW: "SLOT_OUT_OF_WINDOW",

  /** Начало слота не совпадает с сеткой доступности (шаг 30 минут). */
  SLOT_NOT_ALIGNED: "SLOT_NOT_ALIGNED",

  /** Встреча не помещается в рабочие часы владельца календаря. */
  SLOT_OUTSIDE_WORKING_HOURS: "SLOT_OUTSIDE_WORKING_HOURS",

  /** Данные запроса корректны синтаксически, но нарушают доменные правила. */
  VALIDATION_FAILED: "VALIDATION_FAILED",

  string,
}

/** Единый формат тела ошибки для всех эндпоинтов. */
model ErrorBody {
  /** Машиночитаемый код ошибки. */
  code: ErrorCode;

  /** Человекочитаемое описание для отображения пользователю. */
  message: string;

  /**
   * Дополнительный контекст ошибки.
   * Например, для `SLOT_ALREADY_BOOKED` — занятый интервал,
   * для `VALIDATION_FAILED` — список полей с проблемами.
   */
  details?: Record<unknown>;
}

/** Некорректный запрос: не разобрано тело, неверный формат даты и т. п. */
@error
model BadRequestError {
  @statusCode statusCode: 400;
  ...ErrorBody;
}

/** Запрошенный ресурс не найден. */
@error
model NotFoundError {
  @statusCode statusCode: 404;
  ...ErrorBody;
}

/** Конфликт состояния: слот уже занят или идентификатор типа события занят. */
@error
model ConflictError {
  @statusCode statusCode: 409;
  ...ErrorBody;
}

/** Запрос синтаксически корректен, но нарушает доменные правила. */
@error
model UnprocessableError {
  @statusCode statusCode: 422;
  ...ErrorBody;
}
