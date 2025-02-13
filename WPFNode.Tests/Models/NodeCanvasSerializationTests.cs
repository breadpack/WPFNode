using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPFNode.Core.Models;
using WPFNode.Plugins.Basic;
using WPFNode.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WPFNode.Core.Models.Serialization;
using WPFNode.Plugins.Basic.Primitives;
using WPFNode.Abstractions;

namespace WPFNode.Tests.Models;

[TestClass]
public class NodeCanvasSerializationTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly IServiceProvider _serviceProvider;
    private readonly NodePluginService _pluginService;
    private readonly NodeCanvas _canvas;
    private readonly JsonSerializerOptions _jsonOptions;

    public NodeCanvasSerializationTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"nodecanvas_test_{Guid.NewGuid()}.json");
        
        // 서비스 설정
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        
        // NodePluginService를 먼저 생성하고 등록
        _pluginService = new NodePluginService();
        services.AddSingleton<NodePluginService>(_pluginService);
        
        _serviceProvider = services.BuildServiceProvider();

        // 플러그인 어셈블리 로드
        var pluginAssembly = Assembly.GetAssembly(typeof(DoubleInputNode));
        if (pluginAssembly != null)
        {
            foreach (var type in pluginAssembly.GetTypes())
            {
                if (typeof(INode).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    _pluginService.RegisterNodeType(type);
                }
            }
        }

        _canvas = new NodeCanvas();

        // JSON 설정
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = null  // 속성 이름을 그대로 유지
        };
        _jsonOptions.Converters.Add(new NodeCanvasJsonConverter(_serviceProvider));
    }

    [TestMethod]
    public async Task SaveAndLoad_CalculatorGraph_ShouldWorkCorrectly()
    {
        // 1. 계산기 그래프 생성
        // 입력 노드들
        var num1 = _canvas.CreateNode<DoubleInputNode>(100, 100);
        var num2 = _canvas.CreateNode<DoubleInputNode>(100, 200);
        var num3 = _canvas.CreateNode<DoubleInputNode>(100, 300);

        // 연산 노드들
        var add = _canvas.CreateNode<AdditionNode>(300, 150);
        var multiply = _canvas.CreateNode<MultiplicationNode>(500, 200);
        var divide = _canvas.CreateNode<DivisionNode>(700, 250);

        // 출력 노드
        var display = _canvas.CreateNode<ConsoleWriteNode>(900, 250);

        // 2. 연결 구성
        // (num1 + num2) * num3 / num2 를 계산하는 그래프
        _canvas.Connect(num1.OutputPorts[0], add.InputPorts[0]);
        _canvas.Connect(num2.OutputPorts[0], add.InputPorts[1]);
        _canvas.Connect(add.OutputPorts[0], multiply.InputPorts[0]);
        _canvas.Connect(num3.OutputPorts[0], multiply.InputPorts[1]);
        _canvas.Connect(multiply.OutputPorts[0], divide.InputPorts[0]);
        _canvas.Connect(num2.OutputPorts[0], divide.InputPorts[1]);
        _canvas.Connect(divide.OutputPorts[0], display.InputPorts[0]);

        // 3. 값 설정
        ((DoubleInputNode)num1).Value = 10;
        ((DoubleInputNode)num2).Value = 5;
        ((DoubleInputNode)num3).Value = 3;

        // 4. 저장
        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        Console.WriteLine("Saved JSON:");
        Console.WriteLine(json);
        await File.WriteAllTextAsync(_testFilePath, json);

        // 5. 로드
        var loadedJson = await File.ReadAllTextAsync(_testFilePath);
        Console.WriteLine("Loaded JSON:");
        Console.WriteLine(loadedJson);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(loadedJson, _jsonOptions);

        // 6. 검증
        Assert.IsNotNull(loadedCanvas);
        Assert.AreEqual(_canvas.Nodes.Count, loadedCanvas.Nodes.Count);
        Assert.AreEqual(_canvas.Connections.Count, loadedCanvas.Connections.Count);

        // 노드 위치 검증
        var originalNodes = _canvas.Nodes.OrderBy(n => n.Id).ToList();
        var loadedNodes = loadedCanvas.Nodes.OrderBy(n => n.Id).ToList();
        for (int i = 0; i < originalNodes.Count; i++)
        {
            Assert.AreEqual(originalNodes[i].X, loadedNodes[i].X);
            Assert.AreEqual(originalNodes[i].Y, loadedNodes[i].Y);
        }

        // 실행하여 노드들의 값을 초기화
        await _canvas.ExecuteAsync();
        await loadedCanvas.ExecuteAsync();

        // 입력 노드 값 검증
        var originalInputs = originalNodes.OfType<DoubleInputNode>().OrderBy(n => n.Value).ToList();
        var loadedInputs = loadedNodes.OfType<DoubleInputNode>().OrderBy(n => n.Value).ToList();
        for (int i = 0; i < originalInputs.Count; i++)
        {
            Assert.AreEqual(originalInputs[i].Value, loadedInputs[i].Value);
        }

        // 7. 실행 및 결과 확인
        await loadedCanvas.ExecuteAsync();
        // 결과는 콘솔에 ((10 + 5) * 3) / 5 = 9가 출력되어야 함
    }

    [TestMethod]
    public async Task SaveAndLoad_EmptyCanvas_ShouldWorkCorrectly()
    {
        // 빈 캔버스 저장 및 로드 테스트
        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        Assert.IsNotNull(loadedCanvas);
        Assert.AreEqual(0, loadedCanvas.Nodes.Count);
        Assert.AreEqual(0, loadedCanvas.Connections.Count);
    }

    [TestMethod]
    public async Task SaveAndLoad_SingleNode_WithoutConnections_ShouldWorkCorrectly()
    {
        // 단일 노드만 있는 경우 테스트
        var node = _canvas.CreateNode<DoubleInputNode>(100, 100);
        ((DoubleInputNode)node).Value = 42.0;

        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        Assert.IsNotNull(loadedCanvas);
        Assert.AreEqual(1, loadedCanvas.Nodes.Count);
        Assert.AreEqual(0, loadedCanvas.Connections.Count);

        var loadedNode = loadedCanvas.Nodes[0] as DoubleInputNode;
        Assert.IsNotNull(loadedNode);
        Assert.AreEqual(42.0, loadedNode.Value);
        Assert.AreEqual(100, loadedNode.X);
        Assert.AreEqual(100, loadedNode.Y);
    }

    [TestMethod]
    public async Task SaveAndLoad_MultipleNodes_WithCircularConnections_ShouldWorkCorrectly()
    {
        // 순환 연결이 있는 경우 테스트
        var num1 = _canvas.CreateNode<DoubleInputNode>(100, 100);
        var num2 = _canvas.CreateNode<DoubleInputNode>(100, 200);
        var add = _canvas.CreateNode<AdditionNode>(300, 150);
        var multiply = _canvas.CreateNode<MultiplicationNode>(500, 150);

        // 순환 연결 구성
        _canvas.Connect(num1.OutputPorts[0], add.InputPorts[0]);
        _canvas.Connect(num2.OutputPorts[0], add.InputPorts[1]);
        _canvas.Connect(add.OutputPorts[0], multiply.InputPorts[0]);
        _canvas.Connect(multiply.OutputPorts[0], add.InputPorts[0]); // 순환 연결

        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        Assert.IsNotNull(loadedCanvas);
        Assert.AreEqual(4, loadedCanvas.Nodes.Count);
        Assert.AreEqual(4, loadedCanvas.Connections.Count);
    }

    [TestMethod]
    public async Task SaveAndLoad_NodeWithMaxValues_ShouldWorkCorrectly()
    {
        // 최대값, 최소값 경계 테스트
        var node = _canvas.CreateNode<DoubleInputNode>(double.MaxValue, double.MaxValue);
        ((DoubleInputNode)node).Value = double.MaxValue;

        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        var loadedNode = loadedCanvas.Nodes[0] as DoubleInputNode;
        Assert.IsNotNull(loadedNode);
        Assert.AreEqual(double.MaxValue, loadedNode.Value);
        Assert.AreEqual(double.MaxValue, loadedNode.X);
        Assert.AreEqual(double.MaxValue, loadedNode.Y);
    }

    [TestMethod]
    public async Task SaveAndLoad_NodeWithSpecialCharacters_ShouldWorkCorrectly()
    {
        // 특수 문자가 포함된 경우 테스트
        var node = _canvas.CreateNode<DoubleInputNode>(100, 100);
        node.Name = "테스트!@#$%^&*()_+";

        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        var loadedNode = loadedCanvas.Nodes[0];
        Assert.AreEqual("테스트!@#$%^&*()_+", loadedNode.Name);
    }

    [TestMethod]
    [ExpectedException(typeof(JsonException))]
    public async Task SaveAndLoad_InvalidJson_ShouldThrowException()
    {
        // 잘못된 JSON 형식 테스트
        var invalidJson = @"{
            ""Nodes"": [
                {
                    ""X"": 100,
                    ""Y"": 100
                }
            ]
        }"; // $type과 Id가 없는 잘못된 JSON
        JsonSerializer.Deserialize<NodeCanvas>(invalidJson, _jsonOptions);
    }

    [TestMethod]
    public async Task SaveAndLoad_LargeGraph_ShouldWorkCorrectly()
    {
        // 대규모 그래프 테스트
        const int nodeCount = 100;
        var nodes = new List<INode>();

        // 많은 수의 노드 생성 (AdditionNode와 MultiplicationNode를 번갈아가며 생성)
        for (int i = 0; i < nodeCount; i++)
        {
            INode node;
            if (i % 2 == 0)
            {
                node = _canvas.CreateNode<AdditionNode>(i * 100, i * 100);
            }
            else
            {
                node = _canvas.CreateNode<MultiplicationNode>(i * 100, i * 100);
            }
            nodes.Add(node);
        }

        // 연결 생성 (각 노드의 출력을 다음 노드의 첫 번째 입력으로)
        for (int i = 0; i < nodeCount - 1; i++)
        {
            _canvas.Connect(nodes[i].OutputPorts[0], nodes[i + 1].InputPorts[0]);
        }

        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        Assert.IsNotNull(loadedCanvas);
        Assert.AreEqual(nodeCount, loadedCanvas.Nodes.Count);
        Assert.AreEqual(nodeCount - 1, loadedCanvas.Connections.Count);

        // 노드 타입 검증
        var loadedNodes = loadedCanvas.Nodes.ToList();
        for (int i = 0; i < nodeCount; i++)
        {
            if (i % 2 == 0)
            {
                Assert.IsInstanceOfType(loadedNodes[i], typeof(AdditionNode));
            }
            else
            {
                Assert.IsInstanceOfType(loadedNodes[i], typeof(MultiplicationNode));
            }
        }
    }

    [TestMethod]
    public async Task SaveAndLoad_AfterNodeDeletion_ShouldWorkCorrectly()
    {
        // 노드 삭제 후 저장/로드 테스트
        var add = _canvas.CreateNode<AdditionNode>(100, 100);
        var multiply = _canvas.CreateNode<MultiplicationNode>(200, 200);
        
        // 연결 생성 (add의 출력을 multiply의 입력으로)
        var connection = _canvas.Connect(add.OutputPorts[0], multiply.InputPorts[0]);

        // 노드 삭제
        _canvas.RemoveNode(add);

        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        Assert.IsNotNull(loadedCanvas);
        Assert.AreEqual(1, loadedCanvas.Nodes.Count);
        Assert.AreEqual(0, loadedCanvas.Connections.Count);

        // 남은 노드가 MultiplicationNode인지 확인
        var remainingNode = loadedCanvas.Nodes[0];
        Assert.IsInstanceOfType(remainingNode, typeof(MultiplicationNode));
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
} 