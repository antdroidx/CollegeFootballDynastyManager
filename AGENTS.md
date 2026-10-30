# Coding-agent workflow

This repository contains two Android games: College Dynasty Manager and Pro Football Franchise Manager. Future coding updates should follow the workflow below.

## 1. Inspect before changing anything
1. Confirm the repository root, current branch, working-tree status, remotes, and recent commits.
2. Read the repository's current status/testing documents and locate their current names rather than assuming they have not changed.
3. Inspect relevant app/module code, build configuration, tests, and existing GitHub Actions workflows.
4. Identify which game and modules the request affects.
5. Preserve unrelated user changes. Do not reset, clean, or overwrite uncommitted work.
6. Do not assume a previous branch, build number, test result, APK path, or GitHub release is still current.

## 2. Implement requested changes
1. Follow the project's architecture and naming conventions.
2. Keep College and Pro behavior separate unless the request explicitly calls for shared behavior.
3. Preserve each app's package/application identity and existing save compatibility.
4. Add or update focused tests for changed behavior. Avoid redundant or unrelated long tests unless a focused check fails or the change requires broader coverage.
5. Update the project's status/testing documents to record actual implementation and checks.

## 3. Validate before claiming success
1. Run the narrowest relevant tests first.
2. Run required lint, unit, integration, or build checks specified by repository docs and workflows.
3. Build only the affected app(s), unless shared code or project policy requires both.
4. If an APK is required, build the correct variant, verify the output exists, and record its exact path, variant, and build identity.
5. If a check fails, diagnose and fix it, then rerun the affected check.
6. Report precisely which commands/checks ran and their actual results. Never claim a check passed if it was not run.
7. Do not use old APKs as evidence for newly changed code.

## 4. Update Git branches and GitHub
1. Work on the intended feature branch. If a branch is required and none exists, create one using the repository's naming convention.
2. Review the diff for accidental files, generated artifacts, secrets, unrelated changes, or large binaries.
3. Commit only the intended changes with a concise descriptive message.
4. Push the feature branch to the configured GitHub remote if network access and credentials are available.
5. Open or update a pull request only if requested or if repository workflow clearly requires it. Summarize change, tests, and build artifacts accurately.
6. Do not merge to main, delete branches, rename the repository, create a GitHub release, or publish a pre-release unless explicitly authorized.

If GitHub access is unavailable, leave local branch/commits reviewable and explain what remains.

## 5. GitHub Actions and APK artifacts
1. Inspect existing Actions workflows before editing or triggering them.
2. Reuse the repository's established workflow for validation and APK generation. Do not create duplicate workflows or redundant artifacts.
3. Ensure triggers and artifact names clearly distinguish College and Pro builds.
4. Do not change unrelated workflows or remove existing Actions/artifacts unless explicitly asked.
5. If a workflow run is started, wait for it to complete and inspect its actual status and logs.
6. Download or link the exact APK artifact from the successful run when tools permit, including workflow/run identifiers.
7. Distinguish a CI artifact from a GitHub release. Uploading an artifact does not authorize publishing a release.
8. Never say an APK was built or attached unless the exact artifact was verified.

## 6. Final report
Provide a concise report with:
- What changed and which game(s) it affects.
- Branch name and commit hash.
- Whether the branch was pushed and whether a PR was created.
- Tests and builds actually run, with pass/fail results.
- Exact APK artifact link or local path, if produced.
- Any remaining failures or steps requiring authorization.

Do not publish a release or merge changes without explicit instruction.