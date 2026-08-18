using Meetly.Application.Abstractions;
using Meetly.Domain;

namespace Meetly.Application.Services;

/// <summary>
/// Сервис профиля владельца.
/// </summary>
public sealed class ProfileService(IOwnerProfileProvider profileProvider)
{
    /// <summary>
    /// Возвращает профиль владельца календаря.
    /// </summary>
    public OwnerProfile GetProfile() => profileProvider.GetProfile();
}
