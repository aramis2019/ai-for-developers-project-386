using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Meetly.IntegrationTests;

/// <summary>
/// Сквозные тесты по сценариям из contracts/docs/scenarios.md.
/// Проверяют не отдельные эндпоинты, а последовательности запросов,
/// как их выполняет живой пользователь.
/// </summary>
public class UserJourneyTests
{
    /// <summary>Сценарий 1: гость записывается на встречу от начала и до конца.</summary>
    [Fact]
    public async Task Scenario1_Guest_HappyPath_BookingAppearsInAdmin()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        // 1. Гость видит список типов
        var listResp = await client.GetAsync("/api/event-types");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var list = await listResp.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.True(list!.RootElement.GetProperty("items").GetArrayLength() >= 1);

        // 2. Гость выбирает тип и получает слоты
        var slotsResp = await client.GetAsync("/api/event-types/intro-call/slots");
        Assert.Equal(HttpStatusCode.OK, slotsResp.StatusCode);
        var slots = await slotsResp.Content.ReadFromJsonAsync<JsonDocument>();
        var firstSlot = slots!.RootElement.GetProperty("slots")[0];
        var slotStart = firstSlot.GetProperty("start").GetString();

        // 3. Бронирует
        var bookingBody = new
        {
            eventTypeId = "intro-call",
            start = slotStart,
            guest = new { name = "Гость Первый", email = "first@example.com" }
        };
        var createResp = await client.PostAsJsonAsync("/api/bookings", bookingBody);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var booking = await createResp.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("intro-call", booking!.RootElement.GetProperty("eventTypeId").GetString());
        Assert.Equal("Знакомство", booking.RootElement.GetProperty("eventTypeTitle").GetString());
        Assert.True(Guid.TryParse(booking.RootElement.GetProperty("id").GetString(), out _));

        // 4. Бронь попала в админский список
        var adminResp = await client.GetAsync("/api/admin/bookings");
        var admin = await adminResp.Content.ReadFromJsonAsync<JsonDocument>();
        var items = admin!.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("first@example.com", items[0].GetProperty("guest").GetProperty("email").GetString());

