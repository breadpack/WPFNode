using System.Windows.Media;

namespace WPFNode.Interfaces;

public interface INodeControl
{
    object HeaderContent { get; set; }
    Brush HeaderBackground { get; set; }
} 