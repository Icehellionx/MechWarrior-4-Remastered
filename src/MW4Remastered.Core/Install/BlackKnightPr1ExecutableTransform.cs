using System.Buffers.Binary;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MW4Remastered.Core.Install;

public sealed class BlackKnightPr1ExecutableTransform
{
    public const string TransformId = "black-knight-pr1-static-clean-v4";
    public const string InputSha256 = "be9c15731b2ab59471f35add8df4935ea65355cbb4672bc48b63295ad83632b0";
    public const string OutputSha256 = "b31bd0518311eb88e0f94a38e7b5a7ee6e98f4431f8cf585276b3df1166b8a1e";
    public const long InputLength = 4_735_573;
    public const int MappedImageLength = 0x4A8000;
    public const int OutputLength = 0x392000;

    private const uint ImageBase = 0x400000;
    private const uint OriginalIatStartVa = 0x742000;
    private const uint TargetIatRva = 0x342000;
    private const uint TargetIatSize = 0x62C;
    private const uint CleanImportRva = 0x3836A4;
    private const uint CleanImageSize = 0x4A3000;
    private const string DefinitionIntermediateSha256 = "22620369a1130cda8deca1c3752808e26c26d94fe10c27aed5ae0d463fe2888f";
    private const string DefinitionResource = "MW4Remastered.Core.BlackKnightPr1StaticTransform.json";

