using AutoDocs.Core.Comparison;
using AutoDocs.Core.Model;
using Xunit;

namespace AutoDocs.Tests.Comparison;

public class ApiComparerTests
{
    private readonly ApiComparer _comparer = new();

    private static Endpoint CreateEndpoint()
    {
        return new Endpoint(
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
            "Данные пользователя");
    }

    private static ApiDescription CreateApi(
        params Endpoint[] endpoints)
    {
        return new ApiDescription(
            "Test API",
            "v1",
            endpoints);
    }

    private void AssertDifference(
        Endpoint documented,
        Endpoint current,
        DiscrepancyKind expectedKind,
        DiscrepancySeverity expectedSeverity)
    {
        var differences = _comparer.Compare(
            CreateApi(documented),
            CreateApi(current));

        var difference = Assert.Single(differences);

        Assert.Equal(expectedKind, difference.Kind);
        Assert.Equal(expectedSeverity, difference.Severity);
        Assert.Equal(current.Key, difference.EndpointKey);
    }

    [Fact]
    public void Compare_IdenticalApis_ReturnsNoDifferences()
    {
        var documented = CreateApi(CreateEndpoint());
        var current = CreateApi(CreateEndpoint());

        var differences = _comparer.Compare(documented, current);

        Assert.Empty(differences);
    }

    [Fact]
    public void Compare_EndpointAdded_ReturnsDifference()
    {
        var endpoint = CreateEndpoint();

        var differences = _comparer.Compare(
            CreateApi(),
            CreateApi(endpoint));

        var difference = Assert.Single(differences);

        Assert.Equal(
            DiscrepancyKind.EndpointAdded, difference.Kind);

        Assert.Equal(
            DiscrepancySeverity.Error, difference.Severity);

        Assert.Equal(endpoint.Key, difference.NewValue);
    }

    [Fact]
    public void Compare_EndpointRemoved_ReturnsDifference()
    {
        var endpoint = CreateEndpoint();

        var differences = _comparer.Compare(
            CreateApi(endpoint),
            CreateApi());

        var difference = Assert.Single(differences);

        Assert.Equal(
            DiscrepancyKind.EndpointRemoved, difference.Kind);

        Assert.Equal(
            DiscrepancySeverity.Error, difference.Severity);

        Assert.Equal(endpoint.Key, difference.OldValue);
    }

