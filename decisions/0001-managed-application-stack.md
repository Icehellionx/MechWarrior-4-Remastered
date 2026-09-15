# ADR 0001: Managed application stack

- Status: accepted for initial implementation
- Date: 2026-09-15

## Context

The product needs a Windows-only installer/launcher with custom presentation, deterministic filesystem/registry/process behavior, unit-testable policy, and no browser/runtime service. The MW3 reference proved WinForms viable but accumulated complexity by compiling large source files directly. This project has a current .NET 10 SDK and targets modern Windows.

## Decision

Use SDK-style C# projects. Put reusable policy and models in a UI-free `net10.0` core assembly. Build the launcher as `net10.0-windows` WinForms with custom-drawn project-owned visuals. Keep installer and uninstaller as separate executables/services when introduced. Publish final executables self-contained after measuring size and antivirus behavior; do not require a network runtime bootstrap during installation.

Use declarative game/pack definitions. UI reads status from core services and never implements media, registry, patch, or deletion policy.

## Alternatives

- .NET Framework 4.8 minimizes runtime payload but constrains APIs/tooling and repeats MW3's build limitations.
- WPF offers richer layout but adds XAML/resource complexity without proving a launcher benefit.
- Electron/WebView adds a large web runtime and broader attack/update surface for a small offline Windows utility.

## Consequences

- Windows 10/11 x64 is the initial product target; legacy OS support requires a later explicit decision.
- Self-contained output size and Defender behavior become release measurements.
- WinForms styling must remain in presentation classes rather than leaking into install logic.

## Rollback

The core boundary is UI-free, so another Windows UI can replace WinForms without rewriting recognition/install policy.

## Verification

Core tests and launcher compilation must run with `dotnet build`/`dotnet test`; publish verification is added before the first packaged release.