    public string Transform(
        string protectedExecutablePath,
        string mappedImagePath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var inputPath = RequireRegularFile(protectedExecutablePath, "Black Knight PR1 executable");
        var imagePath = RequireRegularFile(mappedImagePath, "Black Knight PR1 mapped image");
        var destination = Path.GetFullPath(outputPath);
        if (File.Exists(destination) || Directory.Exists(destination))
            throw new IOException("Black Knight transform output already exists.");

        var protectedBytes = File.ReadAllBytes(inputPath);
        var mappedBytes = File.ReadAllBytes(imagePath);
        if (protectedBytes.LongLength != InputLength || !Hash(protectedBytes).Equals(InputSha256, StringComparison.Ordinal))
            throw new InvalidDataException("Unsupported Black Knight PR1 executable revision.");
        if (mappedBytes.Length != MappedImageLength)
            throw new InvalidDataException("Black Knight PR1 mapped image has an unexpected size.");

        using var definition = LoadDefinition();
        var root = definition.RootElement;
        if (root.GetProperty("formatVersion").GetInt32() != 1 ||
            root.GetProperty("protectedLength").GetInt64() != InputLength ||
            !root.GetProperty("protectedSha256").GetString()!.Equals(InputSha256, StringComparison.Ordinal) ||
            root.GetProperty("mappedImageLength").GetInt32() != MappedImageLength ||
            !root.GetProperty("outputSha256").GetString()!.Equals(DefinitionIntermediateSha256, StringComparison.Ordinal))
            throw new InvalidDataException("Black Knight PR1 transform definition does not match its source contract.");

        var pe = Pe32Image.Parse(protectedBytes);
        foreach (var sectionName in new[] { ".text", ".txt2", ".rdata", ".data", ".rsrc" })
        {
            cancellationToken.ThrowIfCancellationRequested();
            var section = pe.RequireSection(sectionName);
            RequireRange(mappedBytes, checked((int)section.VirtualAddress), checked((int)section.RawSize));
            RequireRange(protectedBytes, checked((int)section.RawOffset), checked((int)section.RawSize));
            Buffer.BlockCopy(mappedBytes, checked((int)section.VirtualAddress), protectedBytes,
                checked((int)section.RawOffset), checked((int)section.RawSize));
        }

        RebuildImports(protectedBytes, pe, root.GetProperty("imports"));
        RetargetCallsites(protectedBytes, pe, root.GetProperty("callsites"), root.GetProperty("imports"));
        ApplyIatCorrections(protectedBytes, pe, root.GetProperty("iatCorrections"), root.GetProperty("imports"));
        RestoreTailBranches(protectedBytes, pe, root.GetProperty("tailBranches"), root.GetProperty("imports"));
        RejectResidualTailBranches(protectedBytes, pe);
        PatchSetupValidation(protectedBytes);

        WriteUInt32(protectedBytes, pe.OptionalHeaderOffset + 16, 0x334D30);
        var idata = pe.RequireSection("stxt371");
        var importCount = root.GetProperty("imports").GetArrayLength();
        var importDirectorySize = checked((uint)((importCount + 1) * 20));
        RelocateImportMetadata(protectedBytes, pe, idata, CleanImportRva, importCount);
        WriteUInt32(protectedBytes, pe.OptionalHeaderOffset + 104, CleanImportRva);
        WriteUInt32(protectedBytes, pe.OptionalHeaderOffset + 108, importDirectorySize);
        // The clean PR1 image uses the ordinary import directory and does not
        // advertise a separate IAT directory.
        WriteUInt32(protectedBytes, pe.OptionalHeaderOffset + 192, 0);
        WriteUInt32(protectedBytes, pe.OptionalHeaderOffset + 196, 0);
        WriteUInt32(protectedBytes, pe.OptionalHeaderOffset + 56, CleanImageSize);
        WriteUInt32(protectedBytes, pe.OptionalHeaderOffset + 64, 0);
        WriteUInt32(protectedBytes, pe.RequireSection(".rdata").HeaderOffset + 36, 0x40000040);
        // Preserve the normalized, inert header bytes produced by transform v1.
        // The section is excluded by NumberOfSections and file truncation, but
        // these bytes are part of the exact runtime-qualified PE header.
        Encoding.ASCII.GetBytes(".idata\0\0").CopyTo(protectedBytes, idata.HeaderOffset);
        WriteUInt32(protectedBytes, idata.HeaderOffset + 36, 0x40000040);

        // SafeDisc's two loader-only tail sections must not survive into the
        // installed executable. Keeping them caused Windows to enter obsolete
        // dispatch code after the real game OEP and crash during presentation.
        BinaryPrimitives.WriteUInt16LittleEndian(protectedBytes.AsSpan(pe.PeHeaderOffset + 6, 2), 5);

        var outputBytes = protectedBytes.AsSpan(0, OutputLength).ToArray();

        var actualHash = Hash(outputBytes);
        if (!actualHash.Equals(OutputSha256, StringComparison.Ordinal))
            throw new InvalidDataException($"Black Knight PR1 transform output mismatch: {actualHash}.");

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllBytes(destination, outputBytes);
        return destination;
    }

    private static void PatchSetupValidation(byte[] bytes)
    {
        PatchExact(bytes, 0x5D14, [0xE8, 0x07, 0xFA, 0xFF, 0xFF], [0xB0, 0x01, 0x90, 0x90, 0x90]);
        PatchExact(bytes, 0x5D36, [0xE8, 0xE5, 0xF9, 0xFF, 0xFF], [0xB0, 0x01, 0x90, 0x90, 0x90]);
    }

    private static void PatchExact(byte[] bytes, int offset, ReadOnlySpan<byte> expected, ReadOnlySpan<byte> replacement)
    {
        RequireRange(bytes, offset, expected.Length);
        if (!bytes.AsSpan(offset, expected.Length).SequenceEqual(expected))
            throw new InvalidDataException($"Black Knight PR1 patch site 0x{offset:X} did not match its exact input.");
        replacement.CopyTo(bytes.AsSpan(offset, replacement.Length));
    }

