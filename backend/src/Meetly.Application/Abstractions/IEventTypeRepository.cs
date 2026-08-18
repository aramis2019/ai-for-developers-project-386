using Meetly.Domain;

namespace Meetly.Application.Abstractions;

/// <summary>
/// Репозиторий типов событий.
/// </summary>
public interface IEventTypeRepository
{
    /// <summary>
    /// Возвращает все типы событий.
    /// </summary>
    Task<IReadOnlyList<EventType>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ищет тип события по идентификатору.
    /// </summary>
    Task<EventType?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Пытается добавить тип события.
    /// Возвращает false, если тип с таким id уже существует.
    /// </summary>
    Task<bool> TryAddAsync(EventType eventType, CancellationToken cancellationToken = default);
}
