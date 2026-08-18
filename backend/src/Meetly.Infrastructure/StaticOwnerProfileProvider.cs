using Meetly.Application.Abstractions;
using Meetly.Domain;

namespace Meetly.Infrastructure;

/// <summary>
/// Статичный провайдер профиля владельца.
/// Значения захардкожены — как в исходных заглушках.
/// </summary>
public sealed class StaticOwnerProfileProvider : IOwnerProfileProvider
{
    private static readonly OwnerProfile Profile = new()
    {
        Id = "owner",
        Name = "Анна Смирнова",
        Email = "owner@meetly.local",
        WorkingHours = new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0)),
        SlotStepMinutes = 30,
        BookingWindowDays = 14
    };

    public OwnerProfile GetProfile() => Profile;
}
