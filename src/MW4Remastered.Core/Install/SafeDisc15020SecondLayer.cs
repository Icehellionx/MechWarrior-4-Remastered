using System.Buffers.Binary;
using System.Numerics;

namespace MW4Remastered.Core.Install;

public enum SafeDisc15020SecondLayerProfile
{
    Vengeance,
    Mercenaries,
}

public static class SafeDisc15020SecondLayer
{
    public static void DecodePage(
        Span<byte> page,
        SafeDisc15020SecondLayerProfile profile = SafeDisc15020SecondLayerProfile.Vengeance)
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
            value = FirstModifier(value ^ mask, profile) ^ mask;
            BinaryPrimitives.WriteUInt32LittleEndian(page[offset..], SecondModifier(value, profile));
        }
    }

    private static uint FirstModifier(uint value, SafeDisc15020SecondLayerProfile profile) => profile switch
    {
        SafeDisc15020SecondLayerProfile.Vengeance => VengeanceFirstModifier(value),
        SafeDisc15020SecondLayerProfile.Mercenaries => MercenariesFirstModifier(value),
        _ => throw new ArgumentOutOfRangeException(nameof(profile)),
    };

    private static uint SecondModifier(uint value, SafeDisc15020SecondLayerProfile profile) => profile switch
    {
        SafeDisc15020SecondLayerProfile.Vengeance => VengeanceSecondModifier(value),
        SafeDisc15020SecondLayerProfile.Mercenaries => MercenariesSecondModifier(value),
        _ => throw new ArgumentOutOfRangeException(nameof(profile)),
    };

    private static uint VengeanceFirstModifier(uint value)
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

    private static uint VengeanceSecondModifier(uint value)
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

    private static uint MercenariesFirstModifier(uint value)
    {
        unchecked
        {
            value = BitOperations.RotateLeft(value, 0x90);
            value ^= 0x01f32ac8;
            value ^= 0x28be1602;
            value -= 0x1e82614f;
            value += 0x32d25c61;
            value ^= 0x6fd90755;
            value += 0x3502424c;
            value += 0x23731403;
            value -= 0x3f6544d9;
            value ^= 0x42213730;
            value -= 0x7379302e;
            value = BitOperations.RotateLeft(value, 2);
            value -= 0x01c863b2;
            value ^= 0x0d5a7ea9;
            value = BitOperations.RotateRight(value, 0x37);
            value -= 0x51e21f66;
            value ^= 0x718f0b8a;
            value = BitOperations.RotateLeft(value, 0x10);
            value = BitOperations.RotateRight(value, 0xc9);
            value += 0x6a08434d;
            value = BitOperations.RotateLeft(value, 0x41);
            return value - 0x011b4aa6;
        }
    }

    private static uint MercenariesSecondModifier(uint value)
    {
        unchecked
        {
            value = BitOperations.RotateLeft(value, 0xf1);
            value -= 0x71422bc9;
            value = BitOperations.RotateLeft(value, 0x63);
            value ^= 0x34947e79;
            value = 0u - value;
            value = BitOperations.RotateLeft(value, 0x4f);
            value = BitOperations.RotateRight(value, 0xe0);
            value -= 0x2a9c77db;
            value += 0x5b5b742a;
            value--;
            value += 0x222d75f5;
            value = 0u - value;
            value = BitOperations.RotateRight(value, 0xb3);
            value ^= 0x16025095;
            value = 0u - value;
            value ^= 0x5c615474;
            value++;
            value = 0u - value;
            value = BitOperations.RotateLeft(value, 0x65);
            value += 0x7b534221;
            value = BitOperations.RotateLeft(value, 0x79);
            value -= 0x54715fc0;
            value = BitOperations.RotateRight(value, 0xc8);
            value++;
            value = BitOperations.RotateLeft(value, 0x37);
            value--;
            value = 0u - value;
            value = BitOperations.RotateLeft(value, 8);
            value = BitOperations.RotateLeft(value, 0x85);
            value--;
            return value ^ 0x011b4aa6;
        }
    }
}
