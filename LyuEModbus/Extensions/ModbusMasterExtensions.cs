using LyuEModbus.Abstractions;

namespace LyuEModbus.Extensions;

/// <summary>
/// IModbusMasterClient 读写扩展方法
/// </summary>
public static partial class ModbusMasterExtensions
{
    #region 线圈读取

    public static async Task<bool?> ReadCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var result = await master.ReadCoilsAsync(master.SlaveId, address, 1);
            return result[0];
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    public static async Task<Dictionary<ushort, bool>?> ReadCoilsToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var result = await master.ReadCoilsAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, bool>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    #endregion

    #region 线圈写入

    public static async Task<bool> WriteCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        bool value,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            await master.WriteSingleCoilAsync(master.SlaveId, address, value);
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return false;
        }
    }

    public static async Task<bool> WriteCoilsAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        bool[] values,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            await master.WriteMultipleCoilsAsync(master.SlaveId, startAddress, values);
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return false;
        }
    }

    public static async Task<bool?> ToggleCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var current = await master.ReadCoilAsync(address);
            if (current is null)
                return null;
            var newValue = !current.Value;
            await master.WriteSingleCoilAsync(master.SlaveId, address, newValue);
            return newValue;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    #endregion

    #region 保持寄存器读取

    public static async Task<ushort?> ReadHoldingRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            return result[0];
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    public static async Task<Dictionary<ushort, ushort>?> ReadHoldingRegistersToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var result = await master.ReadHoldingRegistersAsync(
                master.SlaveId,
                startAddress,
                count
            );
            var dict = new Dictionary<ushort, ushort>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    public static async Task<int?> ReadInt32Async(
        this IModbusMasterClient master,
        ushort startAddress,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, 2);
            return bigEndian ? (result[0] << 16) | result[1] : (result[1] << 16) | result[0];
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    public static async Task<float?> ReadFloatAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, 2);
            byte[] bytes = bigEndian
                ?
                [
                    (byte)(result[1] & 0xFF),
                    (byte)(result[1] >> 8),
                    (byte)(result[0] & 0xFF),
                    (byte)(result[0] >> 8),
                ]
                :
                [
                    (byte)(result[0] & 0xFF),
                    (byte)(result[0] >> 8),
                    (byte)(result[1] & 0xFF),
                    (byte)(result[1] >> 8),
                ];
            return BitConverter.ToSingle(bytes, 0);
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    #endregion

    #region 保持寄存器写入

    public static async Task<bool> WriteRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        ushort value,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            await master.WriteSingleRegisterAsync(master.SlaveId, address, value);
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return false;
        }
    }

    public static async Task<bool> WriteRegistersAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort[] values,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            await master.WriteMultipleRegistersAsync(master.SlaveId, startAddress, values);
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return false;
        }
    }

    public static async Task<bool> WriteInt32Async(
        this IModbusMasterClient master,
        ushort startAddress,
        int value,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            ushort high = (ushort)(value >> 16);
            ushort low = (ushort)(value & 0xFFFF);
            await master.WriteMultipleRegistersAsync(
                master.SlaveId,
                startAddress,
                bigEndian ? [high, low] : [low, high]
            );
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return false;
        }
    }

    public static async Task<bool> WriteFloatAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        float value,
        bool bigEndian = true,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var bytes = BitConverter.GetBytes(value);
            ushort low = (ushort)(bytes[0] | (bytes[1] << 8));
            ushort high = (ushort)(bytes[2] | (bytes[3] << 8));
            await master.WriteMultipleRegistersAsync(
                master.SlaveId,
                startAddress,
                bigEndian ? [high, low] : [low, high]
            );
            return true;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return false;
        }
    }

    public static async Task<ushort?> IncrementRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        ushort increment = 1,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var current = await master.ReadHoldingRegisterAsync(address);
            if (current is null)
                return null;
            var newValue = (ushort)(current.Value + increment);
            await master.WriteSingleRegisterAsync(master.SlaveId, address, newValue);
            return newValue;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    #endregion

    #region 输入寄存器读取

    public static async Task<ushort?> ReadInputRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var result = await master.ReadInputRegistersAsync(master.SlaveId, address, 1);
            return result[0];
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    public static async Task<Dictionary<ushort, ushort>?> ReadInputRegistersToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var result = await master.ReadInputRegistersAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, ushort>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    #endregion

    #region 离散输入读取

    public static async Task<bool?> ReadDiscreteInputAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var result = await master.ReadInputsAsync(master.SlaveId, address, 1);
            return result[0];
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    public static async Task<Dictionary<ushort, bool>?> ReadDiscreteInputsToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null
    )
    {
        try
        {
            var result = await master.ReadInputsAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, bool>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }
        catch (Exception ex)
        {
            if (onError != null)
                await onError(ex);
            return null;
        }
    }

    #endregion
}
