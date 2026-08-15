using Meetly.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// CORS для фронтенда на Vite (localhost:5173).
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// ---------------------------------------------------------------------------
// ЗАГЛУШКИ. Маршруты и формы ответов взяты из contracts/dist/openapi.yaml,
// DTO сгенерированы в Meetly.Contracts. Бизнес-логики здесь нет намеренно:
// её место — Meetly.Application (юзкейсы) и Meetly.Domain (инварианты).
//
// Что предстоит реализовать:
//   * генерацию сетки слотов из рабочих часов, шага и длительности типа события;
//   * сквозную проверку занятости (ADR 0001) атомарно со вставкой брони;
//   * маппинг доменных ошибок в коды 400/404/409/422 (ADR 0002).
//
// Контрактный тест Meetly.ContractTests следит за тем, чтобы набор маршрутов
// не разъезжался со спекой.
// ---------------------------------------------------------------------------

var admin = app.MapGroup("/api/admin").WithTags("Admin");

admin.MapGet("/profile", () => new Owner
{
    Id = "owner",
    Name = "Анна Смирнова",
    Email = "owner@meetly.local",
    TimeZone = OwnerTimeZone.UTC,
    WorkingHours = new WorkingHours { Start = "09:00", End = "18:00" },
    SlotStepMinutes = 30,
    BookingWindowDays = 14,
});

admin.MapGet("/event-types", () => new EventTypeList { Items = [] });

admin.MapPost("/event-types", (EventTypeCreate body) => Results.StatusCode(StatusCodes.Status501NotImplemented));

admin.MapGet("/bookings", () => new BookingList { Items = [] });

var pub = app.MapGroup("/api").WithTags("Public");

pub.MapGet("/event-types", () => new PublicEventTypeList { Items = [] });

pub.MapGet("/event-types/{eventTypeId}/slots", (string eventTypeId) =>
{
    var now = DateTimeOffset.UtcNow;
    return new SlotsPage
    {
        EventTypeId = eventTypeId,
        DurationMinutes = 30,
        TimeZone = SlotsPageTimeZone.UTC,
        Window = new BookingWindow { From = now, To = now.AddDays(14) },
        Slots = [],
    };
});

pub.MapPost("/bookings", (BookingCreate body) => Results.StatusCode(StatusCodes.Status501NotImplemented));

app.Run();

/// <summary>Точка входа, открытая для WebApplicationFactory в тестах.</summary>
public partial class Program;
