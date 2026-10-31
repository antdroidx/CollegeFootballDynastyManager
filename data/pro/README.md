# Offline Pro contract evidence

The bundled Pro roster source identifies kicking specialists with `NcaaPositionCode`: `19` is kicker and `20` is punter. The app normalizes those source codes to distinct `K` and `P` playing positions when opening both new and existing franchises; this also maps `20` to the `K/20` market curve.

Local source bundles live in `source-snapshots/YYYY-MM-DD/`, outside disposable build artifacts. Raw HTML/Parquet and full notes are ignored by Git; tracked manifests in `docs/data/` retain hashes, source URLs, observation dates and unresolved identities. These bundles are developer inputs, never loaded by either Android app. Preserve them during build-artifact cleanup.

The October26 bundle contains the complete53,507-row raw Parquet snapshot,2,540 current-source records (2,532 unique OTC IDs), all32 cached team pages, NFLPA observations and identity/status reviews. Eight duplicate current identities remain explicit. The normalized current-contract file preserves integer-dollar values and nulls; annual source guarantee values are not silently reclassified as guaranteed base. Raw history remains available in Parquet. Existing club balance observations do not establish one common effective date.

Reproduce with `build/prepare-pro-contract-sources.py` and a matching original Parquet plus cached team pages. The tool verifies reviewed hashes, refuses changed raw evidence under an existing date, and can rerun entirely offline. Use `--python-libs` only to point to an installed pyarrow environment.

The October27 notes bundle is a separate observation of the95 future-bonus cohort. The capture report records25 cached pages and70 timed-out pages at this checkpoint, not complete legal-term coverage. `build/capture-pro-contract-notes.py --offline` regenerates the local index without requests. Online acquisition is limited to two concurrent requests and an explicit per-run fetch budget. Notes require manual event/vesting review before any financial correction.

No source default, current ownership, historical cash, guarantees or opening cap room is changed by either preparation tool. Contract corrections require the reconciliation gates in `docs/pro-contract-reconciliation.md`.
