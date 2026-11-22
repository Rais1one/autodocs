using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using AutoDocs.Core.Analysis;



// 1. Регистрируем MSBuild. Это должно произойти раньше,
//    чем загрузится любой тип из MSBuild или Roslyn Workspaces.
var instance = MSBuildLocator.RegisterDefaults();
Console.WriteLine($"MSBuild: {instance.Name} {instance.Version}");

// 2. Проверяем аргументы командной строки.
if (args.Length == 0)
{
    Console.WriteLine("Использование: AutoDocs.Cli <путь к файлу .csproj>");
    return 2;
}

var projectPath = Path.GetFullPath(args[0]);

if (!File.Exists(projectPath))
{
    Console.WriteLine($"Файл не найден: {projectPath}");
    return 2;
}

var analyzer = new ProjectAnalyzer();
var title = await analyzer.AnalyzeAsync(projectPath);
Console.WriteLine(title);

return 0;
// 3. Вся работа с Roslyn — в отдельном классе.
//    Так типы Roslyn загрузятся только после регистрации MSBuild.
//return await ProjectInspector.RunAsync(projectPath);

//static class ProjectInspector
//{
//    public static async Task<int> RunAsync(string projectPath)
//    {
//        using var workspace = MSBuildWorkspace.Create();

//        Console.WriteLine($"Открываю проект: {projectPath}");
//        var project = await workspace.OpenProjectAsync(projectPath);

//        // Проблемы при загрузке проекта. Если что-то не так — причина здесь.
//        foreach (var diagnostic in workspace.Diagnostics)
//        {
//            Console.WriteLine($"[{diagnostic.Kind}] {diagnostic.Message}");
//        }

//        var compilation = await project.GetCompilationAsync();

//        if (compilation is null)
//        {
//            Console.WriteLine("Не удалось получить компиляцию проекта.");
//            return 2;
//        }

//        Console.WriteLine($"Проект: {project.Name}, файлов: {project.Documents.Count()}");
//        Console.WriteLine();
//        Console.WriteLine("Найденные классы:");

//        foreach (var document in project.Documents)
//        {
//            var root = await document.GetSyntaxRootAsync();
//            if (root is null)
//            {
//                continue;
//            }
//            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
//            var fileName = Path.GetFileName(document.FilePath);

//            foreach (var classNode in classes)
//            {
//                if (classNode.Identifier.Text.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) && !classNode.Modifiers.Any(SyntaxKind.AbstractKeyword))
//                {

//                    var line = classNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
//                    var methods = classNode.Members.OfType<MethodDeclarationSyntax>();


//                    Console.WriteLine($"  {classNode.Identifier.Text}  ({fileName}:{line}) ");

//                    foreach (var method in methods)
//                    {
//                        if (method.Modifiers.Any(SyntaxKind.PublicKeyword))
//                        {
//                            Console.WriteLine($"        |_ {method.Identifier.Text} ");
//                            var attributeLists = method.AttributeLists;
//                            foreach (var attributeList in attributeLists)
//                            {
//                                var attributes = attributeList.Attributes;

//                                foreach (var attribute in attributes)
//                                {
//                                    Console.WriteLine($"            |_{attribute.Name.ToString()} {attribute.ArgumentList} ");
//                                }
//                            }
//                        }

//                    }
//                }
//            }
//        }

//        return 0;
//    }
//}

