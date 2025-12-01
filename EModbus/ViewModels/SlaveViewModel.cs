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

public partial class SlaveViewModel : ViewModelBase
{
    private readonly ToastManager _toastManager;
    private ModbusTcpSlave? _tcpSlave;

    public SlaveViewModel(ToastManager toastManager, ModbusSettings settings)
    {
        _toastManager = toastManager;
        SlaveSettings = settings.Slave;
    }

    [ObservableProperty]
    private SlaveSettings slaveSettings;

    [ObservableProperty]
    private bool isSlaveRunning;

    [ObservableProperty]
    private string slaveStatus = "已停止";

    [ObservableProperty]
    private string slaveLog = string.Empty;

    [RelayCommand]
    private async Task StartSlaveAsync()
    {
        if (IsSlaveRunning)
        {
            _toastManager.ShowToast("从站已在运行中", type: Notification.Warning);
            return;
        }

        try
        {
            _tcpSlave = ModbusTcpSlave.Create()
                .WithAddress(SlaveSettings.IpAddress, SlaveSettings.Port)
                .WithSlaveId(SlaveSettings.SlaveId)
                .WithLog(msg => SlaveLog = SlaveLog.Append(msg + Environment.NewLine))
                .WithStatusChanged(running =>
                {
                    IsSlaveRunning = running;
                    SlaveStatus = running ? $"运行中 - {SlaveSettings.IpAddress}:{SlaveSettings.Port}" : "已停止";
                });

            await _tcpSlave.StartAsync();
            _toastManager.ShowToast("从站启动成功", type: Notification.Success);
        }
        catch (Exception ex)
        {
            SlaveLog = SlaveLog.Append($"启动失败: {ex.Message}{Environment.NewLine}");
            _toastManager.ShowToast($"启动失败: {ex.Message}", type: Notification.Error);
        }
    }

    [RelayCommand]
    private void StopSlave()
    {
        if (!IsSlaveRunning)
        {
            _toastManager.ShowToast("从站未运行", type: Notification.Warning);
            return;
        }

        try
        {
            _tcpSlave?.Stop();
            _tcpSlave = null;
            _toastManager.ShowToast("从站已停止", type: Notification.Info);
        }
        catch (Exception ex)
        {
            SlaveLog = SlaveLog.Append($"停止失败: {ex.Message}{Environment.NewLine}");
            _toastManager.ShowToast($"停止失败: {ex.Message}", type: Notification.Error);
        }
    }

    [RelayCommand]
    private void ClearSlaveLog()
    {
        SlaveLog = string.Empty;
    }
}
