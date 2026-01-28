# Configuration Guide

JSON configuration reference for masDependencyMap.

## Overview

masDependencyMap supports optional JSON configuration files for customizing analysis behavior:

- **filter-config.json** - Control which dependencies are considered "framework" vs "application"
- **scoring-config.json** - Customize extraction difficulty scoring weights

**Key Points:**
- Configuration files are **optional** - sensible defaults are used if not provided
- Files are loaded from the current directory by default
- Use `--config` flag to specify alternative configuration file location
- JSON syntax is validated before analysis starts
- Invalid configuration causes immediate error with specific line/column details

## Configuration Loading

### Loading Order

1. **Default Values**: Hardcoded sensible defaults
2. **filter-config.json**: Loaded if present in current directory
3. **scoring-config.json**: Loaded if present in current directory

### Validation

All configuration is validated at startup **before** analysis begins:

- **JSON Syntax**: Checked for valid JSON format
- **Property Names**: Must use PascalCase (e.g., `FrameworkFilters`, not `frameworkFilters`)
- **Value Ranges**: Numeric values must be within valid ranges
- **Sum Constraints**: Scoring weights must sum to exactly 1.0

**Validation Errors Show:**
```
Error: Configuration validation failed
Details: [Specific error message]
Suggestion: [How to fix]
```

**Validation errors include:**
- Exact line and column number for JSON syntax errors
- Specific property name for validation errors
- Clear explanation of what's wrong and how to fix it

## filter-config.json

### Purpose

Controls which dependencies are considered "framework" dependencies (and excluded from analysis) vs "application" dependencies (included in analysis).

**Use Cases:**
- Exclude common framework dependencies (Microsoft.*, System.*)
- Include company-specific internal frameworks for analysis
- Customize framework detection patterns per project

### File Format

```json
{
  "FrameworkFilters": {
    "BlockList": [
      "Microsoft.*",
      "System.*",
      "Newtonsoft.Json"
    ],
    "AllowList": [
      "YourCompany.*"
    ]
  }
}
```

### Property Reference

#### FrameworkFilters (Required)

Top-level object containing filter configuration.

- **Type**: Object
- **Required**: Yes
- **Properties**: `BlockList`, `AllowList`

#### BlockList (Optional)

List of glob patterns for dependencies to exclude from analysis.

- **Type**: Array of strings
- **Required**: No
- **Default**: `["Microsoft.*", "System.*"]`
- **Pattern Format**: Glob patterns with `*` wildcard
- **Example**: `"Microsoft.*"` matches `Microsoft.Extensions.Logging`, `Microsoft.AspNetCore.Mvc`, etc.

**Common Patterns:**
- `Microsoft.*` - All Microsoft framework libraries
- `System.*` - All System framework libraries
- `Newtonsoft.Json` - Specific library (exact match)
- `Castle.*` - All Castle libraries
- `log4net` - Exact match

**Behavior:**
- Dependencies matching BlockList patterns are considered "framework" and excluded from dependency graph
- Reduces noise by hiding standard framework dependencies
- Focuses analysis on application-level dependencies

#### AllowList (Optional)

List of glob patterns for dependencies to **override** BlockList and include in analysis.

- **Type**: Array of strings
- **Required**: No
- **Default**: `[]` (empty)
- **Pattern Format**: Glob patterns with `*` wildcard
- **Example**: `"YourCompany.*"` matches company-specific internal frameworks

**Common Patterns:**
- `YourCompany.*` - All company-internal libraries
- `YourCompany.Core.*` - Specific internal framework subset
- `InternalFramework` - Exact library name

**Behavior:**
- Dependencies matching AllowList patterns are included in analysis **even if** they match BlockList
- AllowList takes precedence over BlockList
- Useful for analyzing internal frameworks that follow framework naming conventions

**Example Use Case:**

Your company has internal libraries named `Microsoft.YourCompany.*` that follow Microsoft naming conventions but should be analyzed:

