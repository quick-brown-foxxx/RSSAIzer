## Technologies

- .NET 9
- C# 12
- ASP.NET
- Razor Pages
- EF Core (SQLite)
- Swagger
- RSSHub for Telegram channel RSS feeds
- Built-in JSON tools
- Official dotnet OpenAI package for text summarization
- FluentResults for error handling
- System.ServiceModel.Syndication for RSS feed handling
- System.Net.Mail for email sending
- IHostedService for background tasks
- Docker Compose
- Bootstrap 5 and Bootstrap Icons

## Code style and practices

- Immutable data structures (record types)
- Code formatting (CSharpier)
- Unit and integration testing (NUnit)
- XML documentation for interfaces
- Result pattern for error handling
- Exceptions only for unreachable code (cases that should never happen)
- Strong type safety (avoiding primitive obsession)
- Data Validation via attributes

## Architecture

- Structured logging
- Global exception handlers
- Layered architecture
- Domain-driven design principles
- Controller-Service-Repository pattern in backend

## Csharp-specific code style

- Target-typed new `person.City = new()`
- Prefer simple record types for POCOs `public record Person(City City, int Age, List<Person> Relatives);`
- Collection expressions for already declared types `int[] arr = [1, 2, 3];`
- All explicitly declared public properties in POCOs are required-init `public required int Age { get; init; }`
- Using expressions instead of statements `using var stream = new FileStream("file.txt", FileMode.Open);`
- Switch expressions instead of switch statements `var result = x switch { 1 => "one", 2 => "two", _ => "other" };`
- Pattern matching where suitable `if (x is {int a, int[] b} and list is [1, 2, 3, ..]) { ... }`
- Use collection expressions instead of fluent API `x.AddRange(y).Add(z).AsSpan()` -> `[x, ..y, z]`
- Use collection expressions instead of ToArray()/ToList() for known types `person.Relatives = [..relatives]`