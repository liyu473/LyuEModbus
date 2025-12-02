using LyuEModbus.Models;

namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 工厂接口
/// </summary>
public interface IEModbusFactory
{
    IModbusMasterClient CreateTcpMaster(string name, ModbusMasterOptions? options = null);
    IModbusSlaveClient CreateTcpSlave(string name, ModbusSlaveOptions? options = null);
    IModbusMasterClient? GetMaster(string name);
    IModbusSlaveClient? GetSlave(string name);
    IEnumerable<IModbusMasterClient> GetAllMasters();
    IEnumerable<IModbusSlaveClient> GetAllSlaves();
    bool RemoveMaster(string name);
    bool RemoveSlave(string name);
}
