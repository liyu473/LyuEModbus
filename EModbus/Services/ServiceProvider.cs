using System;
using Avalonia;
using EModbus.Extensions;
using EModbus.Models;
using EModbus.ViewModels;
using Jab;
using LogExtension;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShadUI;

namespace EModbus.Services;

[ServiceProvider]
[Import<IUtilitiesModule>]
[Singleton<MainWindowViewModel>]
[Singleton<HomeViewModel>]
[Singleton<SettingsViewModel>]
[Transient<AboutViewModel>]
[Singleton(typeof(ThemeWatcher), Factory = nameof(ThemeWatcherFactory))]
[Singleton(typeof(ILogger<>), Factory = nameof(CreateLoggerGeneric))]
[Singleton(typeof(PageManager), Factory = nameof(PageManagerFactory))]
[Singleton(typeof(IConfiguration), Factory = nameof(ConfigurationFactory))]
[Singleton(typeof(ModbusSettings), Factory = nameof(ModbusSettingsFactory))]
public partial class ServiceProvider : IServiceProvider
{
    // 可替换 ZlogFactory实例，这里使用默认配置
    public static ILogger<T> CreateLoggerGeneric<T>() => ZlogFactory.Get<T>();

    public static ThemeWatcher ThemeWatcherFactory()
    {
        return new ThemeWatcher(Application.Current!);
    }
    
    public  PageManager PageManagerFactory()
    {
        return new PageManager(this);
    }

    public static IConfiguration ConfigurationFactory()
    {
        return ConfigurationExtension.Configuration;
    }

    public static ModbusSettings ModbusSettingsFactory()
    {
        return ConfigurationExtension.ModbusSettings;
    }
}
