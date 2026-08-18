namespace Meetly.Domain;

/// <summary>
/// Контактные данные гостя. Value-object внутри брони.
/// Аккаунт не создаётся, данные живут внутри брони.
/// </summary>
public sealed record Guest
{
    /// <summary>
    /// Имя гостя (1–120 символов).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// E-mail гостя для подтверждения встречи.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Необязательный комментарий: тема встречи, вопросы, ссылки (0–1000 символов).
    /// </summary>
    public string? Note { get; init; }
}
