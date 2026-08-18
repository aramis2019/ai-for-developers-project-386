using Meetly.Domain;

namespace Meetly.Application.Abstractions;

/// <summary>
/// Провайдер профиля владельца календаря.
/// </summary>
public interface IOwnerProfileProvider
{
    /// <summary>
    /// Возвращает профиль владельца (синглтон).
    /// </summary>
    OwnerProfile GetProfile();
}
