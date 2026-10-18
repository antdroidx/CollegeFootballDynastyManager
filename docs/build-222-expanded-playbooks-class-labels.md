# Build 222: expanded playbooks, class labels and lighter surfaces

The working playbook now offers 40 offensive calls and 30 defensive calls. Each offensive formation has at least eight runs, quick passes, play-action or downfield concepts; each defensive formation has at least six balanced, run-focused, coverage or pressure calls. IDs are unique and definitions retain formation, personnel, concept/approach and a factual description for the future assignment editor. Favorites still save with the team game plan.

Player class years use football labels across user and league rosters, the depth organizer, player profiles and transfer-recruit detail: `FR`, `SO`, `JR`, `SR` and `GR`. A player who is currently redshirting or used a redshirt earlier keeps the `-RS` suffix for the remainder of the career, such as `JR-RS` or `GR-RS`.

Generic borders and one-pixel table gaps have been removed across the app. Alternating row surfaces, spacing, typography, school colors and explicit status colors provide organization without heavy separators. Coach traits now use the same clean label/rating treatment as player traits. School badges and identity avatars retain their intentional team-color outlines.

## Validation

- Catalog tests require unique play IDs, at least eight calls per offensive formation and at least six per defensive formation.
- Class-label tests cover every class tier, prior redshirt history and graduate redshirts.
- The full Release suite, Android packaging and API 36 emulator launch are validated by the Build 222 workflow.
