import "@typespec/http";
import "@typespec/openapi";

using TypeSpec.Http;
using TypeSpec.OpenAPI;

namespace Meetly;

/**
 * Админская часть. Работает от имени единственного заранее заданного владельца
 * календаря: аутентификации нет, профиль подставляется сервером.
 */
@tag("Admin")
@route("/api/admin")
namespace Admin {
  /**
   * Профиль владельца календаря.
   *
   * Возвращает настройки, по которым строится сетка слотов: рабочие часы,
   * шаг сетки и глубину окна записи. Профиль всегда существует.
   */
  @summary("Профиль владельца календаря")
  @route("/profile")
  @get
  op getProfile(): Owner;

  /**
   * Список типов событий владельца, отсортированный по `createdAt`
   * по возрастанию.
   */
  @summary("Список типов событий")
  @route("/event-types")
  @get
  op listEventTypes(): EventTypeList;

  /**
   * Создать тип события.
   *
   * Идентификатор задаёт владелец. Если тип события с таким `id` уже есть,
   * возвращается `409 EVENT_TYPE_ALREADY_EXISTS` — существующий тип не
   * перезаписывается.
   *
   * Длительность не влияет на уже созданные бронирования: они хранят свою
   * длительность на момент записи.
   */
  @summary("Создать тип события")
  @route("/event-types")
  @post
  op createEventType(@body body: EventTypeCreate):
    | {
        @statusCode statusCode: 201;
        @body eventType: EventType;
      }
    | BadRequestError
    | ConflictError
    | UnprocessableError;

  /**
   * Предстоящие встречи — единый список бронирований всех типов событий.
   *
   * Возвращаются только брони, которые ещё не начались (`start >= now`),
   * отсортированные по `start` по возрастанию.
   */
  @summary("Предстоящие встречи")
  @route("/bookings")
  @get
  op listUpcomingBookings(): BookingList;
}
