# MechWarrior 4 compatibility ecosystem review

Updated: 2026-09-16. This is the decision gate requested after field testing disproved the current release candidate. Community material is evidence and a source of test oracles; it is not automatically a distributable dependency.

## Trigger and corrected baseline

- Setup `0.5.7` is invalid. It fails at startup with Inno Setup `Runtime error (at 36:461): Could not call proc.` The script creates `LicensePage` only from the Add Media click handler but dereferences the page from global navigation logic. That packaging defect is independent of MW4 runtime compatibility.
- Earlier “responsive process/window plus empty log” smokes were false-positive gates. The user's field run showed that the legacy `STOP: MechWarrior 4 has been incorrectly installed` dialog can remain responsive and produce an empty log. No title is currently launch-qualified.
- The shared forced launch profile is unqualified. It applies the same OpenGL, windowed, movie, joystick, resolution, and frame-limit switches to all three titles before a clean default baseline exists.

## Established implementations inspected

### Lutris / MyLittleLutrisScripts

Pinned recipe revision: `legluondunet/MyLittleLutrisScripts@12fe26723b80eb58383336db7ab6ac91345cf697`.

The recipes establish a useful topology and sequence:

1. Install retail data through the original setup contract.
2. Install Vengeance Patch 3, Black Knight Point Release 1, or Mercenaries Point Release 1.
3. Keep Black Knight in the Vengeance prefix/tree, with its executable and configuration in the `MW4X` subdirectory.
4. Add a graphics compatibility layer separately; current recipes use dgVoodoo2 and an older recipe uses dxwrapper.
5. Configure Vengeance/Mercenaries and Black Knight separately.

This is an oracle, not a product dependency. The repository has no detected root license, and the inspected archives are not clean:

- `mw4_bk_pr1_patch.7z`, SHA-256 `8791a361ee38ef83cbd18c9d758772cd0319ef804653e6bbf9cef3585d1128d1`, contains an opaque replacement executable, `Cdac14ba.dll`, a crack note, `cabarc.exe`, and xdelta payloads.
- `mw4m_patch_pr1.7z`, SHA-256 `3f2f9a0566408d126585ef944949002bca4886ad5124644f5c7a3de5073169ba`, contains a replacement no-CD executable, scene NFO, xdelta payloads, and an `xdelta3` binary.
- The live Black Knight recipe itself reports mouse lag, an Instant Action crash, and an original-installer patch failure. It is not release qualification evidence.

### Official point releases already present on user media

The local Inner Sphere and Clan discs contain both official Vengeance Patch 3 and Black Knight Point Release 1 RTP payloads and patch engines. The current product applies Vengeance Patch 3 but does not reproduce Black Knight PR1. The supplied Mercenaries fix archive contains the official-looking `mercpr1.exe` beside a separate no-CD directory; those components must be inventoried and separated before use.

Exact local official inputs are now separated from the surrounding community archives:

- Black Knight PR1 RTP: 43,865,979 bytes, SHA-256 `5f3b6a383b7c1821e9f682ea3d6675818c23bf0138289e89a951256ecf2ab6ff`.
- Mercenaries `mercpr1.exe`: 5,376,000 bytes, SHA-256 `0c3d0094448e6fe5d2a30fb9ebb24001e8e8c03b39aff9232856bd30000efb30`. Its self-extracting contents are the Microsoft PR1 readme, `MW4MERCS.RTP` (SHA-256 `d3ebf1c2dc098a8e55d7cbeb112426fb413dfd23595a1ed078cd35fe7a551a65`), and the same patch engine family already used for Patch 3. The separate `NOCD` folder is not part of that self-extractor.

Contained reference applications now prove both update contracts without running their legacy setup front ends:

