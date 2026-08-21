# AGENTS.md

Meetly — сервис записи на встречи к одному владельцу календаря.
Монорепозиторий: контракт (TypeSpec), бэкенд (.NET 10), фронтенд (React + Mantine).

## Раскладка

| Путь | Что |
| --- | --- |
| `contracts/` | TypeSpec — **источник истины** API. `dist/openapi.yaml` генерируется |
| `contracts/docs/domain.md` | Доменная модель, инварианты, сценарии |
| `contracts/docs/scenarios.md` | User journeys для ручной и автоматической проверки |
| `contracts/docs/adr/` | Решения по контракту |
| `backend/` | .NET 10, `Meetly.slnx`, Clean Architecture |
| `frontend/` | Vite + React 18 + TypeScript + Mantine + react-query |
| `frontend/src/lib/datetime.ts` | UTC → локальное время, группировка слотов |
| `frontend/src/api/errors.ts` | Ветвление по `ErrorBody.code` |

## Команды

Всё запускается из **корня**. npm workspaces: `contracts` + `frontend`.

### Установка

```bash
npm install                  # contracts + frontend
cd backend && dotnet tool restore && cd ..
```

### Контракт

```bash
npm run contract:build       # .tsp -> contracts/dist/openapi.yaml
npm run contract:lint        # tsp compile --warn-as-error + redocly lint
npm run contract:format      # tsp format
npm run contract:check       # сборка + проверка, что закоммиченный YAML свежий
npm run contract:watch       # пересборка на лету
```

### Генерация из контракта

```bash
npm run api:types            # -> frontend/src/api/schema.d.ts
npm run contracts:csharp     # -> backend/src/Meetly.Contracts/Generated/
```

### Фронтенд

```bash
npm run dev:mock -w @meetly/frontend    # Prism :4010 + Vite :5173 — основной режим
npm run dev:frontend                    # только Vite  :5173
npm run typecheck -w @meetly/frontend   # tsc --noEmit
npm run build:frontend
```

Открывать `http://localhost:5173`: Vite слушает IPv6, по `127.0.0.1:5173`
будет connection refused.

### Бэкенд

```bash
npm run build:backend        # dotnet build backend/Meetly.slnx
npm run test:backend         # dotnet test  backend/Meetly.slnx
dotnet run --project backend/src/Meetly.Api    # :5000

# один тест
dotnet test backend/tests/Meetly.ContractTests --filter EveryContractOperationIsRouted

# EF Core (миграции)
dotnet dotnet-ef migrations add <Name> --project src/Meetly.Infrastructure --startup-project src/Meetly.Api
dotnet dotnet-ef database update --project src/Meetly.Infrastructure --startup-project src/Meetly.Api
# запускать из backend/, tool-manifest — backend/dotnet-tools.json
```

#### Хранилище

`Storage:Provider` в конфиге выбирает `InMemory` или `Postgres`.

- `appsettings.json` — `InMemory` (безопасный дефолт, CI и юнит-тесты)
- `appsettings.Development.json` — `Postgres`, connection string с плейсхолдером
- `appsettings.Development.local.json` — реальный пароль (в `.gitignore` по паттерну
  `backend/**/appsettings.*.local.json`; подхватывается явно в `Program.cs`)
- Интеграционные тесты форсят InMemory через `InMemoryWebApplicationFactory`,
  чтобы не требовать запущенный Postgres.

При старте с `Postgres`: `InitializeMeetlyAsync` применяет миграции и запускает
`DevDataSeeder` (3 типа событий).

#### ADR 0001 в Postgres

Атомарность обеспечивает БД, а не приложение:

- `bookings.during` — generated `tstzrange` из `start_at`/`end_at` с полуинтервалом `[)`
- Exclusion constraint `bookings_no_overlap`: `EXCLUDE USING gist (during WITH &&)`
- При конфликте Postgres возвращает `SqlState 23P01` (exclusion_violation),
  `EfBookingRepository` переводит его в `AddBookingResult.Conflict` → `409 SLOT_ALREADY_BOOKED`
- `btree_gist` расширение не требуется — одиночная колонка `tstzrange` работает с gist «из коробки»

