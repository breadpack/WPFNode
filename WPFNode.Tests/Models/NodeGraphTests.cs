using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPFNode.Plugins.Basic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using WPFNode.Models;
using WPFNode.Plugins.Basic.Primitives;
using WPFNode.Services;
using WPFNode.Exceptions;

namespace WPFNode.Tests.Models;

[TestClass]
public class NodeGraphTests
{
    private IServiceProvider _serviceProvider;
    private NodePluginService _pluginService;
    private NodeCanvas _canvas;

    [TestInitialize]
    public void Setup()
    {
        // 서비스 설정
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<NodePluginService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _pluginService = _serviceProvider.GetRequiredService<NodePluginService>();
        _canvas = new NodeCanvas();

        // 플러그인 등록
        _pluginService.RegisterNodeType(typeof(DoubleInputNode));
        _pluginService.RegisterNodeType(typeof(AdditionNode));
        _pluginService.RegisterNodeType(typeof(MultiplicationNode));
        _pluginService.RegisterNodeType(typeof(DivisionNode));
        _pluginService.RegisterNodeType(typeof(ConsoleWriteNode));
    }

    [TestMethod]
    public async Task SimpleCalculation_ShouldExecuteCorrectly()
    {
        // 1. 노드 생성
        var num1 = _canvas.CreateNode<DoubleInputNode>(100, 100);
        var num2 = _canvas.CreateNode<DoubleInputNode>(100, 200);
        var add = _canvas.CreateNode<AdditionNode>(300, 150);
        var display = _canvas.CreateNode<ConsoleWriteNode>(500, 150);

        // 2. 연결 구성 (num1 + num2)
        _canvas.Connect(num1.OutputPorts[0], add.InputPorts[0]);
        _canvas.Connect(num2.OutputPorts[0], add.InputPorts[1]);
        _canvas.Connect(add.OutputPorts[0], display.InputPorts[0]);

        // 3. 값 설정
        ((DoubleInputNode)num1).Value = 10;
        ((DoubleInputNode)num2).Value = 5;

        // 4. 실행 및 검증
        await _canvas.ExecuteAsync();
        // 결과는 콘솔에 15가 출력되어야 함
    }

    [TestMethod]
    public async Task ComplexCalculation_ShouldExecuteCorrectly()
    {
        // 1. 노드 생성
        var num1 = _canvas.CreateNode<DoubleInputNode>(100, 100);
        var num2 = _canvas.CreateNode<DoubleInputNode>(100, 200);
        var num3 = _canvas.CreateNode<DoubleInputNode>(100, 300);
        var add = _canvas.CreateNode<AdditionNode>(300, 150);
        var multiply = _canvas.CreateNode<MultiplicationNode>(500, 200);
        var divide = _canvas.CreateNode<DivisionNode>(700, 250);
        var display = _canvas.CreateNode<ConsoleWriteNode>(900, 250);

        // 2. 연결 구성 ((num1 + num2) * num3) / num2
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

        // 4. 실행 및 검증
        await _canvas.ExecuteAsync();
        // 결과는 콘솔에 ((10 + 5) * 3) / 5 = 9가 출력되어야 함
    }

    [TestMethod]
    public void NodeConnection_ShouldHandleErrors()
    {
        // 1. 노드 생성
        var num1 = _canvas.CreateNode<DoubleInputNode>(100, 100);
        var num2 = _canvas.CreateNode<DoubleInputNode>(100, 200);
        var add = _canvas.CreateNode<AdditionNode>(300, 150);

        // 2. 잘못된 연결 시도 (입력 포트끼리 연결)
        Assert.ThrowsException<NodeConnectionException>(() =>
            _canvas.Connect(add.InputPorts[0], add.InputPorts[1]));

        // 3. 올바른 연결
        var connection = _canvas.Connect(num1.OutputPorts[0], add.InputPorts[0]);
        Assert.IsNotNull(connection);

        // 4. 중복 연결 시도
        Assert.ThrowsException<NodeConnectionException>(() =>
            _canvas.Connect(num1.OutputPorts[0], add.InputPorts[0]));
    }

    [TestMethod]
    public void NodeManagement_ShouldWorkCorrectly()
    {
        // 1. 노드 생성 및 추가
        var num1 = _canvas.CreateNode<DoubleInputNode>(100, 100);
        var num2 = _canvas.CreateNode<DoubleInputNode>(100, 200);
        var add = _canvas.CreateNode<AdditionNode>(300, 150);

        Assert.AreEqual(3, _canvas.Nodes.Count);

        // 2. 연결 생성
        var connection = _canvas.Connect(num1.OutputPorts[0], add.InputPorts[0]);
        Assert.AreEqual(1, _canvas.Connections.Count);

        // 3. 노드 제거 시 연결도 함께 제거되는지 확인
        _canvas.RemoveNode(num1);
        Assert.AreEqual(2, _canvas.Nodes.Count);
        Assert.AreEqual(0, _canvas.Connections.Count);

        // 4. 노드 위치 변경
        add.X = 400;
        add.Y = 300;
        Assert.AreEqual(400, add.X);
        Assert.AreEqual(300, add.Y);
    }
} 