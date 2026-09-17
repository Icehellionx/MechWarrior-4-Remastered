# 0011 — Shared Vengeance family tree and official update chain

- Status: Accepted
- Date: 2026-09-16
- Supersedes: the installation-policy and qualification conclusions of ADR 0007

## Context and evidence

Field testing disproved the previous Black Knight qualification: a responsive process was actually presenting the legacy `MechWarrior 4 has been incorrectly installed` dialog. The implementation had flattened Black Knight's `MW4X` files into a separate product directory and tested the retail `45.05.10.0701` executable.

Three independent evidence paths disagree with that architecture:

- Original setup-table strings keep expansion-named tools in the Vengeance root and launch `%APPPATH%\MW4X\MW4x.exe`.
- Archived Microsoft KB 325734 documents Black Knight at `MechWarrior Vengeance\mw4x\MW4x.exe`.
- Current Lutris recipes and recent Windows community guidance install Black Knight into the Vengeance tree and configure the `MW4X` subdirectory separately.

Contained application of the exact official patch payloads supplied with the user's media established the update outputs without using community executables. Black Knight PR1 applied over Vengeance Patch 3 plus the expansion and produced `MW4X\MW4x.exe` version `45.30.04.1908`, SHA-256 `be9c15731b2ab59471f35add8df4935ea65355cbb4672bc48b63295ad83632b0`. Mercenaries PR1 produced protected image version `50.07.01.2105`, SHA-256 `aafff190e08119ff3ac86b0628813315cde05de46c0e36840ac3eba986a7d550`. The old custom Mercenaries build and historical Black Knight oracle were retail revisions.

## Decision

- Install Vengeance, Black Knight, and selected retail Mech Paks as one physical **Vengeance family** tree. Vengeance remains the base capability; Black Knight is rooted at `MW4X\MW4x.exe`.
- Represent that tree with one aggregate ownership manifest whose schema records installed capabilities/layers. Do not maintain competing manifests for overlapping physical roots.
- Build or repair the aggregate tree in sibling staging and commit it atomically. A later add-on operation rebuilds the aggregate tree from validated media and preserved user data rather than mutating an unversioned partial tree in place.
- Apply the official sequence before disc-check or renderer work: Vengeance Patch 3 when Black Knight or a retail Mech Pak requires the shared current state, then Black Knight PR1 for Black Knight, and Mercenaries PR1 for the independent Mercenaries tree.
- Treat official RTP payloads as exact-hash user-media inputs. Apply them only in contained scratch through the source-owned, non-elevating patch host; never package patch engines, RTP payloads, protected ICD files, or opaque replacement executables.
- Evaluate disc-check handling independently against each officially patched executable. A transform or loader qualified against a retail revision is not implicitly valid for a point release.
- Use per-title working directories and minimal arguments. Black Knight's executable, input wrapper, and any renderer proxy belong beside `MW4X\MW4x.exe`, not in the Vengeance root.

## Alternatives considered

- Separate self-contained Black Knight tree: rejected as the product default because it contradicts original setup, official support material, official patch layout, and established community practice.
- Layer-specific manifests in one directory: rejected because overlapping/replaced files make uninstall and repair ownership ambiguous.
- Skip official updates and retain known retail no-CD variants: rejected because it discards official fixes and caused version mismatches across executable, scripts, and resource archives.
- Run original patch front ends: rejected because the exact payload and engine can be invoked reproducibly in contained scratch without adding another UI or launch-time elevation.

## Consequences

- `InstallManifest` needs a schema revision or companion trusted install record for Vengeance-family capabilities.
- Destination planning, launcher discovery, repair, and uninstall must understand one shared family root rather than a standalone Black Knight product directory.
- Black Knight's previous SafeDiscLoader2-derived bundle is suspended. Restoring it requires a new exact-revision qualification against PR1 plus menu/gameplay proof and Defender scanning.
- Mercenaries remains physically and transactionally independent, but its transform input moves from retail to the official PR1 protected image.
- Mech Pak entitlement remains a separate evidence gate after file/patch parity.

## Rollback

Disable Black Knight and Mech Pak selection while retaining Vengeance and Mercenaries. Do not roll back to the flattened separate-tree design without primary evidence and a new ADR.

## Verification

- Synthetic planning tests assert `MW4X\MW4x.exe`, shared-root tools, aggregate capabilities, collision handling, atomic rebuild, and ownership-safe uninstall.
- Exact reference tests apply the locked Black Knight and Mercenaries RTP inputs and compare every changed output hash.
- Runtime tests positively identify each real main menu and fail on EULA, autoconfig, CD, incorrect-install, fullscreen, `STOP`, or `EXCEPTION` dialogs.
- Release qualification adds one gameplay path per title, pack visibility where selected, no launch-time elevation, and current Defender scans.