    private static void RelocateImportMetadata(
        byte[] bytes,
        Pe32Image pe,
        PeSection sourceSection,
        uint destinationRva,
        int importCount)
    {
        var usedLength = checked((int)ReadUInt32(bytes, sourceSection.HeaderOffset + 8));
        if (usedLength <= 0 || usedLength > sourceSection.RawSize)
            throw new InvalidDataException("Unexpected Black Knight PR1 import metadata size.");
        var metadata = bytes.AsSpan(checked((int)sourceSection.RawOffset), usedLength).ToArray();
        var delta = checked((long)destinationRva - sourceSection.VirtualAddress);

        for (var descriptorIndex = 0; descriptorIndex < importCount; descriptorIndex++)
        {
            var descriptorOffset = descriptorIndex * 20;
            var originalFirstThunk = ReadUInt32(metadata, descriptorOffset);
            var name = ReadUInt32(metadata, descriptorOffset + 12);
            if (originalFirstThunk < sourceSection.VirtualAddress || name < sourceSection.VirtualAddress)
                throw new InvalidDataException("Black Knight PR1 import metadata escaped its source section.");

            WriteUInt32(metadata, descriptorOffset, checked((uint)(originalFirstThunk + delta)));
            WriteUInt32(metadata, descriptorOffset + 12, checked((uint)(name + delta)));

            var thunkOffset = checked((int)(originalFirstThunk - sourceSection.VirtualAddress));
            while (true)
            {
                RequireRange(metadata, thunkOffset, 4);
                var thunk = ReadUInt32(metadata, thunkOffset);
                if (thunk == 0) break;
                if ((thunk & 0x80000000) == 0)
                {
                    if (thunk < sourceSection.VirtualAddress)
                        throw new InvalidDataException("Black Knight PR1 named import escaped its source section.");
                    WriteUInt32(metadata, thunkOffset, checked((uint)(thunk + delta)));
                }
                thunkOffset += 4;
            }
        }

        var destinationOffset = pe.RvaToOffset(destinationRva);
        RequireRange(bytes, destinationOffset, metadata.Length);
        Buffer.BlockCopy(metadata, 0, bytes, destinationOffset, metadata.Length);
    }

    private static void RebuildImports(byte[] bytes, Pe32Image pe, JsonElement imports)
    {
        var section = pe.RequireSection("stxt371");
        if (section.RawSize != 0x4000) throw new InvalidDataException("Unexpected Black Knight PR1 import section.");
        var data = new byte[section.RawSize];
        var cursor = checked((imports.GetArrayLength() + 1) * 20);
        var descriptors = new List<ImportDescriptor>();
        foreach (var import in imports.EnumerateArray())
        {
            var library = import.GetProperty("library").GetString()!;
            var libraryOffset = WriteAsciiZ(data, ref cursor, library);
            AlignTwo(ref cursor);
            var thunks = new List<uint>();
            foreach (var functionElement in import.GetProperty("functions").EnumerateArray())
            {
                var function = functionElement.GetString()!;
                if (function.StartsWith('#'))
                {
                    thunks.Add(0x80000000U | uint.Parse(function.AsSpan(1)));
                    continue;
                }
                var nameOffset = cursor;
                cursor += 2;
                _ = WriteAsciiZ(data, ref cursor, function);
                AlignTwo(ref cursor);
                thunks.Add(section.VirtualAddress + checked((uint)nameOffset));
            }
            var tableOffset = cursor;
            foreach (var thunk in thunks) { WriteUInt32(data, cursor, thunk); cursor += 4; }
            WriteUInt32(data, cursor, 0); cursor += 4;
            descriptors.Add(new ImportDescriptor(
                section.VirtualAddress + checked((uint)tableOffset),
                section.VirtualAddress + checked((uint)libraryOffset),
                TargetIatRva + import.GetProperty("firstThunkOffset").GetUInt32(),
                thunks));
        }
        if (cursor > data.Length) throw new InvalidDataException("Black Knight PR1 import metadata exceeds its owned section.");

        for (var index = 0; index < descriptors.Count; index++)
        {
            var descriptorOffset = index * 20;
            var descriptor = descriptors[index];
            WriteUInt32(data, descriptorOffset, descriptor.OriginalFirstThunk);
            WriteUInt32(data, descriptorOffset + 12, descriptor.Name);
            WriteUInt32(data, descriptorOffset + 16, descriptor.FirstThunk);
            var iatOffset = pe.RvaToOffset(descriptor.FirstThunk);
            for (var thunkIndex = 0; thunkIndex < descriptor.Thunks.Count; thunkIndex++)
                WriteUInt32(bytes, iatOffset + (thunkIndex * 4), descriptor.Thunks[thunkIndex]);
            WriteUInt32(bytes, iatOffset + (descriptor.Thunks.Count * 4), 0);
        }

        Buffer.BlockCopy(data, 0, bytes, checked((int)section.RawOffset), data.Length);
        WriteUInt32(bytes, section.HeaderOffset + 8, checked((uint)cursor));
    }

