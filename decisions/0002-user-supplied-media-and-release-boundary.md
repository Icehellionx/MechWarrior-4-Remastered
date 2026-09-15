# ADR 0002: User-supplied media and release boundary

- Status: accepted
- Date: 2026-09-15

## Context

The project must support retail ISOs and commonly circulated archival ZIP layouts without publishing original game media. The local evidence set includes original-looking discs, obsolete DRM components, third-party crack directories, executable replacements, and a serial-key text file. The 2010 Microsoft clearance concerned a particular MekTek Mercenaries distribution and is not a blanket grant for every retail title or mirror.

## Decision

Public source and releases contain only project-owned code/assets, redistributable dependencies with recorded licenses, declarative fingerprints/metadata, and generated manifests. Users supply game media at install time.

The media boundary may recognize supported layouts that contain prohibited extras, but extraction plans must exclude crack/no-CD directories, serial/key material, C-Dilla/SafeCast components, SafeDisc drivers, and every path outside an explicit game/patch allowlist. Prohibited files are never executed, copied into staging, or packaged.

Raw manuals remain local inputs. The reproducible cleanup pipeline is public, while cleaned PDFs remain untracked until redistribution rights are independently documented.

Every executable or library in a release tree requires an explicit relative-path allowlist entry. Release verification rejects disc-image formats, credentials, private keys, dumps, reparse points, legacy DRM files, crack directories, and unreviewed executable content.

## Alternatives

- Bundling media because MW4 is often described as freeware conflates a limited historical distribution agreement with public-domain status.
- Rejecting any image containing an extra crack directory would unnecessarily block otherwise usable user media when safe allowlisted extraction can provably ignore it.
- Shipping opaque replacement executables would weaken provenance, reproducibility, and antivirus review.

## Consequences

- Installation requires local user media and may support multiple known layouts.
- Media recognition and extraction are separate: recognition never grants a path permission to be copied.
- Cleaned manuals can be tested locally but are not committed or published by default.
- No-disc behavior must be implemented as a documented exact-input transform or reviewed redistributable source component.

## Rollback

A future rights grant may expand the payload only through a new ADR with source/license evidence and updated release tests. It does not weaken the default user-supplied-media path.

## Verification

- Synthetic media recognition accepts complete layouts and rejects partial, ambiguous, absolute, and traversal inventories.
- Real-media smoke recognizes all seven supplied ISO layouts and releases every owned mount.
- Release-tree policy tests reject media, DRM, crack/key, reparse, and non-allowlisted executable classes.
