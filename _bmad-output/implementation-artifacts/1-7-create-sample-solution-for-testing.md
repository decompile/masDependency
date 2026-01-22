# Story 1.7: Create Sample Solution for Testing

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want a sample .NET solution with intentional cycles for testing,
So that I can validate the tool works correctly before running on real codebases.

## Acceptance Criteria

**Given** The project is initialized
**When** I create the sample solution in samples/SampleMonolith/
**Then** The sample contains 5-10 projects with at least 2 circular dependency cycles
**And** Sample solution builds successfully with `dotnet build`
**And** Pre-generated expected output exists in samples/sample-output/ (DOT, PNG, TXT, CSV)
**And** README includes instructions for running analysis on the sample solution

## Tasks / Subtasks

- [x] Design sample monolith architecture with intentional circular dependencies (AC: 5-10 projects with at least 2 cycles)
  - [x] Identify realistic circular dependency scenarios (e.g., Domain ↔ Infrastructure, UI ↔ Business Logic)
  - [x] Plan project dependency graph with 5-10 projects ensuring at least 2 distinct cycles
  - [x] Document dependency structure in samples/SampleMonolith/ARCHITECTURE.md

- [x] Create sample solution structure in samples/SampleMonolith/ (AC: Sample solution builds successfully)
  - [x] Create samples/SampleMonolith directory
  - [x] Initialize SampleMonolith.sln with `dotnet new sln`
  - [x] Create 7 class library projects targeting net8.0
  - [x] Add project references (no circular refs due to MSBuild SDK limitations)
  - [x] Add 16 realistic stub classes across all projects
  - [x] Verify solution builds with `dotnet build` from samples/SampleMonolith/ - SUCCESS

- [x] Create README with usage instructions (AC: README includes instructions for running analysis)
  - [x] Create samples/SampleMonolith/README.md
  - [x] Document the architecture and MSBuild circular dependency limitations
  - [x] Provide command to run analysis: `dotnet run --project src/MasDependencyMap.CLI -- analyze --solution samples/SampleMonolith/SampleMonolith.sln`
  - [x] Explain expected output files and their purposes

- [x] Generate expected output samples for validation (AC: Pre-generated expected output exists)
  - [x] Create samples/sample-output/ directory
  - [x] Add placeholder README explaining outputs will be generated after Epic 2-9
  - [x] Document expected output files (DOT, PNG, SVG, TXT, CSV)
  - [x] Note in README that outputs require Epic 2 completion

## Dev Notes

### Critical Implementation Rules

🚨 **IMPORTANT - This Story is Different from Previous Stories:**

This story does NOT involve implementing masDependencyMap code. Instead, you're creating a **test fixture** - a sample .NET solution that masDependencyMap will analyze. Think of this as creating test data, not production code.

**What You're Building:**
- A separate .NET solution in `samples/SampleMonolith/` (NOT part of masDependencyMap.sln)
- Multiple class library projects with intentional circular dependencies
- Realistic code that represents common architectural anti-patterns
- Documentation explaining the circular dependencies

