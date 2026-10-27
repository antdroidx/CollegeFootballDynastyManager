# October 26 contract coverage / opening budget audit

Developer diagnostic, not reconciled NFL cap-space reporting. The supplied 2026 allowance CSV remains authoritative for game budgets. The contract pack uses the October 26 nflverse/OTC snapshot. All 32 rows below use full-roster game obligations, include generated fallback contracts, and have no imported departed-player debt. These differences must be resolved before switching the default to sourced contracts or claiming a real-world opening available balance. No artificial balancing adjustment was added.

| Team | Source deals | Fallbacks | Adjusted allowance | Modeled commitments | Modeled room |
|---|---:|---:|---:|---:|---:|
| BUF | 62 | 12 | $296,960,000 | $263,543,545 | $33,416,455 |
| MIA | 55 | 20 | $302,250,000 | $149,245,719 | $153,004,281 |
| NE | 63 | 20 | $347,530,000 | $318,396,830 | $29,133,170 |
| NYJ | 64 | 16 | $314,250,000 | $206,666,354 | $107,583,646 |
| BAL | 60 | 18 | $311,100,000 | $309,493,631 | $1,606,369 |
| CIN | 63 | 12 | $307,620,000 | $310,663,617 | $-3,043,617 |
| CLE | 62 | 20 | $325,640,000 | $220,810,727 | $104,829,273 |
| PIT | 66 | 14 | $307,590,000 | $308,587,977 | $-997,977 |
| HOU | 71 | 13 | $304,610,000 | $237,258,836 | $67,351,164 |
| IND | 58 | 18 | $282,320,000 | $285,017,944 | $-2,697,944 |
| JAX | 60 | 18 | $305,280,000 | $264,393,531 | $40,886,469 |
| TEN | 66 | 11 | $313,870,000 | $281,267,055 | $32,602,945 |
| DEN | 65 | 14 | $298,670,000 | $300,507,934 | $-1,837,934 |
| KC | 56 | 22 | $298,700,000 | $304,098,612 | $-5,398,612 |
| LAC | 64 | 15 | $278,050,000 | $286,325,569 | $-8,275,569 |
| LV | 59 | 19 | $307,200,000 | $267,058,941 | $40,141,059 |
| DAL | 65 | 14 | $312,000,000 | $277,357,820 | $34,642,180 |
| NYG | 64 | 18 | $289,410,000 | $256,998,260 | $32,411,740 |
| PHI | 62 | 17 | $306,680,000 | $231,358,075 | $75,321,925 |
| WSH | 64 | 11 | $303,070,000 | $288,582,995 | $14,487,005 |
| CHI | 66 | 15 | $302,580,000 | $288,434,750 | $14,145,250 |
| DET | 64 | 18 | $286,830,000 | $268,751,316 | $18,078,684 |
| GB | 67 | 15 | $304,920,000 | $274,431,831 | $30,488,169 |
| MIN | 57 | 18 | $307,750,000 | $271,972,865 | $35,777,135 |
| ATL | 63 | 17 | $274,900,000 | $241,625,068 | $33,274,932 |
| CAR | 67 | 12 | $309,620,000 | $295,890,850 | $13,729,150 |
| NO | 67 | 14 | $313,770,000 | $200,456,215 | $113,313,785 |
| TB | 56 | 18 | $301,630,000 | $301,325,623 | $304,377 |
| ARI | 64 | 18 | $316,010,000 | $241,469,407 | $74,540,593 |
| LAR | 60 | 20 | $311,960,000 | $318,143,356 | $-6,183,356 |
| SEA | 62 | 17 | $302,100,000 | $311,522,775 | $-9,422,775 |
| SF | 70 | 15 | $306,240,000 | $256,703,350 | $49,536,650 |

Seattle still has 17 fallback contracts and modeled room of -$9,422,775. This is not the quoted $13,442,430 source space: the dates/counting basis, reserve/practice-squad exemptions, departed-player liabilities and source coverage are not aligned. Changing the headline alone would hide missing accounting.

