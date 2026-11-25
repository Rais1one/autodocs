using AutoDocs.Core.Comparison;
using AutoDocs.Core.Generation;
using AutoDocs.Core.Model;
using Xunit;

namespace AutoDocs.Tests.Comparison;

public class OpenApiReaderTests
{
    [Fact]
    public async Task ReadAsync_ValidDocument_ReturnsApiDescription()
    {
        var api = new ApiDescription(
            "Test API",
            "v1",
            new[]
            {
                new Endpoint(
                    HttpVerb.Get,
                    "/api/users/{id}",
                    "GET /api/users/{id}",
                    new[]
                    {
                        new Parameter(
                            "id",
                            "int",
                            ParameterSource.Route,
                            true,
                            "Идентификатор пользователя")
                    },
                    new[]
                    {
                        new ApiResponse(200, "UserDto"),
                        new ApiResponse(404)
                    },
                    "Получить пользователя",
                    "Данные пользователя")
            });

        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.json");

        try
        {
            var generator = new OpenApiGenerator();
            var json = await generator.GenerateAsync(api);

            await File.WriteAllTextAsync(filePath, json);

            var reader = new OpenApiReader();
            var result = await reader.ReadAsync(filePath);

            Assert.Equal("Test API", result.Title);
            Assert.Equal("v1", result.Version);

            var endpoint = Assert.Single(result.Endpoints);

            Assert.Equal(HttpVerb.Get, endpoint.HttpMethod);
            Assert.Equal("/api/users/{id}", endpoint.Route);
            Assert.Equal("GET /api/users/{id}", endpoint.Key);
            Assert.Equal("Получить пользователя", endpoint.Summary);
            Assert.Equal("Данные пользователя", endpoint.Returns);

            var parameter = Assert.Single(endpoint.Parameters);

            Assert.Equal("id", parameter.Name);
            Assert.Equal("int", parameter.Type);
            Assert.Equal(ParameterSource.Route, parameter.Source);
            Assert.True(parameter.IsRequired);

            Assert.Equal(
                "Идентификатор пользователя",
                parameter.Description);

            Assert.Equal(2, endpoint.Responses.Count);

            Assert.Contains(endpoint.Responses,
                r => r.StatusCode == 200 &&
                     r.TypeName == "UserDto");

            Assert.Contains(endpoint.Responses,
                r => r.StatusCode == 404 &&
                     r.TypeName == null);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadAsync_MissingFile_ThrowsException()
    {
        var reader = new OpenApiReader();

        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.json");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => reader.ReadAsync(filePath));
    }
}