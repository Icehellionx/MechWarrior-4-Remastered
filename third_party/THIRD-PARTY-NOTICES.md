# Third-party notices

## SafeDiscLoader2

Black Knight uses a modified, source-built copy of SafeDiscLoader2 from commit `f27286a363aa675a0422141cb96fc8619cf8b9d8`. Setup exact-hash validates the DLL and places it beside the user-media `MW4x.exe`; Windows loads it through the normal app-local DLL search path. The project patch supports only the qualified retail and PR1 image layouts and replaces two obsolete setup/media checks after decryption.

- Upstream: <https://github.com/nckstwrt/SafeDiscLoader2>
- License: GPL-3.0-only
- Installed license: `Compatibility/BlackKnightRuntime/SafeDiscLoader2-LICENSE.txt`
- Exact corresponding source: `Compatibility/BlackKnightRuntime/SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip`
- Exact project patch: `Compatibility/BlackKnightRuntime/SafeDiscLoader2-MW4-BlackKnight-PR1-Runtime.patch`

The upstream elevated `VersionInjector` project is neither built nor distributed. There is no launch helper, process injection, service, driver, or launch-time elevation; the launcher starts `MW4x.exe` directly as the current user.
