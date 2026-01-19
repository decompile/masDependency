# Story 1.2: Install Core NuGet Dependencies

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want all required NuGet packages installed in Core and CLI projects,
so that I have access to Roslyn, QuikGraph, System.CommandLine, Spectre.Console, and other dependencies.

## Acceptance Criteria

**Given** The solution structure is initialized
**When** I install the NuGet packages per Architecture document
**Then** MasDependencyMap.Core includes Microsoft.CodeAnalysis.CSharp.Workspaces, Microsoft.Build.Locator, QuikGraph v2.5.0
**And** MasDependencyMap.CLI includes System.CommandLine v2.0.2, Spectre.Console v0.54.0, Microsoft.Extensions.DependencyInjection
**And** MasDependencyMap.Core.Tests includes FluentAssertions and Moq
**And** The solution builds successfully with all dependencies resolved

## Tasks / Subtasks

- [x] Install Core project dependencies (AC: Core packages)
  - [x] Install Microsoft.CodeAnalysis.CSharp.Workspaces (latest for .NET 8)
  - [x] Install Microsoft.Build.Locator (latest for .NET 8)
  - [x] Install QuikGraph v2.5.0 (EXACT version required)
- [x] Install CLI project dependencies (AC: CLI packages)
  - [x] Install System.CommandLine v2.0.2 (EXACT version required)
  - [x] Install Spectre.Console v0.54.0 (EXACT version required)
  - [x] Install Microsoft.Extensions.DependencyInjection (latest for .NET 8)
  - [x] Install Microsoft.Extensions.Configuration.Json (latest for .NET 8)
  - [x] Install Microsoft.Extensions.Logging.Console (latest for .NET 8)
- [x] Install Test project dependencies (AC: Test packages)
  - [x] Install FluentAssertions (latest stable)
  - [x] Install Moq (latest stable)
- [x] Install CSV export dependency (AC: Core packages)
  - [x] Install CsvHelper (latest stable) to Core project
- [x] Verify build success (AC: All)
  - [x] Run `dotnet restore` from solution root
  - [x] Run `dotnet build` from solution root
  - [x] Confirm zero errors and all dependencies resolved

## Dev Notes

### Critical Implementation Rules

🚨 **MUST READ BEFORE STARTING** - These are non-negotiable requirements from project-context.md:

**Exact Package Versions Required:**
- **QuikGraph:** MUST be v2.5.0 (exact) - Required for .NET Standard 1.3+ compatibility across the 20-year version span
- **System.CommandLine:** MUST be v2.0.2 (exact) - Specific API compatibility requirement
- **Spectre.Console:** MUST be v0.54.0 (exact) - Specific API compatibility requirement
- **All other packages:** Use latest stable for .NET 8.0 (no version lock)

**Why Version Constraints Matter:**
- Tool targets .NET 8.0 BUT analyzes solutions from .NET Framework 3.5 through .NET 8+
- This is a 20-YEAR version span (project-context.md line 251-254)
- QuikGraph v2.5.0 specifically targets .NET Standard 1.3+ for maximum compatibility
- Later versions may break compatibility with older frameworks

**Package Installation Order:**
1. Core project first (foundation dependencies)
2. CLI project second (depends on Core)
3. Test project last (depends on Core)

### Exact Installation Commands

