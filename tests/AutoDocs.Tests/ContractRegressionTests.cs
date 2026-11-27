using AutoDocs.Application.Services;
using AutoDocs.Core.Analysis;
using AutoDocs.Core.Comparison;
using AutoDocs.Core.Generation;
using AutoDocs.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AutoDocs.Tests;

public class ContractRegressionTests
{
    // Compile fixtures against the SDK reference assemblies selected for this target framework.
    private static readonly MetadataReference[] References = Directory
        .GetFiles(Path.Combine(AppContext.BaseDirectory, "FixtureReferences"), "*.dll")
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToArray();

    private static async Task<ApiDescription> Analyze(string code)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("Fixture", LanguageNames.CSharp)
            .WithMetadataReferences(References)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        project = project.AddDocument("Fixture.cs", "using Microsoft.AspNetCore.Mvc;\n" + code).Project;
        return await new ProjectAnalyzer().AnalyzeAsync(project);
    }

    private static async Task<ApiDescription> Read(string json)
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, json);
            return await new OpenApiReader().ReadAsync(path);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task MvcRoutesAndBindingsMatchThePublicContract()
    {
        var api = await Analyze("""
            [Route("api/[controller]")]
            public class ItemsController : ControllerBase
            {
                [HttpPatch("{id}")] public IActionResult Patch(int id) => Ok();
                [HttpGet("/health")][HttpGet("~/other")][HttpGet("one")][HttpGet("two")]
                public IActionResult Get([FromHeader(Name="X-Client")] string client,
                    [FromServices] System.IServiceProvider service) => Ok();
                [NonAction][HttpGet("helper")] public IActionResult Helper() => Ok();
            }
            """);
        Assert.Equal(5, api.Endpoints.Count);
        Assert.Contains(api.Endpoints, e => e.Key == "PATCH /api/Items/{id}");
        foreach (var path in new[] { "/health", "/other", "/api/Items/one", "/api/Items/two" })
        {
            var endpoint = Assert.Single(api.Endpoints, e => e.Route == path);
            var header = Assert.Single(endpoint.Parameters);
            Assert.Equal("X-Client", header.Name);
            Assert.Equal(ParameterSource.Header, header.Source);
        }
        var read = await Read(await new OpenApiGenerator().GenerateAsync(api));
        Assert.Empty(new ApiComparer().Compare(read, api));
    }

    [Theory]
    [InlineData("int Age", "string Age")]
    [InlineData("string? Email", "string Email")]
    [InlineData("int Age", "int Age, string? Phone")]
    public async Task NestedDtoChangesAreDetectedAfterRoundTrip(string before, string after)
    {
        const string code = """
            public record Contact(FIELDS);
            public record Person(Contact[] Contacts, Person? Parent);
            [Route("people")] public class PeopleController : ControllerBase {
                [HttpPost] public Person Save([FromBody] Person value) => value;
            }
            """;
        var initial = await Analyze(code.Replace("FIELDS", before));
        var docs = await Read(await new OpenApiGenerator().GenerateAsync(initial));
        var comparer = new ApiComparer();
        Assert.Empty(comparer.Compare(docs, initial));
        var changes = comparer.Compare(docs, await Analyze(code.Replace("FIELDS", after)));
        Assert.Contains(changes, d => d.Kind == DiscrepancyKind.ParameterTypeChanged);
        Assert.Contains(changes, d => d.Kind == DiscrepancyKind.ResponseTypeChanged);
    }

    [Fact]
    public async Task CompilationErrorsPreventSuccessfulAnalysis()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Analyze("public class Bad { public int Value => MissingSymbol; }"));
        Assert.Contains("CS0103", error.Message);
        Assert.Contains("MissingSymbol", error.Message);
    }

    [Fact]
    public async Task OperationParameterOverridesPathParameterAndRetainsOthers()
    {
        var api = await Read("""
            {"openapi":"3.0.3","info":{"title":"T","version":"1"},"paths":{"/a/{id}":{
              "parameters":[{"name":"id","in":"path","required":true,"schema":{"type":"integer"}},
                            {"name":"q","in":"query","schema":{"type":"string"}}],
              "get":{"parameters":[{"name":"q","in":"query","schema":{"type":"boolean"}}],
              "responses":{"200":{"description":"OK"}}}}}}
            """);
        var parameters = Assert.Single(api.Endpoints).Parameters;
        Assert.Equal(2, parameters.Count);
        Assert.Contains(parameters, p => p.Name == "id" && p.Source == ParameterSource.Route);
        Assert.Contains(parameters, p => p.Name == "q" && p.Type == "bool");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailedPublicationPreservesPreviousOutput(bool directoryCollision)
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var json = Path.Combine(root.FullName, "openapi.json");
            var markdown = Path.Combine(root.FullName, "API.md");
            File.WriteAllText(json, "previous");
            if (directoryCollision) Directory.CreateDirectory(markdown);
            using (var output = new DocumentOutput(root.FullName))
            {
                File.WriteAllText(output.Stage("openapi.json"), "new");
                var stage = output.Stage("API.md");
                if (directoryCollision) File.WriteAllText(stage, "new");
                Assert.ThrowsAny<IOException>(output.Commit);
            }
            Assert.Equal("previous", File.ReadAllText(json));
            Assert.Equal(directoryCollision, Directory.Exists(markdown));
            Assert.False(File.Exists(markdown));
            Assert.Empty(root.GetFileSystemInfos(".autodocs*"));
        }
        finally { root.Delete(true); }
    }
}