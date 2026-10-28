# Recorded-game efficiency

Both apps use Football.Engine FootballEfficiency and shared presentation. The caller supplies only the current regular season; no preseason/playoff mixing. Played games with both box scores are deduplicated by GameId. No change to football rules, RNG, roster or cap systems.

Value/play = (net scrimmage yards +20 × offensive TD −45 × turnovers)/plays. Existing engine sacks already appear in rushing attempts/yards, so the fallback denominator is pass attempts + rush attempts, not plus sacks again. New context totals count eligible scrimmage events directly, excluding kneels/spikes and special teams. Opponent expectations exclude this matchup and shrink other-game totals with 200 league-average plays. Offense is actual minus expected; defense is expected minus allowed, normalized by max(2, abs(league value/play)) and multiplied by100. Net sums offense and defense. This is a game-specific opponent-adjusted efficiency model, not PFF grades, official DVOA or a fitted EPA model.

Context totals are optional fields on GameTeamStats; save schema and simulation behavior stay unchanged. Success:40% of distance on first down,60% second,100% later; turnovers fail. Old saves show box-score metrics but missing context is unavailable, never inferred from highlights. Mixed old/new success rates explicitly show recorded context sample. Rankings are provisional early in a season. League chart averages are means of eligible team rates; player peer averages are opportunity weighted and limited to the same position.

Player efficiency uses pass AY/A=(yards+20TD−45INT)/attempts (20 minimum), rush yards/carry (10), receiving yards/catch and YAC/catch (5), and drops/(catches+drops) (5 catchable balls). No OL assignment/coverage grades or pressure claims are invented. These are rate comparisons, independent of player ratings.

References: WCAG2.2 relative sRGB contrast (https://www.w3.org/TR/WCAG22/#contrast-minimum); PFF grading describes every-player/every-play film assessment (https://www.pff.com/grades), which this saved telemetry cannot replicate.
