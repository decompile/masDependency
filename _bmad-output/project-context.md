---
project_name: 'masDependencyMap'
user_name: 'Yaniv'
date: '2026-01-18'
status: 'complete'
sections_completed: ['technology_stack', 'language_specific', 'framework_specific', 'testing', 'code_quality', 'development_workflow', 'critical_rules', 'usage_guidelines']
existing_patterns_found: 14
optimized_for_llm: true
total_lines: 341
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

**Core Technologies:**
- .NET 8.0 (target framework: net8.0)
- C# 12 (latest language features)
- Microsoft.CodeAnalysis.CSharp.Workspaces (Roslyn) - Semantic analysis
- Microsoft.Build.Locator - MSBuild workspace integration
- QuikGraph v2.5.0 - Graph data structures and algorithms

**CLI & UI:**
- System.CommandLine v2.0.2 - Command-line parsing and validation
- Spectre.Console v0.54.0 - Rich console UI and progress indicators

**Infrastructure:**
- Microsoft.Extensions.DependencyInjection - DI container
- Microsoft.Extensions.Configuration.Json - JSON configuration management
- Microsoft.Extensions.Logging.Console - Structured logging

**Utilities:**
- CsvHelper (latest stable) - RFC 4180 compliant CSV export

**External Tools:**
- Graphviz 2.38+ (external process, must be in PATH)

**Testing:**
- xUnit (latest for .NET 8)
- FluentAssertions (optional)
- Moq (optional)

**Key Version Constraints:**
- Tool must target .NET 8.0 (not .NET Standard)
- Analysis targets: .NET Framework 3.5+ through .NET 8+ (20-year version span)
- QuikGraph must be v2.5.0 for .NET Standard 1.3+ compatibility

## Critical Implementation Rules

### Language-Specific Rules (.NET C# 12)

**Namespace Organization:**
- MUST use feature-based namespaces, NOT layer-based
- Pattern: `MasDependencyMap.Core.{Feature}` (e.g., `MasDependencyMap.Core.SolutionLoading`)
- NEVER use layer-based like `MasDependencyMap.Core.Services` or `MasDependencyMap.Core.Models`

**Interface & Class Naming:**
- ALWAYS use I-prefix for interfaces (e.g., `ISolutionLoader`)
- Use descriptive implementation names (e.g., `RoslynSolutionLoader`, not `SolutionLoaderImpl`)
- File names MUST match class names exactly (e.g., `RoslynSolutionLoader.cs`)

**Async/Await Patterns:**
- ALWAYS use `Async` suffix for async methods, even when no sync version exists
- Example: `Task<SolutionAnalysis> LoadAsync(string path)` not `Task<SolutionAnalysis> Load(string path)`
- All I/O operations (file access, Roslyn analysis, external process calls) MUST be async

**Nullable Reference Types:**
- Nullable reference types are ENABLED by default in .NET 8 projects
- Use `?` for nullable reference types explicitly
- Avoid `!` null-forgiving operator unless absolutely necessary

