import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { MantineProvider } from "@mantine/core";
import { Notifications } from "@mantine/notifications";
import { DatesProvider } from "@mantine/dates";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "react-router-dom";
import "@mantine/core/styles.css";
import "@mantine/dates/styles.css";
import "@mantine/notifications/styles.css";
import { router } from "./routes";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Ошибки контракта (404, 409, 422) повторять бессмысленно —
      // ретраим только сетевые сбои, и то один раз.
      retry: 1,
      refetchOnWindowFocus: false,
    },
    mutations: { retry: 0 },
  },
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <MantineProvider defaultColorScheme="auto">
      <DatesProvider settings={{ locale: "ru", firstDayOfWeek: 1, weekendDays: [0, 6] }}>
        <QueryClientProvider client={queryClient}>
          <Notifications position="top-right" />
          <RouterProvider router={router} />
        </QueryClientProvider>
      </DatesProvider>
    </MantineProvider>
  </StrictMode>,
);
