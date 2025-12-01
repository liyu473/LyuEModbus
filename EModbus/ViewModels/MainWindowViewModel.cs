using System;
using System.Reflection;
using EModbus.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EModbus.Model;
using EModbus.Services;
using Extensions;
using Microsoft.Extensions.Logging;
using ShadUI;
using ZLogger;

namespace EModbus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string AppName { get; } = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;

    [ObservableProperty]
    public partial DialogManager DialogManager { get; set; }

    [ObservableProperty]
    public partial ToastManager ToastManager { get; set; }

    [ObservableProperty]
    public partial ThemeWatcher ThemeWatcher { get; set; }

    [ObservableProperty]
    public partial ThemeMode CurrentTheme { get; set; } = ThemeMode.System;

    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly NavigationService _navigationService;

    public MainWindowViewModel(
        DialogManager dialogManager,
        ToastManager toastManager,
        ThemeWatcher themeWatcher,
        PageManager pageManager,
        NavigationService navigationService,
        ILogger<MainWindowViewModel> logger
    )
    {
        DialogManager = dialogManager;
        ToastManager = toastManager;
        ThemeWatcher = themeWatcher;
        _logger = logger;

        _navigationService = navigationService;
        SelectedPage = navigationService.GetViewModel(CurrentRoute);
        pageManager.OnNavigate = SwitchPage;
    }

    private void SwitchPage(INavigable page, string route = "")
    {
        if (!route.IsNullOrEmpty())
        {
            var vm = _navigationService.GetViewModel(route);
            SelectedPage = vm;
        }
        else
        {
            SelectedPage = page;
        }
    }

    [ObservableProperty]
    public partial object? SelectedPage { get; set; }

    /// <summary>
    /// 当前激活的菜单项路由
    /// </summary>
    [ObservableProperty]
    public partial string CurrentRoute { get; set; } = "home";

    [RelayCommand]
    private void SwitchPage(string route)
    {
        SwitchPage(null!, route);
    }

    [RelayCommand]
    private void SwitchTheme(ThemeMode themeMode)
    {
        CurrentTheme = themeMode switch
        {
            ThemeMode.System => ThemeMode.Light,
            ThemeMode.Light => ThemeMode.Dark,
            _ => ThemeMode.System,
        };

        ThemeWatcher.SwitchTheme(CurrentTheme);
        _logger.ZLogInformation($"切换主题 {CurrentTheme}");
    }

    [RelayCommand]
    private void ShowAbout()
    {
        var vm = App.Service.GetService<AboutViewModel>();
        DialogManager.ShowCustomDialog(vm);
    }

    public override void Dispose()
    {
        base.Dispose();

        if (SelectedPage is IDisposable disposableCurrent)
        {
            disposableCurrent.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    ~MainWindowViewModel()
    {
        Dispose();
    }
}
