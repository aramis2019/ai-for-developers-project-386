import { createBrowserRouter, Navigate } from "react-router-dom";
import { PublicLayout } from "./layouts/PublicLayout";
import { AdminLayout } from "./layouts/AdminLayout";
import { EventTypesPage } from "./pages/public/EventTypesPage";
import { BookingPage } from "./pages/public/BookingPage";
import { BookingConfirmedPage } from "./pages/public/BookingConfirmedPage";
import { AdminBookingsPage } from "./pages/admin/AdminBookingsPage";
import { AdminEventTypesPage } from "./pages/admin/AdminEventTypesPage";

/**
 * Маршруты повторяют два сценария из contracts/docs/domain.md:
 * гость выбирает вид встречи и слот, владелец смотрит встречи и заводит типы.
 */
export const router = createBrowserRouter([
  {
    element: <PublicLayout />,
    children: [
      { path: "/", element: <EventTypesPage /> },
      { path: "/book/:eventTypeId", element: <BookingPage /> },
      { path: "/book/:eventTypeId/done", element: <BookingConfirmedPage /> },
    ],
  },
  {
    element: <AdminLayout />,
    children: [
      { path: "/admin", element: <Navigate to="/admin/bookings" replace /> },
      { path: "/admin/bookings", element: <AdminBookingsPage /> },
      { path: "/admin/event-types", element: <AdminEventTypesPage /> },
    ],
  },
  { path: "*", element: <Navigate to="/" replace /> },
]);
