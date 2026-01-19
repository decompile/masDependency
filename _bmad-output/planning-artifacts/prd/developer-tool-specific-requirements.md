# Developer Tool Specific Requirements

## Project-Type Overview

masDependencyMap is a command-line static analysis tool built with Roslyn, targeting software architects managing legacy .NET modernization. The tool follows a "pragmatic MVP" philosophy: deliver actionable migration intelligence quickly through a standalone executable with Graphviz visualization, deferring polished UI for post-MVP iterations.

**Distribution Model:**
- MVP: Standalone executable with source code available on GitHub
- Open source release planned (license TBD)
- Future: NuGet global tool, Chocolatey package for easier distribution

## Technical Architecture Considerations

**Platform Support:**
- **All .NET Framework versions from 3.5 onwards** - critical for legacy codebase analysis
- .NET Core 3.1+ and .NET 5/6/7/8+ solutions
- Mixed-framework solution analysis (e.g., solution with both .NET 3.5 and .NET 6 projects)
- VB.NET and C# project support
- MSBuild-based solution loading using MSBuildWorkspace

**Runtime Requirements:**
- .NET 6+ SDK for running the tool itself (modern runtime for analysis tool)
- Graphviz installation required for visualization generation
- Access to target solution files and project files (.sln, .csproj, .vbproj)
- File system write access for output generation (DOT, PNG/SVG, TXT, CSV)

**Technology Stack:**
- Microsoft.CodeAnalysis.CSharp.Workspaces (Roslyn) for solution loading and semantic analysis
- Microsoft.Build.Locator for MSBuild integration
- QuikGraph library for graph algorithms (cycle detection, SCC analysis)
- Graphviz for DOT format rendering
- Standard .NET file I/O for report generation

## Command-Line Interface Design

**Proposed Command Structure:**

```bash
# Basic usage - analyze single solution
masdependencymap analyze --solution path/to/solution.sln --output ./analysis-output

# Multi-solution analysis
masdependencymap analyze --solutions path/to/*.sln --output ./ecosystem-analysis

# With configuration file
masdependencymap analyze --config ./analysis-config.json --output ./output

# Generate only specific outputs
masdependencymap analyze --solution path/to/solution.sln --output ./output --reports cycle,scoring
```

**Command-Line Parameters:**
- `--solution` or `-s`: Path to single .sln file
- `--solutions`: Glob pattern for multiple solution files
- `--output` or `-o`: Output directory for generated files
- `--config` or `-c`: Path to JSON configuration file (filter rules, scoring weights)
- `--reports`: Comma-separated list of report types (dependencies, cycles, scoring, all)
- `--format`: Output format for graphs (dot, png, svg, all)
- `--verbose` or `-v`: Enable detailed logging
- `--help` or `-h`: Display help and usage examples

**Configuration File (JSON):**

```json
{
  "frameworkFilters": {
    "blocklist": ["Microsoft.*", "System.*", "mscorlib", "netstandard"],
    "allowlist": ["YourCompany.*", "CustomFramework.*"]
  },
  "scoringWeights": {
    "coupling": 0.40,
    "complexity": 0.30,
    "techDebt": 0.20,
    "externalExposure": 0.10
  },
  "visualization": {
    "colorScheme": "default",
    "highlightCycles": true,
    "showScores": true
  }
}
```

## Output Format Specifications

**Generated Files:**

1. **Dependency Graphs (DOT Format):**
   - `{solution-name}-dependencies.dot` - Graphviz source
   - `{solution-name}-dependencies.png` - Rendered visualization
   - `{solution-name}-dependencies.svg` - Scalable vector graphic

2. **Analysis Reports (Text):**
   - `{solution-name}-analysis-report.txt` - Comprehensive text report
   - Summary statistics (total projects, cycles, complexity metrics)
   - Cycle-breaking recommendations with rationale

3. **Data Exports (CSV):**
   - `{solution-name}-extraction-scores.csv` - Ranked extraction candidates
   - `{solution-name}-cycle-analysis.csv` - Cycle detection details
   - `{solution-name}-dependency-matrix.csv` - Full dependency matrix

**Report Formats Example:**

```
=== masDependencyMap Analysis Report ===
Solution: LegacyMonolith.sln
Analysis Date: 2026-01-17
Total Projects: 73

--- Dependency Overview ---
Total References: 412
Framework References (Filtered): 2,847
Custom References: 412
Cross-Solution References: 34

--- Cycle Detection ---
Circular Dependency Chains: 12
Projects in Cycles: 45 (61.6%)
Largest Cycle Size: 8 projects

--- Extraction Difficulty Scores ---
Easiest Candidates (Score 0-33):
1. NotificationService (Score: 23) - 3 incoming, 2 outgoing, no external APIs
2. LoggingUtility (Score: 28) - 5 incoming, 1 outgoing, no external APIs
3. ReportGenerator (Score: 31) - 7 incoming, 3 outgoing, 1 external API

Hardest Candidates (Score 67-100):
1. CoreBusinessLogic (Score: 94) - 34 incoming, 28 outgoing, 15 external APIs
2. DataAccessLayer (Score: 89) - 41 incoming, 12 outgoing, 8 external APIs
```

