# Core Architectural Decisions

## Decision Priority Analysis

**Critical Decisions (Block Implementation):**
- Configuration Management: Microsoft.Extensions.Configuration with JSON support
- Logging & Diagnostics: Microsoft.Extensions.Logging for structured logging
- Error Handling: Strategy pattern with ISolutionLoader fallback chain
- Dependency Injection: Full DI support throughout Core and CLI layers

**Important Decisions (Shape Architecture):**
- CSV Export: CsvHelper library for RFC 4180 compliance
- Graphviz Integration: Direct Process.Start() with IGraphvizRenderer abstraction
- DOT Generation: QuikGraph.Graphviz extension for graph serialization
- Deployment: Framework-dependent for MVP

**Deferred Decisions (Post-MVP):**
- Parallel processing optimization (sequential for MVP)
- .NET global tool packaging (framework-dependent for MVP)
- Plugin architecture implementation (DI foundation established)

## Configuration Management

**Decision:** Microsoft.Extensions.Configuration
**Version:** Microsoft.Extensions.Configuration.Json (latest for .NET 8)
**Rationale:** Provides robust configuration management with JSON file support, environment variable overrides, and command-line argument binding. Supports hierarchical configuration and is fully testable via IConfiguration interface.

**Implementation Approach:**
- JSON configuration files for filter rules (`filter-config.json`)
- JSON configuration files for scoring weights (`scoring-config.json`)
- Command-line arguments can override configuration values
- IConfiguration injected into services via DI
- Validation performed during configuration binding

**Affects Components:**
- Framework Filter Engine (loads blocklist/allowlist patterns)
- Scoring Calculator (loads configurable weights)
- CLI argument handling (merges command-line with config files)

## Logging & Diagnostics

**Decision:** Microsoft.Extensions.Logging with ILogger<T>
**Version:** Microsoft.Extensions.Logging.Console (latest for .NET 8)
**Rationale:** Industry-standard structured logging with multiple log levels (Debug, Info, Warning, Error). Integrates with DI container, fully testable, supports verbose mode requirements.

**Implementation Approach:**
- ILogger<T> injected into all Core components
- Console logging provider for verbose mode (`--verbose` flag)
- Log levels: Error (always), Warning (default), Info (verbose), Debug (verbose)
- Spectre.Console used for user-facing output (progress, tables, formatted reports)
- ILogger used for diagnostic/troubleshooting output

**Affects Components:**
- Solution Loader (logs Roslyn/MSBuild warnings)
- Cycle Detector (logs graph algorithm progress)
- All Core components for debugging

## Error Handling & Resilience

**Decision:** Strategy pattern with fallback chain
**Rationale:** Clean separation of loading strategies, testable via interface mocking, extensible for future loader types. Implements NFR7-NFR13 graceful degradation requirements.

**Implementation Approach:**
```csharp
public interface ISolutionLoader
{
    bool CanLoad(string solutionPath);
    Task<SolutionAnalysis> LoadAsync(string solutionPath);
}
```

**Fallback Chain:**
1. **RoslynSolutionLoader** - Full semantic analysis via MSBuildWorkspace
2. **MSBuildSolutionLoader** - MSBuild-based project reference parsing (if Roslyn fails)
3. **ProjectFileSolutionLoader** - Direct .csproj/.vbproj XML parsing (if MSBuild fails)

**Error Message Strategy:**
- System.CommandLine validation for CLI arguments (automatic)
- Spectre.Console markup for formatted error output
- Clear remediation steps in error messages (NFR14)
- Progress indicators show partial success (e.g., "45/50 projects loaded successfully")

**Affects Components:**
- Solution Loader (primary fallback implementation)
- CLI error presentation
- Progress tracking and reporting

## CSV Export

**Decision:** CsvHelper library
**Version:** CsvHelper (latest stable)
**Rationale:** Guarantees RFC 4180 compliance (NFR34), handles edge cases (commas, quotes, newlines in values), type-safe mapping with POCO classes, Excel/Google Sheets compatible.

**Implementation Approach:**
- POCO classes for export models (ExtractionScoreRecord, CycleAnalysisRecord, DependencyRecord)
- CsvHelper ClassMap for custom column headers
- UTF-8 encoding with BOM for Excel compatibility
- Automatic handling of special characters

**Affects Components:**
- Report Generator & Exporter
- Extraction difficulty scoring output
- Cycle analysis output

## Graphviz Integration

**Decision:** Direct Process.Start() with IGraphvizRenderer abstraction
**Rationale:** Full control over invocation, platform-agnostic, no extra dependencies, testable via interface, clear error messages when Graphviz missing.

**Implementation Approach:**
```csharp
public interface IGraphvizRenderer
{
    bool IsGraphvizInstalled();
    Task<string> RenderToFileAsync(string dotFilePath, GraphvizOutputFormat format);
}
```

