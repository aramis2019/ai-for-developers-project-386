using Meetly.Application.Abstractions;
using Meetly.Domain;

namespace Meetly.Infrastructure;

/// <summary>
/// Сидер начальных данных для разработки.
/// Добавляет несколько типов событий через IEventTypeRepository, чтобы работать
/// с любым backend'ом (InMemory, EF+Postgres) — не привязан к конкретной реализации.
/// </summary>
public sealed class DevDataSeeder(
    IEventTypeRepository eventTypeRepository,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Добавляет предзаданные типы событий, если их ещё нет.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await eventTypeRepository.GetAllAsync(cancellationToken);
        if (existing.Count > 0)
        {
            return; // Уже засижено
        }

        var now = timeProvider.GetUtcNow();

        var eventTypes = new[]
        {
            new EventType
            {
                Id = "intro-call",
                Title = "Знакомство",
                Description = "Короткий звонок для знакомства и обсуждения целей.",
                DurationMinutes = 30,
                CreatedAt = now
            },
            new EventType
            {
                Id = "consultation",
                Title = "Консультация",
                Description = "Подробная консультация по вашему вопросу.",
                DurationMinutes = 60,
                CreatedAt = now
            },
            new EventType
            {
                Id = "deep-dive",
                Title = "Глубокий разбор",
                Description = "Детальный разбор сложной темы с примерами и практикой.",
                DurationMinutes = 90,
                CreatedAt = now
            }
        };

        foreach (var eventType in eventTypes)
        {
            await eventTypeRepository.TryAddAsync(eventType, cancellationToken);
        }
    }
}
