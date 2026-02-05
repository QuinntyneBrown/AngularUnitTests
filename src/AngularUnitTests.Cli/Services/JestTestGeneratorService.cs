using AngularUnitTests.Cli.Configuration;
using AngularUnitTests.Cli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace AngularUnitTests.Cli.Services;

public interface IJestTestGeneratorService
{
    Task<string?> GenerateTestFileAsync(TypeScriptFileInfo fileInfo, CancellationToken cancellationToken = default);
}

public class JestTestGeneratorService : IJestTestGeneratorService
{
    private readonly ILogger<JestTestGeneratorService> _logger;
    private readonly AngularTestGeneratorOptions _options;

    public JestTestGeneratorService(
        ILogger<JestTestGeneratorService> logger,
        IOptions<AngularTestGeneratorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
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

    private string GenerateComponentTest(TypeScriptFileInfo fileInfo)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}".Replace(".ts", "");
        var className = fileInfo.ClassName;

        sb.AppendLine("import { ComponentFixture, TestBed } from '@angular/core/testing';");

        // Add provider mocking if needed
        if (fileInfo.Dependencies.Any())
        {
            sb.AppendLine("import { provideHttpClient } from '@angular/common/http';");
            sb.AppendLine("import { provideHttpClientTesting } from '@angular/common/http/testing';");
            if (fileInfo.Dependencies.Contains("Router"))
            {
                sb.AppendLine("import { provideRouter } from '@angular/router';");
            }
        }

        sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
        sb.AppendLine();
        sb.AppendLine($"describe('{className}', () => {{");
        sb.AppendLine($"  let component: {className};");
        sb.AppendLine($"  let fixture: ComponentFixture<{className}>;");
        sb.AppendLine();
        sb.AppendLine("  beforeEach(async () => {");
        sb.AppendLine("    await TestBed.configureTestingModule({");

        if (fileInfo.IsStandalone)
        {
            sb.AppendLine($"      imports: [{className}],");
        }
        else
        {
            sb.AppendLine($"      declarations: [{className}],");
        }

        if (fileInfo.Dependencies.Any())
        {
            sb.AppendLine("      providers: [");
            sb.AppendLine("        provideHttpClient(),");
            sb.AppendLine("        provideHttpClientTesting(),");
            if (fileInfo.Dependencies.Contains("Router"))
            {
                sb.AppendLine("        provideRouter([]),");
            }
            sb.AppendLine("      ],");
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

        sb.AppendLine("import { TestBed } from '@angular/core/testing';");

        // Add HttpClient testing if needed
        var needsHttp = fileInfo.Dependencies.Contains("HttpClient");
        var needsRouter = fileInfo.Dependencies.Contains("Router");

        if (needsHttp)
        {
            sb.AppendLine("import { provideHttpClient } from '@angular/common/http';");
            sb.AppendLine("import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';");
        }
        if (needsRouter)
        {
            sb.AppendLine("import { provideRouter, Router } from '@angular/router';");
        }

        sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
        sb.AppendLine();
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

        sb.AppendLine();
        sb.AppendLine("  beforeEach(() => {");
        sb.AppendLine("    TestBed.configureTestingModule({");
        sb.AppendLine("      providers: [");
        sb.AppendLine($"        {className},");

        if (needsHttp)
        {
            sb.AppendLine("        provideHttpClient(),");
            sb.AppendLine("        provideHttpClientTesting(),");
        }
        if (needsRouter)
        {
            sb.AppendLine("        provideRouter([]),");
        }

        // Handle API_BASE_URL injection token
        if (fileInfo.Dependencies.Contains("API_BASE_URL"))
        {
            sb.AppendLine("        { provide: 'API_BASE_URL', useValue: 'http://localhost:3000' },");
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
        sb.AppendLine("});");

        return sb.ToString();
    }

    private string GenerateDirectiveTest(TypeScriptFileInfo fileInfo)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}".Replace(".ts", "");
        var className = fileInfo.ClassName;

