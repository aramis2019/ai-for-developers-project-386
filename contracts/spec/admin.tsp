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
  @opExample(#{
    returnType: #{
      id: "owner",
      name: "Анна Смирнова",
      email: "anna@meetly.local",
      timeZone: "UTC",
      workingHours: #{ start: "09:00", end: "18:00" },
      slotStepMinutes: 30,
      bookingWindowDays: 14,
    },
  })
  op getProfile(): Owner;

  /**
   * Список типов событий владельца, отсортированный по `createdAt`
   * по возрастанию.
   */
  @summary("Список типов событий")
  @route("/event-types")
  @get
  @opExample(#{
    returnType: #{
      items: #[
        #{
          id: "intro-call",
          title: "Знакомство",
          description: "Короткий созвон, чтобы обсудить задачу и понять, чем я могу помочь.",
          durationMinutes: 30,
          createdAt: utcDateTime.fromISO("2026-08-01T07:12:44Z"),
        },
        #{
          id: "deep-dive",
          title: "Разбор задачи",
          description: "Час на детальный разбор с демонстрацией экрана и правками по ходу.",
          durationMinutes: 60,
          createdAt: utcDateTime.fromISO("2026-08-03T15:40:09Z"),
        },
        #{
          id: "quick-sync",
          title: "Быстрая синхронизация",
          description: "Пятнадцать минут на один конкретный вопрос без подготовки.",
          durationMinutes: 15,
          createdAt: utcDateTime.fromISO("2026-08-11T06:55:20Z"),
        }
      ],
    },
  })
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
  @opExample(#{
    parameters: #{
      body: #{
        id: "quick-sync",
        title: "Быстрая синхронизация",
        description: "Пятнадцать минут на один конкретный вопрос без подготовки.",
        durationMinutes: 15,
      },
    },
    returnType: #{
      statusCode: 201,
      eventType: #{
        id: "quick-sync",
        title: "Быстрая синхронизация",
        description: "Пятнадцать минут на один конкретный вопрос без подготовки.",
        durationMinutes: 15,
        createdAt: utcDateTime.fromISO("2026-08-11T06:55:20Z"),
      },
    },
  })
  @opExample(
    #{
      returnType: #{
        statusCode: 409,
        code: ErrorCode.EVENT_TYPE_ALREADY_EXISTS,
        message: "Тип события с идентификатором «quick-sync» уже существует.",
        details: #{ fields: #{ id: "Такой идентификатор уже занят." } },
      },
    },
    #{
      title: "Идентификатор занят",
      description: "Существующий тип события не перезаписывается — клиент вешает ошибку на поле `id`.",
    }
  )
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
  @opExample(
    #{
      returnType: #{
        items: #[
          #{
            id: "497f6eca-6276-4993-bfeb-53cbbbba6f08",
            eventTypeId: "intro-call",
            eventTypeTitle: "Знакомство",
            start: utcDateTime.fromISO("2026-08-17T10:00:00Z"),
            end: utcDateTime.fromISO("2026-08-17T10:30:00Z"),
            durationMinutes: 30,
            guest: #{
              name: "Игорь Петров",
              email: "igor@example.com",
              note: "Хочу обсудить миграцию на новый контракт.",
            },
            createdAt: utcDateTime.fromISO("2026-08-15T12:04:31Z"),
          },
          #{
            id: "0b5a7c1e-2f43-4c9a-8d17-6ea2f0c93b55",
            eventTypeId: "deep-dive",
            eventTypeTitle: "Разбор задачи",
            start: utcDateTime.fromISO("2026-08-18T13:00:00Z"),
            end: utcDateTime.fromISO("2026-08-18T14:00:00Z"),
            durationMinutes: 60,
            guest: #{ name: "Мария Ковалёва", email: "maria@example.com" },
            createdAt: utcDateTime.fromISO("2026-08-14T18:22:07Z"),
          }
        ],
      },
    },
    #{
      title: "Две встречи разных типов",
      description: "Единый список: занятость сквозная, брони разных типов события не пересекаются.",
    }
  )
  op listUpcomingBookings(): BookingList;
}
