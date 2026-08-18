namespace Meetly.Domain;

/// <summary>
/// Чистые правила валидации слотов.
/// </summary>
public static class SlotRules
{
    /// <summary>
    /// Проверяет, выровнен ли start по шагу сетки относительно начала рабочего дня.
    /// </summary>
    public static bool IsAligned(DateTimeOffset start, WorkingHours workingHours, int slotStepMinutes)
    {
        var timeOfDay = TimeOnly.FromTimeSpan(start.TimeOfDay);
        
        // start должен быть >= workingHours.Start
        if (timeOfDay < workingHours.Start) return false;
        
        // Смещение от начала рабочего дня должно быть кратно шагу
        var offsetFromStart = timeOfDay - workingHours.Start;
        var stepSpan = TimeSpan.FromMinutes(slotStepMinutes);
        
        return offsetFromStart.Ticks % stepSpan.Ticks == 0;
    }

    /// <summary>
    /// Проверяет, помещается ли встреча [start, start + duration) в рабочие часы того же дня.
    /// </summary>
    public static bool FitsWorkingHours(DateTimeOffset start, TimeSpan duration, WorkingHours workingHours)
    {
        var timeOfDay = TimeOnly.FromTimeSpan(start.TimeOfDay);
        return workingHours.FitsInterval(timeOfDay, duration);
    }

    /// <summary>
    /// Проверяет, что слот целиком внутри окна записи [windowStart, windowEnd).
    /// </summary>
    public static bool IsWithinWindow(DateTimeOffset slotStart, DateTimeOffset slotEnd, DateTimeOffset windowStart, DateTimeOffset windowEnd)
    {
        return slotStart >= windowStart && slotEnd <= windowEnd;
    }

    /// <summary>
    /// Проверяет, что start не в прошлом относительно now.
    /// </summary>
    public static bool IsNotInPast(DateTimeOffset start, DateTimeOffset now)
    {
        return start >= now;
    }

    /// <summary>
    /// Проверяет, пересекается ли интервал с любым из занятых интервалов.
    /// </summary>
    public static bool OverlapsAny(TimeInterval interval, IEnumerable<TimeInterval> busyIntervals)
    {
        return busyIntervals.Any(busy => interval.Overlaps(busy));
    }
}
