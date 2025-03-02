using WPFNode.Models;
using WPFNode.Services;
using WPFNode.Tests.TestNodes;
using Xunit;

namespace WPFNode.Tests.Models;

public class LoopNodeTests
{
    private readonly NodeCanvas _canvas;

    public LoopNodeTests()
    {
        NodeServices.Initialize("TestPlugins");
        _canvas = new NodeCanvas();
    }

    [Fact]
    public async Task SimpleLoopExecution_Success()
    {
        // Arrange
        var sequenceNode    = _canvas.CreateNode<NumberSequenceNode>(100, 100);
        var accumulatorNode = _canvas.CreateNode<AccumulatorNode>(300, 100);
        var resultNode      = _canvas.CreateNode<ResultCollectorNode>(500, 100);

        Assert.NotNull(sequenceNode);
        Assert.NotNull(accumulatorNode);
        Assert.NotNull(resultNode);

        // 시퀀스 노드 설정 (1부터 5까지)
        sequenceNode.Reset();  // 기본값 사용 (start = 1, end = 5)

        // 노드 연결
        sequenceNode.ResultPort.Connect(accumulatorNode.InputPort);
        sequenceNode.IsLastPort.Connect(accumulatorNode.IsLastPort);
        accumulatorNode.ResultPort.Connect(resultNode.ValuePort);
        accumulatorNode.IsCompletePort.Connect(resultNode.IsCompletePort);

        // Act
        await _canvas.ExecuteAsync();

        // Assert
        Assert.True(sequenceNode.IsLoopCompleted);
        Assert.Equal(15, accumulatorNode.Sum);  // 1 + 2 + 3 + 4 + 5 = 15
        Assert.Contains(15, resultNode.CollectedValues);
    }

    [Fact]
    public async Task ComplexLoopWithFilterAndTransform_Success()
    {
        // Arrange
        var sequenceNode = _canvas.CreateNode(typeof(NumberSequenceNode), 100, 100) as NumberSequenceNode;
        var multiplyNode = _canvas.CreateNode(typeof(MultiplyNode), 300, 100) as MultiplyNode;
        var filterNode = _canvas.CreateNode(typeof(FilterNode), 500, 100) as FilterNode;
        var accumulatorNode = _canvas.CreateNode(typeof(AccumulatorNode), 700, 100) as AccumulatorNode;
        var resultNode = _canvas.CreateNode(typeof(ResultCollectorNode), 900, 100) as ResultCollectorNode;

        Assert.NotNull(sequenceNode);
        Assert.NotNull(multiplyNode);
        Assert.NotNull(filterNode);
        Assert.NotNull(accumulatorNode);
        Assert.NotNull(resultNode);

        // 시퀀스 노드 설정 (1부터 5까지)
        sequenceNode.Reset();  // 기본값 사용 (start = 1, end = 5)

        // 노드 연결
        _canvas.Connect(sequenceNode.OutputPorts[0], multiplyNode.InputPorts[0]);     // Current -> Input
        _canvas.Connect(multiplyNode.OutputPorts[0], filterNode.InputPorts[0]);       // Result -> Input
        _canvas.Connect(filterNode.OutputPorts[1], accumulatorNode.InputPorts[0]);    // Value -> Input
        _canvas.Connect(sequenceNode.OutputPorts[1], accumulatorNode.InputPorts[1]);  // IsLast -> IsLast
        _canvas.Connect(accumulatorNode.OutputPorts[0], resultNode.InputPorts[0]);    // Sum -> Value
        _canvas.Connect(accumulatorNode.OutputPorts[1], resultNode.InputPorts[1]);    // IsComplete -> IsComplete

        // Act
        await _canvas.ExecuteAsync();

        // Assert
        Assert.True(sequenceNode.IsLoopCompleted);
        
        // 1 -> 2 (짝수) -> 2
        // 2 -> 4 (짝수) -> 4
        // 3 -> 6 (짝수) -> 6
        // 4 -> 8 (짝수) -> 8
        // 5 -> 10 (짝수) -> 10
        // 최종 합계: 30
        Assert.Equal(30, accumulatorNode.Sum);
        Assert.Contains(30, resultNode.CollectedValues);
    }

