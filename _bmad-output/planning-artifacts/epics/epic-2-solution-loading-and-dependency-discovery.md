# Epic 2: Solution Loading and Dependency Discovery

Architects can load .NET solutions (3.5 through .NET 8+), automatically filter out framework noise, and visualize clean dependency graphs showing only custom code with cross-solution dependencies highlighted by color.

## Story 2.1: Implement Solution Loader Interface and Roslyn Loader

As an architect,
I want to load a single .NET solution file using Roslyn semantic analysis,
So that I can extract project references and dependencies.

**Acceptance Criteria:**

**Given** I have a valid .sln file path
**When** RoslynSolutionLoader.LoadAsync() is called
**Then** MSBuildWorkspace loads the solution with Microsoft.Build.Locator integration
**And** A SolutionAnalysis object is returned containing ProjectInfo for each project (name, path, target framework)
**And** Project references are extracted and stored in the SolutionAnalysis
**And** DLL references are extracted and differentiated from project references
**And** Mixed C#/VB.NET projects are handled correctly
**And** .NET Framework 3.5+ and .NET Core/5/6/7/8+ projects are supported

**Given** Roslyn fails to load the solution
**When** MSBuildWorkspace throws an exception
**Then** RoslynLoadException is thrown with clear error message

## Story 2.2: Implement MSBuild Fallback Loader

As an architect,
I want MSBuild-based solution loading as a fallback when Roslyn fails,
So that I can still analyze solutions that don't support full semantic analysis.

**Acceptance Criteria:**

**Given** Roslyn semantic analysis fails for a solution
**When** MSBuildSolutionLoader.LoadAsync() is called
**Then** Solution is loaded via MSBuild workspace without full semantic analysis
**And** Project references are extracted from .csproj/.vbproj files
**And** DLL references are extracted from project files
**And** A SolutionAnalysis object is returned with available project information
**And** ILogger logs a warning that Roslyn failed and MSBuild fallback was used

**Given** MSBuild also fails
**When** MSBuild workspace throws an exception
**Then** MSBuildLoadException is thrown with clear error message

## Story 2.3: Implement Project File Fallback Loader

As an architect,
I want direct .csproj/.vbproj XML parsing as the last fallback,
So that analysis continues even when MSBuild fails.

**Acceptance Criteria:**

**Given** Both Roslyn and MSBuild loaders have failed
**When** ProjectFileSolutionLoader.LoadAsync() is called
**Then** Solution file is parsed to extract project paths
**And** Each .csproj/.vbproj file is parsed as XML to extract ProjectReference elements
**And** PackageReference and Reference elements are extracted for DLL references
**And** A basic SolutionAnalysis object is returned with project dependency information
**And** Missing SDK references are handled gracefully without crashing
**And** ILogger logs a warning that both Roslyn and MSBuild failed, using project file parsing

## Story 2.4: Implement Strategy Pattern Fallback Chain

As an architect,
I want automatic fallback from Roslyn → MSBuild → ProjectFile loaders,
So that I get the best available analysis for any solution.

**Acceptance Criteria:**

**Given** I have registered all three loader implementations in DI
**When** ISolutionLoader is resolved and LoadAsync() is called
**Then** RoslynSolutionLoader attempts loading first
**And** If RoslynLoadException is thrown, MSBuildSolutionLoader is tried next
**And** If MSBuildLoadException is thrown, ProjectFileSolutionLoader is tried last
**And** Each fallback is logged with structured logging showing the failure reason
**And** If all three loaders fail, a comprehensive error message shows all failure reasons
**And** Partial success (e.g., 45/50 projects loaded) is reported via progress indicators

## Story 2.5: Build Dependency Graph with QuikGraph

As an architect,
I want SolutionAnalysis converted into a QuikGraph dependency graph,
So that I can perform graph algorithms for cycle detection.

**Acceptance Criteria:**

**Given** A SolutionAnalysis object with project dependencies
**When** DependencyGraphBuilder.BuildAsync() is called
**Then** A DependencyGraph (QuikGraph wrapper) is created with ProjectNode vertices
**And** DependencyEdge edges connect projects based on project references
**And** Edge type (ProjectReference vs. BinaryReference) is stored on each edge
**And** Multi-solution analysis creates a unified graph with all projects across solutions
**And** Cross-solution dependencies are marked with source solution identifier
**And** The graph structure is validated (no orphaned nodes, all references accounted for)

