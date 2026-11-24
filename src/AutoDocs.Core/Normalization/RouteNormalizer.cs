using System.Text.RegularExpressions;

namespace AutoDocs.Core.Normalization;

public static class RouteNormalizer
{
    public static string Normalize(string route)
    {
        route = "/" + route.Trim('/');

        route = Regex.Replace(route, @"/+", "/");

        route = Regex.Replace(
            route,
            @"\{([^}:?]+)(?::[^}]+)?\??\}",
            "{$1}");

        return route;
    }
}
