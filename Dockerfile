# syntax=docker/dockerfile:1

# Meetly — единый продакшен-образ: ASP.NET раздаёт и API, и статику фронтенда.
#
# Сборка герметична: бандл фронта и publish бэка выполняются внутри стадий,
# состояние рабочей машины/агента на артефакт не влияет.
#
#   docker build -t meetly:local .
#   docker run --rm -e PORT=8080 -p 8080:8080 meetly:local
#
# PORT задаёт порт приложения (используется деплоем и автопроверкой проекта),
# по умолчанию 8080. Хранилище по умолчанию InMemory (appsettings.json) —
# контейнер полностью автономен; для Postgres задать через окружение:
#   Storage__Provider=Postgres  и  ConnectionStrings__Meetly=...

# --- Стадия 1: бандл фронтенда -------------------------------------------
FROM node:24-alpine AS frontend

WORKDIR /src

# Сначала только манифесты — слой npm ci кэшируется, пока не меняются зависимости.
# contracts/package.json нужен npm: это workspace из корневого package.json.
COPY package.json package-lock.json ./
COPY contracts/package.json contracts/
COPY frontend/package.json frontend/
RUN npm ci -w @meetly/frontend --no-audit --no-fund

COPY frontend/ frontend/

# Пустая строка = относительные URL: фронт и API живут на одном origin,
# CORS в проде не нужен. schema.d.ts закоммичен — TypeSpec здесь не нужен.
ARG VITE_API_URL=""
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build -w @meetly/frontend

# --- Стадия 2: publish бэкенда -------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend

WORKDIR /src

# restore идёт через backend/NuGet.config (<clear/> + nuget.org) —
# одинаково на laptop, Jenkins-агенте и в этом контейнере.
COPY backend/ ./

ARG APP_VERSION=dev
RUN dotnet publish src/Meetly.Api/Meetly.Api.csproj \
        --configuration Release \
        --output /app/publish \
        -p:InformationalVersion=$APP_VERSION

# --- Стадия 3: рантайм ----------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=backend /app/publish ./
COPY --from=frontend /src/frontend/dist ./wwwroot/

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# exec — чтобы dotnet стал PID 1 и получал SIGTERM при docker stop.
# Порт берётся из PORT; --urls имеет приоритет над всеми настройками Kestrel.
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet Meetly.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
