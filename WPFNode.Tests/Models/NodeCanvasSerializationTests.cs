using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPFNode.Plugins.Basic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Serialization;
using WPFNode.Plugins.Basic.Primitives;
using WPFNode.Services;
using WPFNode.Constants;

namespace WPFNode.Tests.Models;

[TestClass]
public class NodeCanvasSerializationTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly NodeCanvas _canvas;

    public NodeCanvasSerializationTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"nodecanvas_test_{Guid.NewGuid()}.json");
        _canvas = new NodeCanvas();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        _jsonOptions.Converters.Add(new NodeCanvasJsonConverter());
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
    
    [TestMethod]
    public async Task SaveAndLoad_ConsoleNode_ShouldWorkCorrectly()
    {
        // 콘솔 출력 노드 테스트
        var numberNode  = _canvas.CreateNode<DoubleInputNode>(100, 100);
        var consoleNode = _canvas.CreateNode<ConsoleWriteNode>(100, 100);
        consoleNode.Name = "콘솔 출력 노드";

        numberNode.OutputPorts[0].Connect(consoleNode.InputPorts[0]);

        var json         = _canvas.ToJson();
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        Assert.IsNotNull(loadedCanvas);
        Assert.AreEqual(2, loadedCanvas.Nodes.Count);
        Assert.AreEqual(1, loadedCanvas.Connections.Count);
    }

    [TestMethod]
    public async Task SaveAndLoad_DynamicNode_WithDynamicPorts_ShouldWorkCorrectly()
    {
        // 1. DynamicNode 생성 및 설정
        var dynamicNode = _canvas.CreateDynamicNode(
            "TestDynamicNode",
            "Test",
            "Dynamic node for testing"
        );

        // 동적 포트 추가
        var inputPort1 = dynamicNode.AddInputPort<int>("Input1");
        var inputPort2 = dynamicNode.AddInputPort<string>("Input2");
        var outputPort1 = dynamicNode.AddOutputPort<double>("Output1");
        var outputPort2 = dynamicNode.AddOutputPort<bool>("Output2");

        // 포트 가시성 설정
        inputPort2.IsVisible = false;
        outputPort1.IsVisible = false;

        // 2. 저장
        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        // 3. 검증
        Assert.IsNotNull(loadedCanvas);
        Assert.AreEqual(1, loadedCanvas.Nodes.Count);

        var loadedNode = loadedCanvas.Nodes[0] as DynamicNode;
        Assert.IsNotNull(loadedNode);

        // 포트 수 검증
        Assert.AreEqual(2, loadedNode.InputPorts.Count);
        Assert.AreEqual(2, loadedNode.OutputPorts.Count);

        // 포트 타입 검증
        Assert.AreEqual(typeof(int), loadedNode.InputPorts[0].DataType);
        Assert.AreEqual(typeof(string), loadedNode.InputPorts[1].DataType);
        Assert.AreEqual(typeof(double), loadedNode.OutputPorts[0].DataType);
        Assert.AreEqual(typeof(bool), loadedNode.OutputPorts[1].DataType);

        // 포트 이름 검증
        Assert.AreEqual("Input1", loadedNode.InputPorts[0].Name);
        Assert.AreEqual("Input2", loadedNode.InputPorts[1].Name);
        Assert.AreEqual("Output1", loadedNode.OutputPorts[0].Name);
        Assert.AreEqual("Output2", loadedNode.OutputPorts[1].Name);

        // 포트 가시성 검증
        Assert.IsTrue(loadedNode.InputPorts[0].IsVisible);
        Assert.IsFalse(loadedNode.InputPorts[1].IsVisible);
        Assert.IsFalse(loadedNode.OutputPorts[0].IsVisible);
        Assert.IsTrue(loadedNode.OutputPorts[1].IsVisible);
    }

    [TestMethod]
    public async Task SaveAndLoad_DynamicNode_WithDynamicProperties_ShouldWorkCorrectly()
    {
        // 1. DynamicNode 생성 및 설정
        var dynamicNode = _canvas.CreateDynamicNode(
            "TestDynamicNode",
            "Test",
            "Dynamic node for testing"
        );

        // 동적 프로퍼티 추가
        var intProperty = dynamicNode.AddProperty<int>(
            "IntProperty",
            "Integer Property"
        );
        var stringProperty = dynamicNode.AddProperty<string>(
            "StringProperty",
            "String Property"
        );
        var boolProperty = dynamicNode.AddProperty<bool>(
            "BoolProperty",
            "Boolean Property",
            null,
            true  // 포트로 사용 가능
        );

        // 프로퍼티 값 설정
        intProperty.Value = 42;
        stringProperty.Value = "Test Value";
        boolProperty.Value = true;
        boolProperty.CanConnectToPort = true;

        // 2. 저장
        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        // 3. 검증
        Assert.IsNotNull(loadedCanvas);
        Assert.AreEqual(1, loadedCanvas.Nodes.Count);

        var loadedNode = loadedCanvas.Nodes[0] as DynamicNode;
        Assert.IsNotNull(loadedNode);

        // 프로퍼티 수 검증
        Assert.AreEqual(3, loadedNode.Properties.Count);

        // 프로퍼티 값 검증
        var loadedIntProperty = loadedNode.Properties["IntProperty"];
        var loadedStringProperty = loadedNode.Properties["StringProperty"];
        var loadedBoolProperty = loadedNode.Properties["BoolProperty"];

        Assert.AreEqual(42, loadedIntProperty.Value);
        Assert.AreEqual("Test Value", loadedStringProperty.Value);
        Assert.AreEqual(true, loadedBoolProperty.Value);

        // 프로퍼티 설정 검증
        Assert.IsFalse(loadedIntProperty.CanConnectToPort);
        Assert.IsFalse(loadedStringProperty.CanConnectToPort);
        Assert.IsTrue(loadedBoolProperty.CanConnectToPort);
    }

    [TestMethod]
    public async Task SaveAndLoad_DynamicNode_WithConnections_ShouldWorkCorrectly()
    {
        // 1. 노드 생성
        var sourceNode = _canvas.CreateNode<DoubleInputNode>(100, 100);
        var dynamicNode = _canvas.CreateDynamicNode(
            "TestDynamicNode",
            "Test",
            "Dynamic node for testing",
            200, 100
        );
        var targetNode = _canvas.CreateNode<ConsoleWriteNode>(300, 100);

        // 동적 포트 및 프로퍼티 추가
        var inputPort = dynamicNode.AddInputPort<double>("Input");
        var outputPort = dynamicNode.AddOutputPort<double>("Output");
        var property = dynamicNode.AddProperty<double>(
            "Value",
            "Value Property",
            null,
            true
        );

        // 연결 설정
        _canvas.Connect(sourceNode.OutputPorts[0], inputPort);
        _canvas.Connect(outputPort, targetNode.InputPorts[0]);

        // 값 설정
        ((DoubleInputNode)sourceNode).Value = 42.0;
        property.Value = 123.0;

        // 2. 저장
        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        // 3. 검증
        Assert.IsNotNull(loadedCanvas);
        Assert.AreEqual(3, loadedCanvas.Nodes.Count);
        Assert.AreEqual(2, loadedCanvas.Connections.Count);

        var loadedDynamicNode = loadedCanvas.Nodes
            .OfType<DynamicNode>()
            .FirstOrDefault();
        Assert.IsNotNull(loadedDynamicNode);

        // 포트 연결 검증
        Assert.IsTrue(loadedDynamicNode.InputPorts[0].IsConnected);
        Assert.IsTrue(loadedDynamicNode.OutputPorts[0].IsConnected);

        // 프로퍼티 값 검증
        var loadedProperty = loadedDynamicNode.Properties["Value"];
        Assert.AreEqual(123.0, loadedProperty.Value);
    }

    [TestMethod]
    public async Task SaveAndLoad_DynamicNode_WithPortsAndPropertiesRemoval_ShouldWorkCorrectly()
    {
        // 1. DynamicNode 생성 및 설정
        var dynamicNode = _canvas.CreateDynamicNode(
            "TestDynamicNode",
            "Test",
            "Dynamic node for testing"
        );

        // 포트 및 프로퍼티 추가
        var inputPort1 = dynamicNode.AddInputPort<int>("Input1");
        var inputPort2 = dynamicNode.AddInputPort<string>("Input2");
        var outputPort = dynamicNode.AddOutputPort<double>("Output");
        
        var property1 = dynamicNode.AddProperty<int>(
            "Property1",
            "Property 1",
            null,
            true  // 포트로 사용 가능하도록 설정
        );
        var property2 = dynamicNode.AddProperty<string>(
            "Property2",
            "Property 2"
        );

        // 일부 포트와 프로퍼티 제거
        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);
        var loadedNode = loadedCanvas.Nodes[0] as DynamicNode;
        Assert.IsNotNull(loadedNode);
        
        // 새로운 포트와 프로퍼티 추가
        var newInputPort = loadedNode.AddInputPort<bool>("NewInput");
        var newOutputPort = loadedNode.AddOutputPort<string>("NewOutput");
        var newProperty = loadedNode.AddProperty<bool>(
            "NewProperty",
            "New Property",
            null,
            true  // 포트로 사용 가능하도록 설정
        );

        // 2. 저장
        json = JsonSerializer.Serialize(loadedCanvas, _jsonOptions);
        loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        // 3. 검증
        Assert.IsNotNull(loadedCanvas);
        loadedNode = loadedCanvas.Nodes[0] as DynamicNode;
        Assert.IsNotNull(loadedNode);

        // 포트 검증 (InputPort 2개 + Property 3개)
        Assert.AreEqual(6, loadedNode.InputPorts.Count, "InputPorts 개수가 일치하지 않습니다. (InputPort 3개 + Property 3개)");
        Assert.AreEqual(2, loadedNode.OutputPorts.Count, "OutputPorts 개수가 일치하지 않습니다.");

        // 입력 포트 순서 및 타입 검증
        Assert.AreEqual("Input1", loadedNode.InputPorts[0].Name);
        Assert.AreEqual("Input2", loadedNode.InputPorts[1].Name);
        Assert.AreEqual("Property 1", loadedNode.InputPorts[2].Name);
        Assert.AreEqual("Property 2", loadedNode.InputPorts[3].Name);
        Assert.AreEqual("NewInput", loadedNode.InputPorts[4].Name);
        
        Assert.AreEqual(typeof(int), loadedNode.InputPorts[0].DataType);
        Assert.AreEqual(typeof(string), loadedNode.InputPorts[1].DataType);
        Assert.AreEqual(typeof(int), loadedNode.InputPorts[2].DataType);
        Assert.AreEqual(typeof(string), loadedNode.InputPorts[3].DataType);
        Assert.AreEqual(typeof(bool), loadedNode.InputPorts[4].DataType);

        // Property의 CanConnectToPort 설정에 따른 IsVisible 검증
        Assert.IsTrue(loadedNode.InputPorts[2].IsVisible, "Property1은 CanConnectToPort가 true이므로 IsVisible이어야 합니다");
        Assert.IsFalse(loadedNode.InputPorts[3].IsVisible, "Property2는 CanConnectToPort가 false이므로 IsVisible이 아니어야 합니다");

        // 출력 포트 검증
        Assert.AreEqual("Output", loadedNode.OutputPorts[0].Name);
        Assert.AreEqual("NewOutput", loadedNode.OutputPorts[1].Name);
        Assert.AreEqual(typeof(double), loadedNode.OutputPorts[0].DataType);
        Assert.AreEqual(typeof(string), loadedNode.OutputPorts[1].DataType);

        // 프로퍼티 검증
        Assert.AreEqual(3, loadedNode.Properties.Count, "Properties 개수가 일치하지 않습니다.");
        Assert.IsTrue(loadedNode.Properties.ContainsKey("Property1"));
        Assert.IsTrue(loadedNode.Properties.ContainsKey("Property2"));
        Assert.IsTrue(loadedNode.Properties.ContainsKey("NewProperty"));
        Assert.AreEqual(typeof(int), loadedNode.Properties["Property1"].PropertyType);
        Assert.AreEqual(typeof(string), loadedNode.Properties["Property2"].PropertyType);
        Assert.AreEqual(typeof(bool), loadedNode.Properties["NewProperty"].PropertyType);

        // 프로퍼티의 CanConnectToPort 설정 검증
        Assert.IsTrue(loadedNode.Properties["Property1"].CanConnectToPort);
        Assert.IsFalse(loadedNode.Properties["Property2"].CanConnectToPort);
        Assert.IsTrue(loadedNode.Properties["NewProperty"].CanConnectToPort);
    }

    [TestMethod]
    public async Task SaveAndLoad_DynamicNode_WithOutputPortValues_ShouldWorkCorrectly()
    {
        // 1. DynamicNode 생성 및 설정
        var dynamicNode = _canvas.CreateDynamicNode(
            "TestDynamicNode",
            "Test",
            "Dynamic node for testing"
        );

        // 내부 캔버스에 출력 노드 추가
        var innerOutput1 = dynamicNode.CreateGraphOutput<double>("Output1");
        var innerOutput2 = dynamicNode.CreateGraphOutput<string>("Output2");

        // 가시성 설정
        ((OutputPort<double>)dynamicNode.OutputPorts[0]).IsVisible = false;

        // 내부 출력 노드의 입력 포트에 값 설정
        var doubleInput1 = dynamicNode.InnerCanvas.CreateNode<DoubleInputNode>();
        var stringInput1 = dynamicNode.InnerCanvas.CreateNode<StringInputNode>();
        ((DoubleInputNode)doubleInput1).Value = 42.0;
        ((StringInputNode)stringInput1).Value = "Test Value";
        innerOutput1.Input.Connect(doubleInput1.OutputPorts[0]);
        innerOutput2.Input.Connect(stringInput1.OutputPorts[0]);

        // 직렬화
        var json = _canvas.ToJson();

        // 역직렬화
        var loadedCanvas = NodeCanvas.FromJson(json);
        var loadedNode = (DynamicNode)loadedCanvas.SerializableNodes[0];

        // 포트 수 검증
        Assert.AreEqual(2, loadedNode.OutputPorts.Count);

        // 포트 타입 및 속성 검증
        var loadedPort1 = loadedNode.OutputPorts[0];
        var loadedPort2 = loadedNode.OutputPorts[1];

        Assert.AreEqual(typeof(double), loadedPort1.DataType);
        Assert.AreEqual(typeof(string), loadedPort2.DataType);
        Assert.AreEqual("Output1", loadedPort1.Name);
        Assert.AreEqual("Output2", loadedPort2.Name);
        Assert.IsFalse(loadedPort1.IsVisible);
        Assert.IsTrue(loadedPort2.IsVisible);

        // ProcessAsync 실행 전에는 값이 기본값
        Assert.AreEqual(0.0, loadedPort1.Value);
        Assert.AreEqual(null, loadedPort2.Value);

        // 출력 노드 추가 및 연결
        var consoleNode1 = loadedCanvas.CreateNode<ConsoleWriteNode>();
        var consoleNode2 = loadedCanvas.CreateNode<ConsoleWriteNode>();
        loadedPort1.Connect(consoleNode1.InputPorts[0]);
        loadedPort2.Connect(consoleNode2.InputPorts[0]);

        // ProcessAsync 실행 후 값 검증
        await loadedCanvas.ExecuteAsync();
        Assert.AreEqual(42.0, loadedPort1.Value);
        Assert.AreEqual("Test Value", loadedPort2.Value);
    }

    [TestMethod]
    public async Task SaveAndLoad_DynamicNode_WithPropertyFormatAndControlType_ShouldWorkCorrectly()
    {
        // 1. DynamicNode 생성 및 설정
        var dynamicNode = _canvas.CreateDynamicNode(
            "TestDynamicNode",
            "Test",
            "Dynamic node for testing"
        );

        // 다양한 형식과 컨트롤 타입을 가진 프로퍼티 추가
        var numberProperty = dynamicNode.AddProperty<double>(
            "NumberProperty",
            "Number Property",
            "F2"  // 소수점 2자리 형식
        );

        var timeProperty = dynamicNode.AddProperty<TimeSpan>(
            "TimeProperty",
            "Time Property",
            "hh\\:mm\\:ss"
        );

        var enumProperty = dynamicNode.AddProperty<DayOfWeek>(
            "EnumProperty",
            "Enum Property"
        );

        // 값 설정
        numberProperty.Value = 123.456;
        timeProperty.Value = TimeSpan.FromHours(14.5);
        enumProperty.Value = DayOfWeek.Wednesday;

        // 2. 저장
        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        // 3. 검증
        Assert.IsNotNull(loadedCanvas);
        var loadedNode = loadedCanvas.Nodes[0] as DynamicNode;
        Assert.IsNotNull(loadedNode);

        // 프로퍼티 검증
        var loadedNumberProperty = loadedNode.Properties["NumberProperty"];
        var loadedTimeProperty = loadedNode.Properties["TimeProperty"];
        var loadedEnumProperty = loadedNode.Properties["EnumProperty"];

        // 타입 검증
        Assert.AreEqual(typeof(double), loadedNumberProperty.PropertyType);
        Assert.AreEqual(typeof(TimeSpan), loadedTimeProperty.PropertyType);
        Assert.AreEqual(typeof(DayOfWeek), loadedEnumProperty.PropertyType);

        // 값 검증
        Assert.AreEqual(123.456, loadedNumberProperty.Value);
        Assert.AreEqual(TimeSpan.FromHours(14.5), loadedTimeProperty.Value);
        Assert.AreEqual(DayOfWeek.Wednesday, loadedEnumProperty.Value);

        // 형식 검증
        Assert.AreEqual("F2", loadedNumberProperty.Format);
        Assert.AreEqual("hh\\:mm\\:ss", loadedTimeProperty.Format);
    }

    [TestMethod]
    public async Task SaveAndLoad_DynamicNode_WithInnerGraph_ShouldWorkCorrectly()
    {
        // 1. DynamicNode 생성 및 설정
        var dynamicNode = _canvas.CreateDynamicNode(
            "Calculator",
            "Math",
            "간단한 계산기 노드"
        );

        // 2. 입출력 포트 및 내부 그래프 구성
        var inputA = dynamicNode.CreateGraphInput<double>("A");
        var inputB = dynamicNode.CreateGraphInput<double>("B");
        var output = dynamicNode.CreateGraphOutput<double>("Result");

        // 내부 그래프 로직 구성
        var addNode = dynamicNode.InnerCanvas.CreateNode<AdditionNode>();

        // 연결 구성
        addNode.InputPorts[0].Connect(inputA.Output);
        addNode.InputPorts[1].Connect(inputB.Output);
        output.Input.Connect(addNode.OutputPorts[0]);

        // 3. 저장
        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);

        // 4. 검증
        Assert.IsNotNull(loadedCanvas);
        Assert.AreEqual(1, loadedCanvas.Nodes.Count);

        var loadedNode = loadedCanvas.Nodes[0] as DynamicNode;
        Assert.IsNotNull(loadedNode);

        // 기본 속성 검증
        Assert.AreEqual("Calculator", loadedNode.Name);
        Assert.AreEqual("Math", loadedNode.Category);
        Assert.AreEqual("간단한 계산기 노드", loadedNode.Description);

        // 포트 검증
        Assert.AreEqual(2, loadedNode.InputPorts.Count);
        Assert.AreEqual(1, loadedNode.OutputPorts.Count);

        // 내부 그래프 검증
        Assert.AreEqual(4, loadedNode.InnerCanvas.Nodes.Count); // 2개의 입력 노드 + 1개의 처리 노드 + 1개의 출력 노드
        Assert.AreEqual(3, loadedNode.InnerCanvas.Connections.Count);

        // 입력 값 설정 및 실행
        var num1 = 10.0;
        var num2 = 5.0;

        // 입력 노드에 값 설정
        var sourceNodeA = loadedCanvas.CreateNode<DoubleInputNode>();
        var sourceNodeB = loadedCanvas.CreateNode<DoubleInputNode>();
        ((DoubleInputNode)sourceNodeA).Value = num1;
        ((DoubleInputNode)sourceNodeB).Value = num2;

        // 입력 노드를 DynamicNode의 입력 포트에 연결
        sourceNodeA.OutputPorts[0].Connect(loadedNode.InputPorts[0]);
        sourceNodeB.OutputPorts[0].Connect(loadedNode.InputPorts[1]);

        // 출력 노드 추가 및 연결
        var consoleNode = loadedCanvas.CreateNode<ConsoleWriteNode>();
        loadedNode.OutputPorts[0].Connect(consoleNode.InputPorts[0]);

        // 전체 캔버스 실행
        await loadedCanvas.ExecuteAsync();

        // 결과 검증
        var result = ((OutputPort<double>)loadedNode.OutputPorts[0]).Value;
        Assert.AreEqual(15.0, result);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
} 