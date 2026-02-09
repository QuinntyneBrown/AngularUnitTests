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

    [Fact]
    public async Task DiscoverTypeScriptFilesAsync_DetectsPrivateReadonlyConstructorDependency()
    {
        // Arrange
        var serviceFile = Path.Combine(_testDirectory, "data.service.ts");
        File.WriteAllText(serviceFile, @"
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class DataService {
    constructor(private readonly http: HttpClient) {}
}");

        // Act
        var result = await _service.DiscoverTypeScriptFilesAsync(_testDirectory);

        // Assert
        var fileInfo = result.First();
        Assert.Contains("HttpClient", fileInfo.Dependencies);
    }

    [Fact]
    public async Task DiscoverTypeScriptFilesAsync_DetectsInjectWithOptions()
    {
        // Arrange
        var serviceFile = Path.Combine(_testDirectory, "config.service.ts");
        File.WriteAllText(serviceFile, @"
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ConfigService } from './config.service';

@Injectable({ providedIn: 'root' })
export class AppService {
    private readonly http = inject(HttpClient);
    private readonly config = inject(ConfigService, { optional: true });
}");

        // Act
        var result = await _service.DiscoverTypeScriptFilesAsync(_testDirectory);

        // Assert
        var fileInfo = result.First();
        Assert.Contains("HttpClient", fileInfo.Dependencies);
        Assert.Contains("ConfigService", fileInfo.Dependencies);
    }

    [Fact]
    public async Task DiscoverTypeScriptFilesAsync_DetectsInjectWithGenericType()
    {
        // Arrange
        var guardFile = Path.Combine(_testDirectory, "role.guard.ts");
        File.WriteAllText(guardFile, @"
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
    const authService = inject<AuthService>(AuthService);
    const router = inject(Router);
    return true;
};");

        // Act
        var result = await _service.DiscoverTypeScriptFilesAsync(_testDirectory);

        // Assert
        var fileInfo = result.First();
        Assert.Contains("AuthService", fileInfo.Dependencies);
        Assert.Contains("Router", fileInfo.Dependencies);
    }

    [Fact]
    public async Task DiscoverTypeScriptFilesAsync_DetectsMultipleModifierCombinations()
    {
        // Arrange
        var componentFile = Path.Combine(_testDirectory, "dashboard.component.ts");
        File.WriteAllText(componentFile, @"
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { DataService } from '../services/data.service';

@Component({ selector: 'app-dashboard', standalone: true, template: '' })
export class DashboardComponent {
    constructor(
        private readonly authService: AuthService,
        protected readonly dataService: DataService,
        public router: Router
    ) {}
}");

        // Act
        var result = await _service.DiscoverTypeScriptFilesAsync(_testDirectory);

        // Assert
        var fileInfo = result.First();
        Assert.Contains("AuthService", fileInfo.Dependencies);
        Assert.Contains("DataService", fileInfo.Dependencies);
        Assert.Contains("Router", fileInfo.Dependencies);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
