using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using WPFNode.ViewModels.Nodes;
using NodeCanvasViewModel = WPFNode.ViewModels.Nodes.NodeCanvasViewModel;

namespace WPFNode.Controls;

public class NodeCanvasStateManager
{
    private readonly NodeCanvasControl _owner;
    
    // 캐시 저장소
    private readonly Dictionary<NodePortViewModel, PortControl> _portControlCache = new();
    private readonly Dictionary<NodePortViewModel, NodeControl> _nodeControlCache = new();
    private readonly Dictionary<ConnectionViewModel, ConnectionControl> _connectionControlCache = new();

    public NodeCanvasStateManager(NodeCanvasControl owner)
    {
        _owner = owner;
    }

    public void Initialize(NodeCanvasViewModel viewModel)
    {
        ClearCache();
        SubscribeToViewModelEvents(viewModel);
        InitializeControlCache(viewModel);
    }

    public void Cleanup(NodeCanvasViewModel viewModel)
    {
        UnsubscribeFromViewModelEvents(viewModel);
        ClearCache();
    }

    private void ClearCache()
    {
        _portControlCache.Clear();
        _nodeControlCache.Clear();
        _connectionControlCache.Clear();
    }

    private void SubscribeToViewModelEvents(NodeCanvasViewModel viewModel)
    {
        ((INotifyCollectionChanged)viewModel.Nodes).CollectionChanged += OnNodesCollectionChanged;
        ((INotifyCollectionChanged)viewModel.Connections).CollectionChanged += OnConnectionsCollectionChanged;

        foreach (var node in viewModel.Nodes)
        {
            SubscribeToNodeEvents(node);
        }
    }

    private void UnsubscribeFromViewModelEvents(NodeCanvasViewModel viewModel)
    {
        ((INotifyCollectionChanged)viewModel.Nodes).CollectionChanged -= OnNodesCollectionChanged;
        ((INotifyCollectionChanged)viewModel.Connections).CollectionChanged -= OnConnectionsCollectionChanged;

        foreach (var node in viewModel.Nodes)
        {
            UnsubscribeFromNodeEvents(node);
        }
    }

    private void SubscribeToNodeEvents(NodeViewModel node)
    {
        ((INotifyCollectionChanged)node.InputPorts).CollectionChanged += OnPortsCollectionChanged;
        ((INotifyCollectionChanged)node.OutputPorts).CollectionChanged += OnPortsCollectionChanged;
    }

    private void UnsubscribeFromNodeEvents(NodeViewModel node)
    {
        ((INotifyCollectionChanged)node.InputPorts).CollectionChanged -= OnPortsCollectionChanged;
        ((INotifyCollectionChanged)node.OutputPorts).CollectionChanged -= OnPortsCollectionChanged;
    }

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (NodeViewModel node in e.NewItems!)
                {
                    SubscribeToNodeEvents(node);
                    CacheNodeControls(node);
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (NodeViewModel node in e.OldItems!)
                {
                    UnsubscribeFromNodeEvents(node);
                    RemoveNodeFromCache(node);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                ClearCache();
                InitializeControlCache(_owner.ViewModel);
                break;
        }
    }

    private void OnConnectionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (ConnectionViewModel connection in e.NewItems!)
                {
                    CacheConnectionControl(connection);
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (ConnectionViewModel connection in e.OldItems!)
                {
                    _connectionControlCache.Remove(connection);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                _connectionControlCache.Clear();
                break;
        }
    }

