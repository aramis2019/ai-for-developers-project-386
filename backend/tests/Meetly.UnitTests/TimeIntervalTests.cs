using Meetly.Domain;

namespace Meetly.UnitTests;

public class TimeIntervalTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Overlaps_SameInterval_ReturnsTrue()
    {
        var interval = new TimeInterval(BaseTime, BaseTime.AddMinutes(30));
        
        Assert.True(interval.Overlaps(interval));
    }

    [Fact]
    public void Overlaps_AdjacentIntervals_ReturnsFalse()
    {
        // [09:00, 09:30) и [09:30, 10:00) — не пересекаются (полуинтервалы)
        var a = new TimeInterval(BaseTime, BaseTime.AddMinutes(30));
        var b = new TimeInterval(BaseTime.AddMinutes(30), BaseTime.AddMinutes(60));
        
        Assert.False(a.Overlaps(b));
        Assert.False(b.Overlaps(a));
    }

    [Fact]
    public void Overlaps_PartialOverlap_ReturnsTrue()
    {
        // [09:00, 09:45) и [09:30, 10:00) — пересекаются
        var a = new TimeInterval(BaseTime, BaseTime.AddMinutes(45));
        var b = new TimeInterval(BaseTime.AddMinutes(30), BaseTime.AddMinutes(60));
        
        Assert.True(a.Overlaps(b));
        Assert.True(b.Overlaps(a));
    }

    [Fact]
    public void Overlaps_ContainedInterval_ReturnsTrue()
    {
        // [09:00, 10:00) содержит [09:15, 09:45)
        var outer = new TimeInterval(BaseTime, BaseTime.AddMinutes(60));
        var inner = new TimeInterval(BaseTime.AddMinutes(15), BaseTime.AddMinutes(45));
        
        Assert.True(outer.Overlaps(inner));
        Assert.True(inner.Overlaps(outer));
    }

    [Fact]
    public void Overlaps_DisjointIntervals_ReturnsFalse()
    {
        // [09:00, 09:30) и [10:00, 10:30) — не пересекаются
        var a = new TimeInterval(BaseTime, BaseTime.AddMinutes(30));
        var b = new TimeInterval(BaseTime.AddMinutes(60), BaseTime.AddMinutes(90));
        
        Assert.False(a.Overlaps(b));
        Assert.False(b.Overlaps(a));
    }

    [Fact]
    public void Duration_ReturnsCorrectValue()
    {
        var interval = new TimeInterval(BaseTime, BaseTime.AddMinutes(45));
        
        Assert.Equal(TimeSpan.FromMinutes(45), interval.Duration);
    }

    [Fact]
    public void IsValid_ValidInterval_ReturnsTrue()
    {
        var interval = new TimeInterval(BaseTime, BaseTime.AddMinutes(30));
        
        Assert.True(interval.IsValid);
    }

    [Fact]
    public void IsValid_ZeroLengthInterval_ReturnsFalse()
    {
        var interval = new TimeInterval(BaseTime, BaseTime);
        
        Assert.False(interval.IsValid);
    }

    [Fact]
    public void IsValid_ReversedInterval_ReturnsFalse()
    {
        var interval = new TimeInterval(BaseTime.AddMinutes(30), BaseTime);
        
        Assert.False(interval.IsValid);
    }
}
