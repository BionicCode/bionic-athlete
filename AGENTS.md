# AGENTS.md

## Purpose
This file defines how Codex should work in this repository.

Follow user instructions first. If they conflict with this file, the user wins.

---

## Core engineering rules
- Favor correctness, readability, testability, and explicit boundaries over minimizing file or type count.
- Make the smallest change that fully solves the problem, unless the current design is structurally unsafe.
- Prefer fixing root causes over patching symptoms.
- Do not hide problems by weakening rules, disabling analyzers, changing `.editorconfig`, or lowering warning severities unless the user explicitly asks for that.
- When the task is architectural or likely to span multiple files, start with a short plan before editing code.

---

## Scope and routing
- If the user names entry points, files, types, methods, projects, or tests, treat those as the starting scope.
- If the user does not name entry points, discover the smallest relevant scope before making changes.
- Prefer repository-local evidence over assumptions from naming alone.
- Prefer the smallest relevant solution, project, directory, file set, and test scope over whole-repository work.

---

## Task modes

### 1) Review-only mode
Use this mode when the user asks for review, analysis, root-cause investigation, design feedback, or uses `<review>`.

In review-only mode:
- Do not modify files unless the user asks.
- Do not run builds, tests, or formatters unless the user asks.
- Prefer static reasoning and code inspection.
- Trace real call paths when code behavior matters; do not infer behavior from naming alone when tracing can verify it.

### 2) Implementation mode
Use this mode when the user asks for code changes, fixes, refactors, tests, cleanup, or feature work.

In implementation mode:
- Make the requested code changes.
- Run the relevant validation commands.
- Fix violations surfaced by those commands instead of ignoring them.
- If you cannot run a required command because of sandbox limits, missing SDKs, missing dependencies, missing restore, or permission constraints, say exactly which command could not run and why.

This distinction is intentional. Review-only tasks avoid unnecessary execution. Code-change tasks must verify and clean up their work.

---

## Required validation mindset for implementation mode
For implementation tasks, validation is part of the deliverable.

That means:
- a change is not done when the code looks correct in theory; it is done when relevant validation passes in practice,
- failing tests are a signal to iterate, not a signal to stop,
- and analyzer/style compliance is part of correctness for this repository, not optional cleanup.

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
- If the repo uses a different validation entry point, such as scripts, CI wrappers, `Makefile` targets, or PowerShell helpers, prefer that path and mention it in the report.
- Never finish an implementation task while knowingly leaving newly introduced analyzer/style violations unresolved unless the user explicitly allows it.
- Never finish an implementation task while knowingly leaving newly failing relevant tests unresolved unless the user explicitly allows it or you have hit a concrete blocker that you report.

---

## .editorconfig and analyzer compliance
Treat `.editorconfig`, analyzer configuration, and project analysis settings as part of the repository contract.

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

## Review behavior
If the user asks for a review:
- Organize the review by file when practical.
- Use 1-based line references when available.
- Prioritize correctness, behavior, API contract mismatches, performance on hot paths, test gaps, and maintainability.
- Distinguish clearly between confirmed issues, likely risks, and unverified suspicions.
- If you cannot fully verify a path, say where verification stopped.

If the user uses `<review>` and provides no further instruction, interpret it as:
- perform a deep review of the user-specified scope,
- trace the relevant call tree as far as repository context allows,
- and report actionable findings with evidence.

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

## Git message conventions
When creating commit messages or pull request text:

### Commit messages
- Use an imperative summary.
- Keep the summary specific to the actual change.
- Prefer a leading tag or scope that reflects the dominant kind of change, for example:
  - `fix:`
  - `refactor:`
  - `test:`
  - `docs:`
  - `perf:`
  - `style:`
  - `build:`
  - `ci:`
- If the repository already has a commit convention, follow that instead.

### Pull request descriptions
- Summarize what changed.
- Call out the scope and any important boundaries.
- Mention tests added, updated, or intentionally not covered.
- Mention migration or compatibility impact when relevant.
- Call out assumptions that materially affected the implementation.

---

## Keep this file focused
Keep this file focused on execution rules, validation, and repo-wide conventions.

If you need detailed workflow instructions for recurring tasks such as architecture review, code review, release handling, or migration work, place those in separate repository files or Codex skills and reference them from here.
