using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace MW4Remastered.Core.Install;

public sealed record PreparedVengeanceExecutable(string ExecutablePath, string TransformId);

public interface IVengeanceExecutableTransform
{
    PreparedVengeanceExecutable Transform(
        string discOneRoot,
        string scratchDirectory,
        CancellationToken cancellationToken = default);
}

public sealed class UnavailableVengeanceExecutableTransform : IVengeanceExecutableTransform
{
    public PreparedVengeanceExecutable Transform(
        string discOneRoot,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidDataException(
            "The media-derived Vengeance executable transform has not been qualified for release.");
    }
}

public sealed class VengeanceRetailExecutableTransform : IVengeanceExecutableTransform
{
    public const string TransformId = "vengeance-retail-01.06.11.0220-sd15020-v1";
    public const string LoaderSha256 = "b129d9689e6578a3384e68b180eb2aa59a435988d13f5b89e5fe6f8b87b3ec0b";
    public const string EncryptedImageSha256 = "2e4ca6c9bef73e26a30875ef61d6803930427d7c1856e796dc01439a9a7f5eb3";
    public const string PlayerSha256 = "eb751d527e8c9b893ae0f5dd6ade3e15bbce468c5b0331afc87d0a23ba621ff7";
    public const string OutputSha256 = "48ba4baf0894f0c14c44ddaf503cf953f59be3b919817156f1f11ea9b0df0de4";

    private static readonly uint[] CipherKey = [0xf3a3d602, 0xe7916302, 0x8658befa, 0x9b70046a];
    private static readonly uint[] MissingThunkSeeds = [0xf39716be, 0xf3971c3a];
    private static readonly byte[] DiscCheckPattern = Convert.FromHexString("84C07528B301E8");

    public PreparedVengeanceExecutable Transform(
        string discOneRoot,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(discOneRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(scratchDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        var mediaRoot = Path.GetFullPath(discOneRoot);
        var loaderPath = RequireInput(mediaRoot, "MW4.EXE", LoaderSha256);
        var imagePath = RequireInput(mediaRoot, "MW4.ICD", EncryptedImageSha256);
        _ = RequireInput(mediaRoot, "DPLAYERX.DLL", PlayerSha256);

        var image = File.ReadAllBytes(imagePath);
        var pe = new MutablePe32(image);
        if (pe.EntryPointRva != 0x309380 || pe.ImageBase != 0x00400000)
        {
            throw new InvalidDataException("The Vengeance retail ICD PE layout is not the qualified revision.");
        }

        var loaderLength = checked((uint)new FileInfo(loaderPath).Length);
        foreach (var section in pe.Sections.Where(section => IsEncryptedSection(section.Name)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteSectionEndMarkerBeforeDecode(image, pe, section, loaderLength);
            SafeDisc15020ImageCipher.DecodeSection(
                image.AsSpan(section.RawOffset, section.RawSize),
                section.VirtualSize,
                CipherKey);
        }

        RepairImportNamesAndThunks(image, pe);
        PermuteProtectedThunkTables(image, pe);
        var ranges = RebuildProtectedIatRanges(image, pe);
        RepairIndirectImportReferences(image, pe, ranges, cancellationToken);
        NormalizePortableExecutable(image, pe);
        PatchDiscCheck(image, pe.RawEnd);

        var output = image.AsSpan(0, pe.RawEnd).ToArray();
        var outputHash = ComputeSha256(output);
        if (!string.Equals(outputHash, OutputSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Vengeance transform produced unexpected SHA-256: {outputHash}");
        }

        Directory.CreateDirectory(scratchDirectory);
        var outputPath = Path.Combine(Path.GetFullPath(scratchDirectory), "MW4.exe");
        File.WriteAllBytes(outputPath, output);
        return new PreparedVengeanceExecutable(outputPath, TransformId);
    }

    private static string RequireInput(string mediaRoot, string relativePath, string expectedHash)
    {
        var path = Path.GetFullPath(Path.Combine(mediaRoot, relativePath));
        var relative = Path.GetRelativePath(mediaRoot, path);
        if (Path.IsPathRooted(relative) || relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Vengeance transform input escaped the validated media root.");
        }
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required Vengeance transform input is missing: {relativePath}", path);
        }
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"Vengeance transform input cannot be a reparse point: {relativePath}");
        }
        using var stream = File.OpenRead(path);
        var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported Vengeance {relativePath} SHA-256: {actualHash}");
        }
        return path;
    }

