# ngt - Angular Unit Test Generator

A .NET CLI tool to automatically generate Jest/Vitest unit tests for Angular TypeScript files with 80% test coverage target.

## Features

- **Global .NET Tool** - Install once, use anywhere with the `ngt` command
- **Smart File Detection** - Automatically identifies components, services, guards, interceptors, pipes, directives, and resolvers
- **Modern Angular Support** - Handles both functional (arrow function) and class-based patterns
- **Standalone Components** - Detects and properly tests standalone components and directives
- **Dependency Detection** - Automatically mocks HttpClient, Router, AuthService, and injection tokens
- **Non-Destructive** - Creates numbered test files when specs already exist (e.g., `.spec.2.ts`)
- **Workspace Aware** - Automatically detects Angular workspaces via `angular.json`

## Quick Start

### Installation

```bash
dotnet tool install --global ngt
```

### Usage

Navigate to your Angular project and generate tests:

```bash
cd /path/to/angular-app/src/app
ngt generate
```

Or specify a path:

```bash
ngt generate --path /path/to/angular/app
```

### Example Output

```
Detected Angular workspace at: /home/user/my-angular-app
Generating tests for: /home/user/my-angular-app/src/app
Found 12 TypeScript file(s) to process.

✓ Generated: app.component.spec.ts
✓ Generated: user.service.spec.ts
✓ Generated: auth.guard.spec.ts
✓ Generated: auth.interceptor.spec.ts
✓ Generated: date-format.pipe.spec.ts
- Skipped: user.model (interface/type only)

Test generation complete:
  Success: 10
  Skipped: 2
  Failed: 0
  Total: 12
```

## Supported File Types

| Type | File Pattern | Features |
|------|--------------|----------|
| **Components** | `*.component.ts` | TestBed, fixtures, standalone detection |
| **Services** | `*.service.ts` | HTTP mocking, method-level tests |
| **Guards** | `*.guard.ts` | Functional & class-based, auth mocking |
| **Interceptors** | `*.interceptor.ts` | HTTP testing, token injection |
| **Pipes** | `*.pipe.ts` | Transform testing |
| **Directives** | `*.directive.ts` | Host component testing |
| **Resolvers** | `*.resolver.ts` | Route data resolution |

## Documentation

- [Getting Started](docs/getting-started.md) - Installation and quick start guide
- [User Guide](docs/user-guide.md) - Complete usage documentation
- [Configuration](docs/configuration.md) - Configuration options
- [Supported Types](docs/supported-types.md) - Detailed guide for each Angular type
- [Examples](docs/examples.md) - Complete source and test examples
- [Troubleshooting](docs/troubleshooting.md) - Common issues and solutions
- [Contributing](docs/contributing.md) - How to contribute

## Prerequisites

- .NET 9.0 SDK or later
- An Angular application with TypeScript files

## Command Reference

### Generate Command

```bash
ngt generate [options]
```

| Option | Alias | Description |
|--------|-------|-------------|
| `--path` | `-p` | Path to generate tests for (optional) |
| `--help` | `-h` | Show help |

### Global Options

```bash
ngt --version    # Show version
ngt --help       # Show help
```

## Configuration

Create `appsettings.json` in your project to customize behavior:

```json
{
  "AngularTestGenerator": {
    "TargetCoveragePercentage": 80,
    "TestFileExtension": ".spec.ts",
    "ExcludedDirectories": ["node_modules", "dist", ".angular", "coverage"]
  }
}
```

See [Configuration Guide](docs/configuration.md) for all options.

## How It Works

1. **Discovery** - Scans the directory for TypeScript files, excluding `node_modules`, `dist`, and existing test files
2. **Analysis** - Determines file type, extracts class names, detects dependencies, and identifies patterns (functional vs class-based)
3. **Generation** - Creates appropriate test files with TestBed configuration, mocks, and basic test cases
4. **Naming** - If a test file exists, adds a discriminating number (e.g., `.spec.2.ts`)

## Project Structure

```
src/AngularUnitTests.Cli/
├── Commands/           # CLI command definitions
├── Services/           # Core services (discovery, generation)
├── Models/             # Data models
├── Configuration/      # Options and settings
└── Program.cs          # Entry point
```

## Development

### Build from Source

```bash
git clone https://github.com/QuinntyneBrown/AngularUnitTests.git
cd AngularUnitTests
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Install Local Build

```bash
dotnet pack src/AngularUnitTests.Cli/AngularUnitTests.Cli.csproj -o ./nupkg
dotnet tool install --global --add-source ./nupkg ngt
```

## Contributing

Contributions are welcome! See [Contributing Guide](docs/contributing.md) for details.

## License

MIT
