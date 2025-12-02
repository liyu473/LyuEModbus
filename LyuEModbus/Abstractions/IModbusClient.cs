namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 客户端连接状态
/// </summary>
public enum ModbusConnectionState
{
    /// <summary>
    /// 未连接
    /// </summary>
    Disconnected,
    
    /// <summary>
    /// 连接中
    /// </summary>
    Connecting,
    
    /// <summary>
    /// 已连接
    /// </summary>
    Connected,
    
    /// <summary>
    /// 重连中
    /// </summary>
    Reconnecting
}

/// <summary>
/// Modbus 客户端基础接口
/// </summary>
public interface IModbusClient : IDisposable
{
    /// <summary>
    /// 客户端唯一标识
    /// </summary>
    string ClientId { get; }
    
    /// <summary>
    /// 客户端名称（用于日志和标识）
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 连接地址（IP:Port 格式）
    /// </summary>
    string Address { get; }
    
    /// <summary>
    /// 从站ID
    /// </summary>
    byte SlaveId { get; }
    
    /// <summary>
    /// 连接状态
    /// </summary>
    ModbusConnectionState State { get; }
    
    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    event Action<ModbusConnectionState>? StateChanged;
    
    /// <summary>
    /// 异步连接状态变化事件
    /// </summary>
    event Func<ModbusConnectionState, Task>? StateChangedAsync;
}

/// <summary>
/// Modbus 日志工厂接口（用于创建 NModbus.IModbusLogger）
/// </summary>
public interface IModbusLoggerFactory
{
    /// <summary>
    /// 创建指定名称的日志记录器
    /// </summary>
    NModbus.IModbusLogger CreateLogger(string name);
}

/// <summary>
/// 默认控制台日志记录器
/// </summary>
public class ConsoleModbusLogger : NModbus.IModbusLogger
{
    private readonly string _name;
    
    public ConsoleModbusLogger(string name)
    {
        _name = name;
    }
    
    public void Log(NModbus.LoggingLevel level, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var levelStr = level switch
        {
            NModbus.LoggingLevel.Debug => "DBG",
            NModbus.LoggingLevel.Information => "INF",
            NModbus.LoggingLevel.Warning => "WRN",
            NModbus.LoggingLevel.Error => "ERR",
            _ => "???"
        };
        
        Console.WriteLine($"[{timestamp}] [{levelStr}] [{_name}] {message}");
    }
    
    public bool ShouldLog(NModbus.LoggingLevel level) => true;
}

/// <summary>
/// 默认控制台日志工厂
/// </summary>
public class ConsoleModbusLoggerFactory : IModbusLoggerFactory
{
    public NModbus.IModbusLogger CreateLogger(string name) => new ConsoleModbusLogger(name);
}

/// <summary>
/// 空日志记录器（不输出任何日志）
/// </summary>
public class NullModbusLogger : NModbus.IModbusLogger
{
    public static readonly NullModbusLogger Instance = new();
    
    public void Log(NModbus.LoggingLevel level, string message) { }
    
    public bool ShouldLog(NModbus.LoggingLevel level) => false;
}

/// <summary>
/// 空日志工厂
/// </summary>
public class NullModbusLoggerFactory : IModbusLoggerFactory
{
    public static readonly NullModbusLoggerFactory Instance = new();
    
    public NModbus.IModbusLogger CreateLogger(string name) => NullModbusLogger.Instance;
}
