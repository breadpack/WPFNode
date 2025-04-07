using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using WPFNode.Interfaces;
using WPFNode.ViewModels.Nodes;
using NodeCanvasViewModel = WPFNode.ViewModels.Nodes.NodeCanvasViewModel;

namespace WPFNode.Controls;

public class NodeCanvasStateManager
{
    private readonly NodeCanvasControl _owner;
    
    public NodeCanvasStateManager(NodeCanvasControl owner)
    {
        _owner = owner;
    }

    public void Initialize(NodeCanvasViewModel viewModel)
    {
        SubscribeToViewModelEvents(viewModel);
    }

    public void Cleanup(NodeCanvasViewModel viewModel)
    {
        UnsubscribeFromViewModelEvents(viewModel);
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
        ((INotifyCollectionChanged)node.FlowInPorts).CollectionChanged += OnPortsCollectionChanged;
        ((INotifyCollectionChanged)node.FlowOutPorts).CollectionChanged += OnPortsCollectionChanged;
    }

    private void UnsubscribeFromNodeEvents(NodeViewModel node)
    {
        ((INotifyCollectionChanged)node.InputPorts).CollectionChanged -= OnPortsCollectionChanged;
        ((INotifyCollectionChanged)node.OutputPorts).CollectionChanged -= OnPortsCollectionChanged;
        ((INotifyCollectionChanged)node.FlowInPorts).CollectionChanged -= OnPortsCollectionChanged;
        ((INotifyCollectionChanged)node.FlowOutPorts).CollectionChanged -= OnPortsCollectionChanged;
    }

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (NodeViewModel node in e.NewItems!)
                {
                    SubscribeToNodeEvents(node);
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (NodeViewModel node in e.OldItems!)
                {
                    UnsubscribeFromNodeEvents(node);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                break;
        }
    }

    private void OnConnectionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
    }

    private void OnPortsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
    }

    public PortControl? FindPortControl(NodePortViewModel port)
    {
        return FindPortControlInVisualTree(port);
    }
    
    /// <summary>
    /// 특정 포트에 대한 부모 노드 ViewModel을 찾습니다.
    /// </summary>
    public NodeViewModel? FindNodeForPort(NodePortViewModel port)
    {
        if (_owner.ViewModel == null || port == null) return null;
        
        // 해당 포트를 포함하는 노드 찾기
        return _owner.ViewModel.Nodes
            .FirstOrDefault(n => n.InputPorts.Contains(port) || 
                                 n.OutputPorts.Contains(port) || 
                                 n.FlowInPorts.Contains(port) || 
                                 n.FlowOutPorts.Contains(port));
    }

    private PortControl? FindPortControlInVisualTree(NodePortViewModel port)
    {
        if (_owner.ViewModel == null) return null;

        // 해당 포트를 포함하는 노드 찾기
        var nodeViewModel = _owner.ViewModel.Nodes
            .FirstOrDefault(n => n.InputPorts.Contains(port) || 
                               n.OutputPorts.Contains(port) || 
                               n.FlowInPorts.Contains(port) || 
                               n.FlowOutPorts.Contains(port));

        if (nodeViewModel == null) return null;

        // 캔버스 컨트롤 가져오기
        var canvas = _owner.GetDragCanvas();
        if (canvas == null) return null;

        // 노드를 표시하는 ItemsControl 찾기 (일반적으로 두 번째 ItemsControl)
        var nodeItemsControl = FindChildrenOfType<ItemsControl>(canvas);
        
        var container = nodeItemsControl.Select(n => n.ItemContainerGenerator.ContainerFromItem(nodeViewModel))
                        .FirstOrDefault(n => n != null);
        if (container == null) return null;

        // 노드 컨트롤 찾기
        var nodeControl = FindChildOfType<NodeControl>(container);
        if (nodeControl == null) return null;

        // 노드 컨트롤에서 포트 컨트롤 찾기
        return nodeControl.FindPortControl(port);
    }

    private static IEnumerable<T> FindChildrenOfType<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) yield break;

        var queue = new Queue<DependencyObject>();
        queue.Enqueue(parent);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int childCount = VisualTreeHelper.GetChildrenCount(current);
            
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(current, i);
                
                if (child is T result)
                    yield return result;
                
                queue.Enqueue(child);
            }
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
