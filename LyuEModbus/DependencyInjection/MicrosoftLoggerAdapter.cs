using Microsoft.Extensions.Logging;
using NModbus;

namespace LyuEModbus.DependencyInjection;

/// <summary>
/// Microsoft.Extensions.Logging 适配器
/// </summary>
internal class MicrosoftLoggerAdapter(ILogger logger) : IModbusLogger
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
    
    public bool ShouldLog(LoggingLevel level) => level switch
    {
        LoggingLevel.Debug => logger.IsEnabled(LogLevel.Debug),
        LoggingLevel.Information => logger.IsEnabled(LogLevel.Information),
        LoggingLevel.Warning => logger.IsEnabled(LogLevel.Warning),
        LoggingLevel.Error => logger.IsEnabled(LogLevel.Error),
        _ => logger.IsEnabled(LogLevel.Information)
    };
}
