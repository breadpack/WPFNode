using WPFNode.Interfaces;
using WPFNode.ViewModels.Base;

namespace WPFNode.ViewModels.Nodes;

public class ConnectionViewModel : ViewModelBase
{
    private readonly IConnection        _model;
    private          bool               _isSelected;
    private          NodePortViewModel _source;
    private          NodePortViewModel _target;

    public ConnectionViewModel(IConnection model, NodeCanvasViewModel canvas)
    {
        _model  = model;
        _source = canvas.FindPortViewModel(model.Source);
        _target = canvas.FindPortViewModel(model.Target);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public NodePortViewModel Source
    {
        get => _source;
        private set => SetProperty(ref _source, value);
    }

    public NodePortViewModel Target
    {
        get => _target;
        private set => SetProperty(ref _target, value);
    }
    
    public IConnection Model => _model;
} 