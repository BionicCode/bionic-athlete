# AGENTS.md

## Purpose
This file defines how Codex should work in this repository.

Follow user instructions first. If they conflict with this file, the user wins.

---

## Core engineering rules
- Favor correctness, readability, testability, and explicit domain boundaries over minimizing type count.
- Do not let external SDK/runtime types leak into domain models unless explicitly requested.
- Prefer replacing structurally wrong draft designs over patching them in place.
- When a task is architectural, propose a plan before writing code.
- Do not hide violations by weakening rules, disabling analyzers, or editing `.editorconfig`, project analysis settings, or warning severities unless the user explicitly asks for that.
- Make the smallest change that fully solves the problem, unless the current design is structurally unsafe.

---

## Task modes

### 1) Review-only mode
Use this mode when the user asks for review, analysis, root-cause investigation, or uses `<review>`.

In review-only mode:
- Do not modify files unless the user asks.
- Do not run builds, tests, or formatters unless the user asks.
- Prefer static reasoning and code inspection.
- Trace real call paths; do not infer behavior from naming alone when tracing can verify it.

### 2) Implementation mode
Use this mode when the user asks for code changes, fixes, refactors, or feature work.

In implementation mode:
- Make the requested code changes.
- Then run the relevant validation commands.
- Fix violations surfaced by those commands instead of ignoring them.
- If you cannot run a required command because of sandbox, missing SDK, missing restore, or approval limits, say exactly which command could not run and why.

This distinction is intentional. Review-only tasks avoid unnecessary execution. Code-change tasks must verify and clean up their work.


### Required validation mindset for implementation mode
For implementation tasks, Codex must behave as if validation is part of the deliverable.

That means:
- a change is not done when the edit compiles in theory; it is done when the relevant validation passes in practice,
- failing tests are a signal to iterate, not a signal to stop and hand work back immediately,
- and analyzer/style compliance is part of correctness for this repository, not cosmetic cleanup.

---

## Required implementation workflow
After making code changes, perform this workflow unless the user explicitly forbids command execution:

1. Identify the smallest relevant solution, project, or test scope.
2. Restore packages if needed.
3. Build.
4. Run the smallest relevant tests.
5. Run formatting and analyzer fixes so the result matches repository style settings.
6. Rebuild and rerun the relevant tests if formatting or fixes changed files.
7. If build or tests fail, diagnose the failure, fix the code or tests, and repeat the relevant build/test/fix cycle.
8. Continue iterating until the relevant tests pass and the repository is clean, or until you hit a concrete blocker you can explain.
9. Run a final verification pass that confirms no format/analyzer drift remains.

### Command policy
Prefer repository-local or task-local scope over whole-repo commands when possible.

Typical .NET command sequence:

```bash
# If restore is required
 dotnet restore <solution-or-project>

# Build
 dotnet build <solution-or-project> --no-restore

# Smallest relevant test scope
 dotnet test <test-project-or-solution> --no-build

# Apply style/analyzer fixes from .editorconfig and analyzers
 dotnet format <solution-or-project> --no-restore

# Final verification: fail if any formatting/analyzer drift remains
 dotnet format <solution-or-project> --no-restore --verify-no-changes
```

Rules:
- Do not use `dotnet format --verify-no-changes` as the only formatting step for implementation tasks. That only checks; it does not fix.
- If `--no-restore` fails because restore has not yet happened, run restore once and continue.
- If tests fail, do not stop at the first red run. Investigate, fix, and rerun the smallest relevant build/test scope until the targeted tests pass or a concrete blocker prevents further progress.
- Treat red tests caused by your changes as part of the task, not as optional follow-up work.
- If a test is broken for reasons unrelated to your change, identify the evidence clearly and continue validating the remaining relevant scope where possible.
- If the repo uses a different validation entry point (for example scripts, CI wrappers, or custom build commands), prefer that path and mention it in the report.
- Never finish an implementation task while knowingly leaving newly introduced analyzer/style violations unresolved unless the user explicitly allows it.
- Never finish an implementation task while knowingly leaving newly failing relevant tests unresolved unless the user explicitly allows it or you have hit a concrete blocker that you report.

---

## .editorconfig and analyzer compliance
Treat `.editorconfig`, analyzer configuration, and project analysis settings as part of the contract of the repository.

For .NET repositories:
- Assume `.editorconfig` is authoritative for whitespace, code style, naming, and analyzer configuration.
- Respect warnings and errors from SDK analyzers and any repository-installed analyzers.
- Prefer fixing the code over suppressing the diagnostic.
- If a violation appears to be a false positive, document it and use the narrowest justified suppression only if the user allows repository rule exceptions.

