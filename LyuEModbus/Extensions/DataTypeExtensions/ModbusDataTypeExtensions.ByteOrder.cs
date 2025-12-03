using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// 字节序转换方法
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region 字节序转换核心方法

    private static byte[] RegistersToBytes(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = new byte[registers.Length * 2];
        for (int i = 0; i < registers.Length; i++)
        {
            bytes[i * 2] = (byte)(registers[i] >> 8);
            bytes[i * 2 + 1] = (byte)(registers[i] & 0xFF);
        }

        return byteOrder switch
        {
            ByteOrder.ABCD => bytes,
            ByteOrder.CDAB => SwapWords(bytes),
            ByteOrder.BADC => SwapBytes(bytes),
            ByteOrder.DCBA => SwapBytes(SwapWords(bytes)),
            _ => bytes
        };
    }

    private static ushort[] BytesToRegisters(byte[] bytes, ByteOrder byteOrder)
    {
        var ordered = byteOrder switch
        {
            ByteOrder.ABCD => bytes,
            ByteOrder.CDAB => SwapWords(bytes),
            ByteOrder.BADC => SwapBytes(bytes),
            ByteOrder.DCBA => SwapBytes(SwapWords(bytes)),
            _ => bytes
        };

        var registers = new ushort[ordered.Length / 2];
        for (int i = 0; i < registers.Length; i++)
        {
            registers[i] = (ushort)((ordered[i * 2] << 8) | ordered[i * 2 + 1]);
        }
        return registers;
    }

    private static byte[] SwapWords(byte[] bytes)
    {
        var result = new byte[bytes.Length];
        int wordCount = bytes.Length / 2;

        for (int i = 0; i < wordCount / 2; i++)
        {
            int srcIdx = i * 2;
            int dstIdx = (wordCount - 1 - i) * 2;
            result[srcIdx] = bytes[dstIdx];
            result[srcIdx + 1] = bytes[dstIdx + 1];
            result[dstIdx] = bytes[srcIdx];
            result[dstIdx + 1] = bytes[srcIdx + 1];
        }

        if (wordCount % 2 == 1)
        {
            int midIdx = (wordCount / 2) * 2;
            result[midIdx] = bytes[midIdx];
            result[midIdx + 1] = bytes[midIdx + 1];
        }

        return result;
    }

    private static byte[] SwapBytes(byte[] bytes)
    {
        var result = new byte[bytes.Length];
        for (int i = 0; i < bytes.Length; i += 2)
        {
            result[i] = bytes[i + 1];
            result[i + 1] = bytes[i];
        }
        return result;
    }

    #endregion

    #region 类型转换方法

    private static float RegistersToFloat(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers, byteOrder);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToSingle(bytes, 0);
    }

    private static ushort[] FloatToRegisters(float value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BytesToRegisters(bytes, byteOrder);
    }

    private static int RegistersToInt32(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers, byteOrder);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    private static ushort[] Int32ToRegisters(int value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BytesToRegisters(bytes, byteOrder);
    }

    private static uint RegistersToUInt32(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers, byteOrder);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static ushort[] UInt32ToRegisters(uint value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BytesToRegisters(bytes, byteOrder);
    }

    private static double RegistersToDouble(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers, byteOrder);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToDouble(bytes, 0);
    }

    private static ushort[] DoubleToRegisters(double value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BytesToRegisters(bytes, byteOrder);
    }

    private static long RegistersToInt64(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers, byteOrder);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToInt64(bytes, 0);
    }

    private static ushort[] Int64ToRegisters(long value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BytesToRegisters(bytes, byteOrder);
    }

    private static ulong RegistersToUInt64(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers, byteOrder);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToUInt64(bytes, 0);
    }

    private static ushort[] UInt64ToRegisters(ulong value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BytesToRegisters(bytes, byteOrder);
    }

    #endregion
}
