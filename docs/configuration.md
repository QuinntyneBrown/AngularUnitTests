# Configuration Guide

ngt can be configured to customize its behavior for your project's needs.

## Configuration File

Configuration is managed through `appsettings.json` located in the tool's installation directory. For global tool installations, create a configuration file in your project root.

### Default Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  },
  "AngularTestGenerator": {
    "TargetCoveragePercentage": 80,
    "TestFileExtension": ".spec.ts",
    "TypeScriptExtensions": [".ts"],
    "ExcludedDirectories": ["node_modules", "dist", ".angular", "coverage"],
    "GenerateJestConfig": true
  }
}
```

## Configuration Options

### AngularTestGenerator Section

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `TargetCoveragePercentage` | `int` | `80` | Target test coverage percentage for generated tests |
| `TestFileExtension` | `string` | `".spec.ts"` | Extension used for generated test files |
| `TypeScriptExtensions` | `string[]` | `[".ts"]` | File extensions to scan for TypeScript files |
| `ExcludedDirectories` | `string[]` | See below | Directories to exclude from scanning |
| `GenerateJestConfig` | `bool` | `true` | Whether to generate Jest configuration helpers |

### Default Excluded Directories

```json
["node_modules", "dist", ".angular", "coverage"]
```

## Option Details

### TargetCoveragePercentage

Sets the target code coverage percentage that generated tests aim to achieve. This influences the comprehensiveness of generated tests.

```json
{
  "AngularTestGenerator": {
    "TargetCoveragePercentage": 90
  }
}
```

### TestFileExtension

Customize the extension used for test files. The default `.spec.ts` follows Angular conventions.

```json
{
  "AngularTestGenerator": {
    "TestFileExtension": ".test.ts"
  }
}
```

### TypeScriptExtensions

Specify which file extensions to scan for TypeScript files.

```json
{
  "AngularTestGenerator": {
    "TypeScriptExtensions": [".ts", ".tsx"]
  }
}
```

### ExcludedDirectories

Add directories to exclude from the file discovery process.

```json
{
  "AngularTestGenerator": {
    "ExcludedDirectories": [
      "node_modules",
      "dist",
      ".angular",
      "coverage",
      "e2e",
      "cypress",
      "vendor"
    ]
  }
}
```

### GenerateJestConfig

Controls whether Jest configuration helpers are included in generated tests.

```json
{
  "AngularTestGenerator": {
    "GenerateJestConfig": false
  }
}
```

## Logging Configuration

### Log Levels

Control the verbosity of logging output:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

Available log levels (from most to least verbose):
- `Trace`
- `Debug`
- `Information`
- `Warning`
- `Error`
- `Critical`
- `None`

### Example: Verbose Logging

For debugging issues, enable verbose logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "AngularUnitTests.Cli": "Trace"
    }
  }
}
```

### Example: Quiet Mode

For minimal output, reduce logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

## Project-Specific Configuration

### Using appsettings.json in Your Project

Create an `appsettings.json` file in your Angular project root to customize ngt for that project:

```json
{
  "AngularTestGenerator": {
    "ExcludedDirectories": [
      "node_modules",
      "dist",
      ".angular",
      "coverage",
      "libs/legacy"
    ]
  }
}
```

### Environment-Specific Configuration

Use environment-specific configuration files:

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production overrides

## Common Configuration Scenarios

### Scenario 1: Monorepo with Multiple Apps

For Nx or Angular CLI monorepos, exclude library and app directories you don't want to process:

```json
{
  "AngularTestGenerator": {
    "ExcludedDirectories": [
      "node_modules",
      "dist",
      ".angular",
      "coverage",
      "apps/legacy-app",
      "libs/third-party"
    ]
  }
}
```

### Scenario 2: Strict Coverage Requirements

For projects with high coverage requirements:

```json
{
  "AngularTestGenerator": {
    "TargetCoveragePercentage": 95
  }
}
```

### Scenario 3: Custom Test File Naming

For projects using `.test.ts` instead of `.spec.ts`:

```json
{
  "AngularTestGenerator": {
    "TestFileExtension": ".test.ts"
  }
}
```

### Scenario 4: Including Additional File Types

For projects with custom TypeScript extensions:

```json
{
  "AngularTestGenerator": {
    "TypeScriptExtensions": [".ts", ".mts"]
  }
}
```

## Environment Variables

Configuration can also be set via environment variables using the standard .NET configuration provider pattern:

```bash
export AngularTestGenerator__TargetCoveragePercentage=90
export AngularTestGenerator__TestFileExtension=.test.ts
```

Note: Use double underscores (`__`) to represent nested configuration sections.

## Next Steps

- See [Supported File Types](supported-types.md) for type-specific generation details
- View [Examples](examples.md) of generated tests
- Check [Troubleshooting](troubleshooting.md) for common issues
