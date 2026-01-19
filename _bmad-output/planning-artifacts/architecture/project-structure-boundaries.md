# Project Structure & Boundaries

## Complete Project Directory Structure

```
masDependencyMap/
├── README.md
├── LICENSE
├── .gitignore
├── .editorconfig
├── masDependencyMap.sln
├── Directory.Build.props                  # Shared MSBuild properties
├── Directory.Packages.props               # Central package management
├── global.json                            # .NET SDK version pinning
├── filter-config.example.json             # Example filter configuration
├── scoring-config.example.json            # Example scoring configuration
│
├── src/
│   ├── MasDependencyMap.Core/
│   │   ├── MasDependencyMap.Core.csproj
│   │   │
│   │   ├── SolutionLoading/              # FR1-FR8: Solution analysis & loading
│   │   │   ├── ISolutionLoader.cs
│   │   │   ├── RoslynSolutionLoader.cs
│   │   │   ├── MSBuildSolutionLoader.cs
│   │   │   ├── ProjectFileSolutionLoader.cs
│   │   │   ├── SolutionAnalysis.cs       # Domain model
│   │   │   ├── ProjectInfo.cs
│   │   │   └── Exceptions/
│   │   │       ├── SolutionLoadException.cs
│   │   │       ├── RoslynLoadException.cs
│   │   │       ├── MSBuildLoadException.cs
│   │   │       └── ProjectFileLoadException.cs
│   │   │
│   │   ├── Filtering/                    # FR9-FR13: Framework dependency filtering
│   │   │   ├── IFrameworkFilter.cs
│   │   │   ├── FrameworkFilter.cs
│   │   │   ├── FilterConfiguration.cs    # POCO for JSON config
│   │   │   └── FilterPatternMatcher.cs
│   │   │
│   │   ├── DependencyAnalysis/           # FR14-FR19: Dependency graph construction
│   │   │   ├── IDependencyGraphBuilder.cs
│   │   │   ├── DependencyGraphBuilder.cs
│   │   │   ├── DependencyGraph.cs        # QuikGraph wrapper
│   │   │   ├── ProjectNode.cs            # Graph vertex
│   │   │   ├── DependencyEdge.cs         # Graph edge
│   │   │   └── DependencyType.cs         # Enum: ProjectReference, BinaryReference
│   │   │
│   │   ├── CycleDetection/               # FR20-FR26: Circular dependency detection
│   │   │   ├── ICycleDetector.cs
│   │   │   ├── TarjanCycleDetector.cs    # Tarjan's SCC algorithm
│   │   │   ├── CycleInfo.cs              # Domain model
│   │   │   ├── CycleBreakingSuggestion.cs
│   │   │   └── CouplingAnalyzer.cs       # Method call counting
│   │   │
│   │   ├── Scoring/                      # FR27-FR35: Extraction difficulty scoring
│   │   │   ├── IExtractionScoreCalculator.cs
│   │   │   ├── ExtractionScoreCalculator.cs
│   │   │   ├── ScoringConfiguration.cs   # POCO for JSON config
│   │   │   ├── ExtractionScore.cs        # Domain model
│   │   │   ├── Metrics/
│   │   │   │   ├── ICouplingMetricCalculator.cs
│   │   │   │   ├── CouplingMetricCalculator.cs
│   │   │   │   ├── IComplexityMetricCalculator.cs
│   │   │   │   ├── ComplexityMetricCalculator.cs  # Roslyn cyclomatic complexity
│   │   │   │   ├── ITechDebtAnalyzer.cs
│   │   │   │   ├── TechDebtAnalyzer.cs            # .NET version detection
│   │   │   │   ├── IExternalApiDetector.cs
│   │   │   │   └── ExternalApiDetector.cs         # WebMethod, ApiController scanning
│   │   │   └── ScoringWeights.cs
│   │   │
│   │   ├── Visualization/                # FR14-FR19: DOT generation
│   │   │   ├── IDotGenerator.cs
│   │   │   ├── DotGenerator.cs           # QuikGraph.Graphviz integration
│   │   │   ├── IGraphvizRenderer.cs
│   │   │   ├── GraphvizRenderer.cs       # Process.Start wrapper
│   │   │   ├── GraphvizOutputFormat.cs   # Enum: Dot, Png, Svg
│   │   │   ├── VisualizationOptions.cs
│   │   │   └── Exceptions/
│   │   │       ├── GraphvizException.cs
│   │   │       ├── GraphvizNotFoundException.cs
│   │   │       └── GraphvizRenderException.cs
│   │   │
│   │   ├── Reporting/                    # FR36-FR43: Report generation & export
│   │   │   ├── IReportGenerator.cs
│   │   │   ├── TextReportGenerator.cs
│   │   │   ├── ICsvExporter.cs
│   │   │   ├── CsvExporter.cs            # CsvHelper integration
│   │   │   ├── Models/
│   │   │   │   ├── ExtractionScoreRecord.cs  # CSV export POCO
│   │   │   │   ├── CycleAnalysisRecord.cs
│   │   │   │   └── DependencyRecord.cs
│   │   │   └── ReportTemplates/
│   │   │       └── AnalysisReportTemplate.cs
│   │   │
│   │   ├── Configuration/                # FR52-FR55: Configuration management
│   │   │   ├── IConfigurationLoader.cs
│   │   │   ├── ConfigurationLoader.cs    # Microsoft.Extensions.Configuration
│   │   │   ├── ConfigurationValidator.cs
│   │   │   └── Exceptions/
│   │   │       └── ConfigurationException.cs
│   │   │
│   │   └── Common/                       # Shared utilities
│   │       ├── Extensions/
│   │       │   ├── StringExtensions.cs
│   │       │   └── EnumerableExtensions.cs
│   │       └── Utilities/
│   │           ├── PathUtilities.cs
│   │           └── FileUtilities.cs
│   │
│   └── MasDependencyMap.CLI/
│       ├── MasDependencyMap.CLI.csproj
│       │
│       ├── Program.cs                    # Entry point, DI setup
│       │
│       ├── Commands/                     # FR44-FR51: CLI commands
│       │   ├── AnalyzeCommand.cs         # Main analyze command
│       │   ├── AnalyzeCommandOptions.cs  # System.CommandLine options
│       │   └── CommandHandlerBase.cs     # Shared command handler logic
│       │
│       ├── Output/                       # Spectre.Console output formatting
│       │   ├── ConsoleOutputFormatter.cs
│       │   ├── ErrorFormatter.cs         # Structured error messages
│       │   ├── ProgressReporter.cs       # Progress indicators
│       │   └── TableFormatter.cs         # Table output for reports
│       │
│       ├── DependencyInjection/
│       │   └── ServiceCollectionExtensions.cs
│       │
│       └── appsettings.json              # Logging configuration
│
├── tests/
│   └── MasDependencyMap.Core.Tests/
│       ├── MasDependencyMap.Core.Tests.csproj
│       │
│       ├── SolutionLoading/
│       │   ├── RoslynSolutionLoaderTests.cs
│       │   ├── MSBuildSolutionLoaderTests.cs
│       │   └── ProjectFileSolutionLoaderTests.cs
│       │
│       ├── Filtering/
│       │   ├── FrameworkFilterTests.cs
│       │   └── FilterPatternMatcherTests.cs
│       │
│       ├── DependencyAnalysis/
│       │   ├── DependencyGraphBuilderTests.cs
│       │   └── DependencyGraphTests.cs
│       │
│       ├── CycleDetection/
│       │   ├── TarjanCycleDetectorTests.cs
│       │   └── CouplingAnalyzerTests.cs
│       │
│       ├── Scoring/
│       │   ├── ExtractionScoreCalculatorTests.cs
│       │   └── Metrics/
│       │       ├── CouplingMetricCalculatorTests.cs
│       │       ├── ComplexityMetricCalculatorTests.cs
│       │       ├── TechDebtAnalyzerTests.cs
│       │       └── ExternalApiDetectorTests.cs
│       │
│       ├── Visualization/
│       │   ├── DotGeneratorTests.cs
│       │   └── GraphvizRendererTests.cs
│       │
│       ├── Reporting/
│       │   ├── TextReportGeneratorTests.cs
│       │   └── CsvExporterTests.cs
│       │
│       ├── Configuration/
│       │   └── ConfigurationLoaderTests.cs
│       │
│       └── TestUtilities/
│           ├── TestData/
│           │   ├── SampleSolutions/
│           │   └── ExpectedOutputs/
│           ├── Fixtures/
│           │   └── SolutionAnalysisFixture.cs
│           └── Builders/
│               ├── DependencyGraphBuilder.cs
│               └── SolutionAnalysisBuilder.cs
│
├── docs/
│   ├── architecture.md                   # This document
│   ├── user-guide.md
│   ├── configuration-guide.md
│   └── development.md
│
└── samples/
    ├── SampleMonolith/                   # Sample solution for testing
    │   ├── SampleMonolith.sln
    │   └── Projects/
    └── sample-output/                    # Expected output examples
        ├── SampleMonolith-dependencies.dot
        ├── SampleMonolith-dependencies.png
        ├── SampleMonolith-analysis-report.txt
        └── SampleMonolith-extraction-scores.csv
```