Importer source: https://github.com/nflverse/nflverse-data/releases/tag/contracts
Seattle reference: https://overthecap.com/salary-cap/seattle-seahawks

Reproduce with `dotnet run --project tools/ProContractAudit -- OUTPUT.json`. Retain full dollars in the ledger; $250K steps apply to user offer controls/display rounding.

## Seasonal counting follow-up — new franchises only

Cap-rules version 2 applies offseason salary exclusions while retaining bonus/other charges. Existing rules 0/1 saves keep full-roster counting. A salary credit already present in a source row bounds any additional exclusion, preventing double relief. These are modeled figures, not a reconciliation to official space; source coverage/departed-player debt still need work.

| Team | Excluded camp salary | Camp room | Full-roster room |
|---|---:|---:|---:|
| BUF | $8,363,323 | $41,779,778 | $33,416,455 |
| MIA | $10,706,599 | $163,710,880 | $153,004,281 |
| NE | $17,892,978 | $47,026,148 | $29,133,170 |
| NYJ | $14,317,316 | $121,900,962 | $107,583,646 |
| BAL | $14,193,094 | $15,799,463 | $1,606,369 |
| CIN | $10,480,399 | $7,436,782 | $-3,043,617 |
| CLE | $16,253,339 | $121,082,612 | $104,829,273 |
| PIT | $13,617,595 | $12,619,618 | $-997,977 |
| HOU | $18,194,172 | $85,545,336 | $67,351,164 |
| IND | $12,290,345 | $9,592,401 | $-2,697,944 |
| JAX | $13,096,260 | $53,982,729 | $40,886,469 |
| TEN | $11,497,983 | $44,100,928 | $32,602,945 |
| DEN | $13,023,400 | $11,185,466 | $-1,837,934 |
| KC | $13,076,383 | $7,677,771 | $-5,398,612 |
| LAC | $14,397,855 | $6,122,286 | $-8,275,569 |
| LV | $13,327,317 | $53,468,376 | $40,141,059 |
| DAL | $14,541,716 | $49,183,896 | $34,642,180 |
| NYG | $16,563,283 | $48,975,023 | $32,411,740 |
| PHI | $13,311,528 | $88,633,453 | $75,321,925 |
| WSH | $12,161,056 | $26,648,061 | $14,487,005 |
| CHI | $15,225,795 | $29,371,045 | $14,145,250 |
| DET | $15,206,701 | $33,285,385 | $18,078,684 |
| GB | $17,029,900 | $47,518,069 | $30,488,169 |
| MIN | $10,223,673 | $46,000,808 | $35,777,135 |
| ATL | $14,683,745 | $47,958,677 | $33,274,932 |
| CAR | $14,618,423 | $28,347,573 | $13,729,150 |
| NO | $15,551,689 | $128,865,474 | $113,313,785 |
| TB | $11,004,883 | $11,309,260 | $304,377 |
| ARI | $19,094,917 | $93,635,510 | $74,540,593 |
| LAR | $15,479,561 | $9,296,205 | $-6,183,356 |
| SEA | $15,095,373 | $5,672,598 | $-9,422,775 |
| SF | $19,945,806 | $69,482,456 | $49,536,650 |

Seattle camp room is $5,672,598 after $15,095,373 of permitted modeled exclusions; full-roster room remains -$9,422,775. No invented team adjustment closes the remaining gap to the quoted source figure.


## Official-source cross-check — October 26

NFL Football Operations confirms the 2026 league base of $301.2M: https://operations.nfl.com/calendar-events/nfl-free-agency/nfl-salary-cap . The NFLPA public team report at https://nflpa.com/reports/public-salary-cap-report returned HTTP403 during this audit. Therefore the supplied 32-team rollover/adjustment table is arithmetically validated and source-attributed, but **not independently verified against a dated NFLPA team report**. Do not label its team amounts authoritative or infer omitted opening obligations. This does not block simulation against the explicitly imported budgets; complete source reconciliation/default promotion remains outstanding.