```json
{
  "FrameworkFilters": {
    "BlockList": [
      "Microsoft.*",
      "System.*"
    ],
    "AllowList": [
      "Microsoft.YourCompany.*"
    ]
  }
}
```

**Result:**
- `Microsoft.Extensions.Logging` → Excluded (matches BlockList)
- `Microsoft.YourCompany.Core` → **Included** (matches AllowList overrides BlockList)
- `System.Collections` → Excluded (matches BlockList)

### Complete Example

```json
{
  "FrameworkFilters": {
    "BlockList": [
      "Microsoft.*",
      "System.*",
      "Newtonsoft.Json",
      "AutoMapper",
      "Castle.*",
      "EntityFramework",
      "log4net",
      "NHibernate.*",
      "Dapper"
    ],
    "AllowList": [
      "Contoso.*",
      "Contoso.Internal.Framework.*"
    ]
  }
}
```

**This Configuration:**
- Excludes common third-party frameworks and ORMs
- Excludes all Microsoft and System libraries
- Includes company libraries (Contoso.*)
- Includes internal framework (Contoso.Internal.Framework.*)

### Validation Rules

**Property Names:**
- **MUST** use PascalCase: `FrameworkFilters`, `BlockList`, `AllowList`
- **NEVER** use camelCase: `frameworkFilters` is WRONG
- **NEVER** use snake_case: `block_list` is WRONG

**Pattern Format:**
- Use simple glob patterns with `*` wildcard
- Regular expressions are **NOT supported**
- Patterns are case-sensitive

**Valid Patterns:**
- `Microsoft.*` - Valid
- `System.*.dll` - Valid (matches System.Core.dll, System.Data.dll)
- `Contoso.?.Utilities` - **Invalid** (`?` not supported)
- `^Microsoft\..*$` - **Invalid** (regex not supported)

**Validation Errors:**

Invalid property name (camelCase instead of PascalCase):
```
Error: Configuration validation failed
Reason: Property 'frameworkFilters' not recognized
Suggestion: Use PascalCase: 'FrameworkFilters'
```

Invalid JSON syntax:
```
Error: JSON syntax error in filter-config.json
Location: Line 5, Position 12
Details: ',' expected
```

## scoring-config.json

### Purpose

Customizes weights for extraction difficulty scoring algorithm. The tool calculates an extraction difficulty score for each project based on four factors:

1. **Coupling**: Number of dependencies (incoming + outgoing)
2. **Complexity**: Cyclomatic complexity (via Roslyn analysis)
3. **Tech Debt**: Framework version age and obsolescence
4. **External Exposure**: Public API surface area (types, methods exposed)

**Use Cases:**
- Prioritize coupling over complexity for microservice extraction
- Emphasize tech debt when planning modernization
- Customize scoring for organization-specific priorities

### File Format

```json
{
  "ScoringWeights": {
    "Coupling": 0.40,
    "Complexity": 0.25,
    "TechDebt": 0.20,
    "ExternalExposure": 0.15
  }
}
```

### Property Reference

#### ScoringWeights (Required)

Top-level object containing scoring weight configuration.

- **Type**: Object
- **Required**: Yes
- **Properties**: `Coupling`, `Complexity`, `TechDebt`, `ExternalExposure`

#### Coupling (Required)

Weight for dependency coupling score.

- **Type**: Decimal number
- **Required**: Yes
- **Range**: 0.0 to 1.0
- **Default**: 0.40
- **Measures**: Number of project dependencies (incoming + outgoing)

**High Coupling Indicates:**
- Many dependencies on other projects
- Difficult to extract without breaking many references
- Likely to cause cascading changes

**When to Increase:**
- Prioritizing loosely-coupled microservices
- Planning extraction from tightly-coupled monolith
- Emphasizing dependency reduction

**When to Decrease:**
- Coupling is acceptable in your architecture
- Other factors more important