Important:
- Command-line `dotnet build` does not automatically enforce all IDE code-style diagnostics unless the project enables build enforcement for code-style analysis.
- Therefore, for implementation tasks, do not rely on build alone to prove `.editorconfig` compliance. Run `dotnet format` as part of validation.
- If the repository expects IDE-style rules to fail on build, recommend project-level enforcement such as `EnforceCodeStyleInBuild=true` and appropriate rule severities when that is not already configured.

---

## Planning expectations
When the task is architectural, ambiguous, or likely to span multiple files:
- Start with a short plan.
- Identify assumptions, risks, and boundaries.
- Confirm which parts are in scope before broad refactors.

When the user has already supplied a plan, use it unless it is clearly unsafe or inconsistent with the repository.

---

## REVIEW MISSION (default)
If the user prompt is exactly `<review>`, expand it to the task defined here.

Goal: Perform a deep, call-tree-based code review focused on correctness of reflection identity and caching behavior.

Default entry points:
- `SymbolReflectionInfoCache`
- `SymbolReflectionInfoCacheKey`

Trace the full reachable call tree from those entry points as far as repository context allows.

If the user specifies different entry points, files, methods, or paths, follow the user prompt instead.

### Primary invariants (definition of done)
A correct implementation should satisfy all of the following:
1. Indexer parameters normalize to a stable identity across all retrieval paths.
2. Parameter cache keys are consistent and do not split or duplicate entries depending on acquisition path.
3. `TypeData` caching makes the hot path reflection-free after warmup.
4. Explicit interface accessors are handled correctly without brittle name-based heuristics.

### Review priorities
Prioritize findings in this order:
1. Correctness
2. Cache identity / consistency
3. Hidden duplication or split-cache risks
4. API contract mismatches
5. Performance on hot paths
6. Maintainability / design clarity

### Required review method
- Review by tracing actual call paths, not by isolated file scanning only.
- Start from the selected entry point(s) and follow calls downward until:
  - the full relevant path is understood, or
  - you hit a boundary caused by missing files, generated code, external dependencies, or insufficient context.
- Prefer evidence-based findings over speculative concerns.
- Do not infer behavior from naming alone when call tracing can verify it.

### Output format
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

### Required sections in the review
Include these sections in this order:
1. `Scope / Entry Points`
2. `Call-tree`
3. `Findings`
4. `Coverage / Call-tree traversal depth`

### Stop / uncertainty rules
- If you cannot fully verify a path, stop and explicitly say where verification stopped.
- Do not present an assumption as a confirmed defect.
- Distinguish clearly between:
  - confirmed issue,
  - likely risk,
  - unverified suspicion due to missing context.

### Review style
- Be concise but not shallow.
- Focus on actionable findings.
- Prefer robust fixes over minimal patches when the design is structurally unsafe.
- Call out invariant violations explicitly.

---

## Reporting requirements for implementation tasks
When you changed code, report:
- what you changed,
- which commands you ran,
- whether build passed,
- whether tests passed,
- how many validation iterations were needed if tests initially failed,
- whether formatting/analyzer verification passed,
- and any remaining warnings/errors with a reason.

If execution was blocked, report the exact blocker instead of pretending verification happened.

---

## Optional repository organization improvement
If this file grows too large:
- Keep this file focused on execution rules, validation, and repo-wide conventions.
- Move detailed review policy into a separate file such as `code_review.md` and reference it from here.

---

## Git message conventions
When creating commits or pull request text for this repository:

### Commit messages
- Prefer scopes that match this repository and solution structure, especially:
  - `data`
  - `decoder`
  - `fit`
  - `model`
  - `cache`
  - `tests`
- Mention `FitToCsvConverter.Data` or `FitToCsvConverter.Test` in the commit body when relevant.
- If the change is part of the FIT model/decoder redesign, make that explicit in the summary or body.
- Do not mention CSV export unless export code was actually changed.
- If draft model types were replaced or removed, state that clearly in the body.

### Pull request descriptions
- Call out which parts of the solution were changed, especially:
  - `FitToCsvConverter.Data`
  - `FitToCsvConverter.Test`
- If the work only covers Step A (domain model / decoding boundary / Garmin implementation), explicitly state that CSV export remains out of scope.
- Mention how Garmin SDK types are kept out of the domain model when relevant to the diff.
- Mention migration impact if placeholder or draft types were replaced, removed, or moved.
- Summarize tests added or updated in `FitToCsvConverter.Test`.
- Call out assumptions related to Garmin FIT SDK behavior when generated SDK/profile metadata had to be used as the practical source of truth.

