# MVP Scope

## Core Features

The MVP encompasses the complete 12-week implementation roadmap delivering a Roslyn-based console tool with Graphviz visualization that transforms dependency graphs into actionable migration intelligence.

**Phase 1: Noise Filtering Foundation (Weeks 1-4)**

**Roslyn Solution Loader:**
- .NET 6+ console application that accepts .sln file paths as arguments
- MSBuildWorkspace integration to load solutions and enumerate projects
- Multi-solution support for all 20 solutions simultaneously
- Handles missing SDKs and build errors gracefully with MSBuild fallback

**Framework Dependency Filter:**
- Configurable JSON-based filter with blocklist patterns
- Automatic exclusion of Microsoft.*, System.*, mscorlib, netstandard dependencies
- Allowlist detection to identify "your code" by path or naming convention
- Clear differentiation between custom assemblies and framework dependencies

**DOT Format Generator:**
- Converts filtered dependency graph to Graphviz DOT format
- Generates {solution-name}-dependencies.dot files
- Color coding by solution for visual differentiation
- Cross-solution dependency highlighting
- PNG/SVG export via Graphviz command-line tools

**Success Criteria:** Can visualize dependencies for any solution with single command line invocation, framework noise eliminated, graph shows only custom code

---

**Phase 2: Cycle Detection Intelligence (Weeks 5-8)**

**Graph Model & Cycle Detection:**
- QuikGraph-based directed graph data structure (nodes = projects, edges = references)
- Tarjan's strongly connected components (SCC) algorithm implementation
- Comprehensive cycle detection across entire 20-solution ecosystem
- Cycle statistics: total cycles, average cycle size, projects involved

**Enhanced DOT Visualization:**
- Cycle highlighting in RED color with non-cyclic dependencies in GRAY
- Edge labels showing dependency type (project reference vs DLL reference)
- Suggested break points marked in YELLOW
- Legend explaining color coding

**Cycle-Breaking Analysis:**
- Roslyn semantic analysis to count method calls across dependency boundaries
- Edge "weight" calculation based on coupling strength
- Ranked list of weakest links: "To break Cycle #3, consider removing ProjectA → ProjectB (23 method calls)"
- CSV export of cycle-breaking suggestions for tracking

**Report Generation:**
- Comprehensive text report: {solution}-analysis-report.txt
- Summary statistics: total projects, total cycles, cycle participation rates
- Detailed CSV: project name, incoming refs, outgoing refs, cycle count, suggested breaks
- Migration sequencing suggestions based on cycle analysis

**Success Criteria:** All circular dependency chains identified, visual graph shows cycles with suggested break points, ranked list of easiest dependencies to break

---

**Phase 3: Extraction Difficulty Scoring (Weeks 9-12)**

**Metric Collection:**
- Coupling score calculator: count incoming/outgoing references from graph model
- Roslyn SyntaxWalker for cyclomatic complexity calculation across all classes
- .csproj file parsing to detect technology version (.NET 3.5, COM+, etc.)
- Web service indicator scanning ([WebMethod], [ApiController], WCF configs) for external exposure
- Raw metrics output: ProjectX: Coupling=12, Complexity=450, Tech=NET35, External=Yes

**Scoring Algorithm:**
- 0-100 "Extraction Difficulty Score" calculation formula
- Configurable weights: coupling (40%), complexity (30%), tech debt (20%), external exposure (10%)
- Ranked CSV export: project, score, coupling, complexity, tech_version, external_apis
- Sort by score ascending (lowest score = easiest to extract first)

**Enhanced DOT Visualization:**
- Node labels with extraction difficulty scores: "ProjectX\n(Score: 23)"
- Color coding by score range: GREEN (0-33 easy), YELLOW (34-66 medium), RED (67-100 hard)
- Size scaling: larger nodes indicate higher complexity
- Visual "heat map" showing extraction difficulty across ecosystem

**Validation & Tuning:**
- Manual review of top 10 easiest candidates against architectural judgment
- Manual review of bottom 10 hardest candidates for complexity validation
- Scoring weight tuning based on validation results
- Confident ranked list ready for stakeholder review

**Success Criteria:** Every project has extraction difficulty score with supporting metrics, top 10 easiest candidates align with architectural intuition, visual heat map provides immediate focus areas, exportable data suitable for stakeholder reports

---

## Technical Architecture

**Technology Stack:**
- .NET 6+ SDK
- Microsoft.CodeAnalysis.CSharp.Workspaces (Roslyn)
- Microsoft.Build.Locator for MSBuild integration
- QuikGraph for graph algorithms
- Graphviz for visualization rendering

**Output Formats:**
- DOT files for dependency graphs
- PNG/SVG visualizations via Graphviz
- TXT reports for comprehensive analysis
- CSV exports for data analysis and tracking

**Design Philosophy:**
- Console application for fast time-to-insight (weeks, not months)
- Single command execution to analyze entire ecosystem
- Configurable parameters via JSON configuration files
- Extensible architecture for future enhancements

---

## Out of Scope for MVP

The following features are explicitly deferred to post-MVP releases to maintain focus on core migration intelligence:

**Phase 4 Enhancements (Future Releases):**

**Web Dashboard:**
- Interactive web UI with Cytoscape.js for graph exploration
- Real-time filtering and drill-down capabilities
- Collaborative sharing of analysis results
- Browser-based accessibility without Graphviz installation

