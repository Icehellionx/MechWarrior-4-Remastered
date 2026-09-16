# 0010 — Preserve the original product dependency order

Status: accepted  
Date: 2026-09-16

## Context

Vengeance is the base title. Black Knight was distributed as its expansion, and the retail Mech Pak documentation requires Vengeance with additional install-order behavior around Black Knight. Mercenaries is a separate title. A modern compatibility experiment demonstrated that a media-derived Black Knight tree could launch in isolation, but that technical result does not redefine the product the user is installing.

## Decision

- Model Vengeance as the required base for Black Knight.
- Model Inner Sphere and Clan Mech Paks as dependent on Vengeance; their eventual target overlays may include a supported Black Knight installation after the documented patch and visibility contract is reproduced.
- Keep Mercenaries independently selectable.
- Do not expose a Black Knight-only setup as the product's primary installation path.
- Complete shared media selection and Vengeance-first orchestration before presenting another end-user setup build for acceptance testing.

## Consequences

The proven Black Knight compatibility bundle remains useful internal evidence, but no longer determines setup flow or release readiness. The next compatibility slice remains the media-only Vengeance transform. Dependency-aware integration and failure tests are required before Black Knight or either Mech Pak becomes selectable for installation.

## Rollback

Revisit only if primary product documentation proves a different dependency topology. A standalone technical launch is not sufficient rollback evidence.

## Verification

Core dependency tests assert that Black Knight and both Mech Paks require Vengeance while Vengeance and Mercenaries remain independently selectable. The installer flow contract rejects a Black Knight-only primary action.
