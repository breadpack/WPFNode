using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFNode.Core.Models;

public class Connection : INotifyPropertyChanged
{
    private bool _isValid;

    public Connection(string id, NodePort source, NodePort target)
    {
        if (source.IsInput == target.IsInput)
            throw new ArgumentException("Source and target must be of different types (input/output)");

        Id = id;
        Source = source.IsInput ? target : source;
        Target = source.IsInput ? source : target;
        _isValid = true;

        Source.AddConnection(this);
        Target.AddConnection(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string Id { get; }
    public NodePort Source { get; }
    public NodePort Target { get; }

    public bool IsValid
    {
        get => _isValid;
        private set
        {
            if (_isValid != value)
            {
                _isValid = value;
                OnPropertyChanged();
            }
        }
    }

    public void Disconnect()
    {
        if (!IsValid) return;

        Source.RemoveConnection(this);
        Target.RemoveConnection(this);
        IsValid = false;
    }
} 
