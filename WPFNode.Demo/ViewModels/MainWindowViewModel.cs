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
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using WPFNode.Demo.Models;
using WPFNode.Demo.Nodes;
using WPFNode.Demo.Services;

namespace WPFNode.Demo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly INodePluginService _pluginService;
    private readonly INodeCommandService _commandService;
    private readonly MigrationService _migrationService;
    private readonly string _saveFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    [ObservableProperty]
    private NodeCanvasViewModel _nodeCanvasViewModel;

    [ObservableProperty]
    private ObservableCollection<TableData> _availableTables = new();

    [ObservableProperty]
    private TableData _selectedTable;

    [ObservableProperty]
    private string _migrationJsonResult;

    [ObservableProperty]
    private bool _isMigrationCompleted;

    [ObservableProperty]
    private string _statusMessage = "준비됨";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand AutoLayoutCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand LoadCommand { get; }
    public ICommand LoadedCommand { get; }
    public ICommand SaveMigrationPlanCommand { get; }
    public ICommand LoadMigrationPlanCommand { get; }
    public ICommand ExecuteMigrationCommand { get; }

    public MainWindowViewModel()
    {
        _pluginService = NodeServices.PluginService;
        _commandService = NodeServices.CommandService;
        _saveFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WPFNode",
            "lastSession.json"
        );

        // 마이그레이션 서비스 초기화
        _migrationService = new MigrationService(_pluginService);

        // JSON 설정
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = null
        };
        _jsonOptions.Converters.Add(new NodeCanvasJsonConverter());

        // 명령 초기화
        AutoLayoutCommand = new RelayCommand(ExecuteAutoLayout, CanExecuteAutoLayout);
        SaveCommand = new RelayCommand(SaveCanvas);
        LoadCommand = new RelayCommand(LoadCanvas);
        LoadedCommand = new AsyncRelayCommand(OnLoadedAsync);
        SaveMigrationPlanCommand = new RelayCommand(SaveMigrationPlan, CanSaveMigrationPlan);
        LoadMigrationPlanCommand = new RelayCommand<string>(LoadMigrationPlan);
        ExecuteMigrationCommand = new AsyncRelayCommand(ExecuteMigrationAsync, CanExecuteMigration);

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
        _pluginService.RegisterNodeType(typeof(ExcelInputNode));
        _pluginService.RegisterNodeType(typeof(TableOutputNode));

        // 샘플 테이블 데이터 생성
        AvailableTables.Add(TableDataGenerator.CreateSampleEmployeeData());
        AvailableTables.Add(TableDataGenerator.CreateSampleProductData());

        // 기본 테이블 선택
        SelectedTable = AvailableTables.FirstOrDefault();

        CreateNewCanvas();
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

    private async Task OnLoadedAsync()
    {
        try
        {
            if (SelectedTable != null)
            {
                await LoadOrCreateCanvasForTableAsync(SelectedTable);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"노드 로드 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadOrCreateCanvasForTableAsync(TableData tableData)
    {
        if (tableData == null) return;

        try
        {
            NodeCanvas canvas;
            
            // 테이블에 대한 마이그레이션 플랜이 있는지 확인
            if (_migrationService.MigrationPlanExists(tableData.TableName))
            {
                // 마이그레이션 플랜 로드
                canvas = _migrationService.LoadMigrationPlan(tableData.TableName);
                NodeCanvasViewModel = new NodeCanvasViewModel(canvas);
                
                StatusMessage = $"{tableData.TableName} 테이블에 대한 마이그레이션 플랜을 로드했습니다.";
            }
            else {
                CreateCanvasForTable(tableData);

                StatusMessage = $"{tableData.TableName} 테이블에 대한 새 마이그레이션 플랜을 생성했습니다.";
            }
        }
        catch (Exception ex) {
            MessageBox.Show($"테이블 로드 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            CreateCanvasForTable(tableData);
        }
    }

    private void CreateCanvasForTable(TableData tableData) {
        // 새 캔버스 생성
        CreateNewCanvas();
        var canvas = NodeCanvasViewModel.Model;

        // ExcelInputNode 생성
        var nodeType = typeof(ExcelInputNode);
        var node     = canvas.CreateNode(nodeType, 100, 100);
        if (node is ExcelInputNode excelNode)
        {
            excelNode.Id = tableData.TableName;
            excelNode.SetTableData(tableData);
        }
    }

    private bool CanSaveMigrationPlan()
    {
        return NodeCanvasViewModel?.Model != null && SelectedTable != null;
    }

    private void SaveMigrationPlan()
    {
        if (NodeCanvasViewModel?.Model == null || SelectedTable == null) return;

        try
        {
            _migrationService.SaveMigrationPlan(NodeCanvasViewModel.Model, SelectedTable.TableName);
            StatusMessage = $"{SelectedTable.TableName} 테이블에 대한 마이그레이션 플랜을 저장했습니다.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"마이그레이션 플랜 저장 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadMigrationPlan(string tableName) {
        if (string.IsNullOrEmpty(tableName)) return;

        // 테이블 이름에 해당하는 테이블 데이터 찾기
        var tableData = AvailableTables.FirstOrDefault(t => t.TableName == tableName);
        if (tableData == null) {
            MessageBox.Show($"'{tableName}' 테이블을 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 테이블 선택
        SelectedTable = tableData;

        try {
            // 마이그레이션 플랜 로드 또는 생성
            var canvas = _migrationService.LoadMigrationPlan(tableName);
            NodeCanvasViewModel = new NodeCanvasViewModel(canvas);
        }
        catch (Exception ex) {
            MessageBox.Show($"마이그레이션 플랜 로드 중 오류 발생. 새 마이그레이션 플랜을 생성합니다.\n: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            CreateCanvasForTable(tableData);
        }

        // 마이그레이션 플랜이 존재하는지 여부에 따라 메시지 표시
        if (_migrationService.MigrationPlanExists(tableName)) {
            StatusMessage = $"{tableName} 테이블에 대한 마이그레이션 플랜을 로드했습니다.";
        }
        else {
            StatusMessage = $"{tableName} 테이블에 대한 새 마이그레이션 플랜을 생성했습니다.";
        }
    }

    private bool CanExecuteMigration()
    {
        return SelectedTable != null && NodeCanvasViewModel?.Model != null;
    }

    private async Task ExecuteMigrationAsync()
    {
        if (SelectedTable == null || NodeCanvasViewModel?.Model == null) return;

        try
        {
            StatusMessage = "마이그레이션 실행 중...";
            IsMigrationCompleted = false;

            // JSON 결과 가져오기
            var jsonResult = await _migrationService.MigrateTableDataToJsonAsync(SelectedTable);
            
            // 결과 설정
            MigrationJsonResult = jsonResult;
            IsMigrationCompleted = true;
            StatusMessage = $"{SelectedTable.TableName} 테이블 마이그레이션이 완료되었습니다.";
        }
        catch (Exception ex)
        {
            IsMigrationCompleted = false;
            StatusMessage = $"마이그레이션 실행 중 오류 발생: {ex.Message}";
            MessageBox.Show($"마이그레이션 실행 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            
            StatusMessage = "캔버스가 저장되었습니다.";
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
            
            StatusMessage = "캔버스가 로드되었습니다.";
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