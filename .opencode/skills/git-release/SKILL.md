---
name: git-release
description: Подготовка релиза Meetly - changelog, определение типа версии и команда публикации. Use ONLY when the user explicitly asks to cut, prepare or publish a release, bump the version, or write a CHANGELOG entry. Не запускать самостоятельно при обычных коммитах, PR или мерже - только по прямой просьбе.
---

# Подготовка релиза

Сценарий выполняется **только по прямой просьбе пользователя**. Не инициируй его
сам после коммита, мержа PR или зелёного CI.

Ничего не пушить и не публиковать без явного подтверждения. Скилл готовит
изменения и предлагает команду — выполняет её пользователь.

## Контекст репозитория

- Remote: `https://github.com/aramis2019/ai-for-developers-project-386`
- Основная ветка: `main`
- Тегов пока **нет** — первый релиз берёт всю историю.
- `CHANGELOG.md` пока **нет** — создаётся на первом релизе.
- **`gh` не установлен.** Проверь `gh --version` перед тем, как предлагать
  `gh release create`; если его нет — предложи установку или веб-форму
  `https://github.com/aramis2019/ai-for-developers-project-386/releases/new`.
- Коммиты в истории не следуют Conventional Commits — тип изменения
  определяется по содержимому PR, а не по префиксу сообщения.

## Шаг 1. Собрать изменения после последнего тега

```bash
git fetch --tags
git describe --tags --abbrev=0        # последний тег; ошибка = тегов нет
```

Если тег есть:

```bash
git log <tag>..HEAD --merges --pretty=format:"%h %s"
git log <tag>..HEAD --pretty=format:"%h %s" --no-merges
```

Если тегов нет — взять всю историю: `git log --pretty=format:"%h %s"`.

При наличии `gh` полезнее список PR — в нём есть заголовки и метки:

```bash
gh pr list --state merged --base main --limit 100 \
  --json number,title,labels,mergedAt,url
```

Отфильтровать PR, смерженные позже даты тега.

## Шаг 2. Сгруппировать по типу

Три группы: **Features**, **Fixes**, **Chores**.

Ориентиры для этого репозитория — по затронутым путям:

| Что изменилось | Куда относить |
| --- | --- |
| `contracts/**` — новая операция, новое поле | Features |
| `contracts/**` — исправление ограничения, кода ошибки | Fixes |
| `backend/src/**`, `frontend/src/**` — новый функционал | Features |
| `backend/src/**`, `frontend/src/**` — исправление поведения | Fixes |
| `.github/**`, конфиги, зависимости, README, AGENTS.md | Chores |
| Сгенерированные файлы (`dist/`, `schema.d.ts`, `Generated/`) | **Не упоминать** — это следствие, а не изменение |

Изменения контракта выносить в начало соответствующей секции: они видны
внешним потребителям API и важнее внутренних правок.

## Шаг 3. Определить тип версии

**Не угадывать.** Для изменений API есть машинная проверка — `oasdiff`,
он же работает гейтом в `.github/workflows/contract.yml`.

```bash
git show <last-tag>:contracts/dist/openapi.yaml > /tmp/base-openapi.yaml
npx --yes oasdiff breaking /tmp/base-openapi.yaml contracts/dist/openapi.yaml
npx --yes oasdiff changelog /tmp/base-openapi.yaml contracts/dist/openapi.yaml
```

| Результат | Версия |
| --- | --- |
| `oasdiff breaking` нашёл ERR | **major** |
| Новые операции или необязательные поля в контракте, новый функционал UI | **minor** |
| Только исправления и chores, контракт не изменился | **patch** |

Помни: `ErrorCode` объявлен как открытый union, поэтому **новый код ошибки
не является ломающим изменением** и тянет на minor, а не major.

Пока проект до `1.0.0` в корневом `package.json` — согласуй с пользователем,
считать ли ломающее изменение поводом для `1.0.0` или остаться на `0.x`.

## Шаг 4. Обновить версии и changelog

### Версия живёт в четырёх местах

Пропуск любого из них рассинхронизирует репозиторий:

| Файл | Поле | Сейчас |
| --- | --- | --- |
| `package.json` (корень) | `version` | `0.1.0` |
| `contracts/package.json` | `version` | `1.0.0` |
| `frontend/package.json` | `version` | `0.1.0` |
| `contracts/main.tsp` | `@info(#{ version: "..." })` | `1.0.0` |

Версия API в `main.tsp` и версия релиза репозитория — **разные сущности**.
Контракт версионируется по совместимости API, репозиторий — по релизам.
Уточни у пользователя, поднимать ли обе, если меняется только одна из зон.

**После правки `main.tsp` обязательно:**

```bash
npm run contract:build      # info.version попадает в dist/openapi.yaml
npm run api:types           # версия не влияет на типы, но артефакт должен быть свежим
```

Иначе `npm run contract:check` и гейт свежести в CI упадут.

### CHANGELOG.md

Формат — [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/), даты ISO,
новые записи сверху. Если файла нет — создать с шапкой:

```markdown
# Changelog

Все заметные изменения проекта фиксируются в этом файле.

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/),
проект придерживается [Semantic Versioning](https://semver.org/lang/ru/).

## [Unreleased]

## [1.1.0] - 2026-08-15

### Added
- Публичный эндпоинт `GET /api/event-types/{eventTypeId}/slots` (#12)

### Fixed
- `SLOT_OUT_OF_WINDOW` возвращался вместо `SLOT_NOT_ALIGNED` (#15)

### Changed
- Рабочие часы владельца вынесены в профиль (#14)
```

Секции Keep a Changelog: `Added`, `Changed`, `Deprecated`, `Removed`,
`Fixed`, `Security`. Группы из шага 2 ложатся так: Features → `Added`
(или `Changed`, если поведение менялось), Fixes → `Fixed`, Chores —
в changelog обычно **не попадают**, кроме заметных для пользователя.

Ломающие изменения контракта помечать явно:

```markdown
### Changed
- **BREAKING** `POST /api/bookings` требует поле `guest.email` (#21)
```

## Шаг 5. Предложить команду публикации

Сначала — проверка, что репозиторий в релизном состоянии:

```bash
npm run contract:lint
npm run contract:check
npm run typecheck -w @meetly/frontend
npm run test:backend
git status --short          # должно быть чисто, кроме релизного коммита
```

Затем показать пользователю команды и **дождаться подтверждения**:

```bash
git add CHANGELOG.md package.json contracts/package.json frontend/package.json \
        contracts/main.tsp contracts/dist/openapi.yaml
git commit -m "Release v1.1.0"
git tag -a v1.1.0 -m "Release v1.1.0"
git push origin main --follow-tags

gh release create v1.1.0 \
  --title "v1.1.0" \
  --notes-file <(sed -n '/## \[1.1.0\]/,/## \[/p' CHANGELOG.md | head -n -1)
```

Если `gh` недоступен — предложить создать релиз через веб-форму и приложить
текст секции из `CHANGELOG.md`.

## Чего не делать

- Не пушить, не тегировать и не публиковать релиз без подтверждения пользователя.
- Не выполнять `git push --force` и не переписывать существующие теги.
- Не редактировать сгенерированные файлы вручную ради версии —
  `dist/openapi.yaml` обновляется только через `npm run contract:build`.
- Не указывать в changelog изменения сгенерированных артефактов.
- Не запускать этот сценарий по собственной инициативе.
