using System.Windows.Media;

namespace WPFNode.Abstractions.Controls;

public interface INodeControl
{
    object HeaderContent { get; set; }
    Brush HeaderBackground { get; set; }
} 