using Meetly.Domain;

namespace Meetly.UnitTests;

public class SlotGridTests
{
    private static readonly OwnerProfile DefaultProfile = new()
    {
        Id = "owner",
        Name = "Анна Смирнова",
        Email = "owner@meetly.local",
        WorkingHours = new WorkingHours(new TimeOnly(9, 0), new TimeOnly(18, 0)),
        SlotStepMinutes = 30,
        BookingWindowDays = 14
    };

    // Фиксированное "сейчас" для детерминированных тестов: 17 августа 2026, 09:00 UTC
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Generate_EmptyBusy_ReturnsAllSlotsInWindow()
    {
        var slots = SlotGrid.Generate(DefaultProfile, TimeSpan.FromMinutes(30), Now, []);

        // За 14 дней, с 09:00 до 18:00, шаг 30 мин, длительность 30 мин:
        // Каждый день: с 09:00 до 17:30 включительно = 18 слотов (09:00, 09:30, ..., 17:30)
        // Но первый день начинается с 09:00 (ровно now), последний день заканчивается в now+14d
        // 14 полных дней * 18 слотов = 252 слота
        Assert.Equal(252, slots.Count);
        
        // Первый слот должен начинаться с now
        Assert.Equal(Now, slots[0].Start);
        
        // Все слоты отсортированы по start
        for (int i = 1; i < slots.Count; i++)
        {
            Assert.True(slots[i].Start > slots[i - 1].Start);
        }
    }

    [Fact]
    public void Generate_WithDuration60_FitsWorkingHours()
    {
        var slots = SlotGrid.Generate(DefaultProfile, TimeSpan.FromMinutes(60), Now, []);

        // При длительности 60 минут последний слот дня в 17:00 (17:00-18:00)
        // С 09:00 до 17:00 с шагом 30 = 17 слотов на день (09:00, 09:30, ..., 17:00)
        // 14 дней * 17 слотов = 238 слотов
        Assert.Equal(238, slots.Count);

        // Каждый слот должен заканчиваться не позже 18:00
        foreach (var slot in slots)
        {
            var endTime = TimeOnly.FromTimeSpan(slot.End.TimeOfDay);
            Assert.True(endTime <= new TimeOnly(18, 0));
        }
    }

    [Fact]
    public void Generate_WithBusySlot_ExcludesOverlapping()
    {
        // Занят слот 09:00-09:30 первого дня
        var busy = new[] { new TimeInterval(Now, Now.AddMinutes(30)) };
        
        var slots = SlotGrid.Generate(DefaultProfile, TimeSpan.FromMinutes(30), Now, busy);

        // Должно быть 252 - 1 = 251 слот
        Assert.Equal(251, slots.Count);
        
        // Первый слот должен быть 09:30, не 09:00
        Assert.Equal(Now.AddMinutes(30), slots[0].Start);
    }

    [Fact]
    public void Generate_BusySlotEatsMultipleSlots_WhenLonger()
    {
        // Бронь на 60 минут занимает 2 слота по 30 минут
        var busy = new[] { new TimeInterval(Now, Now.AddMinutes(60)) };
        
        var slots = SlotGrid.Generate(DefaultProfile, TimeSpan.FromMinutes(30), Now, busy);

        // 252 - 2 = 250 слотов (09:00 и 09:30 заняты)
        Assert.Equal(250, slots.Count);
        
        // Первый свободный слот — 10:00
        Assert.Equal(Now.AddMinutes(60), slots[0].Start);
    }

    [Fact]
    public void Generate_Duration60_BusySlot30_ExcludesOverlappingSlots()
    {
        // Бронь 09:30-10:00 перекрывает слоты 09:00-10:00 и 09:30-10:30
        var busy = new[] { new TimeInterval(Now.AddMinutes(30), Now.AddMinutes(60)) };
        
        var slots = SlotGrid.Generate(DefaultProfile, TimeSpan.FromMinutes(60), Now, busy);

        // 238 - 2 = 236 слотов (09:00 и 09:30 перекрываются с busy)
        Assert.Equal(236, slots.Count);
        
        // Первый свободный слот — 10:00
        Assert.Equal(Now.AddMinutes(60), slots[0].Start);
    }

    [Fact]
    public void Generate_NowInMiddleOfDay_SkipsPastSlots()
    {
        // "Сейчас" 12:00 — слоты до 12:00 не генерируются
        var now = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);
        
        var slots = SlotGrid.Generate(DefaultProfile, TimeSpan.FromMinutes(30), now, []);

        // Первый слот должен быть в 12:00
        Assert.Equal(now, slots[0].Start);
        
        // Окно: [Aug 17 12:00, Aug 31 12:00)
        // Первый день (Aug 17): 12:00-17:30 = 12 слотов
        // Дни 18-30 (13 полных дней): 18 слотов каждый = 234 слота
        // Последний день (Aug 31): 09:00-11:30 = 6 слотов (окно заканчивается в 12:00)
        // Итого: 12 + 234 + 6 = 252
        Assert.Equal(252, slots.Count);
    }

    [Fact]
    public void Generate_AllSlotsBusy_ReturnsEmpty()
    {
        // Занимаем всё окно записи
        var busy = new[] { new TimeInterval(Now, Now.AddDays(14)) };
        
        var slots = SlotGrid.Generate(DefaultProfile, TimeSpan.FromMinutes(30), Now, busy);

        Assert.Empty(slots);
    }
}
