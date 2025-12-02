using LyuEModbus.Abstractions;
using LyuEModbus.Factory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NModbus;

namespace LyuEModbus.DependencyInjection;

/// <summary>
/// Microsoft.Extensions.Logging 适配器
/// </summary>
internal class MicrosoftLoggerAdapter : IModbusLogger
{
    private readonly ILogger _logger;
    
    public MicrosoftLoggerAdapter(ILogger logger)
    {
        _logger = logger;
    }
    
    public void Log(LoggingLevel level, string message)
    {
        var logLevel = level switch
        {
            LoggingLevel.Debug => LogLevel.Debug,
            LoggingLevel.Information => LogLevel.Information,
            LoggingLevel.Warning => LogLevel.Warning,
            LoggingLevel.Error => LogLevel.Error,
            _ => LogLevel.Information
        };
        
        _logger.Log(logLevel, message);
    }
    
    public bool ShouldLog(LoggingLevel level) => true;
}

/// <summary>
/// Microsoft.Extensions.Logging 日志工厂适配器
/// </summary>
internal class MicrosoftLoggerFactoryAdapter : IModbusLoggerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    
    public MicrosoftLoggerFactoryAdapter(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }
    
    public IModbusLogger CreateLogger(string name)
    {
        var logger = _loggerFactory.CreateLogger($"LyuEModbus.{name}");
        return new MicrosoftLoggerAdapter(logger);
    }
}

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

/// <summary>
/// DI 扩展方法
/// </summary>
public static class ModbusServiceExtensions
{
    /// <summary>
    /// 添加 Modbus 服务
    /// </summary>
    public static IServiceCollection AddModbus(this IServiceCollection services)
    {
        return services.AddModbus(_ => { });
    }
    
    /// <summary>
    /// 添加 Modbus 服务并配置
    /// </summary>
    public static IServiceCollection AddModbus(this IServiceCollection services, Action<ModbusServiceOptions> configure)
    {
        var options = new ModbusServiceOptions();
        configure(options);
        
        services.AddSingleton<Abstractions.IModbusFactory>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            IModbusLoggerFactory modbusLoggerFactory = loggerFactory != null
                ? new MicrosoftLoggerFactoryAdapter(loggerFactory)
                : new ConsoleModbusLoggerFactory();
            
            var factory = new ModbusClientFactory(modbusLoggerFactory);
            
            foreach (var (name, masterConfigure) in options.MasterConfigurations)
                factory.CreateTcpMaster(name, masterConfigure);
            
            foreach (var (name, slaveConfigure) in options.SlaveConfigurations)
                factory.CreateTcpSlave(name, slaveConfigure);
            
            return factory;
        });
        
        return services;
    }
}
