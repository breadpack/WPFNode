using System;

namespace WPFNode.ViewModels;

public class ViewportChangedEventArgs : EventArgs
{
    public double Scale { get; }
    public double OffsetX { get; }
    public double OffsetY { get; }

    public ViewportChangedEventArgs(double scale, double offsetX, double offsetY)
    {
        Scale = scale;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
} 