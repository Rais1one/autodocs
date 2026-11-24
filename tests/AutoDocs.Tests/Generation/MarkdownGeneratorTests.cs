using AutoDocs.Core.Generation;
using AutoDocs.Core.Model;
using Xunit;

namespace AutoDocs.Tests.Generation;

public class MarkdownGeneratorTests
{
    [Fact]
    public void Generate_CreatesMarkdownFile()
    {
        var endpoint = new Endpoint(
            HttpVerb.Get,
            "/api/Users",
            "GET /api/Users",
            Array.Empty<Parameter>(),
            new[]
            {
                new ApiResponse(200, "UserDto")
            },
            "Получить пользователей."
        );

        var api = new ApiDescription(
            "TestApi",
            "v1",
            new[] { endpoint });

        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.md");

        try
        {
            var generator = new MarkdownGenerator();

            generator.Generate(api, path);

            Assert.True(File.Exists(path));

            var markdown = File.ReadAllText(path);

            Assert.Contains("# TestApi", markdown);
            Assert.Contains("GET", markdown);
            Assert.Contains("Получить пользователей.", markdown);
            Assert.Contains("UserDto", markdown);
            Assert.Contains("200", markdown);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
