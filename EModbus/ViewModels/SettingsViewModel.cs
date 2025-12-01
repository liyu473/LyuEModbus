using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EModbus.Extensions;
using EModbus.Model;
using EModbus.Models;
using ShadUI;

namespace EModbus.ViewModels;

[Page("settings")]
public partial class SettingsViewModel : ViewModelBase, INavigable
{
    private readonly ToastManager _toastManager;

    public SettingsViewModel(ToastManager toastManager)
    {
        _toastManager = toastManager;
        LoadSettings();
    }

    #region 主站配置

    [ObservableProperty]
    private string _masterIpAddress = "127.0.0.1";

    [ObservableProperty]
    private int _masterPort = 502;

    [ObservableProperty]
    private byte _masterSlaveId = 1;

    [ObservableProperty]
    private int _masterReadTimeout = 3000;

    [ObservableProperty]
    private int _masterWriteTimeout = 3000;

    #endregion

    #region 从站配置

    [ObservableProperty]
    private string _slaveIpAddress = "0.0.0.0";

    [ObservableProperty]
    private int _slavePort = 502;

    [ObservableProperty]
    private byte _slaveSlaveId = 1;

    #endregion

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadSettings()
    {
        var settings = ConfigurationExtension.ModbusSettings;

        MasterIpAddress = settings.Master.IpAddress;
        MasterPort = settings.Master.Port;
        MasterSlaveId = settings.Master.SlaveId;
        MasterReadTimeout = settings.Master.ReadTimeout;
        MasterWriteTimeout = settings.Master.WriteTimeout;

        SlaveIpAddress = settings.Slave.IpAddress;
        SlavePort = settings.Slave.Port;
        SlaveSlaveId = settings.Slave.SlaveId;
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        var settings = new ModbusSettings
        {
            Master = new MasterSettings
            {
                IpAddress = MasterIpAddress,
                Port = MasterPort,
                SlaveId = MasterSlaveId,
                ReadTimeout = MasterReadTimeout,
                WriteTimeout = MasterWriteTimeout
            },
            Slave = new SlaveSettings
            {
                IpAddress = SlaveIpAddress,
                Port = SlavePort,
                SlaveId = SlaveSlaveId
            }
        };

        ConfigurationExtension.SaveSettings(settings);
        _toastManager.ShowToast("配置已保存", type: Notification.Success);
    }

    /// <summary>
    /// 重置配置
    /// </summary>
    [RelayCommand]
    private void ResetSettings()
    {
        ConfigurationExtension.ReloadConfiguration();
        LoadSettings();
        _toastManager.ShowToast("配置已重置", type: Notification.Info);
    }
}