# Contributing to ngt

Thank you for your interest in contributing to ngt (Angular Unit Test Generator)! This guide will help you get started.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Making Changes](#making-changes)
- [Testing](#testing)
- [Submitting Changes](#submitting-changes)
- [Coding Guidelines](#coding-guidelines)

## Code of Conduct

Please be respectful and constructive in all interactions. We welcome contributors of all experience levels.

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Git
- A code editor (VS Code, Visual Studio, Rider, etc.)

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR-USERNAME/AngularUnitTests.git
   cd AngularUnitTests
   ```
3. Add upstream remote:
   ```bash
   git remote add upstream https://github.com/QuinntyneBrown/AngularUnitTests.git
   ```

## Development Setup

### Build the Project

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Run the CLI Locally

```bash
dotnet run --project src/AngularUnitTests.Cli/AngularUnitTests.Cli.csproj -- generate --path /path/to/angular/app
```

### Install as Local Tool

```bash
dotnet pack src/AngularUnitTests.Cli/AngularUnitTests.Cli.csproj -o ./nupkg
dotnet tool install --global --add-source ./nupkg ngt

# To update after changes:
dotnet tool uninstall --global ngt
dotnet pack src/AngularUnitTests.Cli/AngularUnitTests.Cli.csproj -o ./nupkg
dotnet tool install --global --add-source ./nupkg ngt
```

## Project Structure

```
AngularUnitTests/
├── src/
│   └── AngularUnitTests.Cli/
│       ├── Commands/
│       │   └── GenerateTestsCommand.cs      # CLI command definitions
│       ├── Configuration/
│       │   └── AngularTestGeneratorOptions.cs  # Configuration options
│       ├── Models/
│       │   └── TypeScriptFileInfo.cs        # Data models
│       ├── Services/
│       │   ├── TypeScriptFileDiscoveryService.cs  # File discovery
│       │   └── JestTestGeneratorService.cs        # Test generation
│       ├── Program.cs                       # Entry point
│       └── appsettings.json                 # Default configuration
├── tests/
│   └── AngularUnitTests.Cli.Tests/
│       └── ...                              # Unit tests
├── docs/
│   └── ...                                  # Documentation
└── README.md
```

### Key Components

| File | Purpose |
|------|---------|
| `Program.cs` | Application entry point, DI configuration, command setup |
| `GenerateTestsCommand.cs` | CLI command definition and handler |
| `TypeScriptFileDiscoveryService.cs` | Scans directories, categorizes TypeScript files |
| `JestTestGeneratorService.cs` | Generates test content for each file type |
| `TypeScriptFileInfo.cs` | Data model for discovered files |
| `AngularTestGeneratorOptions.cs` | Configuration options |

## Making Changes

### Workflow

1. **Create a branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes**

3. **Test your changes**:
   ```bash
   dotnet test
   ```

4. **Commit your changes**:
   ```bash
   git add .
   git commit -m "Add feature: description of your change"
   ```

5. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Create a Pull Request** on GitHub

### Types of Contributions

#### Bug Fixes

1. Create an issue describing the bug (if one doesn't exist)
2. Reference the issue in your PR
3. Include a test case that reproduces the bug

#### New Features

1. Discuss the feature in an issue first
2. Implement the feature
3. Add tests
4. Update documentation

#### Documentation

Documentation improvements are always welcome:
- Fix typos or unclear explanations
- Add examples
- Improve formatting

#### Test Templates

To add or modify test templates:

1. Edit `JestTestGeneratorService.cs`
2. Find the relevant `Generate*Test` method
3. Modify the `StringBuilder` output
4. Test against sample Angular files

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test project
dotnet test tests/AngularUnitTests.Cli.Tests/AngularUnitTests.Cli.Tests.csproj
```

### Writing Tests

Tests are located in `tests/AngularUnitTests.Cli.Tests/`.

Example test:
```csharp
[Fact]
public async Task DiscoverTypeScriptFilesAsync_ShouldFindComponentFiles()
{
    // Arrange
    var options = Options.Create(new AngularTestGeneratorOptions());
    var logger = NullLogger<TypeScriptFileDiscoveryService>.Instance;
    var service = new TypeScriptFileDiscoveryService(logger, options);

    // Act
    var files = await service.DiscoverTypeScriptFilesAsync(testPath);

    // Assert
    files.Should().Contain(f => f.FileType == TypeScriptFileType.Component);
}
```

### Test Coverage

Aim for good test coverage, especially for:
- File discovery logic
- File type detection
- Test generation for each Angular type

## Submitting Changes

### Pull Request Guidelines

1. **Title**: Clear, concise description of the change
2. **Description**: Include:
   - What the PR does
   - Why the change is needed
   - How to test the changes
   - Screenshots (if UI-related)

3. **Size**: Keep PRs focused and reasonably sized

4. **Tests**: Include tests for new functionality

5. **Documentation**: Update docs if behavior changes

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Documentation update
- [ ] Refactoring

## Testing
How to test the changes

## Checklist
- [ ] Tests pass locally
- [ ] Code follows project style
- [ ] Documentation updated (if needed)
```

## Coding Guidelines

### C# Style

- Use C# 12 features (file-scoped namespaces, primary constructors, etc.)
- Follow Microsoft's [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful names for variables, methods, and classes
- Add XML documentation for public APIs

### Code Organization

```csharp
// File-scoped namespace
namespace AngularUnitTests.Cli.Services;

// Interface first
public interface IMyService
{
    Task<Result> DoSomethingAsync(CancellationToken cancellationToken = default);
}

// Implementation
public class MyService : IMyService
{
    private readonly ILogger<MyService> _logger;
    private readonly MyOptions _options;

    public MyService(
        ILogger<MyService> logger,
        IOptions<MyOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<Result> DoSomethingAsync(CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

### Commit Messages

Follow conventional commit format:
```
type(scope): description

- feat: New feature
- fix: Bug fix
- docs: Documentation
- refactor: Code refactoring
- test: Adding tests
- chore: Maintenance tasks
```

Examples:
```
feat(generator): add support for NgRx effects
fix(discovery): handle files with multiple classes
docs(readme): update installation instructions
```

## Areas for Contribution

### Good First Issues

Look for issues labeled `good first issue` on GitHub.

### Feature Ideas

- Custom test templates
- Configuration file in project root
- Support for additional test frameworks (Jasmine, Mocha)
- NgRx store/effects/selectors testing
- Improved method parameter analysis
- TypeScript AST parsing for better analysis

### Known Limitations to Address

- Complex generic types in method signatures
- Multi-class files
- Re-exported types
- Path alias resolution

## Questions?

- Open an issue for questions about contributing
- Tag maintainers for PR reviews

Thank you for contributing to ngt!
