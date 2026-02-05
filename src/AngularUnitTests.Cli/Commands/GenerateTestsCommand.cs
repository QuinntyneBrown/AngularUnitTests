using AngularUnitTests.Cli.Services;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace AngularUnitTests.Cli.Commands;

public class GenerateTestsCommand : Command
{
    public GenerateTestsCommand() : base("generate", "Generate Jest unit tests for Angular TypeScript files")
    {
        var pathOption = new Option<string?>(
            name: "--path",
            description: "Path to the Angular application directory. If not provided, uses the current directory if it's an Angular workspace.")
        {
            IsRequired = false
        };
        pathOption.AddAlias("-p");

        AddOption(pathOption);
    }
}

public class GenerateTestsCommandHandler
{
    private readonly ITypeScriptFileDiscoveryService _discoveryService;
    private readonly IJestTestGeneratorService _generatorService;
    private readonly ILogger<GenerateTestsCommandHandler> _logger;

    public GenerateTestsCommandHandler(
        ITypeScriptFileDiscoveryService discoveryService,
        IJestTestGeneratorService generatorService,
        ILogger<GenerateTestsCommandHandler> logger)
    {
        _discoveryService = discoveryService;
        _generatorService = generatorService;
        _logger = logger;
    }

    public async Task<int> HandleAsync(string? path, CancellationToken cancellationToken)
    {
        try
        {
            // Resolve the path if not provided
            var resolvedPath = ResolvePath(path);
            if (resolvedPath == null)
            {
                return 1;
            }

            _logger.LogInformation("Starting test generation for Angular application at: {Path}", resolvedPath);

            // Validate path
            if (!Directory.Exists(resolvedPath))
            {
                _logger.LogError("Path does not exist: {Path}", resolvedPath);
                Console.Error.WriteLine($"Error: Path does not exist: {resolvedPath}");
                return 1;
            }

            path = resolvedPath;

            // Discover TypeScript files
            var typeScriptFiles = await _discoveryService.DiscoverTypeScriptFilesAsync(path, cancellationToken);
            var fileList = typeScriptFiles.ToList();

            if (!fileList.Any())
            {
                _logger.LogWarning("No TypeScript files found in: {Path}", path);
                Console.WriteLine($"No TypeScript files found in: {path}");
                return 0;
            }

            Console.WriteLine($"Found {fileList.Count} TypeScript file(s) to process.");
            Console.WriteLine();

            // Generate tests for each file
            var successCount = 0;
            var failureCount = 0;
            var skippedCount = 0;

            foreach (var fileInfo in fileList)
            {
                try
                {
                    var testFilePath = await _generatorService.GenerateTestFileAsync(fileInfo, cancellationToken);
                    if (testFilePath != null)
                    {
                        Console.WriteLine($"✓ Generated: {Path.GetFileName(testFilePath)}");
                        successCount++;
                    }
                    else
                    {
                        Console.WriteLine($"- Skipped: {fileInfo.FileName} (interface/type only)");
                        skippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate test for: {FilePath}", fileInfo.FilePath);
                    Console.Error.WriteLine($"✗ Failed: {fileInfo.FileName} - {ex.Message}");
                    failureCount++;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Test generation complete:");
            Console.WriteLine($"  Success: {successCount}");
            Console.WriteLine($"  Skipped: {skippedCount}");
            Console.WriteLine($"  Failed: {failureCount}");
            Console.WriteLine($"  Total: {fileList.Count}");

            _logger.LogInformation("Test generation completed. Success: {Success}, Skipped: {Skipped}, Failed: {Failed}", successCount, skippedCount, failureCount);

            return failureCount > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during test generation");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Resolves the path to the Angular workspace.
    /// If path is provided, returns it.
    /// If not provided, searches for angular.json in current directory and parent directories.
    /// </summary>
    private string? ResolvePath(string? path)
    {
        // If path is explicitly provided, use it
        if (!string.IsNullOrWhiteSpace(path))
        {
            return Path.GetFullPath(path);
        }

        // Try to find angular.json starting from current directory
        var currentDir = Directory.GetCurrentDirectory();
        var workspaceRoot = FindAngularWorkspaceRoot(currentDir);

        if (workspaceRoot != null)
        {
            Console.WriteLine($"Detected Angular workspace at: {workspaceRoot}");
            return workspaceRoot;
        }

        // Not in an Angular workspace
        Console.Error.WriteLine("Error: Not in an Angular workspace. No angular.json found.");
        Console.Error.WriteLine("Either run this command from within an Angular workspace, or specify the path with --path option.");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage: ngt generate --path <path-to-angular-app>");
        return null;
    }

    /// <summary>
    /// Searches for angular.json starting from the given directory and walking up the directory tree.
    /// </summary>
    private string? FindAngularWorkspaceRoot(string startDirectory)
    {
        var currentDir = startDirectory;

        while (!string.IsNullOrEmpty(currentDir))
        {
            var angularJsonPath = Path.Combine(currentDir, "angular.json");
            if (File.Exists(angularJsonPath))
            {
                return currentDir;
            }

            // Move up to parent directory
            var parentDir = Directory.GetParent(currentDir)?.FullName;

            // Stop if we've reached the root
            if (parentDir == currentDir)
            {
                break;
            }

            currentDir = parentDir;
        }

        return null;
    }
}
