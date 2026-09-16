using System.Buffers.Binary;

namespace MW4Remastered.Core.Install;

public static class SafeDisc1R0R1BlockCipher
{
    private const uint Delta = 0x9e3779b9;
    private const uint InitialSum = 0xc6ef3720;
    private const int Rounds = 32;

    public static void DecryptBlock(Span<byte> block, ReadOnlySpan<uint> key)
    {
        if (block.Length != 8)
        {
            throw new ArgumentException("SafeDisc 1 r0/r1 blocks must be exactly eight bytes.", nameof(block));
        }
        if (key.Length != 4)
        {
            throw new ArgumentException("SafeDisc 1 r0/r1 keys must contain exactly four 32-bit words.", nameof(key));
        }

        var left = BinaryPrimitives.ReadUInt32LittleEndian(block);
        var right = BinaryPrimitives.ReadUInt32LittleEndian(block[4..]);
        var sum = InitialSum;

        unchecked
        {
            for (var round = 0; round < Rounds; round++)
            {
                right -= ((left >> 5) + key[3]) ^ ((left << 4) + key[2]) ^ (sum + left);
                left -= ((right >> 5) + key[1]) ^ ((right << 4) + key[0]) ^ (sum + right);
                sum -= Delta;
            }
        }

        BinaryPrimitives.WriteUInt32LittleEndian(block, left);
        BinaryPrimitives.WriteUInt32LittleEndian(block[4..], right);
    }
}
