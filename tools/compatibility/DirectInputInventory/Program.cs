using System.Runtime.InteropServices;
using System.Text.Json;

// Read-only DirectInput 8 enumeration. The game-local dinputto8 adapter forwards to this API.
var architecture = Environment.Is64BitProcess ? "x64" : "x86";
if (args.Any(argument => argument is not ("--all" or "--include-hidden")) || args.Distinct().Count() != args.Length)
    throw new ArgumentException("Usage: DirectInputInventory [--all] [--include-hidden]");

var deviceClass = args.Contains("--all") ? 0u : 4u; // DI8DEVCLASS_ALL or DI8DEVCLASS_GAMECTRL
var flags = args.Contains("--include-hidden") ? 0x00060000u : 0u; // DIEDFL_INCLUDEHIDDEN | INCLUDEPHANTOMS

var devices = new List<DeviceRecord>();
var iid = new Guid("BF798031-483A-4DA2-AA99-5D64ED369700"); // IID_IDirectInput8W
var result = Native.DirectInput8Create(Native.GetModuleHandle(null), 0x0800, ref iid, out var directInput, IntPtr.Zero);
if (result < 0 || directInput == IntPtr.Zero)
    throw new InvalidOperationException($"DirectInput8Create failed: 0x{result:X8}");

try
{
    var enumerate = Native.Method<Native.EnumDevices>(directInput, 4);
    Native.EnumCallback callback = (instance, _) =>
    {
        var guid = Marshal.PtrToStructure<Guid>(IntPtr.Add(instance, 4));
        var product = Marshal.PtrToStructure<Guid>(IntPtr.Add(instance, 20));
        var name = Marshal.PtrToStringUni(IntPtr.Add(instance, 40), 260)?.TrimEnd('\0') ?? "";
        var productName = Marshal.PtrToStringUni(IntPtr.Add(instance, 560), 260)?.TrimEnd('\0') ?? "";
        var create = Native.Method<Native.CreateDevice>(directInput, 3);
        var deviceResult = create(directInput, ref guid, out var device, IntPtr.Zero);
        DeviceCapabilities? capabilities = null;
        try
        {
            if (deviceResult >= 0 && device != IntPtr.Zero)
            {
                var caps = new Native.DiDevCaps { Size = (uint)Marshal.SizeOf<Native.DiDevCaps>() };
                var capsResult = Native.Method<Native.GetCapabilities>(device, 3)(device, ref caps);
                if (capsResult >= 0)
                    capabilities = new DeviceCapabilities(caps.Axes, caps.Buttons, caps.Povs,
                        (caps.Flags & 0x00000100) != 0); // DIDC_FORCEFEEDBACK
                else
                    deviceResult = capsResult;
            }
        }
        finally
        {
            if (device != IntPtr.Zero) Native.Method<Native.Release>(device, 2)(device);
        }

        devices.Add(new DeviceRecord(devices.Count, name, productName, guid, product,
            capabilities, deviceResult < 0 ? $"0x{deviceResult:X8}" : null));
        return 1; // DIENUM_CONTINUE
    };

    result = enumerate(directInput, deviceClass, callback, IntPtr.Zero, flags);
    GC.KeepAlive(callback);
    if (result < 0)
        throw new InvalidOperationException($"EnumDevices failed: 0x{result:X8}");
}
finally
{
    Native.Method<Native.Release>(directInput, 2)(directInput);
}

Console.WriteLine(JsonSerializer.Serialize(new InventoryReport("DirectInput8W", architecture, deviceClass == 4 ? "game controllers" : "all devices", flags, devices),
    new JsonSerializerOptions { WriteIndented = true }));

internal record InventoryReport(string Api, string Architecture, string DeviceClass, uint Flags, IReadOnlyList<DeviceRecord> Devices);
internal record DeviceRecord(int Order, string InstanceName, string ProductName, Guid InstanceGuid,
    Guid ProductGuid, DeviceCapabilities? Capabilities, string? Error);
internal record DeviceCapabilities(uint Axes, uint Buttons, uint Povs, bool ForceFeedback);

internal static class Native
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern IntPtr GetModuleHandle(string? moduleName);

    [DllImport("dinput8.dll", ExactSpelling = true)]
    internal static extern int DirectInput8Create(IntPtr instance, uint version, ref Guid iid,
        out IntPtr directInput, IntPtr outer);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate uint Release(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int CreateDevice(IntPtr self, ref Guid guid, out IntPtr device, IntPtr outer);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int EnumDevices(IntPtr self, uint type, EnumCallback callback, IntPtr context, uint flags);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int EnumCallback(IntPtr instance, IntPtr context);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int GetCapabilities(IntPtr self, ref DiDevCaps caps);

    [StructLayout(LayoutKind.Sequential)]
    internal struct DiDevCaps
    {
        internal uint Size, Flags, DeviceType, Axes, Buttons, Povs, ForceFeedbackSamplePeriod,
            ForceFeedbackMinTimeResolution, FirmwareRevision, HardwareRevision, ForceFeedbackDriverVersion;
    }

    internal static T Method<T>(IntPtr comObject, int slot) where T : Delegate
    {
        var vtable = Marshal.ReadIntPtr(comObject);
        return Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(vtable, slot * IntPtr.Size));
    }
}
