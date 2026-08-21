using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using YamlDotNet.Serialization;

namespace Meetly.ContractTests;

/// <summary>
/// Гейт против дрейфа: набор маршрутов, поднятых приложением, должен совпадать
/// с набором операций в contracts/dist/openapi.yaml.
///
/// Ловит два типовых расхождения:
///   1. операция описана в контракте, но бэкенд её не реализовал;
///   2. бэкенд добавил или переименовал эндпоинт, не обновив контракт.
///
/// Формы тел ответов этим тестом НЕ проверяются — для этого в CI отдельно
/// прогоняется Schemathesis против запущенного приложения.
/// </summary>
public sealed class RouteCoverageTests : IClassFixture<InMemoryWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> factory;

    public RouteCoverageTests(InMemoryWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public void EveryContractOperationIsRouted()
    {
        var contract = ReadContractOperations();
        var implemented = ReadImplementedRoutes();

        var missing = contract.Except(implemented).OrderBy(x => x).ToArray();

        Assert.True(
            missing.Length == 0,
            $"Операции описаны в контракте, но не реализованы:{Environment.NewLine}" +
            string.Join(Environment.NewLine, missing));
    }

    [Fact]
    public void NoRoutesOutsideContract()
    {
        var contract = ReadContractOperations();
        var implemented = ReadImplementedRoutes();

        var extra = implemented.Except(contract).OrderBy(x => x).ToArray();

        Assert.True(
            extra.Length == 0,
            $"Маршруты есть в приложении, но отсутствуют в контракте — обновите contracts/:" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, extra)}");
    }

    /// <summary>Операции контракта в виде "GET /api/event-types".</summary>
    private static HashSet<string> ReadContractOperations()
    {
        var yaml = File.ReadAllText(LocateContract());
        var document = new DeserializerBuilder().Build()
            .Deserialize<Dictionary<string, object>>(yaml);

        var paths = (Dictionary<object, object>)document["paths"];
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (path, operations) in paths)
        {
            foreach (var method in ((Dictionary<object, object>)operations).Keys)
            {
                result.Add($"{method.ToString()!.ToUpperInvariant()} {path}");
            }
        }

        return result;
    }

    /// <summary>Маршруты, реально зарегистрированные приложением.</summary>
    private HashSet<string> ReadImplementedRoutes()
    {
        using var scope = factory.Services.CreateScope();
        var endpoints = scope.ServiceProvider
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>();

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var endpoint in endpoints)
        {
            var pattern = "/" + endpoint.RoutePattern.RawText?.TrimStart('/');

            // Служебные эндпоинты фреймворка в контракт не входят.
            if (!pattern.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Ограничения маршрута ASP.NET ({id:guid}) в OpenAPI не переносятся.
            pattern = Regex.Replace(pattern, @"\{(\w+)(:[^}]+)?\}", "{$1}");

            var methods = endpoint.Metadata
                .GetMetadata<Microsoft.AspNetCore.Routing.IHttpMethodMetadata>()?.HttpMethods
                ?? [];

            foreach (var method in methods)
            {
                result.Add($"{method.ToUpperInvariant()} {pattern}");
            }
        }

        return result;
    }

    private static string LocateContract()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "contracts", "dist", "openapi.yaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Не найден contracts/dist/openapi.yaml. Соберите контракт: npm run contract:build");
    }
}
