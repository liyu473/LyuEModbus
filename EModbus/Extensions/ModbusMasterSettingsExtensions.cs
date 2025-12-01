using EModbus.Model;
using LyuEModbus.Extensions;
using LyuEModbus.Services;

namespace EModbus.Extensions;

/// <summary>
/// ModbusTcpMaster 配置扩展
/// </summary>
public static class ModbusMasterSettingsExtensions
{
    /// <summary>
    /// 从 MasterSettings 配置主站
    /// </summary>
    public static ModbusTcpMaster WithSettings(this ModbusTcpMaster master, MasterSettings settings)
    {
        return master
            .WithAddress(settings.IpAddress, settings.Port)
            .WithSlaveId(settings.SlaveId)
            .WithTimeout(settings.ReadTimeout, settings.WriteTimeout);
    }
}