## Architectural Boundaries

**Component Boundaries:**

masDependencyMap follows a **layered architecture** with clear separation:

```
┌─────────────────────────────────────────┐
│         CLI Layer (Presentation)         │
│    MasDependencyMap.CLI                  │
│  - System.CommandLine parsing            │
│  - Spectre.Console output                │
│  - DI container setup                    │
└──────────────┬──────────────────────────┘
               │ Depends on (interfaces only)
               ↓
┌─────────────────────────────────────────┐
│          Core Layer (Domain)             │
│    MasDependencyMap.Core                 │
│  - Domain models and logic               │
│  - Interface definitions                 │
│  - No dependency on CLI                  │
└──────────────────────────────────────────┘
```

**Cross-Boundary Communication:**

- **CLI → Core:** Via dependency injection of Core interfaces (ISolutionLoader, ICycleDetector, etc.)
- **Core → External Tools:** Via abstractions (IGraphvizRenderer wraps Process.Start)
- **Core → Configuration:** Via IConfiguration interface
- **Core → Logging:** Via ILogger<T> interface

**Data Boundaries:**

No database - all data boundaries are file-based:
- **Input Boundary:** .sln/.csproj/.vbproj files (read-only)
- **Configuration Boundary:** JSON files (filter-config.json, scoring-config.json)
- **Output Boundary:** DOT/PNG/SVG/TXT/CSV files (write-only to user-specified directory)

