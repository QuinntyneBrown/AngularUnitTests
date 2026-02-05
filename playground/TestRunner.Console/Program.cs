using System.Diagnostics;

namespace TestRunner.Console;

class Program
{
    private static readonly string PlaygroundDir = @"C:\projects\AngularUnitTests\playground";
    private static readonly string ArtifactsDir = Path.Combine(PlaygroundDir, "artifacts");
    private static readonly string CliProjectPath = @"C:\projects\AngularUnitTests\src\AngularUnitTests.Cli\AngularUnitTests.Cli.csproj";
    private static readonly string RepoDir = @"C:\projects\AngularUnitTests";
    private static readonly string BooksRepoUrl = "https://github.com/QuinntyneBrown/Books";

    private static CancellationTokenSource? _commitCancellation;
    private static bool _hasChanges = false;
    private static DateTime _lastChangeTime = DateTime.MinValue;
    private static readonly object _changeLock = new();

    static async Task<int> Main(string[] args)
    {
        System.Console.WriteLine("=== Angular Unit Test Generator Runner ===\n");

        string? tempDir = null;

        try
        {
            // Start the periodic commit task
            _commitCancellation = new CancellationTokenSource();
            var commitTask = StartPeriodicCommitTask(_commitCancellation.Token);

            // Step 1: Clone Books repo to temp folder
            tempDir = Path.Combine(Path.GetTempPath(), $"Books_{Guid.NewGuid():N}");
            System.Console.WriteLine($"Step 1: Cloning Books repository to {tempDir}...");

            var cloneResult = await RunProcessAsync("git", $"clone {BooksRepoUrl} \"{tempDir}\"");
            if (cloneResult != 0)
            {
                System.Console.WriteLine("Failed to clone repository");
                return 1;
            }
            System.Console.WriteLine("Repository cloned successfully.\n");

            // Step 2: Copy src/Ui to artifacts
            var sourceUiDir = Path.Combine(tempDir, "src", "Ui");
            var targetUiDir = Path.Combine(ArtifactsDir, "Ui");

            System.Console.WriteLine($"Step 2: Copying {sourceUiDir} to {targetUiDir}...");

            if (Directory.Exists(targetUiDir))
            {
                System.Console.WriteLine("Removing existing Ui directory in artifacts...");
                Directory.Delete(targetUiDir, recursive: true);
            }

            CopyDirectory(sourceUiDir, targetUiDir);
            System.Console.WriteLine("Angular workspace copied successfully.\n");

            // Step 3: Remove all spec files
            System.Console.WriteLine("Step 3: Removing all existing spec files...");
            var specFilesRemoved = RemoveSpecFiles(targetUiDir);
            System.Console.WriteLine($"Removed {specFilesRemoved} spec file(s).\n");

            MarkCodeChange();

            // Step 4: Run the CLI to generate tests
            System.Console.WriteLine("Step 4: Running AngularUnitTests.Cli to generate tests...");
            var cliResult = await RunProcessAsync("dotnet", $"run --project \"{CliProjectPath}\" -- generate --path \"{targetUiDir}\"");
            if (cliResult != 0)
            {
                System.Console.WriteLine("Warning: CLI returned non-zero exit code, but continuing...");
            }
            System.Console.WriteLine("Test generation complete.\n");

            MarkCodeChange();

            // Step 5: Install npm dependencies and run tests
            System.Console.WriteLine("Step 5: Installing npm dependencies...");
            var npmInstallResult = await RunProcessAsync("npm", "install", targetUiDir);
            if (npmInstallResult != 0)
            {
                System.Console.WriteLine("Failed to install npm dependencies");
                return 1;
            }
            System.Console.WriteLine("Dependencies installed successfully.\n");

            // Step 6: Run tests with coverage
            System.Console.WriteLine("Step 6: Running tests with coverage...");
            var testResult = await RunTestsWithCoverage(targetUiDir);

            System.Console.WriteLine($"\nTest run completed with exit code: {testResult}\n");

            // Final commit
            await ForceCommitIfChanges();

            // Cancel the periodic commit task
            _commitCancellation.Cancel();
            try { await commitTask; } catch (OperationCanceledException) { }

            return testResult == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            System.Console.WriteLine(ex.StackTrace);
            return 1;
        }
        finally
        {
            // Cleanup temp directory
            if (tempDir != null && Directory.Exists(tempDir))
            {
                System.Console.WriteLine($"\nCleaning up temp directory: {tempDir}");
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                    System.Console.WriteLine("Temp directory cleaned up successfully.");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Warning: Failed to cleanup temp directory: {ex.Message}");
                }
            }

            _commitCancellation?.Cancel();
            _commitCancellation?.Dispose();
        }
    }

    private static void MarkCodeChange()
    {
        lock (_changeLock)
        {
            _hasChanges = true;
            _lastChangeTime = DateTime.Now;
        }
    }

    private static async Task StartPeriodicCommitTask(CancellationToken cancellationToken)
    {
        var commitNumber = 1;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                bool shouldCommit;
                lock (_changeLock)
                {
                    shouldCommit = _hasChanges;
                    if (shouldCommit)
                    {
                        _hasChanges = false;
                    }
                }

                if (shouldCommit)
                {
                    System.Console.WriteLine("\n[Auto-commit] Committing changes...");

                    await RunProcessAsync("git", "add -A", RepoDir);
                    var commitResult = await RunProcessAsync("git",
                        $"commit -m \"Auto-commit #{commitNumber}: Test generation progress\"", RepoDir);

                    if (commitResult == 0)
                    {
                        await RunProcessAsync("git", "push origin HEAD", RepoDir);
                        System.Console.WriteLine($"[Auto-commit] Commit #{commitNumber} pushed successfully.\n");
                        commitNumber++;
                    }
                    else
                    {
                        System.Console.WriteLine("[Auto-commit] No changes to commit.\n");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Auto-commit] Error: {ex.Message}");
            }
        }
    }

    private static async Task ForceCommitIfChanges()
    {
        bool hasChanges;
        lock (_changeLock)
        {
            hasChanges = _hasChanges;
            _hasChanges = false;
        }

        if (hasChanges)
        {
            System.Console.WriteLine("\n[Final commit] Committing remaining changes...");
            await RunProcessAsync("git", "add -A", RepoDir);
            var commitResult = await RunProcessAsync("git",
                "commit -m \"Final commit: Test generation complete\"", RepoDir);

            if (commitResult == 0)
            {
                await RunProcessAsync("git", "push origin HEAD", RepoDir);
                System.Console.WriteLine("[Final commit] Pushed successfully.\n");
            }
        }
    }

    private static async Task<int> RunTestsWithCoverage(string uiDir)
    {
        // Check for different test configurations
        var jestConfigJs = Path.Combine(uiDir, "jest.config.js");
        var jestConfigTs = Path.Combine(uiDir, "jest.config.ts");
        var vitestConfigJs = Path.Combine(uiDir, "vitest.config.js");
        var vitestConfigTs = Path.Combine(uiDir, "vitest.config.ts");

        // Check for Vitest (used in Angular 18+ with @angular/build:unit-test)
        if (File.Exists(vitestConfigJs) || File.Exists(vitestConfigTs))
        {
            System.Console.WriteLine("Vitest config found, running vitest...");
            return await RunProcessAsync("npx", "vitest run --coverage", uiDir);
        }

        // Check for Jest
        if (File.Exists(jestConfigJs) || File.Exists(jestConfigTs))
        {
            System.Console.WriteLine("Jest config found, running jest...");
            return await RunProcessAsync("npx", "jest --coverage", uiDir);
        }

        // For Angular 18+ with @angular/build:unit-test (uses Vitest internally)
        // The ng test command runs tests without extra flags
        System.Console.WriteLine("Using Angular CLI test runner with Vitest...");
        return await RunProcessAsync("npx", "ng test --watch=false", uiDir);
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        }

        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (DirectoryInfo subDir in dir.GetDirectories())
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }

    private static int RemoveSpecFiles(string directory)
    {
        var count = 0;

        foreach (var file in Directory.EnumerateFiles(directory, "*.spec.ts", SearchOption.AllDirectories))
        {
            File.Delete(file);
            count++;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.spec.js", SearchOption.AllDirectories))
        {
            File.Delete(file);
            count++;
        }

        return count;
    }

    private static async Task<int> RunProcessAsync(string fileName, string arguments, string? workingDirectory = null)
    {
        System.Console.WriteLine($"  > {fileName} {arguments}");

        // On Windows, npm and npx need to be run via cmd.exe
        var isWindows = OperatingSystem.IsWindows();
        var actualFileName = fileName;
        var actualArguments = arguments;

        if (isWindows && (fileName == "npm" || fileName == "npx"))
        {
            actualFileName = "cmd.exe";
            actualArguments = $"/c {fileName} {arguments}";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = actualFileName,
            Arguments = actualArguments,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                System.Console.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                System.Console.WriteLine($"[ERR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return process.ExitCode;
    }
}
