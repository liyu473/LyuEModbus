using CommunityToolkit.Mvvm.ComponentModel;

namespace EModbus.Model;

/// <summary>
/// 寄存器项（用于DataGrid绑定）
/// </summary>
public partial class RegisterItem : ObservableObject
{
    /// <summary>
    /// 地址
    /// </summary>
    [ObservableProperty]
    public partial ushort Address { get; set; }

    /// <summary>
    /// 值
    /// </summary>
    [ObservableProperty]
    public partial ushort Value { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    public RegisterItem() { }

    public RegisterItem(ushort address, ushort value, string description = "")
    {
        Address = address;
        Value = value;
        Description = description;
    }
}

/// <summary>
/// 线圈项（用于DataGrid绑定）
/// </summary>
public partial class CoilItem : ObservableObject
{
    /// <summary>
    /// 地址
    /// </summary>
    [ObservableProperty]
    public partial ushort Address { get; set; }

    /// <summary>
    /// 值
    /// </summary>
    [ObservableProperty]
    public partial bool Value { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    public CoilItem() { }

    public CoilItem(ushort address, bool value, string description = "")
    {
        Address = address;
        Value = value;
        Description = description;
    }
}