## Requirements to Structure Mapping

**Feature Mapping to Namespaces:**

| Functional Requirement Category | Namespace | Key Classes |
|--------------------------------|-----------|-------------|
| FR1-FR8: Solution Loading | `MasDependencyMap.Core.SolutionLoading` | `ISolutionLoader`, `RoslynSolutionLoader`, `MSBuildSolutionLoader` |
| FR9-FR13: Framework Filtering | `MasDependencyMap.Core.Filtering` | `IFrameworkFilter`, `FrameworkFilter`, `FilterConfiguration` |
| FR14-FR19: Dependency Visualization | `MasDependencyMap.Core.DependencyAnalysis`<br>`MasDependencyMap.Core.Visualization` | `DependencyGraphBuilder`, `DotGenerator`, `GraphvizRenderer` |
| FR20-FR26: Cycle Detection | `MasDependencyMap.Core.CycleDetection` | `TarjanCycleDetector`, `CouplingAnalyzer` |
| FR27-FR35: Extraction Scoring | `MasDependencyMap.Core.Scoring` | `ExtractionScoreCalculator`, Metrics calculators |
| FR36-FR43: Report Generation | `MasDependencyMap.Core.Reporting` | `TextReportGenerator`, `CsvExporter` |
| FR44-FR51: CLI Interface | `MasDependencyMap.CLI.Commands` | `AnalyzeCommand`, `AnalyzeCommandOptions` |
| FR52-FR55: Configuration | `MasDependencyMap.Core.Configuration` | `ConfigurationLoader`, `ConfigurationValidator` |
| FR56-FR60: Error Handling | Cross-cutting (each namespace has Exceptions/) | Custom exception hierarchy |

