using AngularUnitTests.Cli.Configuration;
using AngularUnitTests.Cli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AngularUnitTests.Cli.Services;

public interface ITypeScriptFileDiscoveryService
{
    Task<IEnumerable<TypeScriptFileInfo>> DiscoverTypeScriptFilesAsync(string angularAppPath, CancellationToken cancellationToken = default);
}

public class TypeScriptFileDiscoveryService : ITypeScriptFileDiscoveryService
{
    private readonly ILogger<TypeScriptFileDiscoveryService> _logger;
    private readonly AngularTestGeneratorOptions _options;

    public TypeScriptFileDiscoveryService(
        ILogger<TypeScriptFileDiscoveryService> logger,
        IOptions<AngularTestGeneratorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IEnumerable<TypeScriptFileInfo>> DiscoverTypeScriptFilesAsync(
        string angularAppPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering TypeScript files in: {Path}", angularAppPath);

        if (!Directory.Exists(angularAppPath))
        {
            throw new DirectoryNotFoundException($"Angular application path not found: {angularAppPath}");
        }

        var typeScriptFiles = new List<TypeScriptFileInfo>();

        await Task.Run(() =>
        {
            var files = Directory.GetFiles(angularAppPath, "*.ts", SearchOption.AllDirectories)
                .Where(f => !IsExcludedPath(f) && !IsTestFile(f));

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileInfo = AnalyzeTypeScriptFile(file);
                if (fileInfo != null)
                {
                    typeScriptFiles.Add(fileInfo);
                }
            }
        }, cancellationToken);

        _logger.LogInformation("Discovered {Count} TypeScript files", typeScriptFiles.Count);
        return typeScriptFiles;
    }

    private bool IsExcludedPath(string filePath)
    {
        return _options.ExcludedDirectories.Any(dir =>
            filePath.Contains($"{Path.DirectorySeparatorChar}{dir}{Path.DirectorySeparatorChar}") ||
            filePath.Contains($"{Path.DirectorySeparatorChar}{dir}"));
    }

    private bool IsTestFile(string filePath)
    {
        return filePath.Contains(".spec.") || filePath.Contains(".test.");
    }

    private TypeScriptFileInfo? AnalyzeTypeScriptFile(string filePath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fileType = DetermineFileType(fileName, filePath);

            if (fileType == TypeScriptFileType.Unknown)
            {
                _logger.LogDebug("Skipping unknown file type: {FilePath}", filePath);
                return null;
            }

            var className = ExtractClassName(fileName, fileType);
            var existingTestCount = CountExistingTestFiles(filePath);

            return new TypeScriptFileInfo
            {
                FilePath = filePath,
                FileName = fileName,
                FileType = fileType,
                ClassName = className,
                ExistingTestFileCount = existingTestCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing file: {FilePath}", filePath);
            return null;
        }
    }

    private TypeScriptFileType DetermineFileType(string fileName, string filePath)
    {
        if (fileName.EndsWith(".component", StringComparison.OrdinalIgnoreCase))
            return TypeScriptFileType.Component;
        if (fileName.EndsWith(".service", StringComparison.OrdinalIgnoreCase))
            return TypeScriptFileType.Service;
        if (fileName.EndsWith(".directive", StringComparison.OrdinalIgnoreCase))
            return TypeScriptFileType.Directive;
        if (fileName.EndsWith(".pipe", StringComparison.OrdinalIgnoreCase))
            return TypeScriptFileType.Pipe;
        if (fileName.EndsWith(".guard", StringComparison.OrdinalIgnoreCase))
            return TypeScriptFileType.Guard;
        if (fileName.EndsWith(".interceptor", StringComparison.OrdinalIgnoreCase))
            return TypeScriptFileType.Interceptor;
        if (fileName.EndsWith(".resolver", StringComparison.OrdinalIgnoreCase))
            return TypeScriptFileType.Resolver;
        if (fileName.EndsWith(".module", StringComparison.OrdinalIgnoreCase))
            return TypeScriptFileType.Module;

        // Check if file contains models or interfaces
        if (fileName.Contains("model", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("interface", StringComparison.OrdinalIgnoreCase))
            return TypeScriptFileType.Model;

        return TypeScriptFileType.Unknown;
    }

    private string ExtractClassName(string fileName, TypeScriptFileType fileType)
    {
        // Convert kebab-case to PascalCase
        var parts = fileName.Split(new[] { '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
        var className = string.Join("", parts.Select(p =>
        {
            if (string.IsNullOrEmpty(p)) return string.Empty;
            return char.ToUpper(p[0]) + (p.Length > 1 ? p.Substring(1).ToLower() : string.Empty);
        }));

        return className;
    }

    private int CountExistingTestFiles(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

        if (string.IsNullOrEmpty(directory))
            return 0;

        var pattern = $"{fileNameWithoutExtension}.spec*.ts";
        var existingTestFiles = Directory.GetFiles(directory, pattern);

        return existingTestFiles.Length;
    }
}
