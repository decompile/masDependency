using MasDependencyMap.Core.Configuration;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.SolutionLoading;
using MasDependencyMap.Core.Visualization;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Text.Json;

namespace MasDependencyMap.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        MSBuildLocator.RegisterDefaults(); // MUST BE FIRST LINE - Critical for Roslyn integration

        // Validate JSON syntax BEFORE loading configuration
        var ansiConsoleEarly = AnsiConsole.Console; // For validation errors
        var (isValid, errors) = ValidateConfigurationFiles(ansiConsoleEarly);
        if (!isValid)
        {
            return 1; // Exit with error code
        }

        // Load configuration from JSON files (after validation passes)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("filter-config.json", optional: true, reloadOnChange: false)
            .AddJsonFile("scoring-config.json", optional: true, reloadOnChange: false)
            .Build();

        // Define commands and options BEFORE building ServiceProvider to enable early parsing
        var rootCommand = new RootCommand("masDependencyMap - .NET dependency analysis tool");

        // Add --version option (available globally via Recursive flag)
        var versionOption = new Option<bool>("--version")
        {
            Description = "Show version information",
            Recursive = true
        };
        rootCommand.Add(versionOption);

        // Define options for analyze command
        var solutionOption = new Option<FileInfo?>("--solution")
        {
            Description = "Path to .sln file (required)"
        };

        var outputOption = new Option<DirectoryInfo?>("--output")
        {
            Description = "Output directory (default: current directory)"
        };

        var configOption = new Option<FileInfo?>("--config")
        {
            Description = "Path to filter/scoring configuration file"
        };

        var reportsOption = new Option<string?>("--reports")
        {
            Description = "Report types to generate: text|csv|all (default: all)",
            DefaultValueFactory = parseResult => "all"
        };

        var formatOption = new Option<string?>("--format")
        {
            Description = "Output format: png|svg|both (default: both)",
            DefaultValueFactory = parseResult => "both"
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Enable detailed logging"
        };

        // Create analyze command
        var analyzeCommand = new Command("analyze", "Analyze solution dependencies")
        {
            solutionOption,
            outputOption,
            configOption,
            reportsOption,
            formatOption,
            verboseOption
        };

        rootCommand.Subcommands.Add(analyzeCommand);

        // Parse args EARLY to extract --verbose flag BEFORE building ServiceProvider
        var parseResult = rootCommand.Parse(args);
        var verbose = parseResult.GetValue(verboseOption);

        // Set up DI container
        var services = new ServiceCollection();
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
        services.AddSingleton<IConfiguration>(configuration);

        // Register logging services with DYNAMIC log level based on --verbose flag
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Warning);

            // Filter out noisy Microsoft/System logs
            builder.AddFilter("Microsoft", LogLevel.Error);
            builder.AddFilter("System", LogLevel.Error);
        });

        // Register Core service interfaces (Singleton lifetime for stateless services)
        // Use TryAdd pattern to allow test overrides per project-context.md line 217
        // Fallback chain: All three loaders registered as Transient (new instance per analysis)
        services.TryAddTransient<RoslynSolutionLoader>(); // Transient: new instance per analysis
        services.TryAddTransient<MSBuildSolutionLoader>(); // Transient: new instance per analysis
        services.TryAddTransient<ProjectFileSolutionLoader>(); // Transient: new instance per analysis
        services.TryAddTransient<FallbackSolutionLoader>(); // Orchestrator: new instance per analysis
        services.TryAddTransient<ISolutionLoader, FallbackSolutionLoader>(); // Primary interface implementation
        services.TryAddSingleton<IGraphvizRenderer, GraphvizRenderer>();
        services.TryAddSingleton<IDependencyGraphBuilder, DependencyGraphBuilder>();

        // Register FilterConfiguration with validation
        services
            .AddOptions<FilterConfiguration>()
            .Bind(configuration.GetSection("FrameworkFilters"))
            .ValidateOnStart();

        // Register ScoringConfiguration with validation
        services
            .AddOptions<ScoringConfiguration>()
            .Bind(configuration.GetSection("ScoringWeights"))
            .Validate(config =>
            {
                var sum = config.Coupling + config.Complexity + config.TechDebt + config.ExternalExposure;
                return Math.Abs(sum - 1.0) < 0.01; // Allow tiny floating-point errors
            }, "Scoring weights must sum to 1.0")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Build service provider and trigger validation
        ServiceProvider serviceProvider;
        try
        {
            serviceProvider = services.BuildServiceProvider();
            // Trigger ValidateOnStart by accessing options
            var filterConfig = serviceProvider.GetRequiredService<IOptions<FilterConfiguration>>().Value;
            var scoringConfig = serviceProvider.GetRequiredService<IOptions<ScoringConfiguration>>().Value;

            // Display loaded configuration (smoke test output)
            ansiConsoleEarly.MarkupLine("[green]✓ Configuration loaded successfully[/]");
            ansiConsoleEarly.MarkupLine($"[dim]  Blocklist patterns:[/] {filterConfig.BlockList.Count}");
            ansiConsoleEarly.MarkupLine($"[dim]  Allowlist patterns:[/] {filterConfig.AllowList.Count}");
            ansiConsoleEarly.MarkupLine($"[dim]  Scoring weights:[/] C={scoringConfig.Coupling:F2}, Cx={scoringConfig.Complexity:F2}, TD={scoringConfig.TechDebt:F2}, EE={scoringConfig.ExternalExposure:F2}");

            // Validate DI container can resolve all critical services
            _ = serviceProvider.GetRequiredService<ISolutionLoader>();
            _ = serviceProvider.GetRequiredService<IGraphvizRenderer>();
            _ = serviceProvider.GetRequiredService<IDependencyGraphBuilder>();
            _ = serviceProvider.GetRequiredService<ILogger<Program>>();
        }
        catch (OptionsValidationException ex)
        {
            ansiConsoleEarly.MarkupLine("[red]Error:[/] Configuration validation failed");
            ansiConsoleEarly.MarkupLine($"[dim]Details:[/] {ex.Message}");
            foreach (var failure in ex.Failures)
            {
                ansiConsoleEarly.MarkupLine($"[dim]  - {failure}[/]");
            }
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            ansiConsoleEarly.MarkupLine("[red]Error:[/] DI container validation failed");
            ansiConsoleEarly.MarkupLine($"[dim]Reason:[/] {ex.Message}");
            ansiConsoleEarly.MarkupLine("[dim]Suggestion:[/] Check service registrations in Program.cs");
            return 1;
        }

        // Set up action for analyze command
        analyzeCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var ansiConsole = serviceProvider.GetRequiredService<IAnsiConsole>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // Check for --version flag
            if (parseResult.GetValue(versionOption))
            {
                ShowVersion(ansiConsole);
                return 0;
            }

            // Get option values
            var solution = parseResult.GetValue(solutionOption);
            var output = parseResult.GetValue(outputOption);
            var config = parseResult.GetValue(configOption);
            var reports = parseResult.GetValue(reportsOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);

            // Validate required option BEFORE logging (to avoid confusing diagnostics)
            if (solution == null)
            {
                ansiConsole.MarkupLine("[red]Error:[/] --solution is required");
                ansiConsole.MarkupLine("[dim]Reason:[/] The analyze command requires a solution file path");
                ansiConsole.MarkupLine("[dim]Suggestion:[/] Use --solution path/to/your.sln");
                return 1;
            }

            if (!solution.Exists)
            {
                ansiConsole.MarkupLine($"[red]Error:[/] Solution file not found");
                ansiConsole.MarkupLine($"[dim]Reason:[/] No file exists at {solution.FullName}");
                ansiConsole.MarkupLine("[dim]Suggestion:[/] Verify the path and try again");
                return 1;
            }

            // Example structured logging (for demonstration - typically commented in production)
            // Uncomment these lines to see structured logging in action with --verbose flag
            // logger.LogInformation("Analyze command invoked with solution: {SolutionPath}", solution.FullName);
            // logger.LogDebug("Configuration - Reports: {Reports}, Format: {Format}", reports ?? "N/A", format ?? "N/A");

            // Display parsed options using IAnsiConsole
            ansiConsole.MarkupLine("[bold green]Parsed Options:[/]");
            ansiConsole.MarkupLine($"  [dim]Solution:[/] {solution?.FullName ?? "N/A"}");
            ansiConsole.MarkupLine($"  [dim]Output:[/] {output?.FullName ?? "current directory"}");
            ansiConsole.MarkupLine($"  [dim]Config:[/] {config?.FullName ?? "none"}");
            ansiConsole.MarkupLine($"  [dim]Reports:[/] {reports}");
            ansiConsole.MarkupLine($"  [dim]Format:[/] {format}");
            ansiConsole.MarkupLine($"  [dim]Verbose:[/] {verbose}");
            ansiConsole.MarkupLine("");

            // Show success message
            ansiConsole.MarkupLine("[green]✓ Analysis command received successfully![/]");

            return await Task.FromResult(0);
        });

        // Set up action for root command (handles --version and no command cases)
        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var ansiConsole = serviceProvider.GetRequiredService<IAnsiConsole>();

            // Check for --version flag
            if (parseResult.GetValue(versionOption))
            {
                ShowVersion(ansiConsole);
                return 0;
            }

            // No command specified, should not reach here as help will be shown automatically
            return await Task.FromResult(0);
        });

        // Execute command
        // Note: System.CommandLine 2.0.2 API uses Invoke(), wrapped in Task for async Main compatibility
        return await Task.FromResult(rootCommand.Parse(args).Invoke());
    }

    private static (bool IsValid, List<string> Errors) ValidateConfigurationFiles(IAnsiConsole console)
    {
        var errors = new List<string>();
        var files = new[] { "filter-config.json", "scoring-config.json" };

        foreach (var file in files)
        {
            if (!File.Exists(file)) continue; // Missing is OK - defaults used

            try
            {
                var json = File.ReadAllText(file);
                JsonDocument.Parse(json).Dispose();
            }
            catch (JsonException ex)
            {
                console.MarkupLine($"[red]Error:[/] JSON syntax error in [yellow]{file}[/]");
                console.MarkupLine($"[dim]Location:[/] Line {ex.LineNumber}, Position {ex.BytePositionInLine}");
                console.MarkupLine($"[dim]Details:[/] {ex.Message.EscapeMarkup()}");
                errors.Add($"{file}({ex.LineNumber},{ex.BytePositionInLine}): {ex.Message}");
            }
        }

        return (errors.Count == 0, errors);
    }

    private static void ShowVersion(IAnsiConsole ansiConsole)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "1.0.0";
        ansiConsole.MarkupLine($"[green]masDependencyMap[/] version {version}");
    }
}