    private static void RetargetCallsites(byte[] bytes, Pe32Image pe, JsonElement callsites, JsonElement imports)
    {
        var slots = BuildCanonicalSlots(imports);
        var patched = new HashSet<uint>();
        foreach (var entry in callsites.EnumerateArray())
        {
            var rva = entry[0].GetUInt32();
            if (patched.Contains(rva)) continue;
            var key = ImportKey(entry[1].GetString()!, entry[2].GetString()!);
            if (!slots.TryGetValue(key, out var slotRva)) continue;
            var offset = pe.RvaToOffset(rva);
            var oldSlot = ReadUInt32(bytes, offset);
            if (oldSlot < OriginalIatStartVa || oldSlot >= OriginalIatStartVa + TargetIatSize)
                throw new InvalidDataException($"Black Knight PR1 callsite 0x{rva:X8} no longer targets its protected IAT.");
            WriteUInt32(bytes, offset, ImageBase + slotRva);
            patched.Add(rva);
        }
        if (patched.Count != 2_990) throw new InvalidDataException($"Unexpected Black Knight PR1 callsite count: {patched.Count}.");
    }

    private static void ApplyIatCorrections(
        byte[] bytes,
        Pe32Image pe,
        JsonElement corrections,
        JsonElement imports)
    {
        var slots = BuildCanonicalSlots(imports);
        var patched = new HashSet<uint>();
        var text = pe.RequireSection(".text");
        foreach (var entry in corrections.EnumerateArray())
        {
            var rva = entry[0].GetUInt32();
            if (!patched.Add(rva)) throw new InvalidDataException($"Duplicate Black Knight PR1 IAT correction at 0x{rva:X8}.");
            var expectedSlotVa = entry[1].GetUInt32();
            var key = ImportKey(entry[2].GetString()!, entry[3].GetString()!);
            if (!slots.TryGetValue(key, out var slotRva))
                throw new InvalidDataException($"Black Knight PR1 IAT correction 0x{rva:X8} names an unknown import.");
            var offset = pe.RvaToOffset(rva);
            var isIndirectBranchOperand = offset >= 2 && bytes[offset - 2] == 0xFF && bytes[offset - 1] is 0x15 or 0x25;
            var isEntryPointLoadOperand = rva is 0x334C23 or 0x334C2A &&
                offset >= 2 && bytes[offset - 2] == 0x8B && bytes[offset - 1] is 0x3D or 0x2D;
            if (rva < text.VirtualAddress || rva >= text.VirtualAddress + text.RawSize ||
                (!isIndirectBranchOperand && !isEntryPointLoadOperand))
                throw new InvalidDataException($"Black Knight PR1 IAT correction 0x{rva:X8} is not a qualified code operand.");
            var actualSlotVa = ReadUInt32(bytes, offset);
            if (actualSlotVa != expectedSlotVa)
                throw new InvalidDataException(
                    $"Black Knight PR1 IAT correction 0x{rva:X8} expected 0x{expectedSlotVa:X8}, found 0x{actualSlotVa:X8}.");
            WriteUInt32(bytes, offset, ImageBase + slotRva);
        }
        if (patched.Count != 110)
            throw new InvalidDataException($"Unexpected Black Knight PR1 IAT correction count: {patched.Count}.");
    }

