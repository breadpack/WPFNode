using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;
using WPFNode.Plugin.SDK;

namespace WPFNode.Core.Models;

public class NodeGroup : INotifyPropertyChanged
{
    private string _name;
    private double _x;
    private double _y;
    private double _width;
    private double _height;
    private bool _isCollapsed;
    private Color _color;
    private readonly List<NodeBase> _nodes = new();

    [JsonConstructor]
    public NodeGroup(string id, string name)
    {
        Id = id;
        _name = name;
        _width = 200;
        _height = 150;
        _color = Colors.LightBlue;
        Nodes = new ObservableCollection<NodeBase>();
        
        // 노드 추가/제거 시 그룹 크기 업데이트
        Nodes.CollectionChanged += (s, e) => UpdateBounds();
    }

    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("x")]
    public double X
    {
        get => _x;
        set
        {
            if (_x != value)
            {
                _x = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("y")]
    public double Y
    {
        get => _y;
        set
        {
            if (_y != value)
            {
                _y = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("width")]
    public double Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("height")]
    public double Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("isCollapsed")]
    public bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            if (_isCollapsed != value)
            {
                _isCollapsed = value;
                foreach (var node in Nodes)
                {
                    node.IsVisible = !value;
                }
                UpdateBounds();
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("color")]
    public Color Color
    {
        get => _color;
        set
        {
            if (_color != value)
            {
                _color = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isAutoSize = true;
    public bool IsAutoSize
    {
        get => _isAutoSize;
        set
        {
            if (_isAutoSize != value)
            {
                _isAutoSize = value;
                if (value)
                {
                    UpdateBounds();
                }
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("nodes")]
    public ObservableCollection<NodeBase> Nodes { get; }

    private void UpdateBounds()
    {
        if (!Nodes.Any() || IsCollapsed)
        {
            Width = 150;  // 접혀있을 때의 기본 크기
            Height = 30;
            return;
        }

        if (!IsAutoSize) return;  // 수동 크기 조절 모드에서는 자동 크기 조정 안 함

        var minX = Nodes.Min(n => n.X);
        var minY = Nodes.Min(n => n.Y);
        var maxX = Nodes.Max(n => n.X);
        var maxY = Nodes.Max(n => n.Y);

        X = minX - 10;  // 여백 추가
        Y = minY - 10;
        Width = maxX - minX + 20;
        Height = maxY - minY + 20;
    }

    public void ResizeAndMoveNodes(double newX, double newY, double newWidth, double newHeight)
    {
        if (IsCollapsed || !Nodes.Any()) return;

        var oldX = X;
        var oldY = Y;
        var oldWidth = Width;
        var oldHeight = Height;

        // 노드들의 상대적 위치를 유지하면서 크기 조절
        foreach (var node in Nodes)
        {
            var relativeX = (node.X - oldX) / oldWidth;
            var relativeY = (node.Y - oldY) / oldHeight;

            node.X = newX + (relativeX * newWidth);
            node.Y = newY + (relativeY * newHeight);
        }

        // 그룹 크기 업데이트
        X = newX;
        Y = newY;
        Width = newWidth;
        Height = newHeight;

        IsAutoSize = false;  // 수동 크기 조절 모드로 전환
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 
