# Android CI

Pull requests targeting `main` produce the normal fast test APK. Only `main`
pushes build automatically outside PRs, avoiding duplicate feature push/PR
builds. Branches without a PR can use a manual workflow run.

The `build-test-android` job checks packaged resources, restores packages,
runs core tests, and uploads `dynasty-manager-android-debug` immediately after
building. The filename and signed-APK selection stay unchanged. The APK uses
the existing Debug package ID, embedded assemblies, stable development signing
key, supported architectures, and `100000 + github.run_number` version code.
Keep the workflow filename to preserve its run-number sequence. Re-running the
same run keeps the same version code.

The separate `android-launch` job downloads that exact artifact (no rebuild)
and runs the existing API 36 launch check on main pushes. For other branches,
manually run the workflow with `run_emulator` enabled. The APK remains available
even if emulator validation fails; inspect both jobs before considering a main
build fully validated. Runtime logs retain the `android-launch-log` name.

NuGet packages are cached by OS, architecture, resolved SDK and dependency
inputs, with an SDK-scoped fallback. Restore always runs to resolve changes.
Workload installation remains pinned to 10.0.301.1; machine-wide workload
directories and build outputs are intentionally not cached because partial or
stale toolchain state can break clean builds. APK upload skips compression
because APKs are already compressed.

Failed run 36535091244 referenced the absent `appicon_title.png`. The branch
already corrected this to the tracked `appicon_cfdm.png` before these workflow
changes. `check-packaged-resources.py` now catches missing explicit icon, splash,
and asset files before toolchain setup. CI runs this on Linux, where path case
must also match.

Local validation: run `python3 build/check-packaged-resources.py`, `actionlint`,
and `dotnet test tests/DynastyManager.Core.Tests/DynastyManager.Core.Tests.csproj -c Release`.
