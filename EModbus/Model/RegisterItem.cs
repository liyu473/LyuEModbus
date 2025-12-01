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
    private ushort address;

    /// <summary>
    /// 值
    /// </summary>
    [ObservableProperty]
    private ushort value;

    /// <summary>
    /// 描述
    /// </summary>
    [ObservableProperty]
    private string description = string.Empty;

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
    private ushort address;

    /// <summary>
    /// 值
    /// </summary>
    [ObservableProperty]
    private bool value;

    /// <summary>
    /// 描述
    /// </summary>
    [ObservableProperty]
    private string description = string.Empty;

    public CoilItem() { }

    public CoilItem(ushort address, bool value, string description = "")
    {
        Address = address;
        Value = value;
        Description = description;
    }
}
