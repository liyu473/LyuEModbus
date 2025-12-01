using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EModbus.Extensions;
using EModbus.Model;
using Extensions;
using LyuEModbus;
using ShadUI;

namespace EModbus.ViewModels;

[Page("home")]
public partial class HomeViewModel : ViewModelBase, INavigable
{
    private readonly DialogManager _dialogManager;
    private readonly ToastManager _toastManager;
    private readonly ModbusSettings _settings;

    private ModbusTcpSlave? _tcpSlave;
    private ModbusTcpMaster? _tcpMaster;

    public HomeViewModel(DialogManager dialogManager, ToastManager toastManager, ModbusSettings settings)
    {
        _dialogManager = dialogManager;
        _toastManager = toastManager;
        _settings = settings;

        SlaveSettings = settings.Slave;
        MasterSettings = settings.Master;
    }

    #region 从站

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
            _tcpSlave = new ModbusTcpSlave(SlaveSettings.IpAddress, SlaveSettings.Port, SlaveSettings.SlaveId);
            _tcpSlave.OnLog += msg => SlaveLog = SlaveLog.Append(msg);
            _tcpSlave.OnStatusChanged += running =>
            {
                IsSlaveRunning = running;
                SlaveStatus = running ? $"运行中 - {SlaveSettings.IpAddress}:{SlaveSettings.Port}" : "已停止";
            };

            await _tcpSlave.StartAsync();
            _toastManager.ShowToast("从站启动成功", type: Notification.Success);
        }
        catch (Exception ex)
        {
            SlaveLog = SlaveLog.Append($"启动失败: {ex.Message}");
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
            SlaveLog = SlaveLog.Append($"停止失败: {ex.Message}");
            _toastManager.ShowToast($"停止失败: {ex.Message}", type: Notification.Error);
        }
    }

    [RelayCommand]
    private void ClearSlaveLog()
    {
        SlaveLog = string.Empty;
    }

    #endregion

    #region 主站

    [ObservableProperty]
    private MasterSettings masterSettings;

    [ObservableProperty]
    private bool isMasterConnected;

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
            _tcpMaster = new ModbusTcpMaster(
                MasterSettings.IpAddress,
                MasterSettings.Port,
                MasterSettings.SlaveId,
                MasterSettings.ReadTimeout,
                MasterSettings.WriteTimeout);

            _tcpMaster.OnLog += msg => MasterLog = MasterLog.Append(msg);
            _tcpMaster.OnConnectionChanged += connected =>
            {
                IsMasterConnected = connected;
                MasterStatus = connected ? $"已连接 - {MasterSettings.IpAddress}:{MasterSettings.Port}" : "未连接";
            };

            await _tcpMaster.ConnectAsync();
            _toastManager.ShowToast("主站连接成功", type: Notification.Success);
        }
        catch (Exception ex)
        {
            MasterLog = MasterLog.Append($"连接失败: {ex.Message}");
            _toastManager.ShowToast($"连接失败: {ex.Message}", type: Notification.Error);
        }
    }

    [RelayCommand]
    private void DisconnectMaster()
    {
        if (!IsMasterConnected)
        {
            _toastManager.ShowToast("主站未连接", type: Notification.Warning);
            return;
        }

        try
        {
            _tcpMaster?.Disconnect();
            _tcpMaster = null;
            _toastManager.ShowToast("主站已断开", type: Notification.Info);
        }
        catch (Exception ex)
        {
            MasterLog = MasterLog.Append($"断开失败: {ex.Message}");
            _toastManager.ShowToast($"断开失败: {ex.Message}", type: Notification.Error);
        }
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
            MasterLog = MasterLog.Append($"读取失败: {ex.Message}");
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
            MasterLog = MasterLog.Append($"写入失败: {ex.Message}");
            _toastManager.ShowToast($"写入失败: {ex.Message}", type: Notification.Error);
        }
    }

    [RelayCommand]
    private void ClearMasterLog()
    {
        MasterLog = string.Empty;
    }

    #endregion
}
