using LyuEModbus.Abstractions;

namespace LyuEModbus.Extensions;

/// <summary>
/// IModbusMaster 读写扩展方法
/// </summary>
public static class ModbusMasterExtensions
{
    #region 线圈读取
    
    /// <summary>
    /// 读取单个线圈
    /// </summary>
    public static async Task<bool?> ReadCoilAsync(this IModbusMaster master, ushort address, Func<Exception, Task>? onError = null)
    {
        try
        {
            var result = await master.ReadCoilsAsync(address, 1);
            return result[0];
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    /// <summary>
    /// 读取多个线圈并返回字典
    /// </summary>
    public static async Task<Dictionary<ushort, bool>?> ReadCoilsToDictAsync(this IModbusMaster master, ushort startAddress, ushort count, Func<Exception, Task>? onError = null)
    {
        try
        {
            var result = await master.ReadCoilsAsync(startAddress, count);
            var dict = new Dictionary<ushort, bool>();
            for (int i = 0; i < result.Length; i++)
            {
                dict[(ushort)(startAddress + i)] = result[i];
            }
            return dict;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    #endregion
    
    #region 线圈写入
    
    /// <summary>
    /// 写入单个线圈
    /// </summary>
    public static async Task<bool> WriteCoilAsync(this IModbusMaster master, ushort address, bool value, Func<Exception, Task>? onError = null)
    {
        try
        {
            await master.WriteSingleCoilAsync(address, value);
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return false;
        }
    }
    
    /// <summary>
    /// 写入多个线圈
    /// </summary>
    public static async Task<bool> WriteCoilsAsync(this IModbusMaster master, ushort startAddress, bool[] values, Func<Exception, Task>? onError = null)
    {
        try
        {
            await master.WriteMultipleCoilsAsync(startAddress, values);
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return false;
        }
    }
    
    /// <summary>
    /// 切换线圈状态
    /// </summary>
    public static async Task<bool?> ToggleCoilAsync(this IModbusMaster master, ushort address, Func<Exception, Task>? onError = null)
    {
        try
        {
            var current = await master.ReadCoilAsync(address);
            if (current is null) return null;
            var newValue = !current.Value;
            await master.WriteSingleCoilAsync(address, newValue);
            return newValue;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    #endregion
    
    #region 保持寄存器读取
    
    /// <summary>
    /// 读取单个保持寄存器
    /// </summary>
    public static async Task<ushort?> ReadHoldingRegisterAsync(this IModbusMaster master, ushort address, Func<Exception, Task>? onError = null)
    {
        try
        {
            var result = await master.ReadHoldingRegistersAsync(address, 1);
            return result[0];
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    /// <summary>
    /// 读取多个保持寄存器并返回字典
    /// </summary>
    public static async Task<Dictionary<ushort, ushort>?> ReadHoldingRegistersToDictAsync(this IModbusMaster master, ushort startAddress, ushort count, Func<Exception, Task>? onError = null)
    {
        try
        {
            var result = await master.ReadHoldingRegistersAsync(startAddress, count);
            var dict = new Dictionary<ushort, ushort>();
            for (int i = 0; i < result.Length; i++)
            {
                dict[(ushort)(startAddress + i)] = result[i];
            }
            return dict;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    /// <summary>
    /// 读取32位整数（两个连续寄存器）
    /// </summary>
    public static async Task<int?> ReadInt32Async(this IModbusMaster master, ushort startAddress, bool bigEndian = true, Func<Exception, Task>? onError = null)
    {
        try
        {
            var result = await master.ReadHoldingRegistersAsync(startAddress, 2);
            if (bigEndian)
                return (result[0] << 16) | result[1];
            else
                return (result[1] << 16) | result[0];
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    /// <summary>
    /// 读取32位浮点数（两个连续寄存器）
    /// </summary>
    public static async Task<float?> ReadFloatAsync(this IModbusMaster master, ushort startAddress, bool bigEndian = true, Func<Exception, Task>? onError = null)
    {
        try
        {
            var result = await master.ReadHoldingRegistersAsync(startAddress, 2);
            byte[] bytes;
            if (bigEndian)
                bytes = new byte[] { (byte)(result[1] & 0xFF), (byte)(result[1] >> 8), (byte)(result[0] & 0xFF), (byte)(result[0] >> 8) };
            else
                bytes = new byte[] { (byte)(result[0] & 0xFF), (byte)(result[0] >> 8), (byte)(result[1] & 0xFF), (byte)(result[1] >> 8) };
            return BitConverter.ToSingle(bytes, 0);
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    #endregion
    
    #region 保持寄存器写入
    
    /// <summary>
    /// 写入单个保持寄存器
    /// </summary>
    public static async Task<bool> WriteRegisterAsync(this IModbusMaster master, ushort address, ushort value, Func<Exception, Task>? onError = null)
    {
        try
        {
            await master.WriteSingleRegisterAsync(address, value);
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return false;
        }
    }
    
    /// <summary>
    /// 写入多个保持寄存器
    /// </summary>
    public static async Task<bool> WriteRegistersAsync(this IModbusMaster master, ushort startAddress, ushort[] values, Func<Exception, Task>? onError = null)
    {
        try
        {
            await master.WriteMultipleRegistersAsync(startAddress, values);
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return false;
        }
    }
    
    /// <summary>
    /// 写入32位整数
    /// </summary>
    public static async Task<bool> WriteInt32Async(this IModbusMaster master, ushort startAddress, int value, bool bigEndian = true, Func<Exception, Task>? onError = null)
    {
        try
        {
            ushort high = (ushort)(value >> 16);
            ushort low = (ushort)(value & 0xFFFF);
            if (bigEndian)
                await master.WriteMultipleRegistersAsync(startAddress, new[] { high, low });
            else
                await master.WriteMultipleRegistersAsync(startAddress, new[] { low, high });
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return false;
        }
    }
    
    /// <summary>
    /// 写入32位浮点数
    /// </summary>
    public static async Task<bool> WriteFloatAsync(this IModbusMaster master, ushort startAddress, float value, bool bigEndian = true, Func<Exception, Task>? onError = null)
    {
        try
        {
            var bytes = BitConverter.GetBytes(value);
            ushort low = (ushort)(bytes[0] | (bytes[1] << 8));
            ushort high = (ushort)(bytes[2] | (bytes[3] << 8));
            if (bigEndian)
                await master.WriteMultipleRegistersAsync(startAddress, new[] { high, low });
            else
                await master.WriteMultipleRegistersAsync(startAddress, new[] { low, high });
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return false;
        }
    }
    
    /// <summary>
    /// 寄存器值增加
    /// </summary>
    public static async Task<ushort?> IncrementRegisterAsync(this IModbusMaster master, ushort address, ushort increment = 1, Func<Exception, Task>? onError = null)
    {
        try
        {
            var current = await master.ReadHoldingRegisterAsync(address);
            if (current is null) return null;
            var newValue = (ushort)(current.Value + increment);
            await master.WriteSingleRegisterAsync(address, newValue);
            return newValue;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    #endregion
    
    #region 输入寄存器读取
    
    /// <summary>
    /// 读取单个输入寄存器
    /// </summary>
    public static async Task<ushort?> ReadInputRegisterAsync(this IModbusMaster master, ushort address, Func<Exception, Task>? onError = null)
    {
        try
        {
            var result = await master.ReadInputRegistersAsync(address, 1);
            return result[0];
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    /// <summary>
    /// 读取多个输入寄存器并返回字典
    /// </summary>
    public static async Task<Dictionary<ushort, ushort>?> ReadInputRegistersToDictAsync(this IModbusMaster master, ushort startAddress, ushort count, Func<Exception, Task>? onError = null)
    {
        try
        {
            var result = await master.ReadInputRegistersAsync(startAddress, count);
            var dict = new Dictionary<ushort, ushort>();
            for (int i = 0; i < result.Length; i++)
            {
                dict[(ushort)(startAddress + i)] = result[i];
            }
            return dict;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    #endregion
    
    #region 离散输入读取
    
    /// <summary>
    /// 读取单个离散输入
    /// </summary>
    public static async Task<bool?> ReadDiscreteInputAsync(this IModbusMaster master, ushort address, Func<Exception, Task>? onError = null)
    {
        try
        {
            var result = await master.ReadDiscreteInputsAsync(address, 1);
            return result[0];
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    /// <summary>
    /// 读取多个离散输入并返回字典
    /// </summary>
    public static async Task<Dictionary<ushort, bool>?> ReadDiscreteInputsToDictAsync(this IModbusMaster master, ushort startAddress, ushort count, Func<Exception, Task>? onError = null)
    {
        try
        {
            var result = await master.ReadDiscreteInputsAsync(startAddress, count);
            var dict = new Dictionary<ushort, bool>();
            for (int i = 0; i < result.Length; i++)
            {
                dict[(ushort)(startAddress + i)] = result[i];
            }
            return dict;
        }
        catch (Exception ex)
        {
            if (onError != null) await onError(ex);
            return null;
        }
    }
    
    #endregion
}