    [Fact]
    public async Task MultipleLoopNodes_DataGeneration_Success()
    {
        // Arrange
        var sequence1 = _canvas.CreateNode(typeof(NumberSequenceNode), 100, 100) as NumberSequenceNode;
        var sequence2 = _canvas.CreateNode(typeof(NumberSequenceNode), 100, 300) as NumberSequenceNode;
        var collector1 = _canvas.CreateNode(typeof(ResultCollectorNode), 500, 100) as ResultCollectorNode;
        var collector2 = _canvas.CreateNode(typeof(ResultCollectorNode), 500, 300) as ResultCollectorNode;

        Assert.NotNull(sequence1);
        Assert.NotNull(sequence2);
        Assert.NotNull(collector1);
        Assert.NotNull(collector2);

        // 시퀀스 노드 설정 (1부터 5까지)
        sequence1.Reset();  // 기본값 사용 (start = 1, end = 5)
        sequence2.Reset();  // 기본값 사용 (start = 1, end = 5)

        // 각 시퀀스를 별도의 결과 수집기에 연결
        _canvas.Connect(sequence1.OutputPorts[0], collector1.InputPorts[0]);  // Current -> Value
        _canvas.Connect(sequence1.OutputPorts[1], collector1.InputPorts[1]);  // IsLast -> IsComplete
        _canvas.Connect(sequence2.OutputPorts[0], collector2.InputPorts[0]);  // Current -> Value
        _canvas.Connect(sequence2.OutputPorts[1], collector2.InputPorts[1]);  // IsLast -> IsComplete

        // Act
        await _canvas.ExecuteAsync();

        // Assert
        Assert.True(sequence1.IsLoopCompleted);
        Assert.True(sequence2.IsLoopCompleted);

        // 각 시퀀스의 마지막 값(5)이 수집되었는지 확인
        Assert.Equal(5, collector1.CollectedValues.Count);
        Assert.Equal(1, collector1.CollectedValues[0]);
        Assert.Equal(5, collector2.CollectedValues.Count);
        Assert.Equal(1, collector2.CollectedValues[0]);
    }

    [Fact]
    public async Task LoopNodeReset_Success()
    {
        // Arrange
        var sequenceNode = _canvas.CreateNode(typeof(NumberSequenceNode), 100, 100) as NumberSequenceNode;
        var accumulatorNode = _canvas.CreateNode(typeof(AccumulatorNode), 300, 100) as AccumulatorNode;
        var resultNode = _canvas.CreateNode(typeof(ResultCollectorNode), 500, 100) as ResultCollectorNode;

        Assert.NotNull(sequenceNode);
        Assert.NotNull(accumulatorNode);
        Assert.NotNull(resultNode);

        // 시퀀스 노드 설정 (1부터 5까지)
        sequenceNode.Reset();  // 기본값 사용 (start = 1, end = 5)

        // 노드 연결
        _canvas.Connect(sequenceNode.OutputPorts[0], accumulatorNode.InputPorts[0]);  // Current -> Input
        _canvas.Connect(sequenceNode.OutputPorts[1], accumulatorNode.InputPorts[1]);  // IsLast -> IsLast
        _canvas.Connect(accumulatorNode.OutputPorts[0], resultNode.InputPorts[0]);    // Sum -> Value
        _canvas.Connect(accumulatorNode.OutputPorts[1], resultNode.InputPorts[1]);    // IsComplete -> IsComplete

        // Act
        await _canvas.ExecuteAsync();
        Assert.True(sequenceNode.IsLoopCompleted);
        Assert.Equal(15, accumulatorNode.Sum);  // 1 + 2 + 3 + 4 + 5 = 15

        // Reset and execute again
        sequenceNode.Reset();
        accumulatorNode.Reset();
        await _canvas.ExecuteAsync();

        // Assert
        Assert.True(sequenceNode.IsLoopCompleted);
        Assert.Equal(15, accumulatorNode.Sum);  // 1 + 2 + 3 + 4 + 5 = 15
    }

