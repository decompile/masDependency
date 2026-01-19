# Story 1.1: Initialize .NET 8 Solution Structure

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want the masDependencyMap solution initialized with Core/CLI separation,
so that I have a proper foundation for building the analysis tool.

## Acceptance Criteria

**Given** I have .NET 8 SDK installed
**When** I execute the project initialization commands
**Then** The solution contains MasDependencyMap.Core (class library), MasDependencyMap.CLI (console app), and MasDependencyMap.Core.Tests (xUnit project)
**And** All project references are correctly configured (CLI → Core, Tests → Core)
**And** The solution builds successfully with `dotnet build`

## Tasks / Subtasks

- [x] Create solution file and directory structure (AC: All)
  - [x] Create root `masDependencyMap.sln`
  - [x] Create `src/` and `tests/` directories
- [x] Initialize Core class library project (AC: All)
  - [x] Create MasDependencyMap.Core.csproj targeting net8.0
  - [x] Add project to solution
  - [x] Remove default Class1.cs file
- [x] Initialize CLI console application project (AC: All)
  - [x] Create MasDependencyMap.CLI.csproj targeting net8.0
  - [x] Add project to solution
  - [x] Add reference to Core project
- [x] Initialize xUnit test project (AC: All)
  - [x] Create MasDependencyMap.Core.Tests.csproj targeting net8.0
  - [x] Add project to solution
  - [x] Add reference to Core project
- [x] Verify solution builds successfully (AC: All)
  - [x] Run `dotnet build` from solution root
  - [x] Confirm all three projects build without errors

## Dev Notes

### Critical Implementation Rules

🚨 **MUST READ BEFORE STARTING** - These are non-negotiable requirements from project-context.md:

**MSBuild Locator - CRITICAL ORDER:**
- This will matter for future stories but not this one
- When implementing Roslyn integration later: `MSBuildLocator.RegisterDefaults()` MUST be called as the FIRST line in `Program.Main()` before any Roslyn types are loaded
- Failure causes cryptic assembly loading errors

**Project Naming:**
- EXACT casing: `MasDependencyMap.Core`, `MasDependencyMap.CLI`, `MasDependencyMap.Core.Tests`
- Solution file: `masDependencyMap.sln` (lowercase 'm')

**Directory Structure:**
- Use `src/` for source projects, `tests/` for test projects
- This matches .NET conventions and supports future expansion

**Framework Target:**
- ALL projects MUST target `net8.0` exactly
- NO multi-targeting, NO .NET Standard
- This tool analyzes .NET 3.5-8.0 solutions BUT runs on .NET 8.0

**C# Language Features (enabled by default in .NET 8):**
- File-scoped namespaces will be used in all files
- Nullable reference types are enabled
- Implicit usings are enabled
- Top-level statements can be used in CLI Program.cs

### Solution Structure to Create

```
masDependencyMap/
├── masDependencyMap.sln
├── src/
│   ├── MasDependencyMap.Core/
│   │   └── MasDependencyMap.Core.csproj
│   └── MasDependencyMap.CLI/
│       └── MasDependencyMap.CLI.csproj
└── tests/
    └── MasDependencyMap.Core.Tests/
        └── MasDependencyMap.Core.Tests.csproj
```

### Exact Initialization Commands

