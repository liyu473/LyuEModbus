namespace LyuEModbus.Core;

/// <summary>
/// Modbus 轮询控制器
/// </summary>
public class ModbusPoller : IDisposable
{
    private readonly Func<CancellationToken, Task> _pollAction;
    private readonly int _intervalMs;
    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _pauseSemaphore = new(1, 1); // 用于暂停/继续控制
    private Task? _pollTask;
    private bool _disposed;

    /// <summary>
    /// 是否正在运行（包括暂停状态）
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 是否已暂停
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// 轮询出错事件
    /// </summary>
    public event Func<Exception, Task>? OnError;

    /// <summary>
    /// 最小轮询间隔（毫秒）
    /// </summary>
    public const int MinIntervalMs = 10;

    internal ModbusPoller(Func<CancellationToken, Task> pollAction, int intervalMs)
    {
        _pollAction = pollAction ?? throw new ArgumentNullException(nameof(pollAction));
        _intervalMs = Math.Max(MinIntervalMs, intervalMs); // 强制最小间隔，防止 CPU 占满
    }

    /// <summary>
    /// 启动轮询
    /// </summary>
    public void Start()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;
        IsPaused = false;
        _pollTask = RunPollLoopAsync(_cts.Token);
    }

    /// <summary>
    /// 停止轮询
    /// </summary>
    public void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        
        // 如果处于暂停状态，释放信号量让循环退出
        if (IsPaused)
        {
            try { _pauseSemaphore.Release(); } catch { }
        }

        IsRunning = false;
        IsPaused = false;
        _cts = null;
    }

    /// <summary>
    /// 暂停轮询
    /// </summary>
    public void Pause()
    {
        if (!IsRunning || IsPaused) return;

        // 获取信号量，阻止轮询继续
        _pauseSemaphore.Wait();
        IsPaused = true;
    }

    /// <summary>
    /// 继续轮询
    /// </summary>
    public void Resume()
    {
        if (!IsRunning || !IsPaused) return;

        IsPaused = false;
        _pauseSemaphore.Release();
    }

    private async Task RunPollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 等待信号量（暂停时会阻塞在这里）
                await _pauseSemaphore.WaitAsync(ct);
                _pauseSemaphore.Release(); // 立即释放，允许下一轮

                // 执行轮询
                await _pollAction(ct);

                // 等待间隔
                await Task.Delay(_intervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    await OnError.Invoke(ex);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _pauseSemaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
