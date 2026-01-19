# Project Scoping & Phased Development

## MVP Strategy & Philosophy

**MVP Approach:** Problem-Solving MVP

masDependencyMap follows a "fast time-to-insight" philosophy: deliver actionable migration intelligence that solves Alex's immediate problem (finding extraction starting points) through a pragmatic console tool, deferring polished UI and advanced features until the core analysis engine is validated.

**Key MVP Principle:** Get from "paralyzed by complexity" to "confident extraction candidates" in 12 weeks with a working tool that analyzes real codebases and produces real decisions.

**Resource Requirements:**
- **Team Size:** Solo developer (side project)
- **Development Time:** 12 weeks (10-15 hours per week commitment)
- **Technical Skills Required:** C#/.NET expertise, Roslyn familiarity, graph algorithms understanding
- **Infrastructure:** Local development only, no deployment infrastructure needed for MVP

**MVP Success Criteria:**
- Week 3: First filtered dependency graph showing real architecture
- Week 8: Cycle detection with break-point recommendations
- Week 12: Extraction difficulty scores for all projects, ready for first extraction decision

## MVP Feature Set (Phase 1-3: Weeks 1-12)

**Core User Journey Supported:**
- **Alex Chen - From Paralysis to Clarity** (primary journey)
  - Discovery & Setup (Week 1)
  - Full Ecosystem Analysis (Weeks 2-4)
  - Cycle Detection & Breaking Points (Weeks 5-8)
  - Extraction Difficulty Scoring (Weeks 9-12)

**Phase 1: Noise Filtering Foundation (Weeks 1-4)**

*Must-Have Capabilities:*
- Roslyn solution loader (.NET 6+ console application accepting .sln file paths)
- Framework dependency filter (configurable JSON blocklist for Microsoft.*, System.*, etc.)
- DOT format generator (Graphviz visualization output)
- Multi-solution support (analyze all 20 solutions simultaneously)
- Cross-solution dependency tracking with color coding

*Success Gate:* Can visualize dependencies for any solution with single command, framework noise eliminated, graph shows only custom code

**Phase 2: Cycle Detection Intelligence (Weeks 5-8)**