**Cross-Cutting Concerns Mapping:**

| Cross-Cutting Concern | Location |
|-----------------------|----------|
| Logging (NFR-Logging) | ILogger<T> injected into all Core classes |
| Progress Reporting (NFR17) | `MasDependencyMap.CLI.Output.ProgressReporter` |
| Error Messages (NFR14) | `MasDependencyMap.CLI.Output.ErrorFormatter` |
| Configuration Loading | `MasDependencyMap.Core.Configuration` |
| Custom Exceptions | `{Namespace}/Exceptions/` folders |

## Integration Points

**Internal Communication Patterns:**

1. **Analysis Pipeline Flow:**
```
CLI.AnalyzeCommand
  ↓ (via ISolutionLoader)
Core.SolutionLoading.RoslynSolutionLoader
  ↓ (fallback to)
Core.SolutionLoading.MSBuildSolutionLoader
  ↓ (returns SolutionAnalysis)
Core.DependencyAnalysis.DependencyGraphBuilder
  ↓ (builds DependencyGraph)
Core.Filtering.FrameworkFilter
  ↓ (filters graph)
Core.CycleDetection.TarjanCycleDetector
  ↓ (detects cycles)
Core.Scoring.ExtractionScoreCalculator
  ↓ (calculates scores)
Core.Visualization.DotGenerator + GraphvizRenderer
Core.Reporting.TextReportGenerator + CsvExporter
  ↓
Output files written to user-specified directory
```

2. **Dependency Injection Wiring (Program.cs):**
```csharp
services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(builder => builder.AddConsole());

// Solution loading (fallback chain)
services.AddTransient<ISolutionLoader, RoslynSolutionLoader>();
services.AddTransient<MSBuildSolutionLoader>();
services.AddTransient<ProjectFileSolutionLoader>();

// Core services
services.AddSingleton<IFrameworkFilter, FrameworkFilter>();
services.AddTransient<IDependencyGraphBuilder, DependencyGraphBuilder>();
services.AddTransient<ICycleDetector, TarjanCycleDetector>();
services.AddTransient<IExtractionScoreCalculator, ExtractionScoreCalculator>();
services.AddSingleton<IGraphvizRenderer, GraphvizRenderer>();
services.AddTransient<IDotGenerator, DotGenerator>();
services.AddTransient<IReportGenerator, TextReportGenerator>();
services.AddTransient<ICsvExporter, CsvExporter>();
```

**External Integrations:**

| External Tool/Library | Integration Point | Purpose |
|-----------------------|------------------|---------|
| Roslyn (Microsoft.CodeAnalysis) | `RoslynSolutionLoader`, `ComplexityMetricCalculator` | Semantic analysis, solution loading, complexity calculation |
| MSBuild (Microsoft.Build.Locator) | `MSBuildSolutionLoader` | Fallback solution loading |
| QuikGraph | `DependencyGraph`, `TarjanCycleDetector` | Graph data structures, SCC algorithm |
| Graphviz (external process) | `GraphvizRenderer` | DOT file rendering to PNG/SVG |
| CsvHelper | `CsvExporter` | RFC 4180 compliant CSV generation |
| System.CommandLine | `AnalyzeCommand` | CLI argument parsing |
| Spectre.Console | `CLI.Output.*` | Progress indicators, tables, formatted output |

**Data Flow:**

