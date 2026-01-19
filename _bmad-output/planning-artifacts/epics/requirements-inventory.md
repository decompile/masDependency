# Requirements Inventory

## Functional Requirements

**Solution Analysis & Loading:**
- FR1: Architect can load a single .NET solution file (.sln) for dependency analysis
- FR2: Architect can load multiple .NET solution files simultaneously for ecosystem-wide analysis
- FR3: System can parse .NET Framework 3.5+ solution and project files
- FR4: System can parse .NET Core 3.1+ and .NET 5/6/7/8+ solution and project files
- FR5: System can handle mixed C# and VB.NET projects within solutions
- FR6: System can extract project reference dependencies from solution structure
- FR7: System can extract DLL reference dependencies from project files
- FR8: System can differentiate between project references and binary references

**Framework Dependency Filtering:**
- FR9: Architect can configure framework dependency filtering rules via JSON configuration file
- FR10: System can filter Microsoft.* and System.* framework dependencies from analysis by default
- FR11: Architect can define custom blocklist patterns for framework filtering
- FR12: Architect can define custom allowlist patterns to identify organization-specific code
- FR13: System can differentiate between custom code dependencies and framework dependencies in visualizations

**Dependency Visualization:**
- FR14: System can generate dependency graphs in Graphviz DOT format
- FR15: System can render dependency graphs as PNG images
- FR16: System can render dependency graphs as SVG vector graphics
- FR17: Architect can visualize cross-solution dependencies with differentiated colors per solution
- FR18: Architect can view filtered dependency graphs showing only custom code (framework noise removed)
- FR19: System can generate graph visualizations with node labels showing project names

**Cycle Detection & Analysis:**
- FR20: System can identify all circular dependency chains across analyzed solutions using graph algorithms
- FR21: System can highlight circular dependencies in RED on dependency graph visualizations
- FR22: System can calculate cycle statistics (total cycles, cycle sizes, projects involved in cycles)
- FR23: System can analyze coupling strength across dependency edges by counting method calls
- FR24: System can identify weakest coupling edges within circular dependencies
- FR25: System can generate ranked cycle-breaking recommendations based on coupling analysis
- FR26: System can mark suggested cycle break points in YELLOW on dependency visualizations

**Extraction Difficulty Scoring:**
- FR27: System can calculate coupling metrics (incoming and outgoing reference counts) for each project
- FR28: System can calculate cyclomatic complexity metrics for projects using Roslyn semantic analysis
- FR29: System can detect technology version (e.g., .NET 3.5, .NET 6) from project files
- FR30: System can detect external API exposure by scanning for web service attributes and configurations
- FR31: System can combine metrics into 0-100 extraction difficulty scores using configurable weights
- FR32: Architect can configure scoring algorithm weights via JSON configuration
- FR33: System can generate ranked lists of extraction candidates sorted by difficulty score (easiest first)
- FR34: System can color-code dependency graph nodes by extraction difficulty (GREEN 0-33, YELLOW 34-66, RED 67-100)
- FR35: System can display extraction difficulty scores as node labels on dependency graphs

**Report Generation & Export:**
- FR36: System can generate comprehensive text analysis reports with summary statistics
- FR37: System can export dependency data as CSV files for external analysis
- FR38: System can export extraction difficulty scores with supporting metrics as CSV
- FR39: System can export cycle analysis details as CSV
- FR40: System can generate reports showing total projects, total cycles, and cycle participation rates
- FR41: System can generate reports listing top 10 easiest extraction candidates with scores and metrics
- FR42: System can generate reports listing hardest extraction candidates with complexity indicators
- FR43: System can include cycle-breaking recommendations with rationale in text reports

**Command-Line Interface:**
- FR44: Architect can execute analysis via command-line with solution file path parameter
- FR45: Architect can specify output directory for generated files via command-line parameter
- FR46: Architect can specify configuration file path via command-line parameter
- FR47: Architect can select specific report types to generate via command-line parameter
- FR48: Architect can select output formats (DOT, PNG, SVG) via command-line parameter
- FR49: Architect can enable verbose logging via command-line flag
- FR50: Architect can view help documentation via command-line flag
- FR51: Architect can view tool version information via command-line flag

