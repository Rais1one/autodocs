using AutoDocs.Core.Normalization;
using Xunit;

namespace AutoDocs.Tests.Comparison;

public class TypeNormalizerTests
{
    [Theory]
    [InlineData("int", "System.Int32")]
    [InlineData("int?", "int")]
    [InlineData("string?", "string")]
    [InlineData("bool", "Boolean")]
    [InlineData("long", "Int64")]
    [InlineData("List<UserDto>", "IEnumerable<UserDto>")]
    [InlineData("UserDto[]", "IEnumerable<UserDto>")]
    [InlineData("List<int>", "int[]")]
    [InlineData("decimal", "double")]
    [InlineData("DateTimeOffset", "DateTime")]
    [InlineData("UserDto", "UserDto")]
    public void AreEquivalent_EquivalentTypes_ReturnsTrue(
        string first,
        string second)
    {
        Assert.True(TypeNormalizer.AreEquivalent(first, second));
    }

    [Theory]
    [InlineData("int", "long")]
    [InlineData("int", "string")]
    [InlineData("float", "double")]
    [InlineData("UserDto", "OrderDto")]
    [InlineData("List<UserDto>", "List<OrderDto>")]
    [InlineData("UserDto", "UserDto[]")]
    [InlineData("int[]", "long[]")]
    [InlineData(null, "string")]
    public void AreEquivalent_DifferentTypes_ReturnsFalse(
        string? first,
        string? second)
    {
        Assert.False(TypeNormalizer.AreEquivalent(first, second));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "")]
    [InlineData("", " ")]
    public void AreEquivalent_EmptyTypes_ReturnsTrue(
        string? first,
        string? second)
    {
        Assert.True(TypeNormalizer.AreEquivalent(first, second));
    }
}