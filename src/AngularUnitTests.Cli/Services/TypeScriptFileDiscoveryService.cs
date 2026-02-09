using AngularUnitTests.Cli.Configuration;
using AngularUnitTests.Cli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AngularUnitTests.Cli.Services;

public interface ITypeScriptFileDiscoveryService
{
    Task<IEnumerable<TypeScriptFileInfo>> DiscoverTypeScriptFilesAsync(string angularAppPath, CancellationToken cancellationToken = default);
}

public class TypeScriptFileDiscoveryService : ITypeScriptFileDiscoveryService
{
    private readonly ILogger<TypeScriptFileDiscoveryService> _logger;
    private readonly AngularTestGeneratorOptions _options;

    // Regex patterns for detecting Angular patterns
    private static readonly Regex FunctionalGuardPattern = new(@"export\s+const\s+(\w+)\s*:\s*CanActivateFn", RegexOptions.Compiled);
    private static readonly Regex FunctionalInterceptorPattern = new(@"export\s+const\s+(\w+)\s*:\s*HttpInterceptorFn", RegexOptions.Compiled);
    private static readonly Regex FunctionalResolverPattern = new(@"export\s+const\s+(\w+)\s*:\s*ResolveFn", RegexOptions.Compiled);
    private static readonly Regex ClassPattern = new(@"export\s+class\s+(\w+)", RegexOptions.Compiled);
    private static readonly Regex InterfacePattern = new(@"export\s+interface\s+(\w+)", RegexOptions.Compiled);
    private static readonly Regex TypeAliasPattern = new(@"export\s+type\s+(\w+)", RegexOptions.Compiled);
    private static readonly Regex InjectPattern = new(@"inject\s*\(\s*(\w+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex StandalonePattern = new(@"standalone\s*:\s*true", RegexOptions.Compiled);
    // Method pattern: captures method name, parameters, and return type
    private static readonly Regex PublicMethodPattern = new(
        @"^\s+(?!private|protected)(\w+)\s*\(([^)]*)\)\s*:\s*(Observable<[^>]+>|Promise<[^>]+>|[^{\n]+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // Constructor injection patterns
    private static readonly Regex ConstructorBlockPattern = new(
        @"constructor\s*\(([\s\S]*?)\)\s*\{",
        RegexOptions.Compiled);
    private static readonly Regex ConstructorParamTypePattern = new(
        @"(?:private|protected|public|readonly)\s+\w+\s*[?!]?\s*:\s*(\w+)",
        RegexOptions.Compiled);
    private static readonly Regex InjectDecoratorPattern = new(
        @"@Inject\s*\(\s*(\w+)\s*\)",
        RegexOptions.Compiled);

    // Primitive types to skip when extracting constructor dependencies
    private static readonly HashSet<string> PrimitiveTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "string", "number", "boolean", "any", "void", "object", "unknown", "never", "undefined", "null"
    };

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
        var files = Directory.GetFiles(angularAppPath, "*.ts", SearchOption.AllDirectories)
            .Where(f => !IsExcludedPath(f) && !IsTestFile(f));

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileInfo = await AnalyzeTypeScriptFileAsync(file, cancellationToken);
            if (fileInfo != null)
            {
                typeScriptFiles.Add(fileInfo);
            }
        }

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

    private async Task<TypeScriptFileInfo?> AnalyzeTypeScriptFileAsync(string filePath, CancellationToken cancellationToken)
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

