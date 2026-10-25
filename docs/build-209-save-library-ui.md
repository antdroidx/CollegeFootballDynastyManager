# Builds 209–211: save library and compact dynasty screens

The launch screen is now dedicated to starting a new dynasty and loading saved careers. Active-career controls appear only after a dynasty is loaded or created. Each dynasty has one library entry, a Load action for its latest save, and a Versions action for loading or deleting individual snapshots.

New dynasty setup has its own screen with league, school and name fields. Returning to Saves from an active career saves the current state first; cancelling new-dynasty setup returns directly to the library. The active career has Overview, Recruit, Coach, Roster and Manage navigation. Manage holds links to Schedule, Teams, Rankings and Program, plus save/recruiting preferences.

The shared presentation uses smaller headings, consistent body text, quieter slate surfaces, compact intrinsic-width actions and a simple tab indicator. Row actions no longer stretch vertically to match long player descriptions. Home no longer contains Advance Week, coaching shortcuts, preferences or career navigation. This replaces the initial Build 207 layout and remains an evolving design.

In short landscape windows, recruiting uses a scrollable control panel beside its results. This keeps prospects visible instead of letting metrics, tabs, search and filters consume the entire height. Prospect details scroll with the results and opening a prospect returns that pane to the top.

## Validation

- 200 focused Release regression tests passed locally and on GitHub; packaged-resource checks and signed APK packaging passed.
- Build 211 passed GitHub emulator acceptance checks for the save-library launch, new-dynasty form and cancel flow. Screenshots and UI trees are attached to the run.
- Interactive emulator checks cover empty/populated Home, new dynasty creation, cancellation, saving back to Home, loading the latest save and a manual version, Coaching Hub and portrait/landscape recruiting.
- The selected-detail regression was reproduced by scrolling the results before selecting a recruit, then verified fixed: the selected player and evaluation now open at the top of the results pane.
- Manual UI-only builds may skip the six extended synthetic-career tests while still running the 200 focused regressions. Full career coverage remains the default for other builds. Simulation and persistence code are unchanged in this UI revision.

No save-schema or simulation behavior changes are included.

## Actual emulator screenshots

[Home](ui/build-211-home.png) · [Dynasty overview](ui/build-211-overview.png) · [Landscape recruiting](ui/build-211-recruiting-landscape.png)

The downloaded Build 211 APK (2.0.211, Android version code 611) was also installed over the local QA build. Its Home/setup/cancel flow passed, the existing test career loaded successfully, and the Android crash buffer remained empty.