**Source:** [Architecture: starter-template-evaluation.md#Initialization Commands]

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

### Architecture Compliance

**Architectural Pattern:** [Architecture: project-structure-boundaries.md#Architectural Boundaries]
- **Layered Architecture** with clear separation
- CLI layer depends on Core layer (via interfaces)
- Core layer has NO dependencies on CLI
- Tests mirror Core structure

**Why This Structure:**
- Enables future reuse of Core library (web dashboard, VS extension in Phase 4)
- Supports comprehensive unit testing via interface-based design
- Follows .NET best practices for multi-project solutions
- Supports dependency injection pattern (to be implemented in Story 1.5)

### File Structure Requirements

**Default Template Files to Remove:**
- `src/MasDependencyMap.Core/Class1.cs` - Remove after project creation
- Keep all other files generated by templates (Program.cs, UnitTest1.cs can be renamed/modified later)

**Project References:**
```
MasDependencyMap.CLI → references → MasDependencyMap.Core
MasDependencyMap.Core.Tests → references → MasDependencyMap.Core
```

**Solution File Structure:**
The solution file will organize projects into folders:
- `src` folder containing Core and CLI
- `tests` folder containing Core.Tests

### Testing Requirements

**Validation Steps:**
1. Run `dotnet build` from solution root - must succeed with zero errors
2. Verify `dotnet sln list` shows all three projects
3. Check project references are correct:
   - CLI.csproj should reference Core.csproj
   - Core.Tests.csproj should reference Core.csproj
4. Verify all projects target `net8.0` (check .csproj files)

**Test Framework Setup:**
- xUnit template includes xUnit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk
- FluentAssertions and Moq will be added in Story 1.2 (NuGet dependencies)

### Project Context Reference

🔬 **Complete project rules:** See `_bmad-output/project-context.md` for comprehensive guidelines on:
- Namespace organization (feature-based, NOT layer-based)
- Naming conventions (PascalCase, I-prefix for interfaces)
- Async/await patterns (Async suffix mandatory)
- File-scoped namespaces (C# 10+ feature)
- Exception handling hierarchy
- And 340+ lines of critical implementation rules

**Most Relevant Sections for This Story:**
- Technology Stack & Versions (lines 17-50)
- Language-Specific Rules (.NET C# 12) (lines 54-85)
- Critical Don't-Miss Rules (lines 248-317)

### References

- [Architecture: starter-template-evaluation.md] - Solution structure and initialization commands
- [Architecture: project-structure-boundaries.md] - Complete directory structure and boundaries
- [Architecture: core-architectural-decisions.md] - Dependency injection and deployment decisions
- [Epic 1: Project Foundation and Command-Line Interface] - Epic context and requirements
- [Project Context: project-context.md] - Critical implementation rules (340 lines)

### Story Completion Status

✅ **Ultimate context engine analysis completed**

**Artifacts Analyzed:**
- Epic 1 full context (8 stories)
- Architecture: Starter Template Evaluation (166 lines)
- Architecture: Project Structure & Boundaries (450 lines)
- Architecture: Core Architectural Decisions (254 lines)
- Project Context: Critical Implementation Rules (341 lines)

**Context Provided:**
- ✅ Exact initialization commands with rationale
- ✅ Directory structure specification
- ✅ Architectural boundaries and patterns
- ✅ Critical rules from project context
- ✅ Acceptance criteria mapped to tasks
- ✅ Validation steps for completion
- ✅ References to all source documents with sections

**Developer Readiness:** 🎯 READY - All information needed for flawless implementation provided

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-5-20250929

### Debug Log References

No debugging required - straightforward solution initialization completed successfully on first attempt.

### Completion Notes List

✅ Initialized .NET 8 solution structure with Core/CLI/Tests separation
- Created masDependencyMap.sln with correct naming (lowercase 'm')
- Set up src/ directory with MasDependencyMap.Core and MasDependencyMap.CLI projects
- Set up tests/ directory with MasDependencyMap.Core.Tests project
- All projects target net8.0 exactly (manually adjusted from net9.0 default in .NET 9 SDK)
- Project references correctly configured: CLI → Core, Tests → Core
- Removed default Class1.cs from Core project as specified
- Solution builds successfully: 0 warnings, 0 errors
- All acceptance criteria satisfied

🔍 Code Review Fixes Applied (claude-sonnet-4-5-20250929)
- Fixed HIGH: Deleted "nul" file containing git error output (improper error redirection artifact)
- Fixed HIGH: Replaced .gitignore with proper .NET template (was Node.js template with zero .NET entries)
- Fixed MEDIUM: Added .gitignore to File List (documentation completeness)
- Fixed MEDIUM: Documented need for initial commit (noted below)
- Note: LOW issues (Console.WriteLine in template, empty test) will be naturally resolved in Story 1.2

📋 Recommendation for Next Steps
- Create initial commit to establish version baseline: `git add . && git commit -m "Initial commit: .NET 8 solution structure"`
- All code now clean and ready for Story 1.2 (NuGet packages installation)

### File List

- .gitignore
- masDependencyMap.sln
- src/MasDependencyMap.Core/MasDependencyMap.Core.csproj
- src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj
- src/MasDependencyMap.CLI/Program.cs
- tests/MasDependencyMap.Core.Tests/MasDependencyMap.Core.Tests.csproj
- tests/MasDependencyMap.Core.Tests/UnitTest1.cs
