import { Anchor, AppShell, Badge, Container, Group, Skeleton, Text, Title } from "@mantine/core";
import { Link, NavLink, Outlet } from "react-router-dom";
import { useOwnerProfile } from "../api/queries";
import { timeZoneLabel } from "../lib/datetime";

/**
 * Админская часть. Регистрации и авторизации в системе нет: сервер сам
 * подставляет единственный профиль владельца календаря.
 *
 * Шапка показывает настройки из `GET /api/admin/profile` — рабочие часы,
 * шаг сетки и глубину окна записи. Именно они определяют, какие слоты
 * увидит гость.
 */
export function AdminLayout() {
  const { data: profile, isPending } = useOwnerProfile();

  return (
    <AppShell header={{ height: 92 }} padding="md">
      <AppShell.Header>
        <Container size="lg" h="100%">
          <Group h="100%" justify="space-between" align="center">
            <div>
              <Group gap="xs" align="baseline">
                <Title order={3}>Meetly</Title>
                <Text size="sm" c="dimmed">
                  админка
                </Text>
              </Group>

              {isPending ? (
                <Skeleton height={12} width={280} mt={6} />
              ) : profile ? (
                <Group gap={6} mt={4}>
                  <Text size="xs" c="dimmed">
                    {profile.name}
                  </Text>
                  <Badge size="xs" variant="light">
                    {profile.workingHours.start}–{profile.workingHours.end} {profile.timeZone}
                  </Badge>
                  <Badge size="xs" variant="light">
                    шаг {profile.slotStepMinutes} мин
                  </Badge>
                  <Badge size="xs" variant="light">
                    окно {profile.bookingWindowDays} дней
                  </Badge>
                </Group>
              ) : null}
            </div>

            <Group gap="lg">
              <Anchor component={NavLink} to="/admin/bookings" size="sm">
                Встречи
              </Anchor>
              <Anchor component={NavLink} to="/admin/event-types" size="sm">
                Типы событий
              </Anchor>
              <Anchor component={Link} to="/" size="sm" c="dimmed">
                На сайт
              </Anchor>
            </Group>
          </Group>
        </Container>
      </AppShell.Header>

      <AppShell.Main>
        <Container size="lg" py="lg">
          <Outlet />
        </Container>
        <Container size="lg" pb="xl">
          <Text size="xs" c="dimmed">
            Расписание владельца — в {profile?.timeZone ?? "UTC"}. Время встреч ниже показано
            в вашем поясе ({timeZoneLabel()}).
          </Text>
        </Container>
      </AppShell.Main>
    </AppShell>
  );
}