## Installation & Setup

**MVP Installation Process:**

1. **Download Standalone Executable:**
   - Download latest release from GitHub releases page
   - Extract to local directory (e.g., `C:\tools\masdependencymap`)
   - Add to PATH for global access (optional but recommended)

2. **Install Graphviz Dependency:**
   - Windows: `choco install graphviz` or download from graphviz.org
   - Verify installation: `dot -version`

3. **Verify Tool Installation:**
   - Run: `masdependencymap --version`
   - Run: `masdependencymap --help`

4. **First Analysis:**
   - Run on sample solution (provided in GitHub repo)
   - Review generated outputs to understand structure
   - Run on smallest production solution as proof-of-concept

**Time to First Graph:** Target 15 minutes from download to first visualization

## Documentation & Examples

**Required Documentation:**

1. **README.md (Quick Start):**
   - Installation instructions (5 minutes or less)
   - Basic usage example with sample solution
   - Link to full documentation
   - Troubleshooting common issues (MSBuild errors, Graphviz not found)

2. **User Guide (GitHub Wiki or docs/ folder):**
   - Command-line reference with all parameters
   - Configuration file documentation with examples
   - Interpreting analysis reports and visualizations
   - Understanding extraction difficulty scores
   - Cycle-breaking recommendations guide
   - Advanced scenarios (multi-solution analysis, custom filters)

3. **Example Scenarios:**
   - Analyze single solution and generate all reports
   - Multi-solution ecosystem analysis (20 solutions)
   - Custom framework filtering for company-specific libraries
   - Tuning scoring weights based on validation results
   - Re-analyzing after component extraction

4. **Sample Solution:**
   - Provide sample .NET solution in GitHub repo for testing
   - Small solution (5-10 projects) with intentional cycles
   - Pre-generated expected output for validation
   - Demonstrates all three analysis phases (filtering, cycles, scoring)

**Getting Started Path:**

```bash
# Step 1: Clone repo and navigate to samples
git clone https://github.com/yaniv/masDependencyMap.git
cd masDependencyMap/samples

# Step 2: Run analysis on sample solution
../bin/masdependencymap analyze --solution SampleMonolith.sln --output ./sample-output

# Step 3: View generated graph
./sample-output/SampleMonolith-dependencies.png

# Step 4: Review analysis report
cat ./sample-output/SampleMonolith-analysis-report.txt

# Step 5: Check extraction candidates
cat ./sample-output/SampleMonolith-extraction-scores.csv
```

## API Surface (For Future Extension)

While MVP is console-only, design with extensibility in mind:

**Core Analysis Library:**
- Separate core analysis logic from CLI interface
- Enable future Visual Studio extension or web dashboard to reuse analysis engine
- Public API for solution loading, dependency extraction, cycle detection, scoring

**Planned Architecture:**
- `MasDependencyMap.Core` - Analysis engine (solution loading, graph algorithms, scoring)
- `MasDependencyMap.CLI` - Command-line interface (MVP)
- `MasDependencyMap.WebDashboard` - Future web UI (post-MVP)
- `MasDependencyMap.VSExtension` - Future IDE integration (post-MVP)

## Code Examples & Migration Guide

**Usage Examples in Documentation:**

```bash
# Example 1: Quick single-solution analysis
masdependencymap analyze -s MyLegacy.sln -o ./output

# Example 2: Ecosystem analysis across all solutions
masdependencymap analyze --solutions C:/Projects/**/*.sln -o ./ecosystem

# Example 3: Custom filtering for company frameworks
masdependencymap analyze -s MyApp.sln -c custom-filters.json -o ./output

# Example 4: Focus only on cycle detection
masdependencymap analyze -s MyApp.sln --reports cycle -o ./output

# Example 5: Verbose logging for troubleshooting
masdependencymap analyze -s MyApp.sln -o ./output --verbose
```

## Implementation Considerations

**Error Handling:**
- Graceful handling of MSBuild solution loading failures
- Clear error messages for missing Graphviz installation
- Fallback behavior when Roslyn semantic analysis fails (use project references only)
- Progress indicators for long-running analysis (20 solutions, 1000+ projects)

**Performance Considerations:**
- Incremental analysis not required for MVP (future enhancement)
- Caching not required for MVP (re-analyze from scratch each run)
- Parallel project analysis where possible
- Memory-efficient processing for large solutions (70+ projects)

**Compatibility & Edge Cases:**
- Handle mixed C#/VB.NET solutions
- Handle missing SDK references gracefully
- Support solution folders and nested structures
- Handle project references vs. DLL references appropriately
- Detect and report COM+ component dependencies (limited in MVP)
