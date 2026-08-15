### Hexlet tests and linter status:
[![Actions Status](https://github.com/aramis2019/ai-for-developers-project-386/actions/workflows/hexlet-check.yml/badge.svg)](https://github.com/aramis2019/ai-for-developers-project-386/actions)

# Meetly — сервис записи на встречи

Запись на встречи к **одному** владельцу календаря. Регистрации и авторизации нет:
админская часть работает от заранее заданного профиля, гость бронирует анонимно.

Монорепозиторий: контракт, бэкенд и фронтенд в одном месте, но с разными владельцами.

## Структура

```
contracts/          ЕДИНСТВЕННЫЙ ИСТОЧНИК ИСТИНЫ — TypeSpec -> OpenAPI 3.1
  main.tsp
  spec/             errors, models, admin, public
  dist/openapi.yaml сгенерирован, коммитится, руками не правится
  docs/domain.md    доменная модель, инварианты, сценарии
  docs/adr/         решения по контракту

backend/            .NET 10, Clean Architecture
  src/Meetly.Api            эндпоинты, DI, middleware
  src/Meetly.Application    юзкейсы
  src/Meetly.Domain         сущности и инварианты
  src/Meetly.Infrastructure персистентность
  src/Meetly.Contracts      DTO, СГЕНЕРИРОВАННЫЕ из openapi.yaml
  tests/Meetly.ContractTests маршруты приложения vs спека

frontend/           React 18 + TypeScript + Mantine + Vite
  src/api/schema.d.ts  СГЕНЕРИРОВАН из openapi.yaml
  src/api/client.ts    openapi-fetch, типизирован по schema.d.ts
  src/api/queries.ts   хуки react-query
```

## Правила предметной области

- Владелец создаёт типы событий: `id`, название, описание, длительность в минутах.
- Гость выбирает тип события и свободный слот в ближайшие **14 суток**.
- Слоты строятся внутри рабочих часов `09:00–18:00` UTC с шагом **30 минут**.
- Занятость сквозная: **два бронирования не могут пересекаться по времени**,
  даже если это разные типы событий ([ADR 0001](contracts/docs/adr/0001-cross-event-type-busy.md)).
- Все моменты времени — UTC, ISO 8601 с суффиксом `Z`.

Подробно — [contracts/docs/domain.md](contracts/docs/domain.md).

## Эндпоинты

```
GET  /api/event-types                        виды брони
GET  /api/event-types/{eventTypeId}/slots    свободные слоты на 14 дней
POST /api/bookings                           создать бронирование

GET  /api/admin/profile                      профиль владельца
GET  /api/admin/event-types                  типы событий
POST /api/admin/event-types                  создать тип события
GET  /api/admin/bookings                     предстоящие встречи
```

## Быстрый старт

```bash
npm install                  # ставит contracts и frontend (npm workspaces)
npm run contract:build       # .tsp -> contracts/dist/openapi.yaml
npm run api:types            # openapi.yaml -> frontend/src/api/schema.d.ts

npm run docs                 # интерактивная документация  :8080
npm run mock                 # мок-сервер Prism            :4010
npm run dev:frontend         # Vite                        :5173
```

Фронтенд можно разрабатывать **до появления бэкенда** — он ходит в мок:

```bash
npm run dev:mock -w @meetly/frontend   # Prism + Vite одной командой
```

Переключение на реальный бэкенд — переменной `VITE_API_URL` (см. `frontend/.env.example`).

Бэкенд:

```bash
dotnet build backend/Meetly.slnx
dotnet test  backend/Meetly.slnx
dotnet run --project backend/src/Meetly.Api      # :5000

cd backend && dotnet tool restore
dotnet nswag run src/Meetly.Contracts/nswag.json # openapi.yaml -> C# DTO
```

## Как менять контракт

Порядок обязателен: контракт мержится **до** реализации.

```
1. Обсудили поведение          -> правим contracts/docs/domain.md, при нужде ADR
2. PR только с contracts/**    -> апрув обеих команд (CODEOWNERS), CI зелёный
3. Параллельно и независимо:
     бэк:   dotnet nswag run ... -> реализация -> dotnet test
     фронт: npm run api:types    -> UI на моке
4. Интеграция: VITE_API_URL с мока на реальный бэкенд
```

Если контракт правится в том же PR, что и реализация, вы вернулись к
«бэк решил, фронт узнал по 500-й ошибке».

## Гейты CI

| Гейт | Что ловит | Где |
| --- | --- | --- |
| `tsp compile --warn-as-error` + `redocly lint` | Невалидная спека | `contract.yml` |
| `git diff --exit-code contracts/dist` | Забыли пересобрать YAML | `contract.yml` |
| `git diff --exit-code frontend/src/api` | Забыли перегенерить типы | `contract.yml` |
| `oasdiff breaking` | Ломающее изменение контракта | `contract.yml` |
| `Meetly.ContractTests` | Маршруты бэкенда разъехались со спекой | `backend.yml` |
| `git diff` после `nswag run` | DTO бэкенда отстали от контракта | `backend.yml` |
| `schemathesis run` | Реальные ответы не соответствуют схеме | `backend.yml` |
| `tsc --noEmit` | Фронт использует несуществующие поля | `frontend.yml` |

## Сгенерированные файлы

Коммитятся, но правятся только генератором:

| Файл | Команда |
| --- | --- |
| `contracts/dist/openapi.yaml` | `npm run contract:build` |
| `frontend/src/api/schema.d.ts` | `npm run api:types` |
| `backend/src/Meetly.Contracts/Generated/` | `dotnet nswag run src/Meetly.Contracts/nswag.json` |
