# Guided offseason and compact management

## Phases and navigation

Pro opens the active offseason workspace automatically on load and whenever the year/stage changes: Contract retention, Free agency, Draft, then Training camp. Users can still visit Overview, rosters or finances; Overview keeps a prominent return-to-stage card. Continue actions state the next step and show draft/rookie-option/roster blockers. Mandatory rookie options have a direct action; delegation permits normal CPU resolution at advancement.

Training camp uses the existing saved RosterCuts value3. No enum renumbering, new calendar season or save conversion is required. It follows the draft and precedes preseason; the dense position-battle table replaces stacked cards. Final53-player cuts and cap compliance remain required before regular-season entry. Camp may enter preseason with a planning deficit, leaving the existing opening-day gate in force.

The Pro overview now has compact Advance/Simulate/Save controls, a titled single-row matchup, season/coaching/finance summary, a Weekly tasks grid and current action links, with news and transactions retained. Both games put Settings in their shell and move confirmed Save & exit into Settings. College's offseason card is the first dashboard highlight; FBs sort beside HBs in both roster views without changing numeric position IDs.

## Draft room

The workspace opens/advances CPU picks automatically to a user-controlled choice. Users have Prospects, My board, Draft results and Order views, including actual selections, current ownership, original clubs and compensatory picks. Prospect profiles show physical data, projected/scouted grades, confidence and estimated attribute bands instead of exposing exact private ratings. Scout-team processing and delegation reuse existing draft rules. Required option decisions remain visible before moving to camp.

## Contracts and free agency

Negotiations show full-season team cap, room before, this deal's cap hit and room after for current/future years. Extensions replace the old deal in the projection rather than double charging it; existing years stay in the contract. Offseason retention already schedules new terms/bonus at the next applicable league year. Manual HC offseason extensions may exceed planning room; CPU planning and coordinator GM reviews remain conservative, and regular-season eligibility still blocks an over-cap club. No historical obligations or opening room are invented.

Competing bids already resolve through the normal player-offer market. The free-agent table shows actual pending rival bids, not fabricated interest. Optional GM assistance proposes up to three affordable pending offers for controlled-unit roster needs and respects reserved bids, agent terms and roster limits. It does not cut or trade user players. Users can request proposals once or persist automatic proposals on advancement. Offer actions use an explicit accent fill, contrasting ink and visible edge on either table stripe.

## Backfield review

The review covers all198 snapshot HB/FB identities. Twelve additional real FBs are corrected by stable external ID, and Jack Westover uses his current club's TE designation. Existing eight FBs remain FBs; hybrids use current explicit club roles rather than weight. Sources and every reviewed row are in data/pro-backfield-position-audit-2026-10-30.json. No roster ownership, status, appearance or signed contract is changed. Old-save role corrections are idempotent and preserve depth assignments. Applications consume offline data only.

## Scoped evidence

Source401bd82. Pro115 build41110120686:64 targeted cases,16 helpers,32-team cap source validator and packaging0 warnings/errors. APK13003188026. College411 build41110126032:17 combined Core/College position/backfield cases,16 helpers and packaging0 warnings/errors. APK13002244520. Native guided-offseason and overview/roster/settings checks are still pending; neither new APK is accepted solely because packaging succeeded.

A fresh64-game real-Pro sample covers32 roster-backed teams after role corrections:63.14 plays,22.49 points,330.84 yards/team, with zero malformed/duplicate units and matched receiver/coverage target totals. Raw report: data/guided-backfield-2026-10-30/pro-real-64-games.json. Prior generated/College gameplay and annual evidence is reused where inputs are unchanged. This batch does not repeat full GameCast, five/20-season or full726-case suites. An over-broad initial local filter was stopped after accidentally starting a long case; final filters explicitly exclude it. No completed new long simulation is claimed.
