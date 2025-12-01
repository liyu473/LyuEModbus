using System;
using System.Reflection;
using EModbus.Model;

namespace EModbus.Services;

public sealed class PageManager(ServiceProvider serviceProvider)
{
    /// <summary>
    /// 传入viewmodel类型（继承自INavigable或继承自ViewModelBase）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="InvalidOperationException"></exception>
    public void Navigate<T>() where T : INavigable
    {
        var attr = typeof(T).GetCustomAttribute<PageAttribute>();
        if (attr is null) throw new InvalidOperationException("Not a valid page type, missing PageAttribute");

        var page = serviceProvider.GetService<T>();
        if (page is null) throw new InvalidOperationException("Page not found");

        OnNavigate?.Invoke(page, attr.Route);
    }

    private Action<INavigable, string>? _onNavigate;

    public Action<INavigable, string>? OnNavigate
    {
        private get => _onNavigate;
        set
        {
            if (_onNavigate is not null)
            {
                throw new InvalidOperationException("OnNavigate is already set");
            }

            _onNavigate = value;
        }
    }
}