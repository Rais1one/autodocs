using System.Text.Json;
using AutoDocs.Core.Generation;
using AutoDocs.Core.Model;
using Xunit;

namespace AutoDocs.Tests.Generation;

public class OpenApiGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_CreatesExpectedDocument()
    {
        var endpoint = new Endpoint(
            HttpVerb.Get,
            "/api/Users/{id}",
            "GET /api/Users/{id}",
            new[]
            {
                new Parameter(
                    "id",
                    "int",
                    ParameterSource.Route,
                    true,
                    "Идентификатор пользователя.")
            },
            new[]
            {
                new ApiResponse(200, "UserDto"),
                new ApiResponse(404)
            },
            "Получить пользователя.",
            "Найденный пользователь."
        );

        var api = new ApiDescription(
            "TestApi",
            "v1",
            new[] { endpoint });

        var generator = new OpenApiGenerator();

        var json = await generator.GenerateAsync(api);

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        Assert.Equal(
            "TestApi",
            root.GetProperty("info")
                .GetProperty("title").GetString());

        var operation = root.GetProperty("paths")
            .GetProperty("/api/Users/{id}")
            .GetProperty("get");

        Assert.Equal(
            "Получить пользователя.",
            operation.GetProperty("summary").GetString());

        var parameter = operation.GetProperty("parameters")[0];

        Assert.Equal("id",
            parameter.GetProperty("name").GetString());

        Assert.Equal("path",
            parameter.GetProperty("in").GetString());

        Assert.True(
            parameter.GetProperty("required").GetBoolean());

        Assert.Equal(
            "Идентификатор пользователя.",
            parameter.GetProperty("description").GetString());

        var responses = operation.GetProperty("responses");

        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("404", out _));

        var schema = responses.GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema");

        Assert.Equal(
            "UserDto",
            schema.GetProperty("title").GetString());
    }
}
