import { Button, Stack, TextInput, Textarea } from "@mantine/core";
import { useForm } from "@mantine/form";
import { constraints, emailPattern } from "../../api/constraints";
import type { Guest } from "../../api/client";

interface GuestFormProps {
  disabled: boolean;
  submitting: boolean;
  onSubmit: (guest: Guest) => void;
  /** Ошибки полей, пришедшие от сервера в `ErrorBody.details`. */
  serverErrors: Record<string, string>;
}

/**
 * Контактные данные гостя.
 *
 * Клиентская валидация повторяет границы из контракта (см. api/constraints.ts)
 * ради мгновенной обратной связи. Последнее слово всегда за сервером:
 * его `422 VALIDATION_FAILED` раскладывается по полям через `serverErrors`.
 */
export function GuestForm({ disabled, submitting, onSubmit, serverErrors }: GuestFormProps) {
  const { guest } = constraints;

  const form = useForm<Guest>({
    initialValues: { name: "", email: "", note: "" },
    validate: {
      name: (value) => {
        if (value.trim().length < guest.nameMinLength) return "Укажите имя";
        if (value.length > guest.nameMaxLength) return `Не длиннее ${guest.nameMaxLength} символов`;
        return null;
      },
      email: (value) => {
        if (!value.trim()) return "Укажите e-mail";
        if (!emailPattern.test(value)) return "Похоже, в адресе опечатка";
        if (value.length > guest.emailMaxLength) return `Не длиннее ${guest.emailMaxLength} символов`;
        return null;
      },
      note: (value) =>
        (value?.length ?? 0) > guest.noteMaxLength
          ? `Не длиннее ${guest.noteMaxLength} символов`
          : null,
    },
  });

  const handleSubmit = form.onSubmit((values) => {
    onSubmit({
      name: values.name.trim(),
      email: values.email.trim(),
      // Пустой комментарий не отправляем: поле в контракте необязательное.
      ...(values.note?.trim() ? { note: values.note.trim() } : {}),
    });
  });

  return (
    // noValidate отключает встроенную проверку браузера: иначе на `type="email"`
    // Chrome перехватывает отправку раньше Mantine и показывает собственное
    // англоязычное сообщение вместо наших текстов.
    <form onSubmit={handleSubmit} noValidate>
      <Stack gap="sm">
        <TextInput
          label="Имя"
          placeholder="Как к вам обращаться"
          withAsterisk
          disabled={disabled}
          maxLength={guest.nameMaxLength}
          {...form.getInputProps("name")}
          error={serverErrors["guest.name"] ?? form.errors["name"]}
        />

        <TextInput
          label="E-mail"
          placeholder="you@example.com"
          type="email"
          withAsterisk
          disabled={disabled}
          maxLength={guest.emailMaxLength}
          {...form.getInputProps("email")}
          error={serverErrors["guest.email"] ?? form.errors["email"]}
        />

        <Textarea
          label="Комментарий"
          description="Необязательно: тема встречи, вопросы, ссылки"
          autosize
          minRows={2}
          maxRows={6}
          disabled={disabled}
          maxLength={guest.noteMaxLength}
          {...form.getInputProps("note")}
          error={serverErrors["guest.note"] ?? form.errors["note"]}
        />

        <Button type="submit" disabled={disabled} loading={submitting} mt="xs">
          Записаться
        </Button>
      </Stack>
    </form>
  );
}
