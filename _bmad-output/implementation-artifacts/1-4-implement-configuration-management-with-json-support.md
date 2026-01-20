# Story 1.4: Implement Configuration Management with JSON Support

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want to load configuration from JSON files (filter-config.json, scoring-config.json),
So that I can customize framework filters and scoring weights without code changes.

## Acceptance Criteria

**Given** I have created filter-config.json and scoring-config.json in the project root
**When** The tool loads configuration via Microsoft.Extensions.Configuration
**Then** FilterConfiguration POCO is populated from filter-config.json with BlockList and AllowList patterns
**And** ScoringConfiguration POCO is populated from scoring-config.json with weights (Coupling, Complexity, TechDebt, ExternalExposure)
**And** Configuration validation reports specific JSON syntax errors with line numbers if files are malformed
**And** If config files are missing, sensible defaults are used (Microsoft.*/System.* blocked by default)

## Tasks / Subtasks

- [x] Create configuration POCOs (AC: FilterConfiguration and ScoringConfiguration)
  - [x] Create FilterConfiguration with BlockList and AllowList properties
  - [x] Create ScoringConfiguration with weight properties (Coupling, Complexity, TechDebt, ExternalExposure)
  - [x] Add data annotations for validation (Range 0.0-1.0 for weights, NotNull for lists)
  - [x] Implement default value logic in property initializers
- [x] Implement ConfigurationBuilder setup (AC: Load via Microsoft.Extensions.Configuration)
  - [x] Add ConfigurationBuilder in Program.cs after MSBuildLocator call
  - [x] Configure JSON file loading with optional: true for config files
  - [x] Set base path to current directory
  - [x] Register IConfiguration in DI container
