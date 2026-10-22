# Current work: NEXT26 rating corrections

User requested per-position global adjustments from ratingdrops.xlsx. Implemented import-only corrections after raw decoding and before OVR/stars/talent calculation. Existing saves and generic imports untouched. All 594 regular tests passed, including full bundled roster comparison to independent spreadsheet fixture and 21-position boundary coverage. See docs/data/next26-rating-corrections.md. GitHub APK build pending. Previous completed APK: Build287. No automation active; no unrelated work requested.

Source commit: 5dde4bd. Build288 running: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/39828639274 . Packaged resource and Python smoke-helper checks passed; Android APK packaging pending. No emulator or extended career run requested for this import-only change.
