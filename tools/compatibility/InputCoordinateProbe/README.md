# MW4 input coordinate probe

Build with `dotnet build tools/compatibility/InputCoordinateProbe/InputCoordinateProbe.csproj -c Release`. Start exactly one of the three retail games, enter the affected mission, then run the built probe executable. It samples for 15 seconds by default; pass a duration of 1–60 seconds as its sole argument if needed. Each JSON report is written to `%TEMP%\MW4-InputCoordinateProbe-<PID>-<UTC timestamp>.json`, so consecutive captures remain available for comparison.

The probe reads only the visible MW4 window's outer/client rectangles, monitor rectangle, foreground state, OS cursor position/clip, and window DPI awareness. It does not send input, attach hooks, alter configuration, or stop the game. Run once before the mission and again while the torso is locked, comparing cursor movement and clip geometry. It cannot read the game's internal torso value, so it must be paired with an observed mission result. Review the report before sharing; it contains screen coordinates, process ID, and monitor dimensions but no paths, media, serials, or screenshots.

The coordinate fields follow Microsoft's [`GetClipCursor`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipcursor) and [DPI-awareness](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowdpiawarenesscontext) contracts. No third-party input code is included.

Component smoke on the development desktop: a temporary synthetic process named `MW4Mercs.exe` exposed a monitor-sized window for 25 seconds. A ten-second probe request captured 92 complete snapshots with a 2560x1440 monitor, 96 DPI, and all samples foreground. This establishes the read-only sampling path, not game input behavior.
