using System.ComponentModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EModbus.Extensions;
using EModbus.Model;
using Extensions;
using ShadUI;

namespace EModbus.ViewModels;

[Page("settings")]
public partial class SettingsViewModel : ViewModelBase, INavigable
{
    private readonly ModbusSettings _modbusSettings;
    private readonly ToastManager _toastManager;

    public SettingsViewModel(ToastManager toastManager, ModbusSettings modbusSettings)
    {
        _toastManager = toastManager;
        _modbusSettings = modbusSettings;

        Settings = modbusSettings;

        Settings.Slave.PropertyChanged += Slave_PropertyChanged;
        Settings.Master.PropertyChanged += Master_PropertyChanged;
    }

    private void Master_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveSettings();
    }

    private void Slave_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveSettings();
    }

    [ObservableProperty]
    public partial ModbusSettings Settings { get; set; }

    [RelayCommand]
    private void ResetSettings()
    {
        Settings.Slave.UpdatePropertiesHighQualityFrom(new());
        Settings.Master.UpdatePropertiesHighQualityFrom(new());
    }

    private readonly JsonSerializerOptions _jsonOption = new()
    {
        PropertyNamingPolicy = null, // 大驼峰
        WriteIndented = true,
    };

    [RelayCommand]
    private void SaveSettings()
    {
        _modbusSettings.UpdatePropertiesHighQualityFrom(Settings);
        var json = new { Modbus = Settings }.ToJson(_jsonOption);

        File.WriteAllText("appsettings.json", json);

        _toastManager.ShowToast("成功保存应用", type: Notification.Success);
    }
}
