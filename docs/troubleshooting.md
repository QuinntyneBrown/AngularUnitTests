# Troubleshooting

This guide helps you resolve common issues when using ngt.

## Installation Issues

### "ngt" command not found

**Symptom**: After installation, running `ngt` returns "command not found".

**Solutions**:

1. **Verify installation**:
   ```bash
   dotnet tool list --global
   ```
   Look for `ngt` in the list.

2. **Check PATH**: Ensure the .NET tools directory is in your PATH:
   - **Windows**: `%USERPROFILE%\.dotnet\tools`
   - **Linux/macOS**: `$HOME/.dotnet/tools`

3. **Restart terminal**: Close and reopen your terminal after installation.

4. **Manual PATH update** (Linux/macOS):
   ```bash
   export PATH="$PATH:$HOME/.dotnet/tools"
   ```
   Add this to your `~/.bashrc` or `~/.zshrc` for persistence.

### .NET SDK not found

**Symptom**: Error about missing .NET SDK.

**Solution**: Install .NET 9.0 SDK or later from [dotnet.microsoft.com](https://dotnet.microsoft.com/download).

Verify installation:
```bash
dotnet --version
```

### Installation fails with package source error

**Symptom**: Package source errors during installation.

**Solutions**:

1. **Clear NuGet cache**:
   ```bash
   dotnet nuget locals all --clear
   ```

2. **Restore default sources**:
   ```bash
   dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
   ```

## Runtime Issues

### "Not in an Angular workspace" error

**Symptom**:
```
Error: Not in an Angular workspace. No angular.json found.
```

**Solutions**:

1. **Navigate to workspace**: Ensure you're inside an Angular project directory:
   ```bash
   cd /path/to/angular-project
   ngt generate
   ```

2. **Specify path explicitly**:
   ```bash
   ngt generate --path /path/to/angular/app/src
   ```

3. **Verify angular.json exists**: The tool looks for `angular.json` to identify Angular workspaces.

### No TypeScript files found

**Symptom**:
```
No TypeScript files found in: /path/to/directory
```

**Causes and solutions**:

1. **Wrong directory**: Ensure you're in a directory containing `.ts` files:
   ```bash
   ls *.ts  # Should list TypeScript files
   ```

2. **All files excluded**: Check if files are in excluded directories:
   - `node_modules`
   - `dist`
   - `.angular`
   - `coverage`

3. **Only test files exist**: ngt skips `.spec.ts` and `.test.ts` files.

4. **Unknown file types**: Files must follow Angular naming conventions (see [Supported Types](supported-types.md)).

### Files skipped unexpectedly

**Symptom**: Expected files show as "Skipped".

**Common reasons**:

1. **Interface-only files**: Files containing only interfaces or type aliases are skipped:
   ```
   - Skipped: user.model (interface/type only)
   ```
   This is intentional—there's no runtime code to test.

2. **Unknown file type**: Files not matching Angular naming conventions are skipped.

### Generated tests don't compile

**Symptom**: TypeScript errors in generated test files.

**Common issues and fixes**:

1. **Missing imports**: Add required imports for your specific types:
   ```typescript
   import { User } from '../models/user.model';
   ```

2. **Vitest vs Jest**: Generated tests use Vitest syntax. For Jest:
   ```typescript
   // Change from:
   import { vi, Mock } from 'vitest';
   vi.fn()
   vi.spyOn()

   // To:
   jest.fn()
   jest.spyOn()
   ```

3. **AuthService path**: Update the import path for your AuthService:
   ```typescript
   // Change from:
   import { AuthService } from '../../shared/services/auth.service';

   // To your actual path:
   import { AuthService } from '@app/core/services/auth.service';
   ```

4. **API_BASE_URL path**: Update the injection token import:
   ```typescript
   // Change from:
   import { API_BASE_URL } from '../config/api.config';

   // To your actual path:
   import { API_BASE_URL } from '@app/core/config';
   ```

### Tests fail at runtime

**Symptom**: Generated tests fail when run.

**Common issues**:

1. **Missing TestBed providers**: Add missing service providers:
   ```typescript
   providers: [
     provideHttpClient(),
     provideHttpClientTesting(),
     { provide: MyService, useValue: mockMyService },
   ],
   ```

2. **Async issues**: Wrap async operations:
   ```typescript
   it('should do something async', async () => {
     await fixture.whenStable();
     // assertions
   });
   ```

3. **Change detection**: Trigger change detection after updates:
   ```typescript
   component.someProperty = 'new value';
   fixture.detectChanges();
   ```

## File Handling Issues

### Test files overwritten

**Symptom**: Existing test customizations are lost.

**Explanation**: By design, ngt creates new files with discriminating numbers when tests exist:
```
user.component.spec.ts      # Original (not touched)
user.component.spec.2.ts    # New generated file
```

If you want to regenerate, delete the existing spec file first.

### Wrong class name in generated tests

**Symptom**: Generated test uses incorrect class name.

**Cause**: ngt extracts class names from file content and file name patterns. Complex exports may not be detected correctly.

**Solution**: Manually update the class name in the generated test.

### Incorrect import paths

**Symptom**: Import paths in generated tests are incorrect.

**Explanation**: ngt uses relative paths based on file location. For complex project structures with path aliases, you may need to update imports.

**Solution**: Update imports to use your project's path aliases:
```typescript
// Generated:
import { UserService } from './user.service';

// Updated for path aliases:
import { UserService } from '@app/services/user.service';
```

## Performance Issues

### Generation is slow

**Solutions**:

1. **Target specific directories**:
   ```bash
   cd src/app/features/new-feature
   ngt generate
   ```

2. **Check excluded directories**: Ensure `node_modules` is excluded (default).

3. **Reduce scope**: Generate tests for smaller portions of your codebase.

### Memory issues

**Symptom**: Process runs out of memory on large codebases.

**Solutions**:

1. **Process in batches**: Run ngt on subdirectories separately.

2. **Check for circular imports**: These can cause analysis issues.

## Getting Help

### Debug Information

Enable verbose logging for troubleshooting:

Create or update `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Reporting Issues

If you encounter a bug:

1. **Check existing issues**: [GitHub Issues](https://github.com/QuinntyneBrown/AngularUnitTests/issues)

2. **Create a new issue** with:
   - ngt version (`ngt --version`)
   - .NET version (`dotnet --version`)
   - Operating system
   - Steps to reproduce
   - Expected vs actual behavior
   - Sample file (if possible)

### Feature Requests

For feature requests, create a GitHub issue with:
- Description of the feature
- Use case / motivation
- Proposed implementation (optional)

## FAQ

### Q: Can I use ngt with Jest instead of Vitest?

A: Yes, but you'll need to update the mock syntax in generated tests. See the "Generated tests don't compile" section above.

### Q: Does ngt support NX workspaces?

A: Yes, ngt works with any Angular project that has `angular.json`. For Nx workspaces, navigate to the specific app or library directory.

### Q: Can I customize the test templates?

A: Currently, test templates are built into the tool. Custom templates are on the roadmap. See [Contributing](contributing.md) for how to help.

### Q: Why are my models skipped?

A: Files containing only interfaces or type aliases have no runtime code to test, so they're intentionally skipped.

### Q: How do I regenerate tests for a file?

A: Delete the existing `.spec.ts` file, then run ngt again. Alternatively, keep both and merge manually.

## Next Steps

- Read the [User Guide](user-guide.md) for detailed usage
- See [Configuration](configuration.md) to customize behavior
- Check [Contributing](contributing.md) to help improve ngt
