using LyuEModbus.Abstractions;

namespace LyuEModbus.Extensions;

/// <summary>
/// 寄存器位操作扩展方法
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region 读取寄存器的位

    /// <summary>
    /// 读取一个寄存器的所有16个位
    /// </summary>
    /// <param name="master">Modbus主站客户端</param>
    /// <param name="address">寄存器地址</param>
    /// <param name="onError">错误回调（可选）</param>
    /// <param name="retryCount">重试次数（默认0）</param>
    /// <returns>16个布尔值数组，索引0为最低位(bit0)，失败返回null</returns>
    public static async Task<bool[]?> ReadRegisterBitsAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryRefAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            return UShortToBits(registers[0]);
        }, retryCount, onError, $"ReadRegisterBits({address})");
    }

    /// <summary>
    /// 读取一个寄存器的指定位
    /// </summary>
    /// <param name="master">Modbus主站客户端</param>
    /// <param name="address">寄存器地址</param>
    /// <param name="bitIndex">位索引（0-15，0为最低位）</param>
    /// <param name="onError">错误回调（可选）</param>
    /// <param name="retryCount">重试次数（默认0）</param>
    /// <returns>位状态，失败返回null</returns>
    public static async Task<bool?> ReadRegisterBitAsync(
        this IModbusMasterClient master,
        ushort address,
        int bitIndex,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        if (bitIndex is < 0 or > 15)
            throw new ArgumentOutOfRangeException(nameof(bitIndex), "位索引必须在0-15之间");

        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            return (registers[0] & (1 << bitIndex)) != 0;
        }, retryCount, onError, $"ReadRegisterBit({address}, bit{bitIndex})");
    }

    /// <summary>
    /// 读取一个寄存器的所有16个位（同步版本）
    /// </summary>
    public static bool[]? ReadRegisterBits(this IModbusMasterClient master, ushort address)
    {
        try
        {
            var registers = master.ReadHoldingRegisters(master.SlaveId, address, 1);
            return UShortToBits(registers[0]);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region 写入寄存器的位

    /// <summary>
    /// 写入寄存器的指定位（读-改-写）
    /// </summary>
    public static async Task<bool> WriteRegisterBitAsync(
        this IModbusMasterClient master,
        ushort address,
        int bitIndex,
        bool value,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        if (bitIndex is < 0 or > 15)
            throw new ArgumentOutOfRangeException(nameof(bitIndex), "位索引必须在0-15之间");

        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            var newValue = value
                ? (ushort)(registers[0] | (1 << bitIndex))
                : (ushort)(registers[0] & ~(1 << bitIndex));
            await master.WriteSingleRegisterAsync(master.SlaveId, address, newValue);
        }, retryCount, onError, $"WriteRegisterBit({address}, bit{bitIndex}, {value})");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// ushort转16位布尔数组
    /// </summary>
    private static bool[] UShortToBits(ushort value)
    {
        var bits = new bool[16];
        for (int i = 0; i < 16; i++)
            bits[i] = (value & (1 << i)) != 0;
        return bits;
    }

    #endregion
}
