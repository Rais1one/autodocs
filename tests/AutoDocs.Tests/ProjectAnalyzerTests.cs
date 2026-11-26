using AutoDocs.Core.Analysis;
using AutoDocs.Core.Model;
using Xunit;

namespace AutoDocs.Tests;

public class ProjectAnalyzerTests
{
    private static string GetDemoApiPath()
    {
        string? directory = AppContext.BaseDirectory;

        while (directory != null)
        {
            string projectPath = Path.Combine(
                directory,
                "samples",
                "DemoApi",
                "DemoApi.csproj"
            );

            if (File.Exists(projectPath))
                return projectPath;

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new FileNotFoundException("DemoApi.csproj not found");
    }

    private static async Task<ApiDescription> AnalyzeDemoApi()
    {
        var analyzer = new ProjectAnalyzer();
        return await analyzer.AnalyzeAsync(GetDemoApiPath());
    }

    [Fact]
    public async Task Analyzer_ShouldFindEightEndpoints()
    {
        var api = await AnalyzeDemoApi();

        Assert.Equal(8, api.Endpoints.Count);
    }

    [Fact]
    public async Task Analyzer_ShouldDetectRouteParameter()
    {
        var api = await AnalyzeDemoApi();

        var endpoint = Assert.Single(
            api.Endpoints,
            e => e.Key == "GET /api/Orders/{id}"
        );

        var parameter = Assert.Single(
            endpoint.Parameters,
            p => p.Name == "id"
        );

        Assert.Equal(ParameterSource.Route, parameter.Source);
        Assert.True(parameter.IsRequired);
    }

    [Fact]
    public async Task Analyzer_ShouldDetectResponseStatuses()
    {
        var api = await AnalyzeDemoApi();

        var endpoint = Assert.Single(
            api.Endpoints,
            e => e.Key == "GET /api/Orders/{id}"
        );

        Assert.Contains(
            endpoint.Responses,
            r => r.StatusCode == 200 && r.TypeName == "OrderDto"
        );

        Assert.Contains(
            endpoint.Responses,
            r => r.StatusCode == 404
        );
    }

    [Fact]
    public async Task Analyzer_ShouldAddDefaultResponse()
    {
        var api = await AnalyzeDemoApi();

        var endpoint = Assert.Single(
            api.Endpoints,
            e => e.Key == "GET /api/Orders"
        );

        Assert.Contains(
            endpoint.Responses,
            r => r.StatusCode == 200 &&
                 r.TypeName == "IEnumerable<OrderDto>"
        );
    }

    [Fact]
    public async Task Analyzer_ShouldRejectDuplicateEndpoints()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"AutoDocs.Tests-{Guid.NewGuid()}");

        Directory.CreateDirectory(directory);

        try
        {
            var projectPath = Path.Combine(
                directory, "DuplicateApi.csproj");

            await File.WriteAllTextAsync(projectPath, """
                <Project Sdk="Microsoft.NET.Sdk.Web">
                    <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <OutputType>Library</OutputType>
                    </PropertyGroup>
                </Project>
                """);

            await File.WriteAllTextAsync(
                Path.Combine(directory, "UsersController.cs"),
                """
                using Microsoft.AspNetCore.Mvc;

                [ApiController]
                [Route("api/[controller]")]
                public class UsersController : ControllerBase
                {
                    [HttpGet]
                    public IActionResult GetUsers()
                    {
                        return Ok();
                    }

                    [HttpGet]
                    public IActionResult GetDuplicateUsers()
                    {
                        return Ok();
                    }
                }
                """);

            var analyzer = new ProjectAnalyzer();

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => analyzer.AnalyzeAsync(projectPath));

            Assert.Contains("api/Users", exception.Message);
            Assert.Contains("UsersController.cs", exception.Message);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}


