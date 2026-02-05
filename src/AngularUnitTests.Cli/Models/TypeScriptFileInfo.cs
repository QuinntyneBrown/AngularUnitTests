namespace AngularUnitTests.Cli.Models;

public class TypeScriptFileInfo
{
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public required TypeScriptFileType FileType { get; set; }
    public required string ClassName { get; set; }
    public string? TestFilePath { get; set; }
    public int ExistingTestFileCount { get; set; }
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
