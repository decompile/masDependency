# Sample Outputs

This directory will contain pre-generated analysis outputs after Epic 2 is complete.

## Expected Files (After Epic 2 Story 2-9)

Once masDependencyMap implements solution loading, dependency graph construction, and visualization generation, this directory will contain reference outputs:

### Dependency Graph Visualizations
- **dependency-graph.dot** - DOT format graph representation
- **dependency-graph.png** - PNG visual graph (requires Graphviz)
- **dependency-graph.svg** - SVG visual graph (requires Graphviz)

### Analysis Reports
- **analysis-report.txt** - Text report with dependency metrics and statistics
- **dependency-matrix.csv** - CSV export of project-to-project dependencies

## How to Generate

After Epic 2 Story 2-9 is complete:

```bash
# From masDependencyMap repository root
cd samples
dotnet run --project ../src/MasDependencyMap.CLI -- analyze \
  --solution SampleMonolith/SampleMonolith.sln \
  --output sample-output
```

## Purpose

These reference outputs serve multiple purposes:

1. **Documentation**: Show users what masDependencyMap produces
2. **Testing**: Validate that analysis output matches expected format
3. **Regression Testing**: Detect changes in output format across versions
4. **Examples**: Demonstrate tool capabilities for README and docs

## Current Status

⏸️ **Waiting for Epic 2 Implementation**

The sample solution is ready, but output generation requires:
- Story 2-1 through 2-5: Solution loading and dependency graph construction
- Story 2-8: DOT format generation
- Story 2-9: Graphviz rendering to PNG/SVG

Once these stories are complete, run the command above to populate this directory with reference outputs.