        sb.AppendLine("import { TestBed, ComponentFixture } from '@angular/core/testing';");
        sb.AppendLine("import { Component } from '@angular/core';");
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
        sb.AppendLine();
        sb.AppendLine("  beforeEach(async () => {");
        sb.AppendLine("    await TestBed.configureTestingModule({");
        sb.AppendLine("      imports: [TestHostComponent],");
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

        sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
        sb.AppendLine();
        sb.AppendLine($"describe('{className}', () => {{");
        sb.AppendLine($"  let pipe: {className};");
        sb.AppendLine();
        sb.AppendLine("  beforeEach(() => {");
        sb.AppendLine($"    pipe = new {className}();");
        sb.AppendLine("  });");
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

        if (fileInfo.IsFunctional)
        {
            // Functional guard test using TestBed.runInInjectionContext
            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            sb.AppendLine("import { provideRouter, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';");

            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("import { AuthService } from '../../shared/services/auth.service';");
            }

            sb.AppendLine($"import {{ {exportName} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{exportName}', () => {{");

            // Create mock services
            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("  let mockAuthService: jasmine.SpyObj<AuthService>;");
            }
            sb.AppendLine("  let router: Router;");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");

            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("    mockAuthService = jasmine.createSpyObj('AuthService', ['hasValidToken', 'setRedirectUrl']);");
            }

            sb.AppendLine();
            sb.AppendLine("    TestBed.configureTestingModule({");
            sb.AppendLine("      providers: [");
            sb.AppendLine("        provideRouter([]),");

            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("        { provide: AuthService, useValue: mockAuthService },");
            }

            sb.AppendLine("      ],");
            sb.AppendLine("    });");
            sb.AppendLine();
            sb.AppendLine("    router = TestBed.inject(Router);");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  it('should allow activation when authenticated', () => {");

            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("    mockAuthService.hasValidToken.and.returnValue(true);");
            }

            sb.AppendLine();
            sb.AppendLine("    const result = TestBed.runInInjectionContext(() => {");
            sb.AppendLine("      const mockRoute = {} as ActivatedRouteSnapshot;");
            sb.AppendLine("      const mockState = { url: '/test' } as RouterStateSnapshot;");
            sb.AppendLine($"      return {exportName}(mockRoute, mockState);");
            sb.AppendLine("    });");
            sb.AppendLine();
            sb.AppendLine("    expect(result).toBe(true);");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  it('should deny activation when not authenticated', () => {");

            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("    mockAuthService.hasValidToken.and.returnValue(false);");
            }

            sb.AppendLine("    const navigateSpy = spyOn(router, 'navigate');");
            sb.AppendLine();
            sb.AppendLine("    const result = TestBed.runInInjectionContext(() => {");
            sb.AppendLine("      const mockRoute = {} as ActivatedRouteSnapshot;");
            sb.AppendLine("      const mockState = { url: '/protected' } as RouterStateSnapshot;");
            sb.AppendLine($"      return {exportName}(mockRoute, mockState);");
            sb.AppendLine("    });");
            sb.AppendLine();
            sb.AppendLine("    expect(result).toBe(false);");
            sb.AppendLine("    expect(navigateSpy).toHaveBeenCalledWith(['/login']);");
            sb.AppendLine("  });");
            sb.AppendLine("});");
        }
        else
        {
            // Class-based guard test
            var className = fileInfo.ClassName;
            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{className}', () => {{");
            sb.AppendLine($"  let guard: {className};");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");
            sb.AppendLine("    TestBed.configureTestingModule({");
            sb.AppendLine($"      providers: [{className}],");
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

        if (fileInfo.IsFunctional)
        {
            // Functional interceptor test
            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            sb.AppendLine("import { HttpClient, HttpHandlerFn, HttpRequest, provideHttpClient, withInterceptors } from '@angular/common/http';");
            sb.AppendLine("import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';");
            sb.AppendLine("import { provideRouter, Router } from '@angular/router';");

            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("import { AuthService } from '../../shared/services/auth.service';");
            }

            sb.AppendLine($"import {{ {exportName} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{exportName}', () => {{");
            sb.AppendLine("  let httpClient: HttpClient;");
            sb.AppendLine("  let httpMock: HttpTestingController;");

            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("  let mockAuthService: jasmine.SpyObj<AuthService>;");
            }

            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");

            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("    mockAuthService = jasmine.createSpyObj('AuthService', [");
                sb.AppendLine("      'getAccessToken',");
                sb.AppendLine("      'getRefreshToken',");
                sb.AppendLine("      'refreshToken',");
                sb.AppendLine("      'logout',");
                sb.AppendLine("      'setRedirectUrl',");
                sb.AppendLine("    ]);");
            }

            sb.AppendLine();
            sb.AppendLine("    TestBed.configureTestingModule({");
            sb.AppendLine("      providers: [");
            sb.AppendLine($"        provideHttpClient(withInterceptors([{exportName}])),");
            sb.AppendLine("        provideHttpClientTesting(),");
            sb.AppendLine("        provideRouter([]),");

            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("        { provide: AuthService, useValue: mockAuthService },");
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
            sb.AppendLine("  it('should add auth header to requests', () => {");

            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("    mockAuthService.getAccessToken.and.returnValue('test-token');");
            }

            sb.AppendLine();
            sb.AppendLine("    httpClient.get('/api/test').subscribe();");
            sb.AppendLine();
            sb.AppendLine("    const req = httpMock.expectOne('/api/test');");
            sb.AppendLine("    expect(req.request.headers.has('Authorization')).toBe(true);");
            sb.AppendLine("    req.flush({});");
            sb.AppendLine("  });");
            sb.AppendLine();
            sb.AppendLine("  it('should skip auth header for login endpoint', () => {");

            if (fileInfo.Dependencies.Contains("AuthService"))
            {
                sb.AppendLine("    mockAuthService.getAccessToken.and.returnValue('test-token');");
            }

            sb.AppendLine();
            sb.AppendLine("    httpClient.post('/api/auth/login', {}).subscribe();");
            sb.AppendLine();
            sb.AppendLine("    const req = httpMock.expectOne('/api/auth/login');");
            sb.AppendLine("    expect(req.request.headers.has('Authorization')).toBe(false);");
            sb.AppendLine("    req.flush({});");
            sb.AppendLine("  });");
            sb.AppendLine("});");
        }
        else
        {
            // Class-based interceptor test
            var className = fileInfo.ClassName;
            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            sb.AppendLine("import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';");
            sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{className}', () => {{");
            sb.AppendLine($"  let interceptor: {className};");
            sb.AppendLine("  let httpMock: HttpTestingController;");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");
            sb.AppendLine("    TestBed.configureTestingModule({");
            sb.AppendLine("      imports: [HttpClientTestingModule],");
            sb.AppendLine($"      providers: [{className}],");
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

        if (fileInfo.IsFunctional)
        {
            // Functional resolver test
            sb.AppendLine("import { TestBed } from '@angular/core/testing';");
            sb.AppendLine("import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';");
            sb.AppendLine($"import {{ {exportName} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{exportName}', () => {{");
            sb.AppendLine("  beforeEach(() => {");
            sb.AppendLine("    TestBed.configureTestingModule({});");
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
            sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
            sb.AppendLine();
            sb.AppendLine($"describe('{className}', () => {{");
            sb.AppendLine($"  let resolver: {className};");
            sb.AppendLine();
            sb.AppendLine("  beforeEach(() => {");
            sb.AppendLine("    TestBed.configureTestingModule({");
            sb.AppendLine($"      providers: [{className}],");
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

        sb.AppendLine($"import {{ {className} }} from '{relativePath}';");
        sb.AppendLine();
        sb.AppendLine($"describe('{className}', () => {{");
        sb.AppendLine("  it('should be defined', () => {");
        sb.AppendLine($"    expect({className}).toBeDefined();");
        sb.AppendLine("  });");
        sb.AppendLine("});");

        return sb.ToString();
    }
}