#### Complexity (Required)

Weight for cyclomatic complexity score.

- **Type**: Decimal number
- **Required**: Yes
- **Range**: 0.0 to 1.0
- **Default**: 0.25
- **Measures**: Cyclomatic complexity via Roslyn semantic analysis

**High Complexity Indicates:**
- Complex branching logic (if/switch statements)
- Difficult to understand and test
- Higher risk during refactoring

**When to Increase:**
- Planning refactoring efforts
- Emphasizing code quality improvements
- Team has complexity reduction goals

**When to Decrease:**
- Complexity acceptable or well-tested
- Coupling or tech debt higher priority

#### TechDebt (Required)

Weight for technology debt score.

- **Type**: Decimal number
- **Required**: Yes
- **Range**: 0.0 to 1.0
- **Default**: 0.20
- **Measures**: Framework version age and obsolescence

**High Tech Debt Indicates:**
- Old framework versions (.NET Framework 3.5, 4.0)
- Outdated dependencies requiring updates
- Security vulnerabilities in old frameworks

**When to Increase:**
- Planning modernization to .NET 8+
- Security compliance requirements
- Long-term maintenance concerns

**When to Decrease:**
- Framework versions acceptable
- Migration not a priority

#### ExternalExposure (Required)

Weight for external API exposure score.

- **Type**: Decimal number
- **Required**: Yes
- **Range**: 0.0 to 1.0
- **Default**: 0.15
- **Measures**: Public API surface area (types, methods, properties exposed)

**High External Exposure Indicates:**
- Many public APIs used by external consumers
- Breaking changes impact many clients
- API versioning and compatibility concerns

**When to Increase:**
- Planning API refactoring
- Breaking changes have high cost
- Emphasizing backward compatibility

**When to Decrease:**
- Internal projects with no external consumers
- API changes acceptable

### Weight Sum Constraint

**CRITICAL:** All four weights **MUST** sum to exactly 1.0.

**Valid Examples:**
- `0.40 + 0.25 + 0.20 + 0.15 = 1.0` ✓
- `0.50 + 0.30 + 0.10 + 0.10 = 1.0` ✓
- `0.25 + 0.25 + 0.25 + 0.25 = 1.0` ✓

**Invalid Examples:**
- `0.40 + 0.25 + 0.20 + 0.10 = 0.95` ✗ (sum < 1.0)
- `0.50 + 0.30 + 0.20 + 0.20 = 1.20` ✗ (sum > 1.0)

**Validation Error:**
```
Error: Configuration validation failed
Details: Scoring weights must sum to 1.0
Actual sum: 0.95
Suggestion: Adjust weights in scoring-config.json to sum to 1.0
```

**Floating-Point Tolerance:**
- Tiny floating-point errors (< 0.01) are tolerated
- `0.999999` is accepted as `1.0`
- Large deviations are rejected

### Complete Examples

#### Prioritize Coupling (Microservice Extraction)

Emphasize dependency coupling for microservice extraction planning:

```json
{
  "ScoringWeights": {
    "Coupling": 0.50,
    "Complexity": 0.20,
    "TechDebt": 0.15,
    "ExternalExposure": 0.15
  }
}
```

**Use Case:** Planning microservice boundaries, want to identify loosely-coupled extraction candidates.

#### Prioritize Tech Debt (Modernization)

Emphasize framework version age for modernization planning:

```json
{
  "ScoringWeights": {
    "Coupling": 0.20,
    "Complexity": 0.20,
    "TechDebt": 0.50,
    "ExternalExposure": 0.10
  }
}
```

**Use Case:** Planning .NET Framework to .NET 8 migration, prioritize old framework projects first.

#### Balanced Approach (Default)

Equal consideration for all factors:

```json
{
  "ScoringWeights": {
    "Coupling": 0.25,
    "Complexity": 0.25,
    "TechDebt": 0.25,
    "ExternalExposure": 0.25
  }
}
```

