using AutoDocs.Core.Model;
using AutoDocs.Core.Normalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace AutoDocs.Core.Analysis
{
    public class ProjectAnalyzer
    {
        public async Task<ApiDescription> AnalyzeAsync(string projectPath)
        {
            using var workspace = MSBuildWorkspace.Create();

            Console.WriteLine($"Открываю проект: {projectPath}");
            var project = await workspace.OpenProjectAsync(projectPath);

            foreach (var diagnostic in workspace.Diagnostics)
            {
                Console.WriteLine($"[{diagnostic.Kind}] {diagnostic.Message}");
            }

            var compilation = await project.GetCompilationAsync();

            if (compilation is null)
            {
                throw new InvalidOperationException(
                    "Не удалось получить компиляцию проекта.");
            }

            Console.WriteLine(
                $"Проект: {project.Name}, файлов: {project.Documents.Count()}");

            Console.WriteLine();
            Console.WriteLine("Найденные классы:");

            var endpoints = new List<Endpoint>();

            foreach (var document in project.Documents)
            {
                var root = await document.GetSyntaxRootAsync();

                if (root is null)
                    continue;

                var semanticModel = await document.GetSemanticModelAsync();

                if (semanticModel is null)
                    continue;

                var classes = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>();

                var fileName = Path.GetFileName(document.FilePath);

                foreach (var classNode in classes)
                {
                    if (!classNode.Identifier.Text.EndsWith(
                            "Controller",
                            StringComparison.OrdinalIgnoreCase)
                        || classNode.Modifiers.Any(
                            m => m.IsKind(SyntaxKind.AbstractKeyword)))
                    {
                        continue;
                    }

                    var line = classNode.GetLocation()
                        .GetLineSpan().StartLinePosition.Line + 1;

                    var methods = classNode.Members
                        .OfType<MethodDeclarationSyntax>();

                    string name = classNode.Identifier.Text;
                    string? r = null;

                    Console.WriteLine(
                        $"  {name} ({fileName}:{line})");

                    foreach (var attrList in classNode.AttributeLists)
                    {
                        foreach (var attr in attrList.Attributes)
                        {
                            if (attr.Name.ToString() is "Route" or "RouteAttribute")
                            {
                                var arg = attr.ArgumentList?
                                    .Arguments.FirstOrDefault();

                                if (arg?.Expression is LiteralExpressionSyntax literal)
                                {
                                    r = literal.Token.ValueText;
                                    break;
                                }
                            }
                        }

                        if (r is not null)
                            break;
                    }

                    foreach (var method in methods)
                    {
                        if (!method.Modifiers.Any(
                                m => m.IsKind(SyntaxKind.PublicKeyword)))
                        {
                            continue;
                        }

                        Console.WriteLine(
                            $"        |_ {method.Identifier.Text}");

                        Console.WriteLine(
                            $"Возвращаемый тип: {GetResponseType(method.ReturnType)}");

                        HttpVerb? verbs = null;
                        string? route = null;
                        var responses = new List<ApiResponse>();

                        foreach (var attributeList in method.AttributeLists)
                        {
                            foreach (var attribute in attributeList.Attributes)
                            {
                                HttpVerb? verb = attribute.Name.ToString() switch
                                {
                                    "HttpPost" or "HttpPostAttribute" => HttpVerb.Post,
                                    "HttpGet" or "HttpGetAttribute" => HttpVerb.Get,
                                    "HttpPut" or "HttpPutAttribute" => HttpVerb.Put,
                                    "HttpDelete" or "HttpDeleteAttribute" => HttpVerb.Delete,
                                    _ => null
                                };

                                if (verb is not null)
                                {
                                    verbs = verb;

                                    var argument = attribute.ArgumentList?
                                        .Arguments.FirstOrDefault();

                                    if (argument?.Expression is LiteralExpressionSyntax literal)
                                    {
                                        route = literal.Token.ValueText;
                                    }
                                }

                                if (attribute.Name.ToString() is
                                    "ProducesResponseType" or
                                    "ProducesResponseTypeAttribute")
                                {
                                    if (attribute.ArgumentList != null)
                                    {
                                        string responseType = "не указан";
                                        int? statuscode = null;

                                        var arr = attribute.ArgumentList;

                                        foreach (var argument in arr.Arguments)
                                        {
                                            if (argument.Expression is TypeOfExpressionSyntax typeOfExpr)
                                            {
                                                responseType = typeOfExpr.Type.ToString();
                                            }
                                            else
                                            {
                                                var constant = semanticModel
                                                    .GetConstantValue(argument.Expression);

                                                if (constant.HasValue &&
                                                    constant.Value is int statusCode)
                                                {
                                                    statuscode = statusCode;
                                                }
                                            }
                                        }

                                        if (responseType == "не указан" &&
                                            statuscode is >= 200 and < 300 &&
                                            statuscode != 204)
                                        {
                                            string methodResponseType =
                                                GetResponseType(method.ReturnType);

                                            if (methodResponseType != "IActionResult" &&
                                                methodResponseType != "ActionResult" &&
                                                methodResponseType != "void")
                                            {
                                                responseType = methodResponseType;
                                            }
                                        }

                                        if (statuscode.HasValue)
                                        {
                                            responses.Add(new ApiResponse(
                                                statuscode.Value,
                                                responseType == "не указан"
                                                    ? null
                                                    : responseType
                                            ));
                                        }

                                        Console.WriteLine(
                                            $"Найден атрибут: {attribute.ArgumentList} " +
                                            $"HTTP-статус: {statuscode} Тип: {responseType}");
                                    }
                                }
                            }
                        }

                        if (responses.Count == 0)
                        {
                            string responseType =
                                GetResponseType(method.ReturnType);

                            if (responseType is not
                                ("IActionResult" or "ActionResult" or "void"))
                            {
                                responses.Add(new ApiResponse(
                                    200,
                                    responseType
                                ));
                            }
                        }

                        if (verbs is null)
                            continue;

                        var methodSymbol = semanticModel.GetDeclaredSymbol(method);

                        var documentation = XmlDocumentationReader.Read(methodSymbol);

                        var summary = XmlDocumentationReader.GetSummary(documentation);
                        var returns = XmlDocumentationReader.GetReturns(documentation);

                        if (string.IsNullOrWhiteSpace(summary))
                        {
                            Console.WriteLine(
                                $"Предупреждение: отсутствует XML-описание метода {method.Identifier.Text}");
                        }


                        string controllerName =
                            name.Replace("Controller", "");

                        string controllerRoute =
                            (r ?? "api/[controller]")
                            .Replace("[controller]", controllerName);

                        string fullRoute = string.IsNullOrEmpty(route)
                            ? controllerRoute
                            : $"{controllerRoute.TrimEnd('/')}/{route.TrimStart('/')}";

                        
                        fullRoute = RouteNormalizer.Normalize(fullRoute);

                        string key =
                            $"{verbs.Value.ToString().ToUpperInvariant()} {fullRoute}";

                        var parameters = new List<Parameter>();

                        foreach (var parameter in method.ParameterList.Parameters)
                        {
                            string parameterName = parameter.Identifier.Text;
                            string typeName = parameter.Type?.ToString() ?? "";

                            if (IsServiceParameter(typeName))
                            {
                                continue;
                            }

                            ParameterSource? sources = null;

                            foreach (var attributeList in parameter.AttributeLists)
                            {
                                foreach (var attribute in attributeList.Attributes)
                                {
                                    ParameterSource? source =
                                        attribute.Name.ToString() switch
                                        {
                                            "FromHeader" or "FromHeaderAttribute"
                                                => ParameterSource.Header,

                                            "FromBody" or "FromBodyAttribute"
                                                => ParameterSource.Body,

                                            "FromRoute" or "FromRouteAttribute"
                                                => ParameterSource.Route,

                                            "FromQuery" or "FromQueryAttribute"
                                                => ParameterSource.Query,

                                            _ => null
                                        };

                                    if (source is not null)
                                    {
                                        sources = source;
                                    }
                                }
                            }

                            if (sources is null &&
                                IsRouteParameter(fullRoute, parameterName))
                            {
                                sources = ParameterSource.Route;
                            }

                            if (sources is null && IsSimpleType(typeName))
                            {
                                sources = ParameterSource.Query;
                            }

                            if (sources is null)
                            {
                                sources = ParameterSource.Body;
                            }

                            bool isRequired =
                                sources == ParameterSource.Route ||
                                (!typeName.EndsWith("?") &&
                                 parameter.Default is null);

                            parameters.Add(new Parameter
                                (
                                    parameterName,
                                    typeName,
                                    sources.Value,
                                    isRequired,
                                    XmlDocumentationReader.GetParameter(documentation, parameterName)
                                ));


                            Console.WriteLine(
                                $"            |_ {parameterName} " +
                                $"тип: {typeName} " +
                                $"источник: {sources} " +
                                $"обязательный: {isRequired}");
                        }

                        endpoints.Add(new Endpoint(
                            verbs.Value,
                            fullRoute,
                            key,
                            parameters,
                            responses,
                            summary,
                            returns
                        ));

                    }
                }
            }

            Console.WriteLine($"Всего эндпоинтов: {endpoints.Count}");

            return new ApiDescription(
                project.Name,
                "v1",
                endpoints
            );
        }

        private static bool IsRouteParameter(
            string fullRoute,
            string parameterName)
        {
            var segments = fullRoute.Split('/');

            foreach (var segment in segments)
            {
                if (!segment.StartsWith('{') || !segment.EndsWith('}'))
                    continue;

                string content = segment.Trim('{', '}');
                string name = content.Split(':')[0];

                name = name.TrimEnd('?');

                if (string.Equals(
                    name,
                    parameterName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsServiceParameter(string? typeName)
        {
            return typeName is "CancellationToken"
                or "System.Threading.CancellationToken";
        }

        private static bool IsSimpleType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return false;

            string type = typeName.TrimEnd('?');

            return type is
                "string" or "String" or "System.String" or
                "int" or "Int32" or "System.Int32" or
                "long" or "Int64" or "System.Int64" or
                "short" or "Int16" or "System.Int16" or
                "bool" or "Boolean" or "System.Boolean" or
                "double" or "Double" or "System.Double" or
                "float" or "Single" or "System.Single" or
                "decimal" or "Decimal" or "System.Decimal" or
                "Guid" or "System.Guid" or
                "DateTime" or "System.DateTime" or
                "DateOnly" or "System.DateOnly" or
                "TimeOnly" or "System.TimeOnly";
        }

        private static string GetResponseType(TypeSyntax returnType)
        {
            if (returnType is GenericNameSyntax generic)
            {
                string name = generic.Identifier.Text;

                if (name is "Task" or "ActionResult" or "ValueTask")
                {
                    TypeSyntax innerType =
                        generic.TypeArgumentList.Arguments[0];

                    return GetResponseType(innerType);
                }
            }

            return returnType.ToString();
        }
    }
}