```
Input: .sln files
  ↓
[SolutionLoader] → Parse solution structure
  ↓
[DependencyGraphBuilder] → Build QuikGraph model
  ↓
[FrameworkFilter] → Filter Microsoft/System assemblies
  ↓
[CycleDetector] → Find strongly connected components
  ↓
[CouplingAnalyzer] → Count method calls across edges
  ↓
[ExtractionScoreCalculator] → Calculate 0-100 scores
  ↓
[DotGenerator] → Generate DOT format with colors
  ↓
[GraphvizRenderer] → Render PNG/SVG
  ↓
[ReportGenerator] → Generate text reports
  ↓
[CsvExporter] → Export extraction scores, cycles
  ↓
Output: DOT, PNG, SVG, TXT, CSV files
```

## File Organization Patterns

**Configuration Files:**

| File | Location | Purpose |
|------|----------|---------|
| `filter-config.json` | Project root (user-provided) | Framework dependency filter rules |
| `scoring-config.json` | Project root (user-provided) | Extraction difficulty scoring weights |
| `filter-config.example.json` | Project root (version-controlled) | Example filter configuration |
| `scoring-config.example.json` | Project root (version-controlled) | Example scoring configuration |
| `appsettings.json` | `src/MasDependencyMap.CLI/` | Logging configuration |
| `Directory.Build.props` | Project root | Shared MSBuild properties (nullable, implicit usings) |
| `Directory.Packages.props` | Project root | Central package version management |
| `global.json` | Project root | .NET SDK version pinning |

**Source Organization:**

- **By Feature:** Each namespace corresponds to a major functional capability
- **Interface Segregation:** Interfaces defined in same namespace as implementations
- **Exception Hierarchy:** Exceptions grouped in `{Namespace}/Exceptions/` folders
- **Domain Models:** POCOs for data structures (SolutionAnalysis, CycleInfo, ExtractionScore)
- **Configuration POCOs:** Separate classes for JSON deserialization

**Test Organization:**

- **Mirror Core Structure:** Test namespaces mirror Core namespaces exactly
- **Test Utilities Separation:** `TestUtilities/` for fixtures, builders, sample data
- **Sample Solutions:** `tests/.../TestData/SampleSolutions/` for integration tests
- **Test Naming:** `{ClassName}Tests.cs` with `{MethodName}_{Scenario}_{ExpectedResult}` methods

**Output Organization:**

User specifies output directory via `--output ./path/to/output`. Tool generates:

```
output-directory/
├── {SolutionName}-dependencies.dot         # Graphviz source
├── {SolutionName}-dependencies.png         # Rendered graph
├── {SolutionName}-dependencies.svg         # Scalable graph
├── {SolutionName}-analysis-report.txt      # Comprehensive text report
├── {SolutionName}-extraction-scores.csv    # Ranked extraction candidates
├── {SolutionName}-cycle-analysis.csv       # Cycle details
└── {SolutionName}-dependency-matrix.csv    # Full dependency matrix (optional)
```

## Development Workflow Integration

**Development Server Structure:**

.NET CLI tool - no development server. Development workflow:

```bash
# Build solution
dotnet build

# Run CLI locally
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution test.sln --output ./test-output

# Run tests
dotnet test

# Watch mode for TDD
dotnet watch test --project tests/MasDependencyMap.Core.Tests
```

**Build Process Structure:**

```bash
# Restore dependencies
dotnet restore

# Build in Release mode
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Publish framework-dependent executable
dotnet publish src/MasDependencyMap.CLI --configuration Release --output ./publish
```

**Deployment Structure:**

Framework-dependent deployment (MVP):

```
publish/
├── MasDependencyMap.CLI.exe (or masdependencymap on Linux/Mac)
├── MasDependencyMap.Core.dll
├── Microsoft.CodeAnalysis.*.dll
├── QuikGraph.dll
├── Spectre.Console.dll
├── CsvHelper.dll
├── appsettings.json
└── (other dependencies)
```

User runs: `./MasDependencyMap.CLI analyze --solution path/to/solution.sln --output ./analysis`
