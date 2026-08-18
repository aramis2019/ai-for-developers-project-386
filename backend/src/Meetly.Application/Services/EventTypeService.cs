using Meetly.Application.Abstractions;
using Meetly.Application.Results;
using Meetly.Domain;
using Microsoft.Extensions.Logging;

namespace Meetly.Application.Services;

/// <summary>
/// Сервис типов событий.
/// </summary>
public sealed class EventTypeService(
    IEventTypeRepository repository,
    TimeProvider timeProvider,
    ILogger<EventTypeService> logger)
{
    /// <summary>
    /// Возвращает все типы событий (для админки).
    /// </summary>
    public Task<IReadOnlyList<EventType>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    /// <summary>
    /// Возвращает публичный список типов событий (для гостей).
    /// </summary>
    public Task<IReadOnlyList<EventType>> GetPublicAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    /// <summary>
    /// Создаёт новый тип события.
    /// </summary>
    public async Task<CreateEventTypeOutcome> CreateAsync(
        string id,
        string title,
        string description,
        int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        var eventType = new EventType
        {
            Id = id,
            Title = title,
            Description = description,
            DurationMinutes = durationMinutes,
            CreatedAt = timeProvider.GetUtcNow()
        };

        var added = await repository.TryAddAsync(eventType, cancellationToken);
        if (!added)
        {
            logger.LogWarning("Попытка создать тип события с занятым id: {EventTypeId}", id);
            return new CreateEventTypeOutcome.AlreadyExists(id);
        }

        logger.LogInformation(
            "Создан тип события: {EventTypeId}, title={Title}, duration={DurationMinutes}",
            id, title, durationMinutes);

        return new CreateEventTypeOutcome.Created(eventType);
    }
}
