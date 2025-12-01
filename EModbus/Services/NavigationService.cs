using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EModbus.Model;

namespace EModbus.Services;

public class NavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _pages = new();

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        RegisterPages();
    }

    private void RegisterPages()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<PageAttribute>() != null);

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<PageAttribute>();
            if (attr != null)
            {
                _pages[attr.Route] = type;
            }
        }
    }

    public INavigable? GetViewModel(string route)
    {
        if (_pages.TryGetValue(route, out var type))
        {
            return _serviceProvider.GetService(type) as INavigable;
        }
        return null;
    }
}
