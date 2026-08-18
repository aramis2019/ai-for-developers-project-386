using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Meetly.IntegrationTests;

/// <summary>
/// Интеграционные тесты API.
/// Каждый тест использует свежую WebApplicationFactory для изоляции данных.
/// </summary>
public class ApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task CreateEventType_InvalidJsonType_ReturnsContractErrorBody()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        using var content = new StringContent(
            """{"id":"invalid-json","title":"Test","description":"Test","durationMinutes":{}}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/admin/event-types", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var error = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("BAD_REQUEST", error!.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateEventType_TitleTooLong_Returns422()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        var body = new
        {
            id = "too-long-title",
            title = new string('*', 121),
            description = "Test",
            durationMinutes = 30
        };

        var response = await client.PostAsJsonAsync("/api/admin/event-types", body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("VALIDATION_FAILED", error!.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetProfile_ReturnsOwnerProfile()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/profile");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("owner", json!.RootElement.GetProperty("id").GetString());
        Assert.Equal("Анна Смирнова", json.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetAdminEventTypes_ReturnsSeededEventTypes()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/event-types");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var items = json!.RootElement.GetProperty("items");
        
        // DevDataSeeder добавляет 3 типа событий
        Assert.Equal(3, items.GetArrayLength());
    }

    [Fact]
    public async Task CreateEventType_Success_Returns201()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        var body = new { id = "new-event", title = "Новая встреча", description = "Описание", durationMinutes = 45 };
        
        var response = await client.PostAsJsonAsync("/api/admin/event-types", body);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("new-event", json!.RootElement.GetProperty("id").GetString());
        Assert.Equal(45, json.RootElement.GetProperty("durationMinutes").GetInt32());
    }

    [Fact]
    public async Task CreateEventType_DuplicateId_Returns409()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        // intro-call уже существует (DevDataSeeder)
        var body = new { id = "intro-call", title = "Дубль", description = "Описание", durationMinutes = 30 };
        
        var response = await client.PostAsJsonAsync("/api/admin/event-types", body);
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("EVENT_TYPE_ALREADY_EXISTS", json!.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetPublicEventTypes_ReturnsPublicFormat()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/event-types");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var items = json!.RootElement.GetProperty("items");
        
        Assert.Equal(3, items.GetArrayLength());
        
        // Публичный формат не должен содержать createdAt
        var firstItem = items[0];
        Assert.False(firstItem.TryGetProperty("createdAt", out _));
    }

    [Fact]
    public async Task GetSlots_ValidEventType_ReturnsSlots()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/event-types/intro-call/slots");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("intro-call", json!.RootElement.GetProperty("eventTypeId").GetString());
        Assert.Equal(30, json.RootElement.GetProperty("durationMinutes").GetInt32());
        
        var slots = json.RootElement.GetProperty("slots");
        Assert.True(slots.GetArrayLength() > 0, "Должны быть свободные слоты");
    }

    [Fact]
    public async Task GetSlots_NonExistentEventType_Returns404()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/event-types/non-existent/slots");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("EVENT_TYPE_NOT_FOUND", json!.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateBooking_ValidSlot_Returns201()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        // Получаем первый свободный слот
        var slotsResponse = await client.GetAsync("/api/event-types/intro-call/slots");
        var slotsJson = await slotsResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var firstSlot = slotsJson!.RootElement.GetProperty("slots")[0];
        var slotStart = firstSlot.GetProperty("start").GetString();

        var body = new
        {
            eventTypeId = "intro-call",
            start = slotStart,
            guest = new { name = "Иван Петров", email = "ivan@example.com" }
        };

        var response = await client.PostAsJsonAsync("/api/bookings", body);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("intro-call", json!.RootElement.GetProperty("eventTypeId").GetString());
        Assert.Equal("Иван Петров", json.RootElement.GetProperty("guest").GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateBooking_DuplicateSlot_Returns409()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        // Получаем первый свободный слот
        var slotsResponse = await client.GetAsync("/api/event-types/intro-call/slots");
        var slotsJson = await slotsResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var firstSlot = slotsJson!.RootElement.GetProperty("slots")[0];
        var slotStart = firstSlot.GetProperty("start").GetString();

        var body = new
        {
            eventTypeId = "intro-call",
            start = slotStart,
            guest = new { name = "Гость 1", email = "guest1@example.com" }
        };

        // Первое бронирование — успех
        var response1 = await client.PostAsJsonAsync("/api/bookings", body);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Второе бронирование на тот же слот — конфликт
        var body2 = new
        {
            eventTypeId = "intro-call",
            start = slotStart,
            guest = new { name = "Гость 2", email = "guest2@example.com" }
        };

        var response2 = await client.PostAsJsonAsync("/api/bookings", body2);
        
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
        
        var json = await response2.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("SLOT_ALREADY_BOOKED", json!.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateBooking_NonExistentEventType_Returns404()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        var body = new
        {
            eventTypeId = "non-existent",
            start = DateTimeOffset.UtcNow.AddDays(1).ToString("O"),
            guest = new { name = "Гость", email = "guest@example.com" }
        };

        var response = await client.PostAsJsonAsync("/api/bookings", body);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("EVENT_TYPE_NOT_FOUND", json!.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateBooking_MisalignedSlot_Returns422()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        // Время не выровнено по сетке (09:15 вместо 09:00 или 09:30)
        var tomorrow = DateTimeOffset.UtcNow.Date.AddDays(1);
        var misalignedTime = new DateTimeOffset(tomorrow.Year, tomorrow.Month, tomorrow.Day, 9, 15, 0, TimeSpan.Zero);

        var body = new
        {
            eventTypeId = "intro-call",
            start = misalignedTime.ToString("O"),
            guest = new { name = "Гость", email = "guest@example.com" }
        };

        var response = await client.PostAsJsonAsync("/api/bookings", body);
        
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("SLOT_NOT_ALIGNED", json!.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetAdminBookings_AfterCreating_ReturnsBooking()
    {
        await using var factory = new InMemoryWebApplicationFactory();
        using var client = factory.CreateClient();

        // Создаём бронирование
        var slotsResponse = await client.GetAsync("/api/event-types/intro-call/slots");
        var slotsJson = await slotsResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var firstSlot = slotsJson!.RootElement.GetProperty("slots")[0];
        var slotStart = firstSlot.GetProperty("start").GetString();

        var body = new
        {
            eventTypeId = "intro-call",
            start = slotStart,
            guest = new { name = "Тест", email = "test@example.com" }
        };

        await client.PostAsJsonAsync("/api/bookings", body);

        // Проверяем, что бронирование появилось в списке
        var response = await client.GetAsync("/api/admin/bookings");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var items = json!.RootElement.GetProperty("items");
        
        Assert.True(items.GetArrayLength() >= 1);
        Assert.Equal("Тест", items[0].GetProperty("guest").GetProperty("name").GetString());
    }
}

