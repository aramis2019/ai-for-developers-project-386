import { Alert, Badge, Card, Container, Group, Loader, Stack, Text, Title } from "@mantine/core";
import { useEventTypes } from "./api/queries";
import { describeError } from "./api/client";

/**
 * Заглушка публичной страницы: список видов брони.
 *
 * Работает против мок-сервера Prism без единой строчки бэкенда:
 *   npm run dev:mock
 */
export function App() {
  const { data, isPending, error } = useEventTypes();

  return (
    <Container size="sm" py="xl">
      <Stack gap="lg">
        <div>
          <Title order={1}>Meetly</Title>
          <Text c="dimmed">Выберите вид встречи</Text>
        </div>

        {isPending && <Loader />}

        {error && <Alert color="red">{describeError(error as never)}</Alert>}

        {data?.map((eventType) => (
          <Card key={eventType.id} withBorder padding="lg" radius="md">
            <Group justify="space-between" mb="xs">
              <Text fw={600}>{eventType.title}</Text>
              <Badge variant="light">{eventType.durationMinutes} мин</Badge>
            </Group>
            <Text size="sm" c="dimmed">
              {eventType.description}
            </Text>
          </Card>
        ))}
      </Stack>
    </Container>
  );
}
