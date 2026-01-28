# masDependencyMap

A .NET dependency analyzer that visualizes project dependencies, detects circular dependencies, and scores extraction difficulty for microservice candidates.

## Features

### Core Analysis (✅ Complete)
- **Cross-Version Analysis**: Analyze .NET solutions from Framework 3.5 through .NET 8+
- **Flexible Loading Strategy**: Automatic fallback chain (Roslyn → MSBuild → Project File parsing)
- **Multi-Solution Support**: Analyze multiple solutions as an ecosystem
- **Command-Line Interface**: Full parameter parsing with System.CommandLine
- **Structured Logging**: Configurable logging with --verbose flag
- **Configuration Management**: JSON-based configuration with validation

### Visualization (✅ Complete)
- **Dependency Graphs**: Generate DOT, PNG, and SVG visualizations
- **Heat Map Coloring**: Nodes colored by extraction difficulty (green=easy, yellow=medium, red=hard)
- **Score Labels**: Display extraction scores directly on visualization nodes
- **Cycle Highlighting**: Red edges indicate circular dependencies
- **Break Point Suggestions**: Yellow edges show recommended cycle-breaking points
- **Interactive Legends**: Extraction difficulty and dependency type legends

### Cycle Detection (✅ Complete)
- **Tarjan's Algorithm**: Efficient strongly connected component detection
- **Cycle Statistics**: Total cycles, participation rates, largest cycle size
- **Coupling Analysis**: Method call counting to identify weak coupling points
- **Breaking Recommendations**: Ranked suggestions for cycle removal

### Extraction Scoring (✅ Complete)
- **Difficulty Scoring**: 0-100 scale with four weighted metrics
- **Coupling Metric**: Incoming/outgoing dependency analysis
- **Complexity Metric**: Cyclomatic complexity via Roslyn
- **Tech Debt Analysis**: Framework version debt detection
- **API Exposure**: External API surface area detection
- **Configurable Weights**: Customize metric importance via JSON

### Comprehensive Reporting (✅ Complete)
- **Text Reports**: Formatted tables with Spectre.Console
- **CSV Exports**: Extraction scores, cycle analysis, dependency matrix
- **Console Output**: Progress indicators and formatted tables (--verbose)
- **Multiple Formats**: Generate text-only, CSV-only, or all formats

## Prerequisites

- **.NET 8 SDK**: Required to run the tool
  - Download from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
  - Verify installation: `dotnet --version` should show 8.0.x