`InMemoryBookingRepository` даёт ту же гарантию через `lock` вокруг проверки-и-вставки,
но только в одном процессе.

#### Npgsql 10 и DateTimeOffset

Npgsql 10 разрешает записывать в `timestamptz` только `DateTimeOffset` с `Offset=0`.
Всё, что уходит в БД, нормализуется в UTC: `BookingService.CreateAsync` делает
`start.ToUniversalTime()`, `Mappings.ToEntity` — `.ToUniversalTime()` для `Start/End/CreatedAt`.
API-контракт и так говорит UTC, но клиент может прислать со сдвигом.

### Инструменты вокруг контракта

```bash
npm run mock                 # Prism, мок-сервер из спеки   :4010
npm run docs                 # Redoc, документация          :8080
```

Prism отдаёт только пути из спеки: `GET /` → `404 NO_PATH_MATCHED_ERROR`, это норма.

## Правило 1. Контракт правится первым, артефакты — никогда

`contracts/main.tsp` и `contracts/spec/*.tsp` — единственное место, где меняется API.

**Эти файлы редактировать запрещено, они перезаписываются генератором:**

| Файл | Кем генерируется |
| --- | --- |
| `contracts/dist/openapi.yaml` | `npm run contract:build` |
| `frontend/src/api/schema.d.ts` | `npm run api:types` |
| `backend/src/Meetly.Contracts/Generated/` | `npm run contracts:csharp` |

Порядок работы над изменением API:

```
1. contracts/*.tsp  ->  npm run contract:build  ->  npm run contract:lint
2. npm run api:types  и  npm run contracts:csharp
3. Только теперь — код бэкенда и фронтенда
```

Обратный порядок («сначала допишу эндпоинт, потом обновлю спеку») ломает
контрактные тесты и гейты CI. Изменение контракта — отдельный коммит/PR,
затрагивающий только `contracts/**` и сгенерированные файлы.

CI проверяет свежесть через `git diff --exit-code` — забытая перегенерация
валит билд.

## Правило 2. Направление зависимостей в бэкенде

```
Meetly.Api  ->  Meetly.Application  ->  Meetly.Domain
Meetly.Infrastructure  ->  Meetly.Application
Meetly.Contracts  —  ЛИСТОВОЙ, не ссылается ни на что
```

- `Meetly.Domain` не знает ни про ASP.NET, ни про EF Core, ни про `Meetly.Contracts`.
- Сгенерированные DTO из `Meetly.Contracts` живут только в слое `Meetly.Api`.
  Контроллеры мапят их в доменные модели и обратно — DTO не протекают в `Application`.
- Бизнес-логика в `Application` (юзкейсы) и `Domain` (инварианты), не в эндпоинтах.

Новый пакет NuGet добавлять только в тот проект, которому он нужен,
не в `Directory.Build.props`.

## Правило 3. Фронтенд не знает ничего, кроме контракта

Все данные и действия — только через `frontend/src/api/*`. Никаких вычислений
«как должно быть» в обход сервера.

- **Типы** берутся из `schema.d.ts` (`components["schemas"][...]`), руками
  интерфейсы ответов не описываются.
- **Границы календаря** — из `SlotsPage.window.from`/`to`, а не от `new Date()`.
  Окно скользящее, его считает сервер.
- **`start` отправляется той же строкой**, что пришла в слоте. Пересборка даты
  из компонентов ломает выравнивание по сетке → `422 SLOT_NOT_ALIGNED`.
- **Ошибки различаются по `code`**, а не по статусу и не по `message`:
  `409` — гонка, перезапросить слоты; `422` — чинить ввод (см. ADR 0002).
  Хелперы: `isSlotConflict`, `isStaleSlot`, `isEventTypeGone`, `toFieldErrors`.
- **Группировка слотов — по локальным дням** (`groupSlotsByLocalDay`), не по UTC.
- **Нет операции — нет кнопки.** Отмены, переноса и редактирования типов
  событий в контракте нет, добавлять их в UI нельзя. Сначала контракт.

`frontend/src/api/constraints.ts` дублирует ограничения из `.tsp` ради
мгновенной валидации формы. Это осознанное исключение из правила «не дублировать
контракт»: сервер остаётся последней инстанцией, его `422` показывается
пользователю. При изменении ограничений в `.tsp` правьте и этот файл.

