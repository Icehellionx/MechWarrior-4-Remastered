using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using MW4Remastered.Core.Install;

internal static class MechPakActivationTransformSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-mech-pak-activation-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var source = Path.Combine(root, "source");
            Directory.CreateDirectory(source);
            var archive = Path.Combine(source, "core.mw4");
            WriteArchive(archive);
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive))).ToLowerInvariant();
            var transform = new MechPakActivationTransform(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { hash });
            var plan = new InstallPlan("vengeance", new[]
            {
                new InstallFile(source, "core.mw4", "RESOURCE/core.mw4"),
            }, new[] { "vengeance", "clan" });

            var clanPlan = transform.TransformPlan(plan, new[] { "clan" }, Path.Combine(root, "clan"));
            var clanArchive = Resolve(clanPlan.Files.Single());
            Check(ReadFlags(clanArchive).All(flags => flags.Count(flag => flag == 0) == 5 &&
                flags.Count(flag => flag == 2) == 4 && flags.All(flag => flag is 0 or 2)),
                "Mech Pak activation unlocks only the selected Clan records", failures);
            Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive))).Equals(hash, StringComparison.OrdinalIgnoreCase),
                "Mech Pak activation produces a transformed archive rather than mutating source media", failures);

            var bothPlan = transform.TransformPlan(plan, new[] { "clan", "inner-sphere" }, Path.Combine(root, "both"));
            Check(ReadFlags(Resolve(bothPlan.Files.Single())).All(flags => flags.All(flag => flag == 0)),
                "Mech Pak activation unlocks all eight official records when both packs are selected", failures);

            var rejected = false;
            try
            {
                _ = new MechPakActivationTransform(new HashSet<string> { new string('0', 64) })
                    .TransformPlan(plan, new[] { "clan" }, Path.Combine(root, "rejected"));
            }
            catch (InvalidDataException exception)
            {
                rejected = exception.Message.Contains("Unsupported Mech Pak activation archive SHA-256", StringComparison.Ordinal);
            }
            Check(rejected, "Mech Pak activation rejects unqualified resource archive revisions", failures);

            var unknownPackRejected = false;
            try
            {
                _ = transform.TransformPlan(plan, new[] { "unknown-pack" }, Path.Combine(root, "unknown-pack"));
            }
            catch (InvalidDataException exception)
            {
                unknownPackRejected = exception.Message.Contains("Unsupported Mech Pak activation id", StringComparison.Ordinal);
            }
            Check(unknownPackRejected, "Mech Pak activation rejects unknown pack identifiers", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void WriteArchive(string path)
    {
        var tables = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [@"tables\mechtable.mpt"] = CreateTable(),
            [@"tables\mechchassistable.mpt"] = CreateTable(),
        };
        var directoryLength = 0x3c + tables.Sum(item => 23 + Encoding.ASCII.GetByteCount(item.Key));
        using var stream = new MemoryStream();
        stream.SetLength(directoryLength + 1 + tables.Sum(item => item.Value.Length));
        var buffer = stream.GetBuffer();
        Encoding.ASCII.GetBytes("#VBD").CopyTo(buffer, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(4, 4), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(12, 4), checked((uint)directoryLength));
        var rootName = Encoding.ASCII.GetBytes(@"resource\core.mw4");
        buffer[0x2a] = checked((byte)rootName.Length);
        rootName.CopyTo(buffer, 0x2b);

        var directoryOffset = 0x3c;
        var dataOffset = 1u;
        ushort archiveNumber = 1;
        foreach (var table in tables)
        {
            var name = Encoding.ASCII.GetBytes(table.Key);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(directoryOffset + 8, 4), checked((uint)table.Value.Length));
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(directoryOffset + 12, 4), checked((uint)table.Value.Length));
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(directoryOffset + 16, 4), dataOffset);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(directoryOffset + 20, 2), archiveNumber++);
            buffer[directoryOffset + 22] = checked((byte)name.Length);
            name.CopyTo(buffer, directoryOffset + 23);
            directoryOffset += 23 + name.Length;
            table.Value.CopyTo(buffer, directoryLength + checked((int)dataOffset));
            dataOffset += checked((uint)table.Value.Length);
        }
        File.WriteAllBytes(path, stream.ToArray());
    }

    private static byte[] CreateTable()
    {
        using var stream = new MemoryStream();
        var entries = new[]
        {
            ("Arctic Wolf", 1u), ("Cauldron-Born", 1u), ("Kodiak", 1u), ("Masakari", 1u),
            ("Dragon", 2u), ("Highlander", 2u), ("Hunchback", 2u), ("Zeus", 2u),
            ("Argus", 0u),
        };
        uint id = 1;
        var scalar = new byte[4];
        var metadata = new byte[8];
        foreach (var (name, flag) in entries)
        {
            var compact = name.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
            var resource = Encoding.ASCII.GetBytes($@"Mechs\{compact}\{compact}.data");
            BinaryPrimitives.WriteUInt32LittleEndian(scalar, checked((uint)resource.Length));
            stream.Write(scalar);
            stream.Write(resource);
            stream.WriteByte(0);
            Array.Clear(metadata);
            BinaryPrimitives.WriteUInt16LittleEndian(metadata, 1);
            BinaryPrimitives.WriteUInt32LittleEndian(metadata[2..], id++);
            stream.Write(metadata);
            stream.Write(Encoding.ASCII.GetBytes(name));
            stream.WriteByte(0);
            BinaryPrimitives.WriteUInt32LittleEndian(scalar, flag);
            stream.Write(scalar);
        }
        return stream.ToArray();
    }

    private static IReadOnlyList<IReadOnlyList<uint>> ReadFlags(string path)
    {
        var archive = File.ReadAllBytes(path);
        var directoryEnd = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(archive.AsSpan(12, 4)));
        var offset = 0x3c;
        var tables = new List<IReadOnlyList<uint>>();
        while (offset < directoryEnd)
        {
            var length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(archive.AsSpan(offset + 12, 4)));
            var dataOffset = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(archive.AsSpan(offset + 16, 4)));
            var nameLength = archive[offset + 22];
            var name = Encoding.ASCII.GetString(archive, offset + 23, nameLength);
            if (name.EndsWith(".mpt", StringComparison.OrdinalIgnoreCase))
                tables.Add(ReadTableFlags(archive.AsSpan(directoryEnd + dataOffset, length)));
            offset += 23 + nameLength;
        }
        return tables;
    }

    private static IReadOnlyList<uint> ReadTableFlags(ReadOnlySpan<byte> table)
    {
        var flags = new List<uint>();
        var offset = 0;
        while (offset < table.Length)
        {
            var pathLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(table.Slice(offset, 4)));
            offset += 4 + pathLength + 1 + 8;
            while (table[offset++] != 0) { }
            flags.Add(BinaryPrimitives.ReadUInt32LittleEndian(table.Slice(offset, 4)));
            offset += 4;
        }
        return flags;
    }

    private static string Resolve(InstallFile file) =>
        Path.Combine(file.SourceRoot, file.SourceRelativePath.Replace('/', Path.DirectorySeparatorChar));

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }
}
