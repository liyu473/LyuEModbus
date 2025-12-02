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
