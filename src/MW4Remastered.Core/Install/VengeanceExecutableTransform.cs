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

internal enum SafeDisc15020TitlePatch
{
    Vengeance,
    VengeancePatch3,
    Mercenaries,
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
        CancellationToken cancellationToken = default) => TransformCore(
            discOneRoot,
            scratchDirectory,
            "MW4.EXE",
            "MW4.ICD",
            "DPLAYERX.DLL",
            "MW4.exe",
            TransformId,
            LoaderSha256,
            EncryptedImageSha256,
            PlayerSha256,
            OutputSha256,
            0x309380,
            CipherKey,
            MissingThunkSeeds,
            0x58,
            false,
            SafeDisc15020TitlePatch.Vengeance,
            cancellationToken);

    internal static PreparedVengeanceExecutable TransformCore(
        string mediaRootPath,
        string scratchDirectory,
        string loaderFileName,
        string encryptedImageFileName,
        string playerFileName,
        string outputFileName,
        string transformId,
        string loaderSha256,
        string encryptedImageSha256,
        string playerSha256,
        string outputSha256,
        uint expectedEntryPointRva,
        uint[] cipherKey,
        uint[] missingThunkSeeds,
        byte importNameSeed,
        bool chainImportNameFromDecodedByte,
        SafeDisc15020TitlePatch titlePatch,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(scratchDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        var mediaRoot = Path.GetFullPath(mediaRootPath);
        var loaderPath = RequireInput(mediaRoot, loaderFileName, loaderSha256);
        var imagePath = RequireInput(mediaRoot, encryptedImageFileName, encryptedImageSha256);
        _ = RequireInput(mediaRoot, playerFileName, playerSha256);

        var image = File.ReadAllBytes(imagePath);
        var pe = new MutablePe32(image);
        if (pe.EntryPointRva != expectedEntryPointRva || pe.ImageBase != 0x00400000)
        {
            throw new InvalidDataException("The SafeDisc image PE layout is not the qualified revision.");
        }

        var loaderLength = checked((uint)new FileInfo(loaderPath).Length);
        foreach (var section in pe.Sections.Where(section => IsEncryptedSection(section.Name)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteSectionEndMarkerBeforeDecode(image, pe, section, loaderLength);
            SafeDisc15020ImageCipher.DecodeSection(
                image.AsSpan(section.RawOffset, section.RawSize),
                section.VirtualSize,
                cipherKey,
                titlePatch == SafeDisc15020TitlePatch.Mercenaries
                    ? SafeDisc15020SecondLayerProfile.Mercenaries
                    : SafeDisc15020SecondLayerProfile.Vengeance);
        }

        RepairImportNamesAndThunks(
            image,
            pe,
            cipherKey,
            missingThunkSeeds,
            importNameSeed,
            chainImportNameFromDecodedByte);
        PermuteProtectedThunkTables(image, pe, cipherKey);
        var ranges = RebuildProtectedIatRanges(image, pe);
        RepairIndirectImportReferences(image, pe, ranges, cipherKey, titlePatch, cancellationToken);
        NormalizePortableExecutable(image, pe);
        ApplyTitlePatch(image, pe, titlePatch);

        var output = image.AsSpan(0, pe.RawEnd).ToArray();
        var outputHash = ComputeSha256(output);
        if (!string.Equals(outputHash, outputSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"SafeDisc transform produced unexpected SHA-256: {outputHash}");
        }

        Directory.CreateDirectory(scratchDirectory);
        var outputPath = Path.Combine(Path.GetFullPath(scratchDirectory), outputFileName);
        File.WriteAllBytes(outputPath, output);
        return new PreparedVengeanceExecutable(outputPath, transformId);
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
            throw new FileNotFoundException($"Required SafeDisc transform input is missing: {relativePath}", path);
        }
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"SafeDisc transform input cannot be a reparse point: {relativePath}");
        }
        using var stream = File.OpenRead(path);
        var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported SafeDisc input {relativePath} SHA-256: {actualHash}");
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

    private static void RepairImportNamesAndThunks(
        Span<byte> image,
        MutablePe32 pe,
        IReadOnlyList<uint> cipherKey,
        IReadOnlyList<uint> missingThunkSeeds,
        byte importNameSeed,
        bool chainFromDecodedByte)
    {
        var seedIndex = 0;
        var decodedImportNames = new HashSet<uint>();
        var descriptors = new List<(ImportDescriptor Descriptor, string Library)>();
        foreach (var descriptor in pe.ReadImportDescriptors())
        {
            descriptors.Add((descriptor, ReadAsciiZ(image, pe.RvaToOffset(descriptor.NameRva))));
        }
        foreach (var item in descriptors)
        {
            var descriptor = item.Descriptor;
            if (!IsProtectedLibrary(item.Library))
            {
                continue;
            }
            if (seedIndex >= missingThunkSeeds.Count)
            {
                throw new InvalidDataException("Vengeance import metadata contains too many protected thunk tables.");
            }

            var thunkOffset = pe.RvaToOffset(descriptor.OriginalFirstThunkRva);
            if (BinaryPrimitives.ReadUInt32LittleEndian(image[thunkOffset..]) == 0)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(image[thunkOffset..], missingThunkSeeds[seedIndex]);
            }
            seedIndex++;

            for (var cursor = thunkOffset; ; cursor += sizeof(uint))
            {
                var encodedRva = BinaryPrimitives.ReadUInt32LittleEndian(image[cursor..]);
                if (encodedRva == 0)
                {
                    break;
                }
                var decodedRva = encodedRva ^ cipherKey[0];
                BinaryPrimitives.WriteUInt32LittleEndian(image[cursor..], decodedRva);
                if (!decodedImportNames.Add(decodedRva))
                {
                    continue;
                }
                var importNameOffset = pe.RvaToOffset(decodedRva);
                BinaryPrimitives.WriteUInt16LittleEndian(image[importNameOffset..], 0);
                DecodeRollingXorString(
                    image[(importNameOffset + sizeof(ushort))..],
                    importNameSeed,
                    chainFromDecodedByte);
            }
        }
        if (seedIndex != missingThunkSeeds.Count)
        {
            throw new InvalidDataException(
                $"SafeDisc import metadata contained {seedIndex} protected thunk tables; expected {missingThunkSeeds.Count}.");
        }
    }

    private static void DecodeRollingXorString(Span<byte> encoded, byte seed, bool chainFromDecodedByte)
    {
        var previous = seed;
        for (var index = 0; index < encoded.Length; index++)
        {
            var current = encoded[index];
            var decoded = (byte)(current ^ previous);
            encoded[index] = decoded;
            if (decoded == 0)
            {
                return;
            }
            previous = chainFromDecodedByte ? decoded : current;
        }
        throw new InvalidDataException("Vengeance import name is not null terminated.");
    }

    private static void PermuteProtectedThunkTables(
        Span<byte> image,
        MutablePe32 pe,
        IReadOnlyList<uint> cipherKey)
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
            var state = cipherKey[0];
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
        IReadOnlyList<uint> cipherKey,
        SafeDisc15020TitlePatch titlePatch,
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
                var mixed = (titlePatch == SafeDisc15020TitlePatch.Mercenaries
                    ? MercenariesRepairFilterSecond(localOffset)
                    : RepairFilterSecond(localOffset)) ^ localOffset;
                mixed = (titlePatch == SafeDisc15020TitlePatch.Mercenaries
                    ? MercenariesRepairFilterFirst(mixed)
                    : RepairFilterFirst(mixed)) ^ mixed;
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
                if (TryRepairIatReference(rva, localOffset, ranges.Kernel, cipherKey[0], out var repaired) ||
                    TryRepairIatReference(rva, localOffset, ranges.User, cipherKey[0], out repaired))
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(image[operandOffset..], pe.ImageBase + repaired);
                }
            }
        }
    }

    private static bool TryRepairIatReference(
        uint rva,
        uint localOffset,
        IReadOnlyList<IatRange> ranges,
        uint keyWord,
        out uint repaired)
    {
        foreach (var range in ranges)
        {
            if (rva < range.StartRva || rva > range.EndRva)
            {
                continue;
            }
            var count = (range.EndRva - range.StartRva) / sizeof(uint);
            var index = (rva - range.StartRva) / sizeof(uint);
            var rotation = unchecked(localOffset + keyWord) % count;
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

    private static uint MercenariesRepairFilterFirst(uint value)
    {
        unchecked
        {
            value -= 0x39fc4094;
            value ^= 0x0e3405cb;
            value = BitOperations.RotateLeft(value, 0x62);
            value = BitOperations.RotateLeft(value, 0xb4);
            value += 0x635f0c0c;
            value += 0x260e2dd9;
            value += 0x55df3105;
            value -= 0x3f3c38a9;
            value -= 0x6c2121e8;
            value -= 0x6ac842fb;
            value = BitOperations.RotateRight(value, 0x36);
            value -= 0x05814930;
            value += 0x43312af2;
            value = BitOperations.RotateRight(value, 0xe4);
            value += 0x0dd219e5;
            value += 0x6ea07aa2;
            value += 0x7ed36e9a;
            value += 0x23c0027d;
            value -= 0x1e0c5c37;
            value = BitOperations.RotateLeft(value, 0x0b);
            value = BitOperations.RotateRight(value, 6);
            value ^= 0x7d842a41;
            value += 0x4c4a6a8d;
            value = BitOperations.RotateLeft(value, 0xd9);
            value = BitOperations.RotateLeft(value, 0x24);
            value = BitOperations.RotateLeft(value, 3);
            value = BitOperations.RotateRight(value, 0xd9);
            value ^= 0x489777be;
            value ^= 0x6b8b347b;
            value = BitOperations.RotateLeft(value, 1);
            value -= 0x5d5f375e;
            value = BitOperations.RotateRight(value, 0xa7);
            value += 0x26357cb6;
            value = BitOperations.RotateLeft(value, 0xec);
            value = BitOperations.RotateLeft(value, 0xd3);
            value += 0x5c1108d2;
            value ^= 0x661d761d;
            value -= 0x45876e80;
            value -= 0x3dfe4780;
            return value + 0x07b10fd8;
        }
    }

    private static uint MercenariesRepairFilterSecond(uint value)
    {
        unchecked
        {
            value -= 0x39fc4094;
            value = BitOperations.RotateLeft(value, 0x4e);
            value += 0x653e5062;
            value--;
            value ^= 0x3e3342af;
            value = BitOperations.RotateRight(value, 0xd9);
            value = BitOperations.RotateLeft(value, 0xa9);
            value ^= 0x6ac842fb;
            value++;
            value--;
            value += 0x43312af2;
            value--;
            value = BitOperations.RotateRight(value, 0xe5);
            value = BitOperations.RotateRight(value, 0xe4);
            value = BitOperations.RotateRight(value, 0xa2);
            value = BitOperations.RotateRight(value, 0x9a);
            value -= 0x23c0027d;
            value = BitOperations.RotateRight(value, 0x37);
            value = BitOperations.RotateRight(value, 0x0b);
            value ^= 0x78996a6d;
            value++;
            value--;
            value--;
            value -= 0x31d07503;
            value = 0u - value;
            value = BitOperations.RotateLeft(value, 0xbe);
            value ^= 0x6b8b347b;
            value = BitOperations.RotateLeft(value, 0xb7);
            value = BitOperations.RotateLeft(value, 1);
            value = BitOperations.RotateRight(value, 0x5e);
            value ^= 0x3e7c7ca7;
            value -= 0x0de051b1;
            value--;
            value ^= 0x2b5814d3;
            value += 0x2a641ba9;
            value--;
            value--;
            value++;
            value = BitOperations.RotateLeft(value, 0x7a);
            value = 0u - value;
            value -= 0x3dfe4780;
            value = 0u - value;
            value ^= 0x17b2105b;
            value += 0x3a4c2635;
            value++;
            value = BitOperations.RotateLeft(value, 0x5c);
            value -= 0x791b7041;
            value--;
            value -= 0x074f611b;
            value ^= 0x05b4300f;
            value = 0u - value;
            value++;
            value++;
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

    private static void ApplyTitlePatch(Span<byte> image, MutablePe32 pe, SafeDisc15020TitlePatch titlePatch)
    {
        switch (titlePatch)
        {
            case SafeDisc15020TitlePatch.Vengeance:
                PatchDiscCheck(image, pe.RawEnd);
                break;
            case SafeDisc15020TitlePatch.VengeancePatch3:
                PatchVengeancePatch3LegacyClients(image, pe.RawEnd);
                RemoveImportDescriptors(image, pe, "CdaC14BA.dll", "ARTPCLNT.dll");
                break;
            case SafeDisc15020TitlePatch.Mercenaries:
                PatchMercenariesEntitlement(image, pe);
                break;
            default:
                throw new InvalidDataException($"Unsupported SafeDisc title patch: {titlePatch}");
        }
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

    private static void PatchMercenariesEntitlement(Span<byte> image, MutablePe32 pe)
    {
        var gatePattern = Convert.FromHexString("A1446B7D008B0081EC8002000056573305406B7D008B480C6A0181F131957385");
        var gate = FindUniquePattern(image[..pe.RawEnd], gatePattern, "Mercenaries entitlement gate");
        var patchOffset = gate + 0x15;
        Convert.FromHexString("E946000000").CopyTo(image[patchOffset..]);

        // Mercenaries delegates EULA/setup acceptance to EBUEula.dll. That
        // legacy client faults on current Windows even when its two arguments
        // are ABI-correct. Setup has already obtained the user's installation
        // choice, and both mirrored callers consume only AL, so return true at
        // the exact client calls and let their existing cleanup discard the two
        // arguments. No legacy helper is loaded during normal game launch.
        ReplaceAllExact(
            image[..pe.RawEnd],
            Convert.FromHexString("8B44241C8B4C24145051E81DF7FFFF83C40884C0"),
            Convert.FromHexString("8B44241C8B4C24145051B00190909083C40884C0"),
            expectedCount: 1,
            "Mercenaries primary EULA/setup validation call");
        ReplaceAllExact(
            image[..pe.RawEnd],
            Convert.FromHexString("8B4424148B4C24185051E85659D2FF83C40884C0"),
            Convert.FromHexString("8B4424148B4C24185051B00190909083C40884C0"),
            expectedCount: 1,
            "Mercenaries mirrored EULA/setup validation call");

        // Like Vengeance, the Mercenaries startup path separately validates
        // that CDPath resolves to an original-disc volume label. Installed
        // media-derived trees cannot and should not require a mounted ISO.
        // Both callers use only AL and take no arguments.
        ReplaceAllExact(
            image[..pe.RawEnd],
            Convert.FromHexString("B301E8A4F8FFFF"),
            Convert.FromHexString("B301B001909090"),
            expectedCount: 1,
            "Mercenaries primary installed-media validation call");
        ReplaceAllExact(
            image[..pe.RawEnd],
            Convert.FromHexString("E883F8FFFF84C074DF"),
            Convert.FromHexString("B00190909084C074DF"),
            expectedCount: 1,
            "Mercenaries retry installed-media validation call");

        RemoveImportDescriptors(image, pe, "CdaC14BA.dll");
    }

    private static void RemoveImportDescriptors(Span<byte> image, MutablePe32 pe, params string[] libraryNames)
    {
        var requested = libraryNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (requested.Count != libraryNames.Length)
        {
            throw new ArgumentException("Import descriptor names must be unique.", nameof(libraryNames));
        }

        var descriptors = pe.ReadImportDescriptors();
        var matches = new List<int>();
        for (var index = 0; index < descriptors.Count; index++)
        {
            var library = ReadAsciiZ(image, pe.RvaToOffset(descriptors[index].NameRva));
            if (requested.Contains(library))
            {
                matches.Add(index);
                requested.Remove(library);
            }
        }
        if (requested.Count > 0)
        {
            throw new InvalidDataException("Required import descriptors were not found: " + string.Join(", ", requested));
        }

        foreach (var descriptorIndex in matches.OrderDescending())
        {
            var descriptorOffset = descriptors[descriptorIndex].HeaderOffset;
            var bytesToMove = checked((descriptors.Count - descriptorIndex) * 20);
            image.Slice(descriptorOffset + 20, bytesToMove).CopyTo(image[descriptorOffset..]);
        }
    }

    private static void PatchVengeancePatch3LegacyClients(Span<byte> image, int rawEnd)
    {
        // CdaSysUpgrade takes no arguments and ends in a plain RET. The caller
        // treats any nonzero EAX as success, so preserve ESP and return one.
        // Static disassembly of the exact Patch 3 client locks this ABI; adding
        // stack cleanup here discards the caller's saved ESI/EDI registers.
        ReplaceAllExact(
            image[..rawEnd],
            Convert.FromHexString("FF1544807300"),
            Convert.FromHexString("6A0158909090"),
            expectedCount: 2,
            "Patch 3 C-Dilla upgrade calls");

        // AutoRTPatch32 takes three arguments but also ends in a plain RET; its
        // callers perform their own shared stack cleanup after the call. Patch
        // 3 is already applied by the installer, so return zero without
        // changing ESP or loading the obsolete client DLL.
        ReplaceAllExact(
            image[..rawEnd],
            Convert.FromHexString("FF1538807300"),
            Convert.FromHexString("33C090909090"),
            expectedCount: 2,
            "Patch 3 AutoRTPatch calls");

        // Patch 3's EULA helper reads FIRSTRUN from the game's direct HKCU
        // settings record, then rejects the installation unless a separate
        // machine-wide HKLM product record exists. Normal game launch must not
        // elevate merely to recreate that legacy setup artifact. Both mirrored
        // callers treat AL=true as accepted/valid and clean their own two
        // arguments, so return true locally without loading the helper.
        ReplaceAllExact(
            image[..rawEnd],
            Convert.FromHexString("E86FF8FFFF"),
            Convert.FromHexString("B001909090"),
            expectedCount: 1,
            "Patch 3 primary EULA/setup validation call");
        ReplaceAllExact(
            image[..rawEnd],
            Convert.FromHexString("E8E548CEFF"),
            Convert.FromHexString("B001909090"),
            expectedCount: 1,
            "Patch 3 mirrored EULA/setup validation call");

        // A separate Patch 3 routine reads CDPath and accepts it only when the
        // backing volume is labelled MECHWARR_01 or MECHWARR_02. A complete
        // media-derived installation necessarily points CDPath at its installed
        // tree, not at a permanently mounted original disc. Both startup calls
        // consume only AL, so satisfy this already-proven installation-media
        // gate locally while leaving the rest of startup validation intact.
        ReplaceAllExact(
            image[..rawEnd],
            Convert.FromHexString("B301E8E8F9FFFF"),
            Convert.FromHexString("B301B001909090"),
            expectedCount: 1,
            "Patch 3 primary installed-media validation call");
        ReplaceAllExact(
            image[..rawEnd],
            Convert.FromHexString("E8C7F9FFFF"),
            Convert.FromHexString("B001909090"),
            expectedCount: 1,
            "Patch 3 retry installed-media validation call");
    }

    private static void ReplaceAllExact(
        Span<byte> image,
        ReadOnlySpan<byte> pattern,
        ReadOnlySpan<byte> replacement,
        int expectedCount,
        string description)
    {
        if (pattern.Length == 0 || pattern.Length != replacement.Length)
        {
            throw new ArgumentException("Binary patch patterns must be nonempty and equal length.");
        }

        var count = 0;
        var offset = 0;
        while (offset <= image.Length - pattern.Length)
        {
            var match = image[offset..].IndexOf(pattern);
            if (match < 0) break;
            offset += match;
            replacement.CopyTo(image[offset..]);
            count++;
            offset += pattern.Length;
        }
        if (count != expectedCount)
        {
            throw new InvalidDataException($"Expected {expectedCount} {description}, but found {count}.");
        }
    }

    private static int FindUniquePattern(ReadOnlySpan<byte> image, ReadOnlySpan<byte> pattern, string description)
    {
        var match = -1;
        for (var index = 0; index <= image.Length - pattern.Length; index++)
        {
            if (!image.Slice(index, pattern.Length).SequenceEqual(pattern)) continue;
            if (match >= 0) throw new InvalidDataException($"{description} is not unique.");
            match = index;
        }
        if (match < 0) throw new InvalidDataException($"{description} was not found.");
        return match;
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
    private sealed record ImportDescriptor(int HeaderOffset, uint OriginalFirstThunkRva, uint NameRva, uint FirstThunkRva);

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
                descriptors.Add(new ImportDescriptor(offset, originalThunk, name, firstThunk));
                offset += 20;
            }
        }
    }
}
