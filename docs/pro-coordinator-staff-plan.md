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

## Roster/draft implementation checkpoint

The current uncommitted batch implements staff FA, waiver review/resolution, filtered cap repair/final cuts and automatic opposite-unit draft choices through the existing manager/transactions. Staff commitments use the shared GM review without changing career/team identity. Automatic rookie rights are preserved unsigned on future-budget rejection; later signing rechecks authority/budget. Draft UI resumes GM decisions on entry for existing saves. Tests/device acceptance are pending as detailed in current-work.md; do not call this APK-verified yet. NFL waiver original-contract assumption and broader financial/source/market roadmap remain separate unresolved work.

## Accepted coordinator roster/draft batch — Pro58

Application/core source `aad411d`. [Exact APK58](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40462840012/artifacts/12714304881). Build run40462840012 passed196 fast Pro cases,15 UI helpers,32-pair source validation and packaging. Harness-only source6cf9b3b [verification run40485812027](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40485812027) passed app-input equality,16 fixture/loader cases and all six OC/DC retention/roster/draft device scenarios. [Device evidence12725815166](https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40485812027/artifacts/12725815166) is downloaded in .artifacts/pro60-ui; draft and roster screenshots reviewed. Persisted staff FA acquisitions, other-unit GM draft selections/own-unit stops, controlled contract/ownership preservation, coach identity, retention/expiry and advancement all passed. Synthetic fixtures are outside the APK and do not represent real roster/cap facts.

All196 full Pro cases passed locally in6m41s before two extra rollback cases;18 final targeted cases passed afterward including four real-loader fixture round trips. Long generated/sourced seasons and the existing three coordinator seasons with explicit broad GM delegation are included in the earlier full run; do not describe that annual test as an automatic-only coordinator career. Core cases additionally cover future-unit rejection, reserved unsigned rookie rights/recheck, waiver signing-time review, safe cuts and rollback. The exact-APK retries corrected invalid one-team fixtures and offscreen-control synchronization; no production save-validation change, app rebuild or duplicate long run was needed. Earlier failed runs are superseded for acceptance.

Pending offers now share exact bonus remainder and future service-minimum quoting across GM/trade review. Staff use existing CPU transactions and shared budgets without role impersonation. Main, College/shared engine and GameCast remain unchanged: reuse College390/full Pro44 and prior trade/finance evidence. Waiver original-contract assumption and financial source reconciliation/default promotion remain unfinished. Mobile draft/retention tables still need more accessible primary actions without horizontal hunting; this acceptance proves the scoped decisions, not completion of all UI/roadmap work.
