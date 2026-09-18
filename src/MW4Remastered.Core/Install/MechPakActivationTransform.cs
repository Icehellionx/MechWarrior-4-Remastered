using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace MW4Remastered.Core.Install;

public sealed class MechPakActivationTransform
{
    private const uint VbdVersion = 4;
    private const int RootEntryStart = 0x14;
    private const uint ResetCode = 0x100;
    private const uint EndCode = 0x101;
    private const uint FirstDictionaryCode = 0x102;
    private const string MercenariesPr1ArchiveHash = "ad045a0a2026408ecaf22ea739a53812803a91464d647f87a8d3e90b92ba1421";

    private static readonly IReadOnlySet<string> SupportedArchiveHashes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Vengeance Patch 3 core.mw4
            "f59dfecb9aa4f76648b9c7f1e0c1c7e58ba5bcd0022c113dc87c20f6d33aa73c",
            // Black Knight PR1 corex.mw4
            "c1b444cb137820c7f08a66ee70f778da3fc985fda5551e22a22e89aeb6fe02a5",
            // Mercenaries PR1 core.mw4
            MercenariesPr1ArchiveHash,
        };

    private static readonly IReadOnlySet<string> SupportedExecutableHashes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Final Vengeance Patch 3, Black Knight PR1, and Mercenaries PR1
            // executables emitted by this install pipeline. Referencing the
            // owning transforms prevents accidentally approving an earlier
            // intermediate reconstruction rather than the committed file.
            VengeancePatch3ExecutableTransform.OutputSha256,
            BlackKnightPr1ExecutableTransform.OutputSha256,
            MercenariesPr1ExecutableTransform.OutputSha256,
        };

    private static readonly IReadOnlyDictionary<string, uint> PackFlags =
        new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
        {
            ["clan"] = 1,
            ["inner-sphere"] = 2,
        };

    private static readonly IReadOnlySet<string> TargetTables =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "tables\\mechtable.mpt",
            "tables\\mechchassistable.mpt",
            "tables\\weaponstable.mpt",
            "tables\\subsystemtable.mpt",
        };

    private static readonly IReadOnlySet<string> ComponentTables =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "tables\\weaponstable.mpt",
            "tables\\subsystemtable.mpt",
        };

    private readonly IReadOnlySet<string> supportedArchiveHashes;
    private readonly IReadOnlySet<string> supportedExecutableHashes;

    public MechPakActivationTransform()
        : this(SupportedArchiveHashes, SupportedExecutableHashes)
    {
    }

    internal MechPakActivationTransform(
        IReadOnlySet<string> supportedArchiveHashes,
        IReadOnlySet<string>? supportedExecutableHashes = null)
    {
        this.supportedArchiveHashes = supportedArchiveHashes ?? throw new ArgumentNullException(nameof(supportedArchiveHashes));
        this.supportedExecutableHashes = supportedExecutableHashes ?? new HashSet<string>();
    }

    public InstallPlan TransformPlan(
        InstallPlan plan,
        IEnumerable<string> enabledPackIds,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(enabledPackIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(scratchDirectory);

        var enabledFlags = enabledPackIds
            .Select(id => PackFlags.TryGetValue(id, out var flag)
                ? flag
                : throw new InvalidDataException($"Unsupported Mech Pak activation id: {id}"))
            .Distinct()
            .ToHashSet();
        if (enabledFlags.Count == 0) return plan;

        var scratch = Path.GetFullPath(scratchDirectory);
        Directory.CreateDirectory(scratch);
        if ((File.GetAttributes(scratch) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Mech Pak activation scratch directory cannot be a reparse point.");

        var files = plan.Files.ToList();
        var transformed = 0;
        var executableTransforms = 0;
        for (var index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = files[index];
            var destination = candidate.DestinationRelativePath.Replace('/', '\\');
            if (!destination.Equals("RESOURCE\\core.mw4", StringComparison.OrdinalIgnoreCase) &&
                !destination.Equals("RESOURCE\\corex.mw4", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = ResolveRegularSource(candidate);
            var hash = HashFile(source);
            if (!supportedArchiveHashes.Contains(hash))
                throw new InvalidDataException($"Unsupported Mech Pak activation archive SHA-256 for {destination}: {hash}");

            var outputName = $"{transformed:D2}-{Path.GetFileName(destination)}";
            var output = Path.Combine(scratch, outputName);
            TransformArchive(source, output, enabledFlags, hash, cancellationToken);
            files[index] = new InstallFile(scratch, outputName, candidate.DestinationRelativePath);
            transformed++;
        }

        if (transformed == 0)
            throw new InvalidDataException("The install plan did not contain a qualified Mech Pak activation archive.");

        for (var index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = files[index];
            var destination = candidate.DestinationRelativePath.Replace('/', '\\');
            if (!destination.Equals("MW4.exe", StringComparison.OrdinalIgnoreCase) &&
                !destination.Equals("MW4X\\MW4X.exe", StringComparison.OrdinalIgnoreCase) &&
                !destination.Equals("MW4Mercs.exe", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = ResolveRegularSource(candidate);
            var hash = HashFile(source);
            if (!supportedExecutableHashes.Contains(hash))
                throw new InvalidDataException($"Unsupported Mech Pak activation executable SHA-256 for {destination}: {hash}");

            var outputName = $"{transformed + executableTransforms:D2}-{Path.GetFileName(destination)}";
            var output = Path.Combine(scratch, outputName);
            TransformExecutable(source, output, enabledFlags, hash);
            files[index] = new InstallFile(scratch, outputName, candidate.DestinationRelativePath);
            executableTransforms++;
        }

        if (supportedExecutableHashes.Count > 0 && executableTransforms == 0)
            throw new InvalidDataException("The install plan did not contain a qualified Mech Pak activation executable.");
        return new InstallPlan(plan.ProductId, files, plan.Components);
    }

    private static void TransformExecutable(
        string sourcePath,
        string outputPath,
        IReadOnlySet<uint> enabledFlags,
        string sourceHash)
    {
        var bytes = File.ReadAllBytes(sourcePath);
        var match = -1;
        for (var index = 0; index <= bytes.Length - 22; index++)
        {
            if (bytes[index] != 0xe8 ||
                !bytes.AsSpan(index + 5, 4).SequenceEqual(new byte[] { 0x85, 0xc0, 0x75, 0x37 }) ||
                !bytes.AsSpan(index + 9, 2).SequenceEqual(new byte[] { 0x8b, 0x15 }) ||
                !bytes.AsSpan(index + 15, 7).SequenceEqual(new byte[] { 0x50, 0x68, 0x87, 0x17, 0x00, 0x00, 0x52 }))
            {
                continue;
            }
            if (match >= 0)
                throw new InvalidDataException("Mech Pak ownership selection gate is not unique.");
            match = index;
        }
        if (match < 0)
            throw new InvalidDataException("Mech Pak ownership selection gate was not found.");

        // The original client has already recognized the installed pack data,
        // then calls its obsolete SafeCast PID validator immediately before
        // allowing selection. Setup has independently validated the user's
        // corresponding pack media, so retain the call for compatibility but
        // make its existing success branch unconditional.
        bytes[match + 7] = 0xeb;

        // The shell has a second, independent ownership layer. AskUpdate.script
        // calls ShellCheckClanPak/ShellCheckISPak before it permits a listed
        // chassis to be selected. Those callbacks return obfuscated runtime
        // pointers that the obsolete SafeCast installer initialized. The media
        // workflow intentionally does not run SafeCast, so make only the
        // callbacks backed by validated selected media report present. This is
        // separate from the global PID dialog branch above.
        foreach (var enabledFlag in enabledFlags)
        {
            var mercenaries = sourceHash.Equals(
                MercenariesPr1ExecutableTransform.OutputSha256,
                StringComparison.OrdinalIgnoreCase);
            var runtimeSlot = enabledFlag switch
            {
                1 => mercenaries ? (byte)0x4c : (byte)0x3c, // Clan Pak
                2 => mercenaries ? (byte)0x58 : (byte)0x48, // Inner Sphere Pak
                _ => throw new InvalidDataException($"Unsupported Mech Pak activation flag: {enabledFlag}"),
            };
            var callback = FindPackPresenceCallback(bytes, runtimeSlot);
            // mov eax,1; ret 0x0c. The registered shell callback uses the
            // engine's three-argument callee-clean convention.
            bytes[callback] = 0xb8;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(callback + 1, 4), 1);
            bytes[callback + 5] = 0xc2;
            bytes[callback + 6] = 0x0c;
            bytes[callback + 7] = 0x00;
            bytes.AsSpan(callback + 8, 16).Fill(0x90);
        }

        // MechLab has a third ownership boundary below the script callbacks.
        // Its normal selection path first loads the chassis/model and then
        // consults two obfuscated process globals. A missing Clan value makes
        // that routine return -1; a missing Inner Sphere value returns -2.
        // Returning success from the callback above this routine suppresses
        // the dialog but also skips the model load. Instead, project the
        // validated selected media into every copy of the native presence
        // pair and leave the complete load/selection path intact.
        var runtimePresenceGates = ActivateNativePackPresence(bytes, enabledFlags);
        runtimePresenceGates += ActivateInstantActionStockPresence(bytes, enabledFlags);
        var expectedRuntimePresenceGates = sourceHash switch
        {
            VengeancePatch3ExecutableTransform.OutputSha256 => 5,
            BlackKnightPr1ExecutableTransform.OutputSha256 => 5,
            MercenariesPr1ExecutableTransform.OutputSha256 => 5,
            _ => (int?)null,
        };
        if (expectedRuntimePresenceGates is int expected && runtimePresenceGates != expected)
        {
            throw new InvalidDataException(
                $"Mech Pak native presence gate count was {runtimePresenceGates}; expected {expected}.");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.WriteAllBytes(outputPath, bytes);
    }

    private static int ActivateNativePackPresence(byte[] bytes, IReadOnlySet<uint> enabledFlags)
    {
        ReadOnlySpan<byte> initialization =
        [
            0xc6, 0x44, 0x24, 0x12, 0x00,
            0xc6, 0x44, 0x24, 0x13, 0x00,
            0x74, 0x05, 0xc6, 0x44, 0x24, 0x12, 0x01,
        ];
        ReadOnlySpan<byte> secondSuccess = [0x74, 0x05, 0xc6, 0x44, 0x24, 0x13, 0x01];
        ReadOnlySpan<byte> obfuscationConstant = [0x31, 0x95, 0x73, 0x85];
        var matches = 0;

        for (var index = 9; index <= bytes.Length - 34; index++)
        {
            if (!bytes.AsSpan(index, initialization.Length).SequenceEqual(initialization) ||
                !HasObfuscatedPackFieldBefore(bytes, index, obfuscationConstant))
            {
                continue;
            }

            var secondLoad = index + initialization.Length;
            if (bytes[secondLoad] != 0x8b || bytes[secondLoad + 2] is not (0x48 or 0x58))
                continue;

            var xorLength = bytes[secondLoad + 3] switch
            {
                0x35 when bytes.AsSpan(secondLoad + 4, 4).SequenceEqual(obfuscationConstant) => 5,
                0x81 when (bytes[secondLoad + 4] & 0xf8) == 0xf0 &&
                          bytes.AsSpan(secondLoad + 5, 4).SequenceEqual(obfuscationConstant) => 6,
                _ => 0,
            };
            if (xorLength == 0 ||
                !bytes.AsSpan(secondLoad + 3 + xorLength, secondSuccess.Length).SequenceEqual(secondSuccess))
            {
                continue;
            }

            if (enabledFlags.Contains(1)) bytes[index + 4] = 1;
            if (enabledFlags.Contains(2)) bytes[index + 9] = 1;
            matches++;
        }

        if (matches == 0)
            throw new InvalidDataException("Mech Pak native presence gates were not found.");
        return matches;
    }

    internal static int ActivateInstantActionStockPresence(byte[] bytes, IReadOnlySet<uint> enabledFlags)
    {
        // Vengeance and Black Knight compile the Instant Action stock-roster
        // presence locals differently from the other native consumers: the
        // Inner Sphere false value is copied from BL instead of emitted as an
        // immediate zero. That instruction form is absent from Mercenaries,
        // whose equivalent path is covered by ActivateNativePackPresence.
        ReadOnlySpan<byte> stockRosterGate =
        [
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
        ];
        var matches = 0;
        for (var index = 0; index <= bytes.Length - stockRosterGate.Length; index++)
        {
            if (!bytes.AsSpan(index, stockRosterGate.Length).SequenceEqual(stockRosterGate))
                continue;

            if (enabledFlags.Contains(1)) bytes[index + 15] = 1;
            if (enabledFlags.Contains(2))
            {
                // xor bl,bl -> mov bl,1. Both encodings are two bytes.
                bytes[index + 3] = 0xb3;
                bytes[index + 4] = 1;
            }
            matches++;
        }
        return matches;
    }

    private static bool HasObfuscatedPackFieldBefore(
        byte[] bytes,
        int initializationOffset,
        ReadOnlySpan<byte> obfuscationConstant)
    {
        var fieldEnd = initializationOffset;
        if (fieldEnd > 0 && bytes[fieldEnd - 1] is >= 0x50 and <= 0x57)
            fieldEnd--; // optional preserved-register push between the test and local initialization

        // eax uses the one-byte XOR opcode; other registers use 81 /6.
        if (fieldEnd >= 8 &&
            bytes[fieldEnd - 8] == 0x8b &&
            bytes[fieldEnd - 6] is 0x3c or 0x4c &&
            bytes[fieldEnd - 5] == 0x35 &&
            bytes.AsSpan(fieldEnd - 4, 4).SequenceEqual(obfuscationConstant))
        {
            return true;
        }

        return fieldEnd >= 9 &&
               bytes[fieldEnd - 9] == 0x8b &&
               bytes[fieldEnd - 7] is 0x3c or 0x4c &&
               bytes[fieldEnd - 6] == 0x81 &&
               (bytes[fieldEnd - 5] & 0xf8) == 0xf0 &&
               bytes.AsSpan(fieldEnd - 4, 4).SequenceEqual(obfuscationConstant);
    }

    private static int FindPackPresenceCallback(byte[] bytes, byte runtimeSlot)
    {
        var match = -1;
        for (var index = 0; index <= bytes.Length - 24; index++)
        {
            if (bytes[index] != 0xa1 ||
                !bytes.AsSpan(index + 5, 3).SequenceEqual(new byte[] { 0x8b, 0x08, 0x33 }) ||
                bytes[index + 8] != 0x0d ||
                !bytes.AsSpan(index + 13, 3).SequenceEqual(new byte[] { 0x8b, 0x41, runtimeSlot }) ||
                bytes[index + 16] != 0x35 ||
                !bytes.AsSpan(index + 21, 3).SequenceEqual(new byte[] { 0xc2, 0x0c, 0x00 }))
            {
                continue;
            }
            if (match >= 0)
                throw new InvalidDataException("Mech Pak shell presence callback is not unique.");
            match = index;
        }
        if (match < 0)
            throw new InvalidDataException("Mech Pak shell presence callback was not found.");
        return match;
    }

    private static void TransformArchive(
        string sourcePath,
        string outputPath,
        IReadOnlySet<uint> enabledFlags,
        string sourceHash,
        CancellationToken cancellationToken)
        => TransformArchiveTables(
            sourcePath,
            outputPath,
            enabledFlags,
            TargetTables,
            (name, flag) => ExpectedFreshTableRecords(sourceHash, name, flag),
            cancellationToken);

    internal static void UpgradeInstalledArchiveComponents(
        string sourcePath,
        string outputPath,
        IReadOnlySet<uint> enabledFlags,
        CancellationToken cancellationToken = default)
        => TransformArchiveTables(
            sourcePath,
            outputPath,
            enabledFlags,
            ComponentTables,
            ExpectedComponentTableRecords,
            cancellationToken);

    private static void TransformArchiveTables(
        string sourcePath,
        string outputPath,
        IReadOnlySet<uint> enabledFlags,
        IReadOnlySet<string> targetTables,
        Func<string, uint, int> expectedRecords,
        CancellationToken cancellationToken)
    {
        var archive = File.ReadAllBytes(sourcePath);
        if (archive.Length < RootEntryStart + 23 || archive[0] != (byte)'#' || archive[1] != (byte)'V' ||
            archive[2] != (byte)'B' || archive[3] != (byte)'D' ||
            BinaryPrimitives.ReadUInt32LittleEndian(archive.AsSpan(4, 4)) != VbdVersion)
        {
            throw new InvalidDataException("Qualified Mech Pak archive does not have the expected #VBD version 4 header.");
        }

        var directoryEnd = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(archive.AsSpan(12, 4)));
        var directoryStart = RootEntryStart + 23 + archive[RootEntryStart + 22];
        if (directoryEnd < directoryStart || directoryEnd > archive.Length)
            throw new InvalidDataException("Mech Pak archive directory boundary is invalid.");

        var entries = ReadDirectory(archive, directoryStart, directoryEnd)
            .Where(entry => targetTables.Contains(entry.Name))
            .ToArray();
        if (entries.Length != targetTables.Count || entries.Select(entry => entry.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != targetTables.Count)
            throw new InvalidDataException("Mech Pak archive must contain exactly one copy of each activation table.");

        using var output = new MemoryStream(archive.Length + entries.Sum(entry => entry.UnpackedLength));
        output.Write(archive);
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var decoded = ReadPayload(archive, directoryEnd, entry);
            var changed = UnlockTable(decoded, enabledFlags);
            var expectedChanges = enabledFlags.Sum(flag => expectedRecords(entry.Name, flag));
            if (changed != expectedChanges)
                throw new InvalidDataException($"{entry.Name} exposed {changed} selected Mech Pak records; expected {expectedChanges}.");
            if (changed == 0) continue;

            var dataOffset = checked((uint)(output.Length - directoryEnd));
            output.Write(decoded);
            BinaryPrimitives.WriteUInt32LittleEndian(output.GetBuffer().AsSpan(entry.StoredLengthOffset, 4), checked((uint)decoded.Length));
            BinaryPrimitives.WriteUInt32LittleEndian(output.GetBuffer().AsSpan(entry.DataOffsetOffset, 4), dataOffset);
        }

        var target = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllBytes(target, output.ToArray());
    }

    private static int ExpectedFreshTableRecords(string sourceHash, string tableName, uint flag)
    {
        if (sourceHash.Equals(MercenariesPr1ArchiveHash, StringComparison.OrdinalIgnoreCase) &&
            ComponentTables.Contains(tableName))
        {
            return 0; // Mercenaries PR1 already exposes the pack equipment records.
        }
        return ExpectedTableRecords(tableName, flag);
    }

    private static int ExpectedComponentTableRecords(string tableName, uint flag) =>
        ExpectedTableRecords(tableName, flag);

    private static int ExpectedTableRecords(string tableName, uint flag)
    {
        if (tableName.Equals("tables\\mechtable.mpt", StringComparison.OrdinalIgnoreCase) ||
            tableName.Equals("tables\\mechchassistable.mpt", StringComparison.OrdinalIgnoreCase)) return 4;
        if (tableName.Equals("tables\\weaponstable.mpt", StringComparison.OrdinalIgnoreCase))
            return flag == 1 ? 4 : 1;
        if (tableName.Equals("tables\\subsystemtable.mpt", StringComparison.OrdinalIgnoreCase)) return 1;
        throw new InvalidDataException($"Unsupported Mech Pak activation table: {tableName}");
    }

    private static IReadOnlyList<VbdEntry> ReadDirectory(byte[] archive, int directoryStart, int directoryEnd)
    {
        var entries = new List<VbdEntry>();
        var offset = directoryStart;
        while (offset < directoryEnd)
        {
            if (offset > directoryEnd - 23)
                throw new InvalidDataException("Mech Pak archive directory entry is truncated.");
            var unpacked = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(archive.AsSpan(offset + 8, 4)));
            var stored = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(archive.AsSpan(offset + 12, 4)));
            var dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(archive.AsSpan(offset + 16, 4));
            var nameLength = archive[offset + 22];
            if (offset + 23 + nameLength > directoryEnd)
                throw new InvalidDataException("Mech Pak archive directory name is truncated.");
            var name = Encoding.ASCII.GetString(archive, offset + 23, nameLength);
            entries.Add(new VbdEntry(name, unpacked, stored, dataOffset, offset + 12, offset + 16));
            offset += 23 + nameLength;
        }
        if (offset != directoryEnd)
            throw new InvalidDataException("Mech Pak archive directory did not end at its declared boundary.");
        return entries;
    }

    private static byte[] ReadPayload(byte[] archive, int directoryEnd, VbdEntry entry)
    {
        var start = checked(directoryEnd + (int)entry.DataOffset);
        if (entry.StoredLength < 0 || start < directoryEnd || start > archive.Length - entry.StoredLength)
            throw new InvalidDataException($"Mech Pak archive payload is outside the file: {entry.Name}");
        var stored = archive.AsSpan(start, entry.StoredLength);
        if (entry.StoredLength == entry.UnpackedLength) return stored.ToArray();
        return DecompressLzw(stored, entry.UnpackedLength);
    }

    private static byte[] DecompressLzw(ReadOnlySpan<byte> input, int expectedLength)
    {
        var output = new List<byte>(expectedLength);
        var prefixes = new ushort[4096];
        var suffixes = new byte[4096];
        var codeSize = 9;
        var codeMask = 0x1ffu;
        var nextWidthAt = 0x200u;
        var nextCode = FirstDictionaryCode;
        var previousCode = 0u;
        var previousFirst = (byte)0;
        var bitOffset = 0;

        while (true)
        {
            var code = ReadCode(input, ref bitOffset, codeSize, codeMask);
            if (code == EndCode) break;
            if (code == ResetCode)
            {
                codeSize = 9;
                codeMask = 0x1ff;
                nextWidthAt = 0x200;
                nextCode = FirstDictionaryCode;
                var literal = ReadCode(input, ref bitOffset, codeSize, codeMask);
                if (literal > byte.MaxValue) throw new InvalidDataException("Mech Pak archive LZW reset was not followed by a literal.");
                previousCode = literal;
                previousFirst = (byte)literal;
                output.Add(previousFirst);
                continue;
            }

            var currentCode = code;
            var stack = new Stack<byte>();
            if (code >= nextCode)
            {
                stack.Push(previousFirst);
                code = previousCode;
            }
            while (code > byte.MaxValue)
            {
                if (code >= nextCode) throw new InvalidDataException("Mech Pak archive LZW dictionary reference is invalid.");
                stack.Push(suffixes[code]);
                code = prefixes[code];
            }
            previousFirst = (byte)code;
            stack.Push(previousFirst);
            while (stack.Count > 0) output.Add(stack.Pop());

            if (nextCode >= 4096) throw new InvalidDataException("Mech Pak archive LZW dictionary exceeded 12 bits without reset.");
            prefixes[nextCode] = checked((ushort)previousCode);
            suffixes[nextCode] = previousFirst;
            nextCode++;
            previousCode = currentCode;
            if (nextCode >= nextWidthAt && codeSize < 12)
            {
                codeSize++;
                codeMask = (codeMask << 1) | 1;
                nextWidthAt <<= 1;
            }
        }

        if (output.Count != expectedLength)
            throw new InvalidDataException($"Mech Pak archive LZW output length was {output.Count}; expected {expectedLength}.");
        return output.ToArray();
    }

    private static uint ReadCode(ReadOnlySpan<byte> input, ref int bitOffset, int codeSize, uint mask)
    {
        if (bitOffset < 0 || bitOffset + codeSize > input.Length * 8)
            throw new InvalidDataException("Mech Pak archive LZW stream ended before its terminator.");
        var byteOffset = bitOffset >> 3;
        uint window = 0;
        for (var index = 0; index < 4 && byteOffset + index < input.Length; index++)
            window |= (uint)input[byteOffset + index] << (index * 8);
        var code = (window >> (bitOffset & 7)) & mask;
        bitOffset += codeSize;
        return code;
    }

    private static int UnlockTable(byte[] table, IReadOnlySet<uint> enabledFlags)
    {
        var offset = 0;
        var changed = 0;
        while (offset < table.Length)
        {
            if (offset > table.Length - 4) throw new InvalidDataException("Mech Pak activation table is truncated.");
            var pathLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(table.AsSpan(offset, 4)));
            offset += 4;
            if (pathLength < 1 || pathLength > 512 || offset > table.Length - pathLength - 9)
                throw new InvalidDataException("Mech Pak activation table path is invalid.");
            offset += pathLength;
            if (table[offset++] != 0) throw new InvalidDataException("Mech Pak activation table path is not terminated.");
            offset += 8; // marker, resource id, and reserved word
            while (offset < table.Length && table[offset] != 0) offset++;
            if (offset >= table.Length || offset + 5 > table.Length)
                throw new InvalidDataException("Mech Pak activation table display name is invalid.");
            offset++;
            var flag = BinaryPrimitives.ReadUInt32LittleEndian(table.AsSpan(offset, 4));
            if (enabledFlags.Contains(flag))
            {
                BinaryPrimitives.WriteUInt32LittleEndian(table.AsSpan(offset, 4), 0);
                changed++;
            }
            offset += 4;
        }
        return changed;
    }

    private static string ResolveRegularSource(InstallFile file)
    {
        var root = Path.GetFullPath(file.SourceRoot);
        var path = Path.GetFullPath(Path.Combine(root, file.SourceRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        StagedInstallTransaction.RejectContainedFilePath(root, path);
        if (!File.Exists(path)) throw new FileNotFoundException("Mech Pak activation source archive is missing.", path);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Mech Pak activation source archive cannot be a reparse point.");
        return path;
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed record VbdEntry(
        string Name,
        int UnpackedLength,
        int StoredLength,
        uint DataOffset,
        int StoredLengthOffset,
        int DataOffsetOffset);
}
