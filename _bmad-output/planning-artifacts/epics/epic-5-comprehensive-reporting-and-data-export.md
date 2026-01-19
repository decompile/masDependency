# Epic 5: Comprehensive Reporting and Data Export

Architects can generate stakeholder-ready text reports with summary statistics, export data to CSV for external analysis tools, and share migration insights with executives and team members.

## Story 5.1: Implement Text Report Generator with Summary Statistics

As an architect,
I want comprehensive text reports with summary statistics,
So that I can review analysis results in a readable format.

**Acceptance Criteria:**

**Given** Analysis is complete with dependency graph, cycles, and scores
**When** TextReportGenerator.GenerateAsync() is called
**Then** A text file is created at {SolutionName}-analysis-report.txt in the output directory
**And** Report header includes: solution name, analysis date, total projects count
**And** Dependency Overview section shows: total references, framework references (filtered), custom references, cross-solution references
**And** Report sections are clearly formatted with headers and separators
**And** File is generated within 10 seconds regardless of solution size
**And** Text is formatted for readability (proper spacing, alignment, section breaks)

## Story 5.2: Add Cycle Detection Section to Text Reports

As an architect,
I want cycle detection results included in text reports,
So that I can see all circular dependencies and statistics in one place.

**Acceptance Criteria:**

**Given** Cycles have been detected
**When** Text report is generated
**Then** Cycle Detection section includes: total circular dependency chains count
**And** Projects in cycles count and percentage (e.g., "45 projects (61.6%)")
**And** Largest cycle size is reported
**And** Each cycle is listed with the projects involved
**And** Report is formatted clearly: "Circular Dependency Chains: 12"

**Given** No cycles are detected
**When** Text report is generated
**Then** Cycle Detection section shows "No circular dependencies detected"

## Story 5.3: Add Extraction Difficulty Scoring Section to Text Reports

As an architect,
I want extraction difficulty scores included in text reports,
So that I can see ranked candidates and their metrics.

**Acceptance Criteria:**

**Given** Extraction scores are calculated
**When** Text report is generated
**Then** Extraction Difficulty Scores section includes top 10 easiest candidates
**And** Each candidate shows: rank, project name, score, incoming refs, outgoing refs, external APIs
**And** Bottom 10 hardest candidates are listed with their complexity indicators
**And** Report format example: "1. NotificationService (Score: 23) - 3 incoming, 2 outgoing, no external APIs"
**And** Score ranges are explained: "Easiest Candidates (Score 0-33)", "Hardest Candidates (Score 67-100)"

## Story 5.4: Add Cycle-Breaking Recommendations to Text Reports

As an architect,
I want cycle-breaking recommendations with rationale included in text reports,
So that I can share actionable insights with my team.

**Acceptance Criteria:**

**Given** Cycle-breaking recommendations have been generated
**When** Text report is generated
**Then** Cycle-Breaking Recommendations section lists top 5 suggestions
**And** Each recommendation includes: source project → target project, coupling score, rationale
**And** Rationale explains why this edge should be broken (e.g., "Weakest link in 8-project cycle, only 3 method calls")
**And** Report format example: "Break: PaymentService → OrderManagement (Coupling: 3 calls) - Weakest link in 5-project cycle"
**And** Recommendations are actionable and easy for stakeholders to understand

## Story 5.5: Implement CSV Export for Extraction Difficulty Scores

As an architect,
I want extraction scores exported to CSV with supporting metrics,
So that I can analyze data in Excel or Google Sheets.

**Acceptance Criteria:**

**Given** Extraction scores are calculated
**When** CsvExporter.ExportExtractionScoresAsync() is called
**Then** CsvHelper library generates {SolutionName}-extraction-scores.csv in RFC 4180 format
**And** CSV columns use Title Case with Spaces: "Project Name", "Extraction Score", "Coupling Metric", "Complexity Metric", "Tech Debt Score", "External APIs"
**And** All projects are included in the CSV, sorted by extraction score (ascending)
**And** CSV opens correctly in Excel and Google Sheets without manual formatting
**And** UTF-8 encoding with BOM is used for Excel compatibility
**And** Export completes within 10 seconds

## Story 5.6: Implement CSV Export for Cycle Analysis

As an architect,
I want cycle analysis details exported to CSV,
So that I can share cycle information with my team for tracking.

**Acceptance Criteria:**

**Given** Cycles have been detected
**When** CsvExporter.ExportCycleAnalysisAsync() is called
**Then** CsvHelper library generates {SolutionName}-cycle-analysis.csv in RFC 4180 format
**And** CSV columns include: "Cycle ID", "Cycle Size", "Projects Involved", "Suggested Break Point", "Coupling Score"
**And** Each row represents one cycle
**And** Projects Involved column lists all project names in the cycle (comma-separated within quotes)
**And** Suggested Break Point shows "SourceProject → TargetProject"
**And** CSV is RFC 4180 compliant and opens correctly in Excel/Google Sheets

## Story 5.7: Implement CSV Export for Dependency Matrix

As an architect,
I want full dependency data exported to CSV,
So that I can perform custom analysis in external tools.

**Acceptance Criteria:**

**Given** Dependency graph is built
**When** CsvExporter.ExportDependencyMatrixAsync() is called
**Then** CsvHelper library generates {SolutionName}-dependency-matrix.csv in RFC 4180 format
**And** CSV columns include: "Source Project", "Target Project", "Dependency Type", "Coupling Score"
**And** Dependency Type shows "Project Reference" or "Binary Reference"
**And** All edges in the filtered dependency graph are exported
**And** CSV can be imported into analysis tools for custom queries and visualization

## Story 5.8: Format Reports with Spectre.Console Tables

As an architect,
I want text reports to use formatted tables for better readability,
So that reports are stakeholder-ready without manual editing.

**Acceptance Criteria:**

**Given** Report data needs to be presented in tabular format
**When** TextReportGenerator uses Spectre.Console.Table
**Then** Tables are rendered with clear column headers and aligned data
**And** Extraction candidate tables show: Rank | Project Name | Score | Incoming | Outgoing | APIs
**And** Cycle tables show: Cycle ID | Size | Projects | Suggested Break
**And** Tables use appropriate column widths for readability
**And** Tables render correctly in console output when using --verbose mode
**And** Non-technical language is used suitable for executive presentations