## Story 2.6: Implement Framework Dependency Filter

As an architect,
I want to filter out Microsoft.*/System.* framework dependencies from the graph,
So that I see only custom code architecture.

**Acceptance Criteria:**

**Given** A DependencyGraph with framework and custom dependencies
**When** FrameworkFilter.FilterAsync() is called with BlockList patterns from configuration
**Then** All edges to projects matching Microsoft.*, System.*, mscorlib, netstandard are removed
**And** Projects matching AllowList patterns (e.g., YourCompany.*) are retained
**And** The filtered graph contains only custom code dependencies
**And** ILogger logs the count of filtered dependencies (e.g., "Filtered 2,847 framework refs, retained 412 custom refs")
**And** Filter rules are loaded from filter-config.json with PascalCase property names

## Story 2.7: Implement Graphviz Detection and Installation Validation

As an architect,
I want the tool to detect if Graphviz is installed,
So that I get clear installation guidance if it's missing.

**Acceptance Criteria:**

**Given** Graphviz is not installed or not in PATH
**When** GraphvizRenderer.IsGraphvizInstalled() is called
**Then** The method returns false
**And** When rendering is attempted, GraphvizNotFoundException is thrown
**And** Error message uses Spectre.Console markup with Error/Reason/Suggestion format
**And** Suggestion includes installation instructions (Windows: choco install graphviz, or download from graphviz.org)
**And** ILogger logs the detection failure

**Given** Graphviz is installed and in PATH
**When** GraphvizRenderer.IsGraphvizInstalled() is called
**Then** The method returns true by executing `dot -version` successfully
**And** The Graphviz version is logged

## Story 2.8: Generate DOT Format from Dependency Graph

As an architect,
I want the dependency graph exported to Graphviz DOT format,
So that I can render visualizations.

**Acceptance Criteria:**

**Given** A filtered DependencyGraph
**When** DotGenerator.GenerateAsync() is called
**Then** A .dot file is created with Graphviz 2.38+ compatible syntax
**And** Each ProjectNode is rendered with a node label showing project name
**And** DependencyEdge edges are rendered with arrows showing dependency direction
**And** Cross-solution dependencies use different colors per solution
**And** The DOT file is named {SolutionName}-dependencies.dot in the output directory
**And** Manual editing is not required (file is directly usable by Graphviz)

## Story 2.9: Render DOT Files to PNG and SVG with Graphviz

As an architect,
I want DOT files rendered to PNG and SVG images,
So that I can view dependency graphs visually.

**Acceptance Criteria:**

**Given** A .dot file exists and Graphviz is installed
**When** GraphvizRenderer.RenderToFileAsync() is called with OutputFormat.Png
**Then** Process.Start() invokes `dot -Tpng input.dot -o output.png`
**And** PNG file is created in the output directory within 30 seconds
**And** The PNG shows the dependency graph with node labels and colored edges

**When** GraphvizRenderer.RenderToFileAsync() is called with OutputFormat.Svg
**Then** Process.Start() invokes `dot -Tsvg input.dot -o output.svg`
**And** SVG file is created in the output directory
**And** The SVG is a scalable vector graphic suitable for zooming

**And** Rendering works on Windows (dot.exe), Linux, and macOS (dot) via platform-agnostic Process.Start

## Story 2.10: Support Multi-Solution Analysis

As an architect,
I want to analyze multiple solution files simultaneously,
So that I can see cross-solution dependencies across my entire ecosystem.

**Acceptance Criteria:**

**Given** I provide multiple .sln file paths via --solutions parameter
**When** The analyze command processes all solutions
**Then** Each solution is loaded sequentially with progress indicators
**And** A unified DependencyGraph is built containing projects from all solutions
**And** Cross-solution dependencies are identified and marked
**And** Each solution's projects are color-coded differently in the visualization
**And** The output file is named with a combined identifier (e.g., Ecosystem-dependencies.dot)
**And** Progress shows "Loading solutions: 15/20 (75%)" with ETA
