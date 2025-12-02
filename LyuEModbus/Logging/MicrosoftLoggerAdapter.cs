using LyuEModbus.Abstractions;
using Microsoft.Extensions.Logging;
using NModbus;

namespace LyuEModbus.Logging;

/// <summary>
/// Microsoft.Extensions.Logging 适配器 - 将 ILoggerFactory 适配为 IModbusLoggerFactory
/// </summary>
public class MicrosoftModbusLoggerFactory(ILoggerFactory loggerFactory) : IModbusLoggerFactory
{
    public IModbusLogger CreateLogger(string categoryName)
    {
        return new MicrosoftModbusLogger(loggerFactory.CreateLogger(categoryName));
    }
}

/// <summary>
/// Microsoft.Extensions.Logging 适配器 - 将 ILogger 适配为 IModbusLogger
/// </summary>
internal class MicrosoftModbusLogger(ILogger logger) : IModbusLogger
{
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

        if (logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, "{Message}", message);
        }
    }

    public bool ShouldLog(LoggingLevel level)
    {
        var logLevel = level switch
        {
            LoggingLevel.Debug => LogLevel.Debug,
            LoggingLevel.Information => LogLevel.Information,
            LoggingLevel.Warning => LogLevel.Warning,
            LoggingLevel.Error => LogLevel.Error,
            _ => LogLevel.Information
        };

        return logger.IsEnabled(logLevel);
    }
}
