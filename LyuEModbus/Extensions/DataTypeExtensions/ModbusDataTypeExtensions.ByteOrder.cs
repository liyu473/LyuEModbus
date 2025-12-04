using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// 字节序转换方法
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region 字节序转换核心方法

    /// <summary>
    /// 重新排列字节序
    /// </summary>
    /// <param name="raw">原始字节数组（长度必须为8，不足8字节会补0）</param>
    /// <param name="byteOrder">字节序</param>
    /// <returns>重排后的字节数组</returns>
    private static byte[] ReorderBytes(byte[] raw, ByteOrder byteOrder)
    {
        // 确保至少8字节
        var bytes = new byte[8];
        Buffer.BlockCopy(raw, 0, bytes, 0, Math.Min(raw.Length, 8));

        return byteOrder switch
        {
            ByteOrder.ABCD => bytes, // 原样
            ByteOrder.BADC => [bytes[1], bytes[0], bytes[3], bytes[2], bytes[5], bytes[4], bytes[7], bytes[6]], // 每2字节交换
            ByteOrder.CDAB => [bytes[2], bytes[3], bytes[0], bytes[1], bytes[6], bytes[7], bytes[4], bytes[5]], // 每4字节内前后2字节交换
            ByteOrder.DCBA => [bytes[3], bytes[2], bytes[1], bytes[0], bytes[7], bytes[6], bytes[5], bytes[4]], // 每4字节反转
            _ => bytes
        };
    }

    /// <summary>
    /// 寄存器数组转字节数组（使用系统字节序）
    /// </summary>
    private static byte[] RegistersToBytes(ushort[] registers)
    {
        var bytes = new byte[registers.Length * 2];
        Buffer.BlockCopy(registers, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// 字节数组转寄存器数组（使用系统字节序）
    /// </summary>
    private static ushort[] BytesToRegisters(byte[] bytes)
    {
        var registers = new ushort[bytes.Length / 2];
        Buffer.BlockCopy(bytes, 0, registers, 0, bytes.Length);
        return registers;
    }

    #endregion

    #region 类型转换方法

    private static float RegistersToFloat(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers);
        bytes = ReorderBytes(bytes, byteOrder);
        return BitConverter.ToSingle(bytes, 0);
    }

    private static ushort[] FloatToRegisters(float value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        bytes = ReorderBytes(bytes, byteOrder);
        return BytesToRegisters(bytes[..4]);
    }

    private static int RegistersToInt32(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers);
        bytes = ReorderBytes(bytes, byteOrder);
        return BitConverter.ToInt32(bytes, 0);
    }

    private static ushort[] Int32ToRegisters(int value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        bytes = ReorderBytes(bytes, byteOrder);
        return BytesToRegisters(bytes[..4]);
    }

    private static uint RegistersToUInt32(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers);
        bytes = ReorderBytes(bytes, byteOrder);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static ushort[] UInt32ToRegisters(uint value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        bytes = ReorderBytes(bytes, byteOrder);
        return BytesToRegisters(bytes[..4]);
    }

    private static double RegistersToDouble(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers);
        bytes = ReorderBytes(bytes, byteOrder);
        return BitConverter.ToDouble(bytes, 0);
    }

    private static ushort[] DoubleToRegisters(double value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        bytes = ReorderBytes(bytes, byteOrder);
        return BytesToRegisters(bytes);
    }

    private static long RegistersToInt64(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers);
        bytes = ReorderBytes(bytes, byteOrder);
        return BitConverter.ToInt64(bytes, 0);
    }

    private static ushort[] Int64ToRegisters(long value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        bytes = ReorderBytes(bytes, byteOrder);
        return BytesToRegisters(bytes);
    }

    private static ulong RegistersToUInt64(ushort[] registers, ByteOrder byteOrder)
    {
        var bytes = RegistersToBytes(registers);
        bytes = ReorderBytes(bytes, byteOrder);
        return BitConverter.ToUInt64(bytes, 0);
    }

    private static ushort[] UInt64ToRegisters(ulong value, ByteOrder byteOrder)
    {
        var bytes = BitConverter.GetBytes(value);
        bytes = ReorderBytes(bytes, byteOrder);
        return BytesToRegisters(bytes);
    }

    #endregion
}
