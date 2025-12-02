using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EModbus.Extensions;
using EModbus.Model;
using Extensions;
using LyuEModbus.Abstractions;
using LyuEModbus.Extensions;
using LyuEModbus.Factory;
using ShadUI;

namespace EModbus.ViewModels;

public partial class MasterViewModel : ViewModelBase
{
    private readonly ToastManager _toastManager;
    private readonly ModbusClientFactory _factory = ModbusClientFactory.Default;
    private IModbusMasterClient? _tcpMaster;

    public MasterViewModel(ToastManager toastManager, ModbusSettings settings)
    {
        _toastManager = toastManager;
        MasterSettings = settings.Master;
    }

    [ObservableProperty] public partial MasterSettings MasterSettings { get; set; }
    [ObservableProperty] public partial string MasterStatus { get; set; } = "未连接";
    [ObservableProperty] public partial string MasterLog { get; set; } = string.Empty;
    [ObservableProperty] public partial ushort ReadAddress { get; set; } = 0;
    [ObservableProperty] public partial ushort ReadCount { get; set; } = 10;
    [ObservableProperty] public partial string ReadResult { get; set; } = string.Empty;
    [ObservableProperty] public partial ushort WriteAddress { get; set; } = 0;
    [ObservableProperty] public partial ushort WriteValue { get; set; } = 0;
    [ObservableProperty] public partial ushort CoilReadAddress { get; set; } = 0;
    [ObservableProperty] public partial ushort CoilReadCount { get; set; } = 10;
    [ObservableProperty] public partial string CoilReadResult { get; set; } = string.Empty;
    [ObservableProperty] public partial ushort CoilWriteAddress { get; set; } = 0;
    [ObservableProperty] public partial bool CoilWriteValue { get; set; } = false;
    [ObservableProperty] public partial bool AutoReconnect { get; set; } = true;
    [ObservableProperty] public partial int ReconnectAttempt { get; set; }
    [ObservableProperty] public partial bool IsMasterConnected { get; set; }
    [ObservableProperty] public partial bool IsReconnecting { get; set; }

    [RelayCommand]
    private async Task ConnectMasterAsync()
    {
        if (IsMasterConnected)
        {
            _toastManager.ShowToast("主站已连接", type: Notification.Warning);
            return;
        }

        try
        {
            _factory.RemoveMaster("main");
            
            _tcpMaster = _factory.CreateTcpMaster("main", opt =>
            {
                opt.FromSettings(MasterSettings);
                opt.AutoReconnect = true;
                opt.ReconnectInterval = 3000;
                opt.MaxReconnectAttempts = 10;
                opt.EnableHeartbeat = true;
                opt.HeartbeatInterval = 3000;
            });
            
            _tcpMaster.StateChanged += state =>
            {
                IsMasterConnected = state == ModbusConnectionState.Connected;
                IsReconnecting = state == ModbusConnectionState.Reconnecting;
                
                MasterStatus = state switch
                {
                    ModbusConnectionState.Connected => $"已连接 - {MasterSettings.IpAddress}:{MasterSettings.Port}",
                    ModbusConnectionState.Reconnecting => $"重连中... ({ReconnectAttempt}/10)",
                    ModbusConnectionState.Connecting => "连接中...",
                    _ => "未连接"
                };
                
                if (state == ModbusConnectionState.Connected)
                {
                    ReconnectAttempt = 0;
                    _toastManager.ShowToast("连接成功", type: Notification.Success);
                }
                else if (state == ModbusConnectionState.Disconnected && !IsReconnecting)
                {
                    _toastManager.ShowToast("连接已断开", type: Notification.Warning);
                }
            };
            
            _tcpMaster.Reconnecting += attempt =>
            {
                ReconnectAttempt = attempt;
                MasterStatus = $"重连中... ({attempt}/10)";
            };

            await _tcpMaster.ConnectAsync();
        }
        catch (Exception ex)
        {
            MasterLog = MasterLog.Append($"连接失败: {ex.Message}{Environment.NewLine}");
            _toastManager.ShowToast($"连接失败: {ex.Message}", type: Notification.Error);
        }
    }

