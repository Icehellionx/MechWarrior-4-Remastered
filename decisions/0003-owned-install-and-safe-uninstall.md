# ADR 0003: Owned install manifest and safe uninstall

- Status: accepted
- Date: 2026-09-15

## Context

The all-in-one installer must later repair and remove its own files without deleting user saves, configuration, screenshots, mods, or unrelated content. Recursive deletion of an install directory cannot distinguish those classes and is unacceptable. Optical-media inputs also arrive read-only, while official patching and repair require writable installed files.

## Decision

Every committed install is built in a sibling staging directory and moved into place only after every planned file is copied successfully. Installed copies are made writable. The transaction writes `.mw4-remastered/install-manifest.json`, recording normalized relative path, byte length, SHA-256, and source-media label for every owned file.

Verification rejects missing, added, modified, unsafe, or reparse-point content. Uninstall has a different policy: it tolerates and preserves unowned files, but blocks before mutation if any present owned file differs from its manifest. When preflight succeeds, owned files and the manifest are moved into a transaction-owned removal directory before deletion; failures during the move phase roll files back. Only empty directories are removed afterward.

## Consequences

- Saves, settings, mods, and screenshots remain untouched unless a future ADR explicitly declares a specific path project-owned.
- A user-modified owned binary blocks automatic uninstall until repaired or explicitly resolved; the uninstaller does not guess.
- Missing owned files do not block uninstall because there is nothing left to remove.
- The manifest is security-sensitive state and is path-validated; it can never authorize traversal or reparse-point following.
- The current manifest is not yet cryptographically anchored outside the install tree. Release qualification requires a protected external install record or independently trusted product manifest so a forged local manifest cannot relabel unowned in-root content as owned.
- Registry ownership and per-user save locations remain future work and require separate manifests/policy before mutation.

## Alternatives rejected

- Recursive deletion of the install directory: cannot distinguish project payload from user content.
- Filename-only ownership: cannot detect replacement, tampering, or edition drift.
- Deleting modified owned files automatically: too destructive for a preservation tool and removes the user's ability to inspect or recover local changes.
- Leaving optical-media attributes unchanged: prevents reliable patching, repair, and removal.

## Rollback

The copy transaction deletes its uniquely named sibling staging directory if commit has not occurred. Uninstall first moves verified owned files into a uniquely named removal directory; a move-phase failure returns already moved files to their original paths in reverse order. Once the removal directory is deleted, rollback becomes reinstall/repair from validated user media.

## Verification

Synthetic tests cover successful commit, failure rollback, traversal rejection, case-insensitive duplicate destinations, read-only normalization, manifest tamper/extra-file detection, save preservation, and modified-owned-file blocking. A real-tree smoke copied the 231-file Vengeance baseline, added an unowned pilot save, removed all 231 payload files plus the manifest, verified the save remained, and then deleted the exact disposable smoke target.
