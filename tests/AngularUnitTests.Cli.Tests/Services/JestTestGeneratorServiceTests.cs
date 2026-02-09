using AngularUnitTests.Cli.Configuration;
using AngularUnitTests.Cli.Models;
using AngularUnitTests.Cli.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AngularUnitTests.Cli.Tests.Services;

public class JestTestGeneratorServiceTests : IDisposable
{
    private readonly Mock<ILogger<JestTestGeneratorService>> _mockLogger;
    private readonly Mock<IOptions<AngularTestGeneratorOptions>> _mockOptions;
    private readonly JestTestGeneratorService _service;
    private readonly string _testDirectory;

    public JestTestGeneratorServiceTests()
    {
        _mockLogger = new Mock<ILogger<JestTestGeneratorService>>();
        _mockOptions = new Mock<IOptions<AngularTestGeneratorOptions>>();
        
        var options = new AngularTestGeneratorOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);
        
        _service = new JestTestGeneratorService(_mockLogger.Object, _mockOptions.Object);
        
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task GenerateTestFileAsync_ComponentFile_CreatesTestWithCorrectContent()
    {
        // Arrange
        var componentFile = Path.Combine(_testDirectory, "app.component.ts");
        File.WriteAllText(componentFile, "// component");
        
        var fileInfo = new TypeScriptFileInfo
        {
            FilePath = componentFile,
            FileName = "app.component",
            FileType = TypeScriptFileType.Component,
            ClassName = "AppComponent",
            ExistingTestFileCount = 0
        };

        // Act
        var testFilePath = await _service.GenerateTestFileAsync(fileInfo);

        // Assert
        Assert.True(File.Exists(testFilePath));
        var content = await File.ReadAllTextAsync(testFilePath);
        
        Assert.Contains("import { ComponentFixture, TestBed }", content);
        Assert.Contains("AppComponent", content);
        Assert.Contains("should create", content);
    }

    [Fact]
    public async Task GenerateTestFileAsync_ServiceFile_CreatesTestWithCorrectContent()
    {
        // Arrange
        var serviceFile = Path.Combine(_testDirectory, "user.service.ts");
        File.WriteAllText(serviceFile, "// service");
        
        var fileInfo = new TypeScriptFileInfo
        {
            FilePath = serviceFile,
            FileName = "user.service",
            FileType = TypeScriptFileType.Service,
            ClassName = "UserService",
            ExistingTestFileCount = 0
        };

        // Act
        var testFilePath = await _service.GenerateTestFileAsync(fileInfo);

        // Assert
        Assert.True(File.Exists(testFilePath));
        var content = await File.ReadAllTextAsync(testFilePath);
        
        Assert.Contains("import { TestBed }", content);
        Assert.Contains("UserService", content);
        Assert.Contains("should be created", content);
    }

    [Fact]
    public async Task GenerateTestFileAsync_WithExistingTest_CreatesFileWithDiscriminator()
    {
        // Arrange
        var pipeFile = Path.Combine(_testDirectory, "date.pipe.ts");
        var existingTestFile = Path.Combine(_testDirectory, "date.pipe.spec.ts");
        File.WriteAllText(pipeFile, "// pipe");
        File.WriteAllText(existingTestFile, "// existing test");
        
        var fileInfo = new TypeScriptFileInfo
        {
            FilePath = pipeFile,
            FileName = "date.pipe",
            FileType = TypeScriptFileType.Pipe,
            ClassName = "DatePipe",
            ExistingTestFileCount = 1
        };

        // Act
        var testFilePath = await _service.GenerateTestFileAsync(fileInfo);

        // Assert
        Assert.EndsWith(".spec.2.ts", testFilePath);
        Assert.True(File.Exists(testFilePath));
    }

