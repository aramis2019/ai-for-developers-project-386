using Meetly.Application.Abstractions;
using Meetly.Domain;

namespace Meetly.Infrastructure.InMemory;

/// <summary>
/// In-memory реализация репозитория типов событий.
/// </summary>
public sealed class InMemoryEventTypeRepository(InMemoryStore store) : IEventTypeRepository
{
    public Task<IReadOnlyList<EventType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EventType> result = store.EventTypes.Values.OrderBy(e => e.CreatedAt).ToList();
        return Task.FromResult(result);
    }

    public Task<EventType?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(store.EventTypes.GetValueOrDefault(id));
    }

    public Task<bool> TryAddAsync(EventType eventType, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(store.EventTypes.TryAdd(eventType.Id, eventType));
    }
}
