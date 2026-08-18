using Meetly.Application.Abstractions;
using Meetly.Application.Services;
using Meetly.Infrastructure.InMemory;
using Meetly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meetly.Infrastructure;

/// <summary>
/// Тип хранилища, выбираемый через конфиг ("Storage:Provider").
/// </summary>
public enum StorageProvider
{
    /// <summary>ConcurrentDictionary + lock; данные живут только в процессе.</summary>
    InMemory,

    /// <summary>PostgreSQL через EF Core + Npgsql; exclusion constraint на tstzrange.</summary>
    Postgres
}

/// <summary>
/// Расширения для регистрации сервисов Application и Infrastructure.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует Application-сервисы + выбранное хранилище.
    /// Выбор через <paramref name="configuration"/> ключ "Storage:Provider":
    /// "InMemory" (по умолчанию) или "Postgres".
    /// </summary>
    public static IServiceCollection AddMeetlyServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var providerText = configuration["Storage:Provider"] ?? nameof(StorageProvider.InMemory);
        if (!Enum.TryParse<StorageProvider>(providerText, ignoreCase: true, out var provider))
        {
            throw new InvalidOperationException(
                $"Неизвестный Storage:Provider = '{providerText}'. Ожидается InMemory или Postgres.");
        }

        return services.AddMeetlyServices(provider, configuration);
    }

    /// <summary>
    /// Вариант с явным выбором провайдера — используется в тестах.
    /// </summary>
    public static IServiceCollection AddMeetlyServices(
        this IServiceCollection services,
        StorageProvider provider,
        IConfiguration? configuration = null)
    {
        // Провайдер профиля и TimeProvider — общие для всех backend'ов
        services.AddSingleton<IOwnerProfileProvider, StaticOwnerProfileProvider>();
        services.AddSingleton(TimeProvider.System);

        switch (provider)
        {
            case StorageProvider.InMemory:
                services.AddSingleton<InMemoryStore>();
                services.AddSingleton<IEventTypeRepository, InMemoryEventTypeRepository>();
                services.AddSingleton<IBookingRepository, InMemoryBookingRepository>();
                break;

            case StorageProvider.Postgres:
                var connectionString = configuration?.GetConnectionString("Meetly")
                    ?? throw new InvalidOperationException(
                        "Storage:Provider=Postgres требует ConnectionStrings:Meetly в конфигурации.");
                services.AddDbContext<MeetlyDbContext>(options => options.UseNpgsql(connectionString));
                services.AddScoped<IEventTypeRepository, EfEventTypeRepository>();
                services.AddScoped<IBookingRepository, EfBookingRepository>();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }

        // Application-сервисы (scoped, чтобы делить DbContext за запрос)
        services.AddScoped<ProfileService>();
        services.AddScoped<EventTypeService>();
        services.AddScoped<SlotService>();
        services.AddScoped<BookingService>();

        // Сидер — тоже scoped, чтобы работать с scoped-репозиториями
        services.AddScoped<DevDataSeeder>();

        return services;
    }

    /// <summary>
    /// Применяет миграции EF Core (если провайдер — Postgres) и запускает сидер.
    /// </summary>
    public static async Task InitializeMeetlyAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        // Миграции для Postgres
        var db = scope.ServiceProvider.GetService<MeetlyDbContext>();
        if (db is not null)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        // Сидирование
        var seeder = scope.ServiceProvider.GetRequiredService<DevDataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
