namespace AngularUnitTests.Cli.Models;

public class TypeScriptFileInfo
{
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public required TypeScriptFileType FileType { get; set; }
    public required string ClassName { get; set; }
    public string? TestFilePath { get; set; }
    public int ExistingTestFileCount { get; set; }

    /// <summary>
    /// The raw content of the TypeScript file
    /// </summary>
    public string FileContent { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a functional (arrow function) or class-based implementation
    /// </summary>
    public bool IsFunctional { get; set; }

    /// <summary>
    /// Whether the component/directive is standalone
    /// </summary>
    public bool IsStandalone { get; set; }

    /// <summary>
    /// List of detected dependencies (from inject() calls)
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// The actual export name (e.g., 'authGuard' for functional guards)
    /// </summary>
    public string ExportName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the file exports an interface or type (not a class)
    /// </summary>
    public bool IsInterfaceOrType { get; set; }

    /// <summary>
    /// List of public methods discovered in the class
    /// </summary>
    public List<MethodInfo> PublicMethods { get; set; } = new();
}

public class MethodInfo
{
    public string Name { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public List<ParameterInfo> Parameters { get; set; } = new();
    public bool IsAsync { get; set; }
}

public class ParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public enum TypeScriptFileType
{
    Unknown,
    Component,
    Service,
    Directive,
    Pipe,
    Guard,
    Interceptor,
    Resolver,
    Model,
    Module
}
