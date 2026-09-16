# 0008 — Defender-clean release boundary

- Status: Accepted
- Date: 2026-09-16

## Context and evidence

SafeDisc compatibility research necessarily touches techniques that antivirus products scrutinize. During an ignored evaluation of historical SafeDiscLoader 1 material, Microsoft Defender detected `Program:Script/Wacapew.A!ml` first in the opaque upstream `SDLoader.dll`, then in a disposable executable that embedded it. Defender quarantined both. The detected artifacts were never tracked, packaged, or required by the product-owned launcher.

The already-qualified Black Knight path is materially different: its loader is built from pinned GPL source, its narrow helper is project-owned and `asInvoker`, its payload is exact-hash gated, and its installed tree previously passed Defender scanning. Nevertheless, source provenance and privilege boundaries do not substitute for scanning final bytes.

## Decision

- Defender cleanliness is a release gate for every setup package, extracted package tree, and representative installed tree.
- Scans use the current local Defender engine and intelligence with remediation enabled. The project does not add exclusions, suppress detections, weaken protection, or tell users to bypass warnings.
- The scan wrapper checks both process outcome and new Defender operational events. This is necessary because Microsoft's documented exit code `0` can mean either no malware or malware found and successfully remediated.
- Release-tree policy rejects known evaluation injectors, legacy unwrappers, patch engines, dump tools, and opaque compatibility binaries even if a future packaging allowlist names them.
- A detection blocks qualification. Research artifacts may be investigated only in ignored, disposable locations and are never promoted into release inputs.

## Alternatives considered

- Rely only on an executable allowlist: rejected because allowlisting controls intent and inventory, not antivirus classification.
- Use Defender exclusions or disabled remediation: rejected because that weakens the user's protection or hides the behavior being measured.
- Treat unsigned SmartScreen reputation and malware detections as equivalent: rejected; they have different causes and remediation, and both must be reported accurately.

## Consequences

- Every release record must include Defender engine/intelligence versions, targets, results, and any unscanned coverage gaps.
- New or changed native compatibility bytes require a fresh scan even when their sources and hashes are otherwise qualified.
- Code signing may improve identity and SmartScreen reputation, but it cannot waive malware scanning or turn a detection into an acceptable release.

## Rollback

If the scan wrapper proves unreliable, releases remain blocked until an equally strict replacement records current-engine scan evidence and detects successful remediation as failure. The release gate itself is not removed.

## Verification

- The Defender operational log identified the two detections and successful quarantine actions at the exact ignored research paths.
- The offending opaque DLL, its embedded evaluation output, and disposable compiled dump/patch tools were removed while source notes and user media were preserved.
- Synthetic release-tree tests reject each newly named artifact even when explicitly executable-allowlisted.
- Fresh installer, launcher, and x86 compatibility-helper Release build trees passed custom scans with Defender engine `1.1.26080.3` and intelligence `1.459.223.0`, with no threats and no new matching detection/remediation events.
