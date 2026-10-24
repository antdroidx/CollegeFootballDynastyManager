# Build 220: roster, recruit cards and headline variety

Player profiles group ratings into character, physical and skill traits while preserving the complete persisted ratings model. The roster supports All players, Starters, Second team, Depth, Redshirts, Injured and Walk-ons filters alongside position, search and sorting controls.

The roster includes a dedicated depth-chart organizer. A player can be dragged onto another player at the same position to take that depth slot; visible up/down buttons provide the same control for accessibility and devices where dragging is inconvenient. Starter rows are highlighted, changes take effect immediately, and the Coaching Hub links directly to the organizer. The core reorder service prevents redshirt or cross-position movement and normalizes each affected position group.

Recruit cards use a stronger visual hierarchy: player identity, national/public rating and scouted overall range, location/source and scouting progress, then user interest, offer count and leader. Board and scholarship state use distinct card colors, with actions grouped below the information instead of compressing the text into a narrow row.

Overview headlines are selected for variety rather than taking the first three entries from the full news feed. Notable results receive priority, user-team stories are represented when available, and no more than one active injury report appears in the Overview set. Upsets and injury updates use deterministic headline variants derived from recorded event identifiers; story facts remain deterministic and do not invent rankings, interviews or outcomes.

## Validation

- 233 Release tests passed, including direct depth-order placement, starter recalculation, deterministic headline selection and injury-crowding coverage.
- The .NET 10 MAUI Android project compiled successfully and produced a signed debug APK locally.
- GitHub Android packaging and emulator launch validation are recorded on the Build 220 workflow.
