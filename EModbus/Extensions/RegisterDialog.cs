using EModbus.Services;
using EModbus.ViewModels;
using EModbus.Views;
using ShadUI;

namespace EModbus.Extensions;

public static class RegisterDialog
{
    public static ServiceProvider RegisterDialogs(this ServiceProvider service)
    {
        var dialogService = service.GetService<DialogManager>();

        dialogService.Register<AboutContent, AboutViewModel>();

        return service;
    }
}