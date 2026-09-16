# ADR 0005: ISO-only archival ZIP extraction

- Status: accepted for initial implementation
- Date: 2026-09-15

## Context

Common archival downloads wrap one or two original disc images in a ZIP. Some pack archives also contain serial text, cracks, NFOs, or other files that must never enter installation inputs or temporary extraction merely because they share an archive with a valid ISO.

## Decision

Treat ZIPs as containers for ISO media only, not as generic trusted install sources. Validate every entry path before mutation, reject traversal, duplicates, reparse input archives, and Unix symbolic-link entries, then extract only regular `.iso` entries. Report all other files as excluded without opening or writing them. Allow at most four ISOs, at most 4 GiB per ISO, and 8 GiB total expanded ISO data. Extract to a uniquely owned sibling staging directory, verify the exact resulting inventory, and atomically commit the extraction root.

The media-access caller owns the committed scratch root and deletes it after each contained ISO has been processed through an owned read-only mount session.

## Consequences

- Archives containing valid media plus serial/crack material can be used safely without copying the prohibited adjacent files.
- ZIPs containing already-extracted game trees are intentionally unsupported by this path.
- Other archive formats require a later explicit contract rather than falling through to a generic unpacker.
- Large archive extraction requires temporary free space and progress/cancellation work before installer UI integration.

## Rollback

The extractor deletes its unique staging directory on failure. The caller deletes the committed scratch root after owned ISO sessions close.

## Verification

Synthetic tests prove two-ISO extraction, non-ISO exclusion, and pre-extraction traversal rejection. A real Black Knight archival ZIP smoke extracted only `MW4BK.iso`, recognized the expected disc layout and SafeDisc exclusions, dismounted it, and left no new probe scratch directory.
