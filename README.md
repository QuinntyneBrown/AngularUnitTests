# AngularUnitTests

A C# CLI tool to automatically generate Jest unit tests for Angular TypeScript files with 80% test coverage target.

## Features

- **File-per-command architecture** using System.CommandLine
- **Microsoft Extensions** integration for Logging, DI, Configuration, and Options pattern
- **Automatic test generation** for Angular components, services, directives, pipes, guards, interceptors, resolvers, and more
- **Smart file naming** with discriminating values (e.g., `.spec.2.ts`) when test files already exist
- **80% test coverage** patterns in generated tests

## Prerequisites

- .NET 9.0 SDK or later
- An Angular application with TypeScript files

## Building

```bash
dotnet build
```

## Usage

### Generate Tests Command

Generate Jest unit tests for all TypeScript files in an Angular application:

```bash
dotnet run --project src/AngularUnitTests.Cli/AngularUnitTests.Cli.csproj -- generate --path /path/to/angular/app
```

Or using the short form:

```bash
dotnet run --project src/AngularUnitTests.Cli/AngularUnitTests.Cli.csproj -- generate -p /path/to/angular/app
```

### Example

```bash
cd /home/runner/work/AngularUnitTests/AngularUnitTests
dotnet run --project src/AngularUnitTests.Cli/AngularUnitTests.Cli.csproj -- generate --path ~/my-angular-app
```

## How It Works

1. **Discovery**: The tool scans the specified Angular application directory for TypeScript files (`.ts`)
2. **Analysis**: Each file is analyzed to determine its type (component, service, directive, etc.)
3. **Generation**: Jest test files are generated with appropriate test patterns for the file type
4. **Naming**: If a test file already exists, a discriminating number is added (e.g., `.spec.2.ts`)

## Supported File Types

The CLI automatically detects and generates appropriate tests for:

- **Components** (`.component.ts`) - Generates tests with TestBed, ComponentFixture, lifecycle hooks
- **Services** (`.service.ts`) - Generates tests with TestBed injection and method testing
- **Directives** (`.directive.ts`) - Generates tests with host components and DOM manipulation
- **Pipes** (`.pipe.ts`) - Generates tests for transform methods and edge cases
- **Guards** (`.guard.ts`) - Generates tests for route protection logic
- **Interceptors** (`.interceptor.ts`) - Generates tests with HttpTestingController
- **Resolvers** (`.resolver.ts`) - Generates tests for data resolution
- **Modules** (`.module.ts`) - Basic structure tests
- **Models** - Generic tests for data structures

## Configuration

The tool uses the Options pattern for configuration. Edit `appsettings.json` to customize:

```json
{
  "AngularTestGenerator": {
    "TargetCoveragePercentage": 80,
    "TestFileExtension": ".spec.ts",
    "TypeScriptExtensions": [".ts"],
    "ExcludedDirectories": ["node_modules", "dist", ".angular", "coverage"],
    "GenerateJestConfig": true
  }
}
```

## Architecture

### Dependency Injection

The application uses Microsoft.Extensions.DependencyInjection for service registration and lifetime management.

### Logging

Microsoft.Extensions.Logging is configured with console output for tracking test generation progress.

### Configuration

Microsoft.Extensions.Configuration and Options pattern are used for flexible configuration management.

### Command-Line Interface

System.CommandLine provides the command-line interface with:
- Command structure
- Option parsing
- Help generation
- Type-safe parameter binding

## Project Structure

```
src/
└── AngularUnitTests.Cli/
    ├── Commands/
    │   └── GenerateTestsCommand.cs       # CLI command definitions
    ├── Services/
    │   ├── TypeScriptFileDiscoveryService.cs   # File discovery logic
    │   └── JestTestGeneratorService.cs         # Test generation logic
    ├── Models/
    │   └── TypeScriptFileInfo.cs         # Data models
    ├── Configuration/
    │   └── AngularTestGeneratorOptions.cs      # Configuration options
    ├── Program.cs                        # Application entry point
    └── appsettings.json                  # Configuration file
```

## Example Output

```
Found 5 TypeScript file(s) to process.

✓ Generated: app.component.spec.ts
✓ Generated: user.service.spec.ts
✓ Generated: auth.guard.spec.ts
✓ Generated: date-format.pipe.spec.ts
✓ Generated: highlight.directive.spec.ts

Test generation complete:
  Success: 5
  Failed: 0
  Total: 5
```

## License

MIT