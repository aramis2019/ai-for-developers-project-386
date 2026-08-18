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
  @opExample(
    #{
      returnType: #{
        items: #[
          #{
            id: "intro-call",
            title: "Знакомство",
            description: "Короткий созвон, чтобы обсудить задачу и понять, чем я могу помочь.",
            durationMinutes: 30,
          },
          #{
            id: "deep-dive",
            title: "Разбор задачи",
            description: "Час на детальный разбор с демонстрацией экрана и правками по ходу.",
            durationMinutes: 60,
          },
          #{
            id: "quick-sync",
            title: "Быстрая синхронизация",
            description: "Пятнадцать минут на один конкретный вопрос без подготовки.",
            durationMinutes: 15,
          }
        ],
      },
    },
    #{
      title: "Три типа события",
      description: "Разная длительность показывает, как от неё зависит сетка слотов.",
    }
  )
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
  @opExample(
    #{
      parameters: #{ eventTypeId: "intro-call" },
      returnType: #{
        eventTypeId: "intro-call",
        durationMinutes: 30,
        timeZone: "UTC",
        window: #{
          from: utcDateTime.fromISO("2026-08-15T08:00:00Z"),
          to: utcDateTime.fromISO("2026-08-29T08:00:00Z"),
        },
        slots: #[
          #{
            start: utcDateTime.fromISO("2026-08-17T09:00:00Z"),
            end: utcDateTime.fromISO("2026-08-17T09:30:00Z"),
          },
          #{
            start: utcDateTime.fromISO("2026-08-17T09:30:00Z"),
            end: utcDateTime.fromISO("2026-08-17T10:00:00Z"),
          },
          #{
            start: utcDateTime.fromISO("2026-08-17T11:00:00Z"),
            end: utcDateTime.fromISO("2026-08-17T11:30:00Z"),
          },
          #{
            start: utcDateTime.fromISO("2026-08-17T14:00:00Z"),
            end: utcDateTime.fromISO("2026-08-17T14:30:00Z"),
          },
          #{
            start: utcDateTime.fromISO("2026-08-18T10:00:00Z"),
            end: utcDateTime.fromISO("2026-08-18T10:30:00Z"),
          },
          #{
            start: utcDateTime.fromISO("2026-08-18T10:30:00Z"),
            end: utcDateTime.fromISO("2026-08-18T11:00:00Z"),
          },
          #{
            start: utcDateTime.fromISO("2026-08-18T15:30:00Z"),
            end: utcDateTime.fromISO("2026-08-18T16:00:00Z"),
          },
          #{
            start: utcDateTime.fromISO("2026-08-19T09:30:00Z"),
            end: utcDateTime.fromISO("2026-08-19T10:00:00Z"),
          },
          #{
            start: utcDateTime.fromISO("2026-08-19T13:00:00Z"),
            end: utcDateTime.fromISO("2026-08-19T13:30:00Z"),
          },
          #{
            start: utcDateTime.fromISO("2026-08-19T16:00:00Z"),
            end: utcDateTime.fromISO("2026-08-19T16:30:00Z"),
          }
        ],
      },
    },
    #{
      title: "Десять слотов на трёх днях",
      description: "Пропуски внутри дня — интервалы, занятые бронями любого типа события.",
    }
  )
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
  @opExample(
    #{
      parameters: #{
        body: #{
          eventTypeId: "intro-call",
          start: utcDateTime.fromISO("2026-08-17T09:30:00Z"),
          guest: #{
            name: "Игорь Петров",
            email: "igor@example.com",
            note: "Хочу обсудить миграцию на новый контракт.",
          },
        },
      },
      returnType: #{
        statusCode: 201,
        booking: #{
          id: "497f6eca-6276-4993-bfeb-53cbbbba6f08",
          eventTypeId: "intro-call",
          eventTypeTitle: "Знакомство",
          start: utcDateTime.fromISO("2026-08-17T09:30:00Z"),
          end: utcDateTime.fromISO("2026-08-17T10:00:00Z"),
          durationMinutes: 30,
          guest: #{
            name: "Игорь Петров",
            email: "igor@example.com",
            note: "Хочу обсудить миграцию на новый контракт.",
          },
          createdAt: utcDateTime.fromISO("2026-08-15T12:04:31Z"),
        },
      },
    },
    #{ title: "Успешное бронирование" }
  )
  @opExample(
    #{
      returnType: #{
        statusCode: 409,
        code: ErrorCode.SLOT_ALREADY_BOOKED,
        message: "Это время уже занято другой встречей.",
        details: #{
          start: "2026-08-17T09:30:00Z",
          end: "2026-08-17T10:00:00Z",
        },
      },
    },
    #{
      title: "Слот заняли, пока гость заполнял форму",
      description: "Ввод гостя корректен, изменилось состояние сервера: клиент перезапрашивает слоты.",
    }
  )
  @opExample(
    #{
      returnType: #{
        statusCode: 422,
        code: ErrorCode.VALIDATION_FAILED,
        message: "Проверьте контактные данные.",
        details: #{
          fields: #{ `guest.email`: "Некорректный адрес электронной почты." },
        },
      },
    },
    #{
      title: "Некорректные данные гостя",
      description: "`details.fields` раскладывается клиентом по полям формы.",
    }
  )
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
