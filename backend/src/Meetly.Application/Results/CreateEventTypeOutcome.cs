using Meetly.Domain;

namespace Meetly.Application.Results;

/// <summary>
/// Результат создания типа события.
/// </summary>
public abstract record CreateEventTypeOutcome
{
    private CreateEventTypeOutcome() { }

    /// <summary>Тип события успешно создан.</summary>
    public sealed record Created(EventType EventType) : CreateEventTypeOutcome;

    /// <summary>Тип события с таким id уже существует (409).</summary>
    public sealed record AlreadyExists(string Id) : CreateEventTypeOutcome;
}