**Configuration & Customization:**
- FR52: Architect can define custom framework filter rules in JSON configuration file
- FR53: Architect can adjust extraction difficulty scoring weights in JSON configuration file
- FR54: Architect can configure visualization color schemes in JSON configuration file
- FR55: Architect can enable/disable cycle highlighting in visualizations via configuration

**Error Handling & Progress Feedback:**
- FR56: System can provide clear error messages when Graphviz is not installed
- FR57: System can provide clear error messages when solution files cannot be loaded
- FR58: System can display progress indicators for long-running analysis operations
- FR59: System can fall back to project reference parsing when Roslyn semantic analysis fails
- FR60: System can handle missing SDK references gracefully without crashing

**Documentation & Examples:**
- FR61: Architect can access README documentation with installation instructions
- FR62: Architect can access command-line reference documentation
- FR63: Architect can access sample solution for testing tool functionality
- FR64: Architect can access troubleshooting guide for common issues
- FR65: Architect can access configuration file documentation with examples

## NonFunctional Requirements

**Performance:**
- NFR1: Solution analysis for a single solution with 50 projects completes within 5 minutes on standard developer workstation
- NFR2: Multi-solution analysis (20 solutions, 400+ projects) completes within 30 minutes
- NFR3: Graph visualization rendering via Graphviz completes within 30 seconds for graphs with 100+ nodes
- NFR4: System provides progress feedback for operations lasting longer than 10 seconds
- NFR5: Memory usage remains under 4GB during analysis of large solution ecosystems (1000+ projects)
- NFR6: CSV export generation completes within 10 seconds regardless of solution size

**Reliability:**
- NFR7: System gracefully handles MSBuild solution loading failures without crashing, providing actionable error messages
- NFR8: System detects missing Graphviz installation and provides clear installation guidance
- NFR9: When Roslyn semantic analysis fails, system falls back to project reference parsing and continues execution
- NFR10: System handles missing or invalid SDK references without terminating analysis
- NFR11: Command-line parsing validates all parameters and provides helpful error messages for invalid inputs
- NFR12: System recovers gracefully from file system access errors (permissions, locked files)
- NFR13: System validates configuration JSON files and reports specific syntax errors with line numbers

**Usability:**
- NFR14: Error messages include specific problem description, root cause, and suggested remediation steps
- NFR15: Installation documentation enables first-time user to generate first graph within 15 minutes
- NFR16: Help documentation (`--help` flag) displays within command-line interface without requiring external browser
- NFR17: Progress indicators show percentage completion and estimated time remaining for long-running operations
- NFR18: Generated reports use clear, non-technical language suitable for stakeholder presentations
- NFR19: Sample solution provided executes successfully on first run with pre-generated expected output for comparison
- NFR20: Command-line parameters follow standard conventions (short and long flags, consistent naming)

**Maintainability:**
- NFR21: Core analysis logic (solution loading, cycle detection, scoring) separated from CLI interface to enable future reuse
- NFR22: Codebase structured into separate assemblies: MasDependencyMap.Core, MasDependencyMap.CLI
- NFR23: Scoring algorithm weights configurable via JSON without code changes
- NFR24: Framework filter rules configurable via JSON without code changes
- NFR25: Code follows standard .NET naming conventions and includes XML documentation comments for public APIs
- NFR26: Unit tests cover core analysis algorithms (cycle detection, scoring calculation, framework filtering)
- NFR27: README includes architecture overview and extension points for future enhancements

