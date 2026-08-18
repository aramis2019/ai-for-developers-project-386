using Meetly.Application.Results;

namespace Meetly.Api.Errors;

/// <summary>
/// Хелперы для формирования ответов с ошибками.
/// Формат тела: { code, message, details? } — по ADR 0002.
/// 
/// NB: Сгенерированный ErrorCode — пустой record (NSwag не смог отобразить
/// открытый union в enum), поэтому тело собираем анонимным объектом.
/// </summary>
public static class ErrorResults
{
    /// <summary>
    /// 400 BAD_REQUEST — тело не разобрано, неверный формат.
    /// </summary>
    public static IResult BadRequest(string message, object? details = null) =>
        Results.Json(new { code = "BAD_REQUEST", message, details }, statusCode: 400);

    /// <summary>
    /// 404 EVENT_TYPE_NOT_FOUND — тип события не существует.
    /// </summary>
    public static IResult EventTypeNotFound(string eventTypeId) =>
        Results.Json(
            new { code = "EVENT_TYPE_NOT_FOUND", message = $"Тип события '{eventTypeId}' не найден." },
            statusCode: 404);

    /// <summary>
    /// 409 EVENT_TYPE_ALREADY_EXISTS — id типа события занят.
    /// </summary>
    public static IResult EventTypeAlreadyExists(string eventTypeId) =>
        Results.Json(
            new { code = "EVENT_TYPE_ALREADY_EXISTS", message = $"Тип события с id '{eventTypeId}' уже существует." },
            statusCode: 409);

    /// <summary>
    /// 409 SLOT_ALREADY_BOOKED — интервал пересекается с существующей бронью.
    /// </summary>
    public static IResult SlotAlreadyBooked() =>
        Results.Json(
            new { code = "SLOT_ALREADY_BOOKED", message = "Это время уже занято другой встречей." },
            statusCode: 409);

    /// <summary>
    /// 422 SLOT_NOT_ALIGNED — start не выровнен по шагу сетки.
    /// </summary>
    public static IResult SlotNotAligned() =>
        Results.Json(
            new { code = "SLOT_NOT_ALIGNED", message = "Время начала не выровнено по сетке слотов." },
            statusCode: 422);

    /// <summary>
    /// 422 SLOT_OUT_OF_WINDOW — слот вне окна 14 суток или в прошлом.
    /// </summary>
    public static IResult SlotOutOfWindow() =>
        Results.Json(
            new { code = "SLOT_OUT_OF_WINDOW", message = "Слот находится вне окна записи или в прошлом." },
            statusCode: 422);

    /// <summary>
    /// 422 SLOT_OUTSIDE_WORKING_HOURS — встреча не помещается в рабочие часы.
    /// </summary>
    public static IResult SlotOutsideWorkingHours() =>
        Results.Json(
            new { code = "SLOT_OUTSIDE_WORKING_HOURS", message = "Встреча не помещается в рабочие часы." },
            statusCode: 422);

    /// <summary>
    /// 422 VALIDATION_FAILED — нарушены доменные правила.
    /// </summary>
    public static IResult ValidationFailed(string message, object? details = null) =>
        Results.Json(new { code = "VALIDATION_FAILED", message, details }, statusCode: 422);

    /// <summary>
    /// Маппинг BookingErrorCode в IResult.
    /// </summary>
    public static IResult FromBookingError(BookingErrorCode code, string message) => code switch
    {
        BookingErrorCode.EventTypeNotFound => Results.Json(
            new { code = "EVENT_TYPE_NOT_FOUND", message },
            statusCode: 404),
        
        BookingErrorCode.SlotAlreadyBooked => Results.Json(
            new { code = "SLOT_ALREADY_BOOKED", message },
            statusCode: 409),
        
        BookingErrorCode.SlotNotAligned => Results.Json(
            new { code = "SLOT_NOT_ALIGNED", message },
            statusCode: 422),
        
        BookingErrorCode.SlotOutOfWindow => Results.Json(
            new { code = "SLOT_OUT_OF_WINDOW", message },
            statusCode: 422),
        
        BookingErrorCode.SlotOutsideWorkingHours => Results.Json(
            new { code = "SLOT_OUTSIDE_WORKING_HOURS", message },
            statusCode: 422),
        
        _ => Results.Json(
            new { code = "VALIDATION_FAILED", message },
            statusCode: 422)
    };
}
