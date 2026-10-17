# Detailed football skills and notable news

Player cards add passing power/accuracy, catching, break tackle, tackling, run/pass blocking, ball carrying, acceleration, stamina, leadership, discipline, jumping, man/zone defensive awareness and kick power/accuracy. Speed, strength, agility, awareness, technique and durability remain. Injury proneness is explicitly the inverse of durability (higher means more risk), not a second independent injury roll.

Skill ratings persist with player identity, including transfers. Older saves initialize missing skills deterministically from their existing ratings and position; existing initialized broad ratings and skills remain intact. Schema 22 stores the new skill object and nullable pregame strength snapshots. Skills develop/regress with the corresponding physical or technical rating; leadership and discipline remain stable. The legacy overall calculation is retained to preserve existing roster/potential balance.

The drive simulator's passing/rushing team profiles combine 70% existing overall, 25% relevant positional skills and 5% acceleration/stamina/leadership/discipline. Pass blocking affects passing, run blocking affects rushing; QB accuracy/power and receiver catching affect passing; carrying/break tackle affect rushing; coverage and tackling feed their defensive profiles. This is aggregate drive-level integration. Individual catch, fumble, fatigue, penalty or leadership events are not yet independently modeled.

News recognizes upsets (at least an eight-point recorded pregame strength disadvantage), shutouts, 28-point blowouts, five-game winning streaks, broken winning streaks of four or more, conference titles, playoff advancement/national titles and bowl wins. New simulated games retain the actual pregame strength profiles so later roster changes cannot rewrite the upset classification. Old games without strength snapshots can qualify from earlier same-season results only: at least three prior games per team, winner at or below .500, opponent at or above .750. No historical rank is invented. Ties break win streaks; future results are excluded. Notable stories precede routine stories within the same week.

Validation: all 224 Release tests passed, including 218 focused regressions and all six multi-year career health simulations, Android Debug APK packaging passed, packaged resources passed, and the installed APK loaded an older save with all detailed skills visible. Portrait/landscape upset-article navigation, pregame explanation and full player-card scrolling passed with an empty crash buffer. GitHub Build 217 is still running its independent validation before APK upload. The QA screenshots use a labeled synthetic fixture, never shipped game data.

## Screenshots

- [Player attributes](ui/build-217-player-attributes-top.png)
- [Additional football skills](ui/build-217-player-attributes-lower.png)
- [Upset story](ui/build-217-upset-story.png)