*Must-Have Capabilities:*
- Graph model & cycle detection (QuikGraph + Tarjan's SCC algorithm)
- Enhanced DOT visualization (cycle highlighting in RED, suggested break points in YELLOW)
- Cycle-breaking analysis (method call counting across boundaries, ranked weakest links)
- Report generation (TXT reports, CSV exports for tracking)

*Success Gate:* All circular dependency chains identified, visual graph shows cycles with suggested break points, ranked list of easiest dependencies to break

**Phase 3: Extraction Difficulty Scoring (Weeks 9-12)**

*Must-Have Capabilities:*
- Metric collection (coupling, cyclomatic complexity, technology version detection, external API exposure scanning)
- Scoring algorithm (0-100 extraction difficulty score with configurable weights)
- Enhanced DOT visualization (color-coded heat map: GREEN 0-33 easy, YELLOW 34-66 medium, RED 67-100 hard)
- Validation & tuning (manual review of top/bottom 10 candidates, scoring weight adjustment)

*Success Gate:* Every project has extraction difficulty score, top 10 easiest candidates align with architectural intuition (80%+ agreement), visual heat map provides immediate focus areas

**Essential MVP Documentation:**
- README.md with installation and basic usage
- Sample solution for testing (5-10 projects with intentional cycles)
- Command-line reference
- Troubleshooting guide (MSBuild errors, Graphviz issues)

**Explicitly OUT of MVP Scope:**
- Web dashboard (Phase 4)
- What-if simulator (Phase 4)
- Stored procedure dependency analysis (Phase 4)
- COM+ component reverse engineering (Phase 4)
- Visual Studio extension (Phase 4)
- NuGet global tool packaging (Phase 4 - MVP is standalone executable)

## Post-MVP Features

**Phase 4: Enhanced User Experience (Post-Week 12)**

*When:* After MVP validation (first successful component extraction using tool guidance)

*Features:*
- **Web Dashboard:** Interactive web UI with Cytoscape.js for graph exploration, real-time filtering, collaborative sharing
- **What-If Simulator:** Interactive impact analysis showing cascading effects of breaking dependencies
- **Advanced Dependency Analysis:** Stored procedure parsing, COM+ reverse engineering, reflection-based dependency detection
- **IDE Integration:** Visual Studio extension for in-IDE dependency exploration and real-time impact hints

*Trigger:* MVP proves useful for first extraction (Week 12-Month 6), tool recommendations validated by actual extraction results

**Phase 5: Platform Expansion (6-12 Months Post-MVP)**

*When:* After internal success, open source release gaining traction

*Features:*
- Support for Java, Python, JavaScript/TypeScript monoliths
- Multi-repository microservices analysis
- Cross-language dependency tracking for polyglot architectures
- Cloud-native architecture analysis (Kubernetes dependencies, service mesh)

*Trigger:* Community interest in open source project, requests for other language support

**Phase 6: Ecosystem & Market (12+ Months Post-MVP)**

*When:* Tool becomes standard part of modernization workflows

*Features:*
- SaaS offering for team collaboration
- Consulting services for legacy modernization
- Integration with architecture documentation tools (C4 model, Arc42)
- Marketplace for custom scoring algorithms and analyzers

*Trigger:* Multiple organizations using tool, demand for hosted/collaborative version

## Risk Mitigation Strategy

**Technical Risks:**

**Risk:** Roslyn fails to load .NET 3.5 legacy solutions
- **Mitigation:** MSBuild fallback approach - if Roslyn semantic analysis fails, fall back to project reference parsing only
- **Validation:** Test on actual legacy solution in Week 1
- **Contingency:** Document limitations, provide manual workarounds for unsupported scenarios

**Risk:** QuikGraph cycle detection performance issues on large graphs (1000+ projects)
- **Mitigation:** Implement progress indicators, optimize graph construction
- **Validation:** Test on full 20-solution ecosystem early (Week 5)
- **Contingency:** Provide per-solution analysis mode if multi-solution analysis is too slow

**Risk:** Extraction difficulty scoring doesn't align with architectural judgment
- **Mitigation:** Configurable scoring weights, manual validation in Week 12
- **Validation:** Top/bottom 10 candidates manual review process built into Phase 3
- **Contingency:** Iterate on scoring formula based on real-world validation feedback

**Market Risks:**

**Risk:** Tool doesn't actually help find extraction starting points (core value proposition fails)
- **Mitigation:** Early validation on real codebase (smallest solution in Week 1)
- **Validation:** Week 3 checkpoint - if graphs still "visual spaghetti," pivot filtering approach
- **Contingency:** Redesign framework filtering or visualization approach based on Week 3 results

**Risk:** Architects don't trust automated recommendations
- **Mitigation:** Transparent scoring methodology, show supporting metrics (coupling counts, complexity numbers)
- **Validation:** Week 12 validation - do top 10 candidates match manual assessment?
- **Contingency:** Position tool as "decision support" not "automated decisions" - emphasize data transparency

**Resource Risks (Solo Developer):**

**Risk:** 12 weeks proves unrealistic for solo side project
- **Mitigation:** Prioritize ruthlessly - if timeline slips, cut scope not quality
- **Contingency Plan:**
  - Minimum viable: Phase 1 only (filtered graphs) = still useful for visualization
  - Next priority: Phase 2 (cycle detection) = enables manual extraction planning
  - Last priority: Phase 3 (automated scoring) = nice-to-have enhancement

**Risk:** Getting stuck on hard technical problems alone
- **Mitigation:** Engage with Roslyn/QuikGraph communities early, document blockers immediately
- **Contingency:** Simplify implementation - e.g., simpler cycle detection algorithm if QuikGraph proves difficult
- **Time Boxing:** If blocked >2 days on any feature, document limitation and move forward

**Risk:** Loss of motivation or competing priorities
- **Mitigation:** Focus on solving real personal pain point (your actual 20-solution codebase)
- **Validation:** Use tool on real work weekly - if not using it, question value
- **Contingency:** Ship Phase 1 early (Week 4) and validate usefulness before continuing

**Go/No-Go Decision Points:**

- **Week 4:** If filtered graphs aren't readable, stop and redesign filtering approach
- **Week 8:** If cycle detection misses known dependencies, stop and fix algorithm
- **Week 12:** If top 10 candidates don't match intuition (<80% agreement), tune scoring before using for real decisions
