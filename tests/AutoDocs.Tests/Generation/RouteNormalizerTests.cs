using AutoDocs.Core.Normalization;
using Xunit;

namespace AutoDocs.Tests.Generation;

public class RouteNormalizerTests
{
    [Theory]
    [InlineData("api/Users", "/api/Users")]
    [InlineData("/api/Users", "/api/Users")]
    [InlineData("api/Users/{id:int}", "/api/Users/{id}")]
    [InlineData("api/Users/{id:guid}", "/api/Users/{id}")]
    [InlineData("api/Users/{id}", "/api/Users/{id}")]
    [InlineData("api//Users/", "/api/Users")]
    [InlineData("api/Users/{id?}", "/api/Users/{id}")]
    public void Normalize_ReturnsExpectedRoute(
        string route,
        string expected)
    {
        var result = RouteNormalizer.Normalize(route);

        Assert.Equal(expected, result);
    }
}
