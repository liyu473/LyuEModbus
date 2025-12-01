using System;
using System.Collections.ObjectModel;
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
        
        // 延迟初始化寄存器列表，避免DataGrid渲染问题
        _ = InitializeRegistersAsync();
    }

    private async Task InitializeRegistersAsync()
    {
        await Task.Delay(100); // 等待UI准备好
        InitializeRegisters();
    }

    [ObservableProperty]
    private SlaveSettings slaveSettings;

    [ObservableProperty]
    private bool isSlaveRunning;

    [ObservableProperty]
    private string slaveStatus = "已停止";

    [ObservableProperty]
    private string slaveLog = string.Empty;



    /// <summary>
    /// 保持寄存器列表
    /// </summary>
    public ObservableCollection<RegisterItem> HoldingRegisters { get; } = new();

    /// <summary>
    /// 线圈列表
    /// </summary>
    public ObservableCollection<CoilItem> Coils { get; } = new();

    /// <summary>
    /// 初始化寄存器数量
    /// </summary>
    [ObservableProperty]
    private int registerCount = 20;

    private void InitializeRegisters()
    {
        HoldingRegisters.Clear();
        Coils.Clear();

        for (ushort i = 0; i < RegisterCount; i++)
        {
            var reg = new RegisterItem(i, (ushort)(i * 10), $"寄存器{i}");
            reg.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(RegisterItem.Value) && IsSlaveRunning && _tcpSlave != null)
                {
                    var item = (RegisterItem)s!;
                    _tcpSlave.SetHoldingRegister(item.Address, item.Value);
                }
            };
            HoldingRegisters.Add(reg);

            var coil = new CoilItem(i, i % 2 == 0, $"线圈{i}");
            coil.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CoilItem.Value) && IsSlaveRunning && _tcpSlave != null)
                {
                    var item = (CoilItem)s!;
                    _tcpSlave.SetCoil(item.Address, item.Value);
                }
            };
            Coils.Add(coil);
        }
    }

    [RelayCommand]
    private void RefreshRegisters()
    {
        InitializeRegisters();
        _toastManager.ShowToast($"已重置 {RegisterCount} 个寄存器和线圈", type: Notification.Info);
    }

    [RelayCommand]
    private void SyncFromSlave()
    {
        if (!IsSlaveRunning || _tcpSlave == null)
        {
            _toastManager.ShowToast("从站未运行", type: Notification.Warning);
            return;
        }

        // 从从站读取当前值并更新到列表
        var regValues = _tcpSlave.ReadHoldingRegisters(0, (ushort)HoldingRegisters.Count);
        if (regValues != null)
        {
            for (int i = 0; i < Math.Min(regValues.Length, HoldingRegisters.Count); i++)
            {
                HoldingRegisters[i].Value = regValues[i];
            }
        }

        _toastManager.ShowToast("已同步从站数据", type: Notification.Success);
    }

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
                .WithInitHoldingRegisters((ushort)HoldingRegisters.Count)
                .WithInitCoils((ushort)Coils.Count)
                .WithLog(msg => SlaveLog = SlaveLog.Append(msg + Environment.NewLine))
                .WithStatusChanged(running =>
                {
                    IsSlaveRunning = running;
                    SlaveStatus = running ? $"运行中 - {SlaveSettings.IpAddress}:{SlaveSettings.Port}" : "已停止";
                })
                .WithHoldingRegisterWritten((address, oldValue, newValue) =>
                {
                    // 更新UI列表
                    if (address < HoldingRegisters.Count)
                    {
                        HoldingRegisters[address].Value = newValue;
                    }
                    _toastManager.ShowToast($"寄存器[{address}]: {oldValue} → {newValue}", type: Notification.Info);
                })
                .WithCoilWritten((address, value) =>
                {
                    // 更新UI列表
                    if (address < Coils.Count)
                    {
                        Coils[address].Value = value;
                    }
                    _toastManager.ShowToast($"线圈[{address}]: {value}", type: Notification.Info);
                });

            await _tcpSlave.StartAsync();

            // 启动后将列表中的值写入从站
            for (int i = 0; i < HoldingRegisters.Count; i++)
            {
                _tcpSlave.SetHoldingRegister(HoldingRegisters[i].Address, HoldingRegisters[i].Value);
            }
            for (int i = 0; i < Coils.Count; i++)
            {
                _tcpSlave.SetCoil(Coils[i].Address, Coils[i].Value);
            }

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
