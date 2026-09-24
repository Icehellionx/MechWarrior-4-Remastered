using System.Runtime.InteropServices;
using System.Security.Cryptography;

// A read-only x86 API probe. Run with a full path to the exact game-local
// DDraw.dll, or with "system" to test the Windows implementation. No game
// executable, media, registry entry, or installation is required.
if (args.Length is < 1 or > 2 ||
    (args.Length == 2 && args[1] != "surface") ||
    (args[0] != "system" && !Path.IsPathFullyQualified(args[0])))
{
    Console.Error.WriteLine("Usage: DirectDrawProbe system|<absolute path to DDraw.dll> [surface]");
    return 2;
}

var path = args[0] == "system"
    ? Path.Combine(Environment.SystemDirectory, "ddraw.dll")
    : Path.GetFullPath(args[0]);
if (!File.Exists(path))
{
    Console.Error.WriteLine("Selected DirectDraw DLL is missing.");
    return 2;
}

Console.WriteLine($"Process architecture: {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"DLL SHA-256: {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))}");
if (RuntimeInformation.ProcessArchitecture != Architecture.X86)
{
    Console.Error.WriteLine("This probe must run as x86, matching MechWarrior 4.");
    return 2;
}

IntPtr library = IntPtr.Zero;
IntPtr directDraw = IntPtr.Zero;
IntPtr surface = IntPtr.Zero;
IntPtr window = IntPtr.Zero;
IntPtr surfaceDescription = IntPtr.Zero;
try
{
    library = NativeLibrary.Load(path);
    var enumerate = Marshal.GetDelegateForFunctionPointer<DirectDrawEnumerate>(
        NativeLibrary.GetExport(library, "DirectDrawEnumerateA"));
    var create = Marshal.GetDelegateForFunctionPointer<DirectDrawCreateEx>(
        NativeLibrary.GetExport(library, "DirectDrawCreateEx"));
    var devices = 0;
    EnumCallback callback = (_, description, name, _) =>
    {
        devices++;
        Console.WriteLine($"Device {devices}: {Marshal.PtrToStringAnsi(description)} / {Marshal.PtrToStringAnsi(name)}");
        return 1;
    };
    var enumerationResult = enumerate(callback, IntPtr.Zero);
    GC.KeepAlive(callback);
    Console.WriteLine($"DirectDrawEnumerateA: 0x{enumerationResult:X8}; devices={devices}");
    var iid = new Guid("15e65ec0-3b9c-11d2-b92f-00609797ea5b");
    var createResult = create(IntPtr.Zero, out directDraw, ref iid, IntPtr.Zero);
    Console.WriteLine($"DirectDrawCreateEx(NULL, IID_IDirectDraw7): 0x{createResult:X8}; object={(directDraw == IntPtr.Zero ? "null" : "created")}");
    if (enumerationResult != 0 || createResult != 0 || directDraw == IntPtr.Zero)
        return 1;

    if (args.Length == 1)
        return 0;

    // IDirectDraw7 method order and the x86 DDSURFACEDESC2 layout come from
    // the Windows SDK ddraw.h. Keep this optional: it creates a hidden probe
    // window/primary surface but never changes display mode or game files.
    window = CreateWindowExW(0, "STATIC", "DirectDraw probe", 0,
        0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
    if (window == IntPtr.Zero)
    {
        Console.Error.WriteLine($"CreateWindowExW failed: {Marshal.GetLastWin32Error()}");
        return 1;
    }

    var vtable = Marshal.ReadIntPtr(directDraw);
    var setCooperativeLevel = Marshal.GetDelegateForFunctionPointer<SetCooperativeLevel>(
        Marshal.ReadIntPtr(vtable, 20 * IntPtr.Size));
    const int ddsclNormal = 0x8;
    var cooperativeResult = setCooperativeLevel(directDraw, window, ddsclNormal);
    Console.WriteLine($"IDirectDraw7::SetCooperativeLevel(DDSCL_NORMAL): 0x{cooperativeResult:X8}");
    if (cooperativeResult != 0)
        return 1;

    const int descriptionSize = 124; // sizeof(DDSURFACEDESC2) on x86
    surfaceDescription = Marshal.AllocHGlobal(descriptionSize);
    Marshal.Copy(new byte[descriptionSize], 0, surfaceDescription, descriptionSize);
    Marshal.WriteInt32(surfaceDescription, 0, descriptionSize);
    Marshal.WriteInt32(surfaceDescription, 4, 0x1); // DDSD_CAPS
    Marshal.WriteInt32(surfaceDescription, 104, 0x200); // ddsCaps.dwCaps = DDSCAPS_PRIMARYSURFACE
    var createSurface = Marshal.GetDelegateForFunctionPointer<CreateSurface>(
        Marshal.ReadIntPtr(vtable, 6 * IntPtr.Size));
    var surfaceResult = createSurface(directDraw, surfaceDescription, out surface, IntPtr.Zero);
    Console.WriteLine($"IDirectDraw7::CreateSurface(primary): 0x{surfaceResult:X8}; object={(surface == IntPtr.Zero ? "null" : "created")}");
    return surfaceResult == 0 && surface != IntPtr.Zero ? 0 : 1;
}
catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException or SEHException)
{
    Console.Error.WriteLine($"Probe failed before a DirectDraw result: {error.GetType().Name}: {error.Message}");
    return 1;
}
finally
{
    if (surface != IntPtr.Zero) Marshal.Release(surface);
    if (surfaceDescription != IntPtr.Zero) Marshal.FreeHGlobal(surfaceDescription);
    if (directDraw != IntPtr.Zero) Marshal.Release(directDraw);
    if (window != IntPtr.Zero) DestroyWindow(window);
    if (library != IntPtr.Zero) NativeLibrary.Free(library);
}

[DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
static extern IntPtr CreateWindowExW(int extendedStyle, string className, string windowName,
    int style, int x, int y, int width, int height, IntPtr parent, IntPtr menu,
    IntPtr instance, IntPtr parameter);

[DllImport("user32.dll", ExactSpelling = true)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool DestroyWindow(IntPtr window);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
delegate int DirectDrawEnumerate(EnumCallback callback, IntPtr context);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
delegate int EnumCallback(IntPtr guid, IntPtr description, IntPtr name, IntPtr context);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
delegate int DirectDrawCreateEx(IntPtr guid, out IntPtr directDraw, ref Guid iid, IntPtr outer);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
delegate int SetCooperativeLevel(IntPtr directDraw, IntPtr window, int flags);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
delegate int CreateSurface(IntPtr directDraw, IntPtr description, out IntPtr surface, IntPtr outer);
