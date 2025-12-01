using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EModbus.Extensions;
using EModbus.Model;
using Extensions;
using LyuEModbus.Extensions;
using LyuEModbus.Services;
using ShadUI;

namespace EModbus.ViewModels;

public partial class MasterViewModel : ViewModelBase
{
    private readonly ToastManager _toastManager;
    private ModbusTcpMaster? _tcpMaster;

    public MasterViewModel(ToastManager toastManager, ModbusSettings settings)
    {
        _toastManager = toastManager;
        MasterSettings = settings.Master;
    }

    [ObservableProperty]
    private MasterSettings masterSettings;

    [ObservableProperty]
    private string masterStatus = "未连接";

    [ObservableProperty]
    private string masterLog = string.Empty;

    [ObservableProperty]
    private ushort readAddress = 0;

    [ObservableProperty]
    private ushort readCount = 10;

    [ObservableProperty]
    private string readResult = string.Empty;

    [ObservableProperty]
    private ushort writeAddress = 0;

    [ObservableProperty]
    private ushort writeValue = 0;

    // 线圈相关
    [ObservableProperty]
    private ushort coilReadAddress = 0;

    [ObservableProperty]
    private ushort coilReadCount = 10;

    [ObservableProperty]
    private string coilReadResult = string.Empty;

    [ObservableProperty]
    private ushort coilWriteAddress = 0;

    [ObservableProperty]
    private bool coilWriteValue = false;

    [ObservableProperty]
    private bool autoReconnect = true;

    [ObservableProperty]
    private int reconnectAttempt;

    /// <summary>
    /// 是否已连接（由 ModbusTcpMaster.OnConnectionChanged 事件更新）
    /// </summary>
    [ObservableProperty]
    private bool isMasterConnected;

    /// <summary>
    /// 是否正在重连（由 ModbusTcpMaster.OnReconnecting 事件更新）
    /// </summary>
    [ObservableProperty]
    private bool isReconnecting;

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
            _tcpMaster = ModbusTcpMaster.Create()
                .WithSettings(MasterSettings)
                .WithAutoReconnect(3000, 10)
                .WithLog(msg => MasterLog = MasterLog.Append(msg + Environment.NewLine))
                .WithConnectionChanged(connected =>
                {
                    IsMasterConnected = connected;                    
                    if (connected)
                    {
                        MasterStatus = $"已连接 - {MasterSettings.IpAddress}:{MasterSettings.Port}";
                        ReconnectAttempt = 0;
                        IsReconnecting = false;
                        _toastManager.ShowToast("连接成功", type: Notification.Success);
                    }
                    else
                    {
                        MasterStatus = IsReconnecting ? "重连中..." : "未连接";
                        if (!IsReconnecting)
                        {
                            _toastManager.ShowToast("连接已断开", type: Notification.Warning);
                        }
                    }
                })
                .WithReconnecting(attempt =>
                {
                    ReconnectAttempt = attempt;
                    IsReconnecting = true;
                    MasterStatus = $"重连中... ({attempt}/10)";
                });

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
        {
            _toastManager.ShowToast("写入成功", type: Notification.Success);
        }
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
        {
            _toastManager.ShowToast($"写入线圈成功: {CoilWriteValue}", type: Notification.Success);
        }
    }

    [RelayCommand]
    private void ClearMasterLog()
    {
        MasterLog = string.Empty;
    }
}
