using EModbus.Extensions;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using EModbus.Model;
using ShadUI;

namespace EModbus.ViewModels;

[Page("home")]
public partial class HomeViewModel(DialogManager dialogManager, ToastManager toastManager)
    : ViewModelBase,
        INavigable
{
    [RelayCommand]
    private void ShowDialog()
    {
        dialogManager.ShowMessageBox("Title", "Dialog message here ");
    }

    [RelayCommand]
    private void ShowDestructiveDialog()
    {
        dialogManager.ShowMessageBox(
            "Title",
            "Dialog message here ",
            primaryButtonStyle: DialogButtonStyle.Destructive
        );
    }

    [RelayCommand]
    private void ShowDismiss()
    {
        //默认点击背景消失
        dialogManager.ShowActionDialog("Title", "Dialog message here ");
    }

    [RelayCommand]
    private void ShowAction()
    {
        dialogManager.ShowActionDialog(
            "Title",
            "Dialog message here ",
            () => toastManager.ShowToast("你点击了确认按钮", delay: 5)
        );
    }

    [RelayCommand]
    private void ShowCustom()
    {
        var vm = App.Service.GetService<AboutViewModel>();
        dialogManager.ShowCustomDialog(vm);
    }
}
