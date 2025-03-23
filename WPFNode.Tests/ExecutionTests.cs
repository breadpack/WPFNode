using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using WPFNode.Models;
using WPFNode.Plugins.Basic;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Plugins.Basic.Constants;
using WPFNode.Plugins.Basic.Variables;
using Xunit;

namespace WPFNode.Tests;

public class ExecutionTests
{
    private readonly ILogger _logger;

    public ExecutionTests()
    {
        // 로깅 설정
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = loggerFactory.CreateLogger<ExecutionTests>();
    }

    [Fact]
    public async Task AdditionNode_TwoNumbers_ShouldGiveCorrectResult()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var numberA = canvas.AddNode<ConstantNode<double>>(0, 0);
        var numberB = canvas.AddNode<ConstantNode<double>>(0, 100);
        var addNode = canvas.AddNode<AdditionNode>(100, 50);

        // 3. 노드 설정
        numberA.Value.Value = 5.0;
        numberB.Value.Value = 7.0;

        // 4. 노드 연결
        // 개선된 API를 사용하여 직관적인 연결
        startNode.FlowOut.Connect(addNode.FlowIn);
        numberA.Result.Connect(addNode.InputA);
        numberB.Result.Connect(addNode.InputB);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인
        Assert.Equal(12.0, addNode.ResultValue);
    }
} 