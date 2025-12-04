using LyuEModbus.Models;

namespace LyuEModbus.DependencyInjection;

/// <summary>
/// Modbus 服务配置选项
/// </summary>
public class ModbusServiceOptions
{
    internal Dictionary<string, Action<ModbusMasterOptions>> MasterConfigurations { get; } = [];
    internal Dictionary<string, Action<ModbusSlaveOptions>> SlaveConfigurations { get; } = [];
    internal Action<ModbusMasterOptions>? DefaultMasterConfigure { get; private set; }
    internal Action<ModbusSlaveOptions>? DefaultSlaveConfigure { get; private set; }

    /// <summary>
    /// 配置默认主站选项（所有新创建的主站都会继承这些配置）
    /// </summary>
    public ModbusServiceOptions ConfigureDefaultMaster(Action<ModbusMasterOptions> configure)
    {
        DefaultMasterConfigure = configure;
        return this;
    }

    /// <summary>
    /// 配置默认从站选项（所有新创建的从站都会继承这些配置）
    /// </summary>
    public ModbusServiceOptions ConfigureDefaultSlave(Action<ModbusSlaveOptions> configure)
    {
        DefaultSlaveConfigure = configure;
        return this;
    }

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
