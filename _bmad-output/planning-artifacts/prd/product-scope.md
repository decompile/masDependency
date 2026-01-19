# Product Scope

## MVP - Minimum Viable Product (12 Weeks)

A Roslyn-based console tool with Graphviz visualization that transforms dependency graphs into actionable migration intelligence through three phases:

1. **Noise Filtering (Weeks 1-4):** Framework dependency filtering to reveal actual architecture
2. **Cycle Detection (Weeks 5-8):** Identify circular dependencies and suggest break points
3. **Extraction Scoring (Weeks 9-12):** Calculate 0-100 difficulty scores for migration candidates

**Core Technologies:** .NET 6+, Roslyn, QuikGraph, Graphviz
**Output Formats:** DOT files, PNG/SVG visualizations, text reports, CSV exports
**Delivery:** Standalone executable with source code on GitHub

*Detailed MVP implementation plan, resource requirements, and risk mitigation documented in Project Scoping section below.*

## Growth Features (Post-MVP)

**Phase 4 Enhancements - Future Releases:**

**Web Dashboard:**
- Interactive web UI with Cytoscape.js for graph exploration
- Real-time filtering and drill-down capabilities
- Collaborative sharing of analysis results
- Browser-based accessibility without Graphviz installation

**What-If Simulator:**
- Interactive impact analysis: "What if I break this dependency?"
- Cascading effect visualization
- Migration scenario comparison
- Risk assessment for proposed changes

**Advanced Dependency Analysis:**
- Stored procedure dependency analysis (parse SQL for table usage patterns)
- COM+ component reverse engineering and interface mapping
- Dynamic loading and reflection-based dependency detection
- External API contract analysis beyond basic scanning

**IDE Integration:**
- Visual Studio extension for in-IDE dependency exploration
- Real-time dependency impact hints during development
- Integration with Solution Explorer and Architecture Explorer
- Context menu actions for quick dependency queries

## Vision (Future)

**2-3 Year Product Evolution:**

If masDependencyMap proves successful in guiding safe legacy modernization, it evolves from a single-purpose migration planning tool into a comprehensive **Legacy Modernization Intelligence Platform**.

**Platform Expansion:**
- Support additional languages and platforms (Java, Python, JavaScript/TypeScript monoliths)
- Cloud-native architecture analysis (Kubernetes dependencies, service mesh complexity)
- Multi-repository analysis for microservices ecosystems
- Cross-language dependency tracking (polyglot architectures)

**Market Expansion:**
- Open source with community contributions
- Consulting services for legacy modernization
- Potential SaaS offering for team collaboration
- Integration with architecture documentation tools (C4 model, Arc42)

**Long-Term Vision:**
masDependencyMap becomes the **de facto standard** for data-driven legacy modernization planning, replacing manual analysis and guesswork with automated intelligence. The tool evolves from answering "where do we start?" to answering "how do we continuously evolve our architecture to maintain business agility?"
