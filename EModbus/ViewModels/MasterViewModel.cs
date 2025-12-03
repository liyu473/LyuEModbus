using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EModbus.Extensions;
using EModbus.Model;
using Extensions;
using LyuEModbus.Abstractions;
using LyuEModbus.Extensions;
using LyuEModbus.Factory;
using LyuEModbus.Models;
using ShadUI;

namespace EModbus.ViewModels;

public partial class MasterViewModel(
    ToastManager toastManager,
    ModbusSettings settings,
    IEModbusFactory factory
) : ViewModelBase
{
    private IModbusMasterClient? _tcpMaster;

    [ObservableProperty]
    public partial MasterSettings MasterSettings { get; set; } = settings.Master;

    [ObservableProperty]
    public partial string MasterStatus { get; set; } = "未连接";

    [ObservableProperty]
    public partial string MasterLog { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ushort ReadAddress { get; set; } = 0;

    [ObservableProperty]
    public partial ushort ReadCount { get; set; } = 10;

    [ObservableProperty]
    public partial string ReadResult { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ushort WriteAddress { get; set; } = 0;

    [ObservableProperty]
    public partial ushort WriteValue { get; set; } = 0;

    [ObservableProperty]
    public partial ushort CoilReadAddress { get; set; } = 0;

    [ObservableProperty]
    public partial ushort CoilReadCount { get; set; } = 10;

    [ObservableProperty]
    public partial string CoilReadResult { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ushort CoilWriteAddress { get; set; } = 0;

    [ObservableProperty]
    public partial bool CoilWriteValue { get; set; } = false;

    [ObservableProperty]
    public partial bool AutoReconnect { get; set; } = true;

    [ObservableProperty]
    public partial int ReconnectAttempt { get; set; }

    [ObservableProperty]
    public partial int MaxReconnectAttempts { get; set; } = 2;

    [ObservableProperty]
    public partial bool IsMasterConnected { get; set; }

    [ObservableProperty]
    public partial bool IsReconnecting { get; set; }

    [ObservableProperty]
    public partial string PollyResult { get; set; }

    [RelayCommand]
    private async Task ConnectMasterAsync()
    {
        if (IsMasterConnected)
        {
            toastManager.ShowToast("主站已连接", type: Notification.Warning);
            return;
        }

        try
        {
            factory.RemoveMaster("main");

            _tcpMaster = factory
                .CreateTcpMaster("main")
                .WithEndpoint(MasterSettings.IpAddress, MasterSettings.Port)
                .WithSlaveId(MasterSettings.SlaveId)
                .WithTimeout(MasterSettings.ReadTimeout, MasterSettings.WriteTimeout)
                .OnStateChanged(state =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        IsMasterConnected = state == ModbusConnectionState.Connected;
                        IsReconnecting = state == ModbusConnectionState.Reconnecting;

                        MasterStatus = state switch
                        {
                            ModbusConnectionState.Connected =>
                                $"已连接 - {MasterSettings.IpAddress}:{MasterSettings.Port}",
                            ModbusConnectionState.Reconnecting =>
                                $"重连中... ({ReconnectAttempt}/{MaxReconnectAttempts})",
                            ModbusConnectionState.Connecting => "连接中...",
                            _ => "未连接",
                        };

                        if (state == ModbusConnectionState.Connected)
                        {
                            ReconnectAttempt = 0;
                            toastManager.ShowToast("连接成功", type: Notification.Success);
                        }
                    });
                    return Task.CompletedTask;
                })
                .OnReconnecting(
                    (attempt, max) =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            ReconnectAttempt = attempt;
                            MaxReconnectAttempts = max;
                            var maxDisplay = max == 0 ? "∞" : max.ToString();
                            MasterStatus = $"重连中... ({attempt}/{maxDisplay})";
                        });
                        return Task.CompletedTask;
                    },
                    intervalMs: 3000,
                    maxAttempts: MaxReconnectAttempts
                )
                .OnReconnectFailed(() =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        toastManager.ShowToast(
                            $"重连失败，已达到最大次数 {MaxReconnectAttempts}",
                            type: Notification.Error
                        );
                        ReconnectAttempt = 0;
                        IsReconnecting = false;
                    });
                    return Task.CompletedTask;
                })
                .WithHeartbeat(3000)
                .WithHoldingRegisterPolling(
                    0,
                    1,
                    200,
                    (dic) =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            PollyResult = $"{dic[0]}";
                        });
                        return Task.CompletedTask;
                    }
                );

            await _tcpMaster.ConnectAsync();
        }
        catch (Exception ex)
        {
            MasterLog = MasterLog.Append($"连接失败: {ex.Message}{Environment.NewLine}");
            toastManager.ShowToast($"连接失败: {ex.Message}", type: Notification.Error);
        }
    }

    [RelayCommand]
    private void DisconnectMaster()
    {
        if (!IsMasterConnected && !IsReconnecting)
        {
            toastManager.ShowToast("主站未连接", type: Notification.Warning);
            return;
        }

        try
        {
            _tcpMaster?.Disconnect();
            factory.RemoveMaster("main");
            _tcpMaster = null;
            ReconnectAttempt = 0;
            MasterStatus = "未连接";
            IsMasterConnected = false;
            IsReconnecting = false;
            toastManager.ShowToast("主站已断开", type: Notification.Info);
        }
        catch (Exception ex)
        {
            MasterLog = MasterLog.Append($"断开失败: {ex.Message}{Environment.NewLine}");
            toastManager.ShowToast($"断开失败: {ex.Message}", type: Notification.Error);
        }
    }

    [RelayCommand]
    private void StopReconnect()
    {
        if (!IsReconnecting)
        {
            toastManager.ShowToast("未在重连中", type: Notification.Warning);
            return;
        }

        _tcpMaster?.StopReconnect();
        ReconnectAttempt = 0;
        MasterStatus = "未连接";
        IsReconnecting = false;
        toastManager.ShowToast("已停止重连", type: Notification.Info);
    }

    [RelayCommand]
    private async Task ReadHoldingRegistersAsync()
    {
        if (!IsMasterConnected || _tcpMaster == null)
        {
            toastManager.ShowToast("请先连接主站", type: Notification.Warning);
            return;
        }

        var dict = await _tcpMaster.ReadHoldingRegistersToDictAsync(
            ReadAddress,
            ReadCount,
            ex =>
            {
                toastManager.ShowToast($"读取失败: {ex.Message}", type: Notification.Error);
                return Task.CompletedTask;
            }
        );

        if (dict != null)
        {
            ReadResult = string.Join(", ", dict.Values);
            toastManager.ShowToast("读取成功", type: Notification.Success);
        }
    }

    [RelayCommand]
    private async Task WriteSingleRegisterAsync()
    {
        if (!IsMasterConnected || _tcpMaster == null)
        {
            toastManager.ShowToast("请先连接主站", type: Notification.Warning);
            return;
        }

        var success = await _tcpMaster.WriteRegisterAsync(
            WriteAddress,
            WriteValue,
            ex =>
            {
                toastManager.ShowToast($"写入失败: {ex.Message}", type: Notification.Error);
                return Task.CompletedTask;
            }
        );

        if (success)
            toastManager.ShowToast("写入成功", type: Notification.Success);
    }

    [RelayCommand]
    private async Task ReadCoilsAsync()
    {
        if (!IsMasterConnected || _tcpMaster == null)
        {
            toastManager.ShowToast("请先连接主站", type: Notification.Warning);
            return;
        }

        var dict = await _tcpMaster.ReadCoilsToDictAsync(
            CoilReadAddress,
            CoilReadCount,
            ex =>
            {
                toastManager.ShowToast($"读取线圈失败: {ex.Message}", type: Notification.Error);
                return Task.CompletedTask;
            }
        );

        if (dict != null)
        {
            CoilReadResult = string.Join(", ", dict.Values);
            toastManager.ShowToast("读取线圈成功", type: Notification.Success);
        }
    }

    [RelayCommand]
    private async Task WriteSingleCoilAsync()
    {
        if (!IsMasterConnected || _tcpMaster == null)
        {
            toastManager.ShowToast("请先连接主站", type: Notification.Warning);
            return;
        }

        var success = await _tcpMaster.WriteCoilAsync(
            CoilWriteAddress,
            CoilWriteValue,
            ex =>
            {
                toastManager.ShowToast($"写入线圈失败: {ex.Message}", type: Notification.Error);
                return Task.CompletedTask;
            }
        );

        if (success)
            toastManager.ShowToast($"写入线圈成功: {CoilWriteValue}", type: Notification.Success);
    }

    [RelayCommand]
    private void ClearMasterLog() => MasterLog = string.Empty;
}
