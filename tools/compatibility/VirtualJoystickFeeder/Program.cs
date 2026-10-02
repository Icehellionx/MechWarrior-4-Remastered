using System.Runtime.InteropServices;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    Console.WriteLine("VirtualJoystickFeeder status|axis <x|y|z|rx|ry|rz> [milliseconds]|button <1-32> [milliseconds]|pov <0-35999> [milliseconds] [--device <1-16>]");
    return;
}

try
{
    var deviceOption = Array.IndexOf(args, "--device");
    uint device = 1;
    if (deviceOption >= 0)
    {
        if (deviceOption + 1 >= args.Length || !uint.TryParse(args[deviceOption + 1], out device) || device is < 1 or > 16)
            throw new ArgumentException("--device needs an ID from 1 to 16.");
    }

    var command = args[0].ToLowerInvariant();
    if (command == "status")
    {
        Console.WriteLine($"Device {device} status: {Native.GetVJDStatus(device)} (0=owned, 1=free, 2=busy, 3=missing, 4=unknown)");
        for (uint usage = 0x30; usage <= 0x38; ++usage)
            Console.WriteLine($"Axis 0x{usage:X} presence: {Native.GetVJDAxisExist(device, usage)} (1=present)");
        return;
    }

    if (args.Length < 2)
        throw new ArgumentException("An axis, button, or POV value is required.");

    int durationMs = 1500;
    if (args.Length > 2 && args[2] != "--device")
    {
        if (!int.TryParse(args[2], out durationMs) || durationMs is < 100 or > 30000)
            throw new ArgumentException("Duration must be 100 to 30000 milliseconds.");
    }

    var axes = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = 0x30, ["y"] = 0x31, ["z"] = 0x32,
        ["rx"] = 0x33, ["ry"] = 0x34, ["rz"] = 0x35
    };
    uint axis = 0;
    byte button = 0;
    uint pov = 0;
    switch (command)
    {
        case "axis" when !axes.TryGetValue(args[1], out axis):
            throw new ArgumentException("Axis must be x, y, z, rx, ry, or rz.");
        case "button" when !byte.TryParse(args[1], out button) || button is < 1 or > 32:
            throw new ArgumentException("Button must be 1 to 32.");
        case "pov" when !uint.TryParse(args[1], out pov) || pov > 35999:
            throw new ArgumentException("POV must be 0 to 35999 hundredths of a degree.");
        case "axis" or "button" or "pov":
            break;
        default:
            throw new ArgumentException($"Unknown command: {command}.");
    }

    if (!Native.AcquireVJD(device))
        throw new InvalidOperationException($"Could not acquire vJoy device {device}; status {Native.GetVJDStatus(device)}.");

    try
    {
        Neutralize(device);

        switch (command)
        {
            case "axis":
                if (!Native.GetVJDAxisMin(device, axis, out var min) || !Native.GetVJDAxisMax(device, axis, out var max))
                    throw new InvalidOperationException($"Axis {args[1]} is unavailable on device {device}.");
                var center = min + (max - min) / 2;
                if (!Native.SetAxis(min, device, axis))
                    throw new InvalidOperationException("Could not set the axis minimum.");
                Console.WriteLine($"Device {device} axis {args[1]} = {min} for {durationMs} ms (range {min}..{max}).");
                Thread.Sleep(durationMs);
                if (!Native.SetAxis(max, device, axis))
                    throw new InvalidOperationException("Could not set the axis maximum.");
                Console.WriteLine($"Device {device} axis {args[1]} = {max} for {durationMs} ms.");
                Thread.Sleep(durationMs);
                if (!Native.SetAxis(center, device, axis))
                    throw new InvalidOperationException("Could not center the pulsed axis.");
                break;
            case "button":
                if (!Native.SetBtn(true, device, button))
                    throw new InvalidOperationException($"Could not press button {button}.");
                Console.WriteLine($"Device {device} button {button} pressed for {durationMs} ms.");
                Thread.Sleep(durationMs);
                if (!Native.SetBtn(false, device, button))
                    throw new InvalidOperationException("Could not release the pulsed button.");
                break;
            case "pov":
                if (!Native.SetContPov(pov, device, 1))
                    throw new InvalidOperationException("Could not set continuous POV 1.");
                Console.WriteLine($"Device {device} POV 1 = {pov} for {durationMs} ms.");
                Thread.Sleep(durationMs);
                if (!Native.SetContPov(uint.MaxValue, device, 1))
                    throw new InvalidOperationException("Could not neutralize the pulsed POV.");
                break;
        }
    }
    finally
    {
        try { Neutralize(device); }
        finally { Native.RelinquishVJD(device); }
    }
}
catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
{
    Console.Error.WriteLine(exception.Message);
    Environment.ExitCode = 1;
}

// ResetVJD uses driver-configured defaults, which need not be centered. Keep
// unpulsed axes neutral so a button test cannot also apply full-axis input.
static void Neutralize(uint device)
{
    if (!Native.ResetVJD(device))
        throw new InvalidOperationException("Could not reset the vJoy device.");
    for (uint usage = 0x30; usage <= 0x38; ++usage)
    {
        // The upstream C# wrapper accepts exactly 1, not every nonzero result.
        if (Native.GetVJDAxisExist(device, usage) != 1) continue;
        if (!Native.GetVJDAxisMin(device, usage, out var min) ||
            !Native.GetVJDAxisMax(device, usage, out var max) ||
            !Native.SetAxis(min + (max - min) / 2, device, usage))
            throw new InvalidOperationException($"Could not center axis 0x{usage:X}.");
    }
    if (!Native.ResetPovs(device))
        throw new InvalidOperationException("Could not neutralize POV hats.");
}

internal static class Native
{
    private const string Library = "vJoyInterface.dll";

    static Native()
    {
        NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, (name, _, _) =>
        {
            if (name != Library)
                return IntPtr.Zero;
            var installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "vJoy", "x64", Library);
            return File.Exists(installed) ? NativeLibrary.Load(installed) : IntPtr.Zero;
        });
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AcquireVJD(uint device);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RelinquishVJD(uint device);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int GetVJDStatus(uint device);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ResetVJD(uint device);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ResetPovs(uint device);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint GetVJDAxisExist(uint device, uint axis);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetVJDAxisMin(uint device, uint axis, out int minimum);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetVJDAxisMax(uint device, uint axis, out int maximum);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetAxis(int value, uint device, uint axis);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetBtn([MarshalAs(UnmanagedType.Bool)] bool pressed, uint device, byte button);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetContPov(uint value, uint device, byte pov);
}
