import createClient from "openapi-fetch";
import type { components, paths } from "./schema";

/**
 * Типизированный HTTP-клиент.
 *
 * Типы `paths` и `components` целиком выведены из contracts/dist/openapi.yaml —
 * руками здесь ничего не описывается. Опечатка в пути или в имени поля
 * становится ошибкой `tsc`, а не ошибкой в рантайме.
 *
 * Перегенерация после изменения контракта: `npm run api:types`.
 */
export const api = createClient<paths>({
  baseUrl: import.meta.env.VITE_API_URL ?? "http://127.0.0.1:4010",
  headers: { "Content-Type": "application/json" },
});

export type Owner = components["schemas"]["Owner"];
export type EventType = components["schemas"]["EventType"];
export type EventTypeCreate = components["schemas"]["EventTypeCreate"];
export type PublicEventType = components["schemas"]["PublicEventType"];
export type Slot = components["schemas"]["Slot"];
export type SlotsPage = components["schemas"]["SlotsPage"];
export type Booking = components["schemas"]["Booking"];
export type BookingCreate = components["schemas"]["BookingCreate"];
export type Guest = components["schemas"]["Guest"];
