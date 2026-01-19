# Non-Functional Requirements

## Performance

- **NFR1:** Solution analysis for a single solution with 50 projects completes within 5 minutes on standard developer workstation
- **NFR2:** Multi-solution analysis (20 solutions, 400+ projects) completes within 30 minutes
- **NFR3:** Graph visualization rendering via Graphviz completes within 30 seconds for graphs with 100+ nodes
- **NFR4:** System provides progress feedback for operations lasting longer than 10 seconds
- **NFR5:** Memory usage remains under 4GB during analysis of large solution ecosystems (1000+ projects)
- **NFR6:** CSV export generation completes within 10 seconds regardless of solution size

## Reliability

- **NFR7:** System gracefully handles MSBuild solution loading failures without crashing, providing actionable error messages
- **NFR8:** System detects missing Graphviz installation and provides clear installation guidance
- **NFR9:** When Roslyn semantic analysis fails, system falls back to project reference parsing and continues execution
- **NFR10:** System handles missing or invalid SDK references without terminating analysis
- **NFR11:** Command-line parsing validates all parameters and provides helpful error messages for invalid inputs
- **NFR12:** System recovers gracefully from file system access errors (permissions, locked files)
- **NFR13:** System validates configuration JSON files and reports specific syntax errors with line numbers

## Usability

- **NFR14:** Error messages include specific problem description, root cause, and suggested remediation steps
- **NFR15:** Installation documentation enables first-time user to generate first graph within 15 minutes
- **NFR16:** Help documentation (`--help` flag) displays within command-line interface without requiring external browser
- **NFR17:** Progress indicators show percentage completion and estimated time remaining for long-running operations
- **NFR18:** Generated reports use clear, non-technical language suitable for stakeholder presentations
- **NFR19:** Sample solution provided executes successfully on first run with pre-generated expected output for comparison
- **NFR20:** Command-line parameters follow standard conventions (short and long flags, consistent naming)

## Maintainability

- **NFR21:** Core analysis logic (solution loading, cycle detection, scoring) separated from CLI interface to enable future reuse
- **NFR22:** Codebase structured into separate assemblies: MasDependencyMap.Core, MasDependencyMap.CLI
- **NFR23:** Scoring algorithm weights configurable via JSON without code changes
- **NFR24:** Framework filter rules configurable via JSON without code changes
- **NFR25:** Code follows standard .NET naming conventions and includes XML documentation comments for public APIs
- **NFR26:** Unit tests cover core analysis algorithms (cycle detection, scoring calculation, framework filtering)
- **NFR27:** README includes architecture overview and extension points for future enhancements

## Integration

- **NFR28:** System integrates with Graphviz 2.38+ for DOT file rendering to PNG/SVG formats
- **NFR29:** System integrates with MSBuild via Microsoft.Build.Locator to resolve SDK-style project references
- **NFR30:** System integrates with Roslyn (Microsoft.CodeAnalysis.CSharp.Workspaces) for semantic code analysis
- **NFR31:** System integrates with QuikGraph library for graph algorithm execution (Tarjan's SCC)
- **NFR32:** System detects Graphviz installation via PATH environment variable or explicit configuration
- **NFR33:** Generated DOT files compatible with Graphviz 2.38+ specification without manual editing
- **NFR34:** CSV exports use standard RFC 4180 format for compatibility with Excel, Google Sheets, and data analysis tools
