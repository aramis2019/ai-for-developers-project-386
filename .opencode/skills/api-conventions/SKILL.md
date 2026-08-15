---
name: api-conventions
description: Правила проектирования API этого репозитория (Meetly). Use when adding, changing or reviewing an API endpoint, an error response, request validation, a TypeSpec model in contracts/spec/*.tsp, an ASP.NET route in Meetly.Api, or a call from frontend/src/api. Covers RESTful naming, the ErrorBody error shape, где живёт валидация, и логирование мутаций.
---

# Соглашения по API

Проект **contract-first**. Любое изменение API начинается с `contracts/spec/*.tsp`,
а не с кода. Полный порядок работы — в `AGENTS.md`, правило 1.

## 1. RESTful именование

- Ресурсы — существительные во множественном числе, `kebab-case`:
  `/api/event-types`, `/api/bookings`.
- Публичная часть (гость, без авторизации) — префикс `/api`.
  Админская часть (владелец календаря) — префикс `/api/admin`.
- Действие выражается HTTP-методом, а не путём.
  `POST /api/bookings` — да. `POST /api/createBooking` — нет.
- Вложенность только там, где ресурс не существует без родителя:
  `GET /api/event-types/{eventTypeId}/slots` — слоты вычисляются для конкретного
  типа события и вне его не имеют смысла.
- Параметр пути называется так же, как поле в моделях: `{eventTypeId}`, не `{id}`.

В TypeSpec операции группируются в namespace с `@route` и `@tag`
(`Admin` / `Public`) — см. `contracts/spec/admin.tsp`, `contracts/spec/public.tsp`.

## 2. Формат ошибок — `ErrorBody`

Единое тело для **всех** эндпоинтов, объявлено в `contracts/spec/errors.tsp`:

```jsonc
{
  "code": "SLOT_ALREADY_BOOKED",   // строка, машиночитаемый код
  "message": "Это время уже занято", // текст для пользователя
  "details": { "start": "...", "end": "..." } // необязательный контекст
}
```

Жёсткие требования:

- `code` — **строка**, не число. Открытый union `ErrorCode` в
  `contracts/spec/errors.tsp`; добавление нового кода не ломает клиентов.
- Клиент ветвится по `code`, **никогда** по тексту `message`: тексты
  локализуются и переписываются.
- Формы `{ error: string }`, `{ code: number }`, `ProblemDetails` из ASP.NET
  и голый текст — **запрещены**. Только `ErrorBody`.

Соответствие кода и статуса (ADR 0002, `contracts/docs/adr/0002-error-status-codes.md`):

| HTTP | Смысл | Коды |
| --- | --- | --- |
| 400 | Тело не разобрано, неверный формат | `BAD_REQUEST` |
| 404 | Ресурса нет | `EVENT_TYPE_NOT_FOUND` |
| 409 | Гонка: состояние сервера изменилось, ввод был корректен | `SLOT_ALREADY_BOOKED`, `EVENT_TYPE_ALREADY_EXISTS` |
| 422 | Ввод некорректен, повтор не поможет | `SLOT_*`, `VALIDATION_FAILED` |

Различие 409 и 422 существенно: при 409 клиент перезапрашивает слоты, при 422 —
чинит данные. Не сваливать всё в 400.

Новая операция объявляет union из успешного ответа и применимых ошибок:

```tsp
op createBooking(@body body: BookingCreate):
  | { @statusCode statusCode: 201; @body booking: Booking }
  | BadRequestError
  | NotFoundError
  | ConflictError
  | UnprocessableError;
```

## 3. Валидация входных данных

**zod в проекте не используется.** Источник правил валидации — контракт.

Ограничения объявляются декораторами TypeSpec на моделях и скалярах
(`contracts/spec/models.tsp`) и попадают в OpenAPI автоматически:

```tsp
@minLength(1) @maxLength(64)
@pattern("^[a-z0-9]+(?:-[a-z0-9]+)*$", "Slug в нижнем регистре")
scalar EventTypeId extends string;

@minValue(5) @maxValue(480)
durationMinutes: int32;

@format("email") @maxLength(254)
scalar Email extends string;
```

Дальше эти правила расходятся по трём направлениям:

| Слой | Что делает |
| --- | --- |
| Контракт | Единственное место, где правило **объявляется** |
| Фронтенд | `npm run api:types` → типы в `frontend/src/api/schema.d.ts`. Формы валидируются вручную по тем же границам; отдельной схемы не заводить |
| Бэкенд | `npm run contracts:csharp` → DataAnnotations в `Meetly.Contracts/Generated/`. Доменные инварианты — в `Meetly.Domain`, не в эндпоинте |

Правило: если валидация нужна новому полю — добавь декоратор в `.tsp`
и перегенерируй артефакты. Дублировать ограничение руками в C# или TS нельзя,
иначе они разъедутся.

Синтаксическая валидация — в контракте. Доменная (слот свободен, попадает
в окно записи) — в `Meetly.Domain` / `Meetly.Application`, возвращается как
`409` или `422`.

## 4. Логирование мутаций

Мутирующих операций в контракте ровно две:

- `POST /api/admin/event-types`
- `POST /api/bookings`

Каждая должна писать запись аудита: кто, что, когда, результат. Для брони —
`eventTypeId`, интервал `[start, end)`, e-mail гостя, исход (создана / отклонена
с кодом). GET-операции не логируются.

> **Статус: не реализовано.** Инфраструктуры аудита в репозитории пока нет —
> ни `IAuditLogger`, ни таблицы, ни вызовов. Не выдумывай импорт и не пиши
> `_auditLogger.Log(...)` со ссылкой на несуществующий тип.
>
> Когда дойдёт до реализации: интерфейс объявляется в `Meetly.Application`,
> реализация — в `Meetly.Infrastructure`, вызов — в юзкейсе, а не в эндпоинте
> (см. правило 2 в `AGENTS.md` про направление зависимостей).

## Чек-лист изменения API

```
1. contracts/spec/*.tsp
2. npm run contract:build && npm run contract:lint
3. npm run api:types && npm run contracts:csharp
4. Код бэкенда и фронтенда
5. npm run test:backend   (контрактный тест сверит маршруты со спекой)
```

Никогда не редактировать руками: `contracts/dist/openapi.yaml`,
`frontend/src/api/schema.d.ts`, `backend/src/Meetly.Contracts/Generated/`.
