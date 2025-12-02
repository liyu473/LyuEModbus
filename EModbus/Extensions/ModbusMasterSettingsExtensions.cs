using EModbus.Model;
using LyuEModbus.Abstractions;

namespace EModbus.Extensions;

/// <summary>
/// ModbusMasterOptions 配置扩展
/// </summary>
public static class ModbusMasterSettingsExtensions
{
    /// <summary>
    /// 从 MasterSettings 配置主站选项
    /// </summary>
    public static ModbusMasterOptions FromSettings(this ModbusMasterOptions options, MasterSettings settings)
    {
        options.IpAddress = settings.IpAddress;
        options.Port = settings.Port;
        options.SlaveId = settings.SlaveId;
        options.ReadTimeout = settings.ReadTimeout;
        options.WriteTimeout = settings.WriteTimeout;
        return options;
    }
    
    /// <summary>
    /// 从 SlaveSettings 配置从站选项
    /// </summary>
    public static ModbusSlaveOptions FromSettings(this ModbusSlaveOptions options, SlaveSettings settings)
    {
        options.IpAddress = settings.IpAddress;
        options.Port = settings.Port;
        options.SlaveId = settings.SlaveId;
        return options;
    }
}
