# copilot-instructions.md

Follow the repository-wide engineering and validation standards defined in the root [AGENTS.md](../AGENTS.md) and any more specific AGENTS.md files that apply to the current path. The current file is based on that AGENTS.md at repository root and may get outdated. Any AGENTS.md along the working path is the source of truth.

<!--
Shared baseline instructions for repositories using coding agents.
Everything above "Repository Specifics" is intended to remain stable across repositories.
Repository owners may customize only the final "Repository Specifics" section.
Recommended CI approach: protect all content above the repository-specific marker.
-->

## Scope and Precedence
- This file defines repository-wide agent guidance.
- User instructions override this file.
- More specific `AGENTS.md` or `AGENTS.override.md` files in deeper directories may refine or override repository-level guidance.
- Keep this file concise, practical, and repository-agnostic. Repository-specific conventions belong only in the `Repository Specifics` section.
- If the user prompt is exactly `<review>`, treat it as a placeholder that expands to the task defined in the `Default Review Mission` section below.

## Core Engineering Standards
- Favor correctness, readability, testability, and explicit boundaries over minimizing file count or type count.
- Make the smallest change that fully solves the problem, unless the current design is structurally unsafe.
- Prefer fixing root causes over patching symptoms.
- Prefer cohesive designs with clear responsibilities; avoid mixing unrelated concerns in the same type or method when a cleaner separation is practical.
- Apply core maintainability principles such as single responsibility, low coupling, high cohesion, and explicit contracts when shaping or refactoring code.
- Preserve clear separation between domain logic, infrastructure concerns, and external dependencies.
- Do not introduce avoidable coupling between stable application contracts and external SDK, framework, transport, or persistence types unless the task explicitly requires it.
- Prefer clear, intention-revealing symbol names over abbreviations or overly terse names.
- Avoid cryptic abbreviations such as `cnt`, `tmp`, `obj`, or single-letter names like `f` when a more descriptive name materially improves readability.
- Use meaningful names for lambda parameters, locals, fields, and helper methods, especially when short names would force the reader to infer the enumerated item or value type from surrounding code.
- Do not hide problems by weakening rules, disabling analyzers, changing style configuration, or lowering warning severities unless the user explicitly asks for that.
- Follow existing repository conventions unless they conflict with the user prompt or this file.
- Treat documentation as part of engineering quality, not optional polish.

## Scope Discovery and Routing
- If the user names entry points, files, types, methods, projects, tests, or directories, treat those as the starting scope.
- If the user does not name scope, discover the smallest relevant scope before making changes.
- Prefer repository-local evidence over assumptions from naming alone.
- Prefer the smallest relevant solution, project, directory, file set, and test scope over whole-repository work.

## Task Modes

### Review-only mode
Use this mode when the user asks for review, analysis, root-cause investigation, design feedback, or uses `<review>`.

In review-only mode:
- Do not modify files unless the user asks.
- Do not run builds, tests, or formatters unless the user asks.
- Prefer static reasoning and code inspection.
- Trace real call paths when code behavior matters; do not infer behavior from naming alone when tracing can verify it.

### Implementation mode
Use this mode when the user asks for code changes, fixes, refactors, tests, cleanup, or feature work.

In implementation mode:
- Make the requested code changes.
- Run the smallest relevant validation you can reasonably run unless the user explicitly forbids command execution.
- Fix violations surfaced by validation instead of ignoring them.
- Update relevant documentation in the same change when public APIs, behavior, invariants, caveats, or usage patterns changed.
- If you cannot run a relevant command because of sandbox limits, missing SDKs, missing dependencies, missing restore, missing credentials, or permission constraints, state exactly which command could not run and why.

## Planning and Execution
- For architectural, cross-cutting, ambiguous, or multi-file work, start with a short plan before making changes.
- Before editing, inspect the relevant files, call paths, and neighboring abstractions so the change fits the surrounding design.
- Identify assumptions, risks, and boundaries when repository context is missing or ambiguous.
- Use a user-supplied plan unless it is clearly unsafe, inconsistent with repository constraints, or incomplete for the requested scope.

