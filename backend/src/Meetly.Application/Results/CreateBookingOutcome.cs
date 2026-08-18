using Meetly.Domain;

namespace Meetly.Application.Results;

/// <summary>
/// Код ошибки бронирования (для маппинга в HTTP).
/// </summary>
public enum BookingErrorCode
{
    /// <summary>Тип события не найден (404).</summary>
    EventTypeNotFound,
    
    /// <summary>Слот занят (409).</summary>
    SlotAlreadyBooked,
    
    /// <summary>start не выровнен по сетке (422).</summary>
    SlotNotAligned,
    
    /// <summary>Слот вне окна записи или в прошлом (422).</summary>
    SlotOutOfWindow,
    
    /// <summary>Слот не помещается в рабочие часы (422).</summary>
    SlotOutsideWorkingHours
}

/// <summary>
/// Результат создания бронирования.
/// </summary>
public abstract record CreateBookingOutcome
{
    private CreateBookingOutcome() { }

    /// <summary>Бронирование успешно создано.</summary>
    public sealed record Created(Booking Booking) : CreateBookingOutcome;

    /// <summary>Ошибка бронирования.</summary>
    public sealed record Failed(BookingErrorCode Code, string Message) : CreateBookingOutcome;
}