            // Read file content for detailed analysis
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);

            var fileInfo = new TypeScriptFileInfo
            {
                FilePath = filePath,
                FileName = fileName,
                FileType = fileType,
                ClassName = ExtractClassName(fileName, fileType),
                ExistingTestFileCount = CountExistingTestFiles(filePath),
                FileContent = content
            };

            // Analyze the file content for patterns
            AnalyzeFileContent(fileInfo, content);

            return fileInfo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing file: {FilePath}", filePath);
            return null;
        }
    }

    private void AnalyzeFileContent(TypeScriptFileInfo fileInfo, string content)
    {
        // Check for functional vs class-based patterns
        switch (fileInfo.FileType)
        {
            case TypeScriptFileType.Guard:
                var guardMatch = FunctionalGuardPattern.Match(content);
                if (guardMatch.Success)
                {
                    fileInfo.IsFunctional = true;
                    fileInfo.ExportName = guardMatch.Groups[1].Value;
                }
                else
                {
                    var classMatch = ClassPattern.Match(content);
                    if (classMatch.Success)
                    {
                        fileInfo.ExportName = classMatch.Groups[1].Value;
                        fileInfo.ClassName = classMatch.Groups[1].Value;
                    }
                }
                break;

            case TypeScriptFileType.Interceptor:
                var interceptorMatch = FunctionalInterceptorPattern.Match(content);
                if (interceptorMatch.Success)
                {
                    fileInfo.IsFunctional = true;
                    fileInfo.ExportName = interceptorMatch.Groups[1].Value;
                }
                else
                {
                    var classMatch = ClassPattern.Match(content);
                    if (classMatch.Success)
                    {
                        fileInfo.ExportName = classMatch.Groups[1].Value;
                        fileInfo.ClassName = classMatch.Groups[1].Value;
                    }
                }
                break;

            case TypeScriptFileType.Resolver:
                var resolverMatch = FunctionalResolverPattern.Match(content);
                if (resolverMatch.Success)
                {
                    fileInfo.IsFunctional = true;
                    fileInfo.ExportName = resolverMatch.Groups[1].Value;
                }
                else
                {
                    var classMatch = ClassPattern.Match(content);
                    if (classMatch.Success)
                    {
                        fileInfo.ExportName = classMatch.Groups[1].Value;
                        fileInfo.ClassName = classMatch.Groups[1].Value;
                    }
                }
                break;

            case TypeScriptFileType.Model:
                // Check if it's only interfaces or types (no classes)
                var hasClass = ClassPattern.IsMatch(content);
                var hasInterface = InterfacePattern.IsMatch(content);
                var hasType = TypeAliasPattern.IsMatch(content);
                fileInfo.IsInterfaceOrType = !hasClass && (hasInterface || hasType);

                // Get the first export name
                if (hasClass)
                {
                    var classMatch = ClassPattern.Match(content);
                    fileInfo.ExportName = classMatch.Groups[1].Value;
                    fileInfo.ClassName = classMatch.Groups[1].Value;
                }
                else if (hasInterface)
                {
                    var interfaceMatch = InterfacePattern.Match(content);
                    fileInfo.ExportName = interfaceMatch.Groups[1].Value;
                }
                break;

            case TypeScriptFileType.Component:
            case TypeScriptFileType.Directive:
                fileInfo.IsStandalone = StandalonePattern.IsMatch(content);
                var componentClass = ClassPattern.Match(content);
                if (componentClass.Success)
                {
                    fileInfo.ExportName = componentClass.Groups[1].Value;
                    fileInfo.ClassName = componentClass.Groups[1].Value;
                }
                break;

            case TypeScriptFileType.Service:
                var serviceClass = ClassPattern.Match(content);
                if (serviceClass.Success)
                {
                    fileInfo.ExportName = serviceClass.Groups[1].Value;
                    fileInfo.ClassName = serviceClass.Groups[1].Value;
                }
                break;

            default:
                var defaultClass = ClassPattern.Match(content);
                if (defaultClass.Success)
                {
                    fileInfo.ExportName = defaultClass.Groups[1].Value;
                    fileInfo.ClassName = defaultClass.Groups[1].Value;
                }
                break;
        }

        // Extract dependencies from inject() calls
        var injectMatches = InjectPattern.Matches(content);
        foreach (Match match in injectMatches)
        {
            var dependency = match.Groups[1].Value;
            if (!fileInfo.Dependencies.Contains(dependency))
            {
                fileInfo.Dependencies.Add(dependency);
            }
        }

        // Extract dependencies from constructor injection
        var constructorMatch = ConstructorBlockPattern.Match(content);
        if (constructorMatch.Success)
        {
            var constructorParams = constructorMatch.Groups[1].Value;

            // Extract typed constructor parameters (e.g., private http: HttpClient)
            var paramTypeMatches = ConstructorParamTypePattern.Matches(constructorParams);
            foreach (Match match in paramTypeMatches)
            {
                var dependency = match.Groups[1].Value;
                if (!PrimitiveTypes.Contains(dependency) && !fileInfo.Dependencies.Contains(dependency))
                {
                    fileInfo.Dependencies.Add(dependency);
                }
            }

            // Extract @Inject() decorator dependencies (e.g., @Inject(API_BASE_URL))
            var injectDecoratorMatches = InjectDecoratorPattern.Matches(constructorParams);
            foreach (Match match in injectDecoratorMatches)
            {
                var dependency = match.Groups[1].Value;
                if (!fileInfo.Dependencies.Contains(dependency))
                {
                    fileInfo.Dependencies.Add(dependency);
                }
            }
        }

        // Extract public methods from all class-based files (needed for dependency mocking)
        if (ClassPattern.IsMatch(content) && !fileInfo.IsInterfaceOrType)
        {
            ExtractPublicMethods(fileInfo, content);
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

    private void ExtractPublicMethods(TypeScriptFileInfo fileInfo, string content)
    {
        // Find the class body
        var classMatch = ClassPattern.Match(content);
        if (!classMatch.Success) return;

        var classStartIndex = classMatch.Index;
        var classContent = content[classStartIndex..];

        // Find methods that look like: methodName(params): ReturnType {
        var methodMatches = PublicMethodPattern.Matches(classContent);

        foreach (Match match in methodMatches)
        {
            var methodName = match.Groups[1].Value.Trim();
            var parametersString = match.Groups[2].Value.Trim();
            var returnType = match.Groups[3].Value.Trim();

            // Skip constructor and private methods
            if (methodName == "constructor" ||
                methodName.StartsWith("_") ||
                methodName == "ngOnInit" ||
                methodName == "ngOnDestroy" ||
                methodName == "ngOnChanges")
            {
                continue;
            }

            var methodInfo = new Models.MethodInfo
            {
                Name = methodName,
                ReturnType = returnType,
                IsAsync = returnType.StartsWith("Observable") || returnType.StartsWith("Promise")
            };

            // Parse parameters
            if (!string.IsNullOrWhiteSpace(parametersString))
            {
                var paramParts = parametersString.Split(',');
                foreach (var param in paramParts)
                {
                    var trimmed = param.Trim();
                    var colonIndex = trimmed.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var paramName = trimmed[..colonIndex].Trim();
                        var paramType = trimmed[(colonIndex + 1)..].Trim();
                        methodInfo.Parameters.Add(new Models.ParameterInfo
                        {
                            Name = paramName,
                            Type = paramType
                        });
                    }
                }
            }

            if (!fileInfo.PublicMethods.Any(m => m.Name == methodName))
            {
                fileInfo.PublicMethods.Add(methodInfo);
            }
        }
    }
}
