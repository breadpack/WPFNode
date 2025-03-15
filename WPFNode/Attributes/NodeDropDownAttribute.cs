using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NodeDropDownAttribute : Attribute
{
    public string OptionsMethodName { get; }
    public string NameConverterMethodName { get; set; }

    public NodeDropDownAttribute(string optionsMethodName)
    {
        OptionsMethodName = optionsMethodName;
        NameConverterMethodName = string.Empty;
    }
} 