namespace AngularUnitTests.Cli.Configuration;

public class AngularTestGeneratorOptions
{
    public const string SectionName = "AngularTestGenerator";

    public int TargetCoveragePercentage { get; set; } = 80;
    public string TestFileExtension { get; set; } = ".spec.ts";
    public string[] TypeScriptExtensions { get; set; } = new[] { ".ts" };
    public string[] ExcludedDirectories { get; set; } = new[] { "node_modules", "dist", ".angular" };
    public bool GenerateJestConfig { get; set; } = true;
}
