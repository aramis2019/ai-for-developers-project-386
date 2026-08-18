using Meetly.Domain;

namespace Meetly.UnitTests;

public class SlotRulesTests
{
    private static readonly WorkingHours DefaultWorkingHours = new(new TimeOnly(9, 0), new TimeOnly(18, 0));
    private static readonly DateTimeOffset BaseTime = new(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(9, 0, true)]   // 09:00 — выровнено
    [InlineData(9, 30, true)]  // 09:30 — выровнено
    [InlineData(10, 0, true)]  // 10:00 — выровнено
    [InlineData(17, 30, true)] // 17:30 — выровнено
    [InlineData(9, 15, false)] // 09:15 — не выровнено
    [InlineData(9, 45, false)] // 09:45 — не выровнено
    [InlineData(8, 30, false)] // 08:30 — до начала рабочего дня
    public void IsAligned_ChecksAlignment(int hour, int minute, bool expected)
    {
        var start = new DateTimeOffset(2026, 8, 17, hour, minute, 0, TimeSpan.Zero);
        
        var result = SlotRules.IsAligned(start, DefaultWorkingHours, slotStepMinutes: 30);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(9, 0, 30, true)]   // 09:00-09:30 — помещается
    [InlineData(17, 30, 30, true)] // 17:30-18:00 — помещается (ровно до конца)
    [InlineData(17, 30, 60, false)] // 17:30-18:30 — выходит за рабочие часы
    [InlineData(17, 0, 60, true)]  // 17:00-18:00 — помещается
    [InlineData(8, 30, 30, false)] // 08:30-09:00 — до начала рабочего дня
    [InlineData(9, 0, 540, true)]  // 09:00-18:00 — весь рабочий день (9 часов = 540 мин)
    [InlineData(9, 0, 541, false)] // 09:00-18:01 — на 1 минуту больше
    public void FitsWorkingHours_ChecksBounds(int hour, int minute, int durationMinutes, bool expected)
    {
        var start = new DateTimeOffset(2026, 8, 17, hour, minute, 0, TimeSpan.Zero);
        var duration = TimeSpan.FromMinutes(durationMinutes);
        
        var result = SlotRules.FitsWorkingHours(start, duration, DefaultWorkingHours);
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsWithinWindow_SlotInside_ReturnsTrue()
    {
        var windowStart = BaseTime;
        var windowEnd = BaseTime.AddDays(14);
        var slotStart = BaseTime.AddDays(1);
        var slotEnd = slotStart.AddMinutes(30);

        Assert.True(SlotRules.IsWithinWindow(slotStart, slotEnd, windowStart, windowEnd));
    }

    [Fact]
    public void IsWithinWindow_SlotStartsBeforeWindow_ReturnsFalse()
    {
        var windowStart = BaseTime;
        var windowEnd = BaseTime.AddDays(14);
        var slotStart = BaseTime.AddMinutes(-30);
        var slotEnd = BaseTime;

        Assert.False(SlotRules.IsWithinWindow(slotStart, slotEnd, windowStart, windowEnd));
    }

    [Fact]
    public void IsWithinWindow_SlotEndsAfterWindow_ReturnsFalse()
    {
        var windowStart = BaseTime;
        var windowEnd = BaseTime.AddDays(14);
        var slotStart = windowEnd.AddMinutes(-15);
        var slotEnd = windowEnd.AddMinutes(15);

        Assert.False(SlotRules.IsWithinWindow(slotStart, slotEnd, windowStart, windowEnd));
    }

    [Fact]
    public void IsWithinWindow_SlotAtWindowEnd_ReturnsTrue()
    {
        var windowStart = BaseTime;
        var windowEnd = BaseTime.AddDays(14);
        var slotStart = windowEnd.AddMinutes(-30);
        var slotEnd = windowEnd;

        Assert.True(SlotRules.IsWithinWindow(slotStart, slotEnd, windowStart, windowEnd));
    }

    [Fact]
    public void OverlapsAny_NoOverlap_ReturnsFalse()
    {
        var interval = new TimeInterval(BaseTime, BaseTime.AddMinutes(30));
        var busy = new[] 
        { 
            new TimeInterval(BaseTime.AddMinutes(30), BaseTime.AddMinutes(60)),
            new TimeInterval(BaseTime.AddMinutes(60), BaseTime.AddMinutes(90))
        };

        Assert.False(SlotRules.OverlapsAny(interval, busy));
    }

    [Fact]
    public void OverlapsAny_OverlapsOne_ReturnsTrue()
    {
        var interval = new TimeInterval(BaseTime, BaseTime.AddMinutes(45));
        var busy = new[] 
        { 
            new TimeInterval(BaseTime.AddMinutes(30), BaseTime.AddMinutes(60)),
            new TimeInterval(BaseTime.AddMinutes(90), BaseTime.AddMinutes(120))
        };

        Assert.True(SlotRules.OverlapsAny(interval, busy));
    }

    [Fact]
    public void OverlapsAny_EmptyBusy_ReturnsFalse()
    {
        var interval = new TimeInterval(BaseTime, BaseTime.AddMinutes(30));

        Assert.False(SlotRules.OverlapsAny(interval, []));
    }
}
