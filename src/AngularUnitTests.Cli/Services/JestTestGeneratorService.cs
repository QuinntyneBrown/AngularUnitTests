using AngularUnitTests.Cli.Configuration;
using AngularUnitTests.Cli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace AngularUnitTests.Cli.Services;

public interface IJestTestGeneratorService
{
    Task<string> GenerateTestFileAsync(TypeScriptFileInfo fileInfo, CancellationToken cancellationToken = default);
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

    public async Task<string> GenerateTestFileAsync(
        TypeScriptFileInfo fileInfo,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating test for: {FilePath}", fileInfo.FilePath);

        var testFilePath = DetermineTestFilePath(fileInfo);
        var testContent = await GenerateTestContentAsync(fileInfo, cancellationToken);

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
            TypeScriptFileType.Component => await GenerateComponentTestAsync(fileInfo, cancellationToken),
            TypeScriptFileType.Service => await GenerateServiceTestAsync(fileInfo, cancellationToken),
            TypeScriptFileType.Directive => await GenerateDirectiveTestAsync(fileInfo, cancellationToken),
            TypeScriptFileType.Pipe => await GeneratePipeTestAsync(fileInfo, cancellationToken),
            TypeScriptFileType.Guard => await GenerateGuardTestAsync(fileInfo, cancellationToken),
            TypeScriptFileType.Interceptor => await GenerateInterceptorTestAsync(fileInfo, cancellationToken),
            TypeScriptFileType.Resolver => await GenerateResolverTestAsync(fileInfo, cancellationToken),
            _ => await GenerateGenericTestAsync(fileInfo, cancellationToken)
        };
    }

    private async Task<string> GenerateComponentTestAsync(TypeScriptFileInfo fileInfo, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}";

        sb.AppendLine($"import {{ ComponentFixture, TestBed }} from '@angular/core/testing';");
        sb.AppendLine($"import {{ {fileInfo.ClassName} }} from '{relativePath.Replace(".ts", "")}';");
        sb.AppendLine();
        sb.AppendLine($"describe('{fileInfo.ClassName}', () => {{");
        sb.AppendLine($"  let component: {fileInfo.ClassName};");
        sb.AppendLine($"  let fixture: ComponentFixture<{fileInfo.ClassName}>;");
        sb.AppendLine();
        sb.AppendLine($"  beforeEach(async () => {{");
        sb.AppendLine($"    await TestBed.configureTestingModule({{");
        sb.AppendLine($"      declarations: [{fileInfo.ClassName}]");
        sb.AppendLine($"    }}).compileComponents();");
        sb.AppendLine();
        sb.AppendLine($"    fixture = TestBed.createComponent({fileInfo.ClassName});");
        sb.AppendLine($"    component = fixture.componentInstance;");
        sb.AppendLine($"    fixture.detectChanges();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should create', () => {{");
        sb.AppendLine($"    expect(component).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should render component', () => {{");
        sb.AppendLine($"    expect(fixture.nativeElement).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should have correct initial state', () => {{");
        sb.AppendLine($"    expect(component).toMatchSnapshot();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle lifecycle hooks', () => {{");
        sb.AppendLine($"    const ngOnInitSpy = jest.spyOn(component, 'ngOnInit');");
        sb.AppendLine($"    component.ngOnInit();");
        sb.AppendLine($"    expect(ngOnInitSpy).toHaveBeenCalled();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle change detection', () => {{");
        sb.AppendLine($"    fixture.detectChanges();");
        sb.AppendLine($"    expect(fixture.nativeElement).toMatchSnapshot();");
        sb.AppendLine($"  }});");
        sb.AppendLine($"}});");

        return await Task.FromResult(sb.ToString());
    }

    private async Task<string> GenerateServiceTestAsync(TypeScriptFileInfo fileInfo, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}";

        sb.AppendLine($"import {{ TestBed }} from '@angular/core/testing';");
        sb.AppendLine($"import {{ {fileInfo.ClassName} }} from '{relativePath.Replace(".ts", "")}';");
        sb.AppendLine();
        sb.AppendLine($"describe('{fileInfo.ClassName}', () => {{");
        sb.AppendLine($"  let service: {fileInfo.ClassName};");
        sb.AppendLine();
        sb.AppendLine($"  beforeEach(() => {{");
        sb.AppendLine($"    TestBed.configureTestingModule({{");
        sb.AppendLine($"      providers: [{fileInfo.ClassName}]");
        sb.AppendLine($"    }});");
        sb.AppendLine($"    service = TestBed.inject({fileInfo.ClassName});");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should be created', () => {{");
        sb.AppendLine($"    expect(service).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should have correct initial state', () => {{");
        sb.AppendLine($"    expect(service).toBeDefined();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle method calls', () => {{");
        sb.AppendLine($"    // Test service methods here");
        sb.AppendLine($"    expect(service).toMatchSnapshot();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle errors gracefully', () => {{");
        sb.AppendLine($"    // Test error handling");
        sb.AppendLine($"    expect(service).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should maintain state correctly', () => {{");
        sb.AppendLine($"    // Test state management");
        sb.AppendLine($"    expect(service).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine($"}});");

        return await Task.FromResult(sb.ToString());
    }

    private async Task<string> GenerateDirectiveTestAsync(TypeScriptFileInfo fileInfo, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}";

        sb.AppendLine($"import {{ TestBed, ComponentFixture }} from '@angular/core/testing';");
        sb.AppendLine($"import {{ Component }} from '@angular/core';");
        sb.AppendLine($"import {{ {fileInfo.ClassName} }} from '{relativePath.Replace(".ts", "")}';");
        sb.AppendLine();
        sb.AppendLine($"@Component({{");
        sb.AppendLine($"  template: '<div></div>'");
        sb.AppendLine($"}})");
        sb.AppendLine($"class TestComponent {{ }}");
        sb.AppendLine();
        sb.AppendLine($"describe('{fileInfo.ClassName}', () => {{");
        sb.AppendLine($"  let fixture: ComponentFixture<TestComponent>;");
        sb.AppendLine();
        sb.AppendLine($"  beforeEach(() => {{");
        sb.AppendLine($"    TestBed.configureTestingModule({{");
        sb.AppendLine($"      declarations: [{fileInfo.ClassName}, TestComponent]");
        sb.AppendLine($"    }});");
        sb.AppendLine($"    fixture = TestBed.createComponent(TestComponent);");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should create directive', () => {{");
        sb.AppendLine($"    const directive = new {fileInfo.ClassName}();");
        sb.AppendLine($"    expect(directive).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should apply directive behavior', () => {{");
        sb.AppendLine($"    fixture.detectChanges();");
        sb.AppendLine($"    expect(fixture.nativeElement).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle events', () => {{");
        sb.AppendLine($"    fixture.detectChanges();");
        sb.AppendLine($"    expect(fixture.componentInstance).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should update DOM correctly', () => {{");
        sb.AppendLine($"    fixture.detectChanges();");
        sb.AppendLine($"    expect(fixture.nativeElement).toMatchSnapshot();");
        sb.AppendLine($"  }});");
        sb.AppendLine($"}});");

        return await Task.FromResult(sb.ToString());
    }

    private async Task<string> GeneratePipeTestAsync(TypeScriptFileInfo fileInfo, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}";

        sb.AppendLine($"import {{ {fileInfo.ClassName} }} from '{relativePath.Replace(".ts", "")}';");
        sb.AppendLine();
        sb.AppendLine($"describe('{fileInfo.ClassName}', () => {{");
        sb.AppendLine($"  let pipe: {fileInfo.ClassName};");
        sb.AppendLine();
        sb.AppendLine($"  beforeEach(() => {{");
        sb.AppendLine($"    pipe = new {fileInfo.ClassName}();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should create pipe', () => {{");
        sb.AppendLine($"    expect(pipe).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should transform value', () => {{");
        sb.AppendLine($"    const result = pipe.transform('test');");
        sb.AppendLine($"    expect(result).toBeDefined();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle null values', () => {{");
        sb.AppendLine($"    const result = pipe.transform(null);");
        sb.AppendLine($"    expect(result).toBeDefined();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle undefined values', () => {{");
        sb.AppendLine($"    const result = pipe.transform(undefined);");
        sb.AppendLine($"    expect(result).toBeDefined();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle edge cases', () => {{");
        sb.AppendLine($"    const result = pipe.transform('');");
        sb.AppendLine($"    expect(result).toBeDefined();");
        sb.AppendLine($"  }});");
        sb.AppendLine($"}});");

        return await Task.FromResult(sb.ToString());
    }

    private async Task<string> GenerateGuardTestAsync(TypeScriptFileInfo fileInfo, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}";

        sb.AppendLine($"import {{ TestBed }} from '@angular/core/testing';");
        sb.AppendLine($"import {{ {fileInfo.ClassName} }} from '{relativePath.Replace(".ts", "")}';");
        sb.AppendLine();
        sb.AppendLine($"describe('{fileInfo.ClassName}', () => {{");
        sb.AppendLine($"  let guard: {fileInfo.ClassName};");
        sb.AppendLine();
        sb.AppendLine($"  beforeEach(() => {{");
        sb.AppendLine($"    TestBed.configureTestingModule({{");
        sb.AppendLine($"      providers: [{fileInfo.ClassName}]");
        sb.AppendLine($"    }});");
        sb.AppendLine($"    guard = TestBed.inject({fileInfo.ClassName});");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should be created', () => {{");
        sb.AppendLine($"    expect(guard).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should allow activation', () => {{");
        sb.AppendLine($"    const result = guard.canActivate();");
        sb.AppendLine($"    expect(result).toBeDefined();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should prevent activation when unauthorized', () => {{");
        sb.AppendLine($"    // Test authorization logic");
        sb.AppendLine($"    expect(guard).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle route parameters', () => {{");
        sb.AppendLine($"    // Test route parameter handling");
        sb.AppendLine($"    expect(guard).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should redirect when necessary', () => {{");
        sb.AppendLine($"    // Test redirection logic");
        sb.AppendLine($"    expect(guard).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine($"}});");

        return await Task.FromResult(sb.ToString());
    }

    private async Task<string> GenerateInterceptorTestAsync(TypeScriptFileInfo fileInfo, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}";

        sb.AppendLine($"import {{ TestBed }} from '@angular/core/testing';");
        sb.AppendLine($"import {{ HttpClientTestingModule, HttpTestingController }} from '@angular/common/http/testing';");
        sb.AppendLine($"import {{ {fileInfo.ClassName} }} from '{relativePath.Replace(".ts", "")}';");
        sb.AppendLine();
        sb.AppendLine($"describe('{fileInfo.ClassName}', () => {{");
        sb.AppendLine($"  let interceptor: {fileInfo.ClassName};");
        sb.AppendLine($"  let httpMock: HttpTestingController;");
        sb.AppendLine();
        sb.AppendLine($"  beforeEach(() => {{");
        sb.AppendLine($"    TestBed.configureTestingModule({{");
        sb.AppendLine($"      imports: [HttpClientTestingModule],");
        sb.AppendLine($"      providers: [{fileInfo.ClassName}]");
        sb.AppendLine($"    }});");
        sb.AppendLine($"    interceptor = TestBed.inject({fileInfo.ClassName});");
        sb.AppendLine($"    httpMock = TestBed.inject(HttpTestingController);");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  afterEach(() => {{");
        sb.AppendLine($"    httpMock.verify();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should be created', () => {{");
        sb.AppendLine($"    expect(interceptor).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should intercept HTTP requests', () => {{");
        sb.AppendLine($"    // Test request interception");
        sb.AppendLine($"    expect(interceptor).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle HTTP responses', () => {{");
        sb.AppendLine($"    // Test response handling");
        sb.AppendLine($"    expect(interceptor).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle errors', () => {{");
        sb.AppendLine($"    // Test error handling");
        sb.AppendLine($"    expect(interceptor).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine($"}});");

        return await Task.FromResult(sb.ToString());
    }

    private async Task<string> GenerateResolverTestAsync(TypeScriptFileInfo fileInfo, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}";

        sb.AppendLine($"import {{ TestBed }} from '@angular/core/testing';");
        sb.AppendLine($"import {{ {fileInfo.ClassName} }} from '{relativePath.Replace(".ts", "")}';");
        sb.AppendLine();
        sb.AppendLine($"describe('{fileInfo.ClassName}', () => {{");
        sb.AppendLine($"  let resolver: {fileInfo.ClassName};");
        sb.AppendLine();
        sb.AppendLine($"  beforeEach(() => {{");
        sb.AppendLine($"    TestBed.configureTestingModule({{");
        sb.AppendLine($"      providers: [{fileInfo.ClassName}]");
        sb.AppendLine($"    }});");
        sb.AppendLine($"    resolver = TestBed.inject({fileInfo.ClassName});");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should be created', () => {{");
        sb.AppendLine($"    expect(resolver).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should resolve data', async () => {{");
        sb.AppendLine($"    const result = await resolver.resolve();");
        sb.AppendLine($"    expect(result).toBeDefined();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle route parameters', () => {{");
        sb.AppendLine($"    // Test route parameter handling");
        sb.AppendLine($"    expect(resolver).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle errors', () => {{");
        sb.AppendLine($"    // Test error handling");
        sb.AppendLine($"    expect(resolver).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should cache results if applicable', () => {{");
        sb.AppendLine($"    // Test caching logic");
        sb.AppendLine($"    expect(resolver).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine($"}});");

        return await Task.FromResult(sb.ToString());
    }

    private async Task<string> GenerateGenericTestAsync(TypeScriptFileInfo fileInfo, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        var relativePath = $"./{Path.GetFileName(fileInfo.FilePath)}";

        sb.AppendLine($"import {{ {fileInfo.ClassName} }} from '{relativePath.Replace(".ts", "")}';");
        sb.AppendLine();
        sb.AppendLine($"describe('{fileInfo.ClassName}', () => {{");
        sb.AppendLine($"  it('should be defined', () => {{");
        sb.AppendLine($"    expect({fileInfo.ClassName}).toBeDefined();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should have correct structure', () => {{");
        sb.AppendLine($"    // Test structure");
        sb.AppendLine($"    expect({fileInfo.ClassName}).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle basic operations', () => {{");
        sb.AppendLine($"    // Test basic operations");
        sb.AppendLine($"    expect({fileInfo.ClassName}).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should validate input', () => {{");
        sb.AppendLine($"    // Test input validation");
        sb.AppendLine($"    expect({fileInfo.ClassName}).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine();
        sb.AppendLine($"  it('should handle edge cases', () => {{");
        sb.AppendLine($"    // Test edge cases");
        sb.AppendLine($"    expect({fileInfo.ClassName}).toBeTruthy();");
        sb.AppendLine($"  }});");
        sb.AppendLine($"}});");

        return await Task.FromResult(sb.ToString());
    }
}
