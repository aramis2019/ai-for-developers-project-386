using Meetly.Application.Abstractions;
using Meetly.Application.Results;
using Meetly.Domain;
using Microsoft.Extensions.Logging;

namespace Meetly.Application.Services;

/// <summary>
/// Сервис бронирования.
/// </summary>
public sealed class BookingService(
    IEventTypeRepository eventTypeRepository,
    IBookingRepository bookingRepository,
    IOwnerProfileProvider profileProvider,
    TimeProvider timeProvider,
    ILogger<BookingService> logger)
{
    /// <summary>
    /// Возвращает предстоящие бронирования (для админки).
    /// </summary>
    public Task<IReadOnlyList<Booking>> GetUpcomingAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        return bookingRepository.GetUpcomingAsync(now, cancellationToken);
    }

    /// <summary>
    /// Создаёт бронирование.
    /// Порядок проверок по ADR 0002:
    /// 1. Тип события существует? (404)
    /// 2. start выровнен по сетке? (422 SLOT_NOT_ALIGNED)
    /// 3. Помещается в рабочие часы? (422 SLOT_OUTSIDE_WORKING_HOURS)
    /// 4. В пределах окна записи? (422 SLOT_OUT_OF_WINDOW)
    /// 5. Слот свободен? (409 SLOT_ALREADY_BOOKED) — атомарно в репозитории
    /// </summary>
    public async Task<CreateBookingOutcome> CreateAsync(
        string eventTypeId,
        DateTimeOffset start,
        Guest guest,
        CancellationToken cancellationToken = default)
    {
        // Нормализуем к UTC: в API всё UTC (см. domain.md §2), но клиент мог прислать
        // время с локальным offset после JSON-парсинга. Всё дальнейшее оперирует UTC:
        // и alignment по сетке, и рабочие часы, и exclusion constraint в БД.
        start = start.ToUniversalTime();

        // 1. Проверка типа события
        var eventType = await eventTypeRepository.FindByIdAsync(eventTypeId, cancellationToken);
        if (eventType is null)
        {
            return new CreateBookingOutcome.Failed(
                BookingErrorCode.EventTypeNotFound,
                $"Тип события '{eventTypeId}' не найден.");
        }

        var profile = profileProvider.GetProfile();
        var now = timeProvider.GetUtcNow();
        var duration = eventType.Duration;
        var end = start + duration;

        // 2. Проверка выравнивания по сетке
        if (!SlotRules.IsAligned(start, profile.WorkingHours, profile.SlotStepMinutes))
        {
            return new CreateBookingOutcome.Failed(
                BookingErrorCode.SlotNotAligned,
                "Время начала не выровнено по сетке слотов.");
        }

        // 3. Проверка рабочих часов
        if (!SlotRules.FitsWorkingHours(start, duration, profile.WorkingHours))
        {
            return new CreateBookingOutcome.Failed(
                BookingErrorCode.SlotOutsideWorkingHours,
                "Встреча не помещается в рабочие часы.");
        }

        // 4. Проверка окна записи
        var windowEnd = now.AddDays(profile.BookingWindowDays);
        if (!SlotRules.IsWithinWindow(start, end, now, windowEnd))
        {
            return new CreateBookingOutcome.Failed(
                BookingErrorCode.SlotOutOfWindow,
                "Слот находится вне окна записи или в прошлом.");
        }

        // 5. Атомарная проверка и вставка (в EF — через exclusion constraint 23P01,
        //    в InMemory — через lock)
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventTypeId = eventTypeId,
            EventTypeTitle = eventType.Title,
            Interval = new TimeInterval(start, end),
            DurationMinutes = eventType.DurationMinutes,
            Guest = guest,
            CreatedAt = now
        };

        var result = await bookingRepository.TryAddAsync(booking, cancellationToken);
        if (result == AddBookingResult.Conflict)
        {
            logger.LogWarning(
                "Конфликт при бронировании: eventTypeId={EventTypeId}, start={Start}, guest={GuestEmail}",
                eventTypeId, start, guest.Email);

            return new CreateBookingOutcome.Failed(
                BookingErrorCode.SlotAlreadyBooked,
                "Это время уже занято другой встречей.");
        }

        logger.LogInformation(
            "Создано бронирование: id={BookingId}, eventTypeId={EventTypeId}, start={Start}, guest={GuestEmail}",
            booking.Id, eventTypeId, start, guest.Email);

        return new CreateBookingOutcome.Created(booking);
    }
}
