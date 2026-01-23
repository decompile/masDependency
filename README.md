# masDependencyMap

A .NET dependency analyzer that visualizes project dependencies, detects circular dependencies, and scores extraction difficulty for microservice candidates.

## Features

**Currently Implemented (Epic 1 - Foundation):**
- **Cross-Version Analysis**: Analyze .NET solutions from Framework 3.5 through .NET 8+
- **Flexible Loading Strategy**: Automatic fallback chain (Roslyn → MSBuild → Project File parsing)
- **Command-Line Interface**: Full parameter parsing with System.CommandLine
- **Structured Logging**: Configurable logging with --verbose flag
- **Configuration Management**: JSON-based configuration with validation

**Planned (Epics 2-6 - In Development):**
- **Visual Dependency Graphs**: Generate dependency visualizations in PNG and SVG formats (Epic 2)
- **Cycle Detection**: Detect circular dependencies using Tarjan's algorithm (Epic 3)
- **Extraction Scoring**: Score projects for microservice extraction difficulty (Epic 4)
- **Multiple Export Formats**: Export analysis to CSV, text reports, and visual graphs (Epic 5)

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

**Note:** Binary releases will be available once analysis features are implemented (Epic 2+).

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

**Current Status:** Epic 1 (Foundation) is complete. Analysis features (Epics 2-6) are in development.

```bash
# 1. Build the solution (if not already done)
cd masDependencyMap
dotnet build

# 2. Test CLI with sample solution
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution samples/SampleMonolith/SampleMonolith.sln

# Expected output: Configuration loaded, parameters parsed successfully
# Note: Graph generation and analysis will be added in Epic 2+

# 3. View help and available options
dotnet run --project src/MasDependencyMap.CLI -- analyze --help

# 4. Test with verbose logging
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution samples/SampleMonolith/SampleMonolith.sln --verbose
```

**Sample Solution Note:** The sample solution in `samples/SampleMonolith/` demonstrates project structure. Due to .NET SDK project reference limitations, it does not contain actual circular dependencies.

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

## Understanding Output Files (Planned - Epic 2+)

**Note:** Output file generation will be implemented in upcoming epics. Current Epic 1 focuses on CLI foundation.

### Dependency Graphs (Epic 2)
- **DOT Format** (`*-dependencies.dot`): GraphViz source format
- **PNG Format** (`*-dependencies.png`): Raster image for quick viewing
- **SVG Format** (`*-dependencies.svg`): Vector image for high-quality rendering

### Text Reports (Epic 5)
- Console output with project statistics and dependency counts
- Progress indicators for long-running operations

### CSV Exports (Epic 5)
- Dependency matrix, extraction scores, and cycle analysis
- Compatible with Excel and Google Sheets

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
