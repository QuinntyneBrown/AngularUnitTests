# Getting Started with ngt

This guide will help you install and start using ngt (Angular Unit Test Generator) to automatically generate Jest unit tests for your Angular applications.

## Prerequisites

Before installing ngt, ensure you have:

- **.NET 9.0 SDK or later** - [Download .NET](https://dotnet.microsoft.com/download)
- **An Angular application** with TypeScript files

Verify your .NET installation:

```bash
dotnet --version
```

## Installation

### Install as a Global Tool

The recommended way to install ngt is as a global .NET tool:

```bash
dotnet tool install --global ngt
```

After installation, verify it works:

```bash
ngt --help
```

### Update to Latest Version

```bash
dotnet tool update --global ngt
```

### Uninstall

```bash
dotnet tool uninstall --global ngt
```

### Install from Source

If you want to build from source:

```bash
git clone https://github.com/QuinntyneBrown/AngularUnitTests.git
cd AngularUnitTests
dotnet pack src/AngularUnitTests.Cli/AngularUnitTests.Cli.csproj -o ./nupkg
dotnet tool install --global --add-source ./nupkg ngt
```

## Quick Start

### Basic Usage

Navigate to your Angular project and run:

```bash
cd /path/to/your/angular-app
ngt generate
```

The tool will automatically detect the Angular workspace and generate tests for all TypeScript files in the current directory and subdirectories.

### Specify a Path

Generate tests for a specific directory:

```bash
ngt generate --path /path/to/angular/app/src
```

Or using the short form:

```bash
ngt generate -p /path/to/angular/app/src
```

### Example Session

```bash
$ cd ~/projects/my-angular-app/src/app
$ ngt generate

Detected Angular workspace at: /home/user/projects/my-angular-app
Generating tests for: /home/user/projects/my-angular-app/src/app
Found 12 TypeScript file(s) to process.

✓ Generated: app.component.spec.ts
✓ Generated: user.service.spec.ts
✓ Generated: auth.guard.spec.ts
✓ Generated: auth.interceptor.spec.ts
✓ Generated: date-format.pipe.spec.ts
✓ Generated: highlight.directive.spec.ts
✓ Generated: user.resolver.spec.ts
- Skipped: user.model (interface/type only)
- Skipped: api-response.model (interface/type only)
...

Test generation complete:
  Success: 10
  Skipped: 2
  Failed: 0
  Total: 12
```

## What Gets Generated

ngt analyzes your Angular TypeScript files and generates appropriate test files:

| File Type | Example Input | Generated Test |
|-----------|---------------|----------------|
| Component | `user.component.ts` | `user.component.spec.ts` |
| Service | `user.service.ts` | `user.service.spec.ts` |
| Guard | `auth.guard.ts` | `auth.guard.spec.ts` |
| Interceptor | `auth.interceptor.ts` | `auth.interceptor.spec.ts` |
| Pipe | `date-format.pipe.ts` | `date-format.pipe.spec.ts` |
| Directive | `highlight.directive.ts` | `highlight.directive.spec.ts` |
| Resolver | `user.resolver.ts` | `user.resolver.spec.ts` |

## Next Steps

- Read the [User Guide](user-guide.md) for detailed usage information
- Learn about [Configuration Options](configuration.md) to customize the tool
- See [Supported File Types](supported-types.md) for details on each Angular type
- Check out [Examples](examples.md) of generated tests
- Review [Troubleshooting](troubleshooting.md) if you encounter issues
