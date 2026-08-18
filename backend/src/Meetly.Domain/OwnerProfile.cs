namespace Meetly.Domain;

/// <summary>
/// Профиль владельца календаря — единственный заранее заданный профиль системы.
/// Синглтон, доступен только для чтения.
/// </summary>
public sealed record OwnerProfile
{
    /// <summary>
    /// Стабильный идентификатор профиля.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Отображаемое имя владельца на публичных страницах.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Контактный e-mail владельца.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Рабочие часы, внутри которых строится сетка слотов.
    /// </summary>
    public required WorkingHours WorkingHours { get; init; }

    /// <summary>
    /// Шаг сетки слотов в минутах.
    /// </summary>
    public required int SlotStepMinutes { get; init; }

    /// <summary>
    /// Глубина окна записи в сутках от текущего момента.
    /// </summary>
    public required int BookingWindowDays { get; init; }
}