    private static void RestoreTailBranches(byte[] bytes, Pe32Image pe, JsonElement branches, JsonElement imports)
    {
        var slots = BuildCanonicalSlots(imports);
        var thunks = new Dictionary<uint, uint>();
        var text = pe.RequireSection(".text");
        var textStart = checked((int)text.RawOffset);
        var textEnd = checked((int)(text.RawOffset + text.RawSize - 6));
        for (var offset = textStart; offset < textEnd; offset++)
        {
            if (bytes[offset] != 0xFF || bytes[offset + 1] != 0x25) continue;
            var slotVa = ReadUInt32(bytes, offset + 2);
            if (slotVa < OriginalIatStartVa || slotVa >= OriginalIatStartVa + TargetIatSize) continue;
            thunks.TryAdd(slotVa, ImageBase + text.VirtualAddress + checked((uint)(offset - textStart)));
        }

        var patched = 0;
        foreach (var branch in branches.EnumerateArray())
        {
            var rva = branch[0].GetUInt32();
            var opcode = branch[1].GetByte();
            var hasFollowingByte = branch[2].ValueKind != JsonValueKind.Null;
            var key = ImportKey(branch[3].GetString()!, branch[4].GetString()!);
            if (!slots.TryGetValue(key, out var slotRva)) continue;
            var offset = pe.RvaToOffset(rva);
            if (bytes[offset] is not (0xE8 or 0xE9))
                throw new InvalidDataException($"Black Knight PR1 tail branch 0x{rva:X8} is no longer relative.");
            var oldTarget = (long)ImageBase + rva + 5 + BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 1, 4));
            if (oldTarget < 0x8A3000 || oldTarget >= 0x8A8000)
                throw new InvalidDataException($"Black Knight PR1 branch 0x{rva:X8} no longer targets the protected tail.");

