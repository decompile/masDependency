# Starter Template Evaluation

## Primary Technology Domain

**Developer Tooling / Static Analysis CLI** - .NET 8.0 console application with Roslyn integration

## Technical Preferences Established

Based on our discussion:
- **CLI Parsing:** System.CommandLine v2.0.2 for robust argument handling and validation
- **Rich UI:** Spectre.Console v0.54.0 for progress indicators, tables, and colored output
- **Testing:** xUnit for .NET 8+ unit testing
- **Solution Structure:** Multi-project solution with Core library separated from CLI

## Starter Approach: Custom Multi-Project Structure

Since this is a specialized Roslyn-based analysis tool, we'll use a custom solution structure rather than a pre-built template. This gives us precise control over the architecture while following .NET 8 best practices.

**Rationale for Custom Structure:**
- No existing template combines Roslyn + QuikGraph + System.CommandLine + Spectre.Console
- PRD specifies strict separation between Core and CLI (NFR21-NFR22) for future extensibility
- Custom structure allows us to set up testing infrastructure from the start
- Enables proper dependency management and testability boundaries

## Solution Structure

```
masDependencyMap/
├── masDependencyMap.sln
├── src/
│   ├── MasDependencyMap.Core/           # Core analysis engine
│   │   └── MasDependencyMap.Core.csproj
│   └── MasDependencyMap.CLI/            # Command-line interface
│       └── MasDependencyMap.CLI.csproj
└── tests/
    └── MasDependencyMap.Core.Tests/     # Unit tests for Core
        └── MasDependencyMap.Core.Tests.csproj
```

## Initialization Commands

```bash
# Create solution
dotnet new sln -n masDependencyMap

# Create Core library (.NET 8 class library)
dotnet new classlib -n MasDependencyMap.Core -f net8.0 -o src/MasDependencyMap.Core
dotnet sln add src/MasDependencyMap.Core/MasDependencyMap.Core.csproj

# Create CLI console application (.NET 8)
dotnet new console -n MasDependencyMap.CLI -f net8.0 -o src/MasDependencyMap.CLI
dotnet sln add src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj

# Add Core reference to CLI
dotnet add src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj reference src/MasDependencyMap.Core/MasDependencyMap.Core.csproj

# Create xUnit test project (.NET 8)
dotnet new xunit -n MasDependencyMap.Core.Tests -f net8.0 -o tests/MasDependencyMap.Core.Tests
dotnet sln add tests/MasDependencyMap.Core.Tests/MasDependencyMap.Core.Tests.csproj

# Add Core reference to Tests
dotnet add tests/MasDependencyMap.Core.Tests/MasDependencyMap.Core.Tests.csproj reference src/MasDependencyMap.Core/MasDependencyMap.Core.csproj
```

## Required NuGet Packages

**MasDependencyMap.Core:**
```bash
cd src/MasDependencyMap.Core

# Roslyn for semantic analysis
dotnet add package Microsoft.CodeAnalysis.CSharp.Workspaces

# MSBuild integration
dotnet add package Microsoft.Build.Locator

# Graph algorithms
dotnet add package QuikGraph --version 2.5.0
```

**MasDependencyMap.CLI:**
```bash
cd src/MasDependencyMap.CLI

# Command-line parsing
dotnet add package System.CommandLine --version 2.0.2

# Rich console UI and progress indicators
dotnet add package Spectre.Console --version 0.54.0

# Dependency injection (for integration pattern)
dotnet add package Microsoft.Extensions.DependencyInjection
```

**MasDependencyMap.Core.Tests:**
```bash
cd tests/MasDependencyMap.Core.Tests

# Already includes xUnit from template
# Add additional testing utilities as needed
dotnet add package FluentAssertions  # Optional: Better assertions
dotnet add package Moq  # Optional: Mocking framework
```

## Architectural Decisions Provided by This Structure

**Language & Runtime:**
- **.NET 8.0** - Latest LTS release (updated January 13, 2026), supports .NET Standard 2.1
- **C# 12** - Latest language features including top-level statements for CLI entry point
- **Target Framework Moniker (TFM):** net8.0
- **Cross-platform support** - Runs on Windows, Linux, macOS

**CLI Framework Integration Pattern:**
- **System.CommandLine** handles argument parsing, validation, and help generation
- **Spectre.Console** via IAnsiConsole interface for rendering (enables testing)
- **Integration Pattern:** Dependency injection to inject IAnsiConsole into command handlers
- **Testability:** Spectre.Console.Testing package provides TestConsole for unit testing CLI output

**Project Organization:**
- **MasDependencyMap.Core:** Domain logic and analysis algorithms (Roslyn, QuikGraph, scoring)
- **MasDependencyMap.CLI:** Thin presentation layer (command definitions, argument parsing, UI rendering)
- **Separation of Concerns:** Core has zero dependencies on CLI frameworks (enables future web dashboard reuse)
- **Public API Surface:** Core exposes interfaces for solution loading, dependency extraction, cycle detection, scoring

**Testing Infrastructure:**
- **xUnit** - Modern, extensible framework with parallel execution support
- **Test Project Location:** Separate tests/ folder following .NET conventions
- **Testing Capabilities:**
  - [Fact] for unit tests
  - [Theory] with [InlineData] for parameterized tests
  - IClassFixture<T> for shared test context
  - Built-in dependency injection support in test constructors

**Build Tooling:**
- **MSBuild** - Standard .NET build system
- **dotnet CLI** - All build, test, and publish operations
- **Project References** - Compile-time dependency management
- **NuGet Package Management** - Centralized dependency versions

**Development Experience:**
- **Hot Reload** - .NET 8 supports hot reload for console applications during development
- **Top-level statements** - Cleaner Program.cs in CLI project
- **Nullable reference types** - Enabled by default in .NET 8 projects for better null safety
- **Implicit usings** - Reduced boilerplate with global usings
- **File-scoped namespaces** - More concise namespace declarations

**Configuration & Extensibility:**
- JSON configuration files (filter rules, scoring weights) loaded via System.Text.Json
- IConfiguration from Microsoft.Extensions.Configuration for advanced config scenarios
- Dependency injection container in CLI enables plugin architecture in future phases
- Core library designed as reusable component for Phase 4 (web dashboard, VS extension)

**Error Handling Strategy:**
- System.CommandLine validation for command-line arguments (automatic error messages)
- Spectre.Console for formatted error output with colors and markup
- Progress indicators via Spectre.Console.Progress for long-running operations
- Graceful degradation patterns in Core library (Roslyn → MSBuild fallback)

**Output Generation:**
- Graphviz DOT files via QuikGraph.Graphviz extension (if available) or manual generation
- Spectre.Console.Table for formatted text reports
- System.Text.Json for CSV exports (or CSV helper library)
- Process.Start() for invoking external Graphviz for rendering

**Note:** Project initialization using these commands should be the first implementation story in your development plan.
