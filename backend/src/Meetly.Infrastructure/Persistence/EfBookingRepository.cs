using Meetly.Application.Abstractions;
using Meetly.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Meetly.Infrastructure.Persistence;

/// <summary>
/// EF Core-реализация репозитория бронирований.
/// Атомарность инварианта ADR 0001 обеспечивает БД:
/// exclusion constraint на generated tstzrange-колонке "during"
/// не даст вставить пересекающийся интервал независимо от типа события.
/// При нарушении Postgres возвращает SqlState 23P01 (exclusion_violation),
/// который мы переводим в AddBookingResult.Conflict.
/// </summary>
public sealed class EfBookingRepository(MeetlyDbContext db) : IBookingRepository
{
    public async Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await db.Bookings
            .OrderBy(b => b.Start)
            .ToListAsync(cancellationToken);

        return entities.Select(Mappings.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Booking>> GetUpcomingAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var entities = await db.Bookings
            .Where(b => b.Start >= now)
            .OrderBy(b => b.Start)
            .ToListAsync(cancellationToken);

        return entities.Select(Mappings.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<TimeInterval>> GetAllIntervalsAsync(CancellationToken cancellationToken = default)
    {
        var pairs = await db.Bookings
            .Select(b => new { b.Start, b.End })
            .ToListAsync(cancellationToken);

        return pairs.Select(p => new TimeInterval(p.Start, p.End)).ToList();
    }

    /// <summary>
    /// Атомарная вставка бронирования. Если exclusion constraint отвергает вставку
    /// (SqlState 23P01) — возвращаем Conflict; для остальных ошибок пробрасываем исключение.
    /// </summary>
    public async Task<AddBookingResult> TryAddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        var entity = booking.ToEntity();
        db.Bookings.Add(entity);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return AddBookingResult.Added;
        }
        catch (DbUpdateException ex) when (IsExclusionViolation(ex))
        {
            db.Entry(entity).State = EntityState.Detached;
            return AddBookingResult.Conflict;
        }
    }

    private static bool IsExclusionViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation };
}
