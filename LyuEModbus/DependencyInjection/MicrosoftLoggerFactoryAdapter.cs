using LyuEModbus.Abstractions;
using Microsoft.Extensions.Logging;
using NModbus;

namespace LyuEModbus.DependencyInjection;

/// <summary>
/// Microsoft.Extensions.Logging 日志工厂适配器
/// </summary>
internal class MicrosoftLoggerFactoryAdapter(ILoggerFactory loggerFactory) : IModbusLoggerFactory
{
    public IModbusLogger CreateLogger(string name)
    {
        var logger = loggerFactory.CreateLogger($"LyuEModbus.{name}");
        return new MicrosoftLoggerAdapter(logger);
    }
}
