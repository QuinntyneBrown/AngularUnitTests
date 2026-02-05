using AngularUnitTests.Cli.Configuration;
using AngularUnitTests.Cli.Models;
using AngularUnitTests.Cli.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AngularUnitTests.Cli.Tests.Services;

public class TypeScriptFileDiscoveryServiceTests : IDisposable
{
    private readonly Mock<ILogger<TypeScriptFileDiscoveryService>> _mockLogger;
    private readonly Mock<IOptions<AngularTestGeneratorOptions>> _mockOptions;
    private readonly TypeScriptFileDiscoveryService _service;
    private readonly string _testDirectory;

    public TypeScriptFileDiscoveryServiceTests()
    {
        _mockLogger = new Mock<ILogger<TypeScriptFileDiscoveryService>>();
        _mockOptions = new Mock<IOptions<AngularTestGeneratorOptions>>();
        
        var options = new AngularTestGeneratorOptions
        {
            ExcludedDirectories = new[] { "node_modules", "dist", ".angular" }
        };
        
        _mockOptions.Setup(x => x.Value).Returns(options);
        _service = new TypeScriptFileDiscoveryService(_mockLogger.Object, _mockOptions.Object);
        
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task DiscoverTypeScriptFilesAsync_WithValidPath_ReturnsFiles()
    {
        // Arrange
        var componentFile = Path.Combine(_testDirectory, "test.component.ts");
        File.WriteAllText(componentFile, "// test component");

        // Act
        var result = await _service.DiscoverTypeScriptFilesAsync(_testDirectory);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        
        var fileInfo = result.First();
        Assert.Equal(componentFile, fileInfo.FilePath);
        Assert.Equal(TypeScriptFileType.Component, fileInfo.FileType);
    }

    [Fact]
    public async Task DiscoverTypeScriptFilesAsync_WithInvalidPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "/invalid/path/that/does/not/exist";

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _service.DiscoverTypeScriptFilesAsync(invalidPath));
    }

    [Fact]
    public async Task DiscoverTypeScriptFilesAsync_ExcludesTestFiles()
    {
        // Arrange
        var componentFile = Path.Combine(_testDirectory, "test.component.ts");
        var testFile = Path.Combine(_testDirectory, "test.component.spec.ts");
        File.WriteAllText(componentFile, "// test component");
        File.WriteAllText(testFile, "// test");

        // Act
        var result = await _service.DiscoverTypeScriptFilesAsync(_testDirectory);

        // Assert
        Assert.Single(result);
        Assert.DoesNotContain(result, f => f.FilePath.Contains(".spec."));
    }

    [Fact]
    public async Task DiscoverTypeScriptFilesAsync_DetectsServiceFile()
    {
        // Arrange
        var serviceFile = Path.Combine(_testDirectory, "user.service.ts");
        File.WriteAllText(serviceFile, "// user service");

        // Act
        var result = await _service.DiscoverTypeScriptFilesAsync(_testDirectory);

        // Assert
        var fileInfo = result.First();
        Assert.Equal(TypeScriptFileType.Service, fileInfo.FileType);
    }

    [Fact]
    public async Task DiscoverTypeScriptFilesAsync_DetectsPipeFile()
    {
        // Arrange
        var pipeFile = Path.Combine(_testDirectory, "date-format.pipe.ts");
        File.WriteAllText(pipeFile, "// pipe");

        // Act
        var result = await _service.DiscoverTypeScriptFilesAsync(_testDirectory);

        // Assert
        var fileInfo = result.First();
        Assert.Equal(TypeScriptFileType.Pipe, fileInfo.FileType);
    }

    [Fact]
    public async Task DiscoverTypeScriptFilesAsync_DetectsGuardFile()
    {
        // Arrange
        var guardFile = Path.Combine(_testDirectory, "auth.guard.ts");
        File.WriteAllText(guardFile, "// guard");

        // Act
        var result = await _service.DiscoverTypeScriptFilesAsync(_testDirectory);

        // Assert
        var fileInfo = result.First();
        Assert.Equal(TypeScriptFileType.Guard, fileInfo.FileType);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
