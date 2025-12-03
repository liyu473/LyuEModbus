using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// 重试执行方法
/// </summary>
public static partial class ModbusMasterExtensions
{
    private static async Task<T?> ExecuteWithRetryAsync<T>(
        IModbusClient client,
        Func<Task<T>> action,
        int retryCount,
        Func<Exception, Task>? onError,
        string operationName) where T : struct
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var result = await action();
                client.Log(ModbusLogLevel.Debug, $"{operationName} 成功: {result}");
                return result;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    client.Log(ModbusLogLevel.Error, $"{operationName} 失败: {ex.Message}");
                    if (onError != null)
                        await onError(ex);
                    return null;
                }
                client.Log(ModbusLogLevel.Warning, $"{operationName} 重试 {attempts}/{retryCount}: {ex.Message}");
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }

    private static async Task<T?> ExecuteWithRetryRefAsync<T>(
        IModbusClient client,
        Func<Task<T>> action,
        int retryCount,
        Func<Exception, Task>? onError,
        string operationName,
        Func<T, string>? formatResult = null) where T : class
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var result = await action();
                var resultStr = formatResult != null ? formatResult(result) : result.ToString();
                client.Log(ModbusLogLevel.Debug, $"{operationName} 成功: {resultStr}");
                return result;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    client.Log(ModbusLogLevel.Error, $"{operationName} 失败: {ex.Message}");
                    if (onError != null)
                        await onError(ex);
                    return null;
                }
                client.Log(ModbusLogLevel.Warning, $"{operationName} 重试 {attempts}/{retryCount}: {ex.Message}");
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }

    private static async Task<bool> ExecuteWithRetryBoolAsync(
        IModbusClient client,
        Func<Task> action,
        int retryCount,
        Func<Exception, Task>? onError,
        string operationName)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                await action();
                client.Log(ModbusLogLevel.Debug, $"{operationName} 成功");
                return true;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    client.Log(ModbusLogLevel.Error, $"{operationName} 失败: {ex.Message}");
                    if (onError != null)
                        await onError(ex);
                    return false;
                }
                client.Log(ModbusLogLevel.Warning, $"{operationName} 重试 {attempts}/{retryCount}: {ex.Message}");
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }
}
