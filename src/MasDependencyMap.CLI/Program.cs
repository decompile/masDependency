using MasDependencyMap.Core.Configuration;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.ExtractionScoring;
using MasDependencyMap.Core.Filtering;
using MasDependencyMap.Core.Rendering;
using MasDependencyMap.Core.Reporting;
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
            Description = "Path to .sln file (single solution analysis)"
        };

        var solutionsOption = new Option<FileInfo[]?>("--solutions")
        {
            Description = "Paths to multiple .sln files (multi-solution ecosystem analysis)",
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.OneOrMore
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
            solutionsOption,
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
        services.TryAddSingleton<IMultiSolutionAnalyzer, MultiSolutionAnalyzer>(); // Multi-solution coordinator
        services.TryAddSingleton<MasDependencyMap.Core.Rendering.IGraphvizRenderer, MasDependencyMap.Core.Rendering.GraphvizRenderer>();
        services.TryAddSingleton<IDependencyGraphBuilder, DependencyGraphBuilder>();
        services.TryAddSingleton<IFrameworkFilter, FrameworkFilter>();
        services.TryAddSingleton<MasDependencyMap.Core.Visualization.IDotGenerator, MasDependencyMap.Core.Visualization.DotGenerator>();
        services.TryAddSingleton<ITarjanCycleDetector, TarjanCycleDetector>();
        services.TryAddSingleton<ICycleStatisticsCalculator, CycleStatisticsCalculator>();
        services.TryAddSingleton<ICouplingAnalyzer, RoslynCouplingAnalyzer>();
        services.TryAddSingleton<IWeakEdgeIdentifier, WeakEdgeIdentifier>();
        services.TryAddSingleton<IRecommendationGenerator, RecommendationGenerator>();

        // Epic 4: Extraction Scoring Services
        services.TryAddSingleton<ICouplingMetricCalculator, CouplingMetricCalculator>();
        services.TryAddSingleton<IComplexityMetricCalculator, ComplexityMetricCalculator>();
        services.TryAddSingleton<ITechDebtAnalyzer, TechDebtAnalyzer>();
        services.TryAddSingleton<IExternalApiDetector, ExternalApiDetector>();
        services.TryAddSingleton<IExtractionScoreCalculator, ExtractionScoreCalculator>();
        services.TryAddSingleton<IRankedCandidateGenerator, RankedCandidateGenerator>();

        // Epic 5: Reporting Services
        services.TryAddSingleton<ITextReportGenerator, TextReportGenerator>();
        services.TryAddSingleton<ICsvExporter, CsvExporter>();

        // Register FilterConfiguration with validation
        services
            .AddOptions<FilterConfiguration>()
            .Bind(configuration.GetSection("FrameworkFilters"))
            .ValidateDataAnnotations()
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
            _ = serviceProvider.GetRequiredService<MasDependencyMap.Core.Rendering.IGraphvizRenderer>();
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
            var solutions = parseResult.GetValue(solutionsOption);
            var output = parseResult.GetValue(outputOption);
            var config = parseResult.GetValue(configOption);
            var reports = parseResult.GetValue(reportsOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);

            // Validate that at least one solution source is provided
            if (solution == null && (solutions == null || solutions.Length == 0))
            {
                ansiConsole.MarkupLine("[red]Error:[/] Either --solution or --solutions is required");
                ansiConsole.MarkupLine("[dim]Reason:[/] The analyze command requires at least one solution file path");
                ansiConsole.MarkupLine("[dim]Suggestion:[/] Use --solution path/to/your.sln OR --solutions path/to/sol1.sln path/to/sol2.sln");
                return 1;
            }

            // Validate that both options aren't used together
            if (solution != null && solutions != null && solutions.Length > 0)
            {
                ansiConsole.MarkupLine("[red]Error:[/] Cannot use both --solution and --solutions");
                ansiConsole.MarkupLine("[dim]Reason:[/] Use --solution for single solution OR --solutions for multiple solutions");
                ansiConsole.MarkupLine("[dim]Suggestion:[/] Choose one option based on your analysis needs");
                return 1;
            }

            // Determine if single or multi-solution analysis
            bool isMultiSolution = solutions != null && solutions.Length > 0;
            var solutionFiles = isMultiSolution ? solutions! : new[] { solution! };

            // Validate all solution files exist
            var missingSolutions = solutionFiles.Where(s => !s.Exists).ToList();
            if (missingSolutions.Any())
            {
                ansiConsole.MarkupLine("[red]Error:[/] Solution file(s) not found");
                foreach (var missing in missingSolutions)
                {
                    ansiConsole.MarkupLine($"[dim]  - {missing.FullName}[/]");
                }
                ansiConsole.MarkupLine("[dim]Suggestion:[/] Verify the paths and try again");
                return 1;
            }

            // Display parsed options using IAnsiConsole
            ansiConsole.MarkupLine("[bold green]Starting Analysis:[/]");
            if (isMultiSolution)
            {
                ansiConsole.MarkupLine($"  [dim]Solutions:[/] {solutions!.Length} files");
                foreach (var sol in solutions!)
                {
                    ansiConsole.MarkupLine($"    [dim]- {sol.Name}[/]");
                }
            }
            else
            {
                ansiConsole.MarkupLine($"  [dim]Solution:[/] {solution?.Name ?? "N/A"}");
            }
            var outputDir = output?.FullName ?? Directory.GetCurrentDirectory();
            ansiConsole.MarkupLine($"  [dim]Output:[/] {outputDir}");
            ansiConsole.MarkupLine("");

            try
            {
                // Get required services
                var solutionLoader = serviceProvider.GetRequiredService<ISolutionLoader>();
                var multiSolutionAnalyzer = serviceProvider.GetRequiredService<IMultiSolutionAnalyzer>();
                var graphBuilder = serviceProvider.GetRequiredService<IDependencyGraphBuilder>();
                var frameworkFilter = serviceProvider.GetRequiredService<IFrameworkFilter>();
                var dotGenerator = serviceProvider.GetRequiredService<IDotGenerator>();
                var graphvizRenderer = serviceProvider.GetRequiredService<MasDependencyMap.Core.Rendering.IGraphvizRenderer>();

                // Check Graphviz availability
                var isGraphvizInstalled = await graphvizRenderer.IsGraphvizInstalledAsync();
                if (!isGraphvizInstalled)
                {
                    ansiConsole.MarkupLine("[yellow]Warning:[/] Graphviz not found - DOT file will be generated but not rendered to PNG/SVG");
                    ansiConsole.MarkupLine("[dim]Suggestion:[/] Install Graphviz from https://graphviz.org/download/");
                    ansiConsole.MarkupLine("");
                }

                string solutionName;
                IReadOnlyList<SolutionAnalysis> solutionAnalyses;

                if (isMultiSolution)
                {
                    // Multi-solution analysis with progress
                    var solutionPaths = solutions!.Select(s => s.FullName).ToList();
                    solutionName = "Ecosystem";

                    IReadOnlyList<SolutionAnalysis> loadedSolutions = null!;
                    await ansiConsole.Progress()
                        .Columns(new ProgressColumn[]
                        {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new RemainingTimeColumn(),
                            new SpinnerColumn()
                        })
                        .StartAsync(async ctx =>
                        {
                            var loadTask = ctx.AddTask($"[cyan]Loading solutions[/]", maxValue: solutionPaths.Count);
                            var progressReporter = new Progress<SolutionLoadProgress>(progress =>
                            {
                                if (progress.IsComplete)
                                {
                                    if (string.IsNullOrEmpty(progress.ErrorMessage))
                                    {
                                        loadTask.Increment(1);
                                        loadTask.Description = $"[cyan]Loading solutions[/] [green]✓[/] {progress.CurrentFileName} ({progress.ProjectCount} projects)";
                                    }
                                    else
                                    {
                                        loadTask.Increment(1);
                                        loadTask.Description = $"[cyan]Loading solutions[/] [red]✗[/] {progress.CurrentFileName}";
                                    }
                                }
                                else
                                {
                                    loadTask.Description = $"[cyan]Loading[/] {progress.CurrentFileName}...";
                                }
                            });

                            loadedSolutions = await multiSolutionAnalyzer.LoadAllAsync(solutionPaths, progressReporter, cancellationToken)
                                .ConfigureAwait(false);

                            loadTask.StopTask();
                            loadTask.Description = $"[green]✓[/] Loaded {loadedSolutions.Count}/{solutionPaths.Count} solutions";
                        });

                    solutionAnalyses = loadedSolutions;
                }
                else
                {
                    // Single solution analysis
                    solutionName = Path.GetFileNameWithoutExtension(solution!.Name);
                    ansiConsole.MarkupLine($"[cyan]Loading solution[/] {solution!.Name}...");
                    var analysis = await solutionLoader.LoadAsync(solution.FullName, cancellationToken);
                    solutionAnalyses = new[] { analysis };
                    ansiConsole.MarkupLine($"[green]✓[/] Loaded solution: {analysis.Projects.Count} projects");
                }

                // Build dependency graph
                ansiConsole.MarkupLine("[cyan]Building dependency graph...[/]");
                var graph = await graphBuilder.BuildAsync(solutionAnalyses, cancellationToken);
                ansiConsole.MarkupLine($"[green]✓[/] Built graph: {graph.VertexCount} projects, {graph.EdgeCount} dependencies");

                // Filter framework dependencies
                var filteredGraph = await frameworkFilter.FilterAsync(graph, cancellationToken);
                var removedCount = graph.EdgeCount - filteredGraph.EdgeCount;
                ansiConsole.MarkupLine($"[green]✓[/] Filtered {removedCount} framework dependencies");

                // Detect circular dependencies
                var cycleDetector = serviceProvider.GetRequiredService<ITarjanCycleDetector>();
                ansiConsole.MarkupLine("[cyan]Detecting circular dependencies...[/]");
                var cycles = await cycleDetector.DetectCyclesAsync(filteredGraph, cancellationToken);

                IReadOnlyList<CycleBreakingSuggestion>? recommendations = null;

                if (cycles.Count > 0)
                {
                    var statsCalculator = serviceProvider.GetRequiredService<ICycleStatisticsCalculator>();
                    var statistics = await statsCalculator.CalculateAsync(cycles, filteredGraph.VertexCount, cancellationToken);

                    ansiConsole.MarkupLine($"[yellow]⚠ Found {statistics.TotalCycles} circular dependency chains[/]");
                    ansiConsole.MarkupLine($"[dim]  Projects in cycles:[/] {statistics.TotalProjectsInCycles} ({statistics.ParticipationRate:F1}%)");
                    ansiConsole.MarkupLine($"[dim]  Largest cycle:[/] {statistics.LargestCycleSize} projects");

                    // Generate cycle-breaking recommendations
                    var recommendationGenerator = serviceProvider.GetRequiredService<IRecommendationGenerator>();
                    recommendations = await recommendationGenerator.GenerateRecommendationsAsync(cycles, cancellationToken);

                    if (recommendations.Count > 0)
                    {
                        ansiConsole.MarkupLine($"[dim]  Break suggestions:[/] {Math.Min(recommendations.Count, 10)} recommended edges to break cycles");
                    }
                }
                else
                {
                    ansiConsole.MarkupLine("[green]✓[/] No circular dependencies detected");
                }

                // Generate DOT file with cycle and recommendation highlighting
                // Using default maxBreakPoints (10) to avoid visual clutter
                // Extraction scores and score labels integration deferred to Epic 5 (Reporting)
                var dotFilePath = await dotGenerator.GenerateAsync(filteredGraph, outputDir, solutionName, cycles, recommendations, maxBreakPoints: 10, extractionScores: null, showScoreLabels: false, cancellationToken);
                ansiConsole.MarkupLine($"[green]✓[/] Generated DOT file: {Path.GetFileName(dotFilePath)}");

                // Render to image formats if Graphviz is available
                if (isGraphvizInstalled)
                {
                    var shouldRenderPng = format == "png" || format == "both";
                    var shouldRenderSvg = format == "svg" || format == "both";

                    if (shouldRenderPng)
                    {
                        var pngPath = await graphvizRenderer.RenderToFileAsync(dotFilePath, GraphvizOutputFormat.Png, cancellationToken);
                        ansiConsole.MarkupLine($"[green]✓[/] Rendered PNG: {Path.GetFileName(pngPath)}");
                    }

                    if (shouldRenderSvg)
                    {
                        var svgPath = await graphvizRenderer.RenderToFileAsync(dotFilePath, GraphvizOutputFormat.Svg, cancellationToken);
                        ansiConsole.MarkupLine($"[green]✓[/] Rendered SVG: {Path.GetFileName(svgPath)}");
                    }
                }

                // Generate reports based on --reports flag
                var shouldGenerateText = reports == "text" || reports == "all";
                var shouldGenerateCsv = reports == "csv" || reports == "all";

                // Calculate extraction scores for reporting (Epic 4 integration)
                IReadOnlyList<ExtractionScore>? extractionScores = null;
                if (shouldGenerateText || shouldGenerateCsv)
                {
                    ansiConsole.MarkupLine("[cyan]Calculating extraction difficulty scores...[/]");
                    var extractionScoreCalculator = serviceProvider.GetRequiredService<IExtractionScoreCalculator>();
                    extractionScores = await extractionScoreCalculator.CalculateForAllProjectsAsync(filteredGraph, cancellationToken);
                    ansiConsole.MarkupLine($"[green]✓[/] Calculated extraction scores for {extractionScores.Count} projects");
                }

                // Generate text report (Story 5.1-5.8)
                if (shouldGenerateText)
                {
                    ansiConsole.MarkupLine("[cyan]Generating text report...[/]");
                    var textReportGenerator = serviceProvider.GetRequiredService<ITextReportGenerator>();
                    var reportPath = await textReportGenerator.GenerateAsync(
                        filteredGraph,
                        outputDir,
                        solutionName,
                        cycles: cycles.Count > 0 ? cycles : null,
                        extractionScores: extractionScores,
                        recommendations: recommendations,
                        writeToConsole: verbose,
                        cancellationToken);
                    ansiConsole.MarkupLine($"[green]✓[/] Generated text report: {Path.GetFileName(reportPath)}");
                }

                // Generate CSV exports (Stories 5.5-5.7)
                if (shouldGenerateCsv && extractionScores != null)
                {
                    var csvExporter = serviceProvider.GetRequiredService<ICsvExporter>();

                    // Export extraction scores
                    ansiConsole.MarkupLine("[cyan]Exporting extraction scores to CSV...[/]");
                    var scoresPath = await csvExporter.ExportExtractionScoresAsync(extractionScores, outputDir, solutionName, cancellationToken);
                    ansiConsole.MarkupLine($"[green]✓[/] Exported extraction scores: {Path.GetFileName(scoresPath)}");

                    // Export cycle analysis if cycles exist
                    if (cycles.Count > 0 && recommendations != null)
                    {
                        ansiConsole.MarkupLine("[cyan]Exporting cycle analysis to CSV...[/]");
                        var cyclesPath = await csvExporter.ExportCycleAnalysisAsync(cycles, recommendations, outputDir, solutionName, cancellationToken);
                        ansiConsole.MarkupLine($"[green]✓[/] Exported cycle analysis: {Path.GetFileName(cyclesPath)}");
                    }

                    // Export dependency matrix
                    ansiConsole.MarkupLine("[cyan]Exporting dependency matrix to CSV...[/]");
                    var matrixPath = await csvExporter.ExportDependencyMatrixAsync(filteredGraph, outputDir, solutionName, cancellationToken);
                    ansiConsole.MarkupLine($"[green]✓[/] Exported dependency matrix: {Path.GetFileName(matrixPath)}");
                }

                ansiConsole.MarkupLine("");
                ansiConsole.MarkupLine("[bold green]✓ Analysis complete![/]");
                return 0;
            }
            catch (SolutionLoadException ex)
            {
                ansiConsole.MarkupLine("[red]Error:[/] Failed to load solution(s)");
                ansiConsole.MarkupLine($"[dim]Reason:[/] {ex.Message.EscapeMarkup()}");
                ansiConsole.MarkupLine("[dim]Suggestion:[/] Check solution file paths and try again");
                logger.LogError(ex, "Solution load failed");
                return 1;
            }
            catch (GraphvizNotFoundException ex)
            {
                ansiConsole.MarkupLine("[red]Error:[/] Graphviz not found");
                ansiConsole.MarkupLine($"[dim]Reason:[/] {ex.Message.EscapeMarkup()}");
                return 1;
            }
            catch (Exception ex)
            {
                ansiConsole.MarkupLine("[red]Error:[/] Analysis failed");
                ansiConsole.MarkupLine($"[dim]Details:[/] {ex.Message.EscapeMarkup()}");
                logger.LogError(ex, "Unexpected error during analysis");
                return 1;
            }
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