## Ключевой доменный инвариант

> Никакие два бронирования не пересекаются по времени, даже если относятся
> к разным типам событий.

Интервал встречи — полуинтервал `[start, end)`. Проверка занятости идёт по
**всему** множеству броней, не в разрезе типа события, и должна быть атомарна
со вставкой. Подробности — `contracts/docs/adr/0001-cross-event-type-busy.md`.

Разделение ошибок (`409` — гонка, `422` — некорректный ввод) описано
в `contracts/docs/adr/0002-error-status-codes.md`. Клиент ветвится по полю
`code`, а не по тексту `message`.

## Соглашения

- Всё время в API — UTC, ISO 8601 с `Z`. Локализация — на клиенте.
- Комментарии, doc-комментарии `.tsp` и ADR — на русском.
- Markdown-списки в doc-комментариях `.tsp` держать в одну строку на пункт:
  перенос ломает генерацию описаний в OpenAPI.
- `@info` в TypeSpec не принимает `description` — описание сервиса пишется
  doc-комментарием над `namespace`.
- `backend/NuGet.config` намеренно делает `<clear/>` и по `packageSources`,
  и по `disabledPackageSources` — не удалять, иначе restore ломается на машинах
  с корпоративным зеркалом.

## MCP-серверы

Подключены в `opencode.json`. Конфигурация читается при старте opencode —
после правки нужен перезапуск.

| Сервер | Зачем |
| --- | --- |
| `github` | issues, PR, ветки без установки `gh` |
| `playwright` | проверка UI в браузере: рендер, консоль, сквозные сценарии |
| `context7` | документация под установленные версии библиотек |

### Playwright

Единственный способ проверить, что фронтенд **работает**, а не только
компилируется. `tsc --noEmit` и HTTP 200 от Vite доказывают лишь валидность
TypeScript: React-приложение может собраться и показать белый экран.

Обязательно после правок UI:

```
1. npm run dev:mock -w @meetly/frontend   (Prism :4010 + Vite :5173)
2. browser_navigate http://localhost:5173/
3. browser_console_messages — ошибок быть не должно
4. browser_snapshot / browser_take_screenshot
```

Ветки ошибок воспроизводятся подменой ответа (`route.fulfill`) или заголовком
`Prefer: code=409` на Prism — на реальном бэкенде гонку за слот не поймать.

Ограничения: браузер headless, разрешены только `localhost:5173`,
`127.0.0.1:4010`, `localhost:5000`. Chromium загружен в
`%LOCALAPPDATA%\ms-playwright`.

### Context7

Версии в проекте новее, чем знания модели: Mantine 7.17, react-router 7.18,
react-query 5.101, TypeSpec 1.15, EF Core 10. Спрашивать документацию до
написания кода, а не после ошибки компилятора.

Порядок всегда двухшаговый, оба параметра каждого шага обязательны:

1. `resolve-library-id` (`libraryName` + `query`) — получить ID из индекса.
   Выбирать по числу сниппетов и score, а не первый попавшийся.
2. `query-docs` (`libraryId` + `query`) — конкретный вопрос, не «расскажи про X».

Выбор `libraryId` определяет качество ответа:

| Тема | libraryId |
| --- | --- |
| EF Core в целом | `/dotnet/entityframework.docs` |
| Postgres-специфика: `tstzrange`, exclusion constraints, `timestamptz` | `/npgsql/efcore.pg` |
| Mantine | `/websites/mantine_dev` |

Npgsql-темы в общей доке EF Core отсутствуют — запрос туда вернёт
правдоподобный, но неполный ответ (проверено: на вопрос про exclusion
constraint отдаёт только `HasCheckConstraint`).

Context7 отдаёт свежую доку из репозитория, не срез по минорной версии —
ответ всё равно проверяется компиляцией.

Когда НЕ обращаться: вопросы про код этого репозитория, базовый
TypeScript/C#/SQL, всё, что проверяется компиляцией за секунды.

### GitHub

Через него доступны issues, pull requests, ветки и содержимое репозитория
без установки `gh`.

- Токен берётся из переменной окружения `GITHUB_TOKEN`, в конфиге лежит только
  подстановка `{env:GITHUB_TOKEN}`. **Секрет в репозиторий не коммитить.**
