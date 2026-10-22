# Current requested update (after Build 280)

Status: in progress. Work on game-day-2-test; preserve dynasty/recruiting. Do not edit sources/.

Requested: unobtrusive optional carrier labels; three compact portrait quick calls; large replay announcement; kickoff replay only beyond midfield; pass-breakup/defended stat visibility; saved Player of the Game; weather with shared engine effects; bounded in-game and seasonal morale with smaller training effects. Add regression/UI checks and publish test APK via GitHub Actions. User requested automatic continuation after usage replenishes. Follow-up is scoped to unfinished items, not new unsolicited work.

Baseline: Build 280, commit 0dc5c6a, 552 regular core tests + nine helpers + full cloud Android smoke passed. Last local emulator stopped responding during final calendar capture; it was shut down. Avoid overlapping UI capture processes. Existing pass-defended data is recorded, but CategoryEligible excludes defenders whose only production is PD. Existing kickoff replay uses ReturnYards >= 30; needs actual end spot > 50. No weather or morale fields found in current models.

Implementation in working tree: new GameConditions, GameWeatherService, GameMoraleService, PlayerOfGameService; weather/morale integrated into shared engine/profile builder/development; awards saved and archived in DynastyHistoryService; UI portrait quick-call overlay, subtle default-off surname labels, replay banner, PBU visibility and award/weather/morale displays. Rare explosive kickoff returns now use actual end spot, allow touchdowns/conversions, and auto replay only beyond midfield. Not validated yet: next compile/tests, fix failures, update Android smoke for portrait More menu and three cards, publish APK via Actions. Automation id resume-unfinished-cfdm-update is ACTIVE hourly; pause once done. Never redeem reset credit automatically.

Validation checkpoint: 568 regular core tests passed (16 new cases), Android Compile succeeded. Updated portrait smoke to open More and require three call cards. Pending GitHub APK packaging and cloud emulator checks. No local emulator is running.
