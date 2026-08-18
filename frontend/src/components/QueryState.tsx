import type { ReactNode } from "react";
import { Alert, Center, Loader, Stack, Text } from "@mantine/core";
import { describeError } from "../api/errors";

interface QueryStateProps {
  isPending: boolean;
  error: unknown;
  /** Показывать ли пустое состояние вместо детей. */
  isEmpty?: boolean;
  emptyTitle?: string;
  emptyText?: string;
  children: ReactNode;
}

/**
 * Единая обвязка «загрузка / ошибка / пусто» для всех страниц.
 *
 * Пустой список — валидное состояние домена, а не сбой: у владельца может
 * не быть типов событий, а в календаре может не остаться свободных слотов.
 */
export function QueryState({
  isPending,
  error,
  isEmpty = false,
  emptyTitle = "Пока пусто",
  emptyText,
  children,
}: QueryStateProps) {
  if (isPending) {
    return (
      <Center py="xl">
        <Loader />
      </Center>
    );
  }

  if (error) {
    return (
      <Alert color="red" title="Не удалось загрузить данные" variant="light">
        {describeError(error)}
      </Alert>
    );
  }

  if (isEmpty) {
    return (
      <Stack gap={4} py="xl" align="center">
        <Text fw={600}>{emptyTitle}</Text>
        {emptyText && (
          <Text size="sm" c="dimmed" ta="center">
            {emptyText}
          </Text>
        )}
      </Stack>
    );
  }

  return <>{children}</>;
}
