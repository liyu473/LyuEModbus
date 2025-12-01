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
    private bool isMasterConnected;

    [ObservableProperty]
    private bool isReconnecting;

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

    [ObservableProperty]
    private bool autoReconnect = true;

    [ObservableProperty]
    private int reconnectAttempt;

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
                .WithAddress(MasterSettings.IpAddress, MasterSettings.Port)
                .WithSlaveId(MasterSettings.SlaveId)
                .WithTimeout(MasterSettings.ReadTimeout, MasterSettings.WriteTimeout)
                .WithAutoReconnect(AutoReconnect)
                .WithReconnectInterval(3000)
                .WithMaxReconnectAttempts(10)
                .WithLog(msg => MasterLog = MasterLog.Append(msg + Environment.NewLine))
                .WithConnectionChanged(connected =>
                {
                    IsMasterConnected = connected;
                    IsReconnecting = _tcpMaster?.IsReconnecting ?? false;
                    
                    if (connected)
                    {
                        MasterStatus = $"已连接 - {MasterSettings.IpAddress}:{MasterSettings.Port}";
                        ReconnectAttempt = 0;
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
            IsReconnecting = false;
            ReconnectAttempt = 0;
            MasterStatus = "未连接";
            IsMasterConnected = false;
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
        IsReconnecting = false;
        ReconnectAttempt = 0;
        MasterStatus = "未连接";
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

        try
        {
            var result = await _tcpMaster.ReadHoldingRegistersAsync(ReadAddress, ReadCount);
            ReadResult = string.Join(", ", result);
            _toastManager.ShowToast("读取成功", type: Notification.Success);
        }
        catch (Exception ex)
        {
            MasterLog = MasterLog.Append($"读取失败: {ex.Message}{Environment.NewLine}");
            _toastManager.ShowToast($"读取失败: {ex.Message}", type: Notification.Error);
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

        try
        {
            await _tcpMaster.WriteSingleRegisterAsync(WriteAddress, WriteValue);
            _toastManager.ShowToast("写入成功", type: Notification.Success);
        }
        catch (Exception ex)
        {
            MasterLog = MasterLog.Append($"写入失败: {ex.Message}{Environment.NewLine}");
            _toastManager.ShowToast($"写入失败: {ex.Message}", type: Notification.Error);
        }
    }

    [RelayCommand]
    private void ClearMasterLog()
    {
        MasterLog = string.Empty;
    }
}
