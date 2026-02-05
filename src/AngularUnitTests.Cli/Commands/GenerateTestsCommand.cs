using AngularUnitTests.Cli.Services;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace AngularUnitTests.Cli.Commands;

public class GenerateTestsCommand : Command
{
    public GenerateTestsCommand() : base("generate", "Generate Jest unit tests for Angular TypeScript files")
    {
        var pathOption = new Option<string>(
            name: "--path",
            description: "Path to the Angular application directory")
        {
            IsRequired = true
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

    public async Task<int> HandleAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting test generation for Angular application at: {Path}", path);

            // Validate path
            if (!Directory.Exists(path))
            {
                _logger.LogError("Path does not exist: {Path}", path);
                Console.Error.WriteLine($"Error: Path does not exist: {path}");
                return 1;
            }

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

            foreach (var fileInfo in fileList)
            {
                try
                {
                    var testFilePath = await _generatorService.GenerateTestFileAsync(fileInfo, cancellationToken);
                    Console.WriteLine($"✓ Generated: {Path.GetFileName(testFilePath)}");
                    successCount++;
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
            Console.WriteLine($"  Failed: {failureCount}");
            Console.WriteLine($"  Total: {fileList.Count}");

            _logger.LogInformation("Test generation completed. Success: {Success}, Failed: {Failed}", successCount, failureCount);

            return failureCount > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during test generation");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
