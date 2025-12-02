using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EModbus.Extensions;
using EModbus.Model;
using Extensions;
using LyuEModbus.Abstractions;
using LyuEModbus.Factory;
using ShadUI;

namespace EModbus.ViewModels;

public partial class SlaveViewModel : ViewModelBase
{
    private readonly ToastManager _toastManager;
    private readonly ModbusClientFactory _factory = ModbusClientFactory.Default;
    private IModbusSlave? _tcpSlave;

    public SlaveViewModel(ToastManager toastManager, ModbusSettings settings)
    {
        _toastManager = toastManager;
        SlaveSettings = settings.Slave;
        
        _ = InitializeRegistersAsync();
    }

    private async Task InitializeRegistersAsync()
    {
        await Task.Delay(100); 
        InitializeRegisters();
    }

    [ObservableProperty]
    public partial SlaveSettings SlaveSettings { get; set; }

    [ObservableProperty]
    public partial bool IsSlaveRunning { get; set; }

    [ObservableProperty]
    public partial string SlaveStatus { get; set; } = "已停止";

    [ObservableProperty]
    public partial string SlaveLog { get; set; } = string.Empty;

    public ObservableCollection<RegisterItem> HoldingRegisters { get; } = [];

    public ObservableCollection<CoilItem> Coils { get; } = [];

    [ObservableProperty]
    public partial int RegisterCount { get; set; } = 20;

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
            // 移除旧的从站
            _factory.RemoveSlave("main");
            
            // 创建新的从站
            _tcpSlave = _factory.CreateTcpSlave("main", opt =>
            {
                opt.FromSettings(SlaveSettings);
                opt.InitHoldingRegisterCount = (ushort)HoldingRegisters.Count;
                opt.InitCoilCount = (ushort)Coils.Count;
            });
            
            // 订阅事件
            _tcpSlave.StateChanged += state =>
            {
                IsSlaveRunning = state == ModbusConnectionState.Connected;
                SlaveStatus = state == ModbusConnectionState.Connected 
                    ? $"运行中 - {SlaveSettings.IpAddress}:{SlaveSettings.Port}" 
                    : "已停止";
            };
            
            _tcpSlave.HoldingRegisterWritten += (address, oldValue, newValue) =>
            {
                if (address < HoldingRegisters.Count)
                {
                    HoldingRegisters[address].Value = newValue;
                }
                _toastManager.ShowToast($"寄存器[{address}]: {oldValue} → {newValue}", type: Notification.Info);
            };
            
            _tcpSlave.CoilWritten += (address, value) =>
            {
                if (address < Coils.Count)
                {
                    Coils[address].Value = value;
                }
                _toastManager.ShowToast($"线圈[{address}]: {value}", type: Notification.Info);
            };
            
            _tcpSlave.ClientConnected += client =>
            {
                _toastManager.ShowToast($"客户端已连接: {client}", type: Notification.Success);
            };
            
            _tcpSlave.ClientDisconnected += client =>
            {
                _toastManager.ShowToast($"客户端已断开: {client}", type: Notification.Warning);
            };

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
            _factory.RemoveSlave("main");
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
