using System;
using System.IO;
using Avalonia;
using EModbus.Extensions;
using EModbus.Model;
using EModbus.ViewModels;
using Jab;
using LogExtension;
using LyuEModbus.Abstractions;
using LyuEModbus.Factory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShadUI;

namespace EModbus.Services;

[ServiceProvider]
[Import<IUtilitiesModule>]
[Singleton<MainWindowViewModel>]
[Singleton<HomeViewModel>]
[Singleton<SlaveViewModel>]
[Singleton<MasterViewModel>]
[Singleton<SettingsViewModel>]
[Transient<AboutViewModel>]
[Singleton(typeof(ThemeWatcher), Factory = nameof(ThemeWatcherFactory))]
[Singleton(typeof(ILogger<>), Factory = nameof(CreateLoggerGeneric))]
[Singleton(typeof(PageManager), Factory = nameof(PageManagerFactory))]
[Singleton(typeof(IConfiguration), Factory = nameof(ConfigurationFactory))]
[Singleton(typeof(ModbusSettings), Factory = nameof(ModbusSettingsFactory))]
[Singleton(typeof(IEModbusFactory), Factory = nameof(ModbusFactoryFactory))]
public partial class ServiceProvider : IServiceProvider
{
    // 可替换 ZlogFactory实例，这里使用默认配置
    public static ILogger<T> CreateLoggerGeneric<T>() => ZlogFactory.Get<T>();

    public static ThemeWatcher ThemeWatcherFactory()
    {
        return new ThemeWatcher(Application.Current!);
    }

    private static IConfiguration ConfigurationFactory()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
    }

    private static ModbusSettings ModbusSettingsFactory(IConfiguration configuration) {
        return configuration.GetSection("Modbus").Get<ModbusSettings>() ?? new ModbusSettings();
    }

    public PageManager PageManagerFactory()
    {
        return new PageManager(this);
    }

    public static EModbusFactory ModbusFactoryFactory()
    {
        return new EModbusFactory(ZlogFactory.Factory);
    }
}
