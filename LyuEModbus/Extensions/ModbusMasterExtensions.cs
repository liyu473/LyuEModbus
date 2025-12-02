using LyuEModbus.Abstractions;

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
        Func<Task<T>> action,
        int retryCount,
        Func<Exception, Task>? onError) where T : struct
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    if (onError != null)
                        await onError(ex);
                    return null;
                }
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }

    /// <summary>
    /// 带重试的执行方法（返回引用类型）
    /// </summary>
    private static async Task<T?> ExecuteWithRetryRefAsync<T>(
        Func<Task<T>> action,
        int retryCount,
        Func<Exception, Task>? onError) where T : class
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    if (onError != null)
                        await onError(ex);
                    return null;
                }
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }

    /// <summary>
    /// 带重试的执行方法（返回 bool 表示成功/失败）
    /// </summary>
    private static async Task<bool> ExecuteWithRetryBoolAsync(
        Func<Task> action,
        int retryCount,
        Func<Exception, Task>? onError)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                await action();
                return true;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    if (onError != null)
                        await onError(ex);
                    return false;
                }
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }

    #region 线圈读取

    /// <summary>
    /// 读取单个线圈
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="address">线圈地址</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<bool?> ReadCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var result = await master.ReadCoilsAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError);
    }

    /// <summary>
    /// 批量读取线圈并返回字典
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<Dictionary<ushort, bool>?> ReadCoilsToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryRefAsync(async () =>
        {
            var result = await master.ReadCoilsAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, bool>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError);
    }

    #endregion

    #region 线圈写入

    /// <summary>
    /// 写入单个线圈
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="address">线圈地址</param>
    /// <param name="value">线圈值</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<bool> WriteCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        bool value,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(
            () => master.WriteSingleCoilAsync(master.SlaveId, address, value),
            retryCount, onError);
    }

    /// <summary>
    /// 批量写入线圈
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">线圈值数组</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<bool> WriteCoilsAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        bool[] values,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(
            () => master.WriteMultipleCoilsAsync(master.SlaveId, startAddress, values),
            retryCount, onError);
    }

    /// <summary>
    /// 切换线圈状态
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="address">线圈地址</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<bool?> ToggleCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var result = await master.ReadCoilsAsync(master.SlaveId, address, 1);
            var newValue = !result[0];
            await master.WriteSingleCoilAsync(master.SlaveId, address, newValue);
            return newValue;
        }, retryCount, onError);
    }

    #endregion

    #region 保持寄存器读取

    /// <summary>
    /// 读取单个保持寄存器
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="address">寄存器地址</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<ushort?> ReadHoldingRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError);
    }

    /// <summary>
    /// 批量读取保持寄存器并返回字典
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<Dictionary<ushort, ushort>?> ReadHoldingRegistersToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryRefAsync(async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, ushort>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError);
    }

    /// <summary>
    /// 读取 Int32 值（占用2个寄存器）
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="bigEndian">是否大端序</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<int?> ReadInt32Async(
        this IModbusMasterClient master,
        ushort startAddress,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, 2);
            return bigEndian ? (result[0] << 16) | result[1] : (result[1] << 16) | result[0];
        }, retryCount, onError);
    }

    /// <summary>
    /// 读取 Float 值（占用2个寄存器）
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="bigEndian">是否大端序</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<float?> ReadFloatAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, 2);
            byte[] bytes = bigEndian
                ? [(byte)(result[1] & 0xFF), (byte)(result[1] >> 8), (byte)(result[0] & 0xFF), (byte)(result[0] >> 8)]
                : [(byte)(result[0] & 0xFF), (byte)(result[0] >> 8), (byte)(result[1] & 0xFF), (byte)(result[1] >> 8)];
            return BitConverter.ToSingle(bytes, 0);
        }, retryCount, onError);
    }

    #endregion

    #region 保持寄存器写入

    /// <summary>
    /// 写入单个保持寄存器
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="address">寄存器地址</param>
    /// <param name="value">寄存器值</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<bool> WriteRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        ushort value,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(
            () => master.WriteSingleRegisterAsync(master.SlaveId, address, value),
            retryCount, onError);
    }

    /// <summary>
    /// 批量写入保持寄存器
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">寄存器值数组</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<bool> WriteRegistersAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort[] values,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(
            () => master.WriteMultipleRegistersAsync(master.SlaveId, startAddress, values),
            retryCount, onError);
    }

    /// <summary>
    /// 写入 Int32 值（占用2个寄存器）
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="value">Int32 值</param>
    /// <param name="bigEndian">是否大端序</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<bool> WriteInt32Async(
        this IModbusMasterClient master,
        ushort startAddress,
        int value,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(async () =>
        {
            ushort high = (ushort)(value >> 16);
            ushort low = (ushort)(value & 0xFFFF);
            await master.WriteMultipleRegistersAsync(
                master.SlaveId,
                startAddress,
                bigEndian ? [high, low] : [low, high]);
        }, retryCount, onError);
    }

    /// <summary>
    /// 写入 Float 值（占用2个寄存器）
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="value">Float 值</param>
    /// <param name="bigEndian">是否大端序</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<bool> WriteFloatAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        float value,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(async () =>
        {
            var bytes = BitConverter.GetBytes(value);
            ushort low = (ushort)(bytes[0] | (bytes[1] << 8));
            ushort high = (ushort)(bytes[2] | (bytes[3] << 8));
            await master.WriteMultipleRegistersAsync(
                master.SlaveId,
                startAddress,
                bigEndian ? [high, low] : [low, high]);
        }, retryCount, onError);
    }

    /// <summary>
    /// 递增寄存器值
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="address">寄存器地址</param>
    /// <param name="increment">递增量</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<ushort?> IncrementRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        ushort increment = 1,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            var newValue = (ushort)(result[0] + increment);
            await master.WriteSingleRegisterAsync(master.SlaveId, address, newValue);
            return newValue;
        }, retryCount, onError);
    }

    #endregion

    #region 输入寄存器读取

    /// <summary>
    /// 读取单个输入寄存器
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="address">寄存器地址</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<ushort?> ReadInputRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var result = await master.ReadInputRegistersAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError);
    }

    /// <summary>
    /// 批量读取输入寄存器并返回字典
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<Dictionary<ushort, ushort>?> ReadInputRegistersToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryRefAsync(async () =>
        {
            var result = await master.ReadInputRegistersAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, ushort>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError);
    }

    #endregion

    #region 离散输入读取

    /// <summary>
    /// 读取单个离散输入
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="address">输入地址</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<bool?> ReadDiscreteInputAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var result = await master.ReadInputsAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError);
    }

    /// <summary>
    /// 批量读取离散输入并返回字典
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="onError">错误回调</param>
    /// <param name="retryCount">重试次数（默认0不重试）</param>
    public static async Task<Dictionary<ushort, bool>?> ReadDiscreteInputsToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryRefAsync(async () =>
        {
            var result = await master.ReadInputsAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, bool>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError);
    }

    #endregion
}