        // 5. Слот пропал из повторной сетки
        var slotsAfterResp = await client.GetAsync("/api/event-types/intro-call/slots");
        var slotsAfter = await slotsAfterResp.Content.ReadFromJsonAsync<JsonDocument>();
        var startsAfter = slotsAfter!.RootElement.GetProperty("slots")
            .EnumerateArray()
            .Select(s => s.GetProperty("start").GetString())
            .ToList();
        Assert.DoesNotContain(slotStart, startsAfter);
    }

    /// <summary>Сценарий 3: попытка забронировать невыровненный по сетке слот — 422 SLOT_NOT_ALIGNED.</summary>
    [Fact]
    public async Task Scenario3_MisalignedStart_Returns422_WithSlotNotAlignedCode()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        // 09:15 не выровнено при шаге 30 минут
        var tomorrow = DateTimeOffset.UtcNow.Date.AddDays(1);
        var misaligned = new DateTimeOffset(tomorrow.Year, tomorrow.Month, tomorrow.Day, 9, 15, 0, TimeSpan.Zero);

        var body = new
        {
            eventTypeId = "intro-call",
            start = misaligned.ToString("O"),
            guest = new { name = "N", email = "n@example.com" }
        };
        var resp = await client.PostAsJsonAsync("/api/bookings", body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var err = await resp.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("SLOT_NOT_ALIGNED", err!.RootElement.GetProperty("code").GetString());
    }

    /// <summary>Сценарий 3: попытка забронировать в прошлое — 422 SLOT_OUT_OF_WINDOW.</summary>
    [Fact]
    public async Task Scenario3_StartInThePast_Returns422_WithSlotOutOfWindowCode()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        // Валидное по сетке (00:00), но в прошлом
        var past = DateTimeOffset.UtcNow.AddDays(-1).Date;
        var start = new DateTimeOffset(past.Year, past.Month, past.Day, 9, 0, 0, TimeSpan.Zero);

        var body = new
        {
            eventTypeId = "intro-call",
            start = start.ToString("O"),
            guest = new { name = "N", email = "n@example.com" }
        };
        var resp = await client.PostAsJsonAsync("/api/bookings", body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var err = await resp.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("SLOT_OUT_OF_WINDOW", err!.RootElement.GetProperty("code").GetString());
    }

    /// <summary>Сценарий 7 (ADR 0001): бронь одного типа события блокирует пересекающиеся слоты в другом типе.</summary>
    [Fact]
    public async Task Scenario7_BookingInEventTypeA_BlocksOverlappingSlotsInEventTypeB()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        // Тип intro-call (30 мин) и consultation (60 мин) сидятся автоматически — используем их.
        // 1. Берём первый слот intro-call и бронируем
        var aSlotsResp = await client.GetAsync("/api/event-types/intro-call/slots");
        var aSlots = await aSlotsResp.Content.ReadFromJsonAsync<JsonDocument>();
        var aFirst = aSlots!.RootElement.GetProperty("slots")[0].GetProperty("start").GetString()!;
        var aStart = DateTimeOffset.Parse(aFirst, styles: System.Globalization.DateTimeStyles.RoundtripKind);

        var body = new
        {
            eventTypeId = "intro-call",
            start = aFirst,
            guest = new { name = "A", email = "a@example.com" }
        };
        var createResp = await client.PostAsJsonAsync("/api/bookings", body);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        // 2. Смотрим сетку consultation (60 мин) — слоты, пересекающиеся с [aStart, aStart+30m), исчезли
        var bSlotsResp = await client.GetAsync("/api/event-types/consultation/slots");
        var bSlots = await bSlotsResp.Content.ReadFromJsonAsync<JsonDocument>();
        var bStarts = bSlots!.RootElement.GetProperty("slots")
            .EnumerateArray()
            .Select(s => DateTimeOffset.Parse(s.GetProperty("start").GetString()!, styles: System.Globalization.DateTimeStyles.RoundtripKind))
            .ToList();

        var busyStart = aStart;
        var busyEnd = aStart.AddMinutes(30);
        var duration60 = TimeSpan.FromMinutes(60);

        foreach (var b in bStarts)
        {
            var bEnd = b + duration60;
            var overlaps = b < busyEnd && busyStart < bEnd;
            Assert.False(overlaps, $"Слот consultation {b:O} пересекается с бронью intro-call [{busyStart:O}, {busyEnd:O}) — ADR 0001 нарушен");
        }
    }

    /// <summary>Сценарий 5: тип события с занятым id — 409 EVENT_TYPE_ALREADY_EXISTS, повторное создание не перезаписывает.</summary>
    [Fact]
    public async Task Scenario5_CreateEventType_DuplicateId_DoesNotOverwrite()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        // Оригинальный intro-call засижен: "Знакомство"
        var before = await (await client.GetAsync("/api/event-types")).Content.ReadFromJsonAsync<JsonDocument>();
        var originalTitle = before!.RootElement.GetProperty("items")
            .EnumerateArray()
            .First(e => e.GetProperty("id").GetString() == "intro-call")
            .GetProperty("title").GetString();

        // Попытка перезаписать
        var body = new { id = "intro-call", title = "Перезапись", description = "Тест", durationMinutes = 45 };
        var resp = await client.PostAsJsonAsync("/api/admin/event-types", body);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var err = await resp.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("EVENT_TYPE_ALREADY_EXISTS", err!.RootElement.GetProperty("code").GetString());

        // Проверяем, что title не изменился
        var after = await (await client.GetAsync("/api/event-types")).Content.ReadFromJsonAsync<JsonDocument>();
        var actualTitle = after!.RootElement.GetProperty("items")
            .EnumerateArray()
            .First(e => e.GetProperty("id").GetString() == "intro-call")
            .GetProperty("title").GetString();

        Assert.Equal(originalTitle, actualTitle);
    }
}
