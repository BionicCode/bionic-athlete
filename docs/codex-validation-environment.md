# Codex Validation Environment

## Purpose

`Invoke-CodexValidation.ps1` exists to keep Codex build and test runs independent from stale terminal state.

The repository itself was not missing a required setup script. The failures came from the agent shell inheriting an unstable environment:

- `dotnet` first-run initialization tried to write outside the workspace.
- the current process exposed both `Path` and `PATH`, which caused Roslyn/MSBuild child-process crashes.
- sandboxed runs may not be allowed to read the roaming NuGet profile even though Visual Studio can.

The script avoids the first two problems by launching a clean child process with one canonical `Path`, a repo-local `DOTNET_CLI_HOME`, and repo-local temporary directories.

## Entry point

- [Invoke-CodexValidation.ps1](../scripts/Invoke-CodexValidation.ps1)

## Default usage

Run the current sanitized test validation without restore:

```powershell
pwsh -File .\scripts\Invoke-CodexValidation.ps1
```

Equivalent explicit form:

```powershell
pwsh -File .\scripts\Invoke-CodexValidation.ps1 -Action Validate
```

Run only the build step:

```powershell
pwsh -File .\scripts\Invoke-CodexValidation.ps1 -Action Build
```

Run only the test step against existing build output:

```powershell
pwsh -File .\scripts\Invoke-CodexValidation.ps1 -Action Test
```

## Profile modes

`-ProfileMode CurrentUser` is the default.

Use it when:

- Visual Studio or the local shell already restores normally.
- the machine needs the normal user NuGet and TLS context.

`-ProfileMode WorkspaceLocal` isolates `USERPROFILE`, `APPDATA`, and `LOCALAPPDATA` under `.codex-env/`.

Use it when:

- you want a more isolated Codex run,
- or the current shell profile variables are clearly polluted.

## What `Validate` currently means

`Validate` intentionally runs the test project in a clean child process with `--no-build --no-restore`.

That makes it reliable for Codex after the project has already been built by Visual Studio or another trusted local build path.

This is still useful because the stale-environment failures came from the agent shell, not from your normal Visual Studio setup.

## Important caveat about restore and rebuilds

`Restore` and `RestoreValidate` are supported, but restore may still fail in a heavily sandboxed session if the agent cannot access the normal user NuGet/TLS context.

That is a sandbox limitation, not a repository build-definition problem.

In practice:

- local Visual Studio builds can continue to use the normal user profile,
- Codex can reliably use `Validate` after packages are already restored and built,
- and if Codex needs a clean restore from scratch inside a restricted shell, that run may still require a less-restricted execution path.

## Why this is better than mutating the current shell

The failure that broke MSBuild came from the inherited process environment itself.

Changing variables inside the current terminal is not always enough because the stale environment may already contain conflicting keys or first-run state.

The wrapper is therefore intentionally process-based:

- create a clean environment dictionary,
- add one canonical `Path`,
- add repo-local temp and `.dotnet` paths,
- then execute `MSBuild.exe` or `dotnet.exe` in that clean child process.
