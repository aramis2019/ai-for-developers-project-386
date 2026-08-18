namespace Meetly.Domain;

/// <summary>
/// Полуинтервал времени [Start, End).
/// Используется для бронирований и слотов.
/// </summary>
public readonly record struct TimeInterval(DateTimeOffset Start, DateTimeOffset End)
{
    /// <summary>
    /// Проверяет, пересекаются ли два интервала.
    /// Два полуинтервала [a, b) и [c, d) пересекаются, если a &lt; d и c &lt; b.
    /// </summary>
    public bool Overlaps(TimeInterval other) =>
        Start < other.End && other.Start < End;

    /// <summary>
    /// Длительность интервала.
    /// </summary>
    public TimeSpan Duration => End - Start;

    /// <summary>
    /// Проверяет, что интервал валиден (End > Start).
    /// </summary>
    public bool IsValid => End > Start;

    public override string ToString() => $"[{Start:O}, {End:O})";
}