**Use Case:** General analysis without specific priorities.

#### Emphasize Complexity and External Exposure (Refactoring)

Focus on code quality and API stability:

```json
{
  "ScoringWeights": {
    "Coupling": 0.15,
    "Complexity": 0.40,
    "TechDebt": 0.10,
    "ExternalExposure": 0.35
  }
}
```

**Use Case:** Planning refactoring of complex public APIs.

### Validation Rules

**Property Names:**
- **MUST** use PascalCase: `ScoringWeights`, `Coupling`, `Complexity`, `TechDebt`, `ExternalExposure`
- **NEVER** use camelCase: `coupling` is WRONG

**Value Ranges:**
- Each weight **MUST** be between 0.0 and 1.0
- Weights **MUST** sum to exactly 1.0 (tolerance ±0.01)
- Negative weights are **INVALID**
- Weights > 1.0 are **INVALID**

**Validation Errors:**

Weights don't sum to 1.0:
```
Error: Configuration validation failed
Details: Scoring weights must sum to 1.0
Actual sum: 0.90
Suggestion: Adjust weights in scoring-config.json to sum to 1.0
```

Invalid property name:
```
Error: Configuration validation failed
Reason: Property 'coupling' not recognized
Suggestion: Use PascalCase: 'Coupling'
```

Weight out of range:
```
Error: Configuration validation failed
Details: Coupling weight must be between 0.0 and 1.0
Actual value: 1.5
Suggestion: Adjust Coupling weight to valid range
```

## Troubleshooting Configuration

### JSON Syntax Errors

**Symptom:**

```
Error: JSON syntax error in filter-config.json
Location: Line 5, Position 12
Details: ',' expected
```

**Solutions:**

