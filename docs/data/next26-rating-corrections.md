# NEXT26 rating corrections

Source: user-supplied ratingdrops.xlsx, Sheet1, received 2026-10-22. The literal table is retained as tests/DynastyManager.Core.Tests/TestData/ratingdrops.csv.

Next26RatingAdjustments decodes raw 0-59 attributes by adding 40, then applies the spreadsheet's signed per-position changes. Blank cells and explicit zero mean unchanged. OL covers PPOS 5-9, DE 10-11, LB 13-15. DT is 12; K and P remain separate (19/20). Unsupported non-core rows (Importance, Potential, Stamina) contain no changes. Values are bounded to the core 1-99 range after adjustment; raw out-of-range inputs are rejected.

The bundled NEXT26 starting roster uses these corrections automatically for new dynasties. Overall, talent and estimated recruiting stars are calculated afterward. Saved dynasties and generic PLAY-table CSV imports are not adjusted. No changes to player generation, development or recruiting simulation.

Validation: all 11,560 bundled players and 17 attributes compared with the independent workbook CSV fixture. Every PPOS code checked at raw 0 and 59. Save serialization preserves corrected values without another adjustment. All 594 regular regression tests pass.

## Updated overall calibration

The corrected user-supplied NCAA06_POCI.csv (2026-10-22) replaces PLDH/PLDL calibration for 15 of 21 position rows in Ncaa06RatingService. All 17 weights per position are unchanged. The original CSV is retained in tests/DynastyManager.Core.Tests/TestData/NCAA06_POCI.csv, with independent formula comparisons across all positions. This shared formula affects recalculated overall ratings for imported and generated players; it does not alter raw player attributes or apply the NEXT26 rating drops to other roster formats.
