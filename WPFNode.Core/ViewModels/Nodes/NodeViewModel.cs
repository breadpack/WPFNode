using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WPFNode.Core.Models;
using WPFNode.Core.ViewModels.Base;
using CommunityToolkit.Mvvm.Input;

namespace WPFNode.Core.ViewModels.Nodes;

public class NodeViewModel : ViewModelBase
{
    private readonly Node _node;
    private Point _position;
    private string _name;
    private bool _isSelected;
    private readonly ObservableCollection<NodePortViewModel> _inputPorts;
    private readonly ObservableCollection<NodePortViewModel> _outputPorts;

    public NodeViewModel(Node node)
    {
        _node = node;
        _name = node.Name;
        _position = new Point(node.X, node.Y);
        _isSelected = node.IsSelected;
        
        _inputPorts = new ObservableCollection<NodePortViewModel>(
            node.InputPorts.Select(p => new NodePortViewModel(p)));
        _outputPorts = new ObservableCollection<NodePortViewModel>(
            node.OutputPorts.Select(p => new NodePortViewModel(p)));

        DeleteCommand = new RelayCommand(Delete);
        
        // Model 속성 변경 감지
        _node.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Node.X):
                case nameof(Node.Y):
                    Position = new Point(_node.X, _node.Y);
                    break;
                case nameof(Node.Name):
                    Name = _node.Name;
                    break;
                case nameof(Node.IsSelected):
                    IsSelected = _node.IsSelected;
                    break;
            }
        };
    }

    public string Id => _node.Id;
    
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                _node.Name = value;
            }
        }
    }
    
    public Point Position
    {
        get => _position;
        set
        {
            if (SetProperty(ref _position, value))
            {
                _node.X = value.X;
                _node.Y = value.Y;
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                _node.IsSelected = value;
            }
        }
    }

    public IReadOnlyList<NodePortViewModel> InputPorts => _inputPorts;
    public IReadOnlyList<NodePortViewModel> OutputPorts => _outputPorts;

    public ICommand DeleteCommand { get; }

    private void Delete()
    {
        // 삭제 로직은 NodeCanvasViewModel에서 처리
    }

    public Node Model => _node;
} 