**File-Scoped Namespaces:**
- Use file-scoped namespace declarations (C# 10+)
- Example: `namespace MasDependencyMap.Core.SolutionLoading;` not `namespace MasDependencyMap.Core.SolutionLoading { }`

**Exception Handling:**
- Use custom exception hierarchy for domain errors
- Base exceptions per domain: `SolutionLoadException`, `GraphvizException`, `ConfigurationException`
- Specific exceptions: `RoslynLoadException : SolutionLoadException`
- NEVER use generic `Exception` or `InvalidOperationException` when domain-specific exception exists

### Framework-Specific Rules

**System.CommandLine (v2.0.2):**
- Use `AnalyzeCommand` class with `AnalyzeCommandOptions` for argument definitions
- ALWAYS validate arguments using System.CommandLine built-in validation
- Use Spectre.Console for ALL user output, NOT Console.WriteLine
- Pattern: System.CommandLine parses, Spectre.Console renders

**Spectre.Console (v0.54.0):**
- Inject `IAnsiConsole` via DI, NOT direct `AnsiConsole.Console` usage (enables testing)
- User-facing errors MUST use 3-part structure: `[red]Error:[/]`, `[dim]Reason:[/]`, `[dim]Suggestion:[/]`
- Progress indicators: Use `AnsiConsole.Progress()` with `TaskDescriptionColumn`, `ProgressBarColumn`, `PercentageColumn`
- Tables: Use `Spectre.Console.Table` for formatted reports
- NEVER use plain `Console.WriteLine` for user output

**Microsoft.Extensions.DependencyInjection:**
- Full DI throughout Core and CLI layers
- Register services in CLI `Program.cs` using `ServiceCollection`
- Core components MUST use constructor injection, NOT service locator pattern
- Lifetime patterns: Singletons for stateless services, Transient for per-operation services
- Strategy pattern: Register fallback chain as separate implementations

**Microsoft.Extensions.Configuration:**
- JSON files MUST use PascalCase property names (matches C# POCO properties)
- Load configuration files: `filter-config.json`, `scoring-config.json` from current directory by default
- Command-line arguments can override: `--config path/to/config.json`
- Use `IConfiguration` injection, NOT direct `JsonSerializer.Deserialize<T>()`

**Microsoft.Extensions.Logging:**
- Use structured logging with named placeholders: `_logger.LogInformation("Loading {SolutionPath}", path)`
- NEVER use string interpolation in log messages: `_logger.LogInformation($"Loading {path}")` is WRONG
- Log levels: Error (unrecoverable), Warning (fallback triggered), Information (verbose only), Debug (verbose only)
- Inject `ILogger<T>` where T is the class using the logger

**Roslyn (Microsoft.CodeAnalysis):**
- Use `MSBuildWorkspace` for solution loading with `Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults()`
- Semantic analysis via `Document.GetSemanticModelAsync()`
- Fallback chain: Try Roslyn first, catch `RoslynLoadException`, fall back to MSBuild project references
- ALWAYS dispose workspaces when done

**QuikGraph (v2.5.0):**
- Use `AdjacencyGraph<TVertex, TEdge>` for dependency graphs
- Cycle detection via Tarjan's algorithm: `StronglyConnectedComponentsAlgorithm`
- Vertices: `ProjectNode`, Edges: `DependencyEdge`
- QuikGraph.Graphviz extension for DOT serialization (if available)

**CsvHelper:**
- Use `CsvWriter` with `CultureInfo.InvariantCulture`
- Column headers MUST be Title Case with Spaces: `"Project Name"`, `"Extraction Score"`
- UTF-8 encoding with BOM for Excel compatibility
- Create POCO classes for export: `ExtractionScoreRecord`, `CycleAnalysisRecord`

### Testing Rules

**Test Organization:**
- Tests MUST mirror Core namespace structure in separate `tests/` folder
- Pattern: `tests/MasDependencyMap.Core.Tests/{Feature}/{ClassNameTests.cs}`
- Example: `tests/MasDependencyMap.Core.Tests/SolutionLoading/RoslynSolutionLoaderTests.cs`
- NEVER use flat structure or co-located test files

**Test Class Naming:**
- Use `Tests` suffix (plural): `RoslynSolutionLoaderTests`
- One test class per production class

**Test Method Naming:**
- ALWAYS use pattern: `{MethodName}_{Scenario}_{ExpectedResult}`
- Examples: `LoadAsync_ValidSolutionPath_ReturnsAnalysis()`, `LoadAsync_InvalidPath_ThrowsException()`
- NEVER use BDD-style naming: `Should_return_analysis_when_path_is_valid()` is WRONG

**Test Frameworks:**
- xUnit as primary test framework
- FluentAssertions for assertions (optional but recommended)
- Moq for mocking dependencies (optional)
- Arrange-Act-Assert pattern consistently

### Code Quality & Style Rules

**Naming Conventions:**
- File names MUST match class names exactly (e.g., `RoslynSolutionLoader.cs`)
- Use PascalCase for all file names, classes, interfaces, methods, properties
- JSON configuration MUST use PascalCase property names (matches C# POCO properties)
- CSV exports MUST use Title Case with Spaces for headers (e.g., `"Project Name"`, `"Extraction Score"`)
- Private fields use `_camelCase` with underscore prefix

**Documentation Standards:**
- XML documentation comments REQUIRED for public APIs
- Include `<summary>`, `<param>`, `<returns>`, `<exception>` tags
- Explain WHY not WHAT - focus on intent and business context
- Document exceptions that may be thrown
- Example:
  ```csharp
  /// <summary>
  /// Loads a solution using Roslyn semantic analysis.
  /// Falls back to MSBuild if Roslyn fails.
  /// </summary>
  /// <param name="solutionPath">Absolute path to .sln file</param>
  /// <returns>Complete solution analysis with project dependencies</returns>
  /// <exception cref="RoslynLoadException">When solution cannot be loaded</exception>
  ```

**Error Messages:**
- Spectre.Console errors MUST use 3-part structure:
  - `[red]Error:[/]` - What failed
  - `[dim]Reason:[/]` - Why it failed
  - `[dim]Suggestion:[/]` - How to fix it
- Example: `"[red]Error:[/] Solution not found\n[dim]Reason:[/] File does not exist at path\n[dim]Suggestion:[/] Verify the path and try again"`

**Code Organization:**
- One class per file (except nested types)
- Group members by type: fields, constructors, properties, methods
- Private methods at bottom of class
- Use regions sparingly, only for generated code

**Constants and Magic Values:**
- NEVER use magic numbers or strings in logic
- Define constants at class level with descriptive names
- Example: `private const int DefaultMaxDepth = 10;` not `if (depth > 10)`

### Development Workflow Rules

**Fallback Strategy Pattern:**
- ALWAYS implement 3-layer fallback chain for solution loading:
  1. Primary: Roslyn semantic analysis (`RoslynSolutionLoader`)
  2. Secondary: MSBuild project references (`MSBuildSolutionLoader`)
  3. Tertiary: Manual `.csproj` XML parsing (`ProjectFileSolutionLoader`)
- Pattern: Try primary, catch specific exception, log warning, try secondary
- Log fallback transitions: `_logger.LogWarning("Roslyn failed, falling back to MSBuild: {Reason}", ex.Message)`
- NEVER fail silently - each fallback MUST log the reason

**Dependency Injection Registration:**
- Register fallback chain in reverse order (tertiary → primary)
- Use `TryAdd` pattern to allow test overrides
- Example sequence:
  ```csharp
  services.AddSingleton<ProjectFileSolutionLoader>();
  services.AddSingleton<MSBuildSolutionLoader>();
  services.AddSingleton<RoslynSolutionLoader>();
  services.AddSingleton<ISolutionLoader, RoslynSolutionLoader>();
  ```

**Configuration Loading Order:**
- Default configuration files from current directory
- Command-line arguments override defaults
- Validate configuration after loading, fail fast with clear error messages
- NEVER proceed with invalid configuration

**External Process Management:**
- Graphviz execution MUST check PATH availability first
- Validate external tool exists before running analysis
- Provide clear error message if tool missing: include download URL in suggestion
- Set reasonable timeouts for external processes (default: 30 seconds)

**Resource Disposal:**
- ALWAYS dispose `MSBuildWorkspace` after use
- Use `using` statements or `using` declarations for disposables
- NEVER leave workspaces open - can lock files and consume memory

**Progress Reporting:**
- Use Spectre.Console `Progress()` for long-running operations
- Update progress for: Loading solution, Analyzing projects, Building graph, Detecting cycles, Generating output
- Granularity: Per-project for analysis, per-file for export

### Critical Don't-Miss Rules

**🚨 Version Compatibility - CRITICAL:**
- Tool targets .NET 8.0 BUT analyzes solutions from .NET Framework 3.5 through .NET 8+
- This is a 20-YEAR version span - NEVER assume modern framework features in analyzed code
- Roslyn MUST handle ancient project formats (.csproj, packages.config, app.config)
- NEVER use APIs that don't exist in .NET Framework 3.5 when parsing old projects

**🚨 MSBuild Locator - MUST BE FIRST:**
- `MSBuildLocator.RegisterDefaults()` MUST be called BEFORE any Roslyn types are loaded
- Call it as first line in `Program.Main()` before DI container setup
- Failure to do this causes cryptic assembly loading errors
- Example:
  ```csharp
  public static async Task<int> Main(string[] args)
  {
      MSBuildLocator.RegisterDefaults(); // FIRST LINE
      // ... rest of setup
  }
  ```

**🚨 Circular Dependency Detection:**
- Use Tarjan's algorithm via `StronglyConnectedComponentsAlgorithm<TVertex, TEdge>`
- NEVER implement custom cycle detection - QuikGraph's implementation is proven
- A strongly connected component with >1 vertex IS a circular dependency
- Report ALL projects in the cycle, not just the first one found

**🚨 Path Handling:**
- ALWAYS use absolute paths internally
- Convert relative paths from user input to absolute IMMEDIATELY
- Use `Path.GetFullPath()` to normalize paths
- NEVER assume forward slashes - use `Path.Combine()` for cross-platform compatibility

**🚨 Graphviz Integration:**
- Graphviz is EXTERNAL - check it exists before running analysis
- Use `Process.Start()` with timeout (default 30 seconds)
- Capture both stdout AND stderr for error messages
- If Graphviz not found, provide download URL: https://graphviz.org/download/
- NEVER bundle Graphviz - it's an external dependency

**🚨 Console Output Discipline:**
- NEVER EVER use `Console.WriteLine()` for user-facing output
- ALWAYS use `IAnsiConsole` injected via DI
- Reason: Enables testing and consistent formatting
- Only exception: Program.Main() error handling before DI is available

**🚨 Async All The Way:**
- ALL I/O operations MUST be async (file, Roslyn, process execution)
- NEVER use `.Result` or `.Wait()` - causes deadlocks
- Use `ConfigureAwait(false)` in library code (Core layer)
- Main method signature: `static async Task<int> Main(string[] args)`

**🚨 Exception Context:**
- ALWAYS include context in custom exceptions
- Include file paths, project names, specific errors from inner exceptions
- Example: `throw new RoslynLoadException($"Failed to load solution at {path}", ex);`
- NEVER throw generic exceptions with just "Failed" messages

**🚨 Configuration Validation:**
- Validate ALL configuration values immediately after loading
- Check: paths exist, scores are in valid ranges, formats are valid
- Fail fast with specific error message: which config field, why invalid, what's valid
- NEVER proceed with invalid config - unpredictable behavior

**🚨 Memory Management:**
- Dispose `MSBuildWorkspace` ALWAYS - can hold gigabytes for large solutions
- Use `using` statements for all `IDisposable` resources
- Roslyn semantic models are HEAVY - don't cache unnecessarily
- Process projects sequentially by default to control memory usage

---

## Usage Guidelines

**For AI Agents:**

- Read this file BEFORE implementing any code for this project
- Follow ALL rules exactly as documented - no exceptions
- When in doubt, prefer the more restrictive option
- If you discover new patterns during implementation, suggest updates to this file

**For Humans:**

- Keep this file lean and focused on what AI agents need to know
- Update when technology stack changes or new patterns emerge
- Review quarterly to remove rules that have become obvious
- Remove redundant content to optimize LLM context usage

**Maintenance:**

- This file should stay under 500 lines for optimal LLM context efficiency
- Focus on UNOBVIOUS details that agents commonly miss
- Remove rules when they become obvious or standard practice
- Add rules when you notice agents making consistent mistakes

**Last Updated:** 2026-01-18