    [Fact]
    public async Task GenerateTestFileAsync_PipeFile_CreatesTestWithCorrectContent()
    {
        // Arrange
        var pipeFile = Path.Combine(_testDirectory, "format.pipe.ts");
        File.WriteAllText(pipeFile, "// pipe");
        
        var fileInfo = new TypeScriptFileInfo
        {
            FilePath = pipeFile,
            FileName = "format.pipe",
            FileType = TypeScriptFileType.Pipe,
            ClassName = "FormatPipe",
            ExistingTestFileCount = 0
        };

        // Act
        var testFilePath = await _service.GenerateTestFileAsync(fileInfo);

        // Assert
        Assert.True(File.Exists(testFilePath));
        var content = await File.ReadAllTextAsync(testFilePath);
        
        Assert.Contains("FormatPipe", content);
        Assert.Contains("should create pipe", content);
        Assert.Contains("should transform value", content);
    }

    [Fact]
    public async Task GenerateTestFileAsync_GuardFile_CreatesTestWithCorrectContent()
    {
        // Arrange
        var guardFile = Path.Combine(_testDirectory, "auth.guard.ts");
        File.WriteAllText(guardFile, "// guard");
        
        var fileInfo = new TypeScriptFileInfo
        {
            FilePath = guardFile,
            FileName = "auth.guard",
            FileType = TypeScriptFileType.Guard,
            ClassName = "AuthGuard",
            ExistingTestFileCount = 0
        };

        // Act
        var testFilePath = await _service.GenerateTestFileAsync(fileInfo);

        // Assert
        Assert.True(File.Exists(testFilePath));
        var content = await File.ReadAllTextAsync(testFilePath);
        
        Assert.Contains("AuthGuard", content);
        Assert.Contains("should be created", content);
    }

    [Fact]
    public async Task GenerateTestFileAsync_ComponentWithDependencies_MocksAllDeps()
    {
        // Arrange
        var componentFile = Path.Combine(_testDirectory, "login.component.ts");
        File.WriteAllText(componentFile, "// component");

        var fileInfo = new TypeScriptFileInfo
        {
            FilePath = componentFile,
            FileName = "login.component",
            FileType = TypeScriptFileType.Component,
            ClassName = "LoginComponent",
            IsStandalone = true,
            ExistingTestFileCount = 0,
            Dependencies = new List<string> { "AuthService", "Router" }
        };

        // Set up discovered files so mock methods can be resolved
        var authServiceInfo = new TypeScriptFileInfo
        {
            FilePath = Path.Combine(_testDirectory, "auth.service.ts"),
            FileName = "auth.service",
            FileType = TypeScriptFileType.Service,
            ClassName = "AuthService",
            PublicMethods = new List<MethodInfo>
            {
                new() { Name = "login", ReturnType = "Observable<any>" },
                new() { Name = "logout", ReturnType = "void" }
            }
        };
        _service.SetDiscoveredFiles(new List<TypeScriptFileInfo> { fileInfo, authServiceInfo });

        // Act
        var testFilePath = await _service.GenerateTestFileAsync(fileInfo);

        // Assert
        var content = await File.ReadAllTextAsync(testFilePath!);
        Assert.Contains("mockAuthService", content);
        Assert.Contains("vi.fn()", content);
        Assert.Contains("provide: AuthService, useValue: mockAuthService", content);
        Assert.Contains("provideRouter([])", content);
    }