**Detection Strategy:**
- Check PATH environment variable for `dot` executable
- Attempt `dot -version` to verify installation
- Clear error message with installation instructions if not found (NFR8)

**Platform Handling:**
- Windows: `dot.exe` via PATH
- Linux/macOS: `dot` via PATH
- Process.Start() works consistently across platforms

**Affects Components:**
- Visualization Generator
- CLI rendering workflow
- Installation validation

## DOT File Generation

**Decision:** QuikGraph.Graphviz extension
**Version:** QuikGraph.Graphviz (if available, or manual DOT serialization)
**Rationale:** Integrated with QuikGraph graph model, handles node/edge escaping, supports custom formatters for colors and labels.

**Implementation Approach:**
- Use QuikGraph's DOT serialization capabilities
- Custom vertex formatter for node colors and labels (scores, names)
- Custom edge formatter for edge colors and labels (dependency types)
- Manual tweaks for legend and graph attributes

**Color Coding:**
- Cycles: RED edges
- Break points: YELLOW edges
- Heat map: GREEN (0-33), YELLOW (34-66), RED (67-100) nodes

**Affects Components:**
- Visualization Generator
- Cycle highlighting
- Extraction difficulty heat maps

## Dependency Injection

**Decision:** Full DI throughout Core and CLI
**Rationale:** Enables comprehensive testability via constructor injection, supports future plugin architecture, follows .NET best practices, aligns with System.CommandLine + Spectre.Console integration pattern.

**Implementation Approach:**
- ServiceCollection setup in CLI Program.cs
- Core components use constructor injection
- Interface-based design (ISolutionLoader, IGraphvizRenderer, etc.)
- ILogger<T> and IConfiguration injected where needed
- Scoped lifetimes for analysis operations

**Service Registration Pattern:**
```csharp
services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
services.AddSingleton<IGraphvizRenderer, GraphvizRenderer>();
services.AddTransient<ISolutionLoader, RoslynSolutionLoader>();
services.AddLogging(builder => builder.AddConsole());
services.AddSingleton<IConfiguration>(configuration);
```

**Affects Components:**
- All Core components (testable via DI)
- CLI command handlers
- Testing infrastructure

## Deployment & Distribution

**Decision:** Framework-dependent deployment (MVP)
**Rationale:** Target users have .NET SDK, smaller file size, faster development iteration, can expand distribution later based on feedback.

**MVP Distribution:**
- Requires .NET 8 SDK on target machine
- Executable size: ~few MB
- Platform: Windows/Linux/macOS

**Future Options:**
- .NET global tool: `dotnet tool install -g masdependencymap`
- Self-contained deployment for broader distribution
- Chocolatey/Homebrew packages

**Affects Components:**
- Build pipeline
- Installation documentation
- User onboarding

## Performance Strategy

**Decision:** Sequential processing (MVP)
**Rationale:** Simpler progress tracking with Spectre.Console, easier debugging of Roslyn/MSBuild issues, avoids threading complications, meets MVP performance targets (NFR1-NFR2).

**Performance Targets:**
- 5 minutes for 50-project solutions (NFR1)
- 30 minutes for 400+ project ecosystems (NFR2)
- <4GB memory footprint (NFR5)

**Future Optimization:**
- Parallel project analysis if performance becomes critical
- Incremental analysis (cache unchanged projects)
- Distributed processing for very large ecosystems

**Affects Components:**
- Solution Loader
- Progress tracking
- Memory management

## Decision Impact Analysis

**Implementation Sequence:**

1. **Foundation (Week 1-2):**
   - Set up DI container in CLI
   - Configure Microsoft.Extensions.Configuration
   - Configure Microsoft.Extensions.Logging
   - Implement IGraphvizRenderer with detection

2. **Core Analysis (Week 3-6):**
   - Implement ISolutionLoader fallback chain
   - Integrate QuikGraph for graph model
   - Implement DOT generation with QuikGraph.Graphviz

3. **Output & Reporting (Week 7-9):**
   - Implement CsvHelper export for scoring/cycles
   - Integrate Graphviz rendering pipeline
   - Implement Spectre.Console progress indicators

4. **Polish (Week 10-12):**
   - Error handling refinement
   - Verbose logging implementation
   - Configuration file validation

**Cross-Component Dependencies:**

- **Configuration → All Components:** Filter rules, scoring weights flow into multiple analyzers
- **Logging → Troubleshooting:** Verbose mode enables diagnostic output across all components
- **DI → Testing:** All components testable via constructor injection
- **Error Handling → User Experience:** Graceful degradation ensures partial results even on failures
- **Graphviz Detection → Installation:** Early validation prevents runtime failures