    [Fact]
    public async Task MultipleLoopNodes_OddEvenSequence_Success()
    {
        // Arrange
        var oddSequence = _canvas.CreateNode(typeof(NumberSequenceNode), 100, 100) as NumberSequenceNode;
        var evenSequence = _canvas.CreateNode(typeof(NumberSequenceNode), 100, 300) as NumberSequenceNode;
        var oddCollector = _canvas.CreateNode(typeof(ResultCollectorNode), 500, 100) as ResultCollectorNode;
        var evenCollector = _canvas.CreateNode(typeof(ResultCollectorNode), 500, 300) as ResultCollectorNode;

        Assert.NotNull(oddSequence);
        Assert.NotNull(evenSequence);
        Assert.NotNull(oddCollector);
        Assert.NotNull(evenCollector);

        // 시퀀스 노드 설정 (1부터 5까지)
        oddSequence.Reset();   // 기본값 사용 (start = 1, end = 5)
        evenSequence.Reset();  // 기본값 사용 (start = 1, end = 5)

        // 각 시퀀스를 별도의 컬렉터에 연결
        _canvas.Connect(oddSequence.OutputPorts[0], oddCollector.InputPorts[0]);    // Current -> Value
        _canvas.Connect(oddSequence.OutputPorts[1], oddCollector.InputPorts[1]);    // IsLast -> IsComplete
        _canvas.Connect(evenSequence.OutputPorts[0], evenCollector.InputPorts[0]);  // Current -> Value
        _canvas.Connect(evenSequence.OutputPorts[1], evenCollector.InputPorts[1]);  // IsLast -> IsComplete

        // Act
        await _canvas.ExecuteAsync();

        // Assert
        Assert.True(oddSequence.IsLoopCompleted);
        Assert.True(evenSequence.IsLoopCompleted);

        // 각 시퀀스의 마지막 값(5)이 수집되었는지 확인
        Assert.Equal(5, oddCollector.CollectedValues.Count);
        Assert.Equal(1, oddCollector.CollectedValues[0]);
        Assert.Equal(5, evenCollector.CollectedValues.Count);
        Assert.Equal(1, evenCollector.CollectedValues[0]);
    }

    [Fact]
    public async Task NestedLoopExecution_Success()
    {
        // Arrange
        var numSeq1 = _canvas.CreateNode(typeof(NumberSequenceNode), 100, 100) as NumberSequenceNode;
        var numSeq2 = _canvas.CreateNode(typeof(NumberSequenceNode), 300, 100) as NumberSequenceNode;
        var multiplier = _canvas.CreateNode(typeof(MultiplyNode), 500, 100) as MultiplyNode;
        var resultNode = _canvas.CreateNode(typeof(ResultCollectorNode), 700, 100) as ResultCollectorNode;

        Assert.NotNull(numSeq1);
        Assert.NotNull(numSeq2);
        Assert.NotNull(multiplier);
        Assert.NotNull(resultNode);

        // 노드 연결
        // outer loop -> inner loop start value
        numSeq1.ResultPort.Connect(numSeq2.StartPort);  // Current -> Start

        // inner loop -> multiplier -> result collector
        numSeq2.ResultPort.Connect(multiplier.InputPort);
        multiplier.ResultPort.Connect(resultNode.ValuePort);

        // IsLast 신호 연결
        //_canvas.Connect(numSeq2.OutputPorts[1], resultNode.InputPorts[1]);  // IsLast -> IsComplete

        // Act
        await _canvas.ExecuteAsync();

        // Assert
        Assert.True(numSeq1.IsLoopCompleted);
        Assert.True(numSeq2.IsLoopCompleted);

        // 예상 결과:
        // outer: 1 -> inner: (1,2,3,4,5) -> multiply -> result: 10 (5*2)
        // outer: 2 -> inner: (2,3,4,5) -> multiply -> result: 10 (5*2)
        // outer: 3 -> inner: (3,4,5) -> multiply -> result: 10 (5*2)
        // outer: 4 -> inner: (4,5) -> multiply -> result: 10 (5*2)
        // outer: 5 -> inner: (5) -> multiply -> result: 10 (5*2)
        Assert.Equal(15, resultNode.CollectedValues.Count);
    }
} 