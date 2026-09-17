# Third-party notices

## SafeDiscLoader2

Black Knight setup uses a modified, source-built copy of SafeDiscLoader2 from commit `f27286a363aa675a0422141cb96fc8619cf8b9d8` only inside the elevated installer transaction. It captures the already-decrypted PR1 process image so the project-owned static transform can produce the installed executable. The DLL and capture host are never part of normal game launch.

- Upstream: <https://github.com/nckstwrt/SafeDiscLoader2>
- License: GPL-3.0-only
- Installed license: `Compatibility/BlackKnightPr1Capture/SafeDiscLoader2-LICENSE.txt`
- Exact corresponding source: `Compatibility/BlackKnightPr1Capture/SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip`
- Exact project patch: `Compatibility/BlackKnightPr1Capture/SafeDiscLoader2-MW4-BlackKnight-PR1-Capture.patch`

The upstream elevated `VersionInjector` project is neither built nor distributed. The project-owned x86 capture host is `asInvoker`, accepts no target or DLL arguments, operates only on its adjacent `MW4x.exe`/capture DLL during setup, and is not invoked by the installed launcher.
