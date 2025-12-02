namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 工厂接口
/// </summary>
public interface IModbusFactory
{
    IModbusMasterClient CreateTcpMaster(string name, Action<ModbusMasterOptions> configure);
    IModbusMasterClient CreateTcpMaster(string name, ModbusMasterOptions options);
    IModbusSlaveClient CreateTcpSlave(string name, Action<ModbusSlaveOptions> configure);
    IModbusSlaveClient CreateTcpSlave(string name, ModbusSlaveOptions options);
    IModbusMasterClient? GetMaster(string name);
    IModbusSlaveClient? GetSlave(string name);
    IEnumerable<IModbusMasterClient> GetAllMasters();
    IEnumerable<IModbusSlaveClient> GetAllSlaves();
    bool RemoveMaster(string name);
    bool RemoveSlave(string name);
}
