using System.Collections.Concurrent;
using Meetly.Domain;

namespace Meetly.Infrastructure.InMemory;

/// <summary>
/// Общее in-memory хранилище данных.
/// Все операции синхронизируются через Lock.
/// </summary>
public sealed class InMemoryStore
{
    /// <summary>
    /// Типы событий: id → EventType.
    /// </summary>
    public ConcurrentDictionary<string, EventType> EventTypes { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Бронирования: id → Booking.
    /// </summary>
    public ConcurrentDictionary<Guid, Booking> Bookings { get; } = new();

    /// <summary>
    /// Блокировка для атомарных операций (проверка пересечения + вставка).
    /// </summary>
    public object Lock { get; } = new();
}
