# Current work: corrected NCAA06 POCI

User supplied corrected NCAA06_POCI.csv on 2026-10-22 (attachment 90f42be0). Headers and all 21 position identities now align. Applied its PLDH/PLDL values to shared Ncaa06RatingService. All 17 attribute weights unchanged. Previous NEXT26 import-only rating drops retained. The shared calculation applies when player overall is recalculated (including initialized profiles); raw attributes are not changed by POCI. Existing historical snapshots are not rewritten.

595 regular tests passed, including 420 varied-attribute comparisons against literal supplied CSV fixture. Targeted 12-team career health checks running. Prior Build288 succeeded. New APK pending. No automation active or unrelated work requested.
`nAll three targeted 12-team career health checks passed (24 seconds).
