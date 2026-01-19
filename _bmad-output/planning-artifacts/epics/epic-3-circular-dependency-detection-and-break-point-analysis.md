# Epic 3: Circular Dependency Detection and Break-Point Analysis

Architects can identify all circular dependency chains in their solution ecosystem, see coupling strength analysis via method call counting, and get ranked recommendations for which dependencies to break first.

## Story 3.1: Implement Tarjan's SCC Algorithm for Cycle Detection

As an architect,
I want to detect all circular dependency chains using Tarjan's strongly connected components algorithm,
So that I can identify which projects are involved in cycles.

**Acceptance Criteria:**

**Given** A filtered DependencyGraph with circular dependencies
**When** TarjanCycleDetector.DetectCyclesAsync() is called
**Then** QuikGraph's Tarjan's SCC algorithm identifies all strongly connected components
**And** Each SCC with more than 1 project is identified as a circular dependency cycle
**And** CycleInfo objects are created for each cycle containing the list of projects involved
**And** Cycle statistics are calculated: total cycles, largest cycle size, total projects in cycles
**And** ILogger logs "Found 12 circular dependency chains, 45 projects (61.6%) involved in cycles"

**Given** A graph with no cycles
**When** TarjanCycleDetector.DetectCyclesAsync() is called
**Then** An empty list of CycleInfo objects is returned
**And** ILogger logs "No circular dependencies detected"

## Story 3.2: Calculate Cycle Statistics and Participation Rates

As an architect,
I want detailed statistics about circular dependencies,
So that I understand the scale of the cycle problem in my codebase.

**Acceptance Criteria:**

**Given** Cycles have been detected
**When** Cycle statistics are calculated
**Then** Total number of circular dependency chains is reported
**And** Largest cycle size (number of projects in the biggest cycle) is identified
**And** Cycle participation rate is calculated (percentage of projects involved in cycles)
**And** Each cycle's size is stored in the CycleInfo object
**And** Statistics are included in text reports: "Circular Dependency Chains: 12, Projects in Cycles: 45 (61.6%), Largest Cycle Size: 8 projects"

## Story 3.3: Implement Coupling Strength Analysis via Method Call Counting

As an architect,
I want coupling strength measured by counting method calls across dependency edges,
So that I can identify weak vs. strong coupling.

**Acceptance Criteria:**

**Given** A DependencyGraph with project dependencies
**When** CouplingAnalyzer.AnalyzeAsync() is called
**Then** Roslyn semantic analysis counts method calls from one project to another
**And** Each DependencyEdge is annotated with a coupling score (number of method calls)
**And** Edges with 1-5 method calls are classified as "weak coupling"
**And** Edges with 6-20 method calls are classified as "medium coupling"
**And** Edges with 21+ method calls are classified as "strong coupling"
**And** ILogger logs coupling analysis progress for large solutions

**Given** Roslyn semantic analysis is unavailable
**When** CouplingAnalyzer.AnalyzeAsync() is called
**Then** Coupling defaults to reference count (1 per reference) as a fallback
**And** ILogger logs a warning that semantic analysis was unavailable for coupling

## Story 3.4: Identify Weakest Coupling Edges Within Cycles

As an architect,
I want to identify the weakest coupling edges within circular dependencies,
So that I know which dependencies are easiest to break.

**Acceptance Criteria:**

**Given** Cycles have been detected and coupling analysis is complete
**When** Weak coupling edges within cycles are identified
**Then** For each cycle, the edge with the lowest coupling score is flagged
**And** Multiple weak edges (tied for lowest score) are all flagged
**And** Weak edges are stored in CycleInfo objects with their coupling scores
**And** ILogger logs "Identified 18 weak coupling edges across 12 cycles (avg 1.5 per cycle)"

## Story 3.5: Generate Ranked Cycle-Breaking Recommendations

As an architect,
I want ranked recommendations for which dependencies to break first,
So that I can make data-driven decisions about cycle resolution.

**Acceptance Criteria:**

**Given** Weak coupling edges have been identified
**When** Cycle-breaking recommendations are generated
**Then** CycleBreakingSuggestion objects are created for each weak edge
**And** Suggestions include: source project, target project, coupling score, rationale
**And** Rationale explains why this edge is recommended (e.g., "Weakest link in 8-project cycle, only 3 method calls")
**And** Suggestions are ranked by coupling score (lowest first)
**And** Top 5 cycle-breaking recommendations are prominently featured in reports

## Story 3.6: Enhance DOT Visualization with Cycle Highlighting

As an architect,
I want circular dependencies highlighted in RED on dependency graphs,
So that I can visually identify problematic areas.

**Acceptance Criteria:**

**Given** Cycles have been detected in the dependency graph
**When** DotGenerator.GenerateAsync() is called with cycle information
**Then** Edges that are part of circular dependencies are rendered in RED
**And** Edges not in cycles remain in default color (black or gray)
**And** The visual distinction between cyclic and non-cyclic edges is clear
**And** Multi-cycle scenarios show all cyclic edges in RED
**And** Graph legend includes "Red: Circular Dependencies" notation

## Story 3.7: Mark Suggested Break Points in YELLOW on Visualizations

As an architect,
I want suggested cycle break points marked in YELLOW on dependency graphs,
So that I can immediately see where to focus my refactoring efforts.

**Acceptance Criteria:**

**Given** Cycle-breaking recommendations have been generated
**When** DotGenerator.GenerateAsync() is called with recommendations
**Then** Edges identified as break point suggestions are rendered in YELLOW
**And** YELLOW edges represent the weakest coupling within cycles
**And** If an edge is both cyclic (RED) and a break suggestion, it renders as YELLOW (suggestion takes priority)
**And** Graph legend includes "Yellow: Suggested Break Points" notation
**And** Top 10 suggested break points are marked (not all weak edges, to avoid visual clutter)
**And** The visualization clearly guides the architect to specific edges that should be broken