    private static bool IsEncryptedSection(string name) => name is not (
        ".rsrc" or ".bss" or "BSS" or ".tls" or ".idata" or ".rdata" or ".edata" or ".reloc");

    private static void WriteSectionEndMarkerBeforeDecode(
        Span<byte> image,
        MutablePe32 pe,
        PeSection section,
        uint loaderLength)
    {
        var index = pe.Sections.IndexOf(section);
        if (index < 0 || index + 1 >= pe.Sections.Count)
        {
            return;
        }
        var markerOffset = pe.Sections[index + 1].RawOffset - sizeof(uint);
        if (markerOffset < 0 || markerOffset + sizeof(uint) > image.Length ||
            BinaryPrimitives.ReadUInt32LittleEndian(image[markerOffset..]) != 0)
        {
            return;
        }
        BinaryPrimitives.WriteUInt32BigEndian(image[markerOffset..], loaderLength);
    }

    private static void RepairImportNamesAndThunks(Span<byte> image, MutablePe32 pe)
    {
        var seedIndex = 0;
        foreach (var descriptor in pe.ReadImportDescriptors())
        {
            var library = ReadAsciiZ(image, pe.RvaToOffset(descriptor.NameRva));
            if (!IsProtectedLibrary(library))
            {
                continue;
            }
            if (seedIndex >= MissingThunkSeeds.Length)
            {
                throw new InvalidDataException("Vengeance import metadata contains too many protected thunk tables.");
            }

            var thunkOffset = pe.RvaToOffset(descriptor.OriginalFirstThunkRva);
            if (BinaryPrimitives.ReadUInt32LittleEndian(image[thunkOffset..]) == 0)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(image[thunkOffset..], MissingThunkSeeds[seedIndex]);
            }
            seedIndex++;

            for (var cursor = thunkOffset; ; cursor += sizeof(uint))
            {
                var encodedRva = BinaryPrimitives.ReadUInt32LittleEndian(image[cursor..]);
                if (encodedRva == 0)
                {
                    break;
                }
                var decodedRva = encodedRva ^ CipherKey[0];
                BinaryPrimitives.WriteUInt32LittleEndian(image[cursor..], decodedRva);
                var importNameOffset = pe.RvaToOffset(decodedRva);
                BinaryPrimitives.WriteUInt16LittleEndian(image[importNameOffset..], 0);
                DecodeRollingXorString(image[(importNameOffset + sizeof(ushort))..]);
            }
        }
        if (seedIndex != MissingThunkSeeds.Length)
        {
            throw new InvalidDataException("Vengeance import metadata did not contain both protected thunk tables.");
        }
    }

    private static void DecodeRollingXorString(Span<byte> encoded)
    {
        byte previous = 0x58;
        for (var index = 0; index < encoded.Length; index++)
        {
            var current = encoded[index];
            var decoded = (byte)(current ^ previous);
            encoded[index] = decoded;
            if (decoded == 0)
            {
                return;
            }
            previous = current;
        }
        throw new InvalidDataException("Vengeance import name is not null terminated.");
    }

    private static void PermuteProtectedThunkTables(Span<byte> image, MutablePe32 pe)
    {
        foreach (var descriptor in pe.ReadImportDescriptors())
        {
            var library = ReadAsciiZ(image, pe.RvaToOffset(descriptor.NameRva));
            if (!IsProtectedLibrary(library))
            {
                continue;
            }
            var thunkOffset = pe.RvaToOffset(descriptor.OriginalFirstThunkRva);
            var values = new List<uint>();
            for (var cursor = thunkOffset; ; cursor += sizeof(uint))
            {
                var value = BinaryPrimitives.ReadUInt32LittleEndian(image[cursor..]);
                if (value == 0)
                {
                    break;
                }
                values.Add(value);
            }

            var permutation = Enumerable.Range(0, values.Count).ToArray();
            var bitCount = 32 - BitOperations.LeadingZeroCount((uint)values.Count);
            var state = CipherKey[0];
            for (var index = 0; index < values.Count; index++)
            {
                state = unchecked((state * 0x35e85a6d) + 0x361962e9);
                var candidate = unchecked((uint)(((state >> bitCount) * (uint)values.Count))) >> (32 - bitCount);
                if (candidate != index)
                {
                    (permutation[index], permutation[candidate]) = (permutation[candidate], permutation[index]);
                }
            }

            for (var destination = 0; destination < values.Count; destination++)
            {
                var source = Array.IndexOf(permutation, destination);
                BinaryPrimitives.WriteUInt32LittleEndian(
                    image[(thunkOffset + destination * sizeof(uint))..],
                    values[source]);
            }
        }
    }

    private static ProtectedIatRanges RebuildProtectedIatRanges(Span<byte> image, MutablePe32 pe)
    {
        var user = new List<IatRange>();
        var kernel = new List<IatRange>();
        foreach (var descriptor in pe.ReadImportDescriptors())
        {
            var nameOffset = pe.RvaToOffset(descriptor.NameRva);
            var library = ReadAsciiZ(image, nameOffset);
            List<IatRange>? target = null;
            if (library.StartsWith("USER", StringComparison.OrdinalIgnoreCase))
            {
                target = user;
            }
            else if (library.StartsWith("KERN", StringComparison.OrdinalIgnoreCase))
            {
                target = kernel;
                Encoding.ASCII.GetBytes("KeRNeL").CopyTo(image[nameOffset..]);
            }
            if (target is null)
            {
                continue;
            }

            var sourceOffset = pe.RvaToOffset(descriptor.OriginalFirstThunkRva);
            var destinationOffset = pe.RvaToOffset(descriptor.FirstThunkRva);
            var count = 0;
            while (true)
            {
                var value = BinaryPrimitives.ReadUInt32LittleEndian(image[(sourceOffset + count * sizeof(uint))..]);
                BinaryPrimitives.WriteUInt32LittleEndian(image[(destinationOffset + count * sizeof(uint))..], value);
                if (value == 0)
                {
                    break;
                }
                count++;
            }
            target.Add(new IatRange(descriptor.FirstThunkRva, checked(descriptor.FirstThunkRva + (uint)(count * sizeof(uint)))));
        }
        return new ProtectedIatRanges(user, kernel);
    }

    private static void RepairIndirectImportReferences(
        Span<byte> image,
        MutablePe32 pe,
        ProtectedIatRanges ranges,
        CancellationToken cancellationToken)
    {
        foreach (var section in pe.Sections.Where(section => IsEncryptedSection(section.Name)))
        {
            var end = checked(section.RawOffset + section.RawSize - sizeof(uint));
            for (var opcodeOffset = section.RawOffset; opcodeOffset + 6 <= end; opcodeOffset++)
            {
                if ((opcodeOffset & 0xffff) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                if (image[opcodeOffset] != 0xff || image[opcodeOffset + 1] != 0x15)
                {
                    continue;
                }
                var localOffset = checked((uint)(opcodeOffset - section.RawOffset));
                var mixed = RepairFilterSecond(localOffset) ^ localOffset;
                mixed = RepairFilterFirst(mixed) ^ mixed;
                if ((mixed & 3) >= 2)
                {
                    continue;
                }

                var operandOffset = opcodeOffset + 2;
                var absolute = BinaryPrimitives.ReadUInt32LittleEndian(image[operandOffset..]);
                if (absolute < pe.ImageBase)
                {
                    continue;
                }
                var rva = absolute - pe.ImageBase;
                if (TryRepairIatReference(rva, localOffset, ranges.Kernel, out var repaired) ||
                    TryRepairIatReference(rva, localOffset, ranges.User, out repaired))
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(image[operandOffset..], pe.ImageBase + repaired);
                }
            }
        }
    }

    private static bool TryRepairIatReference(uint rva, uint localOffset, IReadOnlyList<IatRange> ranges, out uint repaired)
    {
        foreach (var range in ranges)
        {
            if (rva < range.StartRva || rva > range.EndRva)
            {
                continue;
            }
            var count = (range.EndRva - range.StartRva) / sizeof(uint);
            var index = (rva - range.StartRva) / sizeof(uint);
            var rotation = unchecked(localOffset + CipherKey[0]) % count;
            var repairedIndex = (index + count - rotation) % count;
            repaired = range.StartRva + repairedIndex * sizeof(uint);
            return true;
        }
        repaired = 0;
        return false;
    }

    private static uint RepairFilterFirst(uint value)
    {
        unchecked
        {
            value ^= 0x5b9854e5;
            value = BitOperations.RotateLeft(value, 0xf6);
            value = BitOperations.RotateLeft(value, 0x0d);
            value -= 0x2a2d6ce5;
            value += 0x35837336;
            value += 0x458d3c9a;
            value = BitOperations.RotateRight(value, 0x3e);
            value ^= 0x0de053cc;
            value = BitOperations.RotateLeft(value, 0xee);
            value = BitOperations.RotateRight(value, 0x21);
            value ^= 0x434443d3;
            value += 0x78bc4570;
            value = BitOperations.RotateRight(value, 0xb6);
            value -= 0x5ca571e6;
            value -= 0x205452b9;
            value = BitOperations.RotateRight(value, 0x8a);
            value ^= 0x62740400;
            value = BitOperations.RotateRight(value, 0x49);
            value -= 0x2cf8602f;
            value ^= 0x34ec4dc2;
            value -= 0x1f780401;
            value = BitOperations.RotateLeft(value, 0x1a);
            value += 0x564e1569;
            value ^= 0x3bc21dbb;
            value -= 0x7e112968;
            value = BitOperations.RotateRight(value, 0x1e);
            value += 0x2a73724c;
            value = BitOperations.RotateRight(value, 0xe6);
            value ^= 0x00b50159;
            value -= 0x6861450d;
            value -= 0x61fa3d2b;
            value -= 0x29004d48;
            value = BitOperations.RotateRight(value, 0x8b);
            value += 0x14bc4344;
            value -= 0x41f7399a;
            value ^= 0x48117e74;
            value += 0x79ff47ef;
            return BitOperations.RotateLeft(value, 0xa5);
        }
    }

    private static uint RepairFilterSecond(uint value)
    {
        unchecked
        {
            value = BitOperations.RotateRight(value, 0xe5);
            value ^= 0x14e4177a;
            value = BitOperations.RotateLeft(value, 0xf4);
            value--;
            value--;
            value = BitOperations.RotateRight(value, 0xe5);
            value += 0x530b69b1;
            value += 0x35837336;
            value ^= 0x458d3c9a;
            value = BitOperations.RotateRight(value, 0x13);
            value++;
            value ^= 0x468a5a49;
            value++;
            value += 0x5b5b6721;
            value ^= 0x434443d3;
            value = BitOperations.RotateRight(value, 0xb6);
            value = BitOperations.RotateRight(value, 0xb9);
            value -= 0x2ce84b78;
            value = 0u - value;
            value += 0x66c55649;
            value++;
            value = BitOperations.RotateRight(value, 0xc2);
            value++;
            value--;
            value++;
            value -= 0x1c7b6c09;
            value = 0u - value;
            value--;
            value--;
            value = BitOperations.RotateLeft(value, 0x4c);
            value = 0u - value;
            value = 0u - value;
            value--;
            value ^= 0x73d94de6;
            value = 0u - value;
            value ^= 0x6861450d;
            value = 0u - value;
            value = BitOperations.RotateLeft(value, 0x8b);
            value -= 0x41f7399a;
            value = BitOperations.RotateLeft(value, 0x89);
            value--;
            value -= 0x79ff47ef;
            value--;
            value += 0x02903fff;
            value ^= 0x0e7607da;
            value--;
            value = BitOperations.RotateRight(value, 0x8c);
            value--;
            value = BitOperations.RotateRight(value, 0xa5);
            value = BitOperations.RotateLeft(value, 0x23);
            return value + 1;
        }
    }

    private static void NormalizePortableExecutable(Span<byte> image, MutablePe32 pe)
    {
        image[pe.PeOffset - 2] = 0x2b;
        image[pe.PeOffset - 1] = 0xad;
        Encoding.ASCII.GetBytes("1911").CopyTo(image[(pe.PeOffset + 8)..]);
        BinaryPrimitives.WriteUInt32BigEndian(image[(pe.RawEnd - sizeof(uint))..], checked((uint)pe.RawEnd));
    }

    private static void PatchDiscCheck(Span<byte> image, int rawEnd)
    {
        var match = -1;
        for (var index = 0; index <= rawEnd - DiscCheckPattern.Length; index++)
        {
            if (!image.Slice(index, DiscCheckPattern.Length).SequenceEqual(DiscCheckPattern))
            {
                continue;
            }
            if (match >= 0)
            {
                throw new InvalidDataException("Vengeance disc-check pattern is not unique.");
            }
            match = index;
        }
        if (match < 0)
        {
            throw new InvalidDataException("Vengeance disc-check pattern was not found.");
        }
        image[match + 2] = 0xeb;
    }

    private static bool IsProtectedLibrary(string library) =>
        library.StartsWith("USER", StringComparison.OrdinalIgnoreCase) ||
        library.StartsWith("KERN", StringComparison.OrdinalIgnoreCase);

    private static string ReadAsciiZ(ReadOnlySpan<byte> image, int offset)
    {
        var end = offset;
        while (end < image.Length && image[end] != 0)
        {
            end++;
        }
        if (end == image.Length)
        {
            throw new InvalidDataException("PE string is not null terminated.");
        }
        return Encoding.ASCII.GetString(image[offset..end]);
    }

    private static string ComputeSha256(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed record IatRange(uint StartRva, uint EndRva);
    private sealed record ProtectedIatRanges(IReadOnlyList<IatRange> User, IReadOnlyList<IatRange> Kernel);

    private sealed record PeSection(string Name, int HeaderOffset, int VirtualSize, uint VirtualAddress, int RawSize, int RawOffset);
    private sealed record ImportDescriptor(uint OriginalFirstThunkRva, uint NameRva, uint FirstThunkRva);

    private sealed class MutablePe32
    {
        private readonly byte[] image;

        public MutablePe32(byte[] image)
        {
            this.image = image;
            if (image.Length < 0x100 || BinaryPrimitives.ReadUInt16LittleEndian(image) != 0x5a4d)
            {
                throw new InvalidDataException("Vengeance ICD is not a valid DOS/PE image.");
            }
            PeOffset = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(0x3c)));
            if (PeOffset < 2 || PeOffset + 0xf8 > image.Length ||
                BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(PeOffset)) != 0x00004550)
            {
                throw new InvalidDataException("Vengeance ICD has an invalid PE header.");
            }
            if (BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(PeOffset + 24)) != 0x10b)
            {
                throw new InvalidDataException("Vengeance ICD must be a PE32 image.");
            }
            var sectionCount = BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(PeOffset + 6));
            var optionalHeaderSize = BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(PeOffset + 20));
            var sectionOffset = checked(PeOffset + 24 + optionalHeaderSize);
            var sections = new List<PeSection>(sectionCount);
            for (var index = 0; index < sectionCount; index++)
            {
                var header = checked(sectionOffset + index * 40);
                if (header + 40 > image.Length)
                {
                    throw new InvalidDataException("Vengeance ICD section table is truncated.");
                }
                var nameBytes = image.AsSpan(header, 8);
                var nameLength = nameBytes.IndexOf((byte)0);
                if (nameLength < 0) nameLength = nameBytes.Length;
                var section = new PeSection(
                    Encoding.ASCII.GetString(nameBytes[..nameLength]),
                    header,
                    checked((int)BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(header + 8))),
                    BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(header + 12)),
                    checked((int)BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(header + 16))),
                    checked((int)BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(header + 20))));
                if (section.RawOffset < 0 || section.RawSize < 0 || section.RawOffset + section.RawSize > image.Length)
                {
                    throw new InvalidDataException($"Vengeance ICD section {section.Name} is outside the file.");
                }
                sections.Add(section);
            }
            Sections = sections;
            RawEnd = sections.Max(section => checked(section.RawOffset + section.RawSize));
            ImageBase = BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(PeOffset + 0x34));
            EntryPointRva = BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(PeOffset + 0x28));
            ImportDirectoryRva = BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(PeOffset + 0x80));
        }

        public int PeOffset { get; }
        public uint ImageBase { get; }
        public uint EntryPointRva { get; }
        public uint ImportDirectoryRva { get; }
        public List<PeSection> Sections { get; }
        public int RawEnd { get; }

        public int RvaToOffset(uint rva)
        {
            foreach (var section in Sections)
            {
                var extent = Math.Max(section.VirtualSize, section.RawSize);
                if (rva >= section.VirtualAddress && rva - section.VirtualAddress < extent)
                {
                    return checked(section.RawOffset + (int)(rva - section.VirtualAddress));
                }
            }
            if (rva < Sections.Min(section => section.VirtualAddress))
            {
                return checked((int)rva);
            }
            throw new InvalidDataException($"PE RVA 0x{rva:x8} is outside mapped sections.");
        }

        public IReadOnlyList<ImportDescriptor> ReadImportDescriptors()
        {
            var descriptors = new List<ImportDescriptor>();
            var offset = RvaToOffset(ImportDirectoryRva);
            while (true)
            {
                if (offset + 20 > image.Length)
                {
                    throw new InvalidDataException("Vengeance import directory is truncated.");
                }
                var originalThunk = BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(offset));
                var name = BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(offset + 12));
                var firstThunk = BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(offset + 16));
                if (originalThunk == 0 && name == 0)
                {
                    return descriptors;
                }
                descriptors.Add(new ImportDescriptor(originalThunk, name, firstThunk));
                offset += 20;
            }
        }
    }
}
