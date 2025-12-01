using System;
using ShadUI;

namespace EModbus.Extensions;

public static class ToastExtension
{
    /// <summary>
    /// 显示一个Toast消息
    /// </summary>
    /// <param name="toastManager"></param>
    /// <param name="message"></param>
    /// <param name="title"></param>
    /// <param name="dismissOnClick">点击关闭</param>
    /// <param name="type"></param>
    /// <param name="delay">自动关闭（秒）</param>
    public static void ShowToast(
        this ToastManager toastManager,
        string message,
        string title = "提示",
        bool dismissOnClick = true,
        Notification type = Notification.Info,
        int? delay = 2
    )
    {
        var toast = toastManager.CreateToast(title).WithContent(message);
        if (delay.HasValue)
        {
            toast.WithDelay(delay.Value);
        }

        if (dismissOnClick)
        {
            toast.DismissOnClick();
        }

        toast.Show(type);
    }

    /// <summary>
    /// 显示一个Toast消息,带Action按钮
    /// </summary>
    /// <param name="toastManager"></param>
    /// <param name="message"></param>
    /// <param name="action"></param>
    /// <param name="title"></param>
    /// <param name="dismissOnClick">点击关闭</param>
    /// <param name="type"></param>
    /// <param name="delay">自动关闭（秒）</param>
    /// <param name="actionText"></param>
    public static void ShowToast(
        this ToastManager toastManager,
        string message,
        string actionText,
        Action action,
        string title = "提示",
        bool dismissOnClick = true,
        Notification type = Notification.Info,
        int? delay = 2
    )
    {
        var toast = toastManager
            .CreateToast(title)
            .WithContent(message)
            .WithAction(actionText, action);
        if (delay.HasValue)
        {
            toast.WithDelay(delay.Value);
        }

        if (dismissOnClick)
        {
            toast.DismissOnClick();
        }

        toast.Show(type);
    }
}
