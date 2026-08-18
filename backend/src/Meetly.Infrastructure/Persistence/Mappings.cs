using Meetly.Domain;
using Meetly.Infrastructure.Persistence.Entities;

namespace Meetly.Infrastructure.Persistence;

/// <summary>
/// Маппинг между Domain-моделями и EF-сущностями.
/// Живёт только в Infrastructure — Domain не знает про persistence.
/// </summary>
internal static class Mappings
{
    public static EventType ToDomain(this EventTypeEntity entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        DurationMinutes = entity.DurationMinutes,
        CreatedAt = entity.CreatedAt
    };

    public static EventTypeEntity ToEntity(this EventType domain) => new()
    {
        Id = domain.Id,
        Title = domain.Title,
        Description = domain.Description,
        DurationMinutes = domain.DurationMinutes,
        // Npgsql 10 разрешает записывать в timestamptz только DateTimeOffset с Offset=0.
        CreatedAt = domain.CreatedAt.ToUniversalTime()
    };

    public static Booking ToDomain(this BookingEntity entity) => new()
    {
        Id = entity.Id,
        EventTypeId = entity.EventTypeId,
        EventTypeTitle = entity.EventTypeTitle,
        Interval = new TimeInterval(entity.Start, entity.End),
        DurationMinutes = entity.DurationMinutes,
        Guest = new Guest
        {
            Name = entity.GuestName,
            Email = entity.GuestEmail,
            Note = entity.GuestNote
        },
        CreatedAt = entity.CreatedAt
    };

    public static BookingEntity ToEntity(this Booking domain) => new()
    {
        Id = domain.Id,
        EventTypeId = domain.EventTypeId,
        EventTypeTitle = domain.EventTypeTitle,
        // Npgsql 10 разрешает timestamptz только с Offset=0.
        Start = domain.Interval.Start.ToUniversalTime(),
        End = domain.Interval.End.ToUniversalTime(),
        DurationMinutes = domain.DurationMinutes,
        GuestName = domain.Guest.Name,
        GuestEmail = domain.Guest.Email,
        GuestNote = domain.Guest.Note,
        CreatedAt = domain.CreatedAt.ToUniversalTime()
    };
}
