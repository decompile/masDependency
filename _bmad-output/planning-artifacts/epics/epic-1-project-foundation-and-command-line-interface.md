# Epic 1: Project Foundation and Command-Line Interface

Architects can install the tool, configure it via JSON files, run analysis commands with various options, and access comprehensive documentation to get started quickly.

## Story 1.1: Initialize .NET 8 Solution Structure

As an architect,
I want the masDependencyMap solution initialized with Core/CLI separation,
So that I have a proper foundation for building the analysis tool.

**Acceptance Criteria:**

**Given** I have .NET 8 SDK installed
**When** I execute the project initialization commands
**Then** The solution contains MasDependencyMap.Core (class library), MasDependencyMap.CLI (console app), and MasDependencyMap.Core.Tests (xUnit project)
**And** All project references are correctly configured (CLI → Core, Tests → Core)
**And** The solution builds successfully with `dotnet build`

## Story 1.2: Install Core NuGet Dependencies

As an architect,
I want all required NuGet packages installed in Core and CLI projects,
So that I have access to Roslyn, QuikGraph, System.CommandLine, Spectre.Console, and other dependencies.

**Acceptance Criteria:**

**Given** The solution structure is initialized
**When** I install the NuGet packages per Architecture document
**Then** MasDependencyMap.Core includes Microsoft.CodeAnalysis.CSharp.Workspaces, Microsoft.Build.Locator, QuikGraph v2.5.0
**And** MasDependencyMap.CLI includes System.CommandLine v2.0.2, Spectre.Console v0.54.0, Microsoft.Extensions.DependencyInjection
**And** MasDependencyMap.Core.Tests includes FluentAssertions and Moq
**And** The solution builds successfully with all dependencies resolved

## Story 1.3: Implement Basic CLI with System.CommandLine

As an architect,
I want a working CLI entry point that accepts --help, --version, and analyze command,
So that I can invoke the tool from the command line.

**Acceptance Criteria:**

**Given** The NuGet packages are installed
**When** I run `masdependencymap --help`
**Then** Help documentation is displayed showing available commands and options
**And** When I run `masdependencymap --version`
**Then** The tool version is displayed
**And** When I run `masdependencymap analyze --help`
**Then** The analyze command parameters are shown (--solution, --output, --config, --reports, --format, --verbose)

## Story 1.4: Implement Configuration Management with JSON Support

As an architect,
I want to load configuration from JSON files (filter-config.json, scoring-config.json),
So that I can customize framework filters and scoring weights without code changes.

**Acceptance Criteria:**

**Given** I have created filter-config.json and scoring-config.json in the project root
**When** The tool loads configuration via Microsoft.Extensions.Configuration
**Then** FilterConfiguration POCO is populated from filter-config.json with BlockList and AllowList patterns
**And** ScoringConfiguration POCO is populated from scoring-config.json with weights (Coupling, Complexity, TechDebt, ExternalExposure)
**And** Configuration validation reports specific JSON syntax errors with line numbers if files are malformed
**And** If config files are missing, sensible defaults are used (Microsoft.*/System.* blocked by default)

## Story 1.5: Set Up Dependency Injection Container

As an architect,
I want a DI container in Program.cs that registers all Core services,
So that components use constructor injection for testability.

**Acceptance Criteria:**

**Given** Configuration management is implemented
**When** The CLI application starts
**Then** ServiceCollection is configured with IAnsiConsole, IConfiguration, ILogger<T> registrations
**And** All Core service interfaces are registered (ISolutionLoader, IGraphvizRenderer, IDependencyGraphBuilder, etc.)
**And** Service lifetimes are correctly set (Singleton for stateless, Transient for stateful)
**And** The DI container successfully resolves all dependencies without runtime errors

## Story 1.6: Implement Structured Logging with Microsoft.Extensions.Logging

As an architect,
I want structured logging throughout the application with verbose mode support,
So that I can diagnose issues when running analysis.

**Acceptance Criteria:**

**Given** The DI container is set up
**When** I run the tool with --verbose flag
**Then** Console logging provider outputs Info and Debug level messages with structured placeholders
**And** Without --verbose flag, only Error and Warning level messages are shown
**And** All log messages use named placeholders (e.g., `{SolutionPath}`) not string interpolation
**And** ILogger<T> is injectable into all Core components via constructor injection

## Story 1.7: Create Sample Solution for Testing

As an architect,
I want a sample .NET solution with intentional cycles for testing,
So that I can validate the tool works correctly before running on real codebases.

**Acceptance Criteria:**

**Given** The project is initialized
**When** I create the sample solution in samples/SampleMonolith/
**Then** The sample contains 5-10 projects with at least 2 circular dependency cycles
**And** Sample solution builds successfully with `dotnet build`
**And** Pre-generated expected output exists in samples/sample-output/ (DOT, PNG, TXT, CSV)
**And** README includes instructions for running analysis on the sample solution

## Story 1.8: Create README and User Documentation

As an architect,
I want comprehensive README and user guide documentation,
So that I can get started within 15 minutes.

**Acceptance Criteria:**

**Given** The tool is functional
**When** I read README.md
**Then** Installation instructions are provided (download executable, install Graphviz, verify installation)
**And** Quick start example shows basic usage with sample solution
**And** Time-to-first-graph target is 15 minutes or less
**And** User guide (docs/user-guide.md) includes command-line reference with all parameters
**And** Configuration guide (docs/configuration-guide.md) includes JSON schema examples
**And** Troubleshooting guide (docs/troubleshooting.md) covers MSBuild errors, Graphviz not found
