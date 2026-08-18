namespace Meetly.Domain;

/// <summary>
/// Слот — вычисляемый интервал-кандидат для встречи.
/// Отдельно не хранится: сетка строится на лету.
/// </summary>
public readonly record struct Slot(TimeInterval Interval)
{
    /// <summary>
    /// Начало слота, UTC. Выровнено по шагу сетки.
    /// </summary>
    public DateTimeOffset Start => Interval.Start;

    /// <summary>
    /// Конец слота, UTC. Равен Start + длительность типа события.
    /// </summary>
    public DateTimeOffset End => Interval.End;

    /// <summary>
    /// Создаёт слот из начала и длительности.
    /// </summary>
    public static Slot FromStartAndDuration(DateTimeOffset start, TimeSpan duration) =>
        new(new TimeInterval(start, start + duration));

    public override string ToString() => Interval.ToString();
}
