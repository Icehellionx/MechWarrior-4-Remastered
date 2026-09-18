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
            var executable = Path.Combine(source, "MW4.exe");
            WriteArchive(archive);
            WriteExecutable(executable);
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive))).ToLowerInvariant();
            var executableHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(executable))).ToLowerInvariant();
            var componentExpected = Path.Combine(root, "component-expected.mw4");
            MechPakActivationTransform.UpgradeInstalledArchiveComponents(
                archive, componentExpected, new HashSet<uint> { 1, 2 });
            var componentExpectedHash = Convert.ToHexString(
                SHA256.HashData(File.ReadAllBytes(componentExpected))).ToLowerInvariant();
            var upgradeManifest = new InstallManifest(2, "vengeance",
                [new InstalledFile("RESOURCE/core.mw4", new FileInfo(archive).Length, hash, "test")],
                ["vengeance", "clan", "inner-sphere"]);
            var archiveUpgrade = new MechPakOwnedArchiveUpgrade(
            [
                new MechPakOwnedArchiveUpgrade.QualifiedUpgrade(
                    "vengeance", "RESOURCE/core.mw4", 3, hash, componentExpectedHash),
            ]);
            var upgradeInstall = Path.Combine(root, "upgrade-install", "RESOURCE");
            Directory.CreateDirectory(upgradeInstall);
            File.Copy(archive, Path.Combine(upgradeInstall, "core.mw4"));
            var archiveReplacements = archiveUpgrade.CreateReplacements(
                Path.GetDirectoryName(upgradeInstall)!, upgradeManifest, Path.Combine(root, "archive-upgrade"));
            Check(archiveReplacements.Count == 1 &&
                  Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Resolve(archiveReplacements.Single()))))
                      .Equals(componentExpectedHash, StringComparison.OrdinalIgnoreCase),
                "owned Mech Pak archive upgrade exposes pack weapons and subsystems on an existing install", failures);
            var transform = new MechPakActivationTransform(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { hash },
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { executableHash });
            var plan = new InstallPlan("vengeance", new[]
            {
                new InstallFile(source, "core.mw4", "RESOURCE/core.mw4"),
                new InstallFile(source, "MW4.exe", "MW4.exe"),
            }, new[] { "vengeance", "clan" });

            var clanPlan = transform.TransformPlan(plan, new[] { "clan" }, Path.Combine(root, "clan"));
            var clanArchive = Resolve(clanPlan.Files.Single(file =>
                file.DestinationRelativePath.EndsWith("core.mw4", StringComparison.OrdinalIgnoreCase)));
            var clanFlags = ReadFlags(clanArchive);
            Check(clanFlags.Count == 4 &&
                  clanFlags[0].Count(flag => flag == 0) == 5 && clanFlags[0].Count(flag => flag == 2) == 4 &&
                  clanFlags[1].Count(flag => flag == 0) == 5 && clanFlags[1].Count(flag => flag == 2) == 4 &&
                  clanFlags[2].Count(flag => flag == 0) == 5 && clanFlags[2].Count(flag => flag == 2) == 1 &&
                  clanFlags[3].Count(flag => flag == 0) == 2 && clanFlags[3].Count(flag => flag == 2) == 1,
                "Mech Pak activation unlocks only the selected Clan chassis, weapons, and subsystem records", failures);
            Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive))).Equals(hash, StringComparison.OrdinalIgnoreCase),
                "Mech Pak activation produces a transformed archive rather than mutating source media", failures);
            var clanExecutable = File.ReadAllBytes(Resolve(clanPlan.Files.Single(file =>
                file.DestinationRelativePath.Equals("MW4.exe", StringComparison.OrdinalIgnoreCase))));
            Check(clanExecutable[7] == 0xeb,
                "Mech Pak activation bypasses only the obsolete post-recognition PID selection failure", failures);
            Check(clanExecutable.AsSpan(22, 8).SequenceEqual(new byte[] { 0xb8, 1, 0, 0, 0, 0xc2, 0x0c, 0 }),
                "Mech Pak activation makes the Clan shell ownership callback usable after validated media setup", failures);
            Check(clanExecutable[46] == 0xa1,
                "Mech Pak activation leaves the unselected Inner Sphere shell ownership callback unchanged", failures);
            Check(clanExecutable[^31] == 1 && clanExecutable[^26] == 0,
                "Mech Pak activation projects only selected Clan media into the native model-loading path", failures);
            var stockGate = FindSequence(clanExecutable,
            [
                0x8b, 0x4e, 0x3c, 0x32, 0xdb, 0x81, 0xf1, 0x31, 0x95, 0x73, 0x85,
            ]);
            Check(stockGate >= 0 && clanExecutable[stockGate + 15] == 1 &&
                clanExecutable[stockGate + 3] == 0x32 && clanExecutable[stockGate + 4] == 0xdb,
                "Mech Pak activation exposes selected Clan stock chassis to Instant Action", failures);

            var bothPlan = transform.TransformPlan(plan, new[] { "clan", "inner-sphere" }, Path.Combine(root, "both"));
            Check(ReadFlags(Resolve(bothPlan.Files.Single(file =>
                file.DestinationRelativePath.EndsWith("core.mw4", StringComparison.OrdinalIgnoreCase)))).All(flags => flags.All(flag => flag == 0)),
                "Mech Pak activation unlocks all eight official records when both packs are selected", failures);
            var bothExecutable = File.ReadAllBytes(Resolve(bothPlan.Files.Single(file =>
                file.DestinationRelativePath.Equals("MW4.exe", StringComparison.OrdinalIgnoreCase))));
            Check(bothExecutable[22] == 0xb8 && bothExecutable[46] == 0xb8,
                "Mech Pak activation enables both script-facing ownership callbacks when both media are selected", failures);
            Check(bothExecutable[^31] == 1 && bothExecutable[^26] == 1,
                "Mech Pak activation preserves model loading while satisfying both native ownership flags", failures);
            stockGate = FindSequence(bothExecutable,
            [
                0x8b, 0x4e, 0x3c, 0xb3, 0x01, 0x81, 0xf1, 0x31, 0x95, 0x73, 0x85,
            ]);
            Check(stockGate >= 0 && bothExecutable[stockGate + 15] == 1,
                "Mech Pak activation exposes both selected stock rosters to Instant Action", failures);

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

            var executableRejected = false;
            try
            {
                _ = new MechPakActivationTransform(
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { hash },
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { new string('0', 64) })
                    .TransformPlan(plan, new[] { "clan" }, Path.Combine(root, "rejected-executable"));
            }
            catch (InvalidDataException exception)
            {
                executableRejected = exception.Message.Contains("Unsupported Mech Pak activation executable SHA-256", StringComparison.Ordinal);
            }
            Check(executableRejected, "Mech Pak activation rejects unqualified final executables", failures);

            var duplicateExecutable = Path.Combine(source, "MW4Mercs.exe");
            File.WriteAllBytes(duplicateExecutable, File.ReadAllBytes(executable).Concat(File.ReadAllBytes(executable)).ToArray());
            var duplicateHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(duplicateExecutable))).ToLowerInvariant();
            var duplicatePlan = new InstallPlan(
                plan.ProductId,
                [
                    plan.Files[0],
                    new InstallFile(source, "MW4Mercs.exe", "MW4Mercs.exe"),
                ],
                plan.Components);
            var duplicateRejected = false;
            try
            {
                _ = new MechPakActivationTransform(
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { hash },
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { duplicateHash })
                    .TransformPlan(duplicatePlan, new[] { "clan" }, Path.Combine(root, "duplicate-gate"));
            }
            catch (InvalidDataException exception)
            {
                duplicateRejected = exception.Message.Contains("not unique", StringComparison.Ordinal);
            }
            Check(duplicateRejected, "Mech Pak activation rejects executables with a non-unique ownership gate", failures);

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
            [@"tables\mechtable.mpt"] = CreateChassisTable(),
            [@"tables\mechchassistable.mpt"] = CreateChassisTable(),
            [@"tables\weaponstable.mpt"] = CreateTable(
                ("SMRM10", 1u), ("SMRM20", 1u), ("SMRM30", 1u), ("SMRM40", 1u),
                ("HeavyGauss", 2u), ("ERLargeLaser", 0u)),
            [@"tables\subsystemtable.mpt"] = CreateTable(
                ("EnhancedOptics", 1u), ("IFFJammer", 2u), ("HeatSink", 0u)),
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

    private static void WriteExecutable(string path)
    {
        File.WriteAllBytes(path,
        [
            0xe8, 0x11, 0x22, 0x33, 0x44,
            0x85, 0xc0, 0x75, 0x37,
            0x8b, 0x15, 0x55, 0x66, 0x77, 0x88,
            0x50, 0x68, 0x87, 0x17, 0x00, 0x00, 0x52,
            0xa1, 0x0c, 0x7c, 0x81, 0x00,
            0x8b, 0x08, 0x33, 0x0d, 0x08, 0x7c, 0x81, 0x00,
            0x8b, 0x41, 0x3c, 0x35, 0x31, 0x95, 0x73, 0x85, 0xc2, 0x0c, 0x00,
            0xa1, 0x0c, 0x7c, 0x81, 0x00,
            0x8b, 0x08, 0x33, 0x0d, 0x08, 0x7c, 0x81, 0x00,
            0x8b, 0x41, 0x48, 0x35, 0x31, 0x95, 0x73, 0x85, 0xc2, 0x0c, 0x00,
            0x8b, 0x4e, 0x3c,
            0x32, 0xdb,
            0x81, 0xf1, 0x31, 0x95, 0x73, 0x85,
            0xc6, 0x44, 0x24, 0x12, 0x00,
            0x88, 0x5c, 0x24, 0x13,
            0x74, 0x05, 0xc6, 0x44, 0x24, 0x12, 0x01,
            0x8b, 0x56, 0x48,
            0x81, 0xf2, 0x31, 0x95, 0x73, 0x85,
            0x74, 0x09, 0xc6, 0x44, 0x24, 0x13, 0x01,
            0x8a, 0x5c, 0x24, 0x13,
            0x8b, 0x4e, 0x3c, 0x81, 0xf1, 0x31, 0x95, 0x73, 0x85,
            0xc6, 0x44, 0x24, 0x12, 0x00,
            0xc6, 0x44, 0x24, 0x13, 0x00,
            0x74, 0x05, 0xc6, 0x44, 0x24, 0x12, 0x01,
            0x8b, 0x56, 0x48, 0x81, 0xf2, 0x31, 0x95, 0x73, 0x85,
            0x74, 0x05, 0xc6, 0x44, 0x24, 0x13, 0x01, 0x6a, 0x14,
        ]);
    }

    private static byte[] CreateChassisTable() => CreateTable(
            ("Arctic Wolf", 1u), ("Cauldron-Born", 1u), ("Kodiak", 1u), ("Masakari", 1u),
            ("Dragon", 2u), ("Highlander", 2u), ("Hunchback", 2u), ("Zeus", 2u),
            ("Argus", 0u));

    private static byte[] CreateTable(params (string Name, uint Flag)[] entries)
    {
        using var stream = new MemoryStream();
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

    private static int FindSequence(byte[] bytes, ReadOnlySpan<byte> sequence)
    {
        for (var index = 0; index <= bytes.Length - sequence.Length; index++)
        {
            if (bytes.AsSpan(index, sequence.Length).SequenceEqual(sequence)) return index;
        }
        return -1;
    }

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }
}
