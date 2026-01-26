# Story 4.4: Implement External API Exposure Detector

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want external API exposure detected by scanning for web service attributes,
So that projects with external APIs are marked as harder to extract.

## Acceptance Criteria

**Given** A project with source code
**When** ExternalApiDetector.DetectAsync() is called
**Then** Roslyn semantic analysis scans for [WebMethod], [ApiController], [Route] attributes
**And** Controller classes inheriting from ApiController or ControllerBase are identified
**And** WCF service contracts ([ServiceContract], [OperationContract]) are detected
**And** Count of external API endpoints is calculated
**And** API exposure score is calculated: 0 endpoints = 0, 1-5 endpoints = 33, 6-15 endpoints = 66, 16+ endpoints = 100
**And** ILogger logs API detection results (e.g., "Project X has 8 external API endpoints")

**Given** Roslyn semantic analysis is unavailable
**When** ExternalApiDetector.DetectAsync() is called
**Then** API exposure defaults to 0 (assume no external APIs)
**And** ILogger logs warning that detection was unavailable

## Tasks / Subtasks

- [ ] Create ExternalApiMetric model class (AC: Store API exposure metrics with endpoint counts)
  - [ ] Define ExternalApiMetric record with properties: ProjectName, ProjectPath, EndpointCount, NormalizedScore, ApiTypes (breakdown by type)
  - [ ] Add XML documentation explaining API exposure calculation and scoring thresholds
  - [ ] Use record type for immutability (C# 9+ pattern, consistent with Stories 4.1-4.3)
  - [ ] Include ApiTypeBreakdown property: counts for WebMethod, WebAPI, WCF separately
  - [ ] Place in `MasDependencyMap.Core.ExtractionScoring` namespace

- [ ] Create IExternalApiDetector interface (AC: Abstraction for DI)
  - [ ] Define DetectAsync(ProjectNode project, CancellationToken cancellationToken = default) method signature
  - [ ] Return Task<ExternalApiMetric> for single project analysis
  - [ ] Add XML documentation with examples and exception documentation
  - [ ] Place in `MasDependencyMap.Core.ExtractionScoring` namespace

- [ ] Implement ExternalApiDetector class skeleton (AC: Set up Roslyn infrastructure)
  - [ ] Implement IExternalApiDetector interface
  - [ ] Inject ILogger<ExternalApiDetector> via constructor for structured logging
  - [ ] Set up MSBuildWorkspace initialization pattern (like Story 4.2)
  - [ ] Implement DetectAsync method signature with proper async/await pattern
  - [ ] File-scoped namespace declaration (C# 10+ pattern)
  - [ ] Async method with Async suffix and ConfigureAwait(false) per project conventions

- [ ] Implement WebAPI attribute detection (AC: Scan for [ApiController], [Route] attributes)
  - [ ] Load project compilation via MSBuildWorkspace.OpenProjectAsync()
  - [ ] Get semantic model for each document in project
  - [ ] Use syntax tree walker to find ClassDeclarationSyntax nodes
  - [ ] Check if class has [ApiController] attribute using semantic model
  - [ ] Check if class inherits from ApiController or ControllerBase base types
  - [ ] Scan methods for [HttpGet], [HttpPost], [HttpPut], [HttpDelete], [Route] attributes
  - [ ] Count total API endpoints (methods with HTTP verb attributes)
  - [ ] Store count in ApiTypeBreakdown.WebApiEndpoints

- [ ] Implement legacy WebMethod detection (AC: Scan for [WebMethod] attributes)
  - [ ] Use syntax tree walker to find MethodDeclarationSyntax nodes
  - [ ] Check if method has System.Web.Services.WebMethodAttribute using semantic model
  - [ ] Count total WebMethod endpoints
  - [ ] Store count in ApiTypeBreakdown.WebMethodEndpoints

- [ ] Implement WCF service contract detection (AC: Scan for [ServiceContract], [OperationContract])
  - [ ] Use syntax tree walker to find InterfaceDeclarationSyntax nodes
  - [ ] Check if interface has System.ServiceModel.ServiceContractAttribute
  - [ ] For service contract interfaces, scan methods for OperationContractAttribute
  - [ ] Count total WCF operation contract endpoints
  - [ ] Store count in ApiTypeBreakdown.WcfEndpoints

- [ ] Implement API exposure scoring algorithm (AC: Score 0-100 based on endpoint counts)
  - [ ] Calculate total endpoints: WebMethod + WebAPI + WCF
  - [ ] Apply scoring thresholds:
    - 0 endpoints → score 0 (no external APIs)
    - 1-5 endpoints → score 33 (low exposure)
    - 6-15 endpoints → score 66 (medium exposure)
    - 16+ endpoints → score 100 (high exposure)
  - [ ] Use stepped scoring (not linear) as specified in acceptance criteria
  - [ ] Ensure normalized score is clamped to 0-100 range using Math.Clamp
  - [ ] Document scoring algorithm in XML comments and code comments
  - [ ] Higher normalized score = more external APIs = harder to extract (matches Epic 4 scoring semantics)

- [ ] Implement fallback handling (AC: Graceful degradation when Roslyn unavailable)
  - [ ] Wrap Roslyn workspace loading in try-catch for WorkspaceFailedException, FileNotFoundException, IOException
  - [ ] On exception, log warning: "Could not analyze API exposure for {ProjectName}: {Reason}"
  - [ ] Return ExternalApiMetric with: EndpointCount = 0, NormalizedScore = 0 (assume no external APIs)
  - [ ] Log: "Assuming no external APIs for {ProjectName} due to analysis failure"
  - [ ] Ensure fallback doesn't throw exceptions (graceful degradation)
  - [ ] Dispose MSBuildWorkspace properly in finally block or using statement

- [ ] Add structured logging with named placeholders (AC: Log API detection)
  - [ ] Log Information: "Analyzing API exposure for project {ProjectName}" at start
  - [ ] Log Information: "Detected {TotalEndpoints} API endpoints in {ProjectName}: {WebApiCount} WebAPI, {WebMethodCount} WebMethod, {WcfCount} WCF" after detection
  - [ ] Log Debug: "Project {ProjectName}: Endpoints={EndpointCount}, Score={NormalizedScore}" for results
  - [ ] Log Warning: "Could not analyze API exposure for {ProjectName}, defaulting to 0: {Reason}" on fallback
  - [ ] Use named placeholders, NOT string interpolation (critical project rule)
  - [ ] Log level: Information for key milestones, Debug for detailed metrics, Warning for fallback

- [ ] Register service in DI container (AC: Service integration)
  - [ ] Add registration in CLI Program.cs DI configuration
  - [ ] Use services.AddSingleton<IExternalApiDetector, ExternalApiDetector>() pattern
  - [ ] Register in "Epic 4: Extraction Scoring Services" section (after ITechDebtAnalyzer)
  - [ ] Follow existing DI registration patterns from Stories 4.1-4.3

- [ ] Create comprehensive unit tests (AC: Test coverage)
  - [ ] Create test class: tests/MasDependencyMap.Core.Tests/ExtractionScoring/ExternalApiDetectorTests.cs
  - [ ] Test: DetectAsync_ProjectWithNoApis_Returns0Score (no APIs)
  - [ ] Test: DetectAsync_ProjectWith3Endpoints_Returns33Score (low exposure, 1-5 range)
  - [ ] Test: DetectAsync_ProjectWith10Endpoints_Returns66Score (medium exposure, 6-15 range)
  - [ ] Test: DetectAsync_ProjectWith20Endpoints_Returns100Score (high exposure, 16+ range)
  - [ ] Test: DetectAsync_WebApiControllerWithAttributes_DetectsEndpoints (ApiController, Route, HttpGet)
  - [ ] Test: DetectAsync_LegacyWebMethodProject_DetectsWebMethods ([WebMethod] attributes)
  - [ ] Test: DetectAsync_WcfServiceContract_DetectsOperationContracts ([ServiceContract], [OperationContract])
  - [ ] Test: DetectAsync_MixedApiTypes_CountsAllEndpoints (WebAPI + WCF in same project)
  - [ ] Test: DetectAsync_RoslynUnavailable_ReturnsFallbackScore0 (fallback behavior)
  - [ ] Test: DetectAsync_NullProject_ThrowsArgumentNullException (defensive programming)
  - [ ] Test: DetectAsync_CancellationRequested_ThrowsOperationCanceledException (cancellation support)
  - [ ] Use xUnit, FluentAssertions pattern from project conventions
  - [ ] Test naming: {MethodName}_{Scenario}_{ExpectedResult} pattern
  - [ ] Arrange-Act-Assert structure
  - [ ] Create test project fixtures with API attributes for integration testing

- [ ] Validate against project-context.md rules (AC: Architecture compliance)
  - [ ] Feature-based namespace: MasDependencyMap.Core.ExtractionScoring (NOT layer-based)
  - [ ] Async suffix on all async methods (DetectAsync)
  - [ ] File-scoped namespace declarations (all files)
  - [ ] ILogger injection via constructor (NOT static logger)
  - [ ] ConfigureAwait(false) in library code (Core layer)
  - [ ] XML documentation on all public APIs (model, interface, implementation)
  - [ ] Test files mirror Core namespace structure (tests/MasDependencyMap.Core.Tests/ExtractionScoring)
  - [ ] Dispose MSBuildWorkspace properly (using statement or try-finally)

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Story 4.4 External API Exposure Requirements:**

This story implements external API exposure detection, the FOURTH and FINAL metric in Epic 4's extraction difficulty scoring framework.

**Epic 4 Vision (Recap):**
- Story 4.1: Coupling metrics ✅ DONE
- Story 4.2: Cyclomatic complexity metrics ✅ DONE
- Story 4.3: Technology version debt metrics ✅ DONE
- Story 4.4: External API exposure metrics (THIS STORY)
- Story 4.5: Combined extraction score calculator (uses 4.1-4.4)
- Story 4.6: Ranked extraction candidate lists
- Story 4.7: Heat map visualization with color-coded scores
- Story 4.8: Display extraction scores as node labels

**Story 4.4 Unique Challenges:**

1. **Roslyn Semantic Analysis (Similar to Story 4.2):**
   - Story 4.1: Graph traversal (in-memory data)
   - Story 4.2: Roslyn semantic analysis (cyclomatic complexity)
   - Story 4.3: XML parsing (target framework)
   - Story 4.4: **Roslyn semantic analysis (attribute detection)**
   - MUST use semantic model to reliably detect attributes (syntax alone is insufficient)

2. **Multi-Technology API Detection:**
   - **Modern Web API:** [ApiController], [Route], [HttpGet], [HttpPost], etc.
   - **Legacy ASMX Web Services:** [WebMethod] attributes
   - **WCF Services:** [ServiceContract] on interfaces, [OperationContract] on methods
   - MUST detect ALL three API technology types in same analysis

3. **Stepped Scoring Strategy (Different from Stories 4.1-4.3):**
   - Story 4.1: Relative normalization (max in solution = 100)
   - Story 4.2: Absolute normalization (industry complexity thresholds)
   - Story 4.3: Timeline-based normalization (older = higher debt)
   - Story 4.4: **Stepped/threshold-based scoring (predefined ranges)**
   - 0 endpoints = 0, 1-5 = 33, 6-15 = 66, 16+ = 100
   - NOT linear interpolation - discrete thresholds

4. **Fallback Handling (Like Story 4.2 and 4.3):**
   - Similar to Story 4.2's Roslyn fallback
   - If Roslyn unavailable → default to 0 (assume no external APIs)
   - Conservative fallback: underestimate rather than overestimate exposure
   - Graceful degradation ensures partial data availability

🚨 **CRITICAL - Why Roslyn Semantic Analysis Is Required:**

**Syntax-Only Detection Is Insufficient:**

Attributes look simple in syntax, but semantic analysis is REQUIRED because:
1. **Fully qualified names:** `[System.Web.Services.WebMethod]` vs `[WebMethod]` - need to resolve using statements
2. **Inherited attributes:** `[ApiController]` may be on base class, not visible in syntax of derived class
3. **Base type checking:** Need semantic model to check if `ControllerBase` is in inheritance chain
4. **Attribute aliasing:** `using WM = System.Web.Services.WebMethodAttribute;` then `[WM]`
5. **Conditional compilation:** Attributes may be conditionally included via `#if` directives

**Example Where Syntax Fails:**

```csharp
using System.Web.Services;
using M = System.Web.Services.WebMethodAttribute;

public class LegacyService
{
    [WebMethod]  // Fully qualified: System.Web.Services.WebMethodAttribute
    public string GetData() => "data";

    [M]  // Aliased attribute - syntax doesn't know this is WebMethodAttribute!
    public string GetMoreData() => "more data";
}
```

Without semantic analysis, you'd miss the `[M]` attribute because syntax doesn't resolve the alias.

**Semantic Model Approach:**

```csharp
// Get semantic model from compilation
var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

// Find method syntax node
var methodSyntax = /* MethodDeclarationSyntax from syntax tree */;

// Get method symbol from semantic model
var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax);

// Check attributes using semantic model (resolves aliases, fully qualified names, inheritance)
var hasWebMethod = methodSymbol.GetAttributes()
    .Any(attr => attr.AttributeClass?.ToDisplayString() == "System.Web.Services.WebMethodAttribute");
```

This correctly identifies BOTH `[WebMethod]` and `[M]` as WebMethodAttribute.

🚨 **CRITICAL - API Detection Patterns:**

**Detection Strategy Overview:**

Story 4.4 must detect THREE distinct API technology patterns:

| Technology | Detection Points | Symbols to Check |
|------------|------------------|------------------|
| **Modern Web API** | Classes, Methods | `[ApiController]`, `ApiController` base, `ControllerBase` base, `[Route]`, `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]` |
| **Legacy ASMX** | Methods | `[WebMethod]` (System.Web.Services.WebMethodAttribute) |
| **WCF Services** | Interfaces, Methods | `[ServiceContract]` on interface, `[OperationContract]` on methods |

**Modern Web API Detection:**

```csharp
// 1. Find classes with [ApiController] attribute
var classSyntax = /* ClassDeclarationSyntax from syntax tree */;
var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax);

var hasApiController = classSymbol.GetAttributes()
    .Any(attr => attr.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Mvc.ApiControllerAttribute");

// 2. OR check if class inherits from ApiController or ControllerBase
var inheritsApiController = classSymbol.BaseType != null &&
    (classSymbol.BaseType.ToDisplayString() == "Microsoft.AspNetCore.Mvc.ApiController" ||
     classSymbol.BaseType.ToDisplayString() == "Microsoft.AspNetCore.Mvc.ControllerBase" ||
     classSymbol.BaseType.ToDisplayString() == "System.Web.Http.ApiController");

// 3. If API controller, count methods with HTTP verb attributes
if (hasApiController || inheritsApiController)
{
    foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
    {
        var hasHttpVerb = member.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Mvc.HttpGetAttribute" ||
            attr.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Mvc.HttpPostAttribute" ||
            attr.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Mvc.HttpPutAttribute" ||
            attr.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Mvc.HttpDeleteAttribute" ||
            attr.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Mvc.HttpPatchAttribute" ||
            attr.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Mvc.RouteAttribute" ||
            // Legacy ASP.NET Web API (System.Web.Http)
            attr.AttributeClass?.ToDisplayString() == "System.Web.Http.HttpGetAttribute" ||
            attr.AttributeClass?.ToDisplayString() == "System.Web.Http.HttpPostAttribute" ||
            attr.AttributeClass?.ToDisplayString() == "System.Web.Http.HttpPutAttribute" ||
            attr.AttributeClass?.ToDisplayString() == "System.Web.Http.HttpDeleteAttribute" ||
            attr.AttributeClass?.ToDisplayString() == "System.Web.Http.RouteAttribute");

        if (hasHttpVerb)
        {
            webApiEndpoints++;
        }
    }
}
```

**Legacy ASMX WebMethod Detection:**

```csharp
// Find methods with [WebMethod] attribute
var methodSyntax = /* MethodDeclarationSyntax from syntax tree */;
var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax);

var hasWebMethod = methodSymbol.GetAttributes()
    .Any(attr => attr.AttributeClass?.ToDisplayString() == "System.Web.Services.WebMethodAttribute");

if (hasWebMethod)
{
    webMethodEndpoints++;
}
```

**WCF Service Contract Detection:**

```csharp
// 1. Find interfaces with [ServiceContract] attribute
var interfaceSyntax = /* InterfaceDeclarationSyntax from syntax tree */;
var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceSyntax);

var hasServiceContract = interfaceSymbol.GetAttributes()
    .Any(attr => attr.AttributeClass?.ToDisplayString() == "System.ServiceModel.ServiceContractAttribute");

// 2. If service contract, count methods with [OperationContract]
if (hasServiceContract)
{
    foreach (var member in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
    {
        var hasOperationContract = member.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == "System.ServiceModel.OperationContractAttribute");

        if (hasOperationContract)
        {
            wcfEndpoints++;
        }
    }
}
```

**Why Check Multiple Namespaces:**

The tool analyzes a 20-year version span (.NET Framework 3.5 to .NET 8). API technologies evolved:
- **ASP.NET Web API (Legacy):** `System.Web.Http` namespace (.NET Framework 4.x)
- **ASP.NET Core Web API (Modern):** `Microsoft.AspNetCore.Mvc` namespace (.NET Core 3.1+, .NET 5+)
- **ASMX Web Services:** `System.Web.Services` namespace (.NET Framework 2.0+)
- **WCF:** `System.ServiceModel` namespace (.NET Framework 3.0+, .NET Core 3.1+ with CoreWCF)

MUST check ALL namespace variations to detect APIs across all framework versions.

🚨 **CRITICAL - API Exposure Scoring Algorithm:**

**Scoring Philosophy:**

More external APIs = Higher extraction difficulty because:
1. **Breaking changes:** Each API is a contract with external consumers (can't break compatibility)
2. **Testing complexity:** Each endpoint requires integration tests, contract tests
3. **Migration coordination:** External consumers must be updated or API versioning needed
4. **Security surface:** More endpoints = larger attack surface to secure
5. **Documentation burden:** Each API needs documentation for external consumers

**Scoring Thresholds (Stepped, NOT Linear):**

```
API Endpoint Count → Exposure Score (0-100)
─────────────────────────────────────────────
0 endpoints → 0   (No external APIs, easy to extract)
1-5 endpoints → 33  (Low exposure, manageable API surface)
6-15 endpoints → 66 (Medium exposure, significant API surface)
16+ endpoints → 100 (High exposure, complex API surface)
```

**Implementation:**

```csharp
public static double CalculateApiExposureScore(int totalEndpoints)
{
    // Stepped scoring based on acceptance criteria
    if (totalEndpoints == 0)
        return 0;   // No external APIs
    else if (totalEndpoints <= 5)
        return 33;  // Low exposure (1-5 endpoints)
    else if (totalEndpoints <= 15)
        return 66;  // Medium exposure (6-15 endpoints)
    else
        return 100; // High exposure (16+ endpoints)

    // Note: NOT linear interpolation - discrete thresholds per acceptance criteria
}
```

**Why Stepped Scoring (Not Linear)?**

- Acceptance criteria explicitly states discrete thresholds: "0 endpoints = 0, 1-5 endpoints = 33..."
- Stepped scoring creates clear categories: No APIs, Low, Medium, High
- Matches stakeholder mental model: "Does this project have significant external API exposure?"
- Different from Stories 4.1-4.3 which used continuous/linear scoring

**Total Endpoint Calculation:**

```csharp
var totalEndpoints = webApiEndpoints + webMethodEndpoints + wcfEndpoints;
var normalizedScore = CalculateApiExposureScore(totalEndpoints);

var metric = new ExternalApiMetric(
    ProjectName: project.ProjectName,
    ProjectPath: project.ProjectPath,
    EndpointCount: totalEndpoints,
    NormalizedScore: normalizedScore,
    ApiTypeBreakdown: new ApiTypeBreakdown(
        WebApiEndpoints: webApiEndpoints,
        WebMethodEndpoints: webMethodEndpoints,
        WcfEndpoints: wcfEndpoints));
```

🚨 **CRITICAL - Roslyn Workspace Management:**

**From Project Context (lines 287-289):**
> 🚨 MSBuild Locator - MUST BE FIRST:
> - `MSBuildLocator.RegisterDefaults()` MUST be called BEFORE any Roslyn types are loaded
> - Call it as first line in `Program.Main()` before DI container setup

**MSBuild Locator Already Registered:**

In Program.cs (line 13, from git history):
```csharp
public static async Task<int> Main(string[] args)
{
    MSBuildLocator.RegisterDefaults(); // FIRST LINE
    // ... rest of setup
}
```

Story 4.4 does NOT need to call RegisterDefaults() again - it's global initialization, already done.

**Workspace Disposal Pattern (Like Story 4.2):**

```csharp
public async Task<ExternalApiMetric> DetectAsync(
    ProjectNode project,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(project);

    _logger.LogInformation("Analyzing API exposure for project {ProjectName}", project.ProjectName);

    MSBuildWorkspace? workspace = null;

    try
    {
        // Create workspace
        workspace = MSBuildWorkspace.Create();

        // Load project
        var roslynProject = await workspace.OpenProjectAsync(project.ProjectPath, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // ... API detection logic ...

        return new ExternalApiMetric(/* ... */);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        // Fallback: Roslyn unavailable
        _logger.LogWarning("Could not analyze API exposure for {ProjectName}, defaulting to 0: {Reason}",
            project.ProjectName, ex.Message);

        return new ExternalApiMetric(
            project.ProjectName,
            project.ProjectPath,
            EndpointCount: 0,
            NormalizedScore: 0,
            ApiTypeBreakdown: new ApiTypeBreakdown(0, 0, 0));
    }
    finally
    {
        // CRITICAL: Dispose workspace
        workspace?.Dispose();
    }
}
```

**Why Async Workspace Operations:**

- `workspace.OpenProjectAsync()` is async I/O operation (reads project file, resolves references)
- `document.GetSemanticModelAsync()` is async compilation operation (may trigger background builds)
- Follows project convention: ALL I/O operations MUST be async
- Supports cancellation token for long-running analysis

**Memory Management:**

From project context (lines 312-317):
> 🚨 Memory Management:
> - Dispose `MSBuildWorkspace` ALWAYS - can hold gigabytes for large solutions
> - Roslyn semantic models are HEAVY - don't cache unnecessarily

MUST dispose workspace in finally block to prevent memory leaks.

### Technical Requirements

**New Namespace: MasDependencyMap.Core.ExtractionScoring (Established in Stories 4.1-4.3):**

Epic 4 uses the `ExtractionScoring` namespace created in Story 4.1.

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2
├── CycleAnalysis/               # Epic 3
├── Visualization/               # Epic 2, extended in Epic 3
└── ExtractionScoring/           # Epic 4
    ├── CouplingMetric.cs            # Story 4.1
    ├── ICouplingMetricCalculator.cs # Story 4.1
    ├── CouplingMetricCalculator.cs  # Story 4.1
    ├── ComplexityMetric.cs          # Story 4.2
    ├── IComplexityMetricCalculator.cs # Story 4.2
    ├── ComplexityMetricCalculator.cs # Story 4.2
    ├── CyclomaticComplexityWalker.cs # Story 4.2 (internal)
    ├── TechDebtMetric.cs            # Story 4.3
    ├── ITechDebtAnalyzer.cs         # Story 4.3
    ├── TechDebtAnalyzer.cs          # Story 4.3
    ├── ExternalApiMetric.cs         # Story 4.4 (THIS STORY)
    ├── IExternalApiDetector.cs      # Story 4.4 (THIS STORY)
    └── ExternalApiDetector.cs       # Story 4.4 (THIS STORY)
```

**ExternalApiMetric Model Pattern:**

Use C# 9+ `record` for immutable data (consistent with Stories 4.1-4.3):

```csharp
namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Breakdown of API endpoint counts by technology type.
/// </summary>
/// <param name="WebApiEndpoints">Count of modern ASP.NET Core Web API or legacy ASP.NET Web API endpoints (methods with HTTP verb attributes).</param>
/// <param name="WebMethodEndpoints">Count of legacy ASMX web service methods ([WebMethod] attributes).</param>
/// <param name="WcfEndpoints">Count of WCF service operation contracts ([OperationContract] attributes).</param>
public sealed record ApiTypeBreakdown(
    int WebApiEndpoints,
    int WebMethodEndpoints,
    int WcfEndpoints);

/// <summary>
/// Represents external API exposure metrics for a single project.
/// API exposure quantifies extraction difficulty based on public API surface area and external consumer dependencies.
/// More external APIs indicate harder extraction (breaking changes, testing complexity, migration coordination).
/// </summary>
/// <param name="ProjectName">Name of the project being analyzed.</param>
/// <param name="ProjectPath">Absolute path to the project file (.csproj or .vbproj).</param>
/// <param name="EndpointCount">Total count of external API endpoints detected (sum of all API types).</param>
/// <param name="NormalizedScore">API exposure score normalized to 0-100 scale. 0 = no APIs (easy to extract), 100 = many APIs (hard to extract).</param>
/// <param name="ApiTypeBreakdown">Detailed breakdown of endpoint counts by API technology type (WebAPI, WebMethod, WCF).</param>
public sealed record ExternalApiMetric(
    string ProjectName,
    string ProjectPath,
    int EndpointCount,
    double NormalizedScore,
    ApiTypeBreakdown ApiTypeBreakdown);
```

**IExternalApiDetector Interface Pattern:**

```csharp
namespace MasDependencyMap.Core.ExtractionScoring;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Detects external API exposure for a project by scanning for web service attributes using Roslyn semantic analysis.
/// API exposure measures extraction difficulty based on public API surface area (WebAPI, ASMX WebMethod, WCF).
/// Used to quantify extraction difficulty for migration planning.
/// </summary>
public interface IExternalApiDetector
{
    /// <summary>
    /// Detects external API exposure for a single project.
    /// Uses Roslyn semantic analysis to scan for API attributes across multiple technologies (WebAPI, ASMX, WCF).
    /// Falls back to 0 endpoints (no APIs) if Roslyn analysis unavailable.
    /// </summary>
    /// <param name="project">The project to analyze. Must not be null. ProjectPath must point to valid .csproj or .vbproj file.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// API exposure metrics including endpoint count and normalized 0-100 score.
    /// If Roslyn analysis fails, returns metric with EndpointCount=0, NormalizedScore=0 (conservative fallback).
    /// </returns>
    /// <exception cref="ArgumentNullException">When project is null.</exception>
    /// <exception cref="OperationCanceledException">When cancellation is requested.</exception>
    Task<ExternalApiMetric> DetectAsync(
        ProjectNode project,
        CancellationToken cancellationToken = default);
}
```

**Why Single Project Analysis (Like Stories 4.2 & 4.3)?**

- Story 4.1 (CouplingMetricCalculator): Analyzed ENTIRE graph at once → `Task<IReadOnlyList<CouplingMetric>>`
- Story 4.2 (ComplexityMetricCalculator): Analyzes ONE project at a time → `Task<ComplexityMetric>`
- Story 4.3 (TechDebtAnalyzer): Analyzes ONE project at a time → `Task<TechDebtMetric>`
- Story 4.4 (ExternalApiDetector): Analyzes ONE project at a time → `Task<ExternalApiMetric>`

**Reasoning:**
1. **API exposure is project-specific:** Each project has own API surface, independent of others
2. **No cross-project dependencies:** Unlike coupling (relative), API exposure is absolute per project
3. **Roslyn analysis per project:** Workspace loads one project at a time
4. **Story 4.5 orchestration:** ExtractionScoreCalculator will call this for each project in a loop

### Architecture Compliance

**Dependency Injection Registration:**

```csharp
// In Program.cs DI configuration
services.AddSingleton<IExternalApiDetector, ExternalApiDetector>();
```

**Lifetime:**
- Singleton: ExternalApiDetector is stateless (reads project files, analyzes code, no mutable state)
- Consistent with Stories 4.1-4.3 (all Epic 4 calculators/analyzers are singletons)

**Integration with Existing Components:**

Story 4.4 CONSUMES from Epic 2:
- ProjectNode (vertex type, contains ProjectPath to .csproj file)

Story 4.4 PRODUCES for Epic 4:
- ExternalApiMetric (model)
- IExternalApiDetector (service)
- Will be consumed by Story 4.5 (ExtractionScoreCalculator)

**File Naming and Structure:**

```
src/MasDependencyMap.Core/ExtractionScoring/
├── ExternalApiMetric.cs                   # One class per file, name matches class name
├── IExternalApiDetector.cs                # I-prefix for interfaces
└── ExternalApiDetector.cs                 # Descriptive implementation name
```

**Accessibility:**
- ExternalApiMetric: `public sealed record` (part of public API)
- ApiTypeBreakdown: `public sealed record` (part of public API)
- IExternalApiDetector: `public interface` (part of public API)
- ExternalApiDetector: `public class` (part of public API)

### Library/Framework Requirements

**Existing Roslyn Libraries (Already Installed in Story 4.2):**

All dependencies installed in Story 4.2 for cyclomatic complexity:
- ✅ Microsoft.CodeAnalysis.CSharp.Workspaces - Roslyn semantic analysis
- ✅ Microsoft.Build.Locator - MSBuild workspace integration

**Story 4.2 NuGet Packages (from git history):**

From `src/MasDependencyMap.Core/MasDependencyMap.Core.csproj`:
```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.8.0" />
<PackageReference Include="Microsoft.Build.Locator" Version="1.6.10" />
```

**No New NuGet Packages Required for Story 4.4** ✅

**Roslyn API Usage:**

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

// Load project with workspace
var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync(projectPath, cancellationToken);

// Get compilation
var compilation = await project.GetCompilationAsync(cancellationToken);

// Iterate documents
foreach (var document in project.Documents)
{
    var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
    var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
    var root = await syntaxTree.GetRootAsync(cancellationToken);

    // Find class declarations
    var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

    foreach (var classSyntax in classes)
    {
        // Get symbol from semantic model
        var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax);

        // Check attributes
        var hasApiController = classSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Mvc.ApiControllerAttribute");

        // ... detection logic ...
    }
}
```

**Key Roslyn Concepts:**

- **Workspace:** `MSBuildWorkspace` - Loads .csproj files and resolves references
- **Project:** `Microsoft.CodeAnalysis.Project` - Represents a .csproj with documents and compilation
- **Document:** Source file (.cs) in the project
- **SyntaxTree:** Parsed syntax representation of source code
- **SemanticModel:** Semantic information (types, symbols, resolved references)
- **Symbol:** `ISymbol` - Represents a declared entity (class, method, property, etc.)
- **Attribute:** `AttributeData` - Represents an attribute on a symbol

### File Structure Requirements

**New Files to Create:**

```
src/MasDependencyMap.Core/ExtractionScoring/
├── ExternalApiMetric.cs                          # NEW (includes ApiTypeBreakdown record)
├── IExternalApiDetector.cs                       # NEW
└── ExternalApiDetector.cs                        # NEW

tests/MasDependencyMap.Core.Tests/ExtractionScoring/
└── ExternalApiDetectorTests.cs                   # NEW

tests/MasDependencyMap.Core.Tests/TestData/ApiExposure/
├── NoApiProject/NoApiProject.csproj              # NEW (test fixture, no APIs)
├── NoApiProject/Program.cs                       # NEW (simple main, no API attributes)
├── WebApiProject/WebApiProject.csproj            # NEW (test fixture, modern Web API)
├── WebApiProject/UserController.cs               # NEW ([ApiController], [HttpGet], etc.)
├── WebMethodProject/WebMethodProject.csproj      # NEW (test fixture, legacy ASMX)
├── WebMethodProject/LegacyService.cs             # NEW ([WebMethod] attributes)
├── WcfProject/WcfProject.csproj                  # NEW (test fixture, WCF)
└── WcfProject/IUserService.cs                    # NEW ([ServiceContract], [OperationContract])
```

**Files to Modify:**

```
src/MasDependencyMap.CLI/Program.cs               # MODIFY: Add DI registration
_bmad-output/implementation-artifacts/sprint-status.yaml  # MODIFY: Update story status
```

**No Integration with CLI Commands Yet:**

Story 4.4 creates the detector but doesn't integrate it into CLI commands. That happens in Story 4.5 when all metrics are combined for extraction scoring.

For now:
- Create the service and register it in DI
- Full CLI integration happens in Epic 4 later stories
- Tests will validate functionality

### Testing Requirements

**Test Class: ExternalApiDetectorTests.cs**

**Test Strategy:**

Use integration testing with real test projects (like Story 4.2), NOT mocking Roslyn:
- Create test projects in TestData/ApiExposure/ directory
- Each test project represents a specific API scenario
- Tests load real .csproj files via ExternalApiDetector

**Test Projects Needed:**

```
tests/MasDependencyMap.Core.Tests/TestData/ApiExposure/
├── NoApiProject/              # No API attributes, simple console app
├── WebApiProject/             # Modern ASP.NET Core Web API (3 endpoints)
├── WebMethodProject/          # Legacy ASMX web services (2 WebMethods)
├── WcfProject/                # WCF service contract (5 operation contracts)
└── MixedApiProject/           # Mixed: 2 WebAPI + 3 WCF = 5 total endpoints
```

**Test Coverage Checklist:**

- ✅ Project with 0 API endpoints (score 0)
- ✅ Project with 3 endpoints (score 33, low exposure)
- ✅ Project with 10 endpoints (score 66, medium exposure)
- ✅ Project with 20 endpoints (score 100, high exposure)
- ✅ Modern Web API detection ([ApiController], [HttpGet])
- ✅ Legacy ASMX detection ([WebMethod])
- ✅ WCF service contract detection ([ServiceContract], [OperationContract])
- ✅ Mixed API types in same project (WebAPI + WCF)
- ✅ Roslyn unavailable fallback (invalid project path)
- ✅ Null project throws ArgumentNullException
- ✅ Cancellation support

**Sample Test Project (WebApiProject):**

```csharp
// tests/MasDependencyMap.Core.Tests/TestData/ApiExposure/WebApiProject/UserController.cs
using Microsoft.AspNetCore.Mvc;

namespace WebApiProject;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    [HttpGet]  // Endpoint 1
    public IActionResult GetAllUsers() => Ok(new[] { "user1", "user2" });

    [HttpGet("{id}")]  // Endpoint 2
    public IActionResult GetUser(int id) => Ok($"User {id}");

    [HttpPost]  // Endpoint 3
    public IActionResult CreateUser() => Ok();
}
```

This project should detect 3 WebAPI endpoints → total 3 endpoints → score 33 (low exposure).

**Sample Test:**

```csharp
[Fact]
public async Task DetectAsync_WebApiControllerWith3Endpoints_Returns33Score()
{
    // Arrange
    var testProjectPath = Path.Combine(TestContext.TestDataRoot, "ApiExposure", "WebApiProject", "WebApiProject.csproj");
    var project = new ProjectNode("WebApiProject", testProjectPath);

    // Act
    var metric = await _detector.DetectAsync(project);

    // Assert
    metric.EndpointCount.Should().Be(3);
    metric.NormalizedScore.Should().Be(33);
    metric.ApiTypeBreakdown.WebApiEndpoints.Should().Be(3);
    metric.ApiTypeBreakdown.WebMethodEndpoints.Should().Be(0);
    metric.ApiTypeBreakdown.WcfEndpoints.Should().Be(0);
}
```

### Previous Story Intelligence

**From Story 4.3 (Technology Version Debt Analyzer) - Patterns to Reuse:**

1. **Record Model Pattern:**
   ```csharp
   // Stories 4.1, 4.2, 4.3 used record models
   // Story 4.4 uses same pattern
   public sealed record ExternalApiMetric(...)
   public sealed record ApiTypeBreakdown(...)
   ```

2. **Analyzer Service Pattern:**
   ```csharp
   // Story 4.3: ITechDebtAnalyzer + TechDebtAnalyzer
   // Story 4.4: IExternalApiDetector + ExternalApiDetector (same pattern)
   // Interface + Implementation with ILogger injection
   ```

3. **DI Registration Pattern:**
   ```csharp
   // From Stories 4.1-4.3
   services.AddSingleton<ICouplingMetricCalculator, CouplingMetricCalculator>();
   services.AddSingleton<IComplexityMetricCalculator, ComplexityMetricCalculator>();
   services.AddSingleton<ITechDebtAnalyzer, TechDebtAnalyzer>();
   // Story 4.4
   services.AddSingleton<IExternalApiDetector, ExternalApiDetector>();
   ```

4. **Fallback Handling Pattern:**
   ```csharp
   // Story 4.2: Roslyn unavailable → score 50 (neutral)
   // Story 4.3: XML parsing failed → score 50 (neutral)
   // Story 4.4: Roslyn unavailable → score 0 (assume no APIs, conservative)
   // Fallback strategy: conservative underestimation
   ```

**From Story 4.2 (Cyclomatic Complexity Calculator) - Roslyn Patterns to Reuse:**

Story 4.2 already solved Roslyn workspace management. Story 4.4 reuses these patterns:

1. **MSBuildWorkspace Disposal:**
   ```csharp
   // Story 4.2 pattern
   MSBuildWorkspace? workspace = null;
   try
   {
       workspace = MSBuildWorkspace.Create();
       // ... analysis ...
   }
   finally
   {
       workspace?.Dispose();  // CRITICAL
   }
   ```

2. **Semantic Model Usage:**
   ```csharp
   // Story 4.2: Get method symbols for complexity analysis
   // Story 4.4: Get class/method/interface symbols for attribute detection
   var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
   var symbol = semanticModel.GetDeclaredSymbol(syntaxNode);
   ```

3. **Syntax Tree Traversal:**
   ```csharp
   // Story 4.2: DescendantNodes().OfType<MethodDeclarationSyntax>()
   // Story 4.4: DescendantNodes().OfType<ClassDeclarationSyntax>(), InterfaceDeclarationSyntax
   ```

4. **ConfigureAwait(false) Pattern:**
   ```csharp
   var project = await workspace.OpenProjectAsync(path, cancellationToken: cancellationToken)
       .ConfigureAwait(false);
   ```

5. **Test Data Organization:**
   ```csharp
   // Story 4.2: tests/TestData/HighComplexityTest/, SimpleComplexityTest/
   // Story 4.4: tests/TestData/ApiExposure/WebApiProject/, WebMethodProject/, etc.
   ```

**Key Differences from Story 4.2:**

| Aspect | Story 4.2 (Complexity) | Story 4.4 (API Exposure) |
|--------|------------------------|--------------------------|
| Syntax Nodes | MethodDeclarationSyntax | **ClassDeclarationSyntax, InterfaceDeclarationSyntax, MethodDeclarationSyntax** |
| Analysis Target | Method bodies (syntax walking) | **Attributes on classes/interfaces/methods (semantic)** |
| Symbol Types | IMethodSymbol | **INamedTypeSymbol (class/interface), IMethodSymbol** |
| Scoring | Linear normalization (0-100) | **Stepped thresholds (0, 33, 66, 100)** |
| Fallback Value | 50 (neutral, unknown complexity) | **0 (conservative, assume no APIs)** |
| Detection Count | Cyclomatic complexity sum | **Endpoint count (sum of API methods)** |

### Git Intelligence Summary

**Recent Commits Pattern:**

Last 5 commits show consistent code review process:
1. `4631a61` Code review fixes for Story 4-3: Implement technology version debt analyzer
2. `5911da1` Code review fixes for Story 4-2: Implement cyclomatic complexity calculator with Roslyn
3. `cc24f3c` Code review fixes for Story 4-1: Implement coupling metric calculator
4. `78f33d1` Code review fixes for Story 3-7: Mark suggested break points in YELLOW on visualizations
5. `d8166d9` Story 3-7 complete: Mark suggested break points in YELLOW on visualizations

**Pattern:** Initial commit → Code review → Fixes commit → Status update commit

**Expected Commit Sequence for Story 4.4:**

1. Initial commit: "Story 4-4 complete: Implement external API exposure detector"
2. Code review identifies 5-10 issues (based on Stories 4.1-4.3 pattern)
3. Fixes commit: "Code review fixes for Story 4-4: Implement external API exposure detector"
4. Status update: Update sprint-status.yaml from in-progress → review → done

**Expected File Changes for Story 4.4:**

Based on Stories 4.1-4.3 pattern:
- New: `src/MasDependencyMap.Core/ExtractionScoring/ExternalApiMetric.cs`
- New: `src/MasDependencyMap.Core/ExtractionScoring/IExternalApiDetector.cs`
- New: `src/MasDependencyMap.Core/ExtractionScoring/ExternalApiDetector.cs`
- New: `tests/MasDependencyMap.Core.Tests/ExtractionScoring/ExternalApiDetectorTests.cs`
- New: `tests/MasDependencyMap.Core.Tests/TestData/ApiExposure/` (multiple test projects)
- Modified: `src/MasDependencyMap.CLI/Program.cs` (DI registration)
- Modified: `_bmad-output/implementation-artifacts/sprint-status.yaml` (story status update)
- Modified: `_bmad-output/implementation-artifacts/4-4-implement-external-api-exposure-detector.md` (completion notes)

**Commit Message Pattern for Story Completion:**

```bash
git commit -m "Story 4-4 complete: Implement external API exposure detector

- Created ExternalApiMetric record model with endpoint count and API type breakdown
- Created ApiTypeBreakdown record for detailed API type counts (WebAPI, WebMethod, WCF)
- Created IExternalApiDetector interface for DI abstraction
- Implemented ExternalApiDetector with Roslyn semantic analysis
- Implemented Modern Web API detection: [ApiController], [HttpGet], [HttpPost], etc.
- Implemented Legacy ASMX detection: [WebMethod] attributes
- Implemented WCF service contract detection: [ServiceContract], [OperationContract]
- Implemented stepped scoring: 0 endpoints=0, 1-5=33, 6-15=66, 16+=100
- Implemented fallback handling: defaults to 0 when Roslyn unavailable (conservative)
- Added structured logging with named placeholders for API detection results
- Registered service in DI container as singleton
- Created comprehensive unit tests with 11 test cases across 5 test projects
- Tests validate WebAPI, ASMX, WCF detection, scoring thresholds, fallback, edge cases
- New files: ExternalApiMetric, IExternalApiDetector, ExternalApiDetector, ExternalApiDetectorTests
- All acceptance criteria satisfied
- Epic 4 Story 4.4 completes all four metrics (Coupling, Complexity, TechDebt, API Exposure)
- Ready for Story 4.5: Combined extraction score calculator

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 4.4 extends Epic 4 namespace created in Story 4.1, completing the four-metric foundation:

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2: Graph building
├── CycleAnalysis/               # Epic 3: Cycle detection
├── Visualization/               # Epic 2: DOT generation (extended in Epic 3)
└── ExtractionScoring/           # Epic 4: Extraction difficulty
    ├── CouplingMetric.cs            # Story 4.1
    ├── ICouplingMetricCalculator.cs # Story 4.1
    ├── CouplingMetricCalculator.cs  # Story 4.1
    ├── ComplexityMetric.cs          # Story 4.2
    ├── IComplexityMetricCalculator.cs # Story 4.2
    ├── ComplexityMetricCalculator.cs # Story 4.2
    ├── CyclomaticComplexityWalker.cs # Story 4.2 (internal)
    ├── TechDebtMetric.cs            # Story 4.3
    ├── ITechDebtAnalyzer.cs         # Story 4.3
    ├── TechDebtAnalyzer.cs          # Story 4.3
    ├── ExternalApiMetric.cs         # Story 4.4 (NEW)
    ├── IExternalApiDetector.cs      # Story 4.4 (NEW)
    └── ExternalApiDetector.cs       # Story 4.4 (NEW)
```

**Epic 4 Metric Foundation Complete:**

After Story 4.4, all four metrics are implemented:
1. ✅ Story 4.1: Coupling metrics (graph-based, relative)
2. ✅ Story 4.2: Complexity metrics (Roslyn-based, absolute thresholds)
3. ✅ Story 4.3: Tech debt metrics (XML-based, timeline scoring)
4. ✅ Story 4.4: API exposure metrics (Roslyn-based, stepped scoring)

**Next Story (4.5):** Combine all four metrics into final extraction difficulty score using configurable weights.

**Consistency with Existing Patterns:**
- ✅ Feature-based namespace (NOT layer-based)
- ✅ Interface + Implementation pattern (I-prefix interfaces)
- ✅ File naming matches class naming exactly
- ✅ Test namespace mirrors Core structure
- ✅ Service pattern with ILogger injection
- ✅ Singleton DI registration for stateless services
- ✅ Record model for immutable data
- ✅ Async methods with Async suffix and ConfigureAwait(false)
- ✅ Roslyn workspace disposal in finally block

**Cross-Namespace Dependencies:**
- ExtractionScoring → DependencyAnalysis (uses ProjectNode)
- This is expected and acceptable (Epic 4 builds on Epic 2 infrastructure)
- Same as Stories 4.1, 4.2, 4.3

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-4-extraction-difficulty-scoring-and-candidate-ranking.md, Story 4.4 (lines 59-80)]
- Story requirements: Roslyn semantic analysis scans for API attributes, detect WebMethod/ApiController/Route/ServiceContract, count endpoints, score 0/33/66/100, fallback to 0

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 18-51)]
- Technology stack: .NET 8.0, C# 12, Microsoft.CodeAnalysis.CSharp.Workspaces (Roslyn) already installed
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 54-85)]
- Language rules: Feature-based namespaces, async patterns, file-scoped namespaces, nullable reference types
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 120-125)]
- Roslyn usage: MSBuildWorkspace for solution loading, semantic analysis via GetSemanticModelAsync(), dispose workspaces
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 261-268)]
- MSBuild Locator: RegisterDefaults() MUST be called BEFORE Roslyn types loaded (already done in Program.cs)
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 312-317)]
- Memory management: Dispose MSBuildWorkspace ALWAYS, Roslyn semantic models are HEAVY

**Architecture:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md (lines 156-181)]
- Dependency injection: Full DI throughout Core and CLI, constructor injection, interface-based design
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\implementation-patterns-consistency-rules.md (lines 9-19)]
- Namespace organization: Feature-based, not layer-based (MasDependencyMap.Core.ExtractionScoring)

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-2-implement-cyclomatic-complexity-calculator-with-roslyn.md (full file)]
- Roslyn patterns: MSBuildWorkspace creation/disposal, semantic model usage, syntax tree traversal, ConfigureAwait(false)
- Test data organization: TestData/ directory with real test projects
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-3-implement-technology-version-debt-analyzer.md (full file)]
- Record model pattern, analyzer interface/implementation pattern, DI registration, fallback handling
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-1-implement-coupling-metric-calculator.md]
- Epic 4 namespace establishment, calculator patterns, singleton registration

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Code review pattern: Initial commit → Code review fixes (5-10 issues) → Status update
- Recent pattern: Stories 4.1, 4.2, 4.3 all had code review fixes applied after initial implementation
- File change pattern: New ExtractionScoring classes, tests, DI registration, sprint-status.yaml update

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