- Заголовок `X-MCP-Readonly: true` оставляет только читающие инструменты.
  Создать issue или PR через MCP нельзя — это осознанное ограничение.
  Снимать только по явной просьбе пользователя.
- Конфигурация читается при старте opencode. После правки `opencode.json`
  нужен перезапуск.

Полезные инструменты: `list_issues`, `issue_read`, `search_issues`,
`list_pull_requests`, `pull_request_read`, `search_pull_requests`.

## Conventional Commits

Все коммиты в этом репозитории — включая коммиты агента — оформляются по
[Conventional Commits 1.0](https://www.conventionalcommits.org/ru/v1.0.0/).
Это питает commit-and-tag-version: он парсит историю с прошлого тега,
генерирует CHANGELOG.md и решает, какой bump версии выкатывать.

Формат заголовка:

```
<type>(<scope>): <краткое описание в повелительном наклонении>
```

- `type` — обязательный, из списка ниже.
- `scope` — необязательный. В этом проекте: `contracts`, `backend`, `frontend`,
  `ci`, `docs`, `deps`. Пиши scope, когда он реально сужает область изменения.
- Описание — короткое (до ~70 символов), с маленькой буквы, без точки в конце.
  Русский язык допустим.

| Тип | Влияние на версию | Секция в CHANGELOG |
| --- | --- | --- |
| `feat` | minor (в pre-1.0 — patch) | Функциональность |
| `fix` | patch | Исправления |
| `perf` | patch | Производительность |
| `refactor` | patch | Рефакторинг |
| `docs` | patch | Документация |
| `build` | patch | Сборка |
| `ci` | patch | CI |
| `test` | patch | скрыто |
| `style` | patch | скрыто |
| `chore` | patch | скрыто |

Ломающее изменение (даёт major bump) выражается одним из способов:

- восклицательный знак в заголовке: `feat(contracts)!: убрать поле guest.note`;
- строка `BREAKING CHANGE: <описание>` в теле коммита (после пустой строки).

Примеры хороших коммитов:

```
feat(backend): реализовать сквозную занятость через exclusion constraint
fix(frontend): не пересобирать start слота на клиенте — 422 SLOT_NOT_ALIGNED
docs(contracts): описать сценарии в contracts/docs/scenarios.md
ci: добавить workflow публикации релиза в Gitea
chore(deps): обновить Npgsql до 10.0.3
feat(contracts)!: сделать guest.note обязательным
```

Что делать нельзя: `misc`, `stuff`, `wip`, `Update file.cs` — такие сообщения
commit-and-tag-version проигнорирует, и в changelog они не попадут.

## Релизы: commit-and-tag-version

Release-please с Gitea не работает (ходит в GitHub API), поэтому релиз готовит
`commit-and-tag-version` локально, а публикует Gitea Actions.

- Файлы: `.versionrc.json` (секции CHANGELOG и форматы ссылок на Gitea) и
  workflow `.github/workflows/release.yml` — на пуш тега `v*` создаёт
  Gitea Release с текстом секции из `CHANGELOG.md`.
- Одна версия на весь монорепо. Тег вида `v0.2.0`, обновляются корневые
  `package.json`, `package-lock.json` и `CHANGELOG.md`.
- Как выпустить релиз:
  1. `npm run release:dry` — предпросмотр: какой bump и какой changelog выйдут;
  2. `npm run release` — bump версии, CHANGELOG.md, коммит
     `chore(release): vX.Y.Z` и тег;
  3. `git push --follow-tags origin main` — пуш тега запускает `release.yml`.
- Самый первый релиз: `npm run release -- --first-release` — тег на текущую
  версию из `package.json`, без bump'а.
- Переопределить версию: `npm run release -- --release-as 1.0.0`
  (или `minor`/`patch`).
- Теги руками (`git tag`) не создавать — версия и CHANGELOG разъедутся
  с историей. Роль «мёрджа release PR» здесь играет запуск `npm run release`:
  до него ничего не публикуется.

## Перед коммитом

```bash
npm run contract:lint
npm run contract:check
npm run typecheck -w @meetly/frontend
npm run test:backend
```
