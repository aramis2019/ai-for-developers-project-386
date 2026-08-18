namespace Meetly.Domain;

/// <summary>
/// Подтверждённая запись гостя на конкретный интервал.
/// </summary>
public sealed record Booking
{
    /// <summary>
    /// Идентификатор брони, выданный сервером.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Тип события, на который записался гость.
    /// </summary>
    public required string EventTypeId { get; init; }

    /// <summary>
    /// Название типа события — денормализовано для отображения без JOIN.
    /// </summary>
    public required string EventTypeTitle { get; init; }

    /// <summary>
    /// Интервал встречи [Start, End).
    /// </summary>
    public required TimeInterval Interval { get; init; }

    /// <summary>
    /// Длительность встречи в минутах, зафиксированная при бронировании.
    /// </summary>
    public required int DurationMinutes { get; init; }

    /// <summary>
    /// Контактные данные гостя.
    /// </summary>
    public required Guest Guest { get; init; }

    /// <summary>
    /// Момент создания брони, UTC.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Начало встречи (shortcut).
    /// </summary>
    public DateTimeOffset Start => Interval.Start;

    /// <summary>
    /// Конец встречи (shortcut).
    /// </summary>
    public DateTimeOffset End => Interval.End;
}
