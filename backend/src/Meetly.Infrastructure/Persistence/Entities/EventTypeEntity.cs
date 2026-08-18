namespace Meetly.Infrastructure.Persistence.Entities;

/// <summary>
/// EF-сущность типа события. Плоский класс, не пересекается с Domain.EventType —
/// Domain остаётся чистым POCO, а маппинг живёт в этом слое.
/// </summary>
internal sealed class EventTypeEntity
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required int DurationMinutes { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
