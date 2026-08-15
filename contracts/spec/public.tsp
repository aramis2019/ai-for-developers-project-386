import "@typespec/http";
import "@typespec/openapi";

using TypeSpec.Http;
using TypeSpec.OpenAPI;

namespace Meetly;

/**
 * Публичная часть для гостя. Аккаунт и вход не требуются:
 * все эндпоинты анонимны.
 */
@tag("Public")
@route("/api")
namespace Public {
  /**
   * Виды брони, доступные гостю.
   *
   * Отдаёт название, описание и длительность каждого типа события —
   * данные для страницы выбора.
   */
  @summary("Список видов брони")
  @route("/event-types")
  @get
  op listEventTypes(): PublicEventTypeList;

  /**
   * Свободные слоты выбранного типа события на ближайшие 14 суток.
   *
   * Сетка строится так:
   *
   * 1. Берутся сутки от текущего момента до `now + 14 дней`.
   * 2. Внутри рабочих часов владельца расставляются точки старта с шагом `slotStepMinutes` (30 минут).
   * 3. Остаются только те точки, для которых интервал `[start, start + durationMinutes)` целиком помещается в рабочие часы того же дня и целиком попадает в окно записи.
   * 4. Отбрасываются точки в прошлом и те, чей интервал пересекается с любым существующим бронированием — независимо от его типа события.
   *
   * Ответ отражает состояние на момент запроса: слот может быть занят
   * другим гостем до отправки формы, поэтому источником истины остаётся
   * `POST /api/bookings`.
   */
  @summary("Свободные слоты типа события")
  @route("/event-types/{eventTypeId}/slots")
  @get
  op listSlots(
    /** Идентификатор типа события. */
    @path eventTypeId: EventTypeId,
  ): SlotsPage | BadRequestError | NotFoundError;

  /**
   * Создать бронирование на выбранный слот.
   *
   * Сервер проверяет по порядку:
   *
   * - `eventTypeId` существует, иначе `404 EVENT_TYPE_NOT_FOUND`;
   * - `start` выровнен по шагу сетки, иначе `422 SLOT_NOT_ALIGNED`;
   * - интервал `[start, start + durationMinutes)` целиком лежит в окне записи `[now, now + 14 дней]`, иначе `422 SLOT_OUT_OF_WINDOW`;
   * - интервал целиком помещается в рабочие часы одного дня, иначе `422 SLOT_OUTSIDE_WORKING_HOURS`;
   * - интервал не пересекается ни с одним существующим бронированием любого типа события, иначе `409 SLOT_ALREADY_BOOKED`.
   *
   * Проверка занятости и запись выполняются атомарно: при гонке двух гостей
   * за один слот успешным будет ровно один запрос.
   */
  @summary("Создать бронирование")
  @route("/bookings")
  @post
  op createBooking(@body body: BookingCreate):
    | {
        @statusCode statusCode: 201;
        @body booking: Booking;
      }
    | BadRequestError
    | NotFoundError
    | ConflictError
    | UnprocessableError;
}
