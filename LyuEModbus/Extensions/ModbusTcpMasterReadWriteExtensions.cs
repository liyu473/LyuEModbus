using LyuEModbus.Services;

namespace LyuEModbus.Extensions;

/// <summary>
/// ModbusTcpMaster 读写操作扩展
/// </summary>
public static class ModbusTcpMasterReadWriteExtensions
{
    #region 线圈读取

    /// <summary>
    /// 读取单个线圈
    /// </summary>
    public static async Task<bool> ReadCoilAsync(this ModbusTcpMaster master, ushort address)
    {
        var result = await master.ReadCoilsAsync(address, 1);
        return result[0];
    }

    /// <summary>
    /// 读取多个线圈并返回字典
    /// </summary>
    public static async Task<Dictionary<ushort, bool>> ReadCoilsToDictAsync(this ModbusTcpMaster master, ushort startAddress, ushort count)
    {
        var result = await master.ReadCoilsAsync(startAddress, count);
        var dict = new Dictionary<ushort, bool>();
        for (int i = 0; i < result.Length; i++)
        {
            dict[(ushort)(startAddress + i)] = result[i];
        }
        return dict;
    }

    /// <summary>
    /// 读取指定地址列表的线圈
    /// </summary>
    public static async Task<Dictionary<ushort, bool>> ReadCoilsAsync(this ModbusTcpMaster master, params ushort[] addresses)
    {
        var dict = new Dictionary<ushort, bool>();
        foreach (var address in addresses)
        {
            dict[address] = await master.ReadCoilAsync(address);
        }
        return dict;
    }

    #endregion

    #region 线圈写入

    /// <summary>
    /// 写入单个线圈（链式调用）
    /// </summary>
    public static async Task<ModbusTcpMaster> WriteCoilAsync(this ModbusTcpMaster master, ushort address, bool value)
    {
        await master.WriteSingleCoilAsync(address, value);
        return master;
    }

    /// <summary>
    /// 写入多个线圈（链式调用）
    /// </summary>
    public static async Task<ModbusTcpMaster> WriteCoilsAsync(this ModbusTcpMaster master, ushort startAddress, params bool[] values)
    {
        await master.WriteMultipleCoilsAsync(startAddress, values);
        return master;
    }

    /// <summary>
    /// 批量写入线圈（字典方式）
    /// </summary>
    public static async Task<ModbusTcpMaster> WriteCoilsAsync(this ModbusTcpMaster master, Dictionary<ushort, bool> coils)
    {
        foreach (var kvp in coils)
        {
            await master.WriteSingleCoilAsync(kvp.Key, kvp.Value);
        }
        return master;
    }

    /// <summary>
    /// 切换线圈状态
    /// </summary>
    public static async Task<bool> ToggleCoilAsync(this ModbusTcpMaster master, ushort address)
    {
        var current = await master.ReadCoilAsync(address);
        var newValue = !current;
        await master.WriteSingleCoilAsync(address, newValue);
        return newValue;
    }

    #endregion

    #region 保持寄存器读取

    /// <summary>
    /// 读取单个保持寄存器
    /// </summary>
    public static async Task<ushort> ReadHoldingRegisterAsync(this ModbusTcpMaster master, ushort address)
    {
        var result = await master.ReadHoldingRegistersAsync(address, 1);
        return result[0];
    }

    /// <summary>
    /// 读取多个保持寄存器并返回字典
    /// </summary>
    public static async Task<Dictionary<ushort, ushort>> ReadHoldingRegistersToDictAsync(this ModbusTcpMaster master, ushort startAddress, ushort count)
    {
        var result = await master.ReadHoldingRegistersAsync(startAddress, count);
        var dict = new Dictionary<ushort, ushort>();
        for (int i = 0; i < result.Length; i++)
        {
            dict[(ushort)(startAddress + i)] = result[i];
        }
        return dict;
    }

    /// <summary>
    /// 读取指定地址列表的保持寄存器
    /// </summary>
    public static async Task<Dictionary<ushort, ushort>> ReadHoldingRegistersAsync(this ModbusTcpMaster master, params ushort[] addresses)
    {
        var dict = new Dictionary<ushort, ushort>();
        foreach (var address in addresses)
        {
            dict[address] = await master.ReadHoldingRegisterAsync(address);
        }
        return dict;
    }

    /// <summary>
    /// 读取32位整数（两个连续寄存器）
    /// </summary>
    public static async Task<int> ReadInt32Async(this ModbusTcpMaster master, ushort startAddress, bool bigEndian = true)
    {
        var result = await master.ReadHoldingRegistersAsync(startAddress, 2);
        if (bigEndian)
            return (result[0] << 16) | result[1];
        else
            return (result[1] << 16) | result[0];
    }

    /// <summary>
    /// 读取32位浮点数（两个连续寄存器）
    /// </summary>
    public static async Task<float> ReadFloatAsync(this ModbusTcpMaster master, ushort startAddress, bool bigEndian = true)
    {
        var result = await master.ReadHoldingRegistersAsync(startAddress, 2);
        byte[] bytes;
        if (bigEndian)
            bytes = new byte[] { (byte)(result[1] & 0xFF), (byte)(result[1] >> 8), (byte)(result[0] & 0xFF), (byte)(result[0] >> 8) };
        else
            bytes = new byte[] { (byte)(result[0] & 0xFF), (byte)(result[0] >> 8), (byte)(result[1] & 0xFF), (byte)(result[1] >> 8) };
        return BitConverter.ToSingle(bytes, 0);
    }

