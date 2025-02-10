using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WPFNode.Core.Interfaces;
using WPFNode.Core.Models;
using WPFNode.Core.ViewModels.Nodes;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WPFNode.Core.Services;

namespace WPFNode.Demo.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly INodePluginService _pluginService;
    private readonly INodeCommandService _commandService;
    private NodeCanvasViewModel? _nodeCanvasViewModel;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NodeCanvasViewModel? NodeCanvasViewModel
    {
        get => _nodeCanvasViewModel;
        private set
        {
            _nodeCanvasViewModel = value;
            OnPropertyChanged();
        }
    }

    public ICommand AutoLayoutCommand { get; }

    public MainWindowViewModel()
    {
        _pluginService = NodeServices.PluginService;
        _commandService = NodeServices.CommandService;

        AutoLayoutCommand = new RelayCommand(ExecuteAutoLayout, CanExecuteAutoLayout);

        Initialize();
    }

    private bool CanExecuteAutoLayout()
    {
        return NodeCanvasViewModel?.Nodes.Any() == true;
    }

    private void ExecuteAutoLayout()
    {
        if (NodeCanvasViewModel == null) return;

        var nodes = NodeCanvasViewModel.Nodes.ToList();
        var columns = (int)Math.Ceiling(Math.Sqrt(nodes.Count));
        var spacing = 150.0;

        for (int i = 0; i < nodes.Count; i++)
        {
            var row = i / columns;
            var col = i % columns;
            nodes[i].Model.X = col * spacing + 100;
            nodes[i].Model.Y = row * spacing + 100;
        }
    }

    private void Initialize()
    {
        // 플러그인 로드
        _pluginService.LoadPlugins("Plugins");

        // 노드 캔버스 초기화
        var canvas = new NodeCanvas();
        NodeCanvasViewModel = new NodeCanvasViewModel(canvas, _pluginService, _commandService)
        {
            Scale = 1.0,
            OffsetX = 0,
            OffsetY = 0
        };
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 