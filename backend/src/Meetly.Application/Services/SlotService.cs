using Meetly.Application.Abstractions;
using Meetly.Application.Results;
using Meetly.Domain;

namespace Meetly.Application.Services;

/// <summary>
/// Сервис получения свободных слотов.
/// </summary>
public sealed class SlotService(
    IEventTypeRepository eventTypeRepository,
    IBookingRepository bookingRepository,
    IOwnerProfileProvider profileProvider,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Возвращает свободные слоты для типа события.
    /// </summary>
    public async Task<GetSlotsOutcome> GetSlotsAsync(string eventTypeId, CancellationToken cancellationToken = default)
    {
        var eventType = await eventTypeRepository.FindByIdAsync(eventTypeId, cancellationToken);
        if (eventType is null)
        {
            return new GetSlotsOutcome.EventTypeNotFound(eventTypeId);
        }

        var profile = profileProvider.GetProfile();
        var now = timeProvider.GetUtcNow();
        var windowEnd = now.AddDays(profile.BookingWindowDays);
        var busyIntervals = await bookingRepository.GetAllIntervalsAsync(cancellationToken);

        var slots = SlotGrid.Generate(profile, eventType.Duration, now, busyIntervals);

        return new GetSlotsOutcome.Success(
            eventTypeId,
            eventType.DurationMinutes,
            now,
            windowEnd,
            slots);
    }
}
