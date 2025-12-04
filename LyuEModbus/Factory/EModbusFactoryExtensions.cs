using LyuEModbus.Models;

namespace LyuEModbus.Factory;

/// <summary>
/// EModbusFactory 扩展方法
/// </summary>
public static class EModbusFactoryExtensions
{
    /// <summary>
    /// 配置默认主站选项
    /// </summary>
    public static EModbusFactory ConfigureDefaultMaster(this EModbusFactory factory, Action<ModbusMasterOptions> configure)
    {
        configure(factory.DefaultMasterOptions);
        return factory;
    }

    /// <summary>
    /// 配置默认从站选项
    /// </summary>
    public static EModbusFactory ConfigureDefaultSlave(this EModbusFactory factory, Action<ModbusSlaveOptions> configure)
    {
        configure(factory.DefaultSlaveOptions);
        return factory;
    }
}
