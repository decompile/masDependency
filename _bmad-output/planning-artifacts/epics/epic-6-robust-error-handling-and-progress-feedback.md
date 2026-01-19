# Epic 6: Robust Error Handling and Progress Feedback

Architects get clear, actionable error messages with remediation steps, visual progress indicators with percentage completion and ETA, and graceful degradation when analysis issues occur.

## Story 6.1: Implement Custom Exception Hierarchy

As an architect,
I want domain-specific exceptions with clear hierarchies,
So that errors are caught and handled appropriately in fallback chains.

**Acceptance Criteria:**

**Given** The Core library needs structured exception handling
**When** Custom exception classes are implemented
**Then** Base exceptions exist: SolutionLoadException, GraphvizException, ConfigurationException
**And** Specific exceptions inherit from base: RoslynLoadException, MSBuildLoadException, ProjectFileLoadException (all inherit from SolutionLoadException)
**And** GraphvizNotFoundException and GraphvizRenderException inherit from GraphvizException
**And** All custom exceptions include: error message, inner exception, contextual data (e.g., file path, solution name)
**And** Exception constructors follow standard .NET patterns (message, message + innerException)

## Story 6.2: Implement Graphviz Missing Error with Installation Guidance

As an architect,
I want clear error messages when Graphviz is not installed,
So that I know exactly how to fix the problem.

**Acceptance Criteria:**

**Given** Graphviz is not installed or not in PATH
**When** Graph rendering is attempted
**Then** GraphvizNotFoundException is thrown with clear context
**And** CLI catches the exception and uses Spectre.Console markup for formatted output
**And** Error message format: "[red]Error:[/] Graphviz is not installed or not in PATH"
**And** Reason: "[dim]Reason:[/] Could not find 'dot' executable"
**And** Suggestion: "[dim]Suggestion:[/] Install Graphviz from https://graphviz.org or add to PATH"
**And** Platform-specific instructions are shown (Windows: choco install graphviz)
**And** Exit code is 1 (error)

## Story 6.3: Implement Solution Loading Error with Troubleshooting Steps

As an architect,
I want clear error messages when solution files cannot be loaded,
So that I can troubleshoot loading issues.

**Acceptance Criteria:**

**Given** A solution file cannot be loaded by any loader
**When** All three loaders (Roslyn, MSBuild, ProjectFile) fail
**Then** Comprehensive error message shows all three failure reasons
**And** Error uses Spectre.Console 3-part format (Error/Reason/Suggestion)
**And** Specific errors are shown: "Roslyn: MSBuildWorkspace failed to open solution", "MSBuild: SDK version mismatch", "ProjectFile: Invalid XML in .csproj"
**And** Suggestion includes actionable remediation: "Verify .NET SDK version matches solution target framework", "Check for corrupted project files", "Run 'dotnet restore' first"
**And** ILogger logs each loader failure with structured logging
**And** Exit code is 1 (error)

## Story 6.4: Implement Progress Indicators with Spectre.Console

As an architect,
I want visual progress indicators for long-running operations,
So that I know the analysis is running and how long it will take.

**Acceptance Criteria:**

**Given** An operation takes longer than 10 seconds
**When** Solution loading, cycle detection, or scoring runs
**Then** Spectre.Console.Progress displays a progress bar
**And** Progress shows: task description, progress bar, percentage completion
**And** Format example: "Loading solutions [========>    ] 15/20 (75%)"
**And** ETA is estimated and displayed: "Est. remaining: 2m 15s"
**And** Progress updates in real-time as tasks complete
**And** Multiple concurrent progress tasks are supported (e.g., "Loading solutions", "Building graph", "Detecting cycles")

## Story 6.5: Implement Graceful Degradation with Partial Success Reporting

As an architect,
I want analysis to continue even when some projects fail to load,
So that I can get results for the projects that do load successfully.

**Acceptance Criteria:**

**Given** A solution has 50 projects and 5 fail to load
**When** Analysis runs
**Then** The 45 successful projects are included in the dependency graph
**And** The 5 failed projects are logged with ILogger.LogWarning
**And** Progress indicator shows: "Loading projects: 45/50 (5 failed)"
**And** Final report includes a warning section: "Warning: 5 projects could not be loaded"
**And** Failed projects are listed with their error messages
**And** Analysis continues and completes with partial results
**And** Exit code is 0 (success with warnings)

## Story 6.6: Implement Configuration Validation with Syntax Error Reporting

As an architect,
I want configuration JSON files validated with specific syntax error messages,
So that I can quickly fix configuration problems.

**Acceptance Criteria:**

**Given** filter-config.json has invalid JSON syntax
**When** Configuration is loaded
**Then** ConfigurationException is thrown with the specific error location
**And** Error message includes: file path, line number, column number, syntax error description
**And** Error uses Spectre.Console format: "[red]Error:[/] Invalid JSON in filter-config.json at line 5, column 12"
**And** Reason: "[dim]Reason:[/] Missing comma after property value"
**And** Suggestion: "[dim]Suggestion:[/] Check JSON syntax, ensure all properties are comma-separated"

**Given** scoring-config.json has valid JSON but invalid weights (don't sum to 1.0)
**When** Configuration is validated
**Then** ConfigurationException is thrown with validation error
**And** Error message: "Scoring weights must sum to 1.0 (currently: 0.95)"
**And** Suggestion shows current weights and how to fix them

## Story 6.7: Implement Verbose Logging Mode

As an architect,
I want detailed diagnostic logging when I use --verbose flag,
So that I can troubleshoot issues during analysis.

**Acceptance Criteria:**

**Given** I run the tool with --verbose flag
**When** Analysis executes
**Then** ILogger outputs Info and Debug level messages to console
**And** Verbose logs include: "Loading solution from {SolutionPath}", "Parsing project file at {ProjectPath}", "Detected {CycleCount} cycles", "Calculated extraction score for {ProjectName}: {Score}"
**And** All log messages use structured logging with named placeholders (not string interpolation)
**And** Verbose output is clearly formatted and doesn't interfere with progress indicators

**Given** I run the tool without --verbose flag
**When** Analysis executes
**Then** Only Error and Warning level messages are shown
**And** User-facing output (progress, reports) is shown via Spectre.Console
**And** Diagnostic logs are suppressed for cleaner output

## Story 6.8: Implement Missing SDK Reference Graceful Handling

As an architect,
I want analysis to continue when projects have missing SDK references,
So that one broken project doesn't stop the entire analysis.

**Acceptance Criteria:**

**Given** A project references an SDK that is not installed
**When** Solution loading encounters the missing SDK
**Then** ILogger logs a warning: "Project {ProjectName} references missing SDK {SdkName}"
**And** The project is marked as partially loaded (basic info available, but no semantic analysis)
**And** Dependency graph includes the project based on project references (fallback to XML parsing)
**And** Analysis continues without crashing
**And** Final report includes warning: "Warning: Some projects have missing SDK references"
**And** Specific projects with issues are listed in the warning section
