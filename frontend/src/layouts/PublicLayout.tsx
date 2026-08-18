import { Anchor, AppShell, Badge, Container, Group, Text, Title } from "@mantine/core";
import { Link, Outlet } from "react-router-dom";
import { timeZoneLabel } from "../lib/datetime";

/** Публичная часть: шапка с названием и меткой часового пояса гостя. */
export function PublicLayout() {
  return (
    <AppShell header={{ height: 60 }} padding="md">
      <AppShell.Header>
        <Container size="md" h="100%">
          <Group h="100%" justify="space-between">
            <Anchor component={Link} to="/" underline="never" c="inherit">
              <Title order={3}>Meetly</Title>
            </Anchor>
            <Group gap="sm">
              {/*
                Всё время в API — UTC. Гостю показываем местное и явно
                подписываем зону, иначе 09:00 UTC выглядит как ошибка.
              */}
              <Badge variant="light" size="sm">
                Время: {timeZoneLabel()}
              </Badge>
              <Anchor component={Link} to="/admin/bookings" size="sm" c="dimmed">
                Админка
              </Anchor>
            </Group>
          </Group>
        </Container>
      </AppShell.Header>

      <AppShell.Main>
        <Container size="md" py="lg">
          <Outlet />
        </Container>
      </AppShell.Main>

      <Container size="md" pb="xl">
        <Text size="xs" c="dimmed" ta="center">
          Запись доступна на ближайшие 14 дней.
        </Text>
      </Container>
    </AppShell>
  );
}
