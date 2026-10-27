# Scope Android verification to the change

Use the smallest acceptance flow that covers affected behavior. Reuse verified results for unchanged applications; do not rebuild College solely because Pro finance changed.

- **Pro finance/data/contracts/rollover:** dispatch `pro-android.yml` with `validation_scope=finance`. Run all fast Pro transaction/career/save/financial regressions and the three-season cap lifecycle scenario; package the APK; run `build/pro-finance-smoke.py` on it. This checks new-franchise budgets, all 32 persisted/reconciled allocations, signed adjustments, sourced JSN annual cash/playing term, compact contract details and expandable void accounting, themes and reinstall/save survival. It excludes the unchanged broad GameCast walkthrough and two longer calibration cases.
- **Pro-only live layout:** dispatch `validation_scope=live`: fast Pro regressions plus full Pro device walkthrough, including landscape fit/collapse/field expansion/reopening/More checks. Reuse the prior long financial simulations and College result when shared engine/presentation and College source are unchanged.
- **Shared simulation, GameCast, renderer or live controls:** use `validation_scope=full`; verify both applications when shared application behavior changes. Full remains the default for unclassified pushes/manual runs.
- **Pro management/UI changes outside finance:** use the full Pro acceptance until a relevant narrower flow exists.
- **College-only changes:** run the appropriate College workflow/flow and affected Core/College tests. Pro builds are needed only if shared code is affected.
- **Test-helper changes:** run `python -m unittest discover -s build -p test_android_ui_smoke.py` (13 cases), plus the relevant device flow. Keep system-navigation clipping and fixed-footer regressions.
- **Documentation only:** use `[skip ci]`; do not rebuild APKs.

Run longer financial/roster simulations when annual lifecycle, cap inflation, draft supply or CPU financial planning changes. They are not necessary on every display/data formatting edit. For this first measured-rollover implementation, all 108 Pro cases (including twenty estimated-contract seasons, five sourced-contract seasons and the new three-season imported-cap scenario) passed locally once. The normal finance CI subsequently runs 106 relevant cases rather than repeating those two longest scenarios.

Core/College still has a legacy combined 690-case fast suite. Do not relabel it exclusively Core or College without auditing individual cases. Report Pro tests separately and state the device scope used; a finance-only run is not a new full-GameCast acceptance claim.


For harness/workflow-only retries, supply `reuse_apk_run_id` and its `reuse_apk_artifact` alongside the validation scope. The device job fetches the original run's source SHA and rejects any difference under `src`, shared build properties, SDK/NuGet configuration or signing keys. It reruns helper tests and installs the original APK. The build job is skipped; this preserves the original build/version and avoids duplicate APK compilation/storage. Report the original APK link plus the later verification run. Do not use reuse to bypass application changes.


Rookie option acceptance extends the financial script with two explicitly synthetic, isolated emulator saves: exercise and decline. It verifies the themed decision desk, once-only UI, persisted year-four/year-five guarantees and unchanged negotiation-change lock. The original imported franchise and its 32-team ledger assertions remain separate and unmodified. Core tests cover observed snap history, original contract/trade identity, all four modeled tender tiers, future cap/GM rejection, staff decisions, post-draft deadline, void debt and save round trips. Run long Pro simulations once for this lifecycle change; reuse unchanged College/shared rendering and Pro GameCast walkthrough evidence.