1. **Use a JSON validator**: Paste your JSON into [jsonlint.com](https://jsonlint.com) to identify syntax errors
2. **Check common issues**:
   - Missing commas between array items: `["item1" "item2"]` → `["item1", "item2"]`
   - Extra comma after last item: `["item1", "item2",]` → `["item1", "item2"]`
   - Unquoted property names: `{BlockList: []}` → `{"BlockList": []}`
   - Single quotes instead of double quotes: `{'BlockList': []}` → `{"BlockList": []}`
3. **Use a JSON-aware editor**: VS Code, Visual Studio, or any editor with JSON syntax highlighting

**Common JSON Errors:**

Missing comma:
```json
{
  "FrameworkFilters": {
    "BlockList": [
      "Microsoft.*"
      "System.*"  // ERROR: Missing comma after "Microsoft.*"
    ]
  }
}
```

Extra comma:
```json
{
  "FrameworkFilters": {
    "BlockList": [
      "Microsoft.*",
      "System.*",  // ERROR: Extra comma after last item
    ]
  }
}
```

Single quotes:
```json
{
  'FrameworkFilters': {  // ERROR: Use double quotes
    'BlockList': []
  }
}
```

### Property Name Errors

**Symptom:**

```
Error: Configuration validation failed
Reason: Property 'frameworkFilters' not recognized
```

**Solution:**

Use **PascalCase** for all property names:
- ✓ `FrameworkFilters`
- ✗ `frameworkFilters`
- ✗ `framework_filters`

**Correct Property Names:**
- `FrameworkFilters` (not `frameworkFilters`)
- `BlockList` (not `blockList` or `block_list`)
- `AllowList` (not `allowList` or `allow_list`)
- `ScoringWeights` (not `scoringWeights`)
- `Coupling` (not `coupling`)
- `Complexity` (not `complexity`)
- `TechDebt` (not `techDebt` or `tech_debt`)
- `ExternalExposure` (not `externalExposure`)

### Weight Validation Errors

**Symptom:**

```
Error: Configuration validation failed
Details: Scoring weights must sum to 1.0
Actual sum: 0.95
```

**Solution:**

Adjust weights so they sum to exactly 1.0:

**Before (sum = 0.95):**
```json
{
  "ScoringWeights": {
    "Coupling": 0.40,
    "Complexity": 0.25,
    "TechDebt": 0.20,
    "ExternalExposure": 0.10  // Only adds to 0.95
  }
}
```

**After (sum = 1.0):**
```json
{
  "ScoringWeights": {
    "Coupling": 0.40,
    "Complexity": 0.25,
    "TechDebt": 0.20,
    "ExternalExposure": 0.15  // Now sums to 1.0
  }
}
```

**Calculation Tip:**

Use a calculator or spreadsheet to verify:
```
0.40 + 0.25 + 0.20 + 0.15 = 1.00 ✓
```

### Pattern Validation Errors

**Symptom:**

Patterns don't match expected dependencies.

**Solution:**

1. **Check pattern syntax**: Use simple glob patterns with `*` wildcard only
2. **Check case sensitivity**: Patterns are case-sensitive
3. **Use --verbose flag**: See which patterns matched which dependencies

**Valid Patterns:**
- `Microsoft.*` - Matches `Microsoft.Extensions.Logging`, `Microsoft.AspNetCore.Mvc`
- `System.*.dll` - Matches `System.Core.dll`, `System.Data.dll`
- `Contoso.Internal.*` - Matches `Contoso.Internal.Core`, `Contoso.Internal.Utils`

**Invalid Patterns:**
- `Microsoft.??` - `?` wildcard not supported
- `^Microsoft\..*$` - Regex not supported
- `{Microsoft,System}.*` - Brace expansion not supported

## Default Configuration

If no configuration files are provided, these defaults are used:

### Default filter-config.json

```json
{
  "FrameworkFilters": {
    "BlockList": [
      "Microsoft.*",
      "System.*"
    ],
    "AllowList": []
  }
}
```

**Behavior:**
- Excludes all Microsoft and System framework libraries
- Includes all application-level project dependencies

### Default scoring-config.json

```json
{
  "ScoringWeights": {
    "Coupling": 0.40,
    "Complexity": 0.25,
    "TechDebt": 0.20,
    "ExternalExposure": 0.15
  }
}
```

**Behavior:**
- Prioritizes dependency coupling (40%)
- Considers complexity (25%)
- Considers tech debt (20%)
- Considers external exposure (15%)

## Configuration Best Practices

### Start with Defaults

Start with default configuration and customize only if needed:

1. Run analysis without configuration files
2. Review results and identify noise (framework dependencies)
3. Add filter-config.json to exclude framework dependencies
4. Adjust scoring-config.json if priorities differ

### Validate JSON Syntax

Before running analysis:

1. Use [jsonlint.com](https://jsonlint.com) to validate syntax
2. Use JSON-aware editor (VS Code) for syntax highlighting
3. Run analysis to trigger validation before long-running operations

### Use Version Control

Store configuration files in version control:

- Track changes to configuration over time
- Share configuration across team
- Document why specific weights were chosen

**Example .gitignore:**

```
# Don't ignore configuration files (keep them in version control)
!filter-config.json
!scoring-config.json
```

### Document Customizations

Add comments in a separate markdown file explaining configuration choices:

**config-rationale.md:**

```markdown
# Configuration Rationale

## Filter Configuration

- **BlockList includes Dapper**: We treat Dapper as framework-level ORM
- **AllowList includes Contoso.Internal.***: Internal framework should be analyzed

## Scoring Configuration

- **Coupling weight = 0.50**: Emphasize loose coupling for microservice extraction
- **TechDebt weight = 0.15**: Framework versions acceptable, not prioritizing modernization
```

## See Also

- **[User Guide](user-guide.md)** - Command-line reference and usage examples
- **[Troubleshooting Guide](troubleshooting.md)** - Common issues and solutions
- **[README](../README.md)** - Quick start and installation
