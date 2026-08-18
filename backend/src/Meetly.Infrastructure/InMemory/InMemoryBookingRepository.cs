using Meetly.Application.Abstractions;
using Meetly.Domain;

namespace Meetly.Infrastructure.InMemory;

/// <summary>
/// In-memory реализация репозитория бронирований.
/// Ключевая операция TryAddAsync — атомарна: проверка пересечения + вставка под lock.
/// Это обеспечивает инвариант ADR 0001 (сквозная занятость) в одном процессе.
/// </summary>
public sealed class InMemoryBookingRepository(InMemoryStore store) : IBookingRepository
{
    public Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Booking> result = store.Bookings.Values.OrderBy(b => b.Start).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Booking>> GetUpcomingAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Booking> result = store.Bookings.Values
            .Where(b => b.Start >= now)
            .OrderBy(b => b.Start)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<TimeInterval>> GetAllIntervalsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TimeInterval> result = store.Bookings.Values.Select(b => b.Interval).ToList();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Атомарная проверка и добавление бронирования.
    /// Гарантирует, что никакие два бронирования не пересекаются по времени.
    /// </summary>
    public Task<AddBookingResult> TryAddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        lock (store.Lock)
        {
            var overlaps = store.Bookings.Values.Any(existing =>
                existing.Interval.Overlaps(booking.Interval));

            if (overlaps)
            {
                return Task.FromResult(AddBookingResult.Conflict);
            }

            store.Bookings.TryAdd(booking.Id, booking);
            return Task.FromResult(AddBookingResult.Added);
        }
    }
}
