using Meetly.Contracts;
using DomainModels = Meetly.Domain;

namespace Meetly.Api.Mapping;

/// <summary>
/// Маппинг между доменными моделями и DTO контракта.
/// Живёт только в слое Api — DTO не протекают в Application.
/// </summary>
public static class DtoMapping
{
    public static Owner ToDto(DomainModels.OwnerProfile profile) => new()
    {
        Id = profile.Id,
        Name = profile.Name,
        Email = profile.Email,
        TimeZone = OwnerTimeZone.UTC,
        WorkingHours = new WorkingHours
        {
            Start = profile.WorkingHours.Start.ToString("HH:mm"),
            End = profile.WorkingHours.End.ToString("HH:mm")
        },
        SlotStepMinutes = profile.SlotStepMinutes,
        BookingWindowDays = profile.BookingWindowDays
    };

    public static EventType ToDto(DomainModels.EventType eventType) => new()
    {
        Id = eventType.Id,
        Title = eventType.Title,
        Description = eventType.Description,
        DurationMinutes = eventType.DurationMinutes,
        CreatedAt = eventType.CreatedAt
    };

    public static EventTypeList ToDto(IEnumerable<DomainModels.EventType> eventTypes) => new()
    {
        Items = eventTypes.Select(ToDto).ToList()
    };

    public static PublicEventType ToPublicDto(DomainModels.EventType eventType) => new()
    {
        Id = eventType.Id,
        Title = eventType.Title,
        Description = eventType.Description,
        DurationMinutes = eventType.DurationMinutes
    };

    public static PublicEventTypeList ToPublicDto(IEnumerable<DomainModels.EventType> eventTypes) => new()
    {
        Items = eventTypes.Select(ToPublicDto).ToList()
    };

    public static Slot ToDto(DomainModels.Slot slot) => new()
    {
        Start = slot.Start,
        End = slot.End
    };

    public static SlotsPage ToDto(
        string eventTypeId,
        int durationMinutes,
        DateTimeOffset windowFrom,
        DateTimeOffset windowTo,
        IEnumerable<DomainModels.Slot> slots) => new()
    {
        EventTypeId = eventTypeId,
        DurationMinutes = durationMinutes,
        TimeZone = SlotsPageTimeZone.UTC,
        Window = new BookingWindow
        {
            From = windowFrom,
            To = windowTo
        },
        Slots = slots.Select(ToDto).ToList()
    };

    public static Guest ToDto(DomainModels.Guest guest) => new()
    {
        Name = guest.Name,
        Email = guest.Email,
        Note = guest.Note
    };

    public static DomainModels.Guest ToDomain(Guest guest) => new()
    {
        Name = guest.Name,
        Email = guest.Email,
        Note = guest.Note
    };

    public static Booking ToDto(DomainModels.Booking booking) => new()
    {
        Id = booking.Id,
        EventTypeId = booking.EventTypeId,
        EventTypeTitle = booking.EventTypeTitle,
        Start = booking.Start,
        End = booking.End,
        DurationMinutes = booking.DurationMinutes,
        Guest = ToDto(booking.Guest),
        CreatedAt = booking.CreatedAt
    };

    public static BookingList ToDto(IEnumerable<DomainModels.Booking> bookings) => new()
    {
        Items = bookings.Select(ToDto).ToList()
    };
}
