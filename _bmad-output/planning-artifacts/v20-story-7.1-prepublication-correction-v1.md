# V20 Story 7.1 Prepublication Correction

- Decision date: `2026-09-13`
- Classification: `failed-prepublication-candidate`
- Failed candidate: `596cee6fa5ae12a7ff6e8ac35960f60863b55a27`
- Candidate parent: `64b050831eea694cb2065342cf23a150646eef57`
- Declared semantic-source SHA-256: `90477eb2666c2dc693192770b801664fedab385638917e6202dd9a7e09d4166d`
- Committed semantic-source SHA-256: `eeee633e7045d9636d4babe62b6bca9744c8137b490da2304fcc9192f84fbaf5`
- V19 authority published from the failed candidate: `false`
- V20 authority published from the failed candidate: `false`

The declared digest was captured before the human-approved September 13 V19-to-V20
and JUnit-ledger amendments were applied. The pre-amendment bytes were never
committed, while the amended semantic source was first committed by the failed
candidate above. The candidate therefore cannot serve as a valid V20 entry
baseline.

The approved correction preserves the committed amended semantic source and
rebinds the unpublished V20 inventory, schemas, publisher, and tests to its exact
bytes before either successor authority is published. The correction transaction
must validate independently and be committed before creating the fresh V19
checkpoint candidate.

This record does not certify V19 or V20, lift the Story 7.1 implementation hold,
mark any story done, unlock Story 7.2, or authorize release or push.
