using System.Collections;
using System.ComponentModel;
using System.Windows.Data;

namespace WPFNode.Utilities;

public class DeferredCollectionChange : IDisposable
{
    private readonly ICollectionView? _view;
    private readonly IDisposable? _deferRefresh;
    private bool _disposed;

    public DeferredCollectionChange(IEnumerable collection)
    {
        _view = CollectionViewSource.GetDefaultView(collection);
        _deferRefresh = _view?.DeferRefresh();
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _deferRefresh?.Dispose();
        _disposed = true;
    }
}