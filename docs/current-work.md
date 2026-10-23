# Management UI overhaul — Phases 1–2 in progress

Branch: `feature/management-ui-overhaul`, based on game-day-2-test source 1dd2b72 (Skip/Close fixes). Do not merge main. Sources/ read-only.

User scope: implement Phases 1–2 now using attached FM-style mockups. No simulation, ratings, generation, save schema, package identity or Game Day visual/playcalling changes. Later phases are not yet requested for implementation.

Implemented: .NET MAUI shared management shell, 600/840dp responsive navigation, collapsible compact navigation, System/Light/Dark preferences and live OS appearance updates; reusable responsive panels/profile tabs; readable dense sortable tables and Essential/All saved column sets; team-initial branding; responsive dashboard tasks/poll panels/news; roster profiles fullscreen compact / split pane tablet; profile tabs with existing attributes, morale, history, statistics and management actions; compact bounded recruiting table, priority scouting and board shortcuts; recruit overview/recruitment profile pane. Existing deterministic cartoon portraits reused.

Validation: Android compile passed (six existing warnings); 634 core tests and 9 Python smoke helper tests passed; packaged-resource check passed. Extended Android smoke tests need full GitHub Actions validation, screenshot inspection and possible repairs. Need create dedicated PR, no merge. Then build APK link and final evidence/report.

Previous Build 299: run 39985925999 compiled/tests passed, emulator failed waiting for Skip after Watch because UIAutomator waits for idle through continuous animation. Test now pauses Watch using last XML-observed button bounds before checking confirmation. Must still validate cancel/confirm Skip, final stats, Close and saved result without crash. No game crash trace in Build299 log. Do not describe it as fully validated yet.

Hourly thread heartbeat `resume-management-ui-phases-1-2` is active per user request to resume after usage resets, no reset-credit redemption. Stop/pause this scoped automation once delivery is complete.

Build 300 run 39988287128 from a6d3805 compiled successfully; emulator testing in progress. Draft PR20 targets game-day-2-test. Local review refinements are not yet in that APK: numeric prospect sort keys/public stars/ranks, imported avatar consistency, clean player management tab, safe team-profile navigation, System-change guard and browsing-state retention, profile back button and available-pane-width split behavior. Need commit/push these with any device repairs, then final APK validation.