    [Fact]
    public async Task GenerateTestFileAsync_ServiceWithCustomDeps_MocksCustomDeps()
    {
        // Arrange
        var serviceFile = Path.Combine(_testDirectory, "order.service.ts");
        File.WriteAllText(serviceFile, "// service");

        var fileInfo = new TypeScriptFileInfo
        {
            FilePath = serviceFile,
            FileName = "order.service",
            FileType = TypeScriptFileType.Service,
            ClassName = "OrderService",
            ExistingTestFileCount = 0,
            Dependencies = new List<string> { "HttpClient", "AuthService" }
        };

        var authServiceInfo = new TypeScriptFileInfo
        {
            FilePath = Path.Combine(_testDirectory, "auth.service.ts"),
            FileName = "auth.service",
            FileType = TypeScriptFileType.Service,
            ClassName = "AuthService",
            PublicMethods = new List<MethodInfo>
            {
                new() { Name = "getToken", ReturnType = "string" }
            }
        };
        _service.SetDiscoveredFiles(new List<TypeScriptFileInfo> { fileInfo, authServiceInfo });

        // Act
        var testFilePath = await _service.GenerateTestFileAsync(fileInfo);

        // Assert
        var content = await File.ReadAllTextAsync(testFilePath!);
        Assert.Contains("mockAuthService", content);
        Assert.Contains("getToken: vi.fn()", content);
        Assert.Contains("provide: AuthService, useValue: mockAuthService", content);
        Assert.Contains("provideHttpClient()", content);
        Assert.Contains("provideHttpClientTesting()", content);
    }

    [Fact]
    public async Task GenerateTestFileAsync_FunctionalGuardWithDeps_MocksDeps()
    {
        // Arrange
        var guardFile = Path.Combine(_testDirectory, "auth.guard.ts");
        File.WriteAllText(guardFile, "// guard");

        var fileInfo = new TypeScriptFileInfo
        {
            FilePath = guardFile,
            FileName = "auth.guard",
            FileType = TypeScriptFileType.Guard,
            ClassName = "AuthGuard",
            ExportName = "authGuard",
            IsFunctional = true,
            ExistingTestFileCount = 0,
            Dependencies = new List<string> { "AuthService", "Router" }
        };

        var authServiceInfo = new TypeScriptFileInfo
        {
            FilePath = Path.Combine(_testDirectory, "auth.service.ts"),
            FileName = "auth.service",
            FileType = TypeScriptFileType.Service,
            ClassName = "AuthService",
            PublicMethods = new List<MethodInfo>
            {
                new() { Name = "hasValidToken", ReturnType = "boolean" }
            }
        };
        _service.SetDiscoveredFiles(new List<TypeScriptFileInfo> { fileInfo, authServiceInfo });

        // Act
        var testFilePath = await _service.GenerateTestFileAsync(fileInfo);

        // Assert
        var content = await File.ReadAllTextAsync(testFilePath!);
        Assert.Contains("mockAuthService", content);
        Assert.Contains("hasValidToken: vi.fn()", content);
        Assert.Contains("provide: AuthService, useValue: mockAuthService", content);
        Assert.Contains("provideRouter([])", content);
        Assert.Contains("runInInjectionContext", content);
    }

    [Fact]
    public async Task GenerateTestFileAsync_GenericWithDeps_MocksAllDeps()
    {
        // Arrange
        var file = Path.Combine(_testDirectory, "data.module.ts");
        File.WriteAllText(file, "// module");

        var fileInfo = new TypeScriptFileInfo
        {
            FilePath = file,
            FileName = "data.module",
            FileType = TypeScriptFileType.Module,
            ClassName = "DataModule",
            ExistingTestFileCount = 0,
            Dependencies = new List<string> { "HttpClient", "AuthService" }
        };

        var authServiceInfo = new TypeScriptFileInfo
        {
            FilePath = Path.Combine(_testDirectory, "auth.service.ts"),
            FileName = "auth.service",
            FileType = TypeScriptFileType.Service,
            ClassName = "AuthService",
            PublicMethods = new List<MethodInfo>
            {
                new() { Name = "isLoggedIn", ReturnType = "boolean" }
            }
        };
        _service.SetDiscoveredFiles(new List<TypeScriptFileInfo> { fileInfo, authServiceInfo });

        // Act
        var testFilePath = await _service.GenerateTestFileAsync(fileInfo);

        // Assert
        var content = await File.ReadAllTextAsync(testFilePath!);
        Assert.Contains("TestBed", content);
        Assert.Contains("mockAuthService", content);
        Assert.Contains("isLoggedIn: vi.fn()", content);
        Assert.Contains("provide: AuthService, useValue: mockAuthService", content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