- [x] Create sample configuration JSON files (AC: All)
  - [x] Create filter-config.json with Microsoft.*/System.* defaults
  - [x] Create scoring-config.json with default weights (0.40/0.30/0.20/0.10)
  - [x] Use PascalCase for property names (matches C# conventions)
  - [x] Add .gitignore entries for user-specific config overrides
- [x] Implement configuration validation (AC: JSON syntax errors with line numbers)
  - [x] Create ConfigurationValidator class (inline in Program.cs ValidateConfigurationFiles method)
  - [x] Implement JSON parsing with JsonDocument for syntax validation
  - [x] Catch JsonException and report LineNumber/BytePositionInLine
  - [x] Use ValidateDataAnnotations + ValidateOnStart for POCO validation
  - [x] Display errors with Spectre.Console 3-part error format
- [x] Implement default fallback logic (AC: Sensible defaults when missing)
  - [x] Check if filter-config.json exists before loading
  - [x] Check if scoring-config.json exists before loading
  - [x] Use property initializers with Microsoft.*/System.* defaults
  - [x] Use default weights: Coupling=0.40, Complexity=0.30, TechDebt=0.20, ExternalExposure=0.10
- [x] Test configuration loading (AC: All)
  - [x] Verify config loads successfully when both files exist
  - [x] Verify defaults used when files are missing
  - [x] Verify JSON syntax errors are caught with line numbers
  - [x] Verify invalid weights (outside 0.0-1.0) are rejected
  - [x] Verify weights sum validation (should sum to 1.0)

## Dev Notes

### Critical Implementation Rules

🚨 **MUST READ BEFORE STARTING** - These are non-negotiable requirements from project-context.md:

**PascalCase JSON Convention (project-context.md line 49-65):**
```json
{
  "FrameworkFilters": {
    "BlockList": ["Microsoft.*", "System.*"],
    "AllowList": ["YourCompany.*"]
  }
}
```
- MUST use PascalCase for all JSON property names
- MUST match C# POCO property names exactly
- NEVER use camelCase or snake_case in configuration files
- This enables automatic binding without JsonPropertyName attributes

**Configuration Loading Rules (project-context.md line 108-113):**
- JSON files MUST use PascalCase property names (matches C# POCO properties)
- Load configuration files from current directory by default
- Command-line arguments can override: `--config path/to/config.json`
- Use IConfiguration injection, NOT direct JsonSerializer.Deserialize<T>()

**File Naming Convention (project-context.md line 110-113):**
- Default config files: `filter-config.json`, `scoring-config.json`
- Located in current directory by default
- User can override via --config argument (future story)

### Technical Requirements

**Architecture Decision: Microsoft.Extensions.Configuration (Architecture core-architectural-decisions.md line 22-38):**

**Implementation Approach:**
1. JSON configuration files for filter rules (`filter-config.json`)
2. JSON configuration files for scoring weights (`scoring-config.json`)
3. Command-line arguments can override configuration values (future story)
4. IConfiguration injected into services via DI
5. Validation performed during configuration binding

**Affects Components:**
- Framework Filter Engine (loads blocklist/allowlist patterns)
- Scoring Calculator (loads configurable weights)
- CLI argument handling (merges command-line with config files - future)

**Configuration POCOs Pattern:**

Based on .NET 8 best practices [Source: Configuration in .NET | Microsoft Learn]:

```csharp
// Use sealed classes for performance in .NET 8
public sealed class FilterConfiguration
{
    // Property initializers provide defaults when file missing
    public List<string> BlockList { get; set; } = new()
    {
        "Microsoft.*",
        "System.*",
        "mscorlib",
        "netstandard"
    };

    public List<string> AllowList { get; set; } = new();
}

public sealed class ScoringConfiguration
{
    // Data annotations for validation
    [Range(0.0, 1.0)]
    public double Coupling { get; set; } = 0.40;

    [Range(0.0, 1.0)]
    public double Complexity { get; set; } = 0.30;

    [Range(0.0, 1.0)]
    public double TechDebt { get; set; } = 0.20;

    [Range(0.0, 1.0)]
    public double ExternalExposure { get; set; } = 0.10;
}
```

**ConfigurationBuilder Setup (Program.cs):**

Based on .NET 8 patterns [Source: Microsoft.Extensions.Configuration.Json 8.0.0]:

```csharp
using Microsoft.Extensions.Configuration;

public static async Task<int> Main(string[] args)
{
    MSBuildLocator.RegisterDefaults(); // FIRST LINE - already done in Story 1.3

    // Add configuration loading AFTER MSBuildLocator
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("filter-config.json", optional: true, reloadOnChange: false)
        .AddJsonFile("scoring-config.json", optional: true, reloadOnChange: false)
        .Build();

    // DI container setup
    var services = new ServiceCollection();
    services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
    services.AddSingleton<IConfiguration>(configuration);

    // Register options with validation
    services
        .AddOptions<FilterConfiguration>()
        .Bind(configuration.GetSection("FrameworkFilters"))
        .ValidateDataAnnotations()
        .ValidateOnStart(); // NEW in .NET 8 - validates at startup

    services
        .AddOptions<ScoringConfiguration>()
        .Bind(configuration.GetSection("ScoringWeights"))
        .Validate(config =>
        {
            var sum = config.Coupling + config.Complexity + config.TechDebt + config.ExternalExposure;
            return Math.Abs(sum - 1.0) < 0.01; // Allow tiny floating-point errors
        }, "Scoring weights must sum to 1.0")
        .ValidateDataAnnotations()
        .ValidateOnStart();

    var serviceProvider = services.BuildServiceProvider();

    // Rest of Program.cs...
}
```

**JSON Validation with Line Numbers:**

Based on .NET 8 JsonException capabilities:

```csharp
public class ConfigurationValidator
{
    public static (bool IsValid, List<string> Errors) ValidateJsonFile(
        string filePath,
        IAnsiConsole console)
    {
        var errors = new List<string>();

        if (!File.Exists(filePath))
        {
            return (true, errors); // Missing file is OK - defaults used
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var document = JsonDocument.Parse(json);
            // Successfully parsed
            return (true, errors);
        }
        catch (JsonException ex)
        {
            // Format error with Spectre.Console 3-part structure
            console.MarkupLine($"[red]Error:[/] JSON syntax error in [yellow]{filePath}[/]");
            console.MarkupLine($"[dim]Location:[/] Line {ex.LineNumber}, Position {ex.BytePositionInLine}");
            console.MarkupLine($"[dim]Details:[/] {ex.Message}");
            errors.Add($"{filePath}({ex.LineNumber},{ex.BytePositionInLine}): {ex.Message}");
            return (false, errors);
        }
    }
}
```

### Architecture Compliance

**Configuration Management Decision (Architecture core-architectural-decisions.md line 22-38):**
- Microsoft.Extensions.Configuration.Json (latest for .NET 8)
- Provides robust configuration management with JSON file support
- Supports hierarchical configuration and is fully testable via IConfiguration interface
- IConfiguration injected into services via DI
- Validation performed during configuration binding

**PascalCase Naming Pattern (Architecture implementation-patterns-consistency-rules.md line 48-66):**
- MUST use PascalCase for all JSON property names
- Pattern: Match C# POCO property names exactly
- Enables automatic binding without JsonPropertyName attributes
- Anti-Pattern: ❌ `"blockList"` or `"block_list"`

**File Organization (Architecture project-structure-boundaries.md):**
- Configuration POCOs in Core project: `src/MasDependencyMap.Core/Configuration/`
- Feature-based namespace: `MasDependencyMap.Core.Configuration`
- Sample config files in repository root (for reference)
- User config files in current directory at runtime

### Library/Framework Requirements

**Microsoft.Extensions.Configuration v8.0.0 Key APIs:**

1. **ConfigurationBuilder:**
   - `SetBasePath(string)` - Sets directory for relative paths
   - `AddJsonFile(string, optional, reloadOnChange)` - Adds JSON configuration source
   - `Build()` - Creates IConfiguration instance

2. **IConfiguration Interface:**
   - `GetSection(string)` - Gets configuration subsection
   - `GetValue<T>(string)` - Gets typed configuration value
   - Injected via DI for testability

3. **IOptions<T> Pattern:**
   - `IOptions<T>` - Singleton, immutable after creation
   - `IOptionsSnapshot<T>` - Scoped, supports runtime changes (not needed for MVP)
   - `AddOptions<T>()` - Registers options in DI container
   - `Bind(IConfiguration)` - Binds configuration to POCO
   - `ValidateDataAnnotations()` - Validates using [Range], [Required], etc.
   - `ValidateOnStart()` - NEW in .NET 8, validates at app startup

4. **JsonDocument (System.Text.Json):**
   - `JsonDocument.Parse(string)` - Parses JSON with validation
   - `JsonException` properties: `LineNumber`, `BytePositionInLine`, `Message`
   - Used for syntax validation before configuration binding

**Required NuGet Packages (Already Installed in Story 1.2):**
- ✅ Microsoft.Extensions.Configuration.Json (included with Microsoft.Extensions.DependencyInjection)
- ✅ System.Text.Json (included with .NET 8 SDK)

**Data Annotations (System.ComponentModel.DataAnnotations):**
- `[Range(min, max)]` - Validates numeric values in range
- `[Required]` - Validates non-null values
- Already available in .NET 8 SDK, no additional package needed

### File Structure Requirements

**Files to Create:**

1. **src/MasDependencyMap.Core/Configuration/FilterConfiguration.cs** (new)
   - Sealed POCO class with BlockList and AllowList properties
   - Default values in property initializers
   - Namespace: `MasDependencyMap.Core.Configuration`

2. **src/MasDependencyMap.Core/Configuration/ScoringConfiguration.cs** (new)
   - Sealed POCO class with weight properties
   - Data annotations for validation ([Range(0.0, 1.0)])
   - Default weights: 0.40/0.30/0.20/0.10
   - Namespace: `MasDependencyMap.Core.Configuration`

3. **filter-config.json** (new, repository root)
   - Sample configuration with Microsoft.*/System.* defaults
   - PascalCase property names
   - Comments explaining each section

4. **scoring-config.json** (new, repository root)
   - Sample configuration with default weights
   - PascalCase property names
   - Comments explaining weight meanings

5. **src/MasDependencyMap.CLI/Program.cs** (modify)
   - Add ConfigurationBuilder setup after MSBuildLocator
   - Register IConfiguration in DI container
   - Register IOptions<FilterConfiguration> and IOptions<ScoringConfiguration>
   - Add validation error handling at startup

**Optional (Can Defer to Later Story):**
- `src/MasDependencyMap.Core/Configuration/ConfigurationValidator.cs` - JSON syntax validator
  - Only if validation logic becomes complex
  - Can inline in Program.cs for MVP

**Expected File Structure After This Story:**
```
masDependencyMap/
├── src/
│   ├── MasDependencyMap.Core/
│   │   ├── Configuration/
│   │   │   ├── FilterConfiguration.cs (new)
│   │   │   └── ScoringConfiguration.cs (new)
│   │   └── MasDependencyMap.Core.csproj
│   └── MasDependencyMap.CLI/
│       ├── Program.cs (modified - add configuration)
│       └── MasDependencyMap.CLI.csproj
├── filter-config.json (new, sample)
├── scoring-config.json (new, sample)
└── .gitignore (add *.local.json entries)
```

### Testing Requirements

**Manual Testing Checklist:**

All tests run via `dotnet run --project src/MasDependencyMap.CLI` from solution root:

1. **With Both Config Files Present:**
   ```bash
   # Create sample configs first (use provided samples)
   dotnet run -- analyze --solution test.sln
   # Expected: Configuration loaded successfully, custom filters applied
   # Expected: Custom scoring weights used
   ```

2. **With Config Files Missing:**
   ```bash
   # Rename or delete config files temporarily
   dotnet run -- analyze --solution test.sln
   # Expected: Default configuration used (Microsoft.*/System.* blocked)
   # Expected: Default weights: 0.40/0.30/0.20/0.10
   # Expected: No errors, runs normally
   ```

3. **With Malformed JSON (Syntax Error):**
   ```bash
   # Edit filter-config.json, add syntax error (missing comma, bracket)
   dotnet run -- analyze --solution test.sln
   # Expected: "[red]Error:[/] JSON syntax error in filter-config.json"
   # Expected: "[dim]Location:[/] Line X, Position Y"
   # Expected: "[dim]Details:[/] {specific error message}"
   # Expected: Exit code 1
   ```

4. **With Invalid Weight Values:**
   ```bash
   # Edit scoring-config.json, set Coupling = 1.5 (outside 0.0-1.0)
   dotnet run -- analyze --solution test.sln
   # Expected: Validation error at startup
   # Expected: "Scoring weight 'Coupling' must be between 0.0 and 1.0"
   # Expected: Exit code 1
   ```

5. **With Weights Not Summing to 1.0:**
   ```bash
   # Edit scoring-config.json, weights sum to 0.9 instead of 1.0
   dotnet run -- analyze --solution test.sln
   # Expected: Validation error at startup
   # Expected: "Scoring weights must sum to 1.0"
   # Expected: Exit code 1
   ```

**Success Criteria:**
- All 5 test scenarios pass
- Configuration loaded from JSON files when present
- Sensible defaults used when files missing
- JSON syntax errors reported with line numbers
- Validation errors displayed with Spectre.Console formatting
- Exit codes correct (0 = success, 1 = validation error)

**Unit Testing (Optional for This Story):**
- Can add unit tests for FilterConfiguration and ScoringConfiguration POCOs
- Test default value initialization
- Test data annotation validation
- IConfiguration abstraction enables testing via in-memory configuration

### Previous Story Intelligence

**From Story 1-3 (Completed):**
- ✅ Program.cs already has MSBuildLocator.RegisterDefaults() as first line
- ✅ DI container (ServiceCollection) already set up
- ✅ IAnsiConsole registered and working
- ✅ Async Main method: `static async Task<int> Main(string[] args)` ✓
- ✅ System.CommandLine command parsing working
- ✅ Spectre.Console output formatting working
- ✅ Exit code handling implemented (0 = success, non-zero = error)

**What This Enables:**
- Can add configuration loading between MSBuildLocator and DI setup
- Can register IConfiguration in existing ServiceCollection
- Can use IOptions<T> pattern with existing DI container
- Can display validation errors with existing Spectre.Console setup

**Integration Points:**
- Insert ConfigurationBuilder after line 14 (MSBuildLocator call)
- Add IConfiguration registration after line 18 (IAnsiConsole registration)
- Add IOptions registrations before serviceProvider build (line 21)
- Add validation error handling before rootCommand setup

**From Story 1-2 (Completed):**
- ✅ Microsoft.Extensions.DependencyInjection v10.0.2 installed
- ✅ Includes Microsoft.Extensions.Configuration.Json dependency
- ✅ System.Text.Json available (included with .NET 8 SDK)
- ✅ All packages verified working

**What to Leverage:**
- Configuration packages already available, no new NuGet installs needed
- JsonDocument class available for syntax validation
- Data annotations available for POCO validation

### Git Intelligence Summary

**Recent Commits (Last 5):**
1. `01e1477` - Story 1-2: Add verification evidence and update status to done
2. `9221d68` - Update Claude Code bash permissions for development workflow
3. `0d09a91` - Add NuGet dependencies to Core/CLI/Tests projects
4. `9d92fa3` - Initial commit: .NET 8 solution structure

**Recent File Changes:**
- ✅ Program.cs modified in Story 1-3 (CLI implementation)
- ✅ .csproj files modified in Story 1-2 (NuGet packages)
- ✅ Story markdown files added per story completion
- ✅ sprint-status.yaml updated per story progression

**Expected Commit for This Story:**
```bash
git add src/MasDependencyMap.Core/Configuration/FilterConfiguration.cs
git add src/MasDependencyMap.Core/Configuration/ScoringConfiguration.cs
git add src/MasDependencyMap.CLI/Program.cs
git add filter-config.json
git add scoring-config.json
git add .gitignore

git commit -m "Implement configuration management with JSON support

- Create FilterConfiguration POCO with BlockList/AllowList defaults
- Create ScoringConfiguration POCO with weight validation
- Integrate Microsoft.Extensions.Configuration in Program.cs
- Add IOptions pattern with ValidateOnStart for .NET 8
- Implement JSON syntax validation with line number reporting
- Create sample filter-config.json and scoring-config.json
- Add validation for weight ranges (0.0-1.0) and sum to 1.0
- Use PascalCase for JSON properties per architecture patterns
- Default fallback when config files missing (Microsoft.*/System.* blocked)
- Manual testing confirms all 5 acceptance criteria scenarios

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

**Files to Stage:**
- src/MasDependencyMap.Core/Configuration/FilterConfiguration.cs (new)
- src/MasDependencyMap.Core/Configuration/ScoringConfiguration.cs (new)
- src/MasDependencyMap.CLI/Program.cs (modified)
- filter-config.json (new)
- scoring-config.json (new)
- .gitignore (modified - add *.local.json)

### Latest Tech Information (Web Research - 2026)

**Microsoft.Extensions.Configuration Best Practices for .NET 8:**

Sources:
- [Configuration in .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [Options pattern in .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- [Microsoft.Extensions.Configuration.Json 8.0.0 | NuGet](https://www.nuget.org/packages/microsoft.extensions.configuration.json/8.0.0)

**Key Insights:**

1. **ValidateOnStart is NEW in .NET 8:**
   - Call `.ValidateOnStart()` on IOptions<T> registration
   - Validates configuration at app startup, not first use
   - Fails fast if configuration is invalid
   - Prevents runtime surprises

2. **PascalCase is Standard for .NET Configuration:**
   - Use PascalCase in JSON: `"BlockList"`, `"AllowList"`
   - Matches C# property names exactly
   - Enables automatic binding without attributes
   - Simplifies property mapping

3. **ConfigurationBuilder Pattern:**
   ```csharp
   var configuration = new ConfigurationBuilder()
       .SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
       .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
       .AddEnvironmentVariables()
       .Build();
   ```
   - SetBasePath required for relative file paths
   - optional: true allows missing files (uses defaults)
   - reloadOnChange: true for hot reload (not needed for CLI tool)

4. **IOptions vs IOptionsSnapshot:**
   - IOptions<T>: Singleton, immutable after registration (use for CLI tool)
   - IOptionsSnapshot<T>: Scoped, supports runtime changes (not needed for MVP)
   - IOptionsMonitor<T>: Singleton, supports runtime changes (not needed for MVP)

5. **JSON Validation with JsonDocument:**
   - `JsonDocument.Parse()` validates JSON syntax
   - `JsonException` includes LineNumber and BytePositionInLine
   - Provides precise error location for user feedback

6. **Default Value Patterns:**
   ```csharp
   public List<string> BlockList { get; set; } = new()
   {
       "Microsoft.*",
       "System.*"
   };
   ```
   - Property initializers provide defaults
   - Configuration binding overwrites if file exists
   - Falls back to defaults if file missing

**Sample Configuration Files:**

**filter-config.json:**
```json
{
  "FrameworkFilters": {
    "BlockList": [
      "Microsoft.*",
      "System.*",
      "mscorlib",
      "netstandard"
    ],
    "AllowList": [
      "YourCompany.*"
    ]
  }
}
```

**scoring-config.json:**
```json
{
  "ScoringWeights": {
    "Coupling": 0.40,
    "Complexity": 0.30,
    "TechDebt": 0.20,
    "ExternalExposure": 0.10
  }
}
```

### Project Context Reference

🔬 **Complete project rules:** See `_bmad-output/project-context.md` for comprehensive guidelines.

**Most Relevant Sections for This Story:**

1. **Configuration Loading Rules (lines 108-113):**
   - JSON files MUST use PascalCase property names
   - Load from current directory by default
   - Use IConfiguration injection, NOT direct JsonSerializer

2. **PascalCase JSON Convention (lines 49-65):**
   - Match C# POCO property names exactly
   - Enables automatic binding
   - Anti-pattern: camelCase or snake_case

3. **Feature-Based Namespaces (lines 54-59):**
   - Use `MasDependencyMap.Core.Configuration`
   - NOT layer-based like `MasDependencyMap.Core.Models`

4. **File Naming Convention (lines 164-167):**
   - File names MUST match class names exactly
   - FilterConfiguration.cs (not filter-configuration.cs)

5. **Async All The Way (lines 294-298):**
   - Configuration loading is synchronous (allowed exception)
   - Main method already async from Story 1.3

6. **Console Output Discipline (lines 288-292):**
   - Use IAnsiConsole for validation errors
   - 3-part error structure for user-facing errors

**Critical Rules Checklist:**
- [x] PascalCase for all JSON property names ✅ CRITICAL
- [x] Feature-based namespace: `MasDependencyMap.Core.Configuration` ✅
- [x] File names match class names exactly ✅
- [x] IConfiguration injection via DI ✅
- [x] Validation errors via Spectre.Console markup ✅
- [x] Default values in property initializers ✅

### Implementation Guidance

**Step-by-Step Implementation:**

**Phase 1: Create Configuration POCOs**

1. **Create FilterConfiguration.cs:**
   ```csharp
   using System.Collections.Generic;

   namespace MasDependencyMap.Core.Configuration;

   public sealed class FilterConfiguration
   {
       public List<string> BlockList { get; set; } = new()
       {
           "Microsoft.*",
           "System.*",
           "mscorlib",
           "netstandard"
       };

       public List<string> AllowList { get; set; } = new();
   }
   ```

2. **Create ScoringConfiguration.cs:**
   ```csharp
   using System.ComponentModel.DataAnnotations;

   namespace MasDependencyMap.Core.Configuration;

   public sealed class ScoringConfiguration
   {
       [Range(0.0, 1.0, ErrorMessage = "Coupling weight must be between 0.0 and 1.0")]
       public double Coupling { get; set; } = 0.40;

       [Range(0.0, 1.0, ErrorMessage = "Complexity weight must be between 0.0 and 1.0")]
       public double Complexity { get; set; } = 0.30;

       [Range(0.0, 1.0, ErrorMessage = "TechDebt weight must be between 0.0 and 1.0")]
       public double TechDebt { get; set; } = 0.20;

       [Range(0.0, 1.0, ErrorMessage = "ExternalExposure weight must be between 0.0 and 1.0")]
       public double ExternalExposure { get; set; } = 0.10;
   }
   ```

**Phase 2: Create Sample JSON Files**

3. **Create filter-config.json in repository root:**
   ```json
   {
     "FrameworkFilters": {
       "BlockList": [
         "Microsoft.*",
         "System.*",
         "mscorlib",
         "netstandard"
       ],
       "AllowList": []
     }
   }
   ```

4. **Create scoring-config.json in repository root:**
   ```json
   {
     "ScoringWeights": {
       "Coupling": 0.40,
       "Complexity": 0.30,
       "TechDebt": 0.20,
       "ExternalExposure": 0.10
     }
   }
   ```

**Phase 3: Update Program.cs**

5. **Add using directives:**
   ```csharp
   using Microsoft.Extensions.Configuration;
   using MasDependencyMap.Core.Configuration;
   using System.Text.Json;
   ```

6. **Add ConfigurationBuilder after MSBuildLocator (around line 16):**
   ```csharp
   MSBuildLocator.RegisterDefaults(); // FIRST LINE - already there

   // Add configuration loading
   var configuration = new ConfigurationBuilder()
       .SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("filter-config.json", optional: true, reloadOnChange: false)
       .AddJsonFile("scoring-config.json", optional: true, reloadOnChange: false)
       .Build();

   // Validate JSON syntax before proceeding
   var ansiConsoleEarly = AnsiConsole.Console; // For validation errors
   var (isValid, errors) = ValidateConfigurationFiles(ansiConsoleEarly);
   if (!isValid)
   {
       return 1; // Exit with error code
   }
   ```

7. **Add JSON validation helper method:**
   ```csharp
   private static (bool IsValid, List<string> Errors) ValidateConfigurationFiles(IAnsiConsole console)
   {
       var errors = new List<string>();
       var files = new[] { "filter-config.json", "scoring-config.json" };

       foreach (var file in files)
       {
           if (!File.Exists(file)) continue; // Missing is OK - defaults used

           try
           {
               var json = File.ReadAllText(file);
               JsonDocument.Parse(json).Dispose();
           }
           catch (JsonException ex)
           {
               console.MarkupLine($"[red]Error:[/] JSON syntax error in [yellow]{file}[/]");
               console.MarkupLine($"[dim]Location:[/] Line {ex.LineNumber}, Position {ex.BytePositionInLine}");
               console.MarkupLine($"[dim]Details:[/] {ex.Message}");
               errors.Add($"{file}({ex.LineNumber},{ex.BytePositionInLine}): {ex.Message}");
           }
       }

       return (errors.Count == 0, errors);
   }
   ```

8. **Register IConfiguration in DI container (after IAnsiConsole registration):**
   ```csharp
   services.AddSingleton<IConfiguration>(configuration);
   ```

9. **Register IOptions with validation (after IConfiguration registration):**
   ```csharp
   // Register FilterConfiguration with validation
   services
       .AddOptions<FilterConfiguration>()
       .Bind(configuration.GetSection("FrameworkFilters"))
       .ValidateOnStart();

   // Register ScoringConfiguration with validation
   services
       .AddOptions<ScoringConfiguration>()
       .Bind(configuration.GetSection("ScoringWeights"))
       .Validate(config =>
       {
           var sum = config.Coupling + config.Complexity + config.TechDebt + config.ExternalExposure;
           return Math.Abs(sum - 1.0) < 0.01; // Allow tiny floating-point errors
       }, "Scoring weights must sum to 1.0")
       .ValidateDataAnnotations()
       .ValidateOnStart();
   ```

10. **Handle validation errors at startup:**
    ```csharp
    try
    {
        var serviceProvider = services.BuildServiceProvider();
        // Trigger ValidateOnStart by accessing options
        _ = serviceProvider.GetRequiredService<IOptions<FilterConfiguration>>().Value;
        _ = serviceProvider.GetRequiredService<IOptions<ScoringConfiguration>>().Value;
    }
    catch (OptionsValidationException ex)
    {
        ansiConsoleEarly.MarkupLine("[red]Error:[/] Configuration validation failed");
        ansiConsoleEarly.MarkupLine($"[dim]Details:[/] {ex.Message}");
        foreach (var failure in ex.Failures)
        {
            ansiConsoleEarly.MarkupLine($"[dim]  - {failure}[/]");
        }
        return 1;
    }
    ```

**Phase 4: Update .gitignore**

11. **Add .gitignore entries for user-specific config overrides:**
    ```gitignore
    # User-specific configuration overrides
    *config.local.json
    ```

**Common Pitfalls to Avoid:**

1. ❌ Using camelCase in JSON files (use PascalCase)
2. ❌ Forgetting SetBasePath (files won't be found)
3. ❌ Not handling JsonException for syntax errors
4. ❌ Not validating weight sum equals 1.0
5. ❌ Using Console.WriteLine instead of IAnsiConsole for errors
6. ❌ Forgetting ValidateOnStart() (errors delayed until first use)
7. ❌ Not testing with missing config files (defaults must work)

### References

**Epic & Story Context:**
- [Epic 1: Project Foundation and Command-Line Interface, Story 1.4] - Story requirements
- [Story 1.4 Acceptance Criteria] - Specific validation and default behavior

**Architecture Documents:**
- [Architecture: core-architectural-decisions.md lines 22-38] - Configuration Management decision
- [Architecture: implementation-patterns-consistency-rules.md lines 48-66] - PascalCase JSON convention
- [Architecture: implementation-patterns-consistency-rules.md lines 110-118] - Configuration file locations

**Project Context:**
- [project-context.md lines 108-113] - Configuration loading rules
- [project-context.md lines 49-65] - PascalCase JSON pattern
- [project-context.md lines 54-59] - Feature-based namespace organization

**Previous Stories:**
- [Story 1-3: Implement Basic CLI with System.CommandLine] - Program.cs baseline with DI setup
- [Story 1-2: Install Core NuGet Dependencies] - Configuration packages available

**External Resources (Web Research 2026):**
- [Configuration in .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [Options pattern in .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- [Microsoft.Extensions.Configuration.Json 8.0.0](https://www.nuget.org/packages/microsoft.extensions.configuration.json/8.0.0)
- [JsonConfigurationExtensions.AddJsonFile Method](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.jsonconfigurationextensions.addjsonfile)
- [How to customize property names with System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties)

### Story Completion Status

✅ **Ultimate BMad Method STORY CONTEXT CREATED**

**Artifacts Analyzed:**
- Epic 1 full context (126 lines, 8 stories)
- Story 1-3 previous story (735 lines, comprehensive dev notes and completion evidence)
- Architecture core decisions (254 lines, Configuration Management section)
- Architecture implementation patterns (399 lines, PascalCase and configuration patterns)
- Project context (344 lines, critical rules)
- Git history (3 commits with file changes analyzed)
- Web research (Comprehensive .NET 8 configuration best practices from Microsoft Learn)

**Context Provided:**
- ✅ Exact implementation pattern with step-by-step code examples
- ✅ FilterConfiguration and ScoringConfiguration POCO structures with defaults
- ✅ ConfigurationBuilder setup with JSON file loading
- ✅ IOptions pattern with ValidateOnStart for .NET 8
- ✅ JSON syntax validation with line number reporting
- ✅ Weight sum validation (must equal 1.0)
- ✅ Sample JSON file formats with PascalCase convention
- ✅ Integration with existing Program.cs from Story 1.3
- ✅ Manual testing checklist with 5 acceptance criteria scenarios
- ✅ Default fallback behavior when files missing
- ✅ Architecture compliance mapped to decisions
- ✅ Previous story learnings (DI container ready, packages installed)
- ✅ Git commit pattern for completion
- ✅ Latest 2026 web research on .NET 8 configuration patterns
- ✅ Complete implementation guidance with all code examples
- ✅ Common pitfalls to avoid
- ✅ All references with source paths and line numbers

**Developer Readiness:** 🎯 READY FOR FLAWLESS IMPLEMENTATION

**Critical Success Factors:**
1. PascalCase for all JSON property names (matches C# conventions)
2. Property initializers provide defaults when files missing
3. ValidateOnStart() for early error detection (.NET 8 feature)
4. JSON syntax validation with line number reporting
5. Weight sum validation (must equal 1.0 within floating-point tolerance)
6. IOptions<T> pattern for testable configuration injection
7. Manual testing covers all 5 acceptance criteria scenarios

**What Developer Should Do:**
1. Follow step-by-step implementation guidance in "Implementation Guidance" section
2. Create Configuration folder in Core project first
3. Create POCOs with default values in property initializers
4. Create sample JSON files with PascalCase property names
5. Update Program.cs with configuration loading and validation
6. Run all 5 manual test scenarios in "Testing Requirements"
7. Create git commit using pattern in "Git Intelligence Summary"
8. Verify all acceptance criteria satisfied before marking done

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

### Completion Notes List

✅ **Configuration POCOs Created** (src/MasDependencyMap.Core/Configuration/)
- FilterConfiguration.cs: Sealed class with BlockList/AllowList properties, defaults to Microsoft.*/System.*/mscorlib/netstandard
- ScoringConfiguration.cs: Sealed class with weight properties, [Range(0.0, 1.0)] validation on all weights

✅ **ConfigurationBuilder Integrated** (src/MasDependencyMap.CLI/Program.cs)
- Added Microsoft.Extensions.Configuration.Json support
- Loads filter-config.json and scoring-config.json with optional: true
- JSON syntax validation BEFORE Build() to catch malformed JSON early
- IConfiguration registered in DI container
- ValidateConfigurationFiles helper method with Spectre.Console error formatting

✅ **IOptions Pattern with .NET 8 ValidateOnStart**
- FilterConfiguration bound to "FrameworkFilters" section
- ScoringConfiguration bound to "ScoringWeights" section with sum validation
- ValidateDataAnnotations() checks weight ranges (0.0-1.0)
- Custom Validate() ensures weights sum to 1.0 (±0.01 floating-point tolerance)
- ValidateOnStart() triggers validation at app startup (fails fast)

✅ **Sample Configuration Files**
- filter-config.json: PascalCase properties, Microsoft.*/System.*/mscorlib/netstandard defaults
- scoring-config.json: PascalCase properties, weights 0.40/0.30/0.20/0.10

✅ **Error Handling & Validation**
- JSON syntax errors: Line number and position reported via JsonException
- Malformed markup escaping: ex.Message.EscapeMarkup() prevents Spectre.Console parsing errors
- Weight validation: Range and sum checks with descriptive error messages
- Exit code 1 on validation failures

✅ **Configuration Smoke Test Output**
- Startup displays loaded configuration summary after successful validation
- Shows blocklist/allowlist pattern counts
- Shows all four scoring weights (Coupling, Complexity, TechDebt, ExternalExposure)
- Confirms configuration loaded before proceeding to command execution
- Example: "✓ Configuration loaded successfully / Blocklist patterns: 4 / Scoring weights: C=0.40, Cx=0.30, TD=0.20, EE=0.10"

✅ **All 5 Acceptance Tests Passed with Evidence**

**Test 1: Config Files Present**
```
$ dotnet run --project src/MasDependencyMap.CLI -- analyze --solution masDependencyMap.sln
✓ Configuration loaded successfully
  Blocklist patterns: 8
  Allowlist patterns: 0
  Scoring weights: C=0.40, Cx=0.30, TD=0.20, EE=0.10
Parsed Options:
  Solution: D:\work\masDependencyMap\masDependencyMap.sln
  ...
✓ Analysis command received successfully!
Exit code: 0
```

**Test 2: Config Files Missing (Defaults Used)**
```
$ mv filter-config.json filter-config.json.backup
$ mv scoring-config.json scoring-config.json.backup
$ dotnet run --project src/MasDependencyMap.CLI -- analyze --solution masDependencyMap.sln
✓ Configuration loaded successfully
  Blocklist patterns: 4
  Allowlist patterns: 0
  Scoring weights: C=0.40, Cx=0.30, TD=0.20, EE=0.10
✓ Analysis command received successfully!
Exit code: 0
```

**Test 3: Malformed JSON (Missing Comma on Line 5)**
```
$ # Modified filter-config.json to remove comma after "System.*"
$ dotnet run --project src/MasDependencyMap.CLI -- analyze --solution masDependencyMap.sln
Error: JSON syntax error in filter-config.json
Location: Line 5, Position 6
Details: '"' is invalid after a value. Expected either ',', '}', or ']'.
LineNumber: 5 | BytePositionInLine: 6.
Exit code: 1
```

**Test 4: Invalid Weight Value (1.5 Outside Range)**
```
$ # Modified scoring-config.json: "Coupling": 1.5
$ dotnet run --project src/MasDependencyMap.CLI -- analyze --solution masDependencyMap.sln
Error: Configuration validation failed
Details: Scoring weights must sum to 1.0; DataAnnotation validation failed for
'ScoringConfiguration' members: 'Coupling' with the error: 'Coupling weight must
be between 0.0 and 1.0'.
  - Scoring weights must sum to 1.0
  - DataAnnotation validation failed for 'ScoringConfiguration' members:
'Coupling' with the error: 'Coupling weight must be between 0.0 and 1.0'.
Exit code: 1
```

**Test 5: Weights Not Summing to 1.0 (Sum = 0.90)**
```
$ # Modified scoring-config.json: weights sum to 0.90
$ dotnet run --project src/MasDependencyMap.CLI -- analyze --solution masDependencyMap.sln
Error: Configuration validation failed
Details: Scoring weights must sum to 1.0
  - Scoring weights must sum to 1.0
Exit code: 1
```

✅ **Additional Package Installed**
- Microsoft.Extensions.Options.DataAnnotations v10.0.2
  - **Why needed:** Provides the `ValidateDataAnnotations()` extension method for IOptions configuration validation
  - **Discovered during:** Implementation - Microsoft.Extensions.DependencyInjection doesn't include this extension
  - **Purpose:** Enables automatic validation of [Range] attributes on ScoringConfiguration properties
  - **Architecture compliance:** Follows .NET 8 options pattern best practices

### File List

**New Files:**
- src/MasDependencyMap.Core/Configuration/FilterConfiguration.cs
- src/MasDependencyMap.Core/Configuration/ScoringConfiguration.cs
- filter-config.json
- scoring-config.json

**Modified Files:**
- src/MasDependencyMap.CLI/Program.cs (added configuration loading, validation, and smoke test output)
- src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj (added Microsoft.Extensions.Options.DataAnnotations package)
- .gitignore (added *config.local.json pattern for user-specific config overrides)

**Excluded from File List:**
- .claude/settings.local.json (IDE/tool-specific configuration, not part of application source code)
- Other tool/IDE folders (.cursor/, .windsurf/) are excluded per architecture guidelines
