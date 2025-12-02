using LyuEModbus.Abstractions;
using Microsoft.Extensions.Logging;
using NModbus;

namespace LyuEModbus.DependencyInjection;

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
