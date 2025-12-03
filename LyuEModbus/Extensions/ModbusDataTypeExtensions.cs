using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// Modbus 数据类型读写扩展方法（Float、Int32、Double、UInt32、Int64 等）
/// </summary>
public static class ModbusDataTypeExtensions
{
    private const int DefaultRetryDelayMs = 100;

    #region Float (32-bit, 2 registers)

    /// <summary>
    /// 读取 Float（32位，占用2个寄存器）
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="address">起始地址</param>
    /// <param name="byteOrder">字节序（null 则使用主站配置的默认字节序）</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数</param>
    public static async Task<float?> ReadFloatAsync(
        this IModbusMasterClient master,
        ushort address,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 2);
            return RegistersToFloat(registers, order);
        }, retryCount, onError, $"ReadFloat({address})");
    }

    /// <summary>
    /// 写入 Float
    /// </summary>
    public static async Task<bool> WriteFloatAsync(
        this IModbusMasterClient master,
        ushort address,
        float value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = FloatToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteFloat({address}, {value})");
    }

    /// <summary>
    /// 读取多个 Float
    /// </summary>
    public static async Task<float[]?> ReadFloatsAsync(
        this IModbusMasterClient master,
        ushort address,
        int count,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryRefAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, (ushort)(count * 2));
            var result = new float[count];
            for (int i = 0; i < count; i++)
            {
                var pair = new ushort[] { registers[i * 2], registers[i * 2 + 1] };
                result[i] = RegistersToFloat(pair, order);
            }
            return result;
        }, retryCount, onError, $"ReadFloats({address}, {count})");
    }

    #endregion

    #region Int32 (32-bit, 2 registers)

    /// <summary>
    /// 读取 Int32（32位有符号整数，占用2个寄存器）
    /// </summary>
    public static async Task<int?> ReadInt32Async(
        this IModbusMasterClient master,
        ushort address,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 2);
            return RegistersToInt32(registers, order);
        }, retryCount, onError, $"ReadInt32({address})");
    }

    /// <summary>
    /// 写入 Int32
    /// </summary>
    public static async Task<bool> WriteInt32Async(
        this IModbusMasterClient master,
        ushort address,
        int value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = Int32ToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteInt32({address}, {value})");
    }

    #endregion

    #region Double (64-bit, 4 registers)

    /// <summary>
    /// 读取 Double（64位，占用4个寄存器）
    /// </summary>
    public static async Task<double?> ReadDoubleAsync(
        this IModbusMasterClient master,
        ushort address,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 4);
            return RegistersToDouble(registers, order);
        }, retryCount, onError, $"ReadDouble({address})");
    }

    /// <summary>
    /// 读取多个 Double
    /// </summary>
    public static async Task<double[]?> ReadDoublesAsync(
        this IModbusMasterClient master,
        ushort address,
        int count,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryRefAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, (ushort)(count * 4));
            var result = new double[count];
            for (int i = 0; i < count; i++)
            {
                var quad = new ushort[] { registers[i * 4], registers[i * 4 + 1], registers[i * 4 + 2], registers[i * 4 + 3] };
                result[i] = RegistersToDouble(quad, order);
            }
            return result;
        }, retryCount, onError, $"ReadDoubles({address}, {count})");
    }

    /// <summary>
    /// 写入 Double
    /// </summary>
    public static async Task<bool> WriteDoubleAsync(
        this IModbusMasterClient master,
        ushort address,
        double value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = DoubleToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteDouble({address}, {value})");
    }

    #endregion

    #region UInt32 (32-bit, 2 registers)

    /// <summary>
    /// 读取 UInt32（32位无符号整数，占用2个寄存器）
    /// </summary>
    public static async Task<uint?> ReadUInt32Async(
        this IModbusMasterClient master,
        ushort address,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 2);
            return RegistersToUInt32(registers, order);
        }, retryCount, onError, $"ReadUInt32({address})");
    }

    /// <summary>
    /// 写入 UInt32
    /// </summary>
    public static async Task<bool> WriteUInt32Async(
        this IModbusMasterClient master,
        ushort address,
        uint value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = UInt32ToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteUInt32({address}, {value})");
    }

    #endregion

    #region Int64 (64-bit, 4 registers)

    /// <summary>
    /// 读取 Int64（64位有符号整数，占用4个寄存器）
    /// </summary>
    public static async Task<long?> ReadInt64Async(
        this IModbusMasterClient master,
        ushort address,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 4);
            return RegistersToInt64(registers, order);
        }, retryCount, onError, $"ReadInt64({address})");
    }

    /// <summary>
    /// 写入 Int64
    /// </summary>
    public static async Task<bool> WriteInt64Async(
        this IModbusMasterClient master,
        ushort address,
        long value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = Int64ToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteInt64({address}, {value})");
    }

    #endregion

    #region UInt64 (64-bit, 4 registers)

    /// <summary>
    /// 读取 UInt64（64位无符号整数，占用4个寄存器）
    /// </summary>
    public static async Task<ulong?> ReadUInt64Async(
        this IModbusMasterClient master,
        ushort address,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 4);
            return RegistersToUInt64(registers, order);
        }, retryCount, onError, $"ReadUInt64({address})");
    }

    /// <summary>
    /// 写入 UInt64
    /// </summary>
    public static async Task<bool> WriteUInt64Async(
        this IModbusMasterClient master,
        ushort address,
        ulong value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = UInt64ToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteUInt64({address}, {value})");
    }

    #endregion

    #region 重试执行方法

    private static async Task<T?> ExecuteWithRetryAsync<T>(
        IModbusClient client,
        Func<Task<T>> action,
        int retryCount,
        Func<Exception, Task>? onError,
        string operationName) where T : struct
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var result = await action();
                client.Log(ModbusLogLevel.Debug, $"{operationName} 成功: {result}");
                return result;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    client.Log(ModbusLogLevel.Error, $"{operationName} 失败: {ex.Message}");
                    if (onError != null)
                        await onError(ex);
                    return null;
                }
                client.Log(ModbusLogLevel.Warning, $"{operationName} 重试 {attempts}/{retryCount}: {ex.Message}");
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }

    private static async Task<T?> ExecuteWithRetryRefAsync<T>(
        IModbusClient client,
        Func<Task<T>> action,
        int retryCount,
        Func<Exception, Task>? onError,
        string operationName) where T : class
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var result = await action();
                client.Log(ModbusLogLevel.Debug, $"{operationName} 成功");
                return result;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    client.Log(ModbusLogLevel.Error, $"{operationName} 失败: {ex.Message}");
                    if (onError != null)
                        await onError(ex);
                    return null;
                }
                client.Log(ModbusLogLevel.Warning, $"{operationName} 重试 {attempts}/{retryCount}: {ex.Message}");
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }

    private static async Task<bool> ExecuteWithRetryBoolAsync(
        IModbusClient client,
        Func<Task> action,
        int retryCount,
        Func<Exception, Task>? onError,
        string operationName)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                await action();
                client.Log(ModbusLogLevel.Debug, $"{operationName} 成功");
                return true;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    client.Log(ModbusLogLevel.Error, $"{operationName} 失败: {ex.Message}");
                    if (onError != null)
                        await onError(ex);
                    return false;
                }
                client.Log(ModbusLogLevel.Warning, $"{operationName} 重试 {attempts}/{retryCount}: {ex.Message}");
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }

    #endregion

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
