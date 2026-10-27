# Coordinator staff / GM completion audit

Implement after the in-flight trade APK is accepted; keep application inputs stable for possible exact-APK test retries.

## Confirmed code gaps

- Live unit control and fifth-year opposite-unit staff decisions already work. Do not rebuild them.
- ProCpu.Manage excludes the user team unless includeUser or unemployment. Consequently a coordinator's uncontrolled unit does not receive the ordinary automatic free-agent/waiver/depth handling that other CPU units do.
- Retention advancement skips the user team when AutoNegotiateExpiring is false. ProRetention.Negotiate also restricts user-team players to controlled positions. This leaves the opposite unit unmanaged, despite the coordinator lacking authority to renew those players.
- A fired user's former team is still UserTeam; retention similarly skips it when delegation is false, while CPU in-season management correctly includes unemployed franchises.
- Review retention change-year semantics with tests before altering policy: the ending-season lock and next-year bonus booking are different concepts. New free-agent signings currently leave LastChangedYear at zero; do not assume one-year signings are automatically blocked. Preserve the established once-per-season lock unless a concrete failing case demonstrates a defect.
- Draft recommendations currently stop at user-owned picks; audit GM selection for the other unit rather than forcing a coordinator to recommend only their own unit at every pick. Shared picks remain GM assets and require existing value/budget review.

## Incremental acceptance

Start with retention and explicit staff-unit authority. Refactor the existing negotiation path rather than copy a financial evaluator. Staff manage only uncontrolled positions when the user is a coordinator; ordinary head-coach choices retain manual/delegated behavior. An unemployed former team is fully CPU-managed. All extensions retain agent demands, change locks, future budgets and depth reserves; no cap forgiveness or rewriting negotiated saves. User LetExpire choices remain explicit.

Then extend roster fill/cuts/waivers using a staff-owned-player/position predicate in the existing CPU manager. Never release or negotiate a controlled-unit player on an automatic opposite-unit pass. Preserve specific OL/kicking coverage, shared cap/future commitments and roster limits. User-requested GM fill can remain an explicitly broad decision. Do not silently trade user-controlled players or spend shared future picks outside existing GM safeguards.

Tests: OC and DC mirrored cases; head coach manual and delegated behavior; unemployment; contract-change year/locks; no cross-unit mutation; under-cap future planning and atomic rejection; draft GM behavior. Run scoped career/financial cases and one annual coordinator scenario, plus wider annual tests if common CPU finance changes. Reuse unchanged College/GameCast evidence; verify coordinator management UI/notifications on the resulting exact Pro APK.


Retention milestone accepted in Pro57 /4294f8e, run40458818959. Staff/opposite-unit and unemployed-former-team retention gaps are fixed and verified, including future unit/team budgets without role impersonation. Continue roster/waiver/cut/draft scope; do not repeat completed retention implementation or assume its annual test proved unimplemented automatic roster handling (the scenario explicitly delegates to GM). Retention table remains horizontally scrollable on phones; later mobile UI polish should make action access/swipe guidance clearer without rebuilding solely for wording.
