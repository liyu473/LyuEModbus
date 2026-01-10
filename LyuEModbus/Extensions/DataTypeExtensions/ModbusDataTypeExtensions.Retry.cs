using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// 重试执行方法
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    private static async Task<T?> ExecuteWithRetryAsync<T>(
        IModbusClient client,
        Func<Task<T>> action,
        int retryCount,
        Func<Exception, Task>? onError,
        string operationName) where T : struct
    {
        var masterClient = client as IModbusMasterClient;
        var requestLock = masterClient?.RequestLock;
        
        var attempts = 0;
        while (true)
        {
            try
            {
                // 获取请求锁，确保串行执行
                if (requestLock != null)
                    await requestLock.WaitAsync();
                
                try
                {
                    var result = await action();
                    client.Log(ModbusLogLevel.Debug, $"{operationName} 成功: {result}");
                    return result;
                }
                finally
                {
                    requestLock?.Release();
                }
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
        string operationName) where T : class
    {
        var masterClient = client as IModbusMasterClient;
        var requestLock = masterClient?.RequestLock;
        
        var attempts = 0;
        while (true)
        {
            try
            {
                // 获取请求锁，确保串行执行
                if (requestLock != null)
                    await requestLock.WaitAsync();
                
                try
                {
                    var result = await action();
                    client.Log(ModbusLogLevel.Debug, $"{operationName} 成功");
                    return result;
                }
                finally
                {
                    requestLock?.Release();
                }
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
        var masterClient = client as IModbusMasterClient;
        var requestLock = masterClient?.RequestLock;
        
        var attempts = 0;
        while (true)
        {
            try
            {
                // 获取请求锁，确保串行执行
                if (requestLock != null)
                    await requestLock.WaitAsync();
                
                try
                {
                    await action();
                    client.Log(ModbusLogLevel.Debug, $"{operationName} 成功");
                    return true;
                }
                finally
                {
                    requestLock?.Release();
                }
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
