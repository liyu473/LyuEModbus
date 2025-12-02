using LyuEModbus.Abstractions;
using LyuEModbus.Core;

namespace LyuEModbus.Extensions;

/// <summary>
/// Modbus 轮询扩展方法
/// </summary>
public static class ModbusPollingExtensions
{
    #region 链式调用 - 保持寄存器轮询

    /// <summary>
    /// 配置保持寄存器轮询（链式调用）
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">读取数量</param>
    /// <param name="intervalMs">轮询间隔（毫秒）</param>
    /// <param name="onData">数据回调（地址 -> 值）</param>
    /// <param name="onError">错误回调</param>
    /// <param name="autoStart">是否在连接成功后自动启动轮询</param>
    /// <returns>主站实例（支持链式调用）</returns>
    public static IModbusMasterClient WithHoldingRegisterPolling(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        int intervalMs,
        Func<Dictionary<ushort, ushort>, Task> onData,
        Func<Exception, Task>? onError = null,
        bool autoStart = true)
    {
        if (master is ModbusMasterBase masterBase)
        {
            masterBase.Poller?.Dispose();
            masterBase.Poller = new ModbusPoller(async ct =>
            {
                if (!master.IsConnected) return;

                var result = await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, count);
                var dict = new Dictionary<ushort, ushort>();
                for (int i = 0; i < result.Length; i++)
                    dict[(ushort)(startAddress + i)] = result[i];

                await onData(dict);
            }, intervalMs);

            if (onError != null)
                masterBase.Poller.OnError += onError;

            if (autoStart)
            {
                master.StateChanged += async state =>
                {
                    if (state == Models.ModbusConnectionState.Connected)
                        masterBase.Poller?.Start();
                    else if (state == Models.ModbusConnectionState.Disconnected)
                        masterBase.Poller?.Stop();
                };
            }
        }

        return master;
    }

    #endregion

    #region 链式调用 - 线圈轮询

    /// <summary>
    /// 配置线圈轮询（链式调用）
    /// </summary>
    public static IModbusMasterClient WithCoilPolling(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        int intervalMs,
        Func<Dictionary<ushort, bool>, Task> onData,
        Func<Exception, Task>? onError = null,
        bool autoStart = true)
    {
        if (master is ModbusMasterBase masterBase)
        {
            masterBase.Poller?.Dispose();
            masterBase.Poller = new ModbusPoller(async ct =>
            {
                if (!master.IsConnected) return;

                var result = await master.ReadCoilsAsync(master.SlaveId, startAddress, count);
                var dict = new Dictionary<ushort, bool>();
                for (int i = 0; i < result.Length; i++)
                    dict[(ushort)(startAddress + i)] = result[i];

                await onData(dict);
            }, intervalMs);

            if (onError != null)
                masterBase.Poller.OnError += onError;

            if (autoStart)
            {
                master.StateChanged += async state =>
                {
                    if (state == Models.ModbusConnectionState.Connected)
                        masterBase.Poller?.Start();
                    else if (state == Models.ModbusConnectionState.Disconnected)
                        masterBase.Poller?.Stop();
                };
            }
        }

        return master;
    }

    #endregion

    #region 链式调用 - 自定义轮询

    /// <summary>
    /// 配置自定义轮询（链式调用）
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="intervalMs">轮询间隔（毫秒）</param>
    /// <param name="pollAction">轮询动作</param>
    /// <param name="onError">错误回调</param>
    /// <param name="autoStart">是否在连接成功后自动启动轮询</param>
    public static IModbusMasterClient WithPolling(
        this IModbusMasterClient master,
        int intervalMs,
        Func<IModbusMasterClient, Task> pollAction,
        Func<Exception, Task>? onError = null,
        bool autoStart = true)
    {
        if (master is ModbusMasterBase masterBase)
        {
            masterBase.Poller?.Dispose();
            masterBase.Poller = new ModbusPoller(async ct =>
            {
                if (!master.IsConnected) return;
                await pollAction(master);
            }, intervalMs);

            if (onError != null)
                masterBase.Poller.OnError += onError;

            if (autoStart)
            {
                master.StateChanged += async state =>
                {
                    if (state == Models.ModbusConnectionState.Connected)
                        masterBase.Poller?.Start();
                    else if (state == Models.ModbusConnectionState.Disconnected)
                        masterBase.Poller?.Stop();
                };
            }
        }

        return master;
    }

    #endregion

    #region 轮询组

    /// <summary>
    /// 创建轮询组（集中管理多个轮询任务，默认连接成功后自动启动）
    /// </summary>
    /// <param name="master">主站实例</param>
    /// <param name="configure">配置轮询组</param>
    /// <param name="autoStart">是否在连接成功后自动启动（默认 true）</param>
    /// <returns>主站实例（支持链式调用）</returns>
    public static IModbusMasterClient WithPollerGroup(
        this IModbusMasterClient master,
        Action<ModbusPollerGroup> configure,
        bool autoStart = true)
    {
        if (master is ModbusMasterBase masterBase)
        {
            masterBase.PollerGroup?.Dispose();
            masterBase.PollerGroup = new ModbusPollerGroup(master);
            configure(masterBase.PollerGroup);

            // 默认自动启动
            if (autoStart)
            {
                master.StateChanged += async state =>
                {
                    if (state == Models.ModbusConnectionState.Connected)
                        masterBase.PollerGroup?.Start();
                    else if (state == Models.ModbusConnectionState.Disconnected)
                        masterBase.PollerGroup?.Stop();
                };
            }
        }

        return master;
    }

    /// <summary>
    /// 创建轮询组（独立实例，不绑定到 master）
    /// </summary>
    public static ModbusPollerGroup CreatePollerGroup(this IModbusMasterClient master)
    {
        return new ModbusPollerGroup(master);
    }

    #endregion

    #region 独立创建轮询器（不绑定到 master.Poller）

    /// <summary>
    /// 创建保持寄存器轮询器（独立实例）
    /// </summary>
    public static ModbusPoller CreateHoldingRegisterPoller(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        int intervalMs,
        Func<Dictionary<ushort, ushort>, Task> onData,
        Func<Exception, Task>? onError = null)
    {
        var poller = new ModbusPoller(async ct =>
        {
            if (!master.IsConnected) return;

            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, ushort>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];

            await onData(dict);
        }, intervalMs);

        if (onError != null)
            poller.OnError += onError;

        return poller;
    }

    /// <summary>
    /// 创建自定义轮询器（独立实例）
    /// </summary>
    public static ModbusPoller CreatePoller(
        this IModbusMasterClient master,
        int intervalMs,
        Func<IModbusMasterClient, Task> pollAction,
        Func<Exception, Task>? onError = null)
    {
        var poller = new ModbusPoller(async ct =>
        {
            if (!master.IsConnected) return;
            await pollAction(master);
        }, intervalMs);

        if (onError != null)
            poller.OnError += onError;

        return poller;
    }

    #endregion
}
