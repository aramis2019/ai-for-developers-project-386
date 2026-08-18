using Meetly.Application.Abstractions;
using Meetly.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Meetly.Infrastructure.Persistence;

/// <summary>
/// EF Core-реализация репозитория типов событий.
/// </summary>
public sealed class EfEventTypeRepository(MeetlyDbContext db) : IEventTypeRepository
{
    public async Task<IReadOnlyList<EventType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await db.EventTypes
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(Mappings.ToDomain).ToList();
    }

    public async Task<EventType?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await db.EventTypes.FindAsync([id], cancellationToken);
        return entity?.ToDomain();
    }

    /// <summary>
    /// Пытается вставить тип события; при конфликте по primary key возвращает false.
    /// </summary>
    public async Task<bool> TryAddAsync(EventType eventType, CancellationToken cancellationToken = default)
    {
        var entity = eventType.ToEntity();
        db.EventTypes.Add(entity);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Убираем провалившийся entry из tracker, чтобы не мешал следующим операциям
            db.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
