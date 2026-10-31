# Pro Football local validation runbook

For an agent continuing the scouting update. Run commands from the repository root. This records the repository's established validation approach and the current local obstacle; it does not assert that the pending scouting tests pass.

## Scope and starting point

1. Read `git status --short` before editing. The current scouting work is uncommitted; preserve it and do not discard unrelated changes.
2. Read `docs/current-work.md`, `docs/testing-scope.md`, `global.json`, `tests/ProFootball.Tests/ProFootball.Tests.csproj`, and `.github/workflows/pro-android.yml`. Inspect the Pro scouting, draft, offseason, save, and UI files before modifying them. Do not change College behavior.
3. The repository pins .NET SDK `10.0.301` with latest-patch roll forward. The Pro test project targets `net10.0` and uses `Microsoft.NET.Test.Sdk` 17.14.1 with xUnit. The Pro CI workflow runs on Ubuntu with .NET 10 and invokes `dotnet test` on the Pro test project. Previous accepted checks in the status documents were often CI checks; do not present them as evidence for this working tree.

## Normal focused checks

Use a shell whose working directory is this repository. PowerShell examples:

```powershell
dotnet --version
dotnet build src/ProFootball.Core/ProFootball.Core.csproj -c Release
dotnet build tests/ProFootball.Tests/ProFootball.Tests.csproj -c Release
dotnet test tests/ProFootball.Tests/ProFootball.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~ScoutingDepthTests|FullyQualifiedName~FranchiseDepthAnalyticsTests'
dotnet build src/ProFootball.App/ProFootball.App.csproj -c Debug -f net10.0-android
```

Adjust the filter to include any other *changed* Pro scouting/draft/save test classes after inspecting them. The CI workflow documents the standard Pro project commands and an Android build with `-p:AndroidPackageFormats=apk`; local compilation need not publish or release an APK. Do not run the broad Pro, College, or long simulation suites for this scoped change unless a concrete failure requires it.

Build and test are distinct evidence. Record the exact command, exit code, test count, and failures. A successful compile does not establish passing tests. Update `docs/current-work.md` and `docs/testing-scope.md` only with checks actually completed.

## Current Windows runner obstacle

The previous agent reported that the projects compiled but `dotnet test` timed out while `vstest.console` waited for `testhost` to connect. That test result is **unverified**. The local shell in this Codex session also emits `Attempting to perform the InitializeDefaultDrives operation on the 'FileSystem' provider failed`; `dotnet --info` reached SDK information but exited after a Service Control Manager access error. These symptoms can be environmental and should be captured separately from compiler or assertion failures.

Do not run `testhost.exe` directly as a test: the test project's output contains `ProFootball.Tests.runtimeconfig.json`, whereas a direct `testhost.exe` launch looks for `testhost.runtimeconfig.json`. That missing file alone does not prove the test runner is broken.

Suggested bounded diagnosis, in order:

1. Check `dotnet --version` and build the Pro test project. If compilation fails, fix the reported code errors first. An earlier `build/tests-msbuild2.log` recorded missing `depth` arguments in `FranchiseDepthAnalyticsTests.cs`; verify against the current source rather than treating that old log as the latest outcome.
2. Run one focused `dotnet test ... --no-build --filter ... --logger 'console;verbosity=normal'` command. Capture its complete output and exit code. Avoid overlapping test/build processes using the same output folder.
3. If `testhost` still times out, try the same command from `cmd.exe` or an unrestricted local terminal, and compare the outcome. In the scouting batch, the VSTest diagnostic confirmed that `testhost.exe` launched but no localhost connection arrived; rerunning the focused `dotnet test` command with external execution allowed the local IPC and completed normally. If the same command continues to hang in a sandboxed shell, use that external-runner path only when authorized, and keep the timeout diagnostic file out of commits.
4. If local test execution remains blocked, use the repository's Pro CI test job as the established alternative when authorized. Do not push, publish, or trigger a release solely to work around this failure. Report local tests as blocked until a real runner returns results.

Keep the temporary `build/` logs and binlogs out of commits unless explicitly needed as evidence. They are currently untracked files in the worktree.
