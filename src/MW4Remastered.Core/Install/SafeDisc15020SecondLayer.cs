using System.Buffers.Binary;
using System.Numerics;

namespace MW4Remastered.Core.Install;

public static class SafeDisc15020SecondLayer
{
    public static void DecodePage(Span<byte> page)
    {
        for (var index = 0; index < page.Length / sizeof(uint); index++)
        {
            var offset = index * sizeof(uint);
            var mask = unchecked(
                ((uint)index << 24) |
                ((uint)index << 16) |
                ((uint)index << 8) |
                (uint)index);
            var value = BinaryPrimitives.ReadUInt32LittleEndian(page[offset..]);
            value = FirstModifier(value ^ mask) ^ mask;
            BinaryPrimitives.WriteUInt32LittleEndian(page[offset..], SecondModifier(value));
        }
    }

    private static uint FirstModifier(uint value)
    {
        unchecked
        {
            value = BitOperations.RotateRight(value, 0x9a);
            value -= 0x29d27462;
            value ^= 0x4b5e53e3;
            value += 0x61a4056b;
            value = BitOperations.RotateLeft(value, 0x6b);
            value += 0x3cee272e;
            value -= 0x4a447eaa;
            value = BitOperations.RotateLeft(value, 0x33);
            value = BitOperations.RotateRight(value, 0x60);
            value -= 0x6fd00cd1;
            value -= 0x06027e7b;
            value ^= 0x60c5671c;
            value += 0x727c7b66;
            value += 0x51c77c42;
            value ^= 0x3ccc4a22;
            value ^= 0x58b34034;
            value = BitOperations.RotateLeft(value, 0x7d);
            value = BitOperations.RotateLeft(value, 0xa0);
            value = BitOperations.RotateRight(value, 0x62);
            value = BitOperations.RotateRight(value, 0x4b);
            value ^= 0x0d536ab0;
            value = BitOperations.RotateRight(value, 0x6e);
            return value + 0x22b65ef7;
        }
    }

    private static uint SecondModifier(uint value)
    {
        unchecked
        {
            value--;
            value = 0u - value;
            value = 0u - value;
            value++;
            value--;
            value++;
            value--;
            value++;
            value = BitOperations.RotateRight(value, 0xa5);
            value--;
            value = BitOperations.RotateRight(value, 0x0e);
            value--;
            value += 0x6af16aed;
            value = BitOperations.RotateLeft(value, 0x77);
            value = BitOperations.RotateLeft(value, 0x5e);
            value -= 0x0ce161a4;
            value -= 0x2cdf166b;
            value--;
            value--;
            value += 0x028c0f88;
            value -= 0x29e0230d;
            value = BitOperations.RotateRight(value, 0xff);
            value--;
            value -= 0x7e7b493b;
            value -= 0x671c1fa3;
            value = BitOperations.RotateRight(value, 0x66);
            value = BitOperations.RotateRight(value, 0xa8);
            value++;
            value = BitOperations.RotateLeft(value, 0xc1);
            value -= 0x403460c5;
            value ^= 0x417d16ed;
            value = BitOperations.RotateLeft(value, 0xbe);
            value = BitOperations.RotateLeft(value, 9);
            value++;
            value ^= 0x0eee4752;
            return value + 1;
        }
    }
}