**Source:** [Architecture: starter-template-evaluation.md#Required NuGet Packages]

**MasDependencyMap.Core:**
```bash
cd src/MasDependencyMap.Core

# Roslyn for semantic analysis (latest for .NET 8)
dotnet add package Microsoft.CodeAnalysis.CSharp.Workspaces

# MSBuild integration (latest for .NET 8)
dotnet add package Microsoft.Build.Locator

# Graph algorithms (EXACT version v2.5.0 - CRITICAL!)
dotnet add package QuikGraph --version 2.5.0

# CSV export (latest stable)
dotnet add package CsvHelper
```

**MasDependencyMap.CLI:**
```bash
cd src/MasDependencyMap.CLI

# Command-line parsing (EXACT version v2.0.2)
dotnet add package System.CommandLine --version 2.0.2

# Rich console UI and progress indicators (EXACT version v0.54.0)
dotnet add package Spectre.Console --version 0.54.0

# Dependency injection (latest for .NET 8)
dotnet add package Microsoft.Extensions.DependencyInjection

# Configuration management (latest for .NET 8)
dotnet add package Microsoft.Extensions.Configuration.Json

# Structured logging (latest for .NET 8)
dotnet add package Microsoft.Extensions.Logging.Console
```

**MasDependencyMap.Core.Tests:**
```bash
cd tests/MasDependencyMap.Core.Tests

# xUnit is already included from template
# Add additional testing utilities

# Better assertions (latest stable)
dotnet add package FluentAssertions

# Mocking framework (latest stable)
dotnet add package Moq
```

### Architecture Compliance

**Architectural Decisions:** [Architecture: core-architectural-decisions.md]

**Configuration Management (lines 22-39):**
- Microsoft.Extensions.Configuration.Json for filter-config.json and scoring-config.json
- Enables hierarchical configuration with command-line overrides
- IConfiguration injection pattern for testability

**Logging & Diagnostics (lines 40-57):**
- Microsoft.Extensions.Logging.Console for verbose mode support
- ILogger<T> injection into all Core components
- Log levels: Error (always), Warning (default), Info/Debug (verbose mode)
- Spectre.Console for user-facing output, ILogger for diagnostics

**CSV Export (lines 88-104):**
- CsvHelper library for RFC 4180 compliance (NFR34)
- Handles edge cases: commas, quotes, newlines in CSV values
- UTF-8 with BOM for Excel compatibility
- POCO classes: ExtractionScoreRecord, CycleAnalysisRecord, DependencyRecord

**Dependency Injection (lines 156-181):**
- Full DI throughout Core and CLI layers
- ServiceCollection setup in CLI Program.cs
- Interface-based design for all Core components
- Constructor injection pattern (project-context.md line 101-107)

### Package Purpose & Usage

**Core Project Dependencies:**
1. **Microsoft.CodeAnalysis.CSharp.Workspaces** (Roslyn)
   - Primary solution loader via MSBuildWorkspace
   - Semantic analysis of C# code
   - Used in: RoslynSolutionLoader (Epic 2)
   - CRITICAL: Requires Microsoft.Build.Locator.RegisterDefaults() FIRST (project-context.md line 256-266)

2. **Microsoft.Build.Locator**
   - Locates MSBuild installation for Roslyn integration
   - MUST be called as first line in Program.Main() before any Roslyn types load
   - Failure causes cryptic assembly loading errors

3. **QuikGraph v2.5.0**
   - Graph data structures: AdjacencyGraph<TVertex, TEdge>
   - Tarjan's algorithm: StronglyConnectedComponentsAlgorithm for cycle detection
   - DOT serialization via QuikGraph.Graphviz extension
   - Used in: DependencyGraphBuilder, CycleDetector (Epic 3)

4. **CsvHelper**
   - RFC 4180 compliant CSV export
   - Type-safe mapping with POCO classes
   - Column header customization (Title Case with Spaces)
   - Used in: Report Generator (Epic 5)

**CLI Project Dependencies:**
1. **System.CommandLine v2.0.2**
   - Robust command-line argument parsing
   - Automatic validation and help generation
   - Command/option pattern: `analyze --solution <path> --output <path>`
   - Used in: Program.cs entry point, AnalyzeCommand (Story 1.3)

2. **Spectre.Console v0.54.0**
   - Rich console UI: colors, markup, tables, progress bars
   - IAnsiConsole interface for DI and testing
   - Progress indicators: AnsiConsole.Progress() with TaskDescriptionColumn
   - Error formatting: 3-part structure (Error/Reason/Suggestion)
   - Used in: All user-facing output (Story 1.3+)

3. **Microsoft.Extensions.DependencyInjection**
   - ServiceCollection for DI container
   - Constructor injection pattern
   - Enables testability via interface mocking
   - Used in: Program.cs service registration (Story 1.5)

4. **Microsoft.Extensions.Configuration.Json**
   - JSON configuration file loading
   - Hierarchical configuration support
   - Command-line argument binding and overrides
   - Used in: filter-config.json, scoring-config.json loading (Story 1.4)

5. **Microsoft.Extensions.Logging.Console**
   - Structured logging with ILogger<T>
   - Console provider for verbose mode
   - Named placeholders (NOT string interpolation)
   - Used in: All Core components for diagnostics (Story 1.6)

**Test Project Dependencies:**
1. **xUnit** (already included from template)
   - [Fact] for unit tests
   - [Theory] with [InlineData] for parameterized tests
   - Parallel execution support

2. **FluentAssertions**
   - Better assertion syntax: `result.Should().BeEquivalentTo(expected)`
   - More readable test failures
   - Optional but recommended (project-context.md line 157)

3. **Moq**
   - Mock interface dependencies for unit testing
   - Example: Mock<ISolutionLoader> for testing CLI commands
   - Optional but recommended (project-context.md line 158)

### Dependency Relationships & Build Order

**Project Dependency Chain:**
```
MasDependencyMap.Core (no external project refs)
    ↓
MasDependencyMap.CLI → references → Core
    ↓
MasDependencyMap.Core.Tests → references → Core
```

**NuGet Dependency Resolution:**
- Core packages resolve independently
- CLI packages may pull transitive dependencies from Core
- Test packages include Core's transitive dependencies automatically
- dotnet restore resolves entire dependency graph

**Transitive Dependencies (auto-included):**
- Microsoft.CodeAnalysis.CSharp.Workspaces pulls in:
  - Microsoft.CodeAnalysis.CSharp
  - Microsoft.CodeAnalysis.Workspaces.Common
  - System.Collections.Immutable
- System.CommandLine v2.0.2 is standalone
- Spectre.Console v0.54.0 is standalone

### Testing & Validation Requirements

**Post-Installation Validation:**
1. **Restore Check:**
   - Run `dotnet restore` from solution root
   - Verify no package resolution errors
   - Check all packages downloaded to NuGet cache

2. **Build Check:**
   - Run `dotnet build` from solution root
   - Confirm zero errors, zero warnings (ideal)
   - All three projects build successfully

3. **Version Verification:**
   - Check src/MasDependencyMap.Core/MasDependencyMap.Core.csproj for QuikGraph v2.5.0
   - Check src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj for System.CommandLine v2.0.2 and Spectre.Console v0.54.0
   - Verify no unexpected version upgrades

4. **Dependency Graph Check:**
   - Use `dotnet list package` to view installed packages
   - Verify no security vulnerabilities reported
   - Check for transitive dependency conflicts

**Expected Build Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**Common Issues to Watch For:**
- **QuikGraph version conflict:** Ensure exactly v2.5.0 (project-context.md line 48)
- **System.CommandLine version:** v2.0.2 required (not prerelease versions)
- **Roslyn assembly loading:** Won't manifest until Story 1.3 when Roslyn types are used
- **Package restore failures:** Check NuGet.config if corporate proxy/feed issues

### Previous Story Intelligence

**From Story 1-1 (Completed):**
- Solution structure successfully created with Core/CLI/Tests separation
- All projects target net8.0 exactly (manually adjusted from .NET 9 SDK default)
- Project references correctly configured: CLI → Core, Tests → Core
- Proper .NET .gitignore in place
- Clean build (0 warnings, 0 errors)
- Initial commit created with proper version baseline

**Learnings to Apply:**
- .NET 9 SDK defaults to net9.0, manually changed to net8.0 - watch for this with package versions too
- Git workflow established: stage changes, create meaningful commit
- Code review process validated the structure (claude-sonnet-4-5-20250929)

**Files Created in Story 1-1:**
- masDependencyMap.sln
- src/MasDependencyMap.Core/MasDependencyMap.Core.csproj (will be modified)
- src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj (will be modified)
- tests/MasDependencyMap.Core.Tests/MasDependencyMap.Core.Tests.csproj (will be modified)

**What Changes in This Story:**
- All .csproj files will add PackageReference entries
- NuGet package restore will populate obj/ folders
- No code changes to .cs files yet (happens in Story 1.3+)

### Git Intelligence Summary

**Recent Commit Analysis:**
- **Latest Commit:** `9d92fa3` - Initial commit: .NET 8 solution structure
  - Clean baseline established
  - All projects initialized and building
  - Proper .gitignore in place

**Commit Pattern for This Story:**
After package installation succeeds:
```bash
git add .
git commit -m "Add NuGet dependencies to Core/CLI/Tests projects

- Core: Microsoft.CodeAnalysis.CSharp.Workspaces, Microsoft.Build.Locator, QuikGraph v2.5.0, CsvHelper
- CLI: System.CommandLine v2.0.2, Spectre.Console v0.54.0, Microsoft.Extensions.*
- Tests: FluentAssertions, Moq
- All packages restore and build successfully

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

**Files to Stage:**
- src/MasDependencyMap.Core/MasDependencyMap.Core.csproj (modified)
- src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj (modified)
- tests/MasDependencyMap.Core.Tests/MasDependencyMap.Core.Tests.csproj (modified)

**Files to Ignore:**
- obj/ folders (auto-generated, already in .gitignore)
- bin/ folders (auto-generated, already in .gitignore)

### Project Context Reference

🔬 **Complete project rules:** See `_bmad-output/project-context.md` for comprehensive guidelines on:

**Most Relevant Sections for This Story:**
- **Technology Stack & Versions (lines 17-50):** Complete package list with version constraints
- **Critical Don't-Miss Rules (lines 248-317):**
  - Version Compatibility - 20-year span (lines 250-254)
  - MSBuild Locator - MUST BE FIRST (lines 256-266)
- **Key Version Constraints (lines 46-50):** QuikGraph v2.5.0 requirement explained

**Critical Rules Summary:**
1. QuikGraph MUST be v2.5.0 (line 48)
2. System.CommandLine v2.0.2 (line 29)
3. Spectre.Console v0.54.0 (line 30)
4. MSBuildLocator.RegisterDefaults() FIRST (lines 256-266) - impacts Story 1.3+
5. Target framework: net8.0 for ALL projects (line 48)

### References

- [Architecture: starter-template-evaluation.md#Required NuGet Packages] - Exact installation commands
- [Architecture: core-architectural-decisions.md#Configuration Management] - Why Microsoft.Extensions.Configuration
- [Architecture: core-architectural-decisions.md#Logging & Diagnostics] - Why Microsoft.Extensions.Logging
- [Architecture: core-architectural-decisions.md#CSV Export] - Why CsvHelper
- [Architecture: core-architectural-decisions.md#Dependency Injection] - DI strategy and patterns
- [Epic 1: Project Foundation and Command-Line Interface] - Story context and acceptance criteria
- [Project Context: project-context.md lines 46-50] - Key version constraints
- [Project Context: project-context.md lines 250-254] - Version compatibility requirements (20-year span)
- [Project Context: project-context.md lines 256-266] - MSBuild Locator critical rule
- [Story 1-1: Initialize .NET 8 Solution Structure] - Previous story context and learnings

### Story Completion Status

✅ **Ultimate context engine analysis completed**

**Artifacts Analyzed:**
- Epic 1 full context (8 stories, 126 lines)
- Story 1-1 previous story (246 lines with completion notes)
- Architecture: Starter Template Evaluation (166 lines, NuGet packages section)
- Architecture: Core Architectural Decisions (254 lines, dependency rationale)
- Project Context: Critical Implementation Rules (341 lines)
- Git History: 1 commit analyzed

**Context Provided:**
- ✅ Exact installation commands with package versions
- ✅ Why each package is needed (Roslyn, QuikGraph, System.CommandLine, etc.)
- ✅ Version constraints explained (QuikGraph v2.5.0 for 20-year span compatibility)
- ✅ Installation order and dependency relationships
- ✅ Post-installation validation steps
- ✅ Common issues and troubleshooting guidance
- ✅ Architecture compliance and design patterns
- ✅ Previous story learnings (net8.0 targeting, git workflow)
- ✅ Git commit pattern for completion
- ✅ References to all source documents with line numbers

**Developer Readiness:** 🎯 READY - All information needed for flawless implementation provided

**What Developer Should Do:**
1. Follow exact commands in "Exact Installation Commands" section
2. Run validation checks in "Testing & Validation Requirements" section
3. Create git commit using pattern in "Git Intelligence Summary" section
4. Verify all acceptance criteria satisfied before marking done

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-5-20250929

### Debug Log References

No debug issues encountered. All package installations completed successfully.

### Completion Notes List

✅ **All NuGet packages installed successfully**

**Core Project (MasDependencyMap.Core):**
- Microsoft.CodeAnalysis.CSharp.Workspaces v5.0.0 - Roslyn semantic analysis
- Microsoft.Build.Locator v1.11.2 - MSBuild workspace integration
- QuikGraph v2.5.0 - Graph algorithms (EXACT version as required for .NET Standard 1.3+ compatibility)
- CsvHelper v33.1.0 - RFC 4180 compliant CSV export

**CLI Project (MasDependencyMap.CLI):**
- System.CommandLine v2.0.2 - Command-line parsing (EXACT version as required)
- Spectre.Console v0.54.0 - Rich console UI (EXACT version as required)
- Microsoft.Extensions.DependencyInjection v10.0.2 - DI container
- Microsoft.Extensions.Configuration.Json v10.0.2 - JSON configuration management
- Microsoft.Extensions.Logging.Console v10.0.2 - Structured logging

**Test Project (MasDependencyMap.Core.Tests):**
- FluentAssertions v8.8.0 - Better assertion syntax
- Moq v4.20.72 - Mocking framework

**Build Verification:**
- dotnet restore: All packages resolved successfully
- dotnet build: Build succeeded with 0 warnings, 0 errors
- All three projects (Core, CLI, Tests) compiled successfully

**Critical Version Requirements Met:**
- QuikGraph v2.5.0 (exact) ✅
- System.CommandLine v2.0.2 (exact) ✅
- Spectre.Console v0.54.0 (exact) ✅
- All other packages: Latest stable for .NET 8 ✅

### File List

- src/MasDependencyMap.Core/MasDependencyMap.Core.csproj (modified - added 4 PackageReference entries)
- src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj (modified - added 5 PackageReference entries)
- tests/MasDependencyMap.Core.Tests/MasDependencyMap.Core.Tests.csproj (modified - added 2 PackageReference entries)
