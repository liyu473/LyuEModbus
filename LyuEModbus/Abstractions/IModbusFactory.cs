namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 工厂接口
/// </summary>
public interface IModbusFactory
{
    /// <summary>
    /// 创建 TCP 主站
    /// </summary>
    IModbusMaster CreateTcpMaster(string name, Action<ModbusMasterOptions> configure);
    
    /// <summary>
    /// 创建 TCP 主站
    /// </summary>
    IModbusMaster CreateTcpMaster(string name, ModbusMasterOptions options);
    
    /// <summary>
    /// 创建 TCP 从站
    /// </summary>
    IModbusSlave CreateTcpSlave(string name, Action<ModbusSlaveOptions> configure);
    
    /// <summary>
    /// 创建 TCP 从站
    /// </summary>
    IModbusSlave CreateTcpSlave(string name, ModbusSlaveOptions options);
    
    /// <summary>
    /// 获取已创建的主站
    /// </summary>
    IModbusMaster? GetMaster(string name);
    
    /// <summary>
    /// 获取已创建的从站
    /// </summary>
    IModbusSlave? GetSlave(string name);
    
    /// <summary>
    /// 获取所有主站
    /// </summary>
    IEnumerable<IModbusMaster> GetAllMasters();
    
    /// <summary>
    /// 获取所有从站
    /// </summary>
    IEnumerable<IModbusSlave> GetAllSlaves();
    
    /// <summary>
    /// 移除并释放主站
    /// </summary>
    bool RemoveMaster(string name);
    
    /// <summary>
    /// 移除并释放从站
    /// </summary>
    bool RemoveSlave(string name);
}
