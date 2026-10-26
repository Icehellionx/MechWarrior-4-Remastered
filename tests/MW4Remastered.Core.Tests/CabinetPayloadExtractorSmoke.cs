using System.Text;
using MW4Remastered.Core.Install;

internal static class CabinetPayloadExtractorSmoke
{
    internal static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-cab-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var good = Path.Combine(root, "good.cab");
            WriteCabinet(good, ("GAME/RESOURCE/core.mw4", "core"), ("GAME/RESOURCE/MAPS/test.mw4", "map"));
            var output = Path.Combine(root, "output");
            var extracted = new CabinetPayloadExtractor().ExtractGamePayload(good, output);
            Check(extracted.Count == 2 && File.ReadAllText(Path.Combine(output, "RESOURCE/core.mw4")) == "core" &&
                File.ReadAllText(Path.Combine(output, "RESOURCE/MAPS/test.mw4")) == "map", "real CAB files preserve nested GAME payload contents");

            foreach (var name in new[] { "SETUP.EXE", "GAME/../escape.bin", "GAME/C:/escape.bin", "GAME/RESOURCE/NUL.mw4", "GAME/RESOURCE/trailing. ", "GAME/RESOURCE/COM1.mw4" })
            {
                var bad = Path.Combine(root, "unsafe.cab");
                WriteCabinet(bad, ("GAME/first.bin", "first"), (name, "unsafe"));
                CheckRejected(bad, "unsafe-output", "unsafe CAB destination rejected before any extraction: " + name);
            }
            // SetupAPI decodes CAB names using the host code page. Test the
            // Unicode device-name policy directly, independent of that code page.
            Check(WindowsCabinetReader.IsWindowsAlias("COM¹.mw4") && WindowsCabinetReader.IsWindowsAlias("LPT².bin") &&
                WindowsCabinetReader.IsWindowsAlias("COM³"), "superscript Windows device aliases are rejected");

            var duplicate = Path.Combine(root, "duplicate.cab");
            WriteCabinet(duplicate, ("GAME/A.bin", "first"), ("GAME/a.bin", "second"));
            CheckRejected(duplicate, "duplicate-output", "case-insensitive duplicate CAB destinations rejected before writing");

            var corrupt = Path.Combine(root, "corrupt.cab");
            WriteCabinet(corrupt, true, ("GAME/first.bin", "first"), ("GAME/second.bin", "second"));
            using (var file = new FileStream(corrupt, FileMode.Open, FileAccess.Write)) file.SetLength(file.Length - 3);
            CheckRejected(corrupt, "corrupt-output", "CAB truncation after a read-only first entry leaves no output or staging");

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var cancelled = false;
            try { new CabinetPayloadExtractor().ExtractGamePayload(good, Path.Combine(root, "cancelled-output"), cancellation.Token); }
            catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled && !Directory.Exists(Path.Combine(root, "cancelled-output")), "CAB cancellation leaves no output");

            var existingRejected = false;
            try { new CabinetPayloadExtractor().ExtractGamePayload(good, output); }
            catch (IOException) { existingRejected = true; }
            Check(existingRejected && File.ReadAllText(Path.Combine(output, "RESOURCE/core.mw4")) == "core", "existing CAB output is preserved");

            void CheckRejected(string cabinet, string destinationName, string assertion)
            {
                var rejected = false;
                var destination = Path.Combine(root, destinationName);
                try { new CabinetPayloadExtractor().ExtractGamePayload(cabinet, destination); }
                catch (InvalidDataException) { rejected = true; }
                Check(rejected && !Directory.Exists(destination) && !Directory.EnumerateDirectories(root, ".cab-staging-*").Any(), assertion);
            }
            void Check(bool condition, string assertion)
            {
                if (!condition) failures.Add("FAIL: " + assertion);
            }
        }
        finally { Directory.Delete(root, true); }
    }

    // Synthetic MS-CAB: one uncompressed folder, one CFDATA block per file; no game data.
    // Unlike ZIPs renamed .cab, this exercises the native CAB reader and callbacks.
    internal static void WriteCabinet(string path, params (string Name, string Contents)[] entries)
        => WriteCabinet(path, false, entries);

    private static void WriteCabinet(string path, bool readOnlyFirst, params (string Name, string Contents)[] entries)
    {
        var names = entries.Select(entry => Encoding.UTF8.GetBytes(entry.Name + '\0')).ToArray();
        var payloads = entries.Select(entry => Encoding.UTF8.GetBytes(entry.Contents)).ToArray();
        var dataOffset = 36 + 8 + names.Sum(name => 16 + name.Length);
        var dataSize = payloads.Sum(payload => payload.Length);
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes("MSCF"));
        writer.Write(0u); writer.Write((uint)(dataOffset + (8 * entries.Length) + dataSize)); writer.Write(0u);
        writer.Write(44u); writer.Write(0u);
        writer.Write((byte)3); writer.Write((byte)1); // CAB version 1.3.
        writer.Write((ushort)1); writer.Write((ushort)entries.Length);
        writer.Write((ushort)0); writer.Write((ushort)0); writer.Write((ushort)0);
        writer.Write((uint)dataOffset); writer.Write((ushort)entries.Length); writer.Write((ushort)0);
        var offset = 0u;
        for (var index = 0; index < entries.Length; index++)
        {
            writer.Write((uint)payloads[index].Length); writer.Write(offset);
            writer.Write((ushort)0); writer.Write((ushort)0x5821); writer.Write((ushort)0);
            writer.Write((ushort)(readOnlyFirst && index == 0 ? 0xA1 : 0xA0)); // Archive, UTF-8, optionally ReadOnly.
            writer.Write(names[index]); offset += (uint)payloads[index].Length;
        }
        foreach (var payload in payloads)
        {
            writer.Write(0u); writer.Write((ushort)payload.Length); writer.Write((ushort)payload.Length);
            writer.Write(payload);
        }
    }
}
