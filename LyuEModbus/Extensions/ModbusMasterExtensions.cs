using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// IModbusMasterClient 读写扩展方法
/// </summary>
public static partial class ModbusMasterExtensions
{
    /// <summary>
    /// 默认重试间隔（毫秒）
    /// </summary>
    private const int DefaultRetryDelayMs = 100;

    /// <summary>
    /// 带重试的执行方法
    /// </summary>
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

    /// <summary>
    /// 带重试的执行方法（返回引用类型）
    /// </summary>
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

    /// <summary>
    /// 带重试的执行方法（返回 bool 表示成功/失败）
    /// </summary>
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

    #region 线圈读取

    /// <summary>
    /// 读取单个线圈
    /// </summary>
    public static Task<bool?> ReadCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadCoilsAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError, $"ReadCoil({address})");
    }

    /// <summary>
    /// 批量读取线圈并返回字典
    /// </summary>
    public static Task<Dictionary<ushort, bool>?> ReadCoilsToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            var result = await master.ReadCoilsAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, bool>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError, $"ReadCoils({startAddress}, {count})");
    }

    #endregion

    #region 线圈写入

    /// <summary>
    /// 写入单个线圈
    /// </summary>
    public static Task<bool> WriteCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        bool value,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryBoolAsync(master,
            () => master.WriteSingleCoilAsync(master.SlaveId, address, value),
            retryCount, onError, $"WriteCoil({address}, {value})");
    }

    /// <summary>
    /// 批量写入线圈
    /// </summary>
    public static Task<bool> WriteCoilsAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        bool[] values,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryBoolAsync(master,
            () => master.WriteMultipleCoilsAsync(master.SlaveId, startAddress, values),
            retryCount, onError, $"WriteCoils({startAddress}, {values.Length})");
    }

    /// <summary>
    /// 切换线圈状态
    /// </summary>
    public static Task<bool?> ToggleCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadCoilsAsync(master.SlaveId, address, 1);
            var newValue = !result[0];
            await master.WriteSingleCoilAsync(master.SlaveId, address, newValue);
            return newValue;
        }, retryCount, onError, $"ToggleCoil({address})");
    }

    #endregion

    #region 保持寄存器读取

    /// <summary>
    /// 读取单个保持寄存器
    /// </summary>
    public static Task<ushort?> ReadHoldingRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError, $"ReadHoldingRegister({address})");
    }

    /// <summary>
    /// 批量读取保持寄存器并返回字典
    /// </summary>
    public static Task<Dictionary<ushort, ushort>?> ReadHoldingRegistersToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, ushort>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError, $"ReadHoldingRegisters({startAddress}, {count})");
    }

    /// <summary>
    /// 读取 Int32 值（占用2个寄存器）
    /// </summary>
    public static Task<int?> ReadInt32Async(
        this IModbusMasterClient master,
        ushort startAddress,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, 2);
            return bigEndian ? (result[0] << 16) | result[1] : (result[1] << 16) | result[0];
        }, retryCount, onError, $"ReadInt32({startAddress})");
    }

    /// <summary>
    /// 读取 Float 值（占用2个寄存器）
    /// </summary>
    public static Task<float?> ReadFloatAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, 2);
            byte[] bytes = bigEndian
                ? [(byte)(result[1] & 0xFF), (byte)(result[1] >> 8), (byte)(result[0] & 0xFF), (byte)(result[0] >> 8)]
                : [(byte)(result[0] & 0xFF), (byte)(result[0] >> 8), (byte)(result[1] & 0xFF), (byte)(result[1] >> 8)];
            return BitConverter.ToSingle(bytes, 0);
        }, retryCount, onError, $"ReadFloat({startAddress})");
    }

    #endregion

    #region 保持寄存器写入

    /// <summary>
    /// 写入单个保持寄存器
    /// </summary>
    public static Task<bool> WriteRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        ushort value,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryBoolAsync(master,
            () => master.WriteSingleRegisterAsync(master.SlaveId, address, value),
            retryCount, onError, $"WriteRegister({address}, {value})");
    }

    /// <summary>
    /// 批量写入保持寄存器
    /// </summary>
    public static Task<bool> WriteRegistersAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort[] values,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryBoolAsync(master,
            () => master.WriteMultipleRegistersAsync(master.SlaveId, startAddress, values),
            retryCount, onError, $"WriteRegisters({startAddress}, {values.Length})");
    }

    /// <summary>
    /// 写入 Int32 值（占用2个寄存器）
    /// </summary>
    public static Task<bool> WriteInt32Async(
        this IModbusMasterClient master,
        ushort startAddress,
        int value,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryBoolAsync(master, async () =>
        {
            ushort high = (ushort)(value >> 16);
            ushort low = (ushort)(value & 0xFFFF);
            await master.WriteMultipleRegistersAsync(master.SlaveId, startAddress,
                bigEndian ? [high, low] : [low, high]);
        }, retryCount, onError, $"WriteInt32({startAddress}, {value})");
    }

    /// <summary>
    /// 写入 Float 值（占用2个寄存器）
    /// </summary>
    public static Task<bool> WriteFloatAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        float value,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryBoolAsync(master, async () =>
        {
            var bytes = BitConverter.GetBytes(value);
            ushort low = (ushort)(bytes[0] | (bytes[1] << 8));
            ushort high = (ushort)(bytes[2] | (bytes[3] << 8));
            await master.WriteMultipleRegistersAsync(master.SlaveId, startAddress,
                bigEndian ? [high, low] : [low, high]);
        }, retryCount, onError, $"WriteFloat({startAddress}, {value})");
    }

    /// <summary>
    /// 递增寄存器值
    /// </summary>
    public static Task<ushort?> IncrementRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        ushort increment = 1,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            var newValue = (ushort)(result[0] + increment);
            await master.WriteSingleRegisterAsync(master.SlaveId, address, newValue);
            return newValue;
        }, retryCount, onError, $"IncrementRegister({address}, +{increment})");
    }

    #endregion

    #region 输入寄存器读取

    /// <summary>
    /// 读取单个输入寄存器
    /// </summary>
    public static Task<ushort?> ReadInputRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadInputRegistersAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError, $"ReadInputRegister({address})");
    }

    /// <summary>
    /// 批量读取输入寄存器并返回字典
    /// </summary>
    public static Task<Dictionary<ushort, ushort>?> ReadInputRegistersToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            var result = await master.ReadInputRegistersAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, ushort>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError, $"ReadInputRegisters({startAddress}, {count})");
    }

    #endregion

    #region 离散输入读取

    /// <summary>
    /// 读取单个离散输入
    /// </summary>
    public static Task<bool?> ReadDiscreteInputAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadInputsAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError, $"ReadDiscreteInput({address})");
    }

    /// <summary>
    /// 批量读取离散输入并返回字典
    /// </summary>
    public static Task<Dictionary<ushort, bool>?> ReadDiscreteInputsToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            var result = await master.ReadInputsAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, bool>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError, $"ReadDiscreteInputs({startAddress}, {count})");
    }

    #endregion
}