    [Fact]
    public void Compare_ParameterAdded_ReturnsDifference()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Parameters = documented.Parameters.Append(
                new Parameter(
                    "search",
                    "string",
                    ParameterSource.Query,
                    false)).ToArray()
        };

        AssertDifference(
            documented, current,
            DiscrepancyKind.ParameterAdded,
            DiscrepancySeverity.Error);
    }

    [Fact]
    public void Compare_ParameterRemoved_ReturnsDifference()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Parameters = Array.Empty<Parameter>()
        };

        AssertDifference(
            documented, current,
            DiscrepancyKind.ParameterRemoved,
            DiscrepancySeverity.Error);
    }

    [Fact]
    public void Compare_ParameterTypeChanged_ReturnsDifference()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Parameters = new[]
            {
                documented.Parameters[0] with
                {
                    Type = "long"
                }
            }
        };

        AssertDifference(
            documented, current,
            DiscrepancyKind.ParameterTypeChanged,
            DiscrepancySeverity.Error);
    }

    [Fact]
    public void Compare_ParameterSourceChanged_ReturnsDifference()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Parameters = new[]
            {
                documented.Parameters[0] with
                {
                    Source = ParameterSource.Query
                }
            }
        };

        AssertDifference(
            documented, current,
            DiscrepancyKind.ParameterSourceChanged,
            DiscrepancySeverity.Error);
    }

    [Fact]
    public void Compare_ParameterRequiredChanged_ReturnsDifference()
    {
        var documented = CreateEndpoint() with
        {
            Parameters = new[]
            {
                new Parameter(
                    "search",
                    "string",
                    ParameterSource.Query,
                    false)
            }
        };

        var current = documented with
        {
            Parameters = new[]
            {
                documented.Parameters[0] with
                {
                    IsRequired = true
                }
            }
        };

        AssertDifference(
            documented, current,
            DiscrepancyKind.ParameterRequiredChanged,
            DiscrepancySeverity.Error);
    }

    [Fact]
    public void Compare_ParameterDescriptionChanged_ReturnsDifference()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Parameters = new[]
            {
                documented.Parameters[0] with
                {
                    Description = "Уникальный идентификатор"
                }
            }
        };

        AssertDifference(
            documented, current,
            DiscrepancyKind.ParameterDescriptionChanged,
            DiscrepancySeverity.Warning);
    }

    [Fact]
    public void Compare_ResponseAdded_ReturnsDifference()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Responses = documented.Responses
                .Append(new ApiResponse(403))
                .ToArray()
        };

        AssertDifference(
            documented, current,
            DiscrepancyKind.ResponseAdded,
            DiscrepancySeverity.Error);
    }

    [Fact]
    public void Compare_ResponseRemoved_ReturnsDifference()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Responses = documented.Responses
                .Where(r => r.StatusCode != 404)
                .ToArray()
        };

        AssertDifference(
            documented, current,
            DiscrepancyKind.ResponseRemoved,
            DiscrepancySeverity.Error);
    }

    [Fact]
    public void Compare_ResponseTypeChanged_ReturnsDifference()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Responses = new[]
            {
                new ApiResponse(200, "OrderDto"),
                new ApiResponse(404)
            }
        };

        AssertDifference(
            documented, current,
            DiscrepancyKind.ResponseTypeChanged,
            DiscrepancySeverity.Error);
    }

    [Fact]
    public void Compare_SummaryChanged_ReturnsDifference()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Summary = "Получить пользователя по идентификатору"
        };

        AssertDifference(
            documented, current,
            DiscrepancyKind.SummaryChanged,
            DiscrepancySeverity.Warning);
    }

    [Fact]
    public void Compare_ReturnsChanged_ReturnsDifference()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Returns = "Объект пользователя"
        };

        AssertDifference(
            documented, current,
            DiscrepancyKind.ReturnsChanged,
            DiscrepancySeverity.Warning);
    }

    [Fact]
    public void Compare_EquivalentTypes_ReturnsNoDifferences()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Parameters = new[]
            {
                documented.Parameters[0] with
                {
                    Type = "System.Int32"
                }
            }
        };

        var differences = _comparer.Compare(
            CreateApi(documented),
            CreateApi(current));

        Assert.Empty(differences);
    }

    [Fact]
    public void Compare_DifferentBodyNames_ReturnsNoDifferences()
    {
        var documented = CreateEndpoint() with
        {
            Parameters = new[]
            {
                new Parameter(
                    "body",
                    "CreateUserRequest",
                    ParameterSource.Body,
                    true)
            }
        };

        var current = documented with
        {
            Parameters = new[]
            {
                documented.Parameters[0] with
                {
                    Name = "request"
                }
            }
        };

        var differences = _comparer.Compare(
            CreateApi(documented),
            CreateApi(current));

        Assert.Empty(differences);
    }

    [Fact]
    public void Compare_RouteParameterRenamed_MatchesEndpoint()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Route = "/api/users/{userId}",
            Key = "GET /api/users/{userId}",
            Parameters = new[]
            {
                documented.Parameters[0] with
                {
                    Name = "userId"
                }
            }
        };

        var differences = _comparer.Compare(
            CreateApi(documented),
            CreateApi(current));

        Assert.Equal(2, differences.Count);

        Assert.Contains(differences,
            d => d.Kind == DiscrepancyKind.ParameterAdded);

        Assert.Contains(differences,
            d => d.Kind == DiscrepancyKind.ParameterRemoved);

        Assert.DoesNotContain(differences,
            d => d.Kind == DiscrepancyKind.EndpointAdded ||
                 d.Kind == DiscrepancyKind.EndpointRemoved);
    }

    [Fact]
    public void Compare_DifferenceContainsSourceLocation()
    {
        var documented = CreateEndpoint();

        var current = documented with
        {
            Summary = "Новое описание",
            FilePath = "Controllers/UsersController.cs",
            Line = 18
        };

        var differences = _comparer.Compare(
            CreateApi(documented),
            CreateApi(current));

        var difference = Assert.Single(differences);

        Assert.Equal(current.FilePath, difference.FilePath);
        Assert.Equal(current.Line, difference.Line);
    }



}