**What-If Simulator:**
- Interactive impact analysis: "What if I break this dependency?"
- Cascading effect visualization
- Migration scenario comparison
- Risk assessment for proposed changes

**Advanced Dependency Analysis:**
- Stored procedure dependency analysis (parse SQL for table usage patterns)
- COM+ component reverse engineering and interface mapping
- Dynamic loading and reflection-based dependency detection
- External API contract analysis beyond basic scanning

**IDE Integration:**
- Visual Studio extension for in-IDE dependency exploration
- Real-time dependency impact hints during development
- Integration with Solution Explorer and Architecture Explorer
- Context menu actions for quick dependency queries

**Advanced Reporting:**
- Automated stakeholder presentation generation
- Progress tracking dashboards showing migration over time
- Velocity correlation analysis (before/after extraction metrics)
- Custom report templates for different audiences

**Scalability Enhancements:**
- Incremental analysis (only re-analyze changed solutions)
- Distributed processing for very large codebases
- Caching and persistence for faster subsequent runs
- Cloud-based analysis for team collaboration

---

## MVP Success Criteria

The MVP is considered successful when it achieves the following outcomes:

**Tool Delivery:**
- ✅ Complete 12-week implementation roadmap delivered on schedule
- ✅ Console tool successfully analyzes all 20 solutions without manual intervention
- ✅ Filtered dependency graphs are readable and actionable (not "visual spaghetti")
- ✅ All three core capabilities functional: filtering, cycle detection, extraction scoring

**Validation & Accuracy:**
- ✅ Framework filtering removes 95%+ of Microsoft/.NET dependencies accurately
- ✅ Cycle detection identifies all known circular dependencies (100% coverage)
- ✅ Top 10 extraction candidates align with architect's manual assessment (80%+ agreement)
- ✅ Extraction difficulty scores match architectural intuition when validated

**User Value Creation:**
- ✅ Week 3: Alex can visualize complete architecture with framework noise removed
- ✅ Week 8: Cycle-breaking recommendations align with architectural judgment
- ✅ Week 12: Confident ranked list of migration candidates ready for stakeholder review
- ✅ Time savings achieved: migration planning reduced from weeks to hours

**Business Decision Enablement:**
- ✅ Data-driven migration roadmap presented to stakeholders with supporting metrics
- ✅ Can confidently answer "where do we start?" with quantified recommendations
- ✅ Migration decisions backed by tool metrics instead of guesswork
- ✅ Executive approval secured for migration plan based on tool insights

**Go/No-Go Decision Points:**

**Week 4 Checkpoint:**
- If filtered graphs still show "visual spaghetti," framework filtering needs refinement
- If graphs clearly show architecture, proceed to Phase 2

**Week 8 Checkpoint:**
- If cycle detection misses known dependencies, algorithm needs adjustment
- If cycle-breaking suggestions align with judgment, proceed to Phase 3

**Week 12 Checkpoint:**
- If top 10 candidates don't align with intuition (< 80% agreement), scoring weights need tuning
- If alignment is good (≥ 80%), MVP is successful and ready for first extraction execution

---

## Future Vision

**2-3 Year Product Evolution:**

If masDependencyMap proves successful in guiding safe legacy modernization, it evolves from a single-purpose migration planning tool into a comprehensive **Legacy Modernization Intelligence Platform**.

**Expansion Scenarios:**

**Platform Expansion:**
- Support additional languages and platforms (Java, Python, JavaScript/TypeScript monoliths)
- Cloud-native architecture analysis (Kubernetes dependencies, service mesh complexity)
- Multi-repository analysis for microservices ecosystems
- Cross-language dependency tracking (polyglot architectures)

**Team & Enterprise Features:**
- Multi-user collaboration with shared analysis workspaces
- Role-based access control (architects, developers, stakeholders each see relevant views)
- Integration with project management tools (Jira, Azure DevOps) for migration tracking
- Continuous analysis pipeline integrated with CI/CD

**AI/ML Enhancements:**
- Machine learning model trained on successful vs. failed extractions
- Predictive analytics for extraction effort estimation
- Automated refactoring suggestions based on pattern recognition
- Natural language querying: "Which components interact with the payment system?"

**Market Expansion:**

**Target Markets:**
- Enterprise software companies with legacy .NET monoliths (primary market)
- Consulting firms specializing in legacy modernization (tool-as-a-service)
- Private equity firms evaluating technical debt in acquisition targets
- Government agencies modernizing mission-critical legacy systems

**Business Model Evolution:**
- MVP: Internal tool for personal use (no monetization)
- Year 1: Open source with commercial support and consulting services
- Year 2: SaaS offering with tiered pricing (per-codebase analysis limits)
- Year 3: Enterprise platform with team collaboration and continuous analysis

**Ecosystem Integration:**
- Marketplace for custom scoring algorithms and analysis plugins
- Integration with architecture documentation tools (C4 model, Arc42)
- Partnership with migration service providers
- Community-contributed language and platform analyzers

**Long-Term Vision:**

masDependencyMap becomes the **de facto standard** for data-driven legacy modernization planning, replacing manual analysis and guesswork with automated intelligence. Every major legacy modernization initiative starts with masDependencyMap analysis to identify safe starting points and track progress over time.

The tool evolves from answering "where do we start?" to answering "how do we continuously evolve our architecture to maintain business agility?" - shifting from one-time migration planning to ongoing architectural health monitoring.
