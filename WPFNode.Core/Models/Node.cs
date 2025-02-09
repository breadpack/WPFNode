using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFNode.Core.Models;

public class Node : INotifyPropertyChanged, ICloneable
{
    private string _name;
    private double _x;
    private double _y;
    private bool _isSelected;

    public Node(string id, string name)
    {
        Id = id;
        _name = name;
        InputPorts = new ObservableCollection<NodePort>();
        OutputPorts = new ObservableCollection<NodePort>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string Id { get; }

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

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<NodePort> InputPorts { get; }
    public ObservableCollection<NodePort> OutputPorts { get; }

    public virtual object Clone()
    {
        var clone = new Node(Guid.NewGuid().ToString(), Name + " (Copy)")
        {
            X = X + 20,  // 복사본을 약간 오프셋
            Y = Y + 20
        };

        // 포트 복사
        foreach (var port in InputPorts)
        {
            clone.InputPorts.Add(new NodePort(
                Guid.NewGuid().ToString(),
                port.Name,
                port.DataType,
                true,
                clone));
        }

        foreach (var port in OutputPorts)
        {
            clone.OutputPorts.Add(new NodePort(
                Guid.NewGuid().ToString(),
                port.Name,
                port.DataType,
                false,
                clone));
        }

        return clone;
    }
} 
