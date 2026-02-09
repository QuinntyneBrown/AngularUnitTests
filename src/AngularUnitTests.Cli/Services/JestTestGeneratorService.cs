using AngularUnitTests.Cli.Configuration;
using AngularUnitTests.Cli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.RegularExpressions;

namespace AngularUnitTests.Cli.Services;

public interface IJestTestGeneratorService
{
    void SetDiscoveredFiles(IReadOnlyList<TypeScriptFileInfo> allFiles);
    Task<string?> GenerateTestFileAsync(TypeScriptFileInfo fileInfo, CancellationToken cancellationToken = default);
}

public class JestTestGeneratorService : IJestTestGeneratorService
{
    private readonly ILogger<JestTestGeneratorService> _logger;
    private readonly AngularTestGeneratorOptions _options;

    private IReadOnlyList<TypeScriptFileInfo> _allDiscoveredFiles = Array.Empty<TypeScriptFileInfo>();
    private Dictionary<string, TypeScriptFileInfo> _dependencyLookup = new();

    // Known Angular framework dependencies handled via test utilities (not custom mocked)
    private static readonly HashSet<string> AngularFrameworkDeps = new()
    {
        "HttpClient", "Router", "ActivatedRoute", "FormBuilder",
        "ChangeDetectorRef", "ElementRef", "Renderer2", "NgZone",
        "Injector", "ApplicationRef", "ViewContainerRef", "TemplateRef",
        "Location", "Title", "Meta", "DOCUMENT"
    };

    // Known injection tokens (provided with mock values)
    private static readonly HashSet<string> InjectionTokenDeps = new()
    {
        "API_BASE_URL"
    };

    // Regex for finding import paths in source files
    private static readonly Regex ImportPathPattern = new(
        @"import\s*\{[^}]*\b{0}\b[^}]*\}\s*from\s*['""]([^'""]+)['""]",
        RegexOptions.Compiled);

