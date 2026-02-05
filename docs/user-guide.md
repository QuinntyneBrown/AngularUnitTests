# User Guide

This guide provides comprehensive documentation for using ngt (Angular Unit Test Generator).

## Table of Contents

- [Command Overview](#command-overview)
- [The Generate Command](#the-generate-command)
- [Working Directory Mode](#working-directory-mode)
- [Explicit Path Mode](#explicit-path-mode)
- [File Discovery](#file-discovery)
- [Test File Naming](#test-file-naming)
- [Understanding the Output](#understanding-the-output)

## Command Overview

ngt uses a command-based interface. View available commands:

```bash
ngt --help
```

Output:
```
Description:
  Angular Unit Tests CLI - Generate Jest unit tests for Angular TypeScript files

Usage:
  ngt [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  generate  Generate Jest unit tests for Angular TypeScript files
```

## The Generate Command

The `generate` command is the primary command for creating unit tests.

### Syntax

```bash
ngt generate [options]
```

### Options

| Option | Alias | Description | Required |
|--------|-------|-------------|----------|
| `--path` | `-p` | Path to generate tests for | No |
| `--help` | `-h` | Show help for the command | No |

### Command Help

```bash
ngt generate --help
```

## Working Directory Mode

When you run `ngt generate` without specifying a path, the tool operates in **Working Directory Mode**:

1. **Angular Workspace Detection**: The tool searches for `angular.json` starting from the current directory and walking up the directory tree.

2. **Scoped Generation**: Tests are generated only for TypeScript files in the current directory and its subdirectories (not the entire workspace).

3. **Error Handling**: If no Angular workspace is found, you'll receive an error message with usage instructions.

### Example

```bash
# Navigate to a specific feature folder
cd ~/my-app/src/app/features/users

# Generate tests only for this feature
ngt generate
```

This approach is useful when you want to:
- Generate tests for a specific feature module
- Avoid regenerating tests for the entire application
- Work incrementally on different parts of your codebase

## Explicit Path Mode

Specify an explicit path to generate tests for any directory:

```bash
ngt generate --path /path/to/angular/app
ngt generate -p ./src/app/shared
```

### Relative vs Absolute Paths

Both relative and absolute paths are supported:

```bash
# Absolute path
ngt generate --path /home/user/projects/my-app/src/app

# Relative path (from current directory)
ngt generate --path ./src/app

# Parent directory
ngt generate --path ../other-app/src
```

## File Discovery

### How Files Are Discovered

ngt scans the target directory recursively for TypeScript files (`.ts`) and:

1. **Excludes** certain directories:
   - `node_modules`
   - `dist`
   - `.angular`
   - `coverage`

2. **Excludes** existing test files:
   - Files containing `.spec.` in the name
   - Files containing `.test.` in the name

3. **Categorizes** files by their naming convention:
   - `.component.ts` → Component
   - `.service.ts` → Service
   - `.directive.ts` → Directive
   - `.pipe.ts` → Pipe
   - `.guard.ts` → Guard
   - `.interceptor.ts` → Interceptor
   - `.resolver.ts` → Resolver
   - `.module.ts` → Module
   - Files with `model` or `interface` in name → Model

4. **Analyzes** file content to detect:
   - Functional vs class-based implementations
   - Standalone components/directives
   - Dependencies via `inject()` calls
   - Public methods for service testing

### Skipped Files

The following files are automatically skipped:

- **Interface-only files**: Files that only export interfaces or type aliases
- **Unknown types**: Files that don't match any Angular naming convention
- **Existing test files**: Files already ending in `.spec.ts` or `.test.ts`

## Test File Naming

### Standard Naming

Test files are created alongside source files with the `.spec.ts` extension:

```
user.component.ts → user.component.spec.ts
auth.service.ts → auth.service.spec.ts
```

### Discriminating Values

If a test file already exists, ngt adds a discriminating number:

```
# First run
user.component.ts → user.component.spec.ts

# Second run (original spec exists)
user.component.ts → user.component.spec.2.ts

# Third run (both specs exist)
user.component.ts → user.component.spec.3.ts
```

This prevents overwriting existing test files that may have been customized.

## Understanding the Output

### Status Indicators

| Symbol | Meaning |
|--------|---------|
| `✓` | Test file successfully generated |
| `-` | File skipped (interface/type only) |
| `✗` | Generation failed (with error message) |

### Summary Report

At the end of generation, you'll see a summary:

```
Test generation complete:
  Success: 10    # Tests generated successfully
  Skipped: 2     # Files skipped (interfaces/types)
  Failed: 0      # Generation errors
  Total: 12      # Total files processed
```

### Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success (all files processed without errors) |
| `1` | One or more errors occurred |

### Logging

ngt logs information to the console for tracking progress:

- **Information**: Normal operational messages
- **Warning**: Non-fatal issues (e.g., file analysis problems)
- **Error**: Fatal issues that prevent generation

## Best Practices

### 1. Generate Tests Incrementally

Instead of running on the entire codebase, target specific directories:

```bash
# Generate tests for a new feature
cd src/app/features/new-feature
ngt generate
```

### 2. Review Generated Tests

Generated tests provide a foundation. Review and enhance them:

- Add meaningful test data
- Test edge cases
- Add assertions specific to your business logic

### 3. Customize Configuration

Adjust configuration for your project's needs. See [Configuration](configuration.md) for details.

### 4. Use with CI/CD

Include ngt in your CI pipeline to ensure new code has tests:

```yaml
- name: Generate missing tests
  run: ngt generate --path ./src/app
```

### 5. Version Control

After generating tests:
1. Review the generated files
2. Customize as needed
3. Commit to version control

## Next Steps

- Learn about [Configuration Options](configuration.md)
- See [Supported File Types](supported-types.md) for type-specific details
- View [Examples](examples.md) of generated tests
