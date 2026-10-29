using System.Buffers.Binary;
using MW4Remastered.Core.Install;

internal static class BlackKnightDpiResourceLayoutSmoke
{
    public static void Run(List<string> failures)
    {
        var image = new byte[BlackKnightPr1ExecutableTransform.OutputLength];
        W32(0x3C, 0x100); W32(0x100, 0x4550); W16(0x106, 5); W16(0x114, 0xE0); W16(0x118, 0x10B);
        W32(0x120, 0x161000); W32(0x188, 0x49E000); W32(0x18C, 0x4330);
        W32(0x1F8 + 80 + 16, 0x44000); W32(0x1F8 + 80 + 36, 0x40);
        W32(0x1F8 + 120 + 16, 0x7000); W32(0x1F8 + 120 + 36, 0x40);
        W32(0x298 + 8, 0x4376); W32(0x298 + 12, 0x49E000); W32(0x298 + 16, 0x5000);
        W32(0x298 + 20, 0x38D000); W32(0x298 + 36, 0x40);
        const int raw = 0x38D000;
        W16(raw + 14, 18);
        for (var index = 0; index < 18; index++)
        {
            var entry = 0x100 + index * 16;
            W32(raw + 16 + index * 8, (uint)(index + 1));
            W32(raw + 20 + index * 8, (uint)entry);
            W32(raw + entry, (uint)(0x49E000 + (index == 17 ? 0x45B0 : 0x4400 + index * 8)));
            W32(raw + entry + 4, index == 17 ? 16u : 4u);
        }
        image[0x1000] = 0xCC;
        var cyclic = (byte[])image.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(cyclic.AsSpan(raw + 20, 4), 0x80000000);
        var rejected = false;
        try { BlackKnightDpiResourceLayout.Canonicalize(cyclic); }
        catch (InvalidDataException) { rejected = true; }
        Check(rejected, "DPI resource normalization rejects directory cycles", failures);
        BlackKnightDpiResourceLayout.Canonicalize(image);
        Check(R32(0x120) == 0x50000 && R32(0x18C) == 0x45C0 && R32(0x2A0) == 0x45C0 &&
            Enumerable.Range(0, 18).All(index => R32(raw + 0x108 + index * 16) == 1252) && image[0x1000] == 0xCC,
            "DPI resource normalization corrects metadata while preserving executable bytes", failures);
        var once = (byte[])image.Clone();
        BlackKnightDpiResourceLayout.Canonicalize(image);
        Check(image.SequenceEqual(once), "DPI resource normalization is idempotent", failures);
        W32(raw + 0x100, 0xFFFFFFF0);
        rejected = false;
        try { BlackKnightDpiResourceLayout.Canonicalize(image); }
        catch (InvalidDataException) { rejected = true; }
        Check(rejected, "DPI resource normalization rejects escaped resource data", failures);

        void W16(int offset, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(offset, 2), value);
        void W32(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(offset, 4), value);
        uint R32(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(offset, 4));
    }

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }
}