**Integration:**
- NFR28: System integrates with Graphviz 2.38+ for DOT file rendering to PNG/SVG formats
- NFR29: System integrates with MSBuild via Microsoft.Build.Locator to resolve SDK-style project references
- NFR30: System integrates with Roslyn (Microsoft.CodeAnalysis.CSharp.Workspaces) for semantic code analysis
- NFR31: System integrates with QuikGraph library for graph algorithm execution (Tarjan's SCC)
- NFR32: System detects Graphviz installation via PATH environment variable or explicit configuration
- NFR33: Generated DOT files compatible with Graphviz 2.38+ specification without manual editing
- NFR34: CSV exports use standard RFC 4180 format for compatibility with Excel, Google Sheets, and data analysis tools

## Additional Requirements

**Starter Template & Project Structure:**
- Custom multi-project solution structure (.NET 8) with strict Core/CLI separation
- Solution structure: `MasDependencyMap.Core` (class library), `MasDependencyMap.CLI` (console app), `MasDependencyMap.Core.Tests` (xUnit tests)
- Project initialization via dotnet CLI commands documented in Architecture
- Feature-based namespace organization (by domain, not by layer)

**Technology Stack Integration:**
- Microsoft.CodeAnalysis.CSharp.Workspaces (Roslyn) for semantic analysis and solution loading
- Microsoft.Build.Locator for MSBuild workspace integration
- QuikGraph v2.5.0 for graph data structures and Tarjan's SCC algorithm
- System.CommandLine v2.0.2 for CLI argument parsing
- Spectre.Console v0.54.0 for rich console UI, progress indicators, and formatted output
- CsvHelper for RFC 4180 compliant CSV export
- Microsoft.Extensions.Configuration (JSON support) for configuration management
- Microsoft.Extensions.Logging for structured logging with ILogger<T>

**Configuration Management:**
- JSON-based configuration files: `filter-config.json` (framework filters) and `scoring-config.json` (scoring weights)
- Microsoft.Extensions.Configuration with command-line argument overrides
- Configuration validation with clear error messages for syntax errors

**Logging & Diagnostics:**
- Microsoft.Extensions.Logging with Console provider
- Structured logging with named placeholders (not string interpolation)
- Log levels: Error (always), Warning (default), Info (verbose mode), Debug (verbose mode)
- Separate ILogger for diagnostics vs. Spectre.Console for user-facing output

**Error Handling Strategy:**
- Custom exception hierarchy per domain area (SolutionLoadException, GraphvizException, ConfigurationException)
- Strategy pattern with fallback chain: RoslynSolutionLoader → MSBuildSolutionLoader → ProjectFileSolutionLoader
- Spectre.Console markup for structured error messages (Error/Reason/Suggestion format)
- Graceful degradation with partial success reporting

**External Tool Integration:**
- Graphviz integration via Process.Start() with IGraphvizRenderer abstraction
- Graphviz detection via PATH environment variable with clear installation guidance (NFR8)
- DOT file generation via QuikGraph.Graphviz extension or manual generation
- Supports Windows (dot.exe), Linux/macOS (dot) via platform-agnostic Process.Start

**Dependency Injection:**
- Full DI throughout Core and CLI layers using Microsoft.Extensions.DependencyInjection
- Interface-based design (ISolutionLoader, IGraphvizRenderer, IDependencyGraphBuilder, etc.)
- ILogger<T> and IConfiguration injected via constructor injection
- ServiceCollection setup in CLI Program.cs

**Deployment & Distribution:**
- Framework-dependent deployment for MVP (requires .NET 8 SDK on target machine)
- Standalone executable published via `dotnet publish`
- Future: .NET global tool (`dotnet tool install -g masdependencymap`), self-contained deployment

**Performance Strategy:**
- Sequential processing for MVP (meets 5-minute/30-minute performance targets)
- QuikGraph optimized graph algorithms for cycle detection
- Memory-efficient processing (<4GB for large solutions)
- Future: Parallel project analysis, incremental analysis with caching

**Testing Infrastructure:**
- xUnit test framework with parallel execution support
- Test organization mirrors Core namespace structure
- Test naming convention: `MethodName_Scenario_ExpectedResult`
- FluentAssertions and Moq for enhanced testing capabilities
- Sample solutions in `tests/.../TestData/SampleSolutions/` for integration tests

**Output File Organization:**
- User-specified output directory via `--output` parameter
- File naming pattern: `{SolutionName}-{OutputType}.{Extension}`
- Generated files: DOT source, PNG/SVG visualizations, text reports, CSV exports
- No timestamps in filenames (user creates timestamped output folders)

**Documentation Requirements:**
- README.md with installation and quick start (15-minute time-to-first-graph)
- Architecture.md documenting all architectural decisions and patterns
- User guide with command-line reference and configuration examples
- Sample solution with pre-generated expected output for validation
- Troubleshooting guide for common issues (MSBuild errors, Graphviz not found)

## FR Coverage Map

**Epic 1: Project Foundation and Command-Line Interface**
- FR44: Execute analysis via CLI with solution file path parameter
- FR45: Specify output directory via CLI parameter
- FR46: Specify configuration file path via CLI parameter
- FR47: Select specific report types via CLI parameter
- FR48: Select output formats (DOT, PNG, SVG) via CLI parameter
- FR49: Enable verbose logging via CLI flag
- FR50: View help documentation via CLI flag
- FR51: View tool version information via CLI flag
- FR52: Define custom framework filter rules in JSON configuration file
- FR53: Adjust extraction difficulty scoring weights in JSON configuration file
- FR54: Configure visualization color schemes in JSON configuration file
- FR55: Enable/disable cycle highlighting in visualizations via configuration
- FR61: Access README documentation with installation instructions
- FR62: Access command-line reference documentation
- FR63: Access sample solution for testing tool functionality
- FR64: Access troubleshooting guide for common issues
- FR65: Access configuration file documentation with examples

**Epic 2: Solution Loading and Dependency Discovery**
- FR1: Load a single .NET solution file (.sln) for dependency analysis
- FR2: Load multiple .NET solution files simultaneously for ecosystem-wide analysis
- FR3: Parse .NET Framework 3.5+ solution and project files
- FR4: Parse .NET Core 3.1+ and .NET 5/6/7/8+ solution and project files
- FR5: Handle mixed C# and VB.NET projects within solutions
- FR6: Extract project reference dependencies from solution structure
- FR7: Extract DLL reference dependencies from project files
- FR8: Differentiate between project references and binary references
- FR9: Configure framework dependency filtering rules via JSON configuration file
- FR10: Filter Microsoft.* and System.* framework dependencies from analysis by default
- FR11: Define custom blocklist patterns for framework filtering
- FR12: Define custom allowlist patterns to identify organization-specific code
- FR13: Differentiate between custom code dependencies and framework dependencies in visualizations
- FR14: Generate dependency graphs in Graphviz DOT format
- FR15: Render dependency graphs as PNG images
- FR16: Render dependency graphs as SVG vector graphics
- FR17: Visualize cross-solution dependencies with differentiated colors per solution
- FR18: View filtered dependency graphs showing only custom code (framework noise removed)
- FR19: Generate graph visualizations with node labels showing project names

**Epic 3: Circular Dependency Detection and Break-Point Analysis**
- FR20: Identify all circular dependency chains across analyzed solutions using graph algorithms
- FR21: Highlight circular dependencies in RED on dependency graph visualizations
- FR22: Calculate cycle statistics (total cycles, cycle sizes, projects involved in cycles)
- FR23: Analyze coupling strength across dependency edges by counting method calls
- FR24: Identify weakest coupling edges within circular dependencies
- FR25: Generate ranked cycle-breaking recommendations based on coupling analysis
- FR26: Mark suggested cycle break points in YELLOW on dependency visualizations

**Epic 4: Extraction Difficulty Scoring and Candidate Ranking**
- FR27: Calculate coupling metrics (incoming and outgoing reference counts) for each project
- FR28: Calculate cyclomatic complexity metrics for projects using Roslyn semantic analysis
- FR29: Detect technology version (e.g., .NET 3.5, .NET 6) from project files
- FR30: Detect external API exposure by scanning for web service attributes and configurations
- FR31: Combine metrics into 0-100 extraction difficulty scores using configurable weights
- FR32: Configure scoring algorithm weights via JSON configuration
- FR33: Generate ranked lists of extraction candidates sorted by difficulty score (easiest first)
- FR34: Color-code dependency graph nodes by extraction difficulty (GREEN 0-33, YELLOW 34-66, RED 67-100)
- FR35: Display extraction difficulty scores as node labels on dependency graphs

**Epic 5: Comprehensive Reporting and Data Export**
- FR36: Generate comprehensive text analysis reports with summary statistics
- FR37: Export dependency data as CSV files for external analysis
- FR38: Export extraction difficulty scores with supporting metrics as CSV
- FR39: Export cycle analysis details as CSV
- FR40: Generate reports showing total projects, total cycles, and cycle participation rates
- FR41: Generate reports listing top 10 easiest extraction candidates with scores and metrics
- FR42: Generate reports listing hardest extraction candidates with complexity indicators
- FR43: Include cycle-breaking recommendations with rationale in text reports

**Epic 6: Robust Error Handling and Progress Feedback**
- FR56: Provide clear error messages when Graphviz is not installed
- FR57: Provide clear error messages when solution files cannot be loaded
- FR58: Display progress indicators for long-running analysis operations
- FR59: Fall back to project reference parsing when Roslyn semantic analysis fails
- FR60: Handle missing SDK references gracefully without crashing
