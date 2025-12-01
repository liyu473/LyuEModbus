using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using EModbus.Models;
using Microsoft.Extensions.Configuration;

namespace EModbus.Extensions;

/// <summary>
/// 配置扩展方法
/// </summary>
public static class ConfigurationExtension
{
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    private static IConfiguration? _configuration;
    private static ModbusSettings? _modbusSettings;

    /// <summary>
    /// 获取配置实例
    /// </summary>
    public static IConfiguration Configuration => _configuration ??= BuildConfiguration();

    /// <summary>
    /// 获取 Modbus 配置
    /// </summary>
    public static ModbusSettings ModbusSettings => _modbusSettings ??= GetModbusSettings();

    /// <summary>
    /// 构建配置
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    /// <summary>
    /// 获取 Modbus 配置（手动解析）
    /// </summary>
    private static ModbusSettings GetModbusSettings()
    {
        var section = Configuration.GetSection("Modbus");
        return new ModbusSettings
        {
            Master = new MasterSettings
            {
                IpAddress = section["Master:IpAddress"] ?? "127.0.0.1",
                Port = int.TryParse(section["Master:Port"], out var mp) ? mp : 502,
                SlaveId = byte.TryParse(section["Master:SlaveId"], out var ms) ? ms : (byte)1,
                ReadTimeout = int.TryParse(section["Master:ReadTimeout"], out var rt) ? rt : 3000,
                WriteTimeout = int.TryParse(section["Master:WriteTimeout"], out var wt) ? wt : 3000
            },
            Slave = new SlaveSettings
            {
                IpAddress = section["Slave:IpAddress"] ?? "0.0.0.0",
                Port = int.TryParse(section["Slave:Port"], out var sp) ? sp : 502,
                SlaveId = byte.TryParse(section["Slave:SlaveId"], out var ss) ? ss : (byte)1
            }
        };
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public static void SaveSettings(ModbusSettings settings)
    {
        var jsonObj = new JsonObject
        {
            ["Modbus"] = new JsonObject
            {
                ["Master"] = new JsonObject
                {
                    ["IpAddress"] = settings.Master.IpAddress,
                    ["Port"] = settings.Master.Port,
                    ["SlaveId"] = settings.Master.SlaveId,
                    ["ReadTimeout"] = settings.Master.ReadTimeout,
                    ["WriteTimeout"] = settings.Master.WriteTimeout
                },
                ["Slave"] = new JsonObject
                {
                    ["IpAddress"] = settings.Slave.IpAddress,
                    ["Port"] = settings.Slave.Port,
                    ["SlaveId"] = settings.Slave.SlaveId
                }
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ConfigPath, jsonObj.ToJsonString(options));
        
        // 重新加载配置
        _modbusSettings = settings;
        _configuration = BuildConfiguration();
    }

    /// <summary>
    /// 重新加载配置
    /// </summary>
    public static void ReloadConfiguration()
    {
        _configuration = BuildConfiguration();
        _modbusSettings = GetModbusSettings();
    }
}
