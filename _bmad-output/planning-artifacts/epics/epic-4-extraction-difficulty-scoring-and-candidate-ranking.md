# Epic 4: Extraction Difficulty Scoring and Candidate Ranking

Architects can get 0-100 extraction difficulty scores for every project based on coupling, complexity, tech debt, and external API exposure, with heat map visualization and ranked extraction candidate lists to confidently answer "where do we start?"

## Story 4.1: Implement Coupling Metric Calculator

As an architect,
I want coupling metrics calculated for each project (incoming/outgoing reference counts),
So that I can quantify how connected each project is to others.

**Acceptance Criteria:**

**Given** A DependencyGraph with all project dependencies
**When** CouplingMetricCalculator.CalculateAsync() is called for a project
**Then** Incoming reference count is calculated (how many projects depend on this one)
**And** Outgoing reference count is calculated (how many projects this one depends on)
**And** Total coupling score is calculated as: (incoming * 2) + outgoing (incoming weighted higher)
**And** Coupling metric is normalized to 0-100 scale for scoring algorithm
**And** ILogger logs coupling calculation progress for large solutions

## Story 4.2: Implement Cyclomatic Complexity Calculator with Roslyn

As an architect,
I want cyclomatic complexity calculated for each project using Roslyn semantic analysis,
So that I can measure code complexity as part of extraction difficulty.

**Acceptance Criteria:**

**Given** A project with source code available
**When** ComplexityMetricCalculator.CalculateAsync() is called
**Then** Roslyn semantic analysis walks all method syntax trees
**And** Cyclomatic complexity is calculated for each method (branching statements, loops, conditionals)
**And** Average complexity per method is calculated for the project
**And** Complexity metric is normalized to 0-100 scale (higher = more complex = harder to extract)
**And** ILogger logs complexity calculation progress

**Given** Roslyn semantic analysis is unavailable
**When** ComplexityMetricCalculator.CalculateAsync() is called
**Then** Complexity defaults to a neutral score (50) as fallback
**And** ILogger logs warning that semantic analysis was unavailable

## Story 4.3: Implement Technology Version Debt Analyzer

As an architect,
I want technology version detected from project files (.NET 3.5 vs .NET 6),
So that older frameworks contribute to extraction difficulty scores.

**Acceptance Criteria:**

**Given** A .csproj or .vbproj file
**When** TechDebtAnalyzer.AnalyzeAsync() is called
**Then** TargetFramework is parsed from the project file XML
**And** .NET Framework versions are identified (net35, net40, net45, net461, net472, net48)
**And** .NET Core/Modern versions are identified (netcoreapp3.1, net5.0, net6.0, net7.0, net8.0)
**And** Tech debt score is calculated: .NET 3.5 = 100 (highest debt), .NET 8 = 0 (no debt)
**And** Intermediate versions are scored proportionally (net472 = 40, net6.0 = 10)
**And** ILogger logs detected framework versions

## Story 4.4: Implement External API Exposure Detector

As an architect,
I want external API exposure detected by scanning for web service attributes,
So that projects with external APIs are marked as harder to extract.

**Acceptance Criteria:**

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

## Story 4.5: Implement Extraction Score Calculator with Configurable Weights

As an architect,
I want all metrics combined into a 0-100 extraction difficulty score using configurable weights,
So that I can customize scoring to my organization's priorities.

**Acceptance Criteria:**

**Given** All four metrics are calculated (Coupling, Complexity, TechDebt, ExternalExposure)
**When** ExtractionScoreCalculator.CalculateAsync() is called
**Then** Weights are loaded from scoring-config.json (Coupling: 0.40, Complexity: 0.30, TechDebt: 0.20, ExternalExposure: 0.10)
**And** Final score = (Coupling * 0.40) + (Complexity * 0.30) + (TechDebt * 0.20) + (ExternalExposure * 0.10)
**And** Score is clamped to 0-100 range
**And** ExtractionScore object contains: project name, final score, individual metric scores, supporting details
**And** Lower scores (0-33) indicate easier extraction, higher scores (67-100) indicate harder extraction

**Given** Custom weights are provided in scoring-config.json
**When** Weights are loaded
**Then** Custom weights are validated (sum must equal 1.0)
**And** Configuration validation error is thrown if weights don't sum to 1.0
**And** ILogger logs which weights are being used

## Story 4.6: Generate Ranked Extraction Candidate Lists

As an architect,
I want all projects ranked by extraction difficulty score (easiest first),
So that I know exactly where to start my migration.

**Acceptance Criteria:**

**Given** Extraction scores are calculated for all projects
**When** Ranked extraction candidate list is generated
**Then** Projects are sorted by extraction score (ascending: lowest score first)
**And** Top 10 easiest candidates are identified (scores 0-33)
**And** Bottom 10 hardest candidates are identified (scores 67-100)
**And** Each candidate includes: project name, score, coupling metric, complexity metric, tech debt, API exposure
**And** Ranked list is available for text reports and CSV export
**And** ILogger logs "Generated ranked extraction candidates: 73 total projects, 18 easy (0-33), 31 medium (34-66), 24 hard (67-100)"

## Story 4.7: Implement Heat Map Visualization with Color-Coded Scores

As an architect,
I want dependency graph nodes color-coded by extraction difficulty,
So that I can visually identify easy vs. hard extraction candidates.

**Acceptance Criteria:**

**Given** Extraction scores are calculated for all projects
**When** DotGenerator.GenerateAsync() is called with scoring information
**Then** Nodes with scores 0-33 are rendered in GREEN
**And** Nodes with scores 34-66 are rendered in YELLOW
**And** Nodes with scores 67-100 are rendered in RED
**And** Node color clearly indicates extraction difficulty at a glance
**And** Graph legend includes "Green: Easy (0-33), Yellow: Medium (34-66), Red: Hard (67-100)"

## Story 4.8: Display Extraction Scores as Node Labels

As an architect,
I want extraction difficulty scores shown as labels on graph nodes,
So that I can see exact scores alongside the color-coding.

**Acceptance Criteria:**

**Given** Extraction scores are calculated for all projects
**When** DotGenerator.GenerateAsync() is called with scoring information
**Then** Each node label includes both project name and extraction score
**And** Label format is: "ProjectName\nScore: 23"
**And** Scores are displayed with clear formatting (no decimal places needed)
**And** Label remains readable on both colored backgrounds and when rendered as PNG/SVG
**And** Enabling/disabling score labels can be configured via visualization settings
