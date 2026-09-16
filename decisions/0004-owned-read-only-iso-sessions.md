# ADR 0004: Owned read-only ISO sessions

- Status: accepted for initial implementation
- Date: 2026-09-15

## Context

The installer must accept user-selected original ISO files while keeping media mounting out of WinForms and ensuring it never detaches an image mounted by the user or another process. Mount failure, invalid volume discovery, recognition failure, and normal completion must have an explicit cleanup owner.

## Decision

Represent a mounted image as an `OwnedIsoMediaSession`. Before mounting, validate an existing regular `.iso` file and reject reparse points. Query Windows first and refuse any already-attached image rather than borrowing it. Mount explicitly read-only, validate the returned filesystem root, and dismount only the image owned by that session. Disposal is idempotent. A failed mount/root-validation path attempts dismount; if both the primary operation and cleanup fail, preserve both exceptions.

Use a bounded `PowerShellDiskImageBackend` around the inbox `Get-DiskImage`, `Mount-DiskImage`, `Get-Volume`, and `Dismount-DiskImage` cmdlets for the initial Windows implementation. Invoke Windows PowerShell non-interactively with an encoded, process-local command, literal-quote paths, redirected output, no execution-policy bypass, and a 30-second timeout. Keep the backend behind `IDiskImageBackend` so it can be replaced without changing session ownership or installer orchestration.

## Consequences

- ISO mounting remains a Windows-only media-access capability and never enters launcher/installer presentation code.
- The application refuses pre-attached images, so a user may need to detach one before selecting it.
- The backend adds PowerShell startup latency and depends on inbox storage cmdlets; packaging/hardware qualification must measure this.
- Archive ZIP support remains a separate contained-extraction contract.

## Rollback

Replace `PowerShellDiskImageBackend` with a reviewed Windows API implementation while retaining `OwnedIsoMediaSessionFactory`, its ownership rules, and tests.

## Verification

Synthetic tests cover single-owner disposal, idempotence, pre-attached refusal, invalid mounted-root cleanup, and extension rejection. A real Black Knight ISO smoke recognized the expected layout with both SafeDisc paths excluded and verified the image was detached after the probe exited.