    private void OnPortsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is IEnumerable<NodePortViewModel> ports)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (NodePortViewModel port in e.NewItems!)
                    {
                        var nodeViewModel = _owner.ViewModel?.Nodes.FirstOrDefault(n => 
                            n.InputPorts.Contains(port) || n.OutputPorts.Contains(port));
                        if (nodeViewModel != null)
                        {
                            CachePortControl(nodeViewModel, port);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (NodePortViewModel port in e.OldItems!)
                    {
                        _portControlCache.Remove(port);
                        _nodeControlCache.Remove(port);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    var nodePorts = ports.ToList();
                    foreach (var port in nodePorts)
                    {
                        _portControlCache.Remove(port);
                        _nodeControlCache.Remove(port);
                    }
                    break;
            }
        }
    }

    public PortControl? FindPortControl(NodePortViewModel port)
    {
        if (_portControlCache.TryGetValue(port, out var cachedPortControl))
            return cachedPortControl;

        var portControl = FindPortControlInVisualTree(port);
        if (portControl != null)
            _portControlCache[port] = portControl;
            
        return portControl;
    }

    private void InitializeControlCache(NodeCanvasViewModel viewModel)
    {
        foreach (var node in viewModel.Nodes)
        {
            CacheNodeControls(node);
        }
        
        foreach (var connection in viewModel.Connections)
        {
            CacheConnectionControl(connection);
        }
    }

    private void CacheNodeControls(NodeViewModel nodeViewModel)
    {
        var canvas = _owner.GetDragCanvas();
        if (canvas == null) return;

        var nodeItemsControl = FindChildrenOfType<ItemsControl>(canvas).Skip(1).FirstOrDefault();
        if (nodeItemsControl == null) return;

        var container = nodeItemsControl.ItemContainerGenerator.ContainerFromItem(nodeViewModel);
        if (container == null) return;

        var nodeControl = FindChildOfType<NodeControl>(container);
        if (nodeControl == null) return;

        foreach (var port in nodeViewModel.InputPorts.Concat(nodeViewModel.OutputPorts))
        {
            CachePortControl(nodeViewModel, port);
        }
    }

    private void CachePortControl(NodeViewModel nodeViewModel, NodePortViewModel port)
    {
        var nodeControl = _nodeControlCache.Values.FirstOrDefault(nc => nc.ViewModel == nodeViewModel);
        if (nodeControl == null) return;

        var portControl = nodeControl.FindPortControl(port);
        if (portControl != null)
        {
            _portControlCache[port] = portControl;
            _nodeControlCache[port] = nodeControl;
        }
    }

    private void CacheConnectionControl(ConnectionViewModel connection)
    {
        var canvas = _owner.GetDragCanvas();
        if (canvas == null) return;

        var connectionItemsControl = FindChildrenOfType<ItemsControl>(canvas).FirstOrDefault();
        if (connectionItemsControl == null) return;

        var container = connectionItemsControl.ItemContainerGenerator.ContainerFromItem(connection);
        if (container == null)
        {
            // 컨테이너가 아직 생성되지 않았다면, ItemContainerGenerator가 준비될 때까지 대기
            connectionItemsControl.ItemContainerGenerator.StatusChanged += (s, e) =>
            {
                if (connectionItemsControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                {
                    var newContainer = connectionItemsControl.ItemContainerGenerator.ContainerFromItem(connection);
                    if (newContainer != null)
                    {
                        var connectionControl = FindChildOfType<ConnectionControl>(newContainer);
                        if (connectionControl != null)
                        {
                            _connectionControlCache[connection] = connectionControl;
                        }
                    }
                }
            };
            return;
        }

        var connectionControl = FindChildOfType<ConnectionControl>(container);
        if (connectionControl != null)
        {
            _connectionControlCache[connection] = connectionControl;
        }
    }

    private void RemoveNodeFromCache(NodeViewModel node)
    {
        foreach (var port in node.InputPorts.Concat(node.OutputPorts))
        {
            _portControlCache.Remove(port);
            _nodeControlCache.Remove(port);
        }
    }

    private PortControl? FindPortControlInVisualTree(NodePortViewModel port)
    {
        if (_owner.ViewModel == null) return null;

        var nodeViewModel = _owner.ViewModel.Nodes
            .FirstOrDefault(n => n.InputPorts.Contains(port) || n.OutputPorts.Contains(port));

        if (nodeViewModel == null) return null;

        var canvas = _owner.GetDragCanvas();
        if (canvas == null) return null;

        var nodeItemsControl = FindChildrenOfType<ItemsControl>(canvas).Skip(1).FirstOrDefault();
        if (nodeItemsControl == null) return null;

        var container = nodeItemsControl.ItemContainerGenerator.ContainerFromItem(nodeViewModel);
        if (container == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++)
        {
            var child = VisualTreeHelper.GetChild(container, i);
            if (child is NodeControl nodeControl)
            {
                return nodeControl.FindPortControl(port);
            }
        }

        return null;
    }

    private static IEnumerable<T> FindChildrenOfType<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is T result)
                yield return result;
                
            foreach (var childOfChild in FindChildrenOfType<T>(child))
                yield return childOfChild;
        }
    }

    private static T? FindChildOfType<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is T result)
                return result;
        }
        return null;
    }
} 