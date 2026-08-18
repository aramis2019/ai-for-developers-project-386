using Meetly.Domain;

namespace Meetly.Application.Abstractions;

/// <summary>
/// Результат попытки добавить бронирование.
/// </summary>
public enum AddBookingResult
{
    /// <summary>Бронирование успешно создано.</summary>
    Added,

    /// <summary>Интервал пересекается с существующей бронью.</summary>
    Conflict
}

/// <summary>
/// Репозиторий бронирований.
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Возвращает все бронирования, отсортированные по start.
    /// </summary>
    Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает предстоящие бронирования (start >= now), отсортированные по start.
    /// </summary>
    Task<IReadOnlyList<Booking>> GetUpcomingAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает интервалы всех бронирований (для построения сетки слотов).
    /// </summary>
    Task<IReadOnlyList<TimeInterval>> GetAllIntervalsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Пытается добавить бронирование атомарно.
    /// Проверяет, что интервал не пересекается с существующими бронированиями.
    /// Это ключевая операция для ADR 0001 (сквозная занятость).
    /// </summary>
    Task<AddBookingResult> TryAddAsync(Booking booking, CancellationToken cancellationToken = default);
}
