using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Collections.Generic;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Plugins.Basic;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Plugins.Basic.Constants;
using Xunit;

namespace WPFNode.Tests;

// 테스트에서 실행 경로 및 결과 추적을 위한 간단한 트래커 노드
public class TrackingNode : NodeBase
{
    public List<int> ReceivedValues { get; } = new List<int>();
    
    [NodeFlowIn("Enter")]
    public FlowInPort FlowIn { get; private set; }
    
    [NodeFlowOut("Exit")]
    public FlowOutPort FlowOut { get; private set; }
    
    [NodeInput("Value")]
    public InputPort<int> InputValue { get; private set; }
    
    public TrackingNode(INodeCanvas canvas, Guid id) 
        : base(canvas, id)
    {
    }
    
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(CancellationToken cancellationToken = default)
    {
        // 입력 값을 기록
        int value = InputValue.GetValueOrDefault(0);
        ReceivedValues.Add(value);
        
        await Task.CompletedTask;
        
        // 한 번만 실행되도록 FlowOut 하나만 반환
        yield return FlowOut;
    }
}

// WhileNode 테스트를 위한 특수 조건 노드 - 특정 횟수만큼 true를 반환하고 그 후 false 반환
public class CounterConditionNode : NodeBase
{
    private int _callCount = 0;
    
    [NodeProperty]
    public int MaxTrueCount { get; set; } = 5;
    
    [NodeFlowIn("Execute")]
    public FlowInPort FlowIn { get; private set; }
    
    [NodeFlowOut("Complete")]
    public FlowOutPort FlowOut { get; private set; }
    
    [NodeOutput("Condition")]
    public OutputPort<bool> Condition { get; private set; }
    
    [NodeOutput("Count")]
    public OutputPort<int> Count { get; private set; }
    
