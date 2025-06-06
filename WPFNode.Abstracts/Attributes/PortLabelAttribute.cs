﻿using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class PortLabelAttribute : Attribute
{
    public string Label { get; }

    public PortLabelAttribute(string label)
    {
        Label = label;
    }
}