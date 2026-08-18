using Meetly.Api.Errors;
using Meetly.Api.Mapping;
using Meetly.Application.Results;
using Meetly.Application.Services;
using Meetly.Contracts;
using Meetly.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Локальные оверрайды (секреты, реальные пароли БД). Файл в .gitignore.
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

builder.Services.AddOpenApi();

// По умолчанию Minimal API сам превращает ошибки JSON-binding в пустой 400,
// не давая middleware сформировать контрактный ErrorBody.
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(options =>
    options.ThrowOnBadRequest = true);

// CORS для фронтенда на Vite (localhost:5173).
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Регистрация сервисов Application и Infrastructure.
// Storage:Provider в конфиге выбирает InMemory или Postgres.
builder.Services.AddMeetlyServices(builder.Configuration);

var app = builder.Build();

// Применение миграций (для Postgres) и сидирование начальных данных.
await app.Services.InitializeMeetlyAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Единый обработчик ошибок JSON-парсинга. Без него minimal API отвечает
// голым 400 без Content-Type и body, а контракт требует ErrorBody.
// Ловим BadHttpRequestException (обёртка над JsonException при model binding'e)
// и оборачиваем в 400 { code: "BAD_REQUEST", message, details }.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            code = "BAD_REQUEST",
            message = "Не удалось разобрать тело запроса.",
            details = new { reason = ex.Message }
        });
    }
});

// ---------------------------------------------------------------------------
// ADMIN API
// ---------------------------------------------------------------------------

var admin = app.MapGroup("/api/admin").WithTags("Admin");

admin.MapGet("/profile", (ProfileService profileService) =>
{
    var profile = profileService.GetProfile();
    return DtoMapping.ToDto(profile);
});

admin.MapGet("/event-types", async (EventTypeService eventTypeService, CancellationToken ct) =>
{
    var eventTypes = await eventTypeService.GetAllAsync(ct);
    return DtoMapping.ToDto(eventTypes);
});

admin.MapPost("/event-types", async (EventTypeCreate body, EventTypeService eventTypeService, CancellationToken ct) =>
{
    if (!RequestValidation.TryValidate(body, out var details))
    {
        return ErrorResults.ValidationFailed("Проверьте данные типа события.", details);
    }

    var outcome = await eventTypeService.CreateAsync(body.Id, body.Title, body.Description, body.DurationMinutes, ct);

    return outcome switch
    {
        CreateEventTypeOutcome.Created created =>
            Results.Json(DtoMapping.ToDto(created.EventType), statusCode: 201),

        CreateEventTypeOutcome.AlreadyExists exists =>
            ErrorResults.EventTypeAlreadyExists(exists.Id),

        _ => throw new InvalidOperationException("Unexpected outcome type")
    };
});

admin.MapGet("/bookings", async (BookingService bookingService, CancellationToken ct) =>
{
    var bookings = await bookingService.GetUpcomingAsync(ct);
    return DtoMapping.ToDto(bookings);
});

// ---------------------------------------------------------------------------
// PUBLIC API
// ---------------------------------------------------------------------------

var pub = app.MapGroup("/api").WithTags("Public");

pub.MapGet("/event-types", async (EventTypeService eventTypeService, CancellationToken ct) =>
{
    var eventTypes = await eventTypeService.GetPublicAsync(ct);
    return DtoMapping.ToPublicDto(eventTypes);
});

pub.MapGet("/event-types/{eventTypeId}/slots", async (string eventTypeId, SlotService slotService, CancellationToken ct) =>
{
    var outcome = await slotService.GetSlotsAsync(eventTypeId, ct);

    return outcome switch
    {
        GetSlotsOutcome.Success success =>
            Results.Ok(DtoMapping.ToDto(
                success.EventTypeId,
                success.DurationMinutes,
                success.WindowFrom,
                success.WindowTo,
                success.Slots)),

        GetSlotsOutcome.EventTypeNotFound notFound =>
            ErrorResults.EventTypeNotFound(notFound.EventTypeId),

        _ => throw new InvalidOperationException("Unexpected outcome type")
    };
});

pub.MapPost("/bookings", async (BookingCreate body, BookingService bookingService, CancellationToken ct) =>
{
    if (!RequestValidation.TryValidate(body, out var details) ||
        !RequestValidation.TryValidate(body.Guest, out details))
    {
        return ErrorResults.ValidationFailed("Проверьте контактные данные.", details);
    }

    var guest = DtoMapping.ToDomain(body.Guest);
    var outcome = await bookingService.CreateAsync(body.EventTypeId, body.Start, guest, ct);

    return outcome switch
    {
        CreateBookingOutcome.Created created =>
            Results.Json(DtoMapping.ToDto(created.Booking), statusCode: 201),

        CreateBookingOutcome.Failed failed =>
            ErrorResults.FromBookingError(failed.Code, failed.Message),

        _ => throw new InvalidOperationException("Unexpected outcome type")
    };
});

app.Run();

/// <summary>Точка входа, открытая для WebApplicationFactory в тестах.</summary>
public partial class Program;