    public CounterConditionNode(INodeCanvas canvas, Guid id) 
        : base(canvas, id)
    {
        // 생성자에서 초기화
        _callCount = 0;
    }
    
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(CancellationToken cancellationToken = default)
    {
        // 호출 횟수가 MaxTrueCount 미만이면 true, 이상이면 false 반환
        bool condition = _callCount < MaxTrueCount;
        
        // 호출될 때마다 카운트 증가
        _callCount++;
        
        Condition.Value = condition;
        Count.Value = _callCount - 1;
        
        await Task.CompletedTask;
        
        // 실행 완료 후 FlowOut 포트 반환
        yield return FlowOut;
    }
}

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

    [Fact]
    public async Task IfNode_ConditionTrue_ShouldFollowTruePath()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var conditionNode = canvas.AddNode<ConstantNode<bool>>(0, 100);
        var ifNode = canvas.AddNode<IfNode>(100, 50);
        var truePath = canvas.AddNode<TrackingNode>(200, 0);
        var falsePath = canvas.AddNode<TrackingNode>(200, 100);

        // 3. 노드 설정 - 조건을 true로 설정
        conditionNode.Value.Value = true;

        // 4. 노드 연결
        startNode.FlowOut.Connect(ifNode.FlowIn);
        conditionNode.Result.Connect(ifNode.Condition);
        
        // true, false 경로 연결
        ifNode.TruePort.Connect(truePath.FlowIn);
        ifNode.FalsePort.Connect(falsePath.FlowIn);

        // 트래킹 노드에 값 입력 (경로 구분용)
        var trueValue = canvas.AddNode<ConstantNode<int>>(150, 0);
        var falseValue = canvas.AddNode<ConstantNode<int>>(150, 150);
        trueValue.Value.Value = 1;
        falseValue.Value.Value = 2;
        trueValue.Result.Connect(truePath.InputValue);
        falseValue.Result.Connect(falsePath.InputValue);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - true 경로만 실행되어야 함
        Assert.Single(truePath.ReceivedValues);
        Assert.Equal(1, truePath.ReceivedValues[0]);
        Assert.Empty(falsePath.ReceivedValues);
    }

    [Fact]
    public async Task IfNode_ConditionFalse_ShouldFollowFalsePath()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var conditionNode = canvas.AddNode<ConstantNode<bool>>(0, 100);
        var ifNode = canvas.AddNode<IfNode>(100, 50);
        var truePath = canvas.AddNode<TrackingNode>(200, 0);
        var falsePath = canvas.AddNode<TrackingNode>(200, 100);

        // 3. 노드 설정 - 조건을 false로 설정
        conditionNode.Value.Value = false;

        // 4. 노드 연결
        startNode.FlowOut.Connect(ifNode.FlowIn);
        conditionNode.Result.Connect(ifNode.Condition);
        
        // true, false 경로 연결
        ifNode.TruePort.Connect(truePath.FlowIn);
        ifNode.FalsePort.Connect(falsePath.FlowIn);

        // 트래킹 노드에 값 입력 (경로 구분용)
        var trueValue = canvas.AddNode<ConstantNode<int>>(150, 0);
        var falseValue = canvas.AddNode<ConstantNode<int>>(150, 150);
        trueValue.Value.Value = 1;
        falseValue.Value.Value = 2;
        trueValue.Result.Connect(truePath.InputValue);
        falseValue.Result.Connect(falsePath.InputValue);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - false 경로만 실행되어야 함
        Assert.Empty(truePath.ReceivedValues);
        Assert.Single(falsePath.ReceivedValues);
        Assert.Equal(2, falsePath.ReceivedValues[0]);
    }

    [Fact]
    public async Task ForNode_IteratesCorrectly_WithPositiveStep()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var forNode = canvas.AddNode<ForNode>(100, 50);
        var loopBody = canvas.AddNode<TrackingNode>(200, 0);
        var loopComplete = canvas.AddNode<TrackingNode>(200, 100);

        // 3. 노드 설정 - 0부터 4까지 반복 (총 5회)
        forNode.StartIndex.Value = 0;
        forNode.EndIndex.Value = 4;
        forNode.Step.Value = 1;

        // 4. 노드 연결
        startNode.FlowOut.Connect(forNode.FlowIn);
        forNode.LoopBody.Connect(loopBody.FlowIn);
        forNode.LoopComplete.Connect(loopComplete.FlowIn);
        
        // 트래킹 노드에 현재 인덱스 연결 (루프 본문 실행 확인용)
        forNode.CurrentIndex.Connect(loopBody.InputValue);
        
        // 루프 완료 시 확인용 값 설정
        var completionValue = canvas.AddNode<ConstantNode<int>>(150, 150);
        completionValue.Value.Value = 999;
        completionValue.Result.Connect(loopComplete.InputValue);
        
        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인
        // - 루프 본문이 5회 실행되었는지 확인
        Assert.Equal(5, loopBody.ReceivedValues.Count);
        
        // - 올바른 순서로 인덱스가 증가했는지 확인
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(i, loopBody.ReceivedValues[i]);
        }
        
        // - 루프 완료 후 LoopComplete 포트가 실행되었는지 확인
        Assert.Single(loopComplete.ReceivedValues);
        Assert.Equal(999, loopComplete.ReceivedValues[0]);
    }

    [Fact]
    public async Task ForNode_IteratesCorrectly_WithNegativeStep()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var forNode = canvas.AddNode<ForNode>(100, 50);
        var loopBody = canvas.AddNode<TrackingNode>(200, 0);
        var loopComplete = canvas.AddNode<TrackingNode>(200, 100);

        // 3. 노드 설정 - 10부터 5까지 역순으로 반복 (총 6회)
        forNode.StartIndex.Value = 10;
        forNode.EndIndex.Value = 5;
        forNode.Step.Value = -1;

        // 4. 노드 연결
        startNode.FlowOut.Connect(forNode.FlowIn);
        forNode.LoopBody.Connect(loopBody.FlowIn);
        forNode.LoopComplete.Connect(loopComplete.FlowIn);
        
        // 트래킹 노드에 현재 인덱스 연결 (루프 본문 실행 확인용)
        forNode.CurrentIndex.Connect(loopBody.InputValue);
        
        // 루프 완료 시 확인용 값 설정
        var completionValue = canvas.AddNode<ConstantNode<int>>(150, 150);
        completionValue.Value.Value = 999;
        completionValue.Result.Connect(loopComplete.InputValue);
        
        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인
        // - 루프 본문이 6회 실행되었는지 확인
        Assert.Equal(6, loopBody.ReceivedValues.Count);
        
        // - 올바른 순서로 인덱스가 감소했는지 확인
        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(10 - i, loopBody.ReceivedValues[i]);
        }
        
        // - 루프 완료 후 LoopComplete 포트가 실행되었는지 확인
        Assert.Single(loopComplete.ReceivedValues);
        Assert.Equal(999, loopComplete.ReceivedValues[0]);
    }

    [Fact]
    public async Task WhileNode_RespectsMaxIterations()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var conditionNode = canvas.AddNode<CounterConditionNode>(50, 100);  // ConstantNode 대신 CounterConditionNode 사용
        var whileNode = canvas.AddNode<WhileNode>(100, 50);
        var loopBody = canvas.AddNode<TrackingNode>(200, 0);
        var loopComplete = canvas.AddNode<TrackingNode>(200, 100);

        // 3. 노드 설정
        // - 최대 반복 횟수 설정
        conditionNode.MaxTrueCount = 10;  // 조건은 10회까지 true 반환
        whileNode.MaxIterations = 3;  // 그러나 WhileNode는 최대 3회만 반복

        // 4. 노드 연결
        startNode.FlowOut.Connect(whileNode.FlowIn);
        
        // 데이터 연결 (종속성을 통해 자동으로 재실행됨)
        conditionNode.Condition.Connect(whileNode.Condition);
        
        // Flow 연결
        whileNode.LoopBody.Connect(loopBody.FlowIn);
        whileNode.LoopComplete.Connect(loopComplete.FlowIn);
        
        // 트래킹 노드에 반복 횟수 연결 (루프 실행 확인용)
        whileNode.Iterations.Connect(loopBody.InputValue);
        
        // 루프 완료 시 확인용 값 설정
        var completionValue = canvas.AddNode<ConstantNode<int>>(150, 150);
        completionValue.Value.Value = 999;
        completionValue.Result.Connect(loopComplete.InputValue);
        
        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인
        // - MaxIterations 설정대로 루프 본문이 3회만 실행되었는지 확인
        Assert.Equal(3, loopBody.ReceivedValues.Count);
        
        // - 반복 횟수가 0부터 시작하여 올바르게 증가했는지 확인
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(i, loopBody.ReceivedValues[i]);
        }
        
        // - 최대 반복 횟수 도달 후 LoopComplete 포트가 실행되었는지 확인
        Assert.Single(loopComplete.ReceivedValues);
        Assert.Equal(999, loopComplete.ReceivedValues[0]);
    }

    [Fact]
    public async Task SwitchNode_Basic() {
        var canvas = NodeCanvas.Create();
        
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var switchNode = canvas.AddNode<SwitchNode>(100, 50);
        var case1Node = canvas.AddNode<TrackingNode>(200, 0);
        var case2Node = canvas.AddNode<TrackingNode>(200, 100);
        var defaultNode = canvas.AddNode<TrackingNode>(200, 200);
        
        // 3. 노드 설정
        switchNode.ValueType.Value = typeof(string);
        switchNode.CaseCount.Value = 2; // 두 개의 케이스 설정
        
        // 인덱서를 사용하여 케이스 값 설정
        switchNode[0].Value = "A";
        switchNode[1].Value = "B";

        switchNode.CaseFlowOut(0).Connect(case1Node.FlowIn);
        switchNode.CaseFlowOut(1).Connect(case2Node.FlowIn);
        switchNode.DefaultPort.Connect(defaultNode.FlowIn);
        
        // 입력 값 설정
        var inputValue = canvas.AddNode<ConstantNode<string>>(50, 100);
        inputValue.Value.Value = "A"; // Case A 선택
        
        // 노드 연결
        startNode.FlowOut.Connect(switchNode.FlowIn);
        inputValue.Result.Connect(switchNode.InputValue);
        
        // 트래킹 노드에 값 설정 (실행 경로 확인용)
        var value1 = canvas.AddNode<ConstantNode<int>>(150, 0);
        var value2 = canvas.AddNode<ConstantNode<int>>(150, 100);
        var valueDefault = canvas.AddNode<ConstantNode<int>>(150, 200);
        
        value1.Value.Value = 1;
        value2.Value.Value = 2;
        valueDefault.Value.Value = 999;
        
        value1.Result.Connect(case1Node.InputValue);
        value2.Result.Connect(case2Node.InputValue);
        valueDefault.Result.Connect(defaultNode.InputValue);
        
        // 실행
        await canvas.ExecuteAsync();
        
        // 결과 확인 - Case A가 선택되어야 함
        Assert.Single(case1Node.ReceivedValues);
        Assert.Equal(1, case1Node.ReceivedValues[0]);
        Assert.Empty(case2Node.ReceivedValues);
        Assert.Empty(defaultNode.ReceivedValues);
    }
}
