using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// IModbusMasterClient 读写扩展方法
/// <para>按寄存器类型分为：线圈、保持寄存器、输入寄存器、离散输入</para>
/// </summary>
public static partial class ModbusMasterExtensions
{
    /// <summary>
    /// 默认重试间隔（毫秒）
    /// </summary>
    private const int DefaultRetryDelayMs = 100;
}
