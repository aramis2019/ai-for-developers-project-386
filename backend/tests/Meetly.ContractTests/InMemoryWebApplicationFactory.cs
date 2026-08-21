using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Meetly.ContractTests;

/// <summary>
/// WebApplicationFactory, форсирующая Storage:Provider=InMemory.
/// Голая WebApplicationFactory поднимает окружение Development и через
/// appsettings.Development.json тянет Postgres — тест маршрутов начинает
/// зависеть от запущенной БД (на CI-агенте её нет). Хранилище здесь
/// не проверяется, поэтому форсим InMemory, как в интеграционных тестах.
/// </summary>
public sealed class InMemoryWebApplicationFactory : WebApplicationFactory<Program>
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
