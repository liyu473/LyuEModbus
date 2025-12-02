using LyuEModbus.Abstractions;
using LyuEModbus.Models;
using System.Collections.Concurrent;

namespace LyuEModbus.Core;

/// <summary>
/// 轮询任务项
/// </summary>
internal class PollTask
{
    public required string Name { get; init; }
    public required Func<CancellationToken, Task> Action { get; init; }
    public required int IntervalMs { get; init; }
    public DateTime LastExecuted { get; set; } = DateTime.MinValue;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Modbus 轮询组 - 串行队列模式，避免并发冲突
/// </summary>
public class ModbusPollerGroup : IDisposable
{
    private readonly IModbusMasterClient _master;
    private readonly ConcurrentDictionary<string, PollTask> _tasks = new();
    private readonly List<Func<string, Exception, Task>> _errorHandlers = [];
    private readonly SemaphoreSlim _pauseSemaphore = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;
    private int _baseIntervalMs = 100; // 基础循环间隔

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 是否已暂停
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// 任务数量
    /// </summary>
    public int Count => _tasks.Count;

    /// <summary>
    /// 获取所有任务名称
    /// </summary>
    public IEnumerable<string> TaskNames => _tasks.Keys;

    internal ModbusPollerGroup(IModbusMasterClient master)
    {
        _master = master ?? throw new ArgumentNullException(nameof(master));
    }

    #region 添加轮询任务

    /// <summary>
    /// 添加保持寄存器轮询任务
    /// </summary>
    public ModbusPollerGroup AddHoldingRegisters(
        string name,
        ushort startAddress,
        ushort count,
        int intervalMs,
        Func<Dictionary<ushort, ushort>, Task> onData)
    {
        _tasks[name] = new PollTask
        {
            Name = name,
            IntervalMs = intervalMs,
            Action = async ct =>
            {
                var result = await _master.ReadHoldingRegistersAsync(_master.SlaveId, startAddress, count);
                var dict = new Dictionary<ushort, ushort>();
                for (int i = 0; i < result.Length; i++)
                    dict[(ushort)(startAddress + i)] = result[i];
                await onData(dict);
            }
        };
        return this;
    }

    /// <summary>
    /// 添加单个保持寄存器轮询任务
    /// </summary>
    public ModbusPollerGroup AddHoldingRegister(
        string name,
        ushort address,
        int intervalMs,
        Func<ushort, Task> onData)
    {
        _tasks[name] = new PollTask
        {
            Name = name,
            IntervalMs = intervalMs,
            Action = async ct =>
            {
                var result = await _master.ReadHoldingRegistersAsync(_master.SlaveId, address, 1);
                await onData(result[0]);
            }
        };
        return this;
    }

    /// <summary>
    /// 添加线圈轮询任务
    /// </summary>
    public ModbusPollerGroup AddCoils(
        string name,
        ushort startAddress,
        ushort count,
        int intervalMs,
        Func<Dictionary<ushort, bool>, Task> onData)
    {
        _tasks[name] = new PollTask
        {
            Name = name,
            IntervalMs = intervalMs,
            Action = async ct =>
            {
                var result = await _master.ReadCoilsAsync(_master.SlaveId, startAddress, count);
                var dict = new Dictionary<ushort, bool>();
                for (int i = 0; i < result.Length; i++)
                    dict[(ushort)(startAddress + i)] = result[i];
                await onData(dict);
            }
        };
        return this;
    }

    /// <summary>
    /// 添加输入寄存器轮询任务
    /// </summary>
    public ModbusPollerGroup AddInputRegisters(
        string name,
        ushort startAddress,
        ushort count,
        int intervalMs,
        Func<Dictionary<ushort, ushort>, Task> onData)
    {
        _tasks[name] = new PollTask
        {
            Name = name,
            IntervalMs = intervalMs,
            Action = async ct =>
            {
                var result = await _master.ReadInputRegistersAsync(_master.SlaveId, startAddress, count);
                var dict = new Dictionary<ushort, ushort>();
                for (int i = 0; i < result.Length; i++)
                    dict[(ushort)(startAddress + i)] = result[i];
                await onData(dict);
            }
        };
        return this;
    }

