# Epic List

## Epic 1: Project Foundation and Command-Line Interface

Architects can install the tool, configure it via JSON files, run analysis commands with various options, and access comprehensive documentation to get started quickly.

**FRs covered:** FR44-FR55, FR61-FR65 (Command-line interface, Configuration, Documentation)

**Architecture Requirements:**
- Starter template initialization (.NET 8 multi-project structure)
- MasDependencyMap.Core (class library) and MasDependencyMap.CLI (console app) setup
- System.CommandLine v2.0.2 for argument parsing
- Spectre.Console v0.54.0 for rich console UI
- Microsoft.Extensions.Configuration for JSON configuration management
- Microsoft.Extensions.DependencyInjection for DI container setup
- Project references and NuGet package installation
- Sample solution for testing
- README, user guide, configuration guide documentation

**NFRs Addressed:** NFR15 (15-minute time-to-first-graph), NFR16 (help documentation), NFR20 (standard CLI conventions), NFR23-NFR24 (JSON configuration without code changes)

## Epic 2: Solution Loading and Dependency Discovery

Architects can load .NET solutions (3.5 through .NET 8+), automatically filter out framework noise, and visualize clean dependency graphs showing only custom code with cross-solution dependencies highlighted by color.

**FRs covered:** FR1-FR19 (Solution loading, Framework filtering, Dependency visualization)

**Architecture Requirements:**
- Roslyn (Microsoft.CodeAnalysis.CSharp.Workspaces) integration for semantic analysis
- Microsoft.Build.Locator for MSBuild workspace integration
- QuikGraph v2.5.0 for graph data structures
- 3-layer fallback strategy: RoslynSolutionLoader → MSBuildSolutionLoader → ProjectFileSolutionLoader
- IFrameworkFilter with JSON blocklist/allowlist pattern matching
- DependencyGraphBuilder using QuikGraph
- Graphviz integration via Process.Start with IGraphvizRenderer abstraction
- DOT generation via QuikGraph.Graphviz extension
- PNG/SVG rendering via external Graphviz process

**NFRs Addressed:** NFR1 (5-minute analysis for 50 projects), NFR2 (30-minute for 400+ projects), NFR3 (30-second graph rendering), NFR8 (Graphviz detection), NFR28-NFR33 (External tool integration)

## Epic 3: Circular Dependency Detection and Break-Point Analysis

Architects can identify all circular dependency chains in their solution ecosystem, see coupling strength analysis via method call counting, and get ranked recommendations for which dependencies to break first.

**FRs covered:** FR20-FR26 (Cycle detection and analysis)

**Architecture Requirements:**
- TarjanCycleDetector using QuikGraph's Tarjan's SCC algorithm
- CouplingAnalyzer for method call counting across dependency edges
- Enhanced DOT visualization with RED cycle highlighting and YELLOW break point marking
- Cycle statistics calculation (total cycles, sizes, project participation rates)
- Ranked cycle-breaking recommendations based on weakest coupling edges

**NFRs Addressed:** NFR5 (memory <4GB), NFR31 (QuikGraph integration)

## Epic 4: Extraction Difficulty Scoring and Candidate Ranking

Architects can get 0-100 extraction difficulty scores for every project based on coupling, complexity, tech debt, and external API exposure, with heat map visualization and ranked extraction candidate lists to confidently answer "where do we start?"

**FRs covered:** FR27-FR35 (Extraction difficulty scoring)

**Architecture Requirements:**
- ExtractionScoreCalculator with configurable JSON weights
- CouplingMetricCalculator (incoming/outgoing reference counts)
- ComplexityMetricCalculator using Roslyn semantic analysis for cyclomatic complexity
- TechDebtAnalyzer for .NET version detection from project files
- ExternalApiDetector scanning for WebMethod, ApiController attributes
- Heat map visualization: GREEN (0-33 easy), YELLOW (34-66 medium), RED (67-100 hard)
- Ranked extraction candidate lists sorted by difficulty score

**NFRs Addressed:** NFR23 (configurable scoring weights), NFR30 (Roslyn integration)

## Epic 5: Comprehensive Reporting and Data Export

Architects can generate stakeholder-ready text reports with summary statistics, export data to CSV for external analysis tools, and share migration insights with executives and team members.

**FRs covered:** FR36-FR43 (Report generation and export)

**Architecture Requirements:**
- TextReportGenerator using Spectre.Console.Table for formatted output
- CsvExporter using CsvHelper library for RFC 4180 compliance
- Export models: ExtractionScoreRecord, CycleAnalysisRecord, DependencyRecord
- CSV column naming: Title Case with Spaces for Excel/Google Sheets compatibility
- Analysis reports with cycle-breaking recommendations and rationale
- Top 10 easiest/hardest extraction candidate reporting

**NFRs Addressed:** NFR6 (CSV export <10 seconds), NFR18 (stakeholder-ready formatting), NFR34 (RFC 4180 CSV compliance)

## Epic 6: Robust Error Handling and Progress Feedback

Architects get clear, actionable error messages with remediation steps, visual progress indicators with percentage completion and ETA, and graceful degradation when analysis issues occur.

**FRs covered:** FR56-FR60 (Error handling and progress feedback)

**Architecture Requirements:**
- Custom exception hierarchy: SolutionLoadException, GraphvizException, ConfigurationException
- Spectre.Console markup for 3-part error messages (Error/Reason/Suggestion)
- Microsoft.Extensions.Logging with structured logging templates
- Progress indicators via Spectre.Console.Progress with percentage and ETA
- Fallback chain exception handling with clear logging at each level
- Configuration JSON validation with syntax error reporting

**NFRs Addressed:** NFR4 (progress for 10+ second operations), NFR7-NFR13 (Reliability: graceful failures, clear errors, fallback strategies), NFR14 (actionable error messages), NFR17 (progress with percentage/ETA)