    public JestTestGeneratorService(
        ILogger<JestTestGeneratorService> logger,
        IOptions<AngularTestGeneratorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public void SetDiscoveredFiles(IReadOnlyList<TypeScriptFileInfo> allFiles)
    {
        _allDiscoveredFiles = allFiles;
        _dependencyLookup = allFiles
            .Where(f => !string.IsNullOrEmpty(f.ClassName))
            .GroupBy(f => f.ClassName)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task<string?> GenerateTestFileAsync(
        TypeScriptFileInfo fileInfo,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating test for: {FilePath}", fileInfo.FilePath);

        // Skip interface/type-only model files
        if (fileInfo.FileType == TypeScriptFileType.Model && fileInfo.IsInterfaceOrType)
        {
            _logger.LogInformation("Skipping interface/type-only file: {FilePath}", fileInfo.FilePath);
            return null;
        }

        var testFilePath = DetermineTestFilePath(fileInfo);
        var testContent = await GenerateTestContentAsync(fileInfo, cancellationToken);

        if (string.IsNullOrEmpty(testContent))
        {
            _logger.LogInformation("No test content generated for: {FilePath}", fileInfo.FilePath);
            return null;
        }

        await File.WriteAllTextAsync(testFilePath, testContent, cancellationToken);

        _logger.LogInformation("Generated test file: {TestFilePath}", testFilePath);
        return testFilePath;
    }

    private string DetermineTestFilePath(TypeScriptFileInfo fileInfo)
    {
        var directory = Path.GetDirectoryName(fileInfo.FilePath)!;
        var fileName = Path.GetFileNameWithoutExtension(fileInfo.FilePath);

        if (fileInfo.ExistingTestFileCount == 0)
        {
            return Path.Combine(directory, $"{fileName}.spec.ts");
        }

        // Add discriminating value
        var discriminator = fileInfo.ExistingTestFileCount + 1;
        return Path.Combine(directory, $"{fileName}.spec.{discriminator}.ts");
    }

    private async Task<string> GenerateTestContentAsync(
        TypeScriptFileInfo fileInfo,
        CancellationToken cancellationToken)
    {
        return fileInfo.FileType switch
        {
            TypeScriptFileType.Component => GenerateComponentTest(fileInfo),
            TypeScriptFileType.Service => GenerateServiceTest(fileInfo),
            TypeScriptFileType.Directive => GenerateDirectiveTest(fileInfo),
            TypeScriptFileType.Pipe => GeneratePipeTest(fileInfo),
            TypeScriptFileType.Guard => GenerateGuardTest(fileInfo),
            TypeScriptFileType.Interceptor => GenerateInterceptorTest(fileInfo),
            TypeScriptFileType.Resolver => GenerateResolverTest(fileInfo),
            TypeScriptFileType.Model => GenerateModelTest(fileInfo),
            _ => GenerateGenericTest(fileInfo)
        };
    }

    // ================================================================
    // Helper Methods for Dependency Mocking
    // ================================================================

    /// <summary>
    /// Returns custom dependencies that need explicit mocking (not framework or token deps).
    /// </summary>
    private List<string> GetCustomDependencies(TypeScriptFileInfo fileInfo)
    {
        return fileInfo.Dependencies
            .Where(d => !AngularFrameworkDeps.Contains(d) && !InjectionTokenDeps.Contains(d))
            .ToList();
    }

    /// <summary>
    /// Resolves the import path for a dependency by checking the source file's own imports first,
    /// then computing a relative path from the discovered files, and falling back to a convention guess.
    /// </summary>
    private string GetDependencyImportPath(string sourceFilePath, string dependencyName)
    {
        // First: try to find the import path from the source file's own imports
        var sourceFileInfo = _allDiscoveredFiles.FirstOrDefault(f => f.FilePath == sourceFilePath);
        if (sourceFileInfo != null && !string.IsNullOrEmpty(sourceFileInfo.FileContent))
        {
            var pattern = new Regex($@"import\s*\{{[^}}]*\b{Regex.Escape(dependencyName)}\b[^}}]*\}}\s*from\s*['""]([^'""]+)['""]");
            var match = pattern.Match(sourceFileInfo.FileContent);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        // Second: try to find the dependency file in discovered files and compute relative path
        if (_dependencyLookup.TryGetValue(dependencyName, out var depInfo))
        {
            return GetRelativeImportPath(sourceFilePath, depInfo.FilePath);
        }

        // Fallback: guess based on naming conventions
        var kebabName = ConvertToKebabCase(dependencyName);
        return $"../shared/services/{kebabName}";
    }

    /// <summary>
    /// Computes a relative TypeScript import path from one file to another.
    /// </summary>
    private string GetRelativeImportPath(string fromFilePath, string toFilePath)
    {
        var fromDir = Path.GetDirectoryName(fromFilePath)!;
        var relativePath = Path.GetRelativePath(fromDir, toFilePath);
        relativePath = relativePath.Replace('\\', '/');
        if (relativePath.EndsWith(".ts"))
            relativePath = relativePath[..^3];
        if (!relativePath.StartsWith("./") && !relativePath.StartsWith("../"))
            relativePath = "./" + relativePath;
        return relativePath;
    }

    /// <summary>
    /// Converts a PascalCase class name to kebab-case file name.
    /// e.g., AuthService -> auth.service, TimeEntryService -> time-entry.service
    /// </summary>
    private string ConvertToKebabCase(string className)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < className.Length; i++)
        {
            if (char.IsUpper(className[i]) && i > 0)
            {
                sb.Append('-');
            }
            sb.Append(char.ToLower(className[i]));
        }

        var result = sb.ToString();

        // Convert last type suffix from kebab to dot notation
        string[] suffixes = { "-service", "-guard", "-interceptor", "-resolver", "-pipe", "-directive", "-component" };
        foreach (var suffix in suffixes)
        {
            if (result.EndsWith(suffix))
            {
                result = result[..^suffix.Length] + suffix.Replace("-", ".");
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Returns a mock variable name for a dependency class.
    /// e.g., AuthService -> mockAuthService
    /// </summary>
    private string GetMockVarName(string className)
    {
        return "mock" + className;
    }

    /// <summary>
    /// Looks up the public method names for a dependency class from discovery data.
    /// </summary>
    private List<string> GetServiceMethods(string className)
    {
        if (_dependencyLookup.TryGetValue(className, out var serviceInfo))
        {
            return serviceInfo.PublicMethods.Select(m => m.Name).ToList();
        }
        return new List<string>();
    }

    /// <summary>
    /// Writes vitest import if custom dependencies need mocking.
    /// </summary>
    private void WriteVitestImport(StringBuilder sb, bool needsMocking)
    {
        if (needsMocking)
        {
            sb.AppendLine("import { vi } from 'vitest';");
        }
    }

    /// <summary>
    /// Writes Angular framework test imports (HttpClient testing, Router, etc.)
    /// </summary>
    private void WriteFrameworkImports(StringBuilder sb, TypeScriptFileInfo fileInfo)
    {
        var needsHttp = fileInfo.Dependencies.Contains("HttpClient");
        var needsRouter = fileInfo.Dependencies.Contains("Router") || fileInfo.Dependencies.Contains("ActivatedRoute");
        var needsApiBaseUrl = fileInfo.Dependencies.Contains("API_BASE_URL");

        if (needsHttp)
        {
            sb.AppendLine("import { provideHttpClient } from '@angular/common/http';");
            sb.AppendLine("import { provideHttpClientTesting } from '@angular/common/http/testing';");
        }
        if (needsRouter)
        {
            sb.AppendLine("import { provideRouter } from '@angular/router';");
        }
        if (needsApiBaseUrl)
        {
            var importPath = GetDependencyImportPath(fileInfo.FilePath, "API_BASE_URL");
            sb.AppendLine($"import {{ API_BASE_URL }} from '{importPath}';");
        }
    }

    /// <summary>
    /// Writes import statements for custom dependencies that will be mocked.
    /// </summary>
    private void WriteCustomDepImports(StringBuilder sb, TypeScriptFileInfo fileInfo, List<string> customDeps)
    {
        foreach (var dep in customDeps)
        {
            var importPath = GetDependencyImportPath(fileInfo.FilePath, dep);
            sb.AppendLine($"import {{ {dep} }} from '{importPath}';");
        }
    }

    /// <summary>
    /// Writes mock variable declarations for custom dependencies.
    /// </summary>
    private void WriteMockDeclarations(StringBuilder sb, List<string> customDeps, string indent)
    {
        foreach (var dep in customDeps)
        {
            var varName = GetMockVarName(dep);
            sb.AppendLine($"{indent}let {varName}: any;");
        }
    }

    /// <summary>
    /// Writes mock object creation with vi.fn() for each public method.
    /// </summary>
    private void WriteMockCreation(StringBuilder sb, List<string> customDeps, string indent)
    {
        foreach (var dep in customDeps)
        {
            var varName = GetMockVarName(dep);
            var methods = GetServiceMethods(dep);

            if (methods.Any())
            {
                sb.AppendLine($"{indent}{varName} = {{");
                foreach (var method in methods)
                {
                    sb.AppendLine($"{indent}  {method}: vi.fn(),");
                }
                sb.AppendLine($"{indent}}};");
            }
            else
            {
                sb.AppendLine($"{indent}{varName} = {{}};");
            }
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Writes TestBed providers for framework deps, injection tokens, and custom dependency mocks.
    /// </summary>
    private void WriteProviders(StringBuilder sb, TypeScriptFileInfo fileInfo, List<string> customDeps, string indent)
    {
        var needsHttp = fileInfo.Dependencies.Contains("HttpClient");
        var needsRouter = fileInfo.Dependencies.Contains("Router") || fileInfo.Dependencies.Contains("ActivatedRoute");
        var needsApiBaseUrl = fileInfo.Dependencies.Contains("API_BASE_URL");

        sb.AppendLine($"{indent}providers: [");

        if (needsHttp)
        {
            sb.AppendLine($"{indent}  provideHttpClient(),");
            sb.AppendLine($"{indent}  provideHttpClientTesting(),");
        }
        if (needsRouter)
        {
            sb.AppendLine($"{indent}  provideRouter([]),");
        }
        if (needsApiBaseUrl)
        {
            sb.AppendLine($"{indent}  {{ provide: API_BASE_URL, useValue: 'http://localhost:3000' }},");
        }
        foreach (var dep in customDeps)
        {
            var varName = GetMockVarName(dep);
            sb.AppendLine($"{indent}  {{ provide: {dep}, useValue: {varName} }},");
        }

        sb.AppendLine($"{indent}],");
    }

    /// <summary>
    /// Returns true if the file has any dependencies that need providers.
    /// </summary>
    private bool HasAnyDependencies(TypeScriptFileInfo fileInfo)
    {
        return fileInfo.Dependencies.Any();
    }

    // ================================================================
    // Test Generator Methods
    // ================================================================

    private string GenerateComponentTest(TypeScriptFileInfo fileInfo)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}".Replace(".ts", "");
        var className = fileInfo.ClassName;
        var customDeps = GetCustomDependencies(fileInfo);
        var hasDeps = HasAnyDependencies(fileInfo);

        // Imports
        sb.AppendLine("import { ComponentFixture, TestBed } from '@angular/core/testing';");
        WriteVitestImport(sb, customDeps.Any());
        WriteFrameworkImports(sb, fileInfo);
        WriteCustomDepImports(sb, fileInfo, customDeps);
        sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
        sb.AppendLine();

        // Describe block
        sb.AppendLine($"describe('{className}', () => {{");
        sb.AppendLine($"  let component: {className};");
        sb.AppendLine($"  let fixture: ComponentFixture<{className}>;");
        WriteMockDeclarations(sb, customDeps, "  ");
        sb.AppendLine();

        sb.AppendLine("  beforeEach(async () => {");

        // Create mocks
        WriteMockCreation(sb, customDeps, "    ");

        sb.AppendLine("    await TestBed.configureTestingModule({");

        if (fileInfo.IsStandalone)
        {
            sb.AppendLine($"      imports: [{className}],");
        }
        else
        {
            sb.AppendLine($"      declarations: [{className}],");
        }

        if (hasDeps)
        {
            WriteProviders(sb, fileInfo, customDeps, "      ");
        }

        sb.AppendLine("    }).compileComponents();");
        sb.AppendLine();
        sb.AppendLine($"    fixture = TestBed.createComponent({className});");
        sb.AppendLine("    component = fixture.componentInstance;");
        sb.AppendLine("    fixture.detectChanges();");
        sb.AppendLine("  });");
        sb.AppendLine();
        sb.AppendLine("  it('should create', () => {");
        sb.AppendLine("    expect(component).toBeTruthy();");
        sb.AppendLine("  });");
        sb.AppendLine();
        sb.AppendLine("  it('should render component', () => {");
        sb.AppendLine("    expect(fixture.nativeElement).toBeTruthy();");
        sb.AppendLine("  });");
        sb.AppendLine("});");

        return sb.ToString();
    }

    private string GenerateServiceTest(TypeScriptFileInfo fileInfo)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}".Replace(".ts", "");
        var className = fileInfo.ClassName;

        var needsHttp = fileInfo.Dependencies.Contains("HttpClient");
        var needsRouter = fileInfo.Dependencies.Contains("Router");
        var needsApiBaseUrl = fileInfo.Dependencies.Contains("API_BASE_URL");
        var customDeps = GetCustomDependencies(fileInfo);

        // Imports
        sb.AppendLine("import { TestBed } from '@angular/core/testing';");
        WriteVitestImport(sb, customDeps.Any());

        if (needsHttp)
        {
            sb.AppendLine("import { provideHttpClient } from '@angular/common/http';");
            sb.AppendLine("import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';");
        }
        if (needsRouter)
        {
            sb.AppendLine("import { provideRouter, Router } from '@angular/router';");
        }
        if (needsApiBaseUrl)
        {
            var apiConfigPath = GetDependencyImportPath(fileInfo.FilePath, "API_BASE_URL");
            sb.AppendLine($"import {{ API_BASE_URL }} from '{apiConfigPath}';");
        }

        WriteCustomDepImports(sb, fileInfo, customDeps);
        sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
        sb.AppendLine();

        // Describe block
        sb.AppendLine($"describe('{className}', () => {{");
        sb.AppendLine($"  let service: {className};");

        if (needsHttp)
        {
            sb.AppendLine("  let httpMock: HttpTestingController;");
        }
        if (needsRouter)
        {
            sb.AppendLine("  let router: Router;");
        }
        WriteMockDeclarations(sb, customDeps, "  ");

        sb.AppendLine();
        sb.AppendLine("  beforeEach(() => {");

        // Create mocks for custom deps
        WriteMockCreation(sb, customDeps, "    ");

        sb.AppendLine("    TestBed.configureTestingModule({");
        sb.AppendLine("      providers: [");

        if (needsHttp)
        {
            sb.AppendLine("        provideHttpClient(),");
            sb.AppendLine("        provideHttpClientTesting(),");
        }
        if (needsRouter)
        {
            sb.AppendLine("        provideRouter([]),");
        }
        if (needsApiBaseUrl)
        {
            sb.AppendLine("        { provide: API_BASE_URL, useValue: 'http://localhost:3000' },");
        }
        foreach (var dep in customDeps)
        {
            var varName = GetMockVarName(dep);
            sb.AppendLine($"        {{ provide: {dep}, useValue: {varName} }},");
        }

        sb.AppendLine("      ],");
        sb.AppendLine("    });");
        sb.AppendLine();
        sb.AppendLine($"    service = TestBed.inject({className});");

        if (needsHttp)
        {
            sb.AppendLine("    httpMock = TestBed.inject(HttpTestingController);");
        }
        if (needsRouter)
        {
            sb.AppendLine("    router = TestBed.inject(Router);");
        }

        sb.AppendLine("  });");
        sb.AppendLine();

        if (needsHttp)
        {
            sb.AppendLine("  afterEach(() => {");
            sb.AppendLine("    httpMock.verify();");
            sb.AppendLine("  });");
            sb.AppendLine();
        }

        sb.AppendLine("  it('should be created', () => {");
        sb.AppendLine("    expect(service).toBeTruthy();");
        sb.AppendLine("  });");

        // Generate tests for each public method that returns Observable (excluding Blob types)
        if (needsHttp && fileInfo.PublicMethods.Any())
        {
            var httpMethods = fileInfo.PublicMethods
                .Where(m => m.ReturnType.Contains("Observable"))
                .Where(m => !m.ReturnType.Contains("Blob"))  // Skip Blob responses
                .ToList();

            foreach (var method in httpMethods)
            {
                sb.AppendLine();
                GenerateHttpMethodTest(sb, method, className);
            }
        }

        sb.AppendLine("});");

        return sb.ToString();
    }

    private void GenerateHttpMethodTest(StringBuilder sb, Models.MethodInfo method, string className)
    {
        var methodName = method.Name;
        var isGetMethod = methodName.StartsWith("get") || methodName.StartsWith("Get");
        var isPostMethod = methodName.StartsWith("create") || methodName.StartsWith("Create") ||
                          methodName.StartsWith("add") || methodName.StartsWith("Add") ||
                          methodName.StartsWith("archive") || methodName.StartsWith("Archive") ||
                          methodName.StartsWith("restore") || methodName.StartsWith("Restore") ||
                          methodName.StartsWith("login") || methodName.StartsWith("Login") ||
                          methodName.StartsWith("logout") || methodName.StartsWith("Logout") ||
                          methodName.StartsWith("register") || methodName.StartsWith("Register") ||
                          methodName.StartsWith("refresh") || methodName.StartsWith("Refresh") ||
                          methodName.StartsWith("mark") || methodName.StartsWith("Mark") ||
                          methodName.StartsWith("void") || methodName.StartsWith("Void") ||
                          methodName.StartsWith("send") || methodName.StartsWith("Send") ||
                          methodName.StartsWith("submit") || methodName.StartsWith("Submit") ||
                          methodName.StartsWith("approve") || methodName.StartsWith("Approve") ||
                          methodName.StartsWith("reject") || methodName.StartsWith("Reject") ||
                          methodName.StartsWith("cancel") || methodName.StartsWith("Cancel");
        var isPutMethod = methodName.StartsWith("update") || methodName.StartsWith("Update");
        var isDeleteMethod = methodName.StartsWith("delete") || methodName.StartsWith("Delete") ||
                            methodName.StartsWith("remove") || methodName.StartsWith("Remove");

        var httpMethod = isGetMethod ? "GET" :
                        isPostMethod ? "POST" :
                        isPutMethod ? "PUT" :
                        isDeleteMethod ? "DELETE" : "GET";

        sb.AppendLine($"  describe('{methodName}', () => {{");
        sb.AppendLine($"    it('should make {httpMethod} request', () => {{");

        // Generate mock parameters
        var paramsList = new List<string>();
        foreach (var param in method.Parameters)
        {
            var mockValue = GetMockValue(param.Type, param.Name);
            paramsList.Add(mockValue);
        }
        var paramsCall = string.Join(", ", paramsList);

        // Generate the test
        sb.AppendLine($"      const mockResponse = {{}};");
        sb.AppendLine();
        sb.AppendLine($"      service.{methodName}({paramsCall}).subscribe((response) => {{");
        sb.AppendLine("        expect(response).toBeDefined();");
        sb.AppendLine("      });");
        sb.AppendLine();
        sb.AppendLine($"      const req = httpMock.expectOne((request) => request.method === '{httpMethod}');");
        sb.AppendLine("      req.flush(mockResponse);");
        sb.AppendLine("    });");
        sb.AppendLine("  });");
    }

    private string GetMockValue(string type, string paramName)
    {
        // Generate appropriate mock values based on parameter type
        var normalizedType = type.Trim().ToLowerInvariant();
        var normalizedName = paramName.ToLowerInvariant();

        if (normalizedType == "string")
            return "'test-value'";
        if (normalizedType == "number")
            return "1";
        if (normalizedType == "boolean")
            return "true";
        if (normalizedName.Contains("id") && normalizedType == "string")
            return "'test-id'";

        // For params/request types, use 'any' cast to avoid import issues
        if (type.EndsWith("Params"))
        {
            return "{ userId: 'test-user' } as any";
        }

        if (type.EndsWith("Request"))
        {
            return "{} as any";
        }

        // For complex types, return an empty object with any cast
        return "{} as any";
    }

    private string GenerateDirectiveTest(TypeScriptFileInfo fileInfo)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}".Replace(".ts", "");
        var className = fileInfo.ClassName;
        var customDeps = GetCustomDependencies(fileInfo);
        var hasDeps = HasAnyDependencies(fileInfo);

        sb.AppendLine("import { TestBed, ComponentFixture } from '@angular/core/testing';");
        sb.AppendLine("import { Component } from '@angular/core';");
        WriteVitestImport(sb, customDeps.Any());
        WriteFrameworkImports(sb, fileInfo);
        WriteCustomDepImports(sb, fileInfo, customDeps);
        sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
        sb.AppendLine();
        sb.AppendLine("@Component({");
        sb.AppendLine("  template: '<input type=\"text\" />',");
        sb.AppendLine("  standalone: true,");
        sb.AppendLine($"  imports: [{className}],");
        sb.AppendLine("})");
        sb.AppendLine("class TestHostComponent {}");
        sb.AppendLine();
        sb.AppendLine($"describe('{className}', () => {{");
        sb.AppendLine("  let fixture: ComponentFixture<TestHostComponent>;");
        WriteMockDeclarations(sb, customDeps, "  ");
        sb.AppendLine();
        sb.AppendLine("  beforeEach(async () => {");

        WriteMockCreation(sb, customDeps, "    ");

        sb.AppendLine("    await TestBed.configureTestingModule({");
        sb.AppendLine("      imports: [TestHostComponent],");

        if (hasDeps)
        {
            WriteProviders(sb, fileInfo, customDeps, "      ");
        }

        sb.AppendLine("    }).compileComponents();");
        sb.AppendLine();
        sb.AppendLine("    fixture = TestBed.createComponent(TestHostComponent);");
        sb.AppendLine("    fixture.detectChanges();");
        sb.AppendLine("  });");
        sb.AppendLine();
        sb.AppendLine("  it('should create host component', () => {");
        sb.AppendLine("    expect(fixture.componentInstance).toBeTruthy();");
        sb.AppendLine("  });");
        sb.AppendLine();
        sb.AppendLine("  it('should apply directive', () => {");
        sb.AppendLine("    const input = fixture.nativeElement.querySelector('input');");
        sb.AppendLine("    expect(input).toBeTruthy();");
        sb.AppendLine("  });");
        sb.AppendLine("});");

        return sb.ToString();
    }