## File System and Project Structure
- Respect the existing repository layout before introducing new folders.
- Keep production code in the repository's established source locations.
- Place Markdown documentation files under a top-level `docs/` directory in the repository root.
- Place automated test projects under a top-level `test/` directory in the repository root.
- If `test/` does not exist and tests are needed, create it at the repository root.
- Name a generated unit test project `<SolutionName>.Tests`.
- If the repository contains multiple solution files, use the solution that owns the code being changed. If that ownership is still ambiguous, state the assumption you used.
- Keep test fixtures, sample inputs, and test-only helpers with or below the owning test project unless the repository already uses a shared test-assets location.

## Validation Expectations
For implementation tasks, validation is part of the deliverable.

That means:
- a change is not done when the code only looks correct in theory; it is done when relevant validation passes in practice,
- failing tests are a signal to iterate, not a signal to stop,
- analyzer and style compliance are part of repository quality, not optional cleanup,
- and required documentation updates are part of done when behavior or public surface changed.

## Testing Standards
- Add or update tests for bug fixes, behavior changes, and public API changes when feasible.
- Prefer focused unit tests for logic and invariants; use integration or end-to-end tests when behavior crosses boundaries that unit tests cannot validate.
- Do not rewrite tests merely to fit a broken implementation without explicitly calling that out.
- When a bug is fixed, prefer adding a regression test when practical.
- If a failing test is unrelated to the requested change, identify the evidence clearly and continue validating the remaining relevant scope where possible.

## Analyzer and Style Compliance
- Treat repository style configuration, analyzer settings, and project analysis settings as part of the repository contract.
- Prefer fixing the code over suppressing diagnostics.
- Use the repository's normal validation path for style and analyzers when one exists.
- If a reported violation appears to be a false positive, document the reasoning and use the narrowest justified suppression only when allowed by repository policy or explicit user instruction.
- Do not claim repository compliance based only on a build if the repository enforces style or analyzer rules through separate tooling.

## Documentation and Comments
- Add meaningful code comments where names alone are not enough to explain intent, invariants, trade-offs, caveats, failure modes, unsupported scenarios, or non-obvious behavior.
- Prefer improving structure and naming before adding explanatory comments that compensate for avoidably unclear code.
- Do not add comments that merely restate the code.
- Use XML or language-native API documentation comments for public or externally consumed APIs when the repository already uses them, or when you introduce or significantly change public surface area.
- Use proper XML documentation tags and language-aware markup such as `<see cref="..."/>` and `<see langword="null"/>` where appropriate.
- Use correct documentation syntax and language-aware markup where supported.
- If a change introduces non-obvious API usage, data structures, workflow expectations, or extension points, add or update a small Markdown document in the repository's preferred documentation location as described in the [File System and Project Structure](#file-system-and-project-structure) section.
- Keep documentation aligned with implemented behavior; stale documentation is a defect.

Follow the detailed documentation rules in `DOCUMENTATION.md` when that file exists.

## Default Review Mission
Goal: perform a deep, evidence-based code review focused on correctness, behavioral risk, contract mismatches, and maintainability.

If the user does not provide review scope:
- Start from the changed files, the requested entry points, or the most relevant public API surface you can identify.
- Trace the reachable call tree as far as repository context reasonably allows.
- Prioritize actual behavior over naming assumptions.

### Review Priorities
Prioritize findings in this order:
1. Correctness
2. Behavioral risk and hidden edge cases
3. API or contract mismatches
4. Data consistency / state consistency / cache consistency
5. Performance on meaningful paths
6. Maintainability and design clarity
7. Tests and documentation coverage

