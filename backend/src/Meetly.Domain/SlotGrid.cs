namespace Meetly.Domain;

/// <summary>
/// Генератор сетки свободных слотов.
/// Алгоритм из domain.md §3.3.
/// </summary>
public static class SlotGrid
{
    /// <summary>
    /// Строит список свободных слотов для заданного типа события.
    /// </summary>
    /// <param name="profile">Профиль владельца с рабочими часами и шагом сетки.</param>
    /// <param name="eventTypeDuration">Длительность типа события.</param>
    /// <param name="now">Текущий момент (начало окна записи).</param>
    /// <param name="busyIntervals">Интервалы существующих бронирований (все типы событий).</param>
    /// <returns>Список свободных слотов, отсортированный по start.</returns>
    public static IReadOnlyList<Slot> Generate(
        OwnerProfile profile,
        TimeSpan eventTypeDuration,
        DateTimeOffset now,
        IEnumerable<TimeInterval> busyIntervals)
    {
        var busy = busyIntervals.ToList();
        var slots = new List<Slot>();
        
        var windowStart = now;
        var windowEnd = now.AddDays(profile.BookingWindowDays);
        var step = TimeSpan.FromMinutes(profile.SlotStepMinutes);

        // Итерируемся по дням в окне записи
        var currentDate = now.Date;
        var endDate = windowEnd.Date;

        while (currentDate <= endDate)
        {
            // Генерируем слоты для этого дня
            GenerateDaySlots(
                currentDate,
                profile.WorkingHours,
                step,
                eventTypeDuration,
                windowStart,
                windowEnd,
                busy,
                slots);

            currentDate = currentDate.AddDays(1);
        }

        return slots;
    }

    private static void GenerateDaySlots(
        DateTime date,
        WorkingHours workingHours,
        TimeSpan step,
        TimeSpan duration,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        List<TimeInterval> busy,
        List<Slot> slots)
    {
        // Начало рабочего дня в UTC
        var dayStart = new DateTimeOffset(
            date.Year, date.Month, date.Day,
            workingHours.Start.Hour, workingHours.Start.Minute, 0,
            TimeSpan.Zero);

        // Конец рабочего дня в UTC
        var dayEnd = new DateTimeOffset(
            date.Year, date.Month, date.Day,
            workingHours.End.Hour, workingHours.End.Minute, 0,
            TimeSpan.Zero);

        var current = dayStart;

        while (current + duration <= dayEnd)
        {
            var slotInterval = new TimeInterval(current, current + duration);

            // Проверяем все условия:
            // 1. Слот целиком внутри окна записи
            // 2. Слот не в прошлом (start >= windowStart)
            // 3. Слот не пересекается с занятыми интервалами
            if (SlotRules.IsWithinWindow(slotInterval.Start, slotInterval.End, windowStart, windowEnd) &&
                SlotRules.IsNotInPast(slotInterval.Start, windowStart) &&
                !SlotRules.OverlapsAny(slotInterval, busy))
            {
                slots.Add(new Slot(slotInterval));
            }

            current += step;
        }
    }
}
