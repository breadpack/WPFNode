using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.IO;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Serialization;
using WPFNode.Services;
using WPFNode.ViewModels.Nodes;
using ICommand = System.Windows.Input.ICommand;

namespace WPFNode.Demo.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly INodePluginService _pluginService;
    private readonly INodeCommandService _commandService;
    private NodeCanvasViewModel? _nodeCanvasViewModel;
    private readonly string _saveFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

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
    public ICommand SaveCommand { get; }
    public ICommand LoadCommand { get; }

    public MainWindowViewModel()
    {
        _pluginService = NodeServices.PluginService;
        _commandService = NodeServices.CommandService;
        _saveFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WPFNode",
            "lastSession.json"
        );

        // JSON 설정
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = null
        };
        _jsonOptions.Converters.Add(new NodeCanvasJsonConverter());

        AutoLayoutCommand = new RelayCommand(ExecuteAutoLayout, CanExecuteAutoLayout);
        SaveCommand = new RelayCommand(SaveCanvas);
        LoadCommand = new RelayCommand(LoadCanvas);

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
        
        // 테스트 노드 등록
        _pluginService.RegisterNodeType(typeof(TestNode));

        // 마지막 세션 불러오기 시도
        try
        {
            LoadCanvas();
        }
        catch
        {
            CreateNewCanvas();
        }
    }

    private void CreateNewCanvas()
    {
        NodeCanvasViewModel = new NodeCanvasViewModel()
        {
            Scale = 1.0,
            OffsetX = 0,
            OffsetY = 0
        };
    }

    public void SaveCanvas()
    {
        if (NodeCanvasViewModel?.Model == null) return;

        try
        {
            // 저장 디렉토리가 없으면 생성
            var directory = Path.GetDirectoryName(_saveFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = NodeCanvasViewModel.Model.ToJson();
            File.WriteAllText(_saveFilePath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"캔버스 저장 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void LoadCanvas()
    {
        if (!File.Exists(_saveFilePath))
        {
            CreateNewCanvas();
            return;
        }

        try
        {
            var json = File.ReadAllText(_saveFilePath);
            var canvas = NodeCanvas.FromJson(json);
            NodeCanvasViewModel = new NodeCanvasViewModel(canvas);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"캔버스 불러오기 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            CreateNewCanvas();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 