            var slotVa = ImageBase + slotRva;
            if (hasFollowingByte)
            {
                bytes[offset] = 0xFF;
                bytes[offset + 1] = opcode == 0xE8 ? (byte)0x15 : (byte)0x25;
                WriteUInt32(bytes, offset + 2, slotVa);
                patched++;
                continue;
            }
            if (!thunks.TryGetValue(slotVa, out var thunkVa)) continue;
            bytes[offset] = opcode;
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + 1, 4),
                checked((int)((long)thunkVa - (ImageBase + rva + 5))));
            patched++;
        }
        if (patched != 127) throw new InvalidDataException($"Unexpected Black Knight PR1 tail-branch count: {patched}.");
    }

    private static void RejectResidualTailBranches(byte[] bytes, Pe32Image pe)
    {
        var text = pe.RequireSection(".text");
        var start = checked((int)text.RawOffset);
        var end = checked((int)(text.RawOffset + text.RawSize - 5));
        for (var offset = start; offset <= end; offset++)
        {
            if (bytes[offset] is not (0xE8 or 0xE9)) continue;
            var rva = text.VirtualAddress + checked((uint)(offset - start));
            var target = (long)ImageBase + rva + 5 + BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 1, 4));
            if (target >= 0x8A3000 && target < 0x8A8000)
                throw new InvalidDataException($"Black Knight PR1 branch 0x{rva:X8} still targets removed SafeDisc code.");
        }
    }

    private static Dictionary<string, uint> BuildCanonicalSlots(JsonElement imports)
    {
        var result = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        foreach (var import in imports.EnumerateArray())
        {
            var library = import.GetProperty("library").GetString()!;
            var firstThunk = TargetIatRva + import.GetProperty("firstThunkOffset").GetUInt32();
            var index = 0U;
            foreach (var function in import.GetProperty("functions").EnumerateArray())
            {
                result[ImportKey(library, function.GetString()!)] = firstThunk + (index * 4);
                index++;
            }
        }
        return result;
    }

    private static string ImportKey(string library, string function) => $"{library.ToLowerInvariant()}|{function}";

    private static JsonDocument LoadDefinition()
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefinitionResource)
            ?? throw new InvalidOperationException("Black Knight PR1 transform definition is missing.");
        return JsonDocument.Parse(stream);
    }

    private static int WriteAsciiZ(byte[] destination, ref int cursor, string value)
    {
        var offset = cursor;
        var count = Encoding.ASCII.GetBytes(value, destination.AsSpan(cursor));
        cursor += count;
        destination[cursor++] = 0;
        return offset;
    }

    private static void AlignTwo(ref int cursor) { if ((cursor & 1) != 0) cursor++; }
    private static uint ReadUInt32(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4));
    private static void WriteUInt32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), value);
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void RequireRange(byte[] bytes, int offset, int count)
    {
        if (offset < 0 || count < 0 || offset > bytes.Length - count)
            throw new InvalidDataException("Black Knight PR1 PE range is invalid.");
    }

    private static string RequireRegularFile(string path, string description)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException($"Missing {description}.", fullPath);
        if ((File.GetAttributes(fullPath) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new InvalidDataException($"The {description} must be a regular file.");
        return fullPath;
    }

    private sealed record ImportDescriptor(uint OriginalFirstThunk, uint Name, uint FirstThunk, IReadOnlyList<uint> Thunks);

    private sealed record PeSection(string Name, int HeaderOffset, uint VirtualAddress, uint RawSize, uint RawOffset);

    private sealed class Pe32Image
    {
        private readonly IReadOnlyList<PeSection> sections;
        private Pe32Image(int peHeaderOffset, int optionalHeaderOffset, IReadOnlyList<PeSection> sections)
        {
            PeHeaderOffset = peHeaderOffset;
            OptionalHeaderOffset = optionalHeaderOffset;
            this.sections = sections;
        }

        public int PeHeaderOffset { get; }
        public int OptionalHeaderOffset { get; }

        public static Pe32Image Parse(byte[] bytes)
        {
            RequireRange(bytes, 0, 0x40);
            var peOffset = checked((int)ReadUInt32(bytes, 0x3C));
            RequireRange(bytes, peOffset, 24);
            if (ReadUInt32(bytes, peOffset) != 0x00004550) throw new InvalidDataException("Black Knight input is not a PE image.");
            var sectionCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(peOffset + 6, 2));
            var optionalSize = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(peOffset + 20, 2));
            var optionalOffset = peOffset + 24;
            RequireRange(bytes, optionalOffset, optionalSize + (sectionCount * 40));
            if (BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(optionalOffset, 2)) != 0x10B)
                throw new InvalidDataException("Black Knight input is not PE32.");
            var result = new List<PeSection>();
            var sectionOffset = optionalOffset + optionalSize;
            for (var index = 0; index < sectionCount; index++)
            {
                var header = sectionOffset + (index * 40);
                var name = Encoding.ASCII.GetString(bytes, header, 8).TrimEnd('\0');
                result.Add(new PeSection(name, header, ReadUInt32(bytes, header + 12),
                    ReadUInt32(bytes, header + 16), ReadUInt32(bytes, header + 20)));
            }
            return new Pe32Image(peOffset, optionalOffset, result);
        }

        public PeSection RequireSection(string name) => sections.FirstOrDefault(section => section.Name == name)
            ?? throw new InvalidDataException($"Black Knight PR1 section '{name}' is missing.");

        public int RvaToOffset(uint rva)
        {
            foreach (var section in sections)
            {
                if (rva >= section.VirtualAddress && rva < section.VirtualAddress + section.RawSize)
                    return checked((int)(section.RawOffset + rva - section.VirtualAddress));
            }
            throw new InvalidDataException($"Black Knight PR1 RVA 0x{rva:X8} is not mapped.");
        }
    }
}
