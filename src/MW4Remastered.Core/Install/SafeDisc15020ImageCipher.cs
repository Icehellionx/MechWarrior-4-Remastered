namespace MW4Remastered.Core.Install;

public static class SafeDisc15020ImageCipher
{
    private const int PageSize = 4096;
    private const int BlockSize = 8;

    public static void DecodeSection(Span<byte> rawSection, int virtualSize, ReadOnlySpan<uint> key)
    {
        if (virtualSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(virtualSize));
        }
        if (key.Length != 4)
        {
            throw new ArgumentException("SafeDisc 1 keys must contain exactly four 32-bit words.", nameof(key));
        }

        var encryptedLength = virtualSize == 0
            ? rawSection.Length
            : Math.Min(rawSection.Length, virtualSize);

        for (var pageStart = 0; pageStart < encryptedLength; pageStart += PageSize)
        {
            var pageLength = Math.Min(PageSize, encryptedLength - pageStart);
            SafeDisc15020SecondLayer.DecodePage(rawSection.Slice(pageStart, pageLength));
        }

        var position = 0;
        var remaining = encryptedLength;
        while (remaining > (BlockSize * 2) - 1)
        {
            SafeDisc1BlockCipher.DecryptBlock(rawSection.Slice(position, BlockSize), key);
            position += BlockSize;
            remaining -= BlockSize;
        }

        if (remaining <= BlockSize - 1)
        {
            return;
        }
        if (remaining != BlockSize)
        {
            SafeDisc1BlockCipher.DecryptBlock(
                rawSection.Slice(position + remaining - BlockSize, BlockSize),
                key);
        }
        SafeDisc1BlockCipher.DecryptBlock(rawSection.Slice(position, BlockSize), key);
    }
}
