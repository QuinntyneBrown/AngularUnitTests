using AngularUnitTests.Cli.Commands;
using AngularUnitTests.Cli.Configuration;
using AngularUnitTests.Cli.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Configure services
builder.Services.Configure<AngularTestGeneratorOptions>(
    builder.Configuration.GetSection(AngularTestGeneratorOptions.SectionName));

builder.Services.AddSingleton<ITypeScriptFileDiscoveryService, TypeScriptFileDiscoveryService>();
builder.Services.AddSingleton<IJestTestGeneratorService, JestTestGeneratorService>();
builder.Services.AddSingleton<GenerateTestsCommandHandler>();

var host = builder.Build();

// Create root command
var rootCommand = new RootCommand("Angular Unit Tests CLI - Generate Jest unit tests for Angular TypeScript files");

// Create and configure generate command
var generateCommand = new GenerateTestsCommand();

var pathOption = generateCommand.Options.OfType<Option<string>>().First(o => o.Name == "path");

generateCommand.SetHandler(async (string path) =>
{
    var handler = host.Services.GetRequiredService<GenerateTestsCommandHandler>();
    await handler.HandleAsync(path, CancellationToken.None);
}, pathOption);

rootCommand.AddCommand(generateCommand);

// Execute the command
return await rootCommand.InvokeAsync(args);
