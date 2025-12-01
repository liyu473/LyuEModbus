using System;
using System.Collections.Concurrent;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using EModbus.Model;
using EModbus.ViewModels;


namespace EModbus;

public class ViewLocator : IDataTemplate
{
    private static readonly ConcurrentDictionary<Type, Type?> Cache = [];

    private static readonly Assembly CurrentAssembly = Assembly.GetExecutingAssembly();

    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        var viewModelType = param.GetType();

        var viewType = Cache.GetOrAdd(viewModelType, type =>
        {
            var nameSpace = type.Namespace;
            if (nameSpace is null) return null;

            var name = type.Name;
            var viewNameSpace = nameSpace.Replace("ViewModels", "Views", StringComparison.Ordinal);

            var viewName = name.Replace("ViewModel", "Page", StringComparison.Ordinal);
            var fullName = $"{viewNameSpace}.{viewName}";

            var view = CurrentAssembly.GetType(fullName);

            if (view is not null) return view;

            viewName = name.Replace("ViewModel", "Content", StringComparison.Ordinal);
            view = CurrentAssembly.GetType($"{viewNameSpace}.{viewName}");

            return view;
        });

        if (viewType is null) return new TextBlock { Text = "View not found: " + viewModelType.FullName };

        var control = (Control)Activator.CreateInstance(viewType)!;
        control.DataContext = param;

        return control;
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase or INavigable;
    }
}