# MechWarrior 4 Remastered v0.6.40

This maintenance release fixes a clean-install failure discovered immediately after the first public package was downloaded and tested.

## Fixed

- Black Knight Point Release 1 setup no longer treats the short-lived protected launcher's exit code as the result of its temporary capture child.
- Setup keeps that child inside the existing kill-on-close job and waits up to 60 seconds for the exact completed mapped image.
- Missing, still-open, truncated, or incorrectly sized capture output continues to fail closed.

All gameplay presentation, Mech Pak activation, maximum-quality defaults, bundled manuals, media selection, desktop-shortcut behavior, and launcher behavior are unchanged from v0.6.39.

## Verification

- Setup size: `174,458,775` bytes.
- SHA-256: `8756bf70879964ddd45e923595ce9a254a8fc7c4a8a507431991be59e1e62383`.
- Two clean package builds were byte-identical.
- The exact qualified Black Knight PR1 capture passed with all MW4 registration keys absent.
- Core smoke and all self-contained contract suites passed.
- The exact setup produced no new Microsoft Defender detection with engine `1.1.26080.3` and intelligence `1.459.273.0`.

## Known limitations

- The installer is unsigned and may display an Unknown publisher or SmartScreen reputation warning.
- Broader GPU, multi-monitor, regional-media, campaign, and multiplayer coverage remains welcome.
- Unrecognized or modified media fails closed.
