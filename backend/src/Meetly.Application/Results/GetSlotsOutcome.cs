using Meetly.Domain;

namespace Meetly.Application.Results;

/// <summary>
/// Результат получения слотов для типа события.
/// </summary>
public abstract record GetSlotsOutcome
{
    private GetSlotsOutcome() { }

    /// <summary>Слоты успешно получены.</summary>
    public sealed record Success(
        string EventTypeId,
        int DurationMinutes,
        DateTimeOffset WindowFrom,
        DateTimeOffset WindowTo,
        IReadOnlyList<Slot> Slots) : GetSlotsOutcome;

    /// <summary>Тип события не найден (404).</summary>
    public sealed record EventTypeNotFound(string EventTypeId) : GetSlotsOutcome;
}
