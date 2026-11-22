using AutoDocs.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDocs.Core.Analysis
{
    public class ProjectAnalyzer
    {

        public async Task<ApiDescription> AnalyzeAsync(string projectPath)
        {
            using var workspace = MSBuildWorkspace.Create();

            Console.WriteLine($"Открываю проект: {projectPath}");
            var project = await workspace.OpenProjectAsync(projectPath);

            // Проблемы при загрузке проекта. Если что-то не так — причина здесь.
            foreach (var diagnostic in workspace.Diagnostics)
            {
                Console.WriteLine($"[{diagnostic.Kind}] {diagnostic.Message}");
            }

            var compilation = await project.GetCompilationAsync();

            if (compilation is null)
            {
                throw new InvalidOperationException("Не удалось получить компиляцию проекта.");
                //return 2;
            }

            Console.WriteLine($"Проект: {project.Name}, файлов: {project.Documents.Count()}");
            Console.WriteLine();
            Console.WriteLine("Найденные классы:");
            var endpoint = new List<Endpoint>();
            foreach (var document in project.Documents)
            {
                var root = await document.GetSyntaxRootAsync();
                if (root is null)
                {
                    continue;
                }
                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                var fileName = Path.GetFileName(document.FilePath);
                

                foreach (var classNode in classes)
                {
                    if (classNode.Identifier.Text.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) && !classNode.Modifiers.Any(SyntaxKind.AbstractKeyword))
                    {

                        var line = classNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        var methods = classNode.Members.OfType<MethodDeclarationSyntax>();
                        string name = classNode.Identifier.Text;
                        string? r = null;
                        Console.WriteLine($"  {classNode.Identifier.Text}  ({fileName}:{line}) ");
                        foreach (var attrList in classNode.AttributeLists)
                        {
                            foreach (var attr in attrList.Attributes)
                            {
                                if (attr.Name.ToString() is "Route" or "RouteAttribute")
                                {
                                    var arg = attr.ArgumentList?.Arguments.FirstOrDefault();
                                    if (arg is not null)
                                    {
                                        r = arg.Expression.ToString().Trim('"');
                                        break;
                                    }
                                }
                            }
                            if (r is not null) break;
                        }
                        Console.WriteLine(r);
                       

                        foreach (var method in methods)
                        {
                            if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                            {
                                Console.WriteLine($"        |_ {method.Identifier.Text} ");
                                HttpVerb? verbs = null;
                                string? route = null;
                                var attributeLists = method.AttributeLists;
                                foreach (var attributeList in attributeLists)
                                {
                                    var attributes = attributeList.Attributes;
                                    

                                    foreach (var attribute in attributes)
                                    {
                                        //Console.WriteLine($"            |_тут{attribute.Name.ToString()} {attribute.ArgumentList} ");

                                        HttpVerb? verb = attribute.Name.ToString() switch
                                        {
                                            "HttpPost" => HttpVerb.Post,
                                            "HttpGet" => HttpVerb.Get,
                                            "HttpPut" => HttpVerb.Put,
                                            "HttpDelete" => HttpVerb.Delete,
                                            _ => null
                                        };
                                        if (verb is not null)
                                        {
                                            verbs = verb;
                                            var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                                            if (argument?.Expression is LiteralExpressionSyntax literal)
                                            {
                                                route = literal.Token.ValueText;
                                            }
                                        }

                                        
                                    }
                                }
                                if (verbs is not null)
                                {
                                    string controllerName = name.Replace("Controller", "");

                                    string controllerRoute = (r ?? "api/[controller]")
                                        .Replace("[controller]", controllerName);

                                    string fullRoute = string.IsNullOrEmpty(route)
                                        ? controllerRoute
                                        : $"{controllerRoute.TrimEnd('/')}/{route.TrimStart('/')}";

                                    string key = $"{verbs.Value.ToString().ToUpperInvariant()} {fullRoute}";

                                    endpoint.Add(new Endpoint(
                                        verbs.Value,
                                        fullRoute,
                                        key,
                                        [],
                                        []
                                    ));

                                    Console.WriteLine(key);
                                }
                            }

                        }
                    }
                }
            }

            // return 0;
            Console.WriteLine($"Всего эндпоинтов: {endpoint.Count}");
            return new ApiDescription
            (
                project.Name,
                "v1",
                endpoint
                

            );
        }
    }
}
