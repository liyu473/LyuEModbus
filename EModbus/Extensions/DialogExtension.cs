using System;
using System.Threading.Tasks;
using ShadUI;

namespace EModbus.Extensions;

public static class DialogExtension
{
    /// <summary>
    /// 显示一个类似于MessageBox的对话框,确定按钮执行Action
    /// </summary>
    /// <param name="dialogManager"></param>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <param name="primaryButtonText"></param>
    /// <param name="primaryButtonAction"></param>
    /// <param name="maxWidth"></param>
    /// <param name="minWidth"></param>
    /// <param name="isDismissible">是否可以点击背景关闭对话框</param>
    /// <param name="primaryButtonStyle">可以是Primary(黑)或Destructive(红)</param>
    public static void ShowMessageBox(
        this DialogManager dialogManager,
        string title,
        string message,
        string primaryButtonText = "确定",
        Action? primaryButtonAction = null,
        int maxWidth = 512,
        int minWidth = 300,
        bool isDismissible = false,
        DialogButtonStyle primaryButtonStyle = DialogButtonStyle.Primary
    )
    {
        var dialog = dialogManager
            .CreateDialog(title, message)
            .WithPrimaryButton(primaryButtonText, primaryButtonAction, primaryButtonStyle)
            .WithCancelButton("")
            .WithMaxWidth(maxWidth)
            .WithMinWidth(minWidth);
        if (isDismissible)
        {
            dialog.Dismissible();
        }
        dialog.Show();
    }

    /// <summary>
    /// 显示一个对话框,确定按钮执行Action
    /// </summary>
    /// <param name="dialogManager"></param>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <param name="primaryButtonText"></param>
    /// <param name="primaryButtonAction"></param>
    /// <param name="cancelButtonText"></param>
    /// <param name="maxWidth"></param>
    /// <param name="minWidth"></param>
    /// <param name="isDismissible">是否可以点击背景关闭对话框</param>
    /// <param name="primaryButtonStyle">可以是Primary(黑)或Destructive(红)</param>
    public static void ShowActionDialog(
        this DialogManager dialogManager,
        string title,
        string message,
        Action? primaryButtonAction = null,
        string primaryButtonText = "确定",
        string cancelButtonText = "取消",
        int maxWidth = 512,
        int minWidth = 300,
        bool isDismissible = true,
        DialogButtonStyle primaryButtonStyle = DialogButtonStyle.Primary
    )
    {
        var dialog = dialogManager
            .CreateDialog(title, message)
            .WithPrimaryButton(primaryButtonText, primaryButtonAction, primaryButtonStyle)
            .WithCancelButton(cancelButtonText)
            .WithMaxWidth(maxWidth)
            .WithMinWidth(minWidth);
        if (isDismissible)
        {
            dialog.Dismissible();
        }
        dialog.Show();
    }

    /// <summary>
    /// 显示一个对话框,确定按钮执行Func,返回Task
    /// </summary>
    /// <param name="dialogManager"></param>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <param name="primaryButtonText"></param>
    /// <param name="primaryButtonFunc"></param>
    /// <param name="cancelButtonText"></param>
    /// <param name="maxWidth"></param>
    /// <param name="minWidth"></param>
    /// <param name="isDismissible">是否可以点击背景关闭对话框</param>
    /// <param name="primaryButtonStyle"></param>
    public static void ShowFuncDialog(
        this DialogManager dialogManager,
        string title,
        string message,
        string primaryButtonText = "确定",
        Func<Task>? primaryButtonFunc = null,
        string cancelButtonText = "取消",
        int maxWidth = 512,
        int minWidth = 300,
        bool isDismissible = true,
        DialogButtonStyle primaryButtonStyle = DialogButtonStyle.Primary
    )
    {
        var dialog = dialogManager
            .CreateDialog(title, message)
            .WithPrimaryButton(primaryButtonText, primaryButtonFunc, primaryButtonStyle)
            .WithCancelButton(cancelButtonText)
            .WithMaxWidth(maxWidth)
            .WithMinWidth(minWidth);
        if (isDismissible)
        {
            dialog.Dismissible();
        }
        dialog.Show();
    }

    /// <summary>
    /// 显示一个对话框,确定按钮执行Func,返回Task
    /// </summary>
    /// <param name="dialogManager"></param>
    /// <param name="vm">传入viewmodel(注入DialogManager)</param>
    /// <param name="primaryButtonFunc"></param>
    /// <param name="maxWidth"></param>
    /// <param name="minWidth"></param>
    /// <param name="isDismissible">是否可以点击背景关闭对话框</param>
    public static void ShowCustomDialog(
        this DialogManager dialogManager,
        object vm,
        Func<Task>? primaryButtonFunc = null,
        int maxWidth = 512,
        int minWidth = 300,
        bool isDismissible = true
    )
    {
        var dialog = dialogManager.CreateDialog(vm).WithMaxWidth(maxWidth).WithMinWidth(minWidth);
        if (primaryButtonFunc != null)
        {
            dialog.WithSuccessCallback(primaryButtonFunc);
        }
        if (isDismissible)
        {
            dialog.Dismissible();
        }
        dialog.Show();
    }
    
    /// <summary>
    /// 显示一个对话框,确定按钮执行Func,返回Task
    /// </summary>
    /// <param name="dialogManager"></param>
    /// <param name="vm">传入viewmodel(注入DialogManager)</param>
    /// <param name="primaryButtonAction"></param>
    /// <param name="maxWidth"></param>
    /// <param name="minWidth"></param>
    /// <param name="isDismissible">是否可以点击背景关闭对话框</param>
    public static void ShowCustomDialog<TContext>(
        this DialogManager dialogManager,
        TContext vm,
        Action? primaryButtonAction = null,
        int maxWidth = 512,
        int minWidth = 300,
        bool isDismissible = true
    )
    {
        var dialog = dialogManager.CreateDialog(vm).WithMaxWidth(maxWidth).WithMinWidth(minWidth);
        if (primaryButtonAction != null)
        {
            dialog.WithSuccessCallback(primaryButtonAction);
        }
        if (isDismissible)
        {
            dialog.Dismissible();
        }
        dialog.Show();
    }
}