    #endregion

    #region 保持寄存器写入

    /// <summary>
    /// 写入单个保持寄存器（链式调用）
    /// </summary>
    public static async Task<ModbusTcpMaster> WriteRegisterAsync(this ModbusTcpMaster master, ushort address, ushort value)
    {
        await master.WriteSingleRegisterAsync(address, value);
        return master;
    }

    /// <summary>
    /// 写入多个保持寄存器（链式调用）
    /// </summary>
    public static async Task<ModbusTcpMaster> WriteRegistersAsync(this ModbusTcpMaster master, ushort startAddress, params ushort[] values)
    {
        await master.WriteMultipleRegistersAsync(startAddress, values);
        return master;
    }

    /// <summary>
    /// 批量写入保持寄存器（字典方式）
    /// </summary>
    public static async Task<ModbusTcpMaster> WriteRegistersAsync(this ModbusTcpMaster master, Dictionary<ushort, ushort> registers)
    {
        foreach (var kvp in registers)
        {
            await master.WriteSingleRegisterAsync(kvp.Key, kvp.Value);
        }
        return master;
    }

    /// <summary>
    /// 写入32位整数（两个连续寄存器）
    /// </summary>
    public static async Task<ModbusTcpMaster> WriteInt32Async(this ModbusTcpMaster master, ushort startAddress, int value, bool bigEndian = true)
    {
        ushort high = (ushort)(value >> 16);
        ushort low = (ushort)(value & 0xFFFF);
        if (bigEndian)
            await master.WriteMultipleRegistersAsync(startAddress, new[] { high, low });
        else
            await master.WriteMultipleRegistersAsync(startAddress, new[] { low, high });
        return master;
    }

    /// <summary>
    /// 写入32位浮点数（两个连续寄存器）
    /// </summary>
    public static async Task<ModbusTcpMaster> WriteFloatAsync(this ModbusTcpMaster master, ushort startAddress, float value, bool bigEndian = true)
    {
        var bytes = BitConverter.GetBytes(value);
        ushort low = (ushort)(bytes[0] | (bytes[1] << 8));
        ushort high = (ushort)(bytes[2] | (bytes[3] << 8));
        if (bigEndian)
            await master.WriteMultipleRegistersAsync(startAddress, new[] { high, low });
        else
            await master.WriteMultipleRegistersAsync(startAddress, new[] { low, high });
        return master;
    }

    /// <summary>
    /// 寄存器值增加
    /// </summary>
    public static async Task<ushort> IncrementRegisterAsync(this ModbusTcpMaster master, ushort address, ushort increment = 1)
    {
        var current = await master.ReadHoldingRegisterAsync(address);
        var newValue = (ushort)(current + increment);
        await master.WriteSingleRegisterAsync(address, newValue);
        return newValue;
    }

    /// <summary>
    /// 寄存器值减少
    /// </summary>
    public static async Task<ushort> DecrementRegisterAsync(this ModbusTcpMaster master, ushort address, ushort decrement = 1)
    {
        var current = await master.ReadHoldingRegisterAsync(address);
        var newValue = (ushort)(current - decrement);
        await master.WriteSingleRegisterAsync(address, newValue);
        return newValue;
    }

    #endregion

    #region 输入寄存器读取

    /// <summary>
    /// 读取单个输入寄存器
    /// </summary>
    public static async Task<ushort> ReadInputRegisterAsync(this ModbusTcpMaster master, ushort address)
    {
        var result = await master.ReadInputRegistersAsync(address, 1);
        return result[0];
    }

    /// <summary>
    /// 读取多个输入寄存器并返回字典
    /// </summary>
    public static async Task<Dictionary<ushort, ushort>> ReadInputRegistersToDictAsync(this ModbusTcpMaster master, ushort startAddress, ushort count)
    {
        var result = await master.ReadInputRegistersAsync(startAddress, count);
        var dict = new Dictionary<ushort, ushort>();
        for (int i = 0; i < result.Length; i++)
        {
            dict[(ushort)(startAddress + i)] = result[i];
        }
        return dict;
    }

    #endregion

    #region 离散输入读取

    /// <summary>
    /// 读取单个离散输入
    /// </summary>
    public static async Task<bool> ReadInputAsync(this ModbusTcpMaster master, ushort address)
    {
        var result = await master.ReadInputsAsync(address, 1);
        return result[0];
    }

    /// <summary>
    /// 读取多个离散输入并返回字典
    /// </summary>
    public static async Task<Dictionary<ushort, bool>> ReadInputsToDictAsync(this ModbusTcpMaster master, ushort startAddress, ushort count)
    {
        var result = await master.ReadInputsAsync(startAddress, count);
        var dict = new Dictionary<ushort, bool>();
        for (int i = 0; i < result.Length; i++)
        {
            dict[(ushort)(startAddress + i)] = result[i];
        }
        return dict;
    }

    #endregion
}
