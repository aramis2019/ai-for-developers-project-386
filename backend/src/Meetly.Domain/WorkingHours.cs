namespace Meetly.Domain;

/// <summary>
/// Рабочие часы владельца календаря в UTC.
/// Слоты формируются только внутри этого промежутка.
/// </summary>
public readonly record struct WorkingHours(TimeOnly Start, TimeOnly End)
{
    /// <summary>
    /// Проверяет, что рабочие часы валидны (End > Start).
    /// </summary>
    public bool IsValid => End > Start;

    /// <summary>
    /// Проверяет, помещается ли интервал [start, start + duration) целиком
    /// в рабочие часы одного дня.
    /// </summary>
    public bool FitsInterval(TimeOnly start, TimeSpan duration)
    {
        if (start < Start) return false;
        
        var end = start.Add(duration);
        // TimeOnly.Add может перейти на следующий день - это невалидно
        // Проверяем, что end >= start (не перешли через полночь) и end <= End
        return end >= start && end <= End;
    }

    /// <summary>
    /// Длительность рабочего дня.
    /// </summary>
    public TimeSpan Duration => End - Start;

    public override string ToString() => $"{Start:HH:mm}-{End:HH:mm}";
}