- **Graphviz 2.38+**: Required for visualization
  - Download from [graphviz.org/download](https://graphviz.org/download/)
  - Verify installation: `dot -V` (note: uppercase V)

## Installation

### Build from Source

```bash
# Navigate to the project directory
cd masDependencyMap

# Build the solution
dotnet build

# Verify installation
dotnet run --project src/MasDependencyMap.CLI -- --version
```

### Install Graphviz

**Windows:**
1. Download installer from [graphviz.org/download](https://graphviz.org/download/)
2. Install to `C:\Program Files\Graphviz` (or preferred location)
3. Add `C:\Program Files\Graphviz\bin` to your PATH environment variable
4. Restart terminal and verify: `dot -V`

Alternatively, use Chocolatey:
```powershell
choco install graphviz
dot -V
```

**macOS:**
```bash
brew install graphviz
dot -V
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt-get install graphviz
dot -V
```

**Note:** Story 2.7 implements Graphviz detection with helpful installation guidance. The tool will check if Graphviz is installed before attempting visualization.

## Quick Start

```bash
# 1. Build the solution
cd masDependencyMap
dotnet build

# 2. Run analysis on the sample solution
dotnet run --project src/MasDependencyMap.CLI -- analyze \
  --solution samples/SampleMonolith/SampleMonolith.sln \
  --output ./analysis-results

# Expected output files in ./analysis-results/:
# - SampleMonolith-dependencies.dot (Graphviz source)
# - SampleMonolith-dependencies.png (visualization)
# - SampleMonolith-dependencies.svg (scalable visualization)
# - SampleMonolith-analysis-report.txt (formatted text report)
# - SampleMonolith-extraction-scores.csv (extraction difficulty)
# - SampleMonolith-dependency-matrix.csv (full dependency matrix)

# 3. View help and available options
dotnet run --project src/MasDependencyMap.CLI -- analyze --help

# 4. Run with verbose logging to see tables in console
dotnet run --project src/MasDependencyMap.CLI -- analyze \
  --solution samples/SampleMonolith/SampleMonolith.sln \
  --verbose
```

**Sample Solution:** The `samples/SampleMonolith/` contains a 7-project solution demonstrating various dependency patterns. While it doesn't contain circular dependencies (due to .NET SDK limitations), it effectively demonstrates extraction scoring and visualization features.

## Usage Examples

### Basic Analysis

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln
```

### Custom Output Directory

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --output ./analysis-results
```

### Verbose Mode for Troubleshooting

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --verbose
```

### Generate Only PNG Format

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --format png
```

### Generate Only Text Reports

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --reports text
```

## Documentation

- **[User Guide](docs/user-guide.md)** - Complete command-line reference with all parameters
- **[Configuration Guide](docs/configuration-guide.md)** - JSON configuration for filters and scoring
- **[Troubleshooting](docs/troubleshooting.md)** - Common issues and solutions

## Understanding Output Files

### Dependency Visualizations
- **DOT Format** (`{SolutionName}-dependencies.dot`): GraphViz source file with UTF-8 encoding (no BOM for Graphviz compatibility)
- **PNG Format** (`{SolutionName}-dependencies.png`): Raster image for quick viewing
- **SVG Format** (`{SolutionName}-dependencies.svg`): Vector image for high-quality rendering

**Visualization Features:**
- **Heat Map Colors**: Nodes colored by extraction difficulty
  - Green (lightgreen): Easy extraction candidates (scores 0-33)
  - Yellow: Medium difficulty (scores 34-66)
  - Red (lightcoral): Hard to extract (scores 67-100)
- **Score Labels**: Each node displays `"ProjectName\nScore: 42"`
- **Cycle Highlighting**: Red edges indicate circular dependencies
- **Break Points**: Yellow edges show top 10 recommended cycle-breaking points
- **Legends**: Automatic legends for extraction difficulty and dependency types

### Text Reports
- **File**: `{SolutionName}-analysis-report.txt`
- **Format**: Formatted tables using Spectre.Console with UTF-8 encoding (no BOM)
- **Sections**:
  - Report header (solution name, date, project count)
  - Dependency overview (total refs, framework/custom breakdown)
  - Extraction difficulty scores (easiest/hardest candidates)
  - Cycle detection (if cycles found)
  - Cycle-breaking recommendations (if applicable)
- **Console Output**: Use `--verbose` flag to display formatted tables in terminal

### CSV Exports
All CSV files use RFC 4180 format with UTF-8 BOM for Excel compatibility.

- **Extraction Scores** (`{SolutionName}-extraction-scores.csv`):
  - Columns: Project Name, Extraction Score, Coupling Metric, Complexity Metric, Tech Debt Score, External APIs
  - Sorted by extraction score ascending (easiest first)

- **Cycle Analysis** (`{SolutionName}-cycle-analysis.csv`):
  - Generated only if circular dependencies detected
  - Includes cycle details and suggested break points

- **Dependency Matrix** (`{SolutionName}-dependency-matrix.csv`):
  - Full project-to-project dependency mapping
  - Includes coupling scores for each dependency edge
  - Sorted by source project, then target project

## Configuration

Create optional JSON configuration files in your project directory:

- `filter-config.json` - Control which dependencies are considered "framework" vs "application"
- `scoring-config.json` - Customize extraction difficulty scoring weights

See the [Configuration Guide](docs/configuration-guide.md) for detailed examples and schemas.

## Getting Help

```bash
# Show help for all commands
dotnet run --project src/MasDependencyMap.CLI -- --help

# Show help for analyze command
dotnet run --project src/MasDependencyMap.CLI -- analyze --help

# Show version
dotnet run --project src/MasDependencyMap.CLI -- --version
```

## License

[MIT License](LICENSE) - See LICENSE file for details

## Contributing

Contributions are welcome! Please open an issue or pull request for bug fixes, features, or documentation improvements.

## Support

If you encounter issues:
1. Check the [Troubleshooting Guide](docs/troubleshooting.md)
2. Run with `--verbose` flag for detailed diagnostics
3. Open an issue with the verbose output and system information