    /// <summary>
    /// 添加离散输入轮询任务
    /// </summary>
    public ModbusPollerGroup AddDiscreteInputs(
        string name,
        ushort startAddress,
        ushort count,
        int intervalMs,
        Func<Dictionary<ushort, bool>, Task> onData)
    {
        _tasks[name] = new PollTask
        {
            Name = name,
            IntervalMs = intervalMs,
            Action = async ct =>
            {
                var result = await _master.ReadInputsAsync(_master.SlaveId, startAddress, count);
                var dict = new Dictionary<ushort, bool>();
                for (int i = 0; i < result.Length; i++)
                    dict[(ushort)(startAddress + i)] = result[i];
                await onData(dict);
            }
        };
        return this;
    }

    /// <summary>
    /// 添加自定义轮询任务
    /// </summary>
    public ModbusPollerGroup Add(
        string name,
        int intervalMs,
        Func<IModbusMasterClient, Task> action)
    {
        _tasks[name] = new PollTask
        {
            Name = name,
            IntervalMs = intervalMs,
            Action = async ct => await action(_master)
        };
        return this;
    }

    #endregion

    #region 配置

    /// <summary>
    /// 配置全局错误处理（参数：任务名称, 异常）
    /// </summary>
    public ModbusPollerGroup OnError(Func<string, Exception, Task> handler)
    {
        _errorHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// 配置基础循环间隔（默认100ms）
    /// </summary>
    public ModbusPollerGroup WithBaseInterval(int intervalMs)
    {
        _baseIntervalMs = Math.Max(10, intervalMs);
        return this;
    }



    #endregion

    #region 控制

    /// <summary>
    /// 启动轮询组
    /// </summary>
    public void Start()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;
        IsPaused = false;
        _loopTask = RunLoopAsync(_cts.Token);
    }

    /// <summary>
    /// 停止轮询组
    /// </summary>
    public void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        if (IsPaused)
        {
            try { _pauseSemaphore.Release(); } catch { }
        }

        IsRunning = false;
        IsPaused = false;
        _cts = null;
    }

    /// <summary>
    /// 暂停所有轮询
    /// </summary>
    public void PauseAll()
    {
        if (!IsRunning || IsPaused) return;
        _pauseSemaphore.Wait();
        IsPaused = true;
    }

    /// <summary>
    /// 继续所有轮询
    /// </summary>
    public void ResumeAll()
    {
        if (!IsRunning || !IsPaused) return;
        IsPaused = false;
        _pauseSemaphore.Release();
    }

    /// <summary>
    /// 暂停指定任务
    /// </summary>
    public void Pause(string name)
    {
        if (_tasks.TryGetValue(name, out var task))
            task.IsEnabled = false;
    }

    /// <summary>
    /// 继续指定任务
    /// </summary>
    public void Resume(string name)
    {
        if (_tasks.TryGetValue(name, out var task))
            task.IsEnabled = true;
    }

    /// <summary>
    /// 移除指定任务
    /// </summary>
    public bool Remove(string name) => _tasks.TryRemove(name, out _);

    /// <summary>
    /// 检查任务是否暂停
    /// </summary>
    public bool IsPausedTask(string name) =>
        _tasks.TryGetValue(name, out var task) && !task.IsEnabled;

    #endregion

    #region 核心循环

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 暂停检查
                await _pauseSemaphore.WaitAsync(ct);
                _pauseSemaphore.Release();

                // 检查连接状态
                if (!_master.IsConnected)
                {
                    await Task.Delay(_baseIntervalMs, ct);
                    continue;
                }

                var now = DateTime.UtcNow;

                // 串行执行到期的任务
                foreach (var task in _tasks.Values)
                {
                    if (ct.IsCancellationRequested) break;
                    if (!task.IsEnabled) continue;

                    var elapsed = (now - task.LastExecuted).TotalMilliseconds;
                    if (elapsed < task.IntervalMs) continue;

                    try
                    {
                        await task.Action(ct);
                        task.LastExecuted = DateTime.UtcNow;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        foreach (var handler in _errorHandlers)
                        {
                            try { await handler(task.Name, ex); } catch { }
                        }
                    }
                }

                // 基础间隔
                await Task.Delay(_baseIntervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _tasks.Clear();
        _errorHandlers.Clear();
        _pauseSemaphore.Dispose();

        GC.SuppressFinalize(this);
    }
}
