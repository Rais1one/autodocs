using AutoDocs.Core.Model;
using AutoDocs.Core.Normalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace AutoDocs.Core.Analysis;

public class ProjectAnalyzer
{
    public async Task<ApiDescription> AnalyzeAsync(string projectPath)
    {
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);
        var failures = workspace.Diagnostics.Where(d => d.Kind == WorkspaceDiagnosticKind.Failure);
        if (failures.Any())
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures.Select(d => d.Message)));
        return await AnalyzeAsync(project);
    }

    public async Task<ApiDescription> AnalyzeAsync(Project project)
    {
        var compilation = await project.GetCompilationAsync()
            ?? throw new InvalidOperationException("Не удалось получить компиляцию проекта.");
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0)
            throw new InvalidOperationException("Ошибки компиляции:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(d => d.ToString())));

        var endpoints = new List<Endpoint>();
        var schemas = new DtoSchemas();
        var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            var root = await tree.GetRootAsync();
            foreach (var declaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var controller = model.GetDeclaredSymbol(declaration) as INamedTypeSymbol;
                if (controller is null || controller.IsAbstract || controller.DeclaredAccessibility != Accessibility.Public ||
                    !controller.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) || !seen.Add(controller))
                    continue;
                var prefixes = MvcAttributes(controller).Where(a => a.AttributeClass!.Name == "RouteAttribute")
                    .Select(Template).DefaultIfEmpty("").ToList();
                var name = controller.Name[..^"Controller".Length];
                foreach (var method in controller.GetMembers().OfType<IMethodSymbol>())
                {
                    if (method.MethodKind != MethodKind.Ordinary || method.IsStatic || method.DeclaredAccessibility != Accessibility.Public)
                        continue;
                    var attributes = MvcAttributes(method).ToList();
                    if (attributes.Any(a => a.AttributeClass!.Name == "NonActionAttribute")) continue;
                    var routes = attributes.Where(a => a.AttributeClass!.Name == "RouteAttribute").Select(Template).ToList();
                    var xml = XmlDocumentationReader.Read(method);
                    var responses = Responses(method, attributes, schemas);
                    var location = method.Locations[0].GetLineSpan();
                    foreach (var attribute in attributes)
                    {
                        var attributeName = attribute.AttributeClass!.Name;
                        if (!attributeName.StartsWith("Http") || !attributeName.EndsWith("Attribute") ||
                            !Enum.TryParse<HttpVerb>(attributeName[4..^9], true, out var verb)) continue;
                        var template = Template(attribute);
                        IEnumerable<string> templates = template.Length == 0 && routes.Count > 0 ? routes : [template];
                        var paths = from prefix in prefixes from suffix in templates select Combine(prefix, suffix);
                        foreach (var path in paths.Distinct())
                        {
                            var route = RouteNormalizer.Normalize(path.Replace("[controller]", name)
                                .Replace("[action]", method.Name));
                            var parameters = new List<Parameter>();
                            foreach (var parameter in method.Parameters)
                            {
                                var binding = ParameterFor(parameter, route, schemas, XmlDocumentationReader.GetParameter(xml, parameter.Name));
                                if (binding is not null) parameters.Add(binding);
                            }
                            endpoints.Add(new Endpoint(verb, route, $"{verb.ToString().ToUpperInvariant()} {route}",
                                parameters, responses, XmlDocumentationReader.GetSummary(xml), XmlDocumentationReader.GetReturns(xml),
                                location.Path, location.StartLinePosition.Line + 1));
                        }
                    }
                }
            }
        }
        var duplicate = endpoints.GroupBy(e => e.Key).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Обнаружены эндпоинты с одинаковым ключом: {duplicate.Key}." +
                Environment.NewLine + string.Join(Environment.NewLine, duplicate.Select(e => $"{e.FilePath}:{e.Line}")));
        return new ApiDescription(project.Name, "v1", endpoints) { Components = schemas.Components };
    }

    private static IEnumerable<AttributeData> MvcAttributes(ISymbol symbol) =>
        symbol.GetAttributes().Where(a => a.AttributeClass?.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Mvc");

    private static string Template(AttributeData attribute) =>
        attribute.ConstructorArguments.FirstOrDefault().Value as string ?? "";

    private static string Combine(string prefix, string template)
    {
        if (template.StartsWith("~/")) return template[1..];
        if (template.StartsWith('/')) return template;
        return $"{prefix.TrimEnd('/')}/{template}";
    }

    private static Parameter? ParameterFor(IParameterSymbol parameter, string route, DtoSchemas schemas, string? description)
    {
        var type = parameter.Type;
        var attributes = MvcAttributes(parameter).ToList();
        if (type.ToDisplayString() == "System.Threading.CancellationToken" ||
            attributes.Any(a => a.AttributeClass!.Name == "FromServicesAttribute")) return null;

        var binding = attributes.FirstOrDefault(a => a.AttributeClass!.Name is
            "FromHeaderAttribute" or "FromRouteAttribute" or "FromQueryAttribute" or "FromBodyAttribute");
        var name = binding?.NamedArguments.FirstOrDefault(a => a.Key == "Name").Value.Value as string ?? parameter.Name;
        var typeName = Display(type);
        var source = binding?.AttributeClass?.Name switch
        {
            "FromHeaderAttribute" => ParameterSource.Header,
            "FromRouteAttribute" => ParameterSource.Route,
            "FromQueryAttribute" => ParameterSource.Query,
            "FromBodyAttribute" => ParameterSource.Body,
            _ when route.Contains("{" + name + "}", StringComparison.OrdinalIgnoreCase) => ParameterSource.Route,
            _ when type.TypeKind == TypeKind.Enum || Generation.TypeMapper.Map(typeName).Type != "object" => ParameterSource.Query,
            _ => ParameterSource.Body
        };
        var required = source == ParameterSource.Route || (!typeName.EndsWith('?') && !parameter.HasExplicitDefaultValue);
        return new Parameter(name, typeName, source, required, description) { Schema = schemas.Create(type) };
    }

    private static string Display(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    private static List<ApiResponse> Responses(IMethodSymbol method, List<AttributeData> attributes, DtoSchemas schemas)
    {
        var result = new List<ApiResponse>();
        var type = method.ReturnType;
        while (type is INamedTypeSymbol { TypeArguments.Length: 1 } wrapper &&
            wrapper.Name is "Task" or "ValueTask" or "ActionResult")
            type = wrapper.TypeArguments[0];
        var inferred = type.SpecialType == SpecialType.System_Void || type.Name is "IActionResult" or "ActionResult" or "Task" or "ValueTask" ? null : type;
        foreach (var attribute in attributes.Where(a => a.AttributeClass!.Name == "ProducesResponseTypeAttribute"))
        {
            var args = attribute.ConstructorArguments;
            if (args.FirstOrDefault(a => a.Value is int).Value is not int status) continue;
            var body = attribute.AttributeClass!.TypeArguments.FirstOrDefault() ??
                args.FirstOrDefault(a => a.Kind == TypedConstantKind.Type).Value as ITypeSymbol;
            if (status is >= 200 and < 300 and not 204) body ??= inferred;
            result.Add(Response(status, body, schemas));
        }
        if (result.Count == 0 && inferred is not null) result.Add(Response(200, inferred, schemas));
        return result;
    }

    private static ApiResponse Response(int status, ITypeSymbol? type, DtoSchemas schemas) =>
        type is null ? new ApiResponse(status) : new ApiResponse(status, Display(type)) { Schema = schemas.Create(type) };
}
