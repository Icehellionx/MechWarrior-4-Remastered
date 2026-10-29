using System.Buffers.Binary;

namespace MW4Remastered.Core.Install;

// UpdateResource serializes the same resources differently on Wine 11: it
// leaves old size fields when .rsrc still fits, and clears resource code pages.
// Recompute only those metadata fields. The caller still requires the exact
// qualified Windows output hash; resource content and executable code are unchanged.
internal static class BlackKnightDpiResourceLayout
{
    internal static void Canonicalize(byte[] image)
    {
        if (image.Length != BlackKnightPr1ExecutableTransform.OutputLength ||
            Read32(image, 0x3C) != 0x100 || Read32(image, 0x100) != 0x4550 ||
            Read16(image, 0x106) != 5 || Read16(image, 0x118) != 0x10B)
            throw new InvalidDataException("Unsupported Black Knight DPI PE layout.");
        const int optional = 0x118;
        var sections = optional + Read16(image, 0x114);
        uint initialized = 0;
        var resourceSection = -1;
        var resourceRva = Read32(image, optional + 112);
        for (var index = 0; index < 5; index++)
        {
            var section = sections + index * 40;
            if ((Read32(image, section + 36) & 0x40) != 0)
                initialized = checked(initialized + Read32(image, section + 16));
            if (Read32(image, section + 12) == resourceRva) resourceSection = section;
        }
        if (resourceSection < 0 || resourceRva != 0x49E000 || initialized != 0x50000)
            throw new InvalidDataException("Unsupported Black Knight DPI section metadata.");
        var raw = checked((int)Read32(image, resourceSection + 20));
        var size = checked((int)Read32(image, resourceSection + 16));
        if (raw != 0x38D000 || size != 0x5000 || raw + size != image.Length)
            throw new InvalidDataException("Unsupported Black Knight DPI resource extent.");

        var visited = new HashSet<int>();
        var entries = new HashSet<int>();
        var extent = 0;
        Walk(0, 0);
        extent = checked((extent + 3) & ~3);
        if (entries.Count != 18 || extent != 0x45C0)
            throw new InvalidDataException("Black Knight DPI resource inventory does not match the qualified manifest.");
        Write32(image, optional + 8, initialized);
        Write32(image, optional + 116, (uint)extent);
        Write32(image, resourceSection + 8, (uint)extent);
        foreach (var entry in entries) Write32(image, raw + entry + 8, 1252);

        void Walk(int directory, int depth)
        {
            if (depth > 3 || !visited.Add(directory))
                throw new InvalidDataException("Cyclic or overly deep Black Knight DPI resources.");
            RequireRange(directory, 16);
            var count = Read16(image, raw + directory + 12) + Read16(image, raw + directory + 14);
            if (count > 64) throw new InvalidDataException("Unexpected Black Knight DPI resource directory size.");
            RequireRange(directory, 16 + count * 8);
            extent = Math.Max(extent, directory + 16 + count * 8);
            for (var index = 0; index < count; index++)
            {
                var child = Read32(image, raw + directory + 20 + index * 8);
                if ((child & 0x80000000) != 0) { Walk((int)(child & 0x7FFFFFFF), depth + 1); continue; }
                var entry = checked((int)child);
                RequireRange(entry, 16);
                if (!entries.Add(entry)) throw new InvalidDataException("Duplicate Black Knight DPI resource data entry.");
                var address = Read32(image, raw + entry);
                var length = Read32(image, raw + entry + 4);
                if (address < resourceRva || (ulong)(address - resourceRva) + length > (uint)size)
                    throw new InvalidDataException("Black Knight DPI resource data escapes its section.");
                extent = Math.Max(extent, Math.Max(entry + 16, checked((int)(address - resourceRva + length))));
            }
        }

        void RequireRange(int offset, int length)
        {
            if (offset < 0 || length < 0 || offset > size - length)
                throw new InvalidDataException("Black Knight DPI resource metadata escapes its section.");
        }
    }

    private static ushort Read16(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, 2));
    private static uint Read32(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4));
    private static void Write32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), value);
}