### Required Review Method
- Review by tracing actual call paths, not by isolated file scanning only.
- Start from the selected entry point(s) and follow calls downward until:
  - the full relevant path is understood, or
  - you hit a boundary caused by missing files, generated code, dynamic dispatch you cannot resolve, external dependencies, or insufficient context.
- Prefer evidence-based findings over speculative concerns.
- Distinguish clearly between confirmed defects, likely risks, and unverified suspicions.

### Review Output Format
Organize the review by file.

For each file:
- Use a file header with the filename.
- Use 1-based line references in the format `[L123]`.
- Tag each finding with one primary category:
  - `[ERROR]`
  - `[BUG]`
  - `[SECURITY]`
  - `[PERF]`
  - `[DESIGN]`
  - `[API]`
  - `[DOCS]`
  - `[TEST]`
  - `[STYLE]`
  - `[RISK]`

When useful, mention secondary impacts in the explanation, but keep one primary tag per finding.

### Required Review Sections
Include these sections in this order:
1. `Scope / Entry Points`
2. `Call-tree`
3. `Findings`
4. `Coverage / Call-tree traversal depth`

### Stop / Uncertainty Rules
- If you cannot fully verify a path, stop and explicitly say where verification stopped.
- Do not present an assumption as a confirmed defect.
- State why verification stopped: missing file, generated code, unclear runtime behavior, unresolved dynamic dispatch, external dependency, insufficient context, or command execution not requested.

## Reporting Requirements for Implementation Tasks
When you changed code, report:
- what you changed,
- what validation you ran,
- whether build passed,
- whether tests passed,
- whether style or analyzer verification passed,
- whether documentation was updated and at what level,
- and any remaining warnings, errors, assumptions, or blockers.

If execution was blocked, report the exact blocker instead of pretending verification happened.

## Change and Review Style
- Be concise but not shallow.
- Focus on actionable findings and robust fixes.
- Call out invariant violations, broken assumptions, migration risks, and notable trade-offs explicitly.
- Prefer clear explanations over rhetorical language.

## Commit and Pull Request Guidance
- Follow repository-specific commit conventions if defined.
- If no repository convention exists, use clear, scoped commit messages that describe what changed.
- In pull request summaries, explain:
  - what changed,
  - why it changed,
  - how it was validated,
  - any remaining assumptions, limitations, or follow-up work.
- Do not claim tests were run if they were not.

## Keep This File Focused
- Keep this file focused on execution rules, validation expectations, review behavior, and repository-wide conventions.
- Put detailed workflow instructions for recurring specialized tasks in separate repository files, nested `AGENTS.md` files, or skills, and reference them from this file when needed.
- Keep repository-specific commands, framework choices, and detailed local processes in the `Repository Specifics` section or a more specific instructions file.

<!-- BEGIN REPOSITORY SPECIFICS: repository owners may edit only this section -->
# Repository Specifics

Fill in or edit this section per repository. Everything above this section is intended to remain stable across repositories.

## Solution and Structure
- Primary solution name: `<SolutionName>`
- Source root(s): `src/`
- Test root: `test/`
- Unit test project name: `<SolutionName>.Tests`
- Documentation location: `docs/`

## Build and Validation
- Preferred restore command: `<fill me>`
- Preferred build command: `<fill me>`
- Preferred unit test command: `<fill me>`
- Preferred integration test command: `<optional>`
- Preferred style or analyzer validation command: `<optional>`

## Repository Conventions
- Preferred frameworks, languages, or libraries: `<fill me>`
- Naming conventions beyond language defaults: `<fill me>`
- Commit / PR conventions: `<fill me>`
- Documentation-specific expectations: `See DOCUMENTATION.md if present.`
- Any allowed exceptions to the shared rules above: `<fill me>`

## Optional Specialized Instruction Files
- Additional instruction files or skills used by this repository: `<optional>`
- Paths where nested instructions intentionally override this file: `<optional>`

<!-- END REPOSITORY SPECIFICS -->
