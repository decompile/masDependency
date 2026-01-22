# Sample Monolith Architecture

## Purpose

This solution demonstrates a realistic .NET 8 monolith architecture for testing masDependencyMap's dependency analysis capabilities.

## Important Note: Circular Dependencies in Modern .NET

**MSBuild SDK-Style Projects Limitation:**

Modern .NET SDK-style projects (introduced in .NET Core) do NOT allow circular project references at build time. Attempting to create project reference cycles results in build errors:

```
error MSB4006: There is a circular dependency in the target dependency graph
```

This is a fundamental difference from legacy .NET Framework projects, which allowed circular assembly references through GAC or manual assembly resolution.

**Implication for This Sample:**

This sample solution uses a realistic .NET 8 architecture WITHOUT compile-time circular project references. However, it demonstrates:

1. **Complex Multi-Layer Dependencies** - Realistic layered architecture with multiple projects
2. **High Coupling** - Projects with strong interdependencies that would benefit from analysis
3. **Legacy Module Interactions** - Separate legacy modules representing old codebas code that evolved over time

**Future Considerations:**

To test circular dependency detection in masDependencyMap, consider these approaches:

1. **Create Old-Style .csproj Projects**: Use non-SDK project format (pre-.NET Core) that allowed circular references
2. **Assembly Copying**: Manually copy compiled assemblies to create runtime circular dependencies
3. **Analyze Real-World Legacy Codebases**: Test on actual .NET Framework solutions with existing cycles
4. **Type-Level Cycle Detection**: Analyze namespace/type dependencies rather than just project references

## Project Dependency Graph

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

## Project Descriptions

### Core
**Purpose**: Domain entities and core business models

**Dependencies**:
- Common (shared utilities)

**Contents**:
- `Domain/Customer.cs` - Customer entity
- `Domain/Order.cs` - Order entity
- `Services/ICustomerValidator.cs` - Validation interface

**Why This Exists**:
Represents the core business domain. In clean architecture, this should have minimal dependencies.

### Infrastructure
**Purpose**: Data access and persistence layer

**Dependencies**:
- Core (for domain entities)

**Contents**:
- `Data/ICustomerRepository.cs` - Repository interface
- `Data/CustomerRepository.cs` - In-memory repository implementation
- `Data/OrderRepository.cs` - Order data access

**Why This Exists**:
Implements data access for Core entities. Follows repository pattern.

**Classic Anti-Pattern (Not Implemented Due to MSBuild Limits)**:
In poorly designed monoliths, Core often references Infrastructure for repository interfaces, creating a circular dependency. This violates clean architecture principles where Infrastructure should implement interfaces defined in Core.

### Services
**Purpose**: Business logic layer

**Dependencies**:
- Core (domain entities)
- Infrastructure (data access)
- Common (utilities)

**Contents**:
- `CustomerService.cs` - Customer business logic
- `OrderService.cs` - Order business logic

**Why This Exists**:
Orchestrates business operations using Core entities and Infrastructure repositories.

### UI
**Purpose**: Presentation layer

**Dependencies**:
- Services (business logic)

**Contents**:
- `CustomerController.cs` - Customer UI controller
- `OrderController.cs` - Order UI controller

**Why This Exists**:
Represents the presentation tier in a traditional N-tier architecture.

### Common
**Purpose**: Shared utilities

**Dependencies**: None

**Contents**:
- `StringExtensions.cs` - String utility extensions
- `DateTimeProvider.cs` - Testable date/time abstraction

**Why This Exists**:
Provides cross-cutting utilities used by multiple layers. Should have no dependencies to avoid coupling.

### Legacy.ModuleA
**Purpose**: Represents a legacy subsystem/module

**Dependencies**: Legacy.ModuleB (currently removed due to MSBuild)

**Contents**:
- `FeatureA.cs` - Legacy feature A
- `UtilityA.cs` - Legacy utility A

**Why This Exists**:
Simulates an old module that evolved independently. In real monoliths, legacy modules often have tight coupling.

**Intended Circular Dependency** (Not Possible in SDK-Style Projects):
- ModuleA depends on ModuleB for shared functionality
- ModuleB depends on ModuleA for different shared functionality
- Creates classic "spaghetti code" anti-pattern

### Legacy.ModuleB
**Purpose**: Represents another legacy subsystem/module

**Dependencies**: Legacy.ModuleA (currently removed due to MSBuild)

**Contents**:
- `FeatureB.cs` - Legacy feature B
- `UtilityB.cs` - Legacy utility B

**Why This Exists**:
Simulates another old module. Together with ModuleA, demonstrates cross-module dependencies common in 10+ year old codebases.

## Dependency Analysis Testing Value

This sample enables testing:

1. **Multi-Project Dependency Resolution**
   - 7 projects with varied dependency patterns
   - Tests solution loading and project reference parsing

2. **Layered Architecture Detection**
   - Clear N-tier structure: UI → Services → Infrastructure → Core
   - Tests depth and hierarchy analysis

3. **Coupling Analysis**
   - Multiple projects depending on same modules (Core, Common)
   - Tests fan-in/fan-out metrics

4. **Visualization Generation**
   - Complex enough to create interesting graphs
   - Simple enough to understand at a glance

5. **Future Circular Dependency Detection**
   - Once circular references are added (via non-SDK projects or assembly copying)
   - Tests Tarjan's SCC algorithm implementation

## Testing Value

Despite the lack of compile-time circular dependencies, this sample provides value for testing:

- ✅ Solution loading with multiple projects
- ✅ Project reference graph construction
- ✅ Dependency depth analysis
- ✅ Coupling metrics (fan-in/fan-out)
- ✅ Graphviz visualization generation
- ✅ Multi-layer architecture patterns
- ⏸️ Circular dependency detection (requires future enhancement)

## How to Add Circular Dependencies (Future Work)

If circular dependency testing becomes critical, consider these approaches:

### Approach 1: Old-Style .csproj Format
Convert Legacy.ModuleA and Legacy.ModuleB to use pre-.NET Core project format:
```xml
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <!-- Old-style .csproj that allows circular references -->
</Project>
```

### Approach 2: Manual Assembly References
1. Build ModuleA without ModuleB reference
2. Build ModuleB with reference to ModuleA.dll (file reference, not project reference)
3. Copy ModuleB.dll to ModuleA output directory
4. Both modules now have runtime circular dependency

### Approach 3: Test on Real Legacy Codebases
Identify real .NET Framework solutions with circular dependencies and use them as test subjects.

## Building

```bash
cd samples/SampleMonolith
dotnet build
```

Expected: Build succeeds with 0 warnings, 0 errors.

## Analysis (After Epic 2 Implementation)

```bash
# From masDependencyMap root
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution samples/SampleMonolith/SampleMonolith.sln --output samples/sample-output
```

Expected outputs:
- dependency-graph.dot (DOT format graph)
- dependency-graph.png (Visual graph)
- analysis-report.txt (Text report with dependency metrics)