- Mercenaries PR1 applied 11 file operations with no missing or invalid inputs. The protected image became `MW4MERCS.ICD` version `50.07.01.2105`, 3,915,897 bytes, SHA-256 `aafff190e08119ff3ac86b0628813315cde05de46c0e36840ac3eba986a7d550`. The historical PR1 no-CD comparison executable reports the same game version, whereas the current custom build is still retail `50.06.09.3002`.
- Black Knight PR1 applied cleanly over Vengeance Patch 3 plus the expansion in its original shared layout. It produced `MW4X\MW4x.exe` version `45.30.04.1908`, 4,735,573 bytes, SHA-256 `be9c15731b2ab59471f35add8df4935ea65355cbb4672bc48b63295ad83632b0`, along with the expansion resource and language updates. The historical replacement executable previously used as an oracle is only retail `45.05.10.0701`.

This is the largest architecture gap found by the review: all three community installation paths establish the official point-release state before compatibility work, while the current custom path does not do so consistently.

Static inspection of the original setup tables independently confirms another gap. Vengeance writes its product and DirectPlay records under 32-bit HKLM. Black Knight keeps `AutoConfigx.exe`, `MissionLangx.dll`, and other expansion-named files in the Vengeance root but launches `%APPPATH%\MW4X\MW4X.exe`; Microsoft KB 325734 documents that same subdirectory. The current builder instead creates a separate Black Knight product tree and flattens the `MW4X` runtime files beside the executable. Mercenaries likewise defines a 32-bit HKLM product record plus DirectPlay application values. The current HKCU-only registration is therefore an experiment, not installer parity.

