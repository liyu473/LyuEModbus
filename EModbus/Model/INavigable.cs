using System;

namespace EModbus.Model;

public interface INavigable
{
    void Initialize()
    {
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PageAttribute(string route) : Attribute
{
    public string Route { get; } = route;
}