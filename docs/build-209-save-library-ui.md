# Build 209: save library and compact dynasty screens

The launch screen is now dedicated to starting a new dynasty and loading saved careers. Active-career controls appear only after a dynasty is loaded or created. Each dynasty has one library entry, a Load action for its latest save, and a Versions action for loading or deleting individual snapshots.

New dynasty setup has its own screen with league, school and name fields. Returning to Saves from an active career saves the current state first; cancelling new-dynasty setup returns directly to the library. The active career has Overview, Recruit, Coach, Roster and Manage navigation. Manage holds links to Schedule, Teams, Rankings and Program, plus save/recruiting preferences.

The shared presentation uses smaller headings, consistent body text, quieter slate surfaces, compact intrinsic-width actions and a simple tab indicator. Row actions no longer stretch vertically to match long player descriptions. Home no longer contains Advance Week, coaching shortcuts, preferences or career navigation. This replaces the initial Build 207 layout and remains an evolving design.

## Validation

- 200 focused Release regression tests passed; packaged-resource checks passed.
- GitHub emulator coverage now asserts the save-library launch, new-dynasty form and cancel flow, and captures their screenshots and UI trees.
- Interactive portrait/landscape and save/load verification is being completed before delivery.

No save-schema or simulation behavior changes are included.
