# Сторонние компоненты и лицензии

AutoDocs использует сторонние библиотеки с открытым исходным кодом. Права на сторонние компоненты принадлежат их соответствующим правообладателям.

## 1. Использованные исходные материалы

### Swashbuckle.AspNetCore

- Источник: https://github.com/domaindrivendev/Swashbuckle.AspNetCore
- Лицензия: MIT.
- Copyright (c) 2016 Richard Morris.
- Файл AutoDocs: `src/AutoDocs.Core/Generation/TypeMapper.cs`.
- Характер использования: адаптация таблицы соответствия примитивных типов .NET типам и форматам OpenAPI.
- Изменения: использование строковых представлений типов, обработка псевдонимов C# и нормализация имён типов.
- Текст лицензии: `licenses/Swashbuckle-LICENSE.txt`.

### OpenAPI Diff

- Источник: https://github.com/OpenAPITools/openapi-diff
- Лицензия: Apache License 2.0.
- Файл AutoDocs: `src/AutoDocs.Core/Comparison/ApiComparer.cs`.
- Характер использования: алгоритмическая основа сравнения описаний REST API.
- Адаптация: реализация на C# с использованием модели данных AutoDocs и собственной классификации расхождений.
- Текст лицензии: `licenses/openapi-diff-LICENSE.txt`.

## 2. Основные NuGet-зависимости

| Компонент | Версия | Лицензия |
|---|---|---|
| Microsoft.CodeAnalysis.CSharp.Workspaces | 5.9.0 | MIT |
| Microsoft.CodeAnalysis.Workspaces.MSBuild | 5.9.0 | MIT |
| Microsoft.Build.Framework | 17.11.48, 18.12.2 | MIT |
| Microsoft.Build.Locator | 1.11.2 | MIT |
| Microsoft.OpenApi | 2.12.0, 3.11.1 | MIT |
| Grynwald.MarkdownGenerator | 3.0.106 | MIT |
| Spectre.Console.Cli | 0.57.2 | MIT |
| CommunityToolkit.Mvvm | 8.4.2 | MIT |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.15 | MIT |
| Microsoft.AspNetCore.OpenApi | 10.0.12 | MIT |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT |
| xunit | 2.9.3 | Apache-2.0 |
| xunit.abstractions | 2.0.3 | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 |
| coverlet.collector | 6.0.4 | MIT |

## 3. Транзитивные зависимости

В процессе восстановления NuGet-пакетов также используются связанные компоненты Microsoft.CodeAnalysis, Microsoft.Build, Microsoft.Extensions, Microsoft.EntityFrameworkCore, Microsoft.Data.Sqlite, System.Composition, Spectre.Console, SQLitePCLRaw, Humanizer.Core, Newtonsoft.Json и xUnit.

Список разрешённых зависимостей и их версий можно получить командой:

```powershell
dotnet list package --include-transitive
```

Условия лицензирования отдельных пакетов доступны в их метаданных и на сайте https://www.nuget.org/.

## 4. Тексты лицензий

- MIT: https://opensource.org/license/mit
- Apache License 2.0: https://www.apache.org/licenses/LICENSE-2.0

Оригинальные лицензии использованных исходных материалов находятся в каталоге `licenses`.

Сторонние компоненты сохраняют собственные условия лицензирования независимо от условий распространения AutoDocs.

Дата проверки сведений о зависимостях: 26.11.2026.
