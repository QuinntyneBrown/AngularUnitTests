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
        Assert.Contains("canActivate", content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
