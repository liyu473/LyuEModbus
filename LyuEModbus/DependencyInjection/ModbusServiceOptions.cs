using LyuEModbus.Models;

namespace LyuEModbus.DependencyInjection;

/// <summary>
/// Modbus 服务配置选项
/// </summary>
public class ModbusServiceOptions
{
    internal Dictionary<string, Action<ModbusMasterOptions>> MasterConfigurations { get; } = new();
    internal Dictionary<string, Action<ModbusSlaveOptions>> SlaveConfigurations { get; } = new();
    
    /// <summary>
    /// 添加预配置的 TCP 主站
    /// </summary>
    public ModbusServiceOptions AddTcpMaster(string name, Action<ModbusMasterOptions> configure)
    {
        MasterConfigurations[name] = configure;
        return this;
    }
    
    /// <summary>
    /// 添加预配置的 TCP 从站
    /// </summary>
    public ModbusServiceOptions AddTcpSlave(string name, Action<ModbusSlaveOptions> configure)
    {
        SlaveConfigurations[name] = configure;
        return this;
    }
}
