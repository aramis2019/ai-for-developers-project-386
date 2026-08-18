namespace Meetly.Infrastructure.Persistence.Entities;

/// <summary>
/// EF-сущность бронирования. Guest — плоские колонки, интервал — Start/End.
/// В миграции добавлена generated tstzrange-колонка "during" с exclusion constraint.
/// </summary>
internal sealed class BookingEntity
{
    public required Guid Id { get; set; }
    public required string EventTypeId { get; set; }
    public required string EventTypeTitle { get; set; }
    public required DateTimeOffset Start { get; set; }
    public required DateTimeOffset End { get; set; }
    public required int DurationMinutes { get; set; }
    public required string GuestName { get; set; }
    public required string GuestEmail { get; set; }
    public string? GuestNote { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
