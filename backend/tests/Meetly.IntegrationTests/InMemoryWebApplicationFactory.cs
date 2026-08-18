using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Meetly.IntegrationTests;

/// <summary>
/// WebApplicationFactory, форсирующая Storage:Provider=InMemory.
/// Интеграционные тесты не должны зависеть от запущенного Postgres —
/// хранилище выбирается только через конфиг, вся остальная логика идентична.
/// </summary>
internal sealed class InMemoryWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Форсим Environment=Testing чтобы не подхватывался appsettings.Development.json
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "InMemory"
            });
        });
    }
}
