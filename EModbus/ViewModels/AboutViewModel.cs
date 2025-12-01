using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace EModbus.ViewModels;

public partial class AboutViewModel(DialogManager dialogManager) : ViewModelBase
{
    public string AppVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
    public string AppName { get; } =
        Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;

    [RelayCommand]
    private void Close()
    {
        // 关闭对话框并触发Dialog的WithSuccessCallback
        // dialogManager.Close(this, new CloseDialogOptions { Success = true });
        dialogManager.Close(this);
    }
}
