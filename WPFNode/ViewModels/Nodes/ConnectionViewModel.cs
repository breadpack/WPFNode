using System.Collections.Specialized;
using WPFNode.Interfaces;
using WPFNode.ViewModels.Base;

namespace WPFNode.ViewModels.Nodes;

public class ConnectionViewModel : ViewModelBase, ISelectable, IDisposable
{
    private readonly IConnection        _model;
    private          NodePortViewModel _source;
    private          NodePortViewModel _target;
    private readonly NodeCanvasViewModel _canvas;

    public ConnectionViewModel(IConnection model, NodeCanvasViewModel canvas)
    {
        _model  = model;
        _canvas = canvas;
        _source = canvas.FindPortViewModel(model.Source);
        _target = canvas.FindPortViewModel(model.Target);
        
        _canvas.SelectedItems.CollectionChanged += SelectedItemsOnCollectionChanged;
    }

    public void   Dispose() {
        _canvas.SelectedItems.CollectionChanged -= SelectedItemsOnCollectionChanged;
    }

    /// <summary>
    /// 연결선이 선택되었는지 확인합니다.
    /// </summary>
    public bool IsSelected => _canvas.IsItemSelected(this);

    /// <summary>
    /// 연결선을 선택합니다.
    /// </summary>
    /// <param name="clearOthers">다른 항목의 선택을 해제할지 여부</param>
    public void Select(bool clearOthers = true)
    {
        _canvas.SelectItem(this, clearOthers);
        OnPropertyChanged(nameof(IsSelected));
    }

    /// <summary>
    /// 연결선의 선택을 해제합니다.
    /// </summary>
    public void Deselect()
    {
        _canvas.DeselectItem(this);
        OnPropertyChanged(nameof(IsSelected));
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

/// <summary>
/// 포트 참조를 최신 상태로 업데이트합니다.
/// 포트가 변경된 경우에만 PropertyChanged 이벤트를 발생시킵니다.
/// </summary>
public void UpdatePortReferences()
{
    var newSource = _canvas.FindPortViewModel(Model.Source);
    var newTarget = _canvas.FindPortViewModel(Model.Target);
    
    if (newSource != _source || newTarget != _target)
    {
        Source = newSource;
        Target = newTarget;
    }
}
    
    public IConnection Model => _model;

    private void SelectedItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        if (e is { Action: NotifyCollectionChangedAction.Add, NewItems: not null }) {
            foreach (ISelectable item in e.NewItems) {
                if (item == this) {
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        } else if (e is { Action: NotifyCollectionChangedAction.Remove, OldItems: not null }) {
            foreach (ISelectable item in e.OldItems) {
                if (item == this) {
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }
        else if (e.Action is NotifyCollectionChangedAction.Reset) {
            OnPropertyChanged(nameof(IsSelected));
        }
    }
    
    // ISelectable 인터페이스 구현
    public Guid   Id            => Model.Guid;
    public string Name          => $"{Source.Name} → {Target.Name}";
    public string SelectionType => "Connection";
}
