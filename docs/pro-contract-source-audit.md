# Contract coverage / opening budget audit

## Corrected allowance audit — source 61ec66e

This section supersedes the older budget tables below. Exact allowance inputs use the paired NFLPA observation in docs/data; source commitments still contain generated fallbacks and no imported departed-player debt. Seasonal exclusions are shown separately. Reported source room is evidence only, never a balancing input.

| Team | Sourced | Fallback | Allowance | Charged commitments | Camp excluded salary | Modeled room |
|---|---:|---:|---:|---:|---:|---:|
| BUF | 62 | 12 | $301,580,912 | $255,180,222 | $8,363,323 | $46,400,690 |
| MIA | 55 | 20 | $308,878,443 | $138,539,120 | $10,706,599 | $170,339,323 |
| NE | 63 | 20 | $352,979,254 | $300,503,852 | $17,892,978 | $52,475,402 |
| NYJ | 64 | 16 | $323,863,800 | $192,349,038 | $14,317,316 | $131,514,762 |
| BAL | 60 | 18 | $314,726,142 | $295,300,537 | $14,193,094 | $19,425,605 |
| CIN | 63 | 12 | $317,958,913 | $300,183,218 | $10,480,399 | $17,775,695 |
| CLE | 62 | 20 | $339,154,202 | $204,557,388 | $16,253,339 | $134,596,814 |
| PIT | 66 | 14 | $321,760,072 | $294,970,382 | $13,617,595 | $26,789,690 |
| HOU | 71 | 13 | $310,744,359 | $219,064,664 | $18,194,172 | $91,679,695 |
| IND | 58 | 18 | $303,765,630 | $272,727,599 | $12,290,345 | $31,038,031 |
| JAX | 60 | 18 | $310,713,790 | $251,297,271 | $13,096,260 | $59,416,519 |
| TEN | 66 | 11 | $343,717,978 | $269,769,072 | $11,497,983 | $73,948,906 |
| DEN | 65 | 14 | $304,221,900 | $287,484,534 | $13,023,400 | $16,737,366 |
| KC | 56 | 22 | $302,110,749 | $291,022,229 | $13,076,383 | $11,088,520 |
| LAC | 64 | 15 | $305,894,485 | $271,927,714 | $14,397,855 | $33,966,771 |
| LV | 59 | 19 | $315,202,045 | $253,731,624 | $13,327,317 | $61,470,421 |
| DAL | 65 | 14 | $332,065,571 | $262,816,104 | $14,541,716 | $69,249,467 |
| NYG | 64 | 18 | $300,816,176 | $240,434,977 | $16,563,283 | $60,381,199 |
| PHI | 62 | 17 | $316,623,458 | $218,046,547 | $13,311,528 | $98,576,911 |
| WSH | 64 | 11 | $328,630,626 | $276,421,939 | $12,161,056 | $52,208,687 |
| CHI | 66 | 15 | $309,914,708 | $273,208,955 | $15,225,795 | $36,705,753 |
| DET | 64 | 18 | $323,032,099 | $253,544,615 | $15,206,701 | $69,487,484 |
| GB | 67 | 15 | $314,903,931 | $257,401,931 | $17,029,900 | $57,502,000 |
| MIN | 57 | 18 | $314,930,297 | $261,749,192 | $10,223,673 | $53,181,105 |
| ATL | 63 | 17 | $305,612,517 | $226,941,323 | $14,683,745 | $78,671,194 |
| CAR | 67 | 12 | $312,564,671 | $281,272,427 | $14,618,423 | $31,292,244 |
| NO | 67 | 14 | $317,772,192 | $184,904,526 | $15,551,689 | $132,867,666 |
| TB | 56 | 18 | $317,516,374 | $290,320,740 | $11,004,883 | $27,195,634 |
| ARI | 64 | 18 | $328,107,832 | $222,374,490 | $19,094,917 | $105,733,342 |
| LAR | 60 | 20 | $314,323,123 | $302,663,795 | $15,479,561 | $11,659,328 |
| SEA | 62 | 17 | $315,053,262 | $296,427,402 | $15,095,373 | $18,625,860 |
| SF | 70 | 15 | $341,170,524 | $236,757,544 | $19,945,806 | $104,412,980 |

Seattle modeled camp room is $18,625,860; full-roster room is $3,530,487. OTC currently reports $13,442,430 with $300,971,343 total cap liabilities, implying a different $314,413,773 allowance/basis than the observed NFLPA $315,053,262. Date/basis, player statuses, source coverage and departed debt still require reconciliation; neither discrepancy is filled with invented charges.

References: https://nflpa.com/reports/public-salary-cap-report ; https://overthecap.com/salary-cap/seattle-seahawks

## Historical audit before allowance correction
# October 26 contract coverage / opening budget audit

Historical developer diagnostic, superseded by the corrected audit above. The original supplied allowance CSV was subsequently found to contain rounded spending rather than actual allowances. The contract pack uses the October 26 nflverse/OTC snapshot. All 32 rows below use full-roster game obligations, include generated fallback contracts, and have no imported departed-player debt. These differences must be resolved before switching the default to sourced contracts or claiming a real-world opening available balance. No artificial balancing adjustment was added.

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


### Cached NFLPA cross-check follow-up

A domain-restricted search retrieved the indexed text of the NFLPA public report with query parameters `?TB_iframe=true&height=512.1&width=921.6`, indexed about four weeks before this audit. Its stated base is $301.2M. The indexed text contains **two rows per club** with different team-cap/room values; the displayed totals also reflect duplicates. Comparing only the first row per club (explicitly a candidate series, not an assumed current authoritative series), all 32 supplied carryovers and all 32 adjusted team caps differ by at most $5,000, consistent with the supplied table's $10K rounding. Seattle candidate carryover is $15,029,706 and adjusted cap $302,100,001, versus imported $15,030,000 / $302,100,000. This supports the supplied allowance table's origin/arithmetic, but **does not identify the duplicate series, establish the report's effective date, or reconcile player commitments/dead money**. Live retrieval still returns HTTP403. No opening-room balance or liabilities were imported, and no application budget was overwritten from cached search text.
