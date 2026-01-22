# Sample Monolith for masDependencyMap Testing

This is a sample .NET 8 solution with a realistic multi-layer architecture for testing masDependencyMap's dependency analysis capabilities.

## Projects (7 total)

- **Core** - Domain entities and core business models
- **Infrastructure** - Data access and persistence layer
- **Services** - Business logic layer
- **UI** - Presentation layer
- **Common** - Shared utilities
- **Legacy.ModuleA** - Legacy subsystem module A
- **Legacy.ModuleB** - Legacy subsystem module B

## Architecture

```
Common (no dependencies)
  ↑
Core → Common
  ↑
Infrastructure → Core
  ↑
Services → Core, Infrastructure, Common
  ↑
UI → Services

Legacy.ModuleA (independent)
Legacy.ModuleB (independent)
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture documentation and discussion of circular dependency limitations in modern .NET.

## Important Note: No Circular Dependencies

**This sample does NOT contain circular project references** because modern .NET SDK-style projects do not allow them at build time. This is a fundamental MSBuild limitation.

For testing circular dependency detection, see ARCHITECTURE.md for suggested approaches using legacy .NET Framework project formats or manual assembly references.

## Building

```bash
cd samples/SampleMonolith
dotnet build
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Verifying Projects

List all projects in the solution:
```bash
dotnet sln list
```

Expected:
```
src\Core\Core.csproj
src\Infrastructure\Infrastructure.csproj
src\Services\Services.csproj
src\UI\UI.csproj
src\Common\Common.csproj
src\Legacy.ModuleA\Legacy.ModuleA.csproj
src\Legacy.ModuleB\Legacy.ModuleB.csproj
```

## Running Analysis (After Epic 2 Implementation)

Once masDependencyMap implements solution loading and graph generation (Epic 2), run analysis:

```bash
# From masDependencyMap repository root
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution samples/SampleMonolith/SampleMonolith.sln --output samples/sample-output
```

## Expected Outputs (After Epic 2)

Analysis will generate the following outputs in `samples/sample-output/`:

- **dependency-graph.dot** - DOT format dependency graph
- **dependency-graph.png** - Visual PNG graph (requires Graphviz)
- **dependency-graph.svg** - Visual SVG graph (requires Graphviz)
- **analysis-report.txt** - Text report with dependency metrics and statistics

**Note**: Output generation will be available after Epic 2 Story 2-9 (Render DOT files to PNG and SVG with Graphviz) is complete.

## Testing Value

This sample solution enables testing:

- ✅ Solution loading with Roslyn, MSBuild, or project file fallback
- ✅ Multi-project dependency graph construction
- ✅ Dependency depth analysis
- ✅ Coupling metrics (fan-in/fan-out)
- ✅ Graphviz DOT format generation
- ✅ PNG/SVG visualization rendering
- ✅ Text and CSV report generation
- ⏸️ Circular dependency detection (requires future sample with old-style .csproj)

## What This Sample Demonstrates

### Realistic Multi-Layer Architecture
- Traditional N-tier architecture (UI → Services → Infrastructure → Core)
- Common utilities layer referenced by multiple projects
- Independent legacy modules

### Dependency Patterns
- Linear dependencies (UI → Services → Infrastructure → Core)
- Shared dependencies (multiple projects → Common)
- Isolated modules (Legacy.ModuleA, Legacy.ModuleB)

### .NET 8 Modern Patterns
- SDK-style project format
- File-scoped namespaces (C# 10+)
- Nullable reference types enabled
- XML documentation generation enabled
- ImplicitUsings for cleaner code

## Project Structure

```
samples/SampleMonolith/
├── SampleMonolith.sln
├── README.md
├── ARCHITECTURE.md
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   ├── Domain/
    │   │   ├── Customer.cs
    │   │   └── Order.cs
    │   └── Services/
    │       └── ICustomerValidator.cs
    ├── Infrastructure/
    │   ├── Infrastructure.csproj
    │   └── Data/
    │       ├── ICustomerRepository.cs
    │       ├── CustomerRepository.cs
    │       └── OrderRepository.cs
    ├── Services/
    │   ├── Services.csproj
    │   ├── CustomerService.cs
    │   └── OrderService.cs
    ├── UI/
    │   ├── UI.csproj
    │   ├── CustomerController.cs
    │   └── OrderController.cs
    ├── Common/
    │   ├── Common.csproj
    │   ├── StringExtensions.cs
    │   └── DateTimeProvider.cs
    ├── Legacy.ModuleA/
    │   ├── Legacy.ModuleA.csproj
    │   ├── FeatureA.cs
    │   └── UtilityA.cs
    └── Legacy.ModuleB/
        ├── Legacy.ModuleB.csproj
        ├── FeatureB.cs
        └── UtilityB.cs
```

## Future Enhancements

To add circular dependency testing capabilities:

1. **Add Old-Style .csproj Projects**: Create projects using pre-.NET Core project format that allowed circular references
2. **Assembly Reference Approach**: Manually copy assemblies to create runtime circular dependencies
3. **Real-World Sample**: Import actual legacy .NET Framework solution with existing cycles

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed approaches.

## Related Documentation

- **Epic 2**: Solution Loading and Dependency Discovery
- **Epic 3**: Circular Dependency Detection with Tarjan's Algorithm
- **Story 1.7**: Create Sample Solution for Testing (this sample)
