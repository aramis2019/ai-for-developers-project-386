namespace Meetly.Domain;

/// <summary>
/// Тип события (вид брони), который владелец предлагает гостям.
/// </summary>
public sealed record EventType
{
    /// <summary>
    /// Идентификатор, заданный владельцем при создании.
    /// Slug в нижнем регистре: ^[a-z0-9]+(?:-[a-z0-9]+)*$
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Название, которое видит гость.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Описание встречи: о чём она и как проходит.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Длительность встречи в минутах (5–480).
    /// </summary>
    public required int DurationMinutes { get; init; }

    /// <summary>
    /// Момент создания типа события, UTC.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Длительность как TimeSpan.
    /// </summary>
    public TimeSpan Duration => TimeSpan.FromMinutes(DurationMinutes);
}
