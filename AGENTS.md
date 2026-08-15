# AGENTS.md

Meetly — сервис записи на встречи к одному владельцу календаря.
Монорепозиторий: контракт (TypeSpec), бэкенд (.NET 10), фронтенд (React + Mantine).

## Раскладка

| Путь | Что |
| --- | --- |
| `contracts/` | TypeSpec — **источник истины** API. `dist/openapi.yaml` генерируется |
| `contracts/docs/domain.md` | Доменная модель, инварианты, сценарии |
| `contracts/docs/adr/` | Решения по контракту |
| `backend/` | .NET 10, `Meetly.slnx`, Clean Architecture |
| `frontend/` | Vite + React 18 + TypeScript + Mantine |

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
npm run dev:frontend                    # Vite         :5173
npm run dev:mock -w @meetly/frontend    # Prism + Vite одной командой
npm run typecheck -w @meetly/frontend   # tsc --noEmit
npm run build:frontend
```

### Бэкенд

```bash
npm run build:backend        # dotnet build backend/Meetly.slnx
npm run test:backend         # dotnet test  backend/Meetly.slnx
dotnet run --project backend/src/Meetly.Api    # :5000

# один тест
dotnet test backend/tests/Meetly.ContractTests --filter EveryContractOperationIsRouted
```

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

## Перед коммитом

```bash
npm run contract:lint
npm run contract:check
npm run typecheck -w @meetly/frontend
npm run test:backend
```
