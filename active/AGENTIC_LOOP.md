# Agentic engineering loop

## Purpose

This is the standing workflow for building MechWarrior 4 Remastered without tangling installer, launcher, media, patching, compatibility, manuals, diagnostics, and uninstall behavior. Source, media structure, hashes, runtime logs, and reproducible tests outrank these documents when they disagree.

## Context intake

Always read this file, `HANDOFF.md`, the user's named evidence, and the nearest repository instructions. Load other active documents only when relevant:

| Document | Load when |
|---|---|
| `PROJECT_BRIEF.md` | Checking scope or user-facing product intent. |
| `ARCHITECTURE.md` | Changing ownership, dependencies, media/patch boundaries, or packaging. |
| `PRODUCTION_PLAN.md` | Choosing priorities, milestones, or acceptance criteria. |
| `TECH_DEBT.md` | Growing a large file, accepting risk, or extracting a contract. |
| `TEST_STRATEGY.md` | Changing behavior, tests, diagnostics, or release gates. |
| `RELEASES.md` | Building, signing, scanning, publishing, or making baseline claims. |
| `AUXILIARY_MODELS.md` | Routing or auditing auxiliary model work. |
| `../decisions/` | Making or revisiting a durable architectural choice. |

## Repository gate

Before Git work, resolve the owning repository with `git -C <candidate> rev-parse --show-toplevel`. The root is intended to own orchestration and product source unless a later ADR creates an independent component repository. Never stage or report one repository's status as another's.

## Slice contract

Before non-trivial edits state:

```text
User-facing goal:
Smallest verifiable slice this turn:
Verification before sign-off:
Shared contract inspected:
Adjacent behavior protected:
```

## Default loop

1. Read the smallest relevant context and inspect the owning worktree.
2. Identify the boundary that owns the requested behavior.
3. Establish a reproducible baseline from structure, hashes, tests, logs, or clean builds.
4. Separate facts from hypotheses, especially around freeware status, legacy installers, no-disc behavior, and 64-bit compatibility.
5. Prefer the smallest reversible change that proves the owning contract.
6. Keep UI, media access, install transactions, transforms, compatibility, manuals, diagnostics, and uninstall separate.
7. Run a static/compile check for every touched language.
8. Run a focused test distinguishing the new behavior from failure or mismatch.
9. Run owning smoke and adjacent game/pack/media regressions.
10. For release-affecting work, verify source provenance, payload allowlists, manifests/checksums, clean install/repair/uninstall, and Defender scans.
11. Update active documents only when current facts change; move completed detail to `archive/HISTORY.md`.
12. Report exact evidence, untested hardware/media paths, and remaining risks.

Documentation-only work follows the same loop at reduced depth. Security, registry, process lifetime, executable transforms, DLL proxying, installer, uninstall, and release changes require the full loop.

## Anti-spaghetti rules

- Each module owns one named contract and one reason to change.
- UI code requests actions; it does not implement registry, extraction, patching, or deletion.
- Installer orchestration coordinates services but does not absorb their implementations.
- Build scripts assemble declared inputs; they do not silently mutate source media.
- Media sessions and temporary state have explicit cleanup in `finally`, `using`, or RAII boundaries.
- Avoid catch-all `helpers`, `utils`, `manager`, `common`, and `misc` modules.
- Use one parameterized path for the three games and two packs unless evidence proves different contracts.
- Do not modify generated executables or user media when a reproducible exact-input transform or source build exists.
- Preserve upstream provenance and isolate local compatibility changes.

An extraction is complete only when ownership is clearer, callers use the boundary, parity tests pass, and the original owner measurably shrinks.

## Evidence ladder

1. Static: syntax, compile, schema, manifest, license/provenance validation.
2. Focused: deterministic recognition, transform, containment, cleanup, or status test.
3. Component: staging, media session, process, registry/config, manual pipeline.
4. Smoke: clean disposable install, launch where possible, repair, and uninstall.
5. Regression: three games, optional packs, media variants, and failure cleanup.
6. Release: reproducible build, payload inventory, checksums, Defender scans, documented legal/hardware gaps.
7. Field: sanitized logs from affected hardware; never private data or proprietary files sent to auxiliary models.

Do not claim a hardware, media, legal, or security result from a different evidence layer.

## Auxiliary review

Use local-first `auto` for bounded code/test review. Remote prompts must be small, sanitized, project-only, and stored under ignored `.local/`. Classify findings as accepted, rejected, deferred, duplicate, or unsubstantiated, then independently reproduce accepted defects. Auxiliary output never authorizes actions.

## Final report

```text
Outcome:
Files changed:
Boundary reused or introduced:
Verification run and results:
Smoke and regression paths:
Release gate run? yes/no:
Not checked:
Remaining risk and next action:
```
