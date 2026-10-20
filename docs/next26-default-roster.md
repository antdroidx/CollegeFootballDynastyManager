# Bundled NEXT26 starting roster

The supplied `NEXT26-CFB26-BASE-AUG26-2025 - Copy.csv` is packaged unchanged as `Resources/Raw/next26-aug26-2025.csv`. SHA256: `24103bdbf709fa953bf66c03bd5755c91ee657f5f82e1dd00d9064f741db5940`.

The **NCAA 06 NEXT26 · Aug 26, 2025** roster is automatically selected for new dynasties unless a league setup already has an explicit roster preference. Users can select Legacy/generated or an imported pack instead, and set either as their preference. The bundled pack is read once at startup on a worker thread; it is not duplicated into the user library JSON. Existing dynasty saves retain their players.

The file contains 11,560 players, exactly 85 for each of 136 schools, including the source's 1,176 FAKE-marked filler players. All remain in the starting roster. Explicit school aliases reconcile the export's names with the game (for example Texas A&M → Texas AM and Louisiana → LA Lafayette). The modern 138-school setup generates players for North Dakota State and Sacramento State. The legacy setup uses the pack's matching schools. League structure and dynasty start year remain independent of this dated roster.

## Conversion

- The 17 core attributes use raw **0–59** values, converted to displayed ratings by **adding 40 exactly once**. Invalid stored values fail validation. This is the expanded NEXT scale confirmed by the user and the DB Editor conversion code; the older 32-entry lookup in the reference workbook does not apply.
- `FirstName` / `LastName` / `College` supply decoded identity. This export leaves `PGID` blank; `CFB26 PGID` supplies its unique IDs. `PPOS` preserves detailed NCAA position codes, including punter code 20 within the game's K position group.
- `PYER` 0–4 becomes FR–GR. `PRSD` 0 is eligible, 1 current redshirt, 2 previously redshirted, as documented in `NCAA-DB-Conversion-Reference.xlsx`. Current and previous redshirts are distinct.
- POCI overall is recalculated from position and converted attributes. Source `POVR` is not substituted for the computed value. Leadership and Discipline default to 60. Imported potential uses the existing floor of max(60, calculated overall); the file does not provide potential.

## Estimated recruiting stars

No historical recruiting stars are provided. At import, estimate freshman readiness as current position-weighted OVR minus **3 per completed class year**, plus one additional year for a previously completed redshirt. Current redshirts do not receive that extra completed year. Clamp readiness to 40–99, then assign 5 stars at 84+, 4 at 76+, 3 at 68+, 2 at 60+, otherwise 1. These bands follow the existing generated HS readiness ranges and are an approximation, not a reconstruction of a recruiting service's records.

The one-time result is stored with `RecruitingStarsEstimated=true`. Profiles explicitly say **Estimated HS stars**; tables prefix the value with `~`. Season statistics and career archives preserve that provenance. Later development, transfers and class changes never recalculate the estimate. Actual recorded HS stars and walk-on status are not overwritten.

Regression coverage compares every core attribute for every packaged player against an independent CSV reader, validates counts/mappings/class/redshirt states, checks raw endpoints and invalid values, verifies the two generated-school fallbacks, and tests fixed estimates through save reload and career archiving.
