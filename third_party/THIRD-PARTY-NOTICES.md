# Third-party notices

## SafeDiscLoader2

Black Knight PR1 uses a modified, source-built copy of SafeDiscLoader2 from commit `f27286a363aa675a0422141cb96fc8619cf8b9d8`. Setup exact-hash validates the DLL and places it beside the exact official PR1 `MW4x.exe`; Windows loads it through the normal app-local DLL search path. The project patch supports only the qualified PR1 image layout and replaces two obsolete setup/media checks after decryption.

- Upstream: <https://github.com/nckstwrt/SafeDiscLoader2>
- License: GPL-3.0-only
- Installed license: `Compatibility/BlackKnightRuntime/SafeDiscLoader2-LICENSE.txt`
- Exact corresponding source: `Compatibility/BlackKnightRuntime/SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip`
- Exact project patch: `Compatibility/BlackKnightRuntime/SafeDiscLoader2-MW4-BlackKnight-PR1-Runtime.patch`

The upstream elevated `VersionInjector` project is neither built nor distributed. There is no launch helper, process injection, service, driver, or launch-time elevation; the launcher starts `MW4x.exe` directly as the current user.

## DDrawCompat-MW3

Vengeance and Mercenaries use a source-built, narrowly patched revision of the project's pinned MechWarrior 3 DDrawCompat fork to preserve DirectDraw/Direct3D 7 behavior while scaling a 1024x768 fullscreen surface into a desktop-sized 4:3 borderless presentation. The wrapper does not switch the desktop into exclusive fullscreen, preserving normal Windows Alt-Tab behavior. The MW4 patch only retries transiently lost DirectDraw surface locks/unlocks in-process. Black Knight deliberately does not load this wrapper because its app-local PR1 compatibility runtime exits when a `ddraw.dll` proxy is present.

- Project fork: <https://github.com/Icehellionx/DDrawCompat-MW3>
- Exact tag: `v0.7.1-mw3-r18`
- Exact commit: `73ac0f47af16a1d28dcda25c3228053beb3eb5f4`
- Upstream base: DDrawCompat v0.7.1 commit `2c9a07f`
- License: BSD Zero Clause License (0BSD)
- Local patch SHA-256: `ad88d82ae9ec03be4a85c7c08ae3dd1e3c9c47fc88821dafdbb2460a42e44d98`
- Qualified DLL SHA-256: `b589c27402c283f699857aec26948b33595ee93f645891ec4f9607254148b509`
- Installed license, local patch, and exact corresponding source are under `Compatibility/DDrawCompat`.