    [RelayCommand]
    private void DisconnectMaster()
    {
        if (!IsMasterConnected && !IsReconnecting)
        {
            _toastManager.ShowToast("主站未连接", type: Notification.Warning);
            return;
        }

        try
        {
            _tcpMaster?.Disconnect();
            _factory.RemoveMaster("main");
            _tcpMaster = null;
            ReconnectAttempt = 0;
            MasterStatus = "未连接";
            IsMasterConnected = false;
            IsReconnecting = false;
            _toastManager.ShowToast("主站已断开", type: Notification.Info);
        }
        catch (Exception ex)
        {
            MasterLog = MasterLog.Append($"断开失败: {ex.Message}{Environment.NewLine}");
            _toastManager.ShowToast($"断开失败: {ex.Message}", type: Notification.Error);
        }
    }

    [RelayCommand]
    private void StopReconnect()
    {
        if (!IsReconnecting)
        {
            _toastManager.ShowToast("未在重连中", type: Notification.Warning);
            return;
        }

        _tcpMaster?.StopReconnect();
        ReconnectAttempt = 0;
        MasterStatus = "未连接";
        IsReconnecting = false;
        _toastManager.ShowToast("已停止重连", type: Notification.Info);
    }

    [RelayCommand]
    private async Task ReadHoldingRegistersAsync()
    {
        if (!IsMasterConnected || _tcpMaster == null)
        {
            _toastManager.ShowToast("请先连接主站", type: Notification.Warning);
            return;
        }

        var dict = await _tcpMaster.ReadHoldingRegistersToDictAsync(ReadAddress, ReadCount,
            ex => { _toastManager.ShowToast($"读取失败: {ex.Message}", type: Notification.Error); return Task.CompletedTask; });
        
        if (dict != null)
        {
            ReadResult = string.Join(", ", dict.Values);
            _toastManager.ShowToast("读取成功", type: Notification.Success);
        }
    }

    [RelayCommand]
    private async Task WriteSingleRegisterAsync()
    {
        if (!IsMasterConnected || _tcpMaster == null)
        {
            _toastManager.ShowToast("请先连接主站", type: Notification.Warning);
            return;
        }

        var success = await _tcpMaster.WriteRegisterAsync(WriteAddress, WriteValue,
            ex => { _toastManager.ShowToast($"写入失败: {ex.Message}", type: Notification.Error); return Task.CompletedTask; });
        
        if (success)
            _toastManager.ShowToast("写入成功", type: Notification.Success);
    }

    [RelayCommand]
    private async Task ReadCoilsAsync()
    {
        if (!IsMasterConnected || _tcpMaster == null)
        {
            _toastManager.ShowToast("请先连接主站", type: Notification.Warning);
            return;
        }

        var dict = await _tcpMaster.ReadCoilsToDictAsync(CoilReadAddress, CoilReadCount,
            ex => { _toastManager.ShowToast($"读取线圈失败: {ex.Message}", type: Notification.Error); return Task.CompletedTask; });
        
        if (dict != null)
        {
            CoilReadResult = string.Join(", ", dict.Values);
            _toastManager.ShowToast("读取线圈成功", type: Notification.Success);
        }
    }

    [RelayCommand]
    private async Task WriteSingleCoilAsync()
    {
        if (!IsMasterConnected || _tcpMaster == null)
        {
            _toastManager.ShowToast("请先连接主站", type: Notification.Warning);
            return;
        }

        var success = await _tcpMaster.WriteCoilAsync(CoilWriteAddress, CoilWriteValue,
            ex => { _toastManager.ShowToast($"写入线圈失败: {ex.Message}", type: Notification.Error); return Task.CompletedTask; });
        
        if (success)
            _toastManager.ShowToast($"写入线圈成功: {CoilWriteValue}", type: Notification.Success);
    }

    [RelayCommand]
    private void ClearMasterLog() => MasterLog = string.Empty;
}
