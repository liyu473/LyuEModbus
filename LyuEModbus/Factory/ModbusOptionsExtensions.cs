using LyuEModbus.Models;

namespace LyuEModbus.Factory;

/// <summary>
/// Modbus 配置选项扩展方法
/// </summary>
internal static class ModbusOptionsExtensions
{
    #region ModbusMasterOptions

    /// <summary>
    /// 合并主站配置（overrides 覆盖 defaults 中的非空值）
    /// </summary>
    public static ModbusMasterOptions MergeWith(this ModbusMasterOptions defaults, ModbusMasterOptions? overrides)
    {
        if (overrides == null) return defaults.Clone();

        return new ModbusMasterOptions
        {
            IpAddress = overrides.IpAddress ?? defaults.IpAddress,
            Port = overrides.Port ?? defaults.Port,
            SlaveId = overrides.SlaveId ?? defaults.SlaveId,
            ReadTimeout = overrides.ReadTimeout ?? defaults.ReadTimeout,
            WriteTimeout = overrides.WriteTimeout ?? defaults.WriteTimeout,
            AutoReconnect = overrides.AutoReconnect || defaults.AutoReconnect,
            ReconnectInterval = overrides.ReconnectInterval > 0 ? overrides.ReconnectInterval : defaults.ReconnectInterval,
            MaxReconnectAttempts = overrides.MaxReconnectAttempts > 0 ? overrides.MaxReconnectAttempts : defaults.MaxReconnectAttempts,
            EnableHeartbeat = overrides.EnableHeartbeat || defaults.EnableHeartbeat,
            HeartbeatInterval = overrides.HeartbeatInterval > 0 ? overrides.HeartbeatInterval : defaults.HeartbeatInterval,
            ByteOrder = overrides.ByteOrder != ByteOrder.ABCD ? overrides.ByteOrder : defaults.ByteOrder
        };
    }

    /// <summary>
    /// 克隆主站配置
    /// </summary>
    public static ModbusMasterOptions Clone(this ModbusMasterOptions source)
    {
        return new ModbusMasterOptions
        {
            IpAddress = source.IpAddress,
            Port = source.Port,
            SlaveId = source.SlaveId,
            ReadTimeout = source.ReadTimeout,
            WriteTimeout = source.WriteTimeout,
            AutoReconnect = source.AutoReconnect,
            ReconnectInterval = source.ReconnectInterval,
            MaxReconnectAttempts = source.MaxReconnectAttempts,
            EnableHeartbeat = source.EnableHeartbeat,
            HeartbeatInterval = source.HeartbeatInterval,
            ByteOrder = source.ByteOrder
        };
    }

    #endregion

    #region ModbusSlaveOptions

    /// <summary>
    /// 合并从站配置（overrides 覆盖 defaults 中的非空值）
    /// </summary>
    public static ModbusSlaveOptions MergeWith(this ModbusSlaveOptions defaults, ModbusSlaveOptions? overrides)
    {
        if (overrides == null) return defaults.Clone();

        return new ModbusSlaveOptions
        {
            IpAddress = overrides.IpAddress ?? defaults.IpAddress,
            Port = overrides.Port ?? defaults.Port,
            SlaveId = overrides.SlaveId ?? defaults.SlaveId,
            InitHoldingRegisterCount = overrides.InitHoldingRegisterCount ?? defaults.InitHoldingRegisterCount,
            InitCoilCount = overrides.InitCoilCount ?? defaults.InitCoilCount,
            ChangeDetectionInterval = overrides.ChangeDetectionInterval > 0 ? overrides.ChangeDetectionInterval : defaults.ChangeDetectionInterval
        };
    }

    /// <summary>
    /// 克隆从站配置
    /// </summary>
    public static ModbusSlaveOptions Clone(this ModbusSlaveOptions source)
    {
        return new ModbusSlaveOptions
        {
            IpAddress = source.IpAddress,
            Port = source.Port,
            SlaveId = source.SlaveId,
            InitHoldingRegisterCount = source.InitHoldingRegisterCount,
            InitCoilCount = source.InitCoilCount,
            ChangeDetectionInterval = source.ChangeDetectionInterval
        };
    }

    #endregion
}