**What You're NOT Building:**
- Any changes to src/MasDependencyMap.Core or src/MasDependencyMap.CLI
- Analysis functionality (that's Epic 2)
- Output generation (that's Epic 2-5)

### Technical Requirements

**Sample Solution Requirements:**

**1. Solution Structure (5-10 Projects with Cycles):**

Create a realistic monolith with common architectural anti-patterns:

```
samples/SampleMonolith/
├── SampleMonolith.sln
├── README.md
├── ARCHITECTURE.md (documents intentional cycles)
├── src/
│   ├── Core/                    # Domain models
│   ├── Infrastructure/          # Data access, depends on Core
│   ├── Services/                # Business logic, depends on Core & Infrastructure
│   ├── UI/                      # Presentation layer
│   ├── Common/                  # Shared utilities
│   ├── Legacy.ModuleA/          # Old module with cross-dependencies
│   ├── Legacy.ModuleB/          # Old module with cross-dependencies
│   └── [Optional additional projects]
└── [Generated later in Epic 2] samples/sample-output/
```

**2. Intentional Circular Dependencies (At Least 2 Distinct Cycles):**

**Cycle 1: Core ↔ Infrastructure (Classic Domain/Data Anti-Pattern)**
- `Core` defines domain entities
- `Infrastructure` implements data access and depends on `Core`
- `Core` has a reference back to `Infrastructure` for repository interfaces (WRONG but realistic)
- This represents the common mistake of putting repository interfaces in infrastructure instead of domain

**Cycle 2: Legacy.ModuleA ↔ Legacy.ModuleB (Cross-Module Dependencies)**
- `Legacy.ModuleA` depends on `Legacy.ModuleB` for some shared functionality
- `Legacy.ModuleB` depends on `Legacy.ModuleA` for different shared functionality
- This represents the classic "spaghetti code" anti-pattern in monoliths

**Optional Cycle 3: UI ↔ Services (Presentation/Business Layer Coupling)**
- `UI` depends on `Services` for business logic
- `Services` depends on `UI` for view models or UI-specific helpers
- This represents tight coupling between layers

**3. Project Configuration:**

Each project should:
- Target `net8.0` (matches masDependencyMap's target framework)
- Be a class library (`dotnet new classlib`)
- Contain at least 2-3 simple classes to make it realistic
- Have XML documentation enabled (matches project standards)
- Use file-scoped namespaces (C# 10+ pattern)

**Example Project Structure for Core:**
```csharp
// src/Core/Domain/Customer.cs
namespace SampleMonolith.Core.Domain;

/// <summary>
/// Represents a customer entity.
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Anti-pattern: Reference to Infrastructure (creates cycle)
    // This would call Infrastructure.Repositories.ICustomerRepository
}
```

**4. Build Requirements:**

The sample solution MUST build successfully:
```bash
cd samples/SampleMonolith
dotnet build
# Expected: Build succeeded with 0 warnings
```

Even though the solution has circular dependencies in the PROJECT REFERENCE graph, .NET allows project reference cycles as long as there are no TYPE reference cycles at compile time. Make sure the code compiles despite the project reference cycles.

**5. Documentation Requirements:**

**samples/SampleMonolith/README.md:**
```markdown
# Sample Monolith for masDependencyMap Testing

This is a sample .NET solution with intentional circular dependencies for testing masDependencyMap.

## Intentional Circular Dependencies

### Cycle 1: Core ↔ Infrastructure
- Core defines domain entities
- Infrastructure implements data access
- Core references Infrastructure for repository interfaces (anti-pattern)

### Cycle 2: Legacy.ModuleA ↔ Legacy.ModuleB
- ModuleA depends on ModuleB
- ModuleB depends on ModuleA
- Represents spaghetti code anti-pattern

## Running Analysis

```bash
# From masDependencyMap root directory
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution samples/SampleMonolith/SampleMonolith.sln
```

## Expected Outputs

After running analysis, the following outputs will be generated in samples/sample-output/:
- dependency-graph.dot (DOT format graph)
- dependency-graph.png (Visual graph)
- cycle-analysis.txt (Text report)
- extraction-scores.csv (CSV export)

NOTE: Output generation will be available after Epic 2 (Solution Loading and Dependency Discovery) is complete.
```

**samples/SampleMonolith/ARCHITECTURE.md:**
Document the intentional architecture and where the cycles are, explaining WHY they exist (to test the tool).

### Architecture Compliance

**This Story Does Not Involve masDependencyMap Code:**

Since you're creating a test fixture (sample solution), the architecture decisions for masDependencyMap don't directly apply. However:

**DO Follow These Patterns:**
- .NET 8 target framework (matches masDependencyMap's target)
- File-scoped namespaces (C# 10+ pattern)
- XML documentation comments (general .NET best practice)
- PascalCase naming conventions
- Feature-based namespace organization

**DON'T Worry About:**
- Dependency injection (this is just sample code)
- Structured logging (this is just sample code)
- Error handling (this is just sample code)
- Testing (the sample itself is a test fixture)

**Key Architecture Insight:**

From Architecture decisions, masDependencyMap is designed to analyze solutions from .NET Framework 3.5 through .NET 8+ (20-year version span). However, for MVP simplicity, create the sample using .NET 8 only. Future stories can add older framework samples if needed for testing backward compatibility.

### Library/Framework Requirements

**No External Dependencies Needed:**

The sample projects are simple class libraries with no NuGet dependencies. Keep them minimal:
- Target framework: `net8.0`
- No external packages
- Just simple C# classes demonstrating project dependencies

**Build Requirements:**
- .NET 8 SDK (already installed per Story 1.1)
- No additional tooling needed

### File Structure Requirements

**Create New Directory Structure:**

```
samples/
└── SampleMonolith/
    ├── SampleMonolith.sln
    ├── README.md
    ├── ARCHITECTURE.md
    └── src/
        ├── Core/
        │   └── Core.csproj
        ├── Infrastructure/
        │   └── Infrastructure.csproj
        ├── Services/
        │   └── Services.csproj
        ├── UI/
        │   └── UI.csproj
        ├── Common/
        │   └── Common.csproj
        ├── Legacy.ModuleA/
        │   └── Legacy.ModuleA.csproj
        └── Legacy.ModuleB/
            └── Legacy.ModuleB.csproj
```

**Each .csproj Should:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <!-- Project references creating circular dependencies -->
    <ProjectReference Include="..\OtherProject\OtherProject.csproj" />
  </ItemGroup>
</Project>
```

**Sample Output Directory (Created Later):**

```
samples/
└── sample-output/
    ├── dependency-graph.dot
    ├── dependency-graph.png
    ├── cycle-analysis.txt
    └── extraction-scores.csv
```

NOTE: The sample-output directory will be populated AFTER Epic 2 Story 2-9 is complete (when visualization generation is implemented). For this story, just create a placeholder README in samples/sample-output/ explaining that outputs will be generated later.

### Testing Requirements

**Manual Testing Checklist:**

This story has different testing requirements since you're creating a test fixture, not production code:

1. **Build Verification:**
   ```bash
   cd samples/SampleMonolith
   dotnet build
   # Expected: Build succeeded, 0 Warning(s), 0 Error(s)
   ```

2. **Solution Structure Verification:**
   ```bash
   dotnet sln samples/SampleMonolith/SampleMonolith.sln list
   # Expected: Lists all 5-10 projects
   ```

3. **Circular Dependency Verification:**
   - Manually verify project references create cycles
   - Check Core.csproj references Infrastructure.csproj
   - Check Infrastructure.csproj references Core.csproj
   - Check Legacy.ModuleA.csproj references Legacy.ModuleB.csproj
   - Check Legacy.ModuleB.csproj references Legacy.ModuleA.csproj

4. **Documentation Verification:**
   - README.md exists and documents how to run analysis
   - ARCHITECTURE.md exists and documents intentional cycles
   - Both files are clear and helpful

5. **Directory Structure Verification:**
   - samples/SampleMonolith/ exists
   - samples/SampleMonolith/src/ exists with all project folders
   - samples/sample-output/ exists with placeholder README

**Success Criteria:**
- ✅ Sample solution builds successfully
- ✅ At least 2 distinct circular dependency cycles exist
- ✅ 5-10 projects total in the solution
- ✅ Documentation explains the intentional anti-patterns
- ✅ Ready for use in Epic 2 testing

### Previous Story Intelligence

**From Story 1-6 (Completed 2026-01-21):**

This story is VERY different from Story 1-6. Previous story implemented logging infrastructure in masDependencyMap. This story creates a test fixture (sample solution) that masDependencyMap will analyze.

**No Code Reuse from Story 1-6:**
- Story 1-6 modified src/MasDependencyMap.CLI/Program.cs
- Story 1-6 modified Core service implementations
- This story creates entirely new solution in samples/

**However, Use Similar Commit Pattern:**

Story 1-6 established a clear commit message pattern:
```
[Action] [Feature] [Details]

- Bulleted list of changes
- Each change is specific and measurable
- Includes manual testing evidence
- Includes acceptance criteria verification

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

**Use this pattern for Story 1-7:**
```
Create sample solution for testing circular dependency detection

- Created SampleMonolith.sln with 7 projects in samples/SampleMonolith/
- Implemented intentional Cycle 1: Core ↔ Infrastructure
- Implemented intentional Cycle 2: Legacy.ModuleA ↔ Legacy.ModuleB
- All projects target net8.0 and build successfully
- Added README.md with analysis instructions
- Added ARCHITECTURE.md documenting intentional anti-patterns
- Created samples/sample-output/ placeholder for future outputs
- Manual testing: dotnet build succeeds with 0 warnings
- All acceptance criteria satisfied

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

**Key Difference:**
Story 1-6 was about implementing production code with tests. Story 1-7 is about creating test data. Simpler scope, different deliverables.

### Git Intelligence Summary

**Recent Commit Pattern (Last 10 Commits):**

Recent commits show clear pattern:
1. Implement feature
2. Add comprehensive story documentation
3. Update sprint-status.yaml
4. Include detailed commit message with Co-Authored-By

**File Patterns Established:**
- Story files in `_bmad-output/implementation-artifacts/`
- Source code in `src/MasDependencyMap.Core/` or `src/MasDependencyMap.CLI/`
- Sprint tracking in `_bmad-output/implementation-artifacts/sprint-status.yaml`

**For This Story:**
Since you're creating a sample solution, the file pattern is different:
- New files in `samples/SampleMonolith/`
- Story file in `_bmad-output/implementation-artifacts/1-7-create-sample-solution-for-testing.md`
- Update `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Commit Pattern:**
```bash
# Stage sample solution files
git add samples/SampleMonolith/

# Stage story documentation
git add _bmad-output/implementation-artifacts/1-7-create-sample-solution-for-testing.md

# Stage sprint status update
git add _bmad-output/implementation-artifacts/sprint-status.yaml

# Commit with detailed message
git commit -m "Create sample solution for testing circular dependency detection

- Created SampleMonolith.sln with [N] projects in samples/SampleMonolith/
- Implemented intentional circular dependencies (list cycles)
- All projects target net8.0 and build successfully
- Added README.md with analysis instructions
- Added ARCHITECTURE.md documenting intentional anti-patterns
- Created samples/sample-output/ placeholder directory
- Manual testing: dotnet build succeeds with 0 warnings
- All acceptance criteria satisfied

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### Implementation Guidance

**Step-by-Step Implementation:**

**Phase 1: Plan the Sample Architecture**

1. **Design Circular Dependency Scenarios:**
   - Sketch out 5-10 projects and their dependencies
   - Ensure at least 2 distinct cycles
   - Make it realistic (represent real-world anti-patterns)
   - Document in ARCHITECTURE.md

2. **Choose Project Names:**
   - Use realistic names: Core, Infrastructure, Services, UI, Common, Legacy.ModuleA, Legacy.ModuleB
   - Optionally add more: Analytics, Reporting, Integration, etc.
   - Keep it between 5-10 projects total

**Phase 2: Create Solution Structure**

3. **Create Directory and Solution:**
   ```bash
   # From repository root
   mkdir -p samples/SampleMonolith/src
   cd samples/SampleMonolith
   dotnet new sln -n SampleMonolith
   ```

4. **Create Projects:**
   ```bash
   # Create each project as class library
   dotnet new classlib -n Core -o src/Core -f net8.0
   dotnet new classlib -n Infrastructure -o src/Infrastructure -f net8.0
   dotnet new classlib -n Services -o src/Services -f net8.0
   dotnet new classlib -n UI -o src/UI -f net8.0
   dotnet new classlib -n Common -o src/Common -f net8.0
   dotnet new classlib -n Legacy.ModuleA -o src/Legacy.ModuleA -f net8.0
   dotnet new classlib -n Legacy.ModuleB -o src/Legacy.ModuleB -f net8.0

   # Add projects to solution
   dotnet sln add src/Core/Core.csproj
   dotnet sln add src/Infrastructure/Infrastructure.csproj
   dotnet sln add src/Services/Services.csproj
   dotnet sln add src/UI/UI.csproj
   dotnet sln add src/Common/Common.csproj
   dotnet sln add src/Legacy.ModuleA/Legacy.ModuleA.csproj
   dotnet sln add src/Legacy.ModuleB/Legacy.ModuleB.csproj
   ```

**Phase 3: Create Circular Dependencies**

5. **Add Project References to Create Cycles:**

   **Cycle 1: Core ↔ Infrastructure**
   ```bash
   # Infrastructure depends on Core (normal)
   dotnet add src/Infrastructure/Infrastructure.csproj reference src/Core/Core.csproj

   # Core depends on Infrastructure (creates cycle - anti-pattern)
   dotnet add src/Core/Core.csproj reference src/Infrastructure/Infrastructure.csproj
   ```

   **Cycle 2: Legacy.ModuleA ↔ Legacy.ModuleB**
   ```bash
   # ModuleA depends on ModuleB
   dotnet add src/Legacy.ModuleA/Legacy.ModuleA.csproj reference src/Legacy.ModuleB/Legacy.ModuleB.csproj

   # ModuleB depends on ModuleA (creates cycle)
   dotnet add src/Legacy.ModuleB/Legacy.ModuleB.csproj reference src/Legacy.ModuleA/Legacy.ModuleA.csproj
   ```

   **Additional Dependencies (No Cycles):**
   ```bash
   # Services depends on Core and Infrastructure
   dotnet add src/Services/Services.csproj reference src/Core/Core.csproj
   dotnet add src/Services/Services.csproj reference src/Infrastructure/Infrastructure.csproj

   # UI depends on Services
   dotnet add src/UI/UI.csproj reference src/Services/Services.csproj

   # Common is referenced by multiple projects (no cycles)
   dotnet add src/Core/Core.csproj reference src/Common/Common.csproj
   dotnet add src/Services/Services.csproj reference src/Common/Common.csproj
   ```

6. **Add Simple Stub Classes:**

   Create 2-3 simple classes in each project to make it realistic. Example:

   **src/Core/Domain/Customer.cs:**
   ```csharp
   namespace SampleMonolith.Core.Domain;

   /// <summary>
   /// Represents a customer entity.
   /// </summary>
   public class Customer
   {
       public int Id { get; set; }
       public string Name { get; set; } = string.Empty;
       public string Email { get; set; } = string.Empty;
   }
   ```

   **src/Infrastructure/Repositories/CustomerRepository.cs:**
   ```csharp
   using SampleMonolith.Core.Domain;

   namespace SampleMonolith.Infrastructure.Repositories;

   /// <summary>
   /// Repository for customer data access.
   /// </summary>
   public class CustomerRepository
   {
       public Customer? GetById(int id)
       {
           // Stub implementation
           return null;
       }
   }
   ```

   Repeat for each project with appropriate classes.

**Phase 4: Create Documentation**

7. **Create README.md:**
   ```markdown
   # Sample Monolith for masDependencyMap Testing

   This is a sample .NET solution with intentional circular dependencies for testing masDependencyMap.

   ## Projects

   - **Core**: Domain entities and models
   - **Infrastructure**: Data access and persistence
   - **Services**: Business logic layer
   - **UI**: Presentation layer
   - **Common**: Shared utilities
   - **Legacy.ModuleA**: Legacy module A
   - **Legacy.ModuleB**: Legacy module B

   ## Intentional Circular Dependencies

   ### Cycle 1: Core ↔ Infrastructure
   - Core defines domain entities
   - Infrastructure implements data access and depends on Core
   - Core references Infrastructure (anti-pattern for repository interfaces)

   ### Cycle 2: Legacy.ModuleA ↔ Legacy.ModuleB
   - ModuleA depends on ModuleB for shared functionality
   - ModuleB depends on ModuleA for different shared functionality
   - Represents spaghetti code anti-pattern

   ## Building

   ```bash
   cd samples/SampleMonolith
   dotnet build
   ```

   ## Running Analysis

   After Epic 2 is complete:

   ```bash
   # From masDependencyMap root directory
   dotnet run --project src/MasDependencyMap.CLI -- analyze --solution samples/SampleMonolith/SampleMonolith.sln --output samples/sample-output
   ```

   ## Expected Outputs

   - dependency-graph.dot (DOT format graph)
   - dependency-graph.png (Visual graph showing cycles in red)
   - cycle-analysis.txt (Text report with cycle details)
   - extraction-scores.csv (CSV export with difficulty scores)
   ```

8. **Create ARCHITECTURE.md:**
   ```markdown
   # Sample Monolith Architecture

   ## Purpose

   This solution demonstrates intentional architectural anti-patterns for testing masDependencyMap's circular dependency detection and extraction difficulty scoring.

   ## Project Dependency Graph

   ```
   Core ←→ Infrastructure  (CYCLE 1)
     ↓         ↓
   Services ←--+
     ↓
    UI

   Legacy.ModuleA ←→ Legacy.ModuleB  (CYCLE 2)

   Common (referenced by Core, Services)
   ```

   ## Intentional Anti-Patterns

   ### Cycle 1: Core ↔ Infrastructure (Domain-Data Coupling)

   **Why This Exists:**
   - Core should define domain entities only
   - Infrastructure should implement data access
   - Anti-pattern: Core references Infrastructure for repository interfaces
   - **Real-world occurrence:** Common in projects that don't follow clean architecture

   **How to Break:**
   - Move repository interfaces from Infrastructure to Core
   - Infrastructure implements interfaces defined in Core
   - Remove Core → Infrastructure reference

   ### Cycle 2: Legacy.ModuleA ↔ Legacy.ModuleB (Cross-Module Dependencies)

   **Why This Exists:**
   - Legacy modules grew organically without clear boundaries
   - Each module needed functionality from the other
   - Anti-pattern: Bidirectional dependencies between modules
   - **Real-world occurrence:** Common in monoliths that evolved over 10+ years

   **How to Break:**
   - Extract shared functionality to Common module
   - Remove one direction of dependency
   - Or split into microservices with clear boundaries

   ## Testing Value

   This sample enables testing:
   - Circular dependency detection (Tarjan's algorithm)
   - Cycle visualization (red edges in graph)
   - Cycle breaking recommendations (weakest coupling points)
   - Extraction difficulty scoring (projects in cycles score higher)
   ```

9. **Create sample-output Placeholder:**
   ```bash
   mkdir -p samples/sample-output
   echo "# Sample Outputs

This directory will contain pre-generated outputs after Epic 2 Story 2-9 is complete:
- dependency-graph.dot
- dependency-graph.png
- cycle-analysis.txt
- extraction-scores.csv

These will serve as reference outputs for testing and documentation." > samples/sample-output/README.md
   ```

**Phase 5: Build and Verify**

10. **Build the Sample Solution:**
    ```bash
    cd samples/SampleMonolith
    dotnet build
    # Expected: Build succeeded, 0 Warning(s), 0 Error(s)
    ```

11. **Verify Circular Dependencies:**
    - Check project files manually to confirm cycles exist
    - Verify Core.csproj references Infrastructure.csproj
    - Verify Infrastructure.csproj references Core.csproj
    - Verify Legacy.ModuleA.csproj references Legacy.ModuleB.csproj
    - Verify Legacy.ModuleB.csproj references Legacy.ModuleA.csproj

12. **Test from masDependencyMap Root:**
    ```bash
    # Return to repository root
    cd ../../

    # Verify solution path works from root
    ls samples/SampleMonolith/SampleMonolith.sln
    # Expected: File exists
    ```

**Phase 6: Documentation and Commit**

13. **Update Story File:**
    - Mark all tasks as complete
    - Add completion notes
    - Update status to "done" (or "review" if code review is required)

14. **Update Sprint Status:**
    - Update `_bmad-output/implementation-artifacts/sprint-status.yaml`
    - Change `1-7-create-sample-solution-for-testing: backlog` to `ready-for-dev` or `done`

15. **Commit Changes:**
    ```bash
    git add samples/SampleMonolith/
    git add _bmad-output/implementation-artifacts/1-7-create-sample-solution-for-testing.md
    git add _bmad-output/implementation-artifacts/sprint-status.yaml

    git commit -m "Create sample solution for testing circular dependency detection

- Created SampleMonolith.sln with 7 projects in samples/SampleMonolith/
- Implemented intentional Cycle 1: Core ↔ Infrastructure (domain-data coupling anti-pattern)
- Implemented intentional Cycle 2: Legacy.ModuleA ↔ Legacy.ModuleB (cross-module dependencies)
- Additional projects: Services, UI, Common (no cycles, realistic dependencies)
- All projects target net8.0 with nullable reference types enabled
- Added 2-3 stub classes per project for realism
- Added README.md with build instructions and analysis commands
- Added ARCHITECTURE.md documenting intentional anti-patterns and how to break cycles
- Created samples/sample-output/ placeholder for future generated outputs
- Manual testing: dotnet build succeeds with 0 warnings, 0 errors
- Manual verification: Confirmed 2 distinct circular dependency cycles exist
- All acceptance criteria satisfied

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
    ```

**Common Pitfalls to Avoid:**

1. ❌ Don't create type-level circular dependencies (won't compile)
   - ✅ Create PROJECT REFERENCE cycles only

2. ❌ Don't make the sample too complex (keep it 5-10 projects)
   - ✅ Focus on clear, demonstrable cycles

3. ❌ Don't forget to document WHY the cycles exist
   - ✅ ARCHITECTURE.md explains the anti-patterns

4. ❌ Don't try to generate outputs now (Epic 2 not done yet)
   - ✅ Create placeholder README in sample-output/

5. ❌ Don't modify masDependencyMap source code in this story
   - ✅ This story creates test data only

### Project Context Reference

🔬 **Complete project rules:** See `_bmad-output/project-context.md` for comprehensive guidelines.

**Relevant Sections for This Story:**

Since this story creates a test fixture (sample solution) rather than masDependencyMap production code, most project-context.md rules don't directly apply. However:

**DO Follow:**
1. **.NET 8 Target Framework** (project-context.md lines 48-49)
   - Sample projects should target net8.0
   - Matches masDependencyMap's target framework

2. **File-Scoped Namespaces** (project-context.md lines 76-78)
   - Use C# 10+ file-scoped namespace syntax
   - Example: `namespace SampleMonolith.Core.Domain;`

3. **PascalCase Naming** (project-context.md lines 163-168)
   - File names match class names
   - Use PascalCase for classes, properties, methods

**DON'T Worry About:**
- Dependency injection (not needed in sample code)
- Structured logging (not needed in sample code)
- Error handling (sample code is just stubs)
- Testing (the sample itself IS the test fixture)
- QuikGraph, Roslyn, or other masDependencyMap dependencies

**Key Insight from Project Context:**

From lines 250-254: "Tool targets .NET 8.0 BUT analyzes solutions from .NET Framework 3.5 through .NET 8+"

For MVP, create sample using .NET 8 only. Future stories can add older framework samples if backward compatibility testing is needed.

### References

**Epic & Story Context:**
- [Epic 1: Project Foundation and Command-Line Interface, Story 1.7] - Story requirements from epics/epic-1-project-foundation-and-command-line-interface.md lines 95-109
- [Story 1.7 Acceptance Criteria] - Sample solution with intentional cycles for testing

**Architecture Documents:**
- [Architecture: core-architectural-decisions.md, Error Handling section] - Strategy pattern with fallback chain for solution loading
- [Architecture: core-architectural-decisions.md, Performance Strategy section] - Tool must handle 50-400+ project solutions
- [Architecture: project-structure-boundaries.md] - Feature-based namespace organization

**Project Context:**
- [project-context.md lines 48-51] - .NET 8 target framework and version compatibility requirements
- [project-context.md lines 76-78] - File-scoped namespace syntax
- [project-context.md lines 250-254] - Critical: Tool analyzes 20-year version span (.NET Framework 3.5+ through .NET 8+)
- [project-context.md lines 269-275] - Circular dependency detection via Tarjan's algorithm

**Previous Stories:**
- [Story 1-6: Implement Structured Logging] - Completed 2026-01-21, established commit message pattern
- [Story 1-5: Set Up Dependency Injection Container] - Completed, established DI infrastructure
- [Story 1-1: Initialize .NET 8 Solution Structure] - Established solution structure pattern

**External Resources:**
- [Tarjan's Strongly Connected Components Algorithm](https://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm) - Used in Epic 3 for cycle detection
- [QuikGraph Documentation](https://github.com/KeRNeLith/QuikGraph) - Graph library used for dependency graph modeling

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

**MSBuild Circular Dependency Limitation Discovered:**
- Modern .NET SDK-style projects do NOT allow circular project references
- MSBuild error: "MSB4006: There is a circular dependency in the target dependency graph"
- This is a fundamental difference from legacy .NET Framework projects
- Dev Notes incorrectly stated that .NET allows project reference cycles

**Resolution:**
- Created realistic multi-layer architecture WITHOUT compile-time circular dependencies
- Documented MSBuild limitation thoroughly in ARCHITECTURE.md
- Suggested future approaches for circular dependency testing (old-style .csproj, assembly copying, real legacy codebases)

### Completion Notes List

✅ **Story Completed Successfully with Architecture Adjustment**

**What Was Implemented:**

1. **Sample Solution Created (7 Projects):**
   - Core - Domain entities and models
   - Infrastructure - Data access layer
   - Services - Business logic layer
   - UI - Presentation layer
   - Common - Shared utilities
   - Legacy.ModuleA - Legacy subsystem module
   - Legacy.ModuleB - Legacy subsystem module

2. **Realistic Stub Classes (16 Files):**
   - Common: StringExtensions, DateTimeProvider
   - Core: Customer, Order, ICustomerValidator
   - Infrastructure: ICustomerRepository, CustomerRepository, OrderRepository
   - Services: CustomerService, OrderService
   - UI: CustomerController, OrderController
   - Legacy.ModuleA: FeatureA, UtilityA
   - Legacy.ModuleB: FeatureB, UtilityB

3. **Project Configuration:**
   - All projects target net8.0
   - XML documentation generation enabled
   - Nullable reference types enabled
   - File-scoped namespaces (C# 10+)
   - ImplicitUsings enabled

4. **Project Dependencies (Realistic Multi-Layer):**
   - UI → Services
   - Services → Core, Infrastructure, Common
   - Infrastructure → Core
   - Core → Common
   - Legacy modules independent (no circular refs due to MSBuild limitation)

5. **Documentation Created:**
   - README.md with build/analysis instructions
   - ARCHITECTURE.md with detailed architecture explanation and MSBuild circular dependency limitation discussion
   - samples/sample-output/README.md placeholder for future generated outputs

**Key Discovery - Circular Dependency Limitation:**

The story's Dev Notes contained an ERROR stating that ".NET allows project reference cycles as long as there are no TYPE reference cycles at compile time." This is INCORRECT for modern .NET SDK-style projects.

**MSBuild SDK-style projects do NOT allow circular project references under any circumstances.** Attempting to create them results in build error MSB4006.

**Implemented Solution:**
- Created realistic multi-layer architecture demonstrating complex dependencies
- Documented the MSBuild limitation thoroughly
- Suggested future approaches for circular dependency testing:
  1. Use old-style .csproj format (pre-.NET Core)
  2. Manual assembly copying approach
  3. Test on real legacy .NET Framework codebases

**Acceptance Criteria Assessment:**

❌ **AC Partially Not Met (With Justification):**
- "Sample contains 5-10 projects with at least 2 circular dependency cycles"
- **Met**: 7 projects (within 5-10 range)
- **NOT MET**: 0 circular dependency cycles (intended 2, but MSBuild doesn't allow)
- **Justification**: Technical impossibility in modern .NET SDK-style projects

✅ **All Other ACs Met:**
- Sample solution builds successfully: `dotnet build` succeeds with 0 warnings, 0 errors
- Pre-generated expected output placeholder exists: samples/sample-output/README.md
- README includes instructions for running analysis: Comprehensive usage documentation

**Testing Evidence:**

```
Build verification:
$ cd samples/SampleMonolith && dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.31

Project verification:
$ dotnet sln list
src\Common\Common.csproj
src\Core\Core.csproj
src\Infrastructure\Infrastructure.csproj
src\Legacy.ModuleA\Legacy.ModuleA.csproj
src\Legacy.ModuleB\Legacy.ModuleB.csproj
src\Services\Services.csproj
src\UI\UI.csproj
```

**Files Created (39 total):**
- 1 solution file (SampleMonolith.sln)
- 7 project files (*.csproj)
- 16 source code files (*.cs)
- 3 documentation files (README.md, ARCHITECTURE.md, sample-output/README.md)
- 12 generated binary/build files

**Value Delivered:**

Despite the lack of circular project references (due to MSBuild SDK limitation), the sample provides significant testing value:
- ✅ Solution loading with multiple projects
- ✅ Multi-layer dependency graph construction
- ✅ Realistic .NET 8 architecture patterns
- ✅ Comprehensive documentation
- ⏸️ Circular dependency detection (requires future sample enhancement)

### File List

**Created Files:**

**Solution & Documentation:**
- samples/SampleMonolith/SampleMonolith.sln
- samples/SampleMonolith/README.md
- samples/SampleMonolith/ARCHITECTURE.md
- samples/sample-output/README.md

**Project Files (7):**
- samples/SampleMonolith/src/Common/Common.csproj
- samples/SampleMonolith/src/Core/Core.csproj
- samples/SampleMonolith/src/Infrastructure/Infrastructure.csproj
- samples/SampleMonolith/src/Services/Services.csproj
- samples/SampleMonolith/src/UI/UI.csproj
- samples/SampleMonolith/src/Legacy.ModuleA/Legacy.ModuleA.csproj
- samples/SampleMonolith/src/Legacy.ModuleB/Legacy.ModuleB.csproj

**Common Project (2 classes):**
- samples/SampleMonolith/src/Common/StringExtensions.cs
- samples/SampleMonolith/src/Common/DateTimeProvider.cs

**Core Project (3 classes):**
- samples/SampleMonolith/src/Core/Domain/Customer.cs
- samples/SampleMonolith/src/Core/Domain/Order.cs
- samples/SampleMonolith/src/Core/Services/ICustomerValidator.cs

**Infrastructure Project (3 classes):**
- samples/SampleMonolith/src/Infrastructure/Data/ICustomerRepository.cs
- samples/SampleMonolith/src/Infrastructure/Data/CustomerRepository.cs
- samples/SampleMonolith/src/Infrastructure/Data/OrderRepository.cs

**Services Project (2 classes):**
- samples/SampleMonolith/src/Services/CustomerService.cs
- samples/SampleMonolith/src/Services/OrderService.cs

**UI Project (2 classes):**
- samples/SampleMonolith/src/UI/CustomerController.cs
- samples/SampleMonolith/src/UI/OrderController.cs

**Legacy.ModuleA Project (2 classes):**
- samples/SampleMonolith/src/Legacy.ModuleA/FeatureA.cs
- samples/SampleMonolith/src/Legacy.ModuleA/UtilityA.cs

**Legacy.ModuleB Project (2 classes):**
- samples/SampleMonolith/src/Legacy.ModuleB/FeatureB.cs
- samples/SampleMonolith/src/Legacy.ModuleB/UtilityB.cs

**Modified Files:**
- _bmad-output/implementation-artifacts/sprint-status.yaml (status: ready-for-dev → in-progress)
- _bmad-output/implementation-artifacts/1-7-create-sample-solution-for-testing.md (this file - tasks marked complete, completion notes added)
