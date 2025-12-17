namespace LyuEModbus.Extensions;

/// <summary>
/// ushort 类型的扩展方法
/// </summary>
public static class UShortExtensions
{
    /// <summary>
    /// 加上指定值
    /// </summary>
    public static ushort Add(this ushort value, int amount)
    {
        var result = value + amount;
        return (ushort)Math.Clamp(result, ushort.MinValue, ushort.MaxValue);
    }

    /// <summary>
    /// 减去指定值
    /// </summary>
    public static ushort Subtract(this ushort value, int amount)
    {
        var result = value - amount;
        return (ushort)Math.Clamp(result, ushort.MinValue, ushort.MaxValue);
    }

    /// <summary>
    /// 自增1
    /// </summary>
    public static ushort Increment(this ushort value)
    {
        return value == ushort.MaxValue ? value : (ushort)(value + 1);
    }

    /// <summary>
    /// 自减1
    /// </summary>
    public static ushort Decrement(this ushort value)
    {
        return value == ushort.MinValue ? value : (ushort)(value - 1);
    }
}