The static resources also expose a semantic mismatch in the current experiment. Black Knight's prerequisite check reads Vengeance's machine-wide `EXE Path` value and separately tests `MW4.exe`, strongly indicating that the value denotes an installation directory rather than the full executable path currently written by `LegacyGameRegistration`. That class also invents a Black Knight `CDPath` of `L:\`. These values are suspended: only a controlled original-installer registry diff may establish their exact paths, value kinds, and ownership policy.

### SafeDiscShim

`RibShark/SafeDiscShim` is GPL-3.0-or-later with a specific section-7 permission for combining with SafeDisc. It replaces the blocked `secdrv.sys` interaction without installing the driver, but deliberately retains the original-disc requirement. Its open MW4 issue reports a remaining `drvmgt.dll` interaction. It is a useful SafeDisc behavior oracle, not a route to the ISO-only/no-mounted-disc product contract.

### dxwrapper and dgVoodoo2

dxwrapper is source-available under a permissive project license with separately documented bundled-component licenses, explicitly lists MechWarrior 4 as compatible, and owns graphics/input/timing concerns rather than installation or DRM. dgVoodoo2 is used by the current Lutris recipes but has different redistribution terms and no inspected MW4-specific source path.

Neither wrapper belongs in the product until a clean, correctly installed, officially patched title reaches menus/gameplay without it and a wrapper-specific A/B test demonstrates a remaining renderer/input defect. A renderer wrapper cannot cure an “incorrectly installed” setup-state failure.

### dinputto8 and recent community practice

The maintained `elishacloud/dinputto8` project is source-available under the zlib license and translates DirectInput 1-7 calls to DirectInput 8. A 2024/2025 MW4 community guide places its `dinput.dll` beside each title executable, explicitly including `Vengeance\MW4X` for Black Knight, and treats it as the input/configuration fix before optional joystick remapping. This independently reinforces the shared-tree topology and gives the project a narrower input-layer candidate than a combined graphics wrapper.

It remains an A/B-test candidate, not a default payload. It cannot fix registry, patch-level, EULA, or disc-check failures, and its release binary must not be adopted without a pinned source/license record, reproducible-build or exact-upstream-artifact policy, Defender evidence, and a demonstrated failure that disappears with the wrapper.

### Microsoft Mech Pak support evidence

Archived Microsoft KB 325999 describes missing logos/content as a SafeCast entitlement/repair problem and says Black Knight installation order can invalidate pack visibility. Local media inspection independently shows ordinary resource files plus obsolete C-Dilla/SafeCast components, including a reference to `CDILLA16.EXE`. The assets are not 16-bit encrypted.

The product may retain exact resource extraction and rollback-safe overlay mechanics. It must separately reproduce and test the official patch-level and entitlement/visibility effects; file presence alone is not success.

## Architecture decision for the next iteration

### Retain

- Exact-hash media recognition, ISO/ZIP containment, read-only mount ownership, atomic staging, manifests, rollback-safe uninstall, release allowlists, and Defender gates.
- Source-owned exact-input transforms as experiments, but do not call them qualified until compared against a known-good official installation and exercised beyond the first window.
- The single visible installer and compact launcher UX goals.

### Suspend

- Any new end-user installer build.
- Claims that Vengeance, Black Knight, Mercenaries, or either Mech Pak is launch-qualified.
- The separate self-contained Black Knight product tree as the default architecture. First reproduce the original shared Vengeance/`MW4X` topology; isolation can be reconsidered only after parity is proven.
- The shared aggressive command-line profile. Establish per-title minimal baselines first.
- Addition of a graphics wrapper before installation-state parity.

### Reject as distributable inputs

- Opaque no-CD executables, scene patch archives, crack notes, C-Dilla/SafeCast executables, `secdrv.sys`, injectors, and unlicensed community bundles.
- Antivirus exclusions, disabled Windows security, or launch-time elevation.

## Falsification sequence

Each stage must fail or pass independently; no later stage may mask an earlier one.

1. **Reference install:** On a disposable path, record original-installer file manifest and HKCU/HKLM 32-bit registry state for retail Vengeance; add Black Knight into the same tree; install retail Mercenaries independently. Do not launch.
2. **Official patch state:** Apply Vengeance Patch 3, Black Knight PR1, and Mercenaries PR1 from exact validated inputs. Diff files and registry against the pre-patch snapshots.
3. **Custom staging parity:** Compare the source-owned staging output against those known-good manifests. Explain every missing, extra, renamed, or version-different file and registry value.
4. **Minimal launch:** Use each title's documented working directory and only its correct autoconfig bypass if needed (`-noautoconfig` versus Black Knight `-noautoconfigx`). No wrapper and no shared bundle of switches.
5. **Setup-state proof:** Treat any EULA, autoconfig, incorrect-install, CD request, or error dialog as failure. Capture window class/title/text, process exit, and registry/file traces; a responsive process is insufficient.
6. **Disc-check layer:** After setup-state parity, A/B test the exact media-derived transform against SafeDiscShim plus original media and against historical replacement executables as local oracles. Do not redistribute the latter.
7. **Renderer/input layer:** Only after main menu and Instant Action load, A/B test native rendering, dxwrapper, and if licensing permits dgVoodoo2. Record menus, mission load, alt-tab, fullscreen/windowed, mouse, joystick, audio, movies, and frame pacing separately.
8. **Mech Pak entitlement:** Compare clean pre/post official SafeCast repair state in an isolated reference environment, then reproduce only the minimum entitlement effect and verify logos, chassis, variants, Instant Action, and Black Knight install-order behavior.
9. **Package gate:** A compiled setup must open, collect media, install, reach all selected title menus and one mission, uninstall owned files while preserving saves, and pass final-tree Defender scans before it is offered for field testing.

## Auxiliary review disposition

Local adviser (`qwen3:8b`) and adversary (`mistral:7b-instruct`) calls were run with a sanitized project-only prompt. Remote Featherless calls were not made because the external-data approval gate rejected transmission without separate informed approval.

- **Accepted:** split installation/patch, DRM, renderer, registry/arguments, and gameplay tests; reject opaque community patch archives; test without wrappers first; stop treating process responsiveness as success.
- **Partially accepted:** evaluate dxwrapper only after a native baseline. Its open-source status is not itself qualification.
- **Rejected:** the adviser's suggestion to stop exact-media transforms merely because SafeCast exists; SafeCast is a Mech Pak entitlement layer, not the complete disc-free contract. The adversary's unsupported endorsement of separate Black Knight trees also conflicts with original and community topology.
- **Unsubstantiated:** legal conclusions from either model. Licensing decisions remain source-based and independently reviewed.

## Stop conditions

- Do not publish or hand the user another setup while any title can reach an incorrect-install or EULA/autoconfig gate.
- Do not add a wrapper to conceal missing official patch or registry state.
- Do not generalize from one media revision without exact input hashes and a clear unsupported-media failure.
- Do not call a title qualified until a menu and gameplay path pass, not merely a timed process check.