    private string GeneratePipeTest(TypeScriptFileInfo fileInfo)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}".Replace(".ts", "");
        var className = fileInfo.ClassName;
        var customDeps = GetCustomDependencies(fileInfo);
        var hasDeps = HasAnyDependencies(fileInfo);

        if (hasDeps)
        {
            // Pipe with dependencies: use TestBed
            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            WriteVitestImport(sb, customDeps.Any());
            WriteFrameworkImports(sb, fileInfo);
            WriteCustomDepImports(sb, fileInfo, customDeps);
            sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{className}', () => {{");
            sb.AppendLine($"  let pipe: {className};");
            WriteMockDeclarations(sb, customDeps, "  ");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");
            WriteMockCreation(sb, customDeps, "    ");
            sb.AppendLine("    TestBed.configureTestingModule({");
            WriteProviders(sb, fileInfo, customDeps, "      ");
            sb.AppendLine("    });");
            sb.AppendLine($"    pipe = TestBed.inject({className});");
            sb.AppendLine("  });");
        }
        else
        {
            // Simple pipe: direct instantiation
            sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{className}', () => {{");
            sb.AppendLine($"  let pipe: {className};");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");
            sb.AppendLine($"    pipe = new {className}();");
            sb.AppendLine("  });");
        }

        sb.AppendLine();
        sb.AppendLine("  it('should create pipe', () => {");
        sb.AppendLine("    expect(pipe).toBeTruthy();");
        sb.AppendLine("  });");
        sb.AppendLine();
        sb.AppendLine("  it('should transform value', () => {");
        sb.AppendLine("    const result = pipe.transform('test');");
        sb.AppendLine("    expect(result).toBeDefined();");
        sb.AppendLine("  });");
        sb.AppendLine("});");

        return sb.ToString();
    }

    private string GenerateGuardTest(TypeScriptFileInfo fileInfo)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}".Replace(".ts", "");
        var exportName = fileInfo.ExportName;
        var customDeps = GetCustomDependencies(fileInfo);

        if (fileInfo.IsFunctional)
        {
            // Functional guard test using TestBed.runInInjectionContext
            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            sb.AppendLine("import { provideRouter, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';");
            WriteVitestImport(sb, customDeps.Any());
            WriteCustomDepImports(sb, fileInfo, customDeps);
            sb.AppendLine($"import {{ {exportName} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{exportName}', () => {{");

            WriteMockDeclarations(sb, customDeps, "  ");
            sb.AppendLine("  let router: Router;");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");

            WriteMockCreation(sb, customDeps, "    ");

            sb.AppendLine("    TestBed.configureTestingModule({");
            sb.AppendLine("      providers: [");
            sb.AppendLine("        provideRouter([]),");
            foreach (var dep in customDeps)
            {
                var varName = GetMockVarName(dep);
                sb.AppendLine($"        {{ provide: {dep}, useValue: {varName} }},");
            }
            sb.AppendLine("      ],");
            sb.AppendLine("    });");
            sb.AppendLine();
            sb.AppendLine("    router = TestBed.inject(Router);");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  it('should be defined', () => {");
            sb.AppendLine($"    expect({exportName}).toBeDefined();");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  it('should return a result when executed', () => {");
            sb.AppendLine("    const result = TestBed.runInInjectionContext(() => {");
            sb.AppendLine("      const mockRoute = {} as ActivatedRouteSnapshot;");
            sb.AppendLine("      const mockState = { url: '/test' } as RouterStateSnapshot;");
            sb.AppendLine($"      return {exportName}(mockRoute, mockState);");
            sb.AppendLine("    });");
            sb.AppendLine();
            sb.AppendLine("    expect(result).toBeDefined();");
            sb.AppendLine("  });");
            sb.AppendLine("});");
        }
        else
        {
            // Class-based guard test
            var className = fileInfo.ClassName;

            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            WriteVitestImport(sb, customDeps.Any());
            WriteFrameworkImports(sb, fileInfo);
            WriteCustomDepImports(sb, fileInfo, customDeps);
            sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{className}', () => {{");
            sb.AppendLine($"  let guard: {className};");
            WriteMockDeclarations(sb, customDeps, "  ");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");
            WriteMockCreation(sb, customDeps, "    ");
            sb.AppendLine("    TestBed.configureTestingModule({");

            if (HasAnyDependencies(fileInfo))
            {
                WriteProviders(sb, fileInfo, customDeps, "      ");
            }
            else
            {
                sb.AppendLine($"      providers: [{className}],");
            }

            sb.AppendLine("    });");
            sb.AppendLine($"    guard = TestBed.inject({className});");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  it('should be created', () => {");
            sb.AppendLine("    expect(guard).toBeTruthy();");
            sb.AppendLine("  });");
            sb.AppendLine("});");
        }

        return sb.ToString();
    }

    private string GenerateInterceptorTest(TypeScriptFileInfo fileInfo)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}".Replace(".ts", "");
        var exportName = fileInfo.ExportName;
        var customDeps = GetCustomDependencies(fileInfo);

        if (fileInfo.IsFunctional)
        {
            // Functional interceptor test
            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            sb.AppendLine("import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';");
            sb.AppendLine("import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';");
            sb.AppendLine("import { provideRouter } from '@angular/router';");
            WriteVitestImport(sb, customDeps.Any());
            WriteCustomDepImports(sb, fileInfo, customDeps);
            sb.AppendLine($"import {{ {exportName} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{exportName}', () => {{");
            sb.AppendLine("  let httpClient: HttpClient;");
            sb.AppendLine("  let httpMock: HttpTestingController;");
            WriteMockDeclarations(sb, customDeps, "  ");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");

            WriteMockCreation(sb, customDeps, "    ");

            sb.AppendLine("    TestBed.configureTestingModule({");
            sb.AppendLine("      providers: [");
            sb.AppendLine($"        provideHttpClient(withInterceptors([{exportName}])),");
            sb.AppendLine("        provideHttpClientTesting(),");
            sb.AppendLine("        provideRouter([]),");

            foreach (var dep in customDeps)
            {
                var varName = GetMockVarName(dep);
                sb.AppendLine($"        {{ provide: {dep}, useValue: {varName} }},");
            }

            sb.AppendLine("      ],");
            sb.AppendLine("    });");
            sb.AppendLine();
            sb.AppendLine("    httpClient = TestBed.inject(HttpClient);");
            sb.AppendLine("    httpMock = TestBed.inject(HttpTestingController);");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  afterEach(() => {");
            sb.AppendLine("    httpMock.verify();");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  it('should intercept requests', () => {");
            sb.AppendLine("    httpClient.get('/api/test').subscribe();");
            sb.AppendLine();
            sb.AppendLine("    const req = httpMock.expectOne('/api/test');");
            sb.AppendLine("    expect(req.request.method).toBe('GET');");
            sb.AppendLine("    req.flush({});");
            sb.AppendLine("  });");
            sb.AppendLine("});");
        }
        else
        {
            // Class-based interceptor test
            var className = fileInfo.ClassName;

            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            sb.AppendLine("import { provideHttpClient } from '@angular/common/http';");
            sb.AppendLine("import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';");
            WriteVitestImport(sb, customDeps.Any());
            WriteFrameworkImports(sb, fileInfo);
            WriteCustomDepImports(sb, fileInfo, customDeps);
            sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{className}', () => {{");
            sb.AppendLine($"  let interceptor: {className};");
            sb.AppendLine("  let httpMock: HttpTestingController;");
            WriteMockDeclarations(sb, customDeps, "  ");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");
            WriteMockCreation(sb, customDeps, "    ");
            sb.AppendLine("    TestBed.configureTestingModule({");
            sb.AppendLine("      providers: [");
            sb.AppendLine("        provideHttpClient(),");
            sb.AppendLine("        provideHttpClientTesting(),");

            var needsRouter = fileInfo.Dependencies.Contains("Router") || fileInfo.Dependencies.Contains("ActivatedRoute");
            if (needsRouter)
            {
                sb.AppendLine("        provideRouter([]),");
            }
            if (fileInfo.Dependencies.Contains("API_BASE_URL"))
            {
                sb.AppendLine("        { provide: API_BASE_URL, useValue: 'http://localhost:3000' },");
            }
            foreach (var dep in customDeps)
            {
                var varName = GetMockVarName(dep);
                sb.AppendLine($"        {{ provide: {dep}, useValue: {varName} }},");
            }
            sb.AppendLine($"        {className},");

            sb.AppendLine("      ],");
            sb.AppendLine("    });");
            sb.AppendLine($"    interceptor = TestBed.inject({className});");
            sb.AppendLine("    httpMock = TestBed.inject(HttpTestingController);");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  afterEach(() => {");
            sb.AppendLine("    httpMock.verify();");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  it('should be created', () => {");
            sb.AppendLine("    expect(interceptor).toBeTruthy();");
            sb.AppendLine("  });");
            sb.AppendLine("});");
        }

        return sb.ToString();
    }

    private string GenerateResolverTest(TypeScriptFileInfo fileInfo)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}".Replace(".ts", "");
        var exportName = fileInfo.ExportName;
        var customDeps = GetCustomDependencies(fileInfo);

        if (fileInfo.IsFunctional)
        {
            // Functional resolver test
            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            sb.AppendLine("import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';");
            WriteVitestImport(sb, customDeps.Any());
            WriteFrameworkImports(sb, fileInfo);
            WriteCustomDepImports(sb, fileInfo, customDeps);
            sb.AppendLine($"import {{ {exportName} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{exportName}', () => {{");
            WriteMockDeclarations(sb, customDeps, "  ");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");
            WriteMockCreation(sb, customDeps, "    ");
            sb.AppendLine("    TestBed.configureTestingModule({");

            if (HasAnyDependencies(fileInfo))
            {
                WriteProviders(sb, fileInfo, customDeps, "      ");
            }

            sb.AppendLine("    });");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  it('should resolve data', () => {");
            sb.AppendLine("    const result = TestBed.runInInjectionContext(() => {");
            sb.AppendLine("      const mockRoute = {} as ActivatedRouteSnapshot;");
            sb.AppendLine("      const mockState = { url: '/test' } as RouterStateSnapshot;");
            sb.AppendLine($"      return {exportName}(mockRoute, mockState);");
            sb.AppendLine("    });");
            sb.AppendLine();
            sb.AppendLine("    expect(result).toBeDefined();");
            sb.AppendLine("  });");
            sb.AppendLine("});");
        }
        else
        {
            // Class-based resolver test
            var className = fileInfo.ClassName;

            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            WriteVitestImport(sb, customDeps.Any());
            WriteFrameworkImports(sb, fileInfo);
            WriteCustomDepImports(sb, fileInfo, customDeps);
            sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{className}', () => {{");
            sb.AppendLine($"  let resolver: {className};");
            WriteMockDeclarations(sb, customDeps, "  ");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");
            WriteMockCreation(sb, customDeps, "    ");
            sb.AppendLine("    TestBed.configureTestingModule({");

            if (HasAnyDependencies(fileInfo))
            {
                WriteProviders(sb, fileInfo, customDeps, "      ");
            }
            else
            {
                sb.AppendLine($"      providers: [{className}],");
            }

            sb.AppendLine("    });");
            sb.AppendLine($"    resolver = TestBed.inject({className});");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  it('should be created', () => {");
            sb.AppendLine("    expect(resolver).toBeTruthy();");
            sb.AppendLine("  });");
            sb.AppendLine("});");
        }

        return sb.ToString();
    }

    private string GenerateModelTest(TypeScriptFileInfo fileInfo)
    {
        // Skip if it's only interfaces
        if (fileInfo.IsInterfaceOrType)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}".Replace(".ts", "");
        var className = fileInfo.ClassName;

        sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
        sb.AppendLine();
        sb.AppendLine($"describe('{className}', () => {{");
        sb.AppendLine("  it('should be defined', () => {");
        sb.AppendLine($"    expect({className}).toBeDefined();");
        sb.AppendLine("  });");
        sb.AppendLine();
        sb.AppendLine("  it('should create instance', () => {");
        sb.AppendLine($"    const instance = new {className}();");
        sb.AppendLine("    expect(instance).toBeTruthy();");
        sb.AppendLine("  });");
        sb.AppendLine("});");

        return sb.ToString();
    }

    private string GenerateGenericTest(TypeScriptFileInfo fileInfo)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}".Replace(".ts", "");
        var className = fileInfo.ClassName;
        var customDeps = GetCustomDependencies(fileInfo);
        var hasDeps = HasAnyDependencies(fileInfo);

        if (hasDeps)
        {
            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            WriteVitestImport(sb, customDeps.Any());
            WriteFrameworkImports(sb, fileInfo);
            WriteCustomDepImports(sb, fileInfo, customDeps);
            sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{className}', () => {{");
            WriteMockDeclarations(sb, customDeps, "  ");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");
            WriteMockCreation(sb, customDeps, "    ");
            sb.AppendLine("    TestBed.configureTestingModule({");
            WriteProviders(sb, fileInfo, customDeps, "      ");
            sb.AppendLine("    });");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  it('should be defined', () => {");
            sb.AppendLine($"    expect({className}).toBeDefined();");
            sb.AppendLine("  });");
            sb.AppendLine("});");
        }
        else
        {
            sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{className}', () => {{");
            sb.AppendLine("  it('should be defined', () => {");
            sb.AppendLine($"    expect({className}).toBeDefined();");
            sb.AppendLine("  });");
            sb.AppendLine("});");
        }

        return sb.ToString();
    }
}
