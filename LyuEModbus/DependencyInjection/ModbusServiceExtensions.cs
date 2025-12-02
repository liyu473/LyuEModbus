using LyuEModbus.Abstractions;
using LyuEModbus.Factory;
using LyuEModbus.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LyuEModbus.DependencyInjection;

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
        
        services.AddSingleton<IModbusFactory>(sp =>
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
