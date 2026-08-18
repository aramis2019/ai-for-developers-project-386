import { useState } from "react";
import {
  Button,
  Card,
  Group,
  Modal,
  NumberInput,
  Stack,
  Table,
  Text,
  TextInput,
  Textarea,
  Title,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { notifications } from "@mantine/notifications";
import { useDisclosure } from "@mantine/hooks";
import type { EventTypeCreate } from "../../api/client";
import { useAdminEventTypes, useCreateEventType } from "../../api/queries";
import { constraints } from "../../api/constraints";
import { describeError, isApiError, toFieldErrors } from "../../api/errors";
import { QueryState } from "../../components/QueryState";
import { formatDateTime, formatDuration } from "../../lib/datetime";

/**
 * `/admin/event-types` — типы событий и создание нового.
 *
 * Редактирования и удаления в контракте текущей версии нет
 * (contracts/docs/domain.md, раздел 5), поэтому таблица только для чтения.
 */
export function AdminEventTypesPage() {
  const { data, isPending, error } = useAdminEventTypes();
  const [opened, { open, close }] = useDisclosure(false);

  return (
    <Stack gap="lg">
      <Group justify="space-between" align="flex-start">
        <div>
          <Title order={1}>Типы событий</Title>
          <Text c="dimmed" mt={4}>
            Определяют длительность встречи и то, какие слоты увидит гость.
          </Text>
        </div>
        <Button onClick={open}>Создать</Button>
      </Group>

      <QueryState
        isPending={isPending}
        error={error}
        isEmpty={data?.length === 0}
        emptyTitle="Типов событий пока нет"
        emptyText="Создайте первый — он сразу появится на публичной странице."
      >
        <Card withBorder radius="md" padding={0}>
          <Table verticalSpacing="sm" horizontalSpacing="md">
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Идентификатор</Table.Th>
                <Table.Th>Название</Table.Th>
                <Table.Th w={120}>Длительность</Table.Th>
                <Table.Th w={170}>Создан</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {data?.map((eventType) => (
                <Table.Tr key={eventType.id}>
                  <Table.Td>
                    <Text size="sm" ff="monospace">
                      {eventType.id}
                    </Text>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">{eventType.title}</Text>
                    {eventType.description && (
                      <Text size="xs" c="dimmed" lineClamp={2}>
                        {eventType.description}
                      </Text>
                    )}
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">{formatDuration(eventType.durationMinutes)}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm" c="dimmed">
                      {formatDateTime(eventType.createdAt)}
                    </Text>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Card>
      </QueryState>

      <Modal opened={opened} onClose={close} title="Новый тип события" centered>
        <CreateEventTypeForm onCreated={close} />
      </Modal>
    </Stack>
  );
}

function CreateEventTypeForm({ onCreated }: { onCreated: () => void }) {
  const createEventType = useCreateEventType();
  const [serverErrors, setServerErrors] = useState<Record<string, string>>({});
  const { eventType: limits } = constraints;

  const form = useForm<EventTypeCreate>({
    initialValues: { id: "", title: "", description: "", durationMinutes: 30 },
    validate: {
      id: (value) => {
        if (!value) return "Укажите идентификатор";
        if (value.length > limits.idMaxLength) return `Не длиннее ${limits.idMaxLength} символов`;
        if (!limits.idPattern.test(value)) {
          return "Только строчная латиница, цифры и дефис: intro-call";
        }
        return null;
      },
      title: (value) => {
        if (value.trim().length < limits.titleMinLength) return "Укажите название";
        if (value.length > limits.titleMaxLength) return `Не длиннее ${limits.titleMaxLength} символов`;
        return null;
      },
      description: (value) =>
        value.length > limits.descriptionMaxLength
          ? `Не длиннее ${limits.descriptionMaxLength} символов`
          : null,
      durationMinutes: (value) => {
        if (value < limits.durationMin || value > limits.durationMax) {
          return `От ${limits.durationMin} до ${limits.durationMax} минут`;
        }
        return null;
      },
    },
  });

  const handleSubmit = form.onSubmit((values) => {
    setServerErrors({});
    createEventType.mutate(values, {
      onSuccess: (created) => {
        notifications.show({
          color: "green",
          title: "Тип события создан",
          message: `${created.title} — ${formatDuration(created.durationMinutes)}`,
        });
        form.reset();
        onCreated();
      },
      onError: (error) => {
        // Идентификатор задаёт владелец, поэтому конфликт — ошибка конкретного
        // поля, а не общий сбой формы (ADR 0002).
        if (isApiError(error) && error.code === "EVENT_TYPE_ALREADY_EXISTS") {
          form.setFieldError("id", "Такой идентификатор уже занят");
          return;
        }

        const fields = toFieldErrors(error);
        setServerErrors(fields);
        if (Object.keys(fields).length === 0) {
          notifications.show({
            color: "red",
            title: "Не удалось создать",
            message: describeError(error),
          });
        }
      },
    });
  });

  return (
    <form onSubmit={handleSubmit}>
      <Stack gap="sm">
        <TextInput
          label="Идентификатор"
          description="Попадёт в публичную ссылку: /book/intro-call"
          placeholder="intro-call"
          withAsterisk
          maxLength={limits.idMaxLength}
          {...form.getInputProps("id")}
          error={serverErrors["id"] ?? form.errors["id"]}
        />

        <TextInput
          label="Название"
          placeholder="Знакомство"
          withAsterisk
          maxLength={limits.titleMaxLength}
          {...form.getInputProps("title")}
          error={serverErrors["title"] ?? form.errors["title"]}
        />

        <Textarea
          label="Описание"
          description="Что гость увидит на странице выбора"
          autosize
          minRows={2}
          maxRows={6}
          maxLength={limits.descriptionMaxLength}
          {...form.getInputProps("description")}
          error={serverErrors["description"] ?? form.errors["description"]}
        />

        <NumberInput
          label="Длительность, минут"
          withAsterisk
          min={limits.durationMin}
          max={limits.durationMax}
          step={5}
          {...form.getInputProps("durationMinutes")}
          error={serverErrors["durationMinutes"] ?? form.errors["durationMinutes"]}
        />

        <Button type="submit" loading={createEventType.isPending} mt="xs">
          Создать
        </Button>
      </Stack>
    </form>
  );
}
