# WPFNode 아키텍처

## 핵심 구성 요소

WPFNode 라이브러리는 다음과 같은 핵심 구성 요소로 이루어져 있습니다:

### 1. 노드 (Node)

노드는 WPFNode 시스템의 기본 구성 요소입니다. 각 노드는 특정 작업을 수행하며, 입력 포트와 출력 포트를 통해 다른 노드와 통신합니다. 모든 노드는 `NodeBase` 클래스를 상속받습니다.

```csharp
// 노드의 기본 구조
public class SampleNode : NodeBase
{
    // 포트 및 프로퍼티 정의
    
    // 생성자
    public SampleNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) { }
    
    // 노드 실행 로직
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        // 노드 실행 로직 구현
        yield break;
    }
}
```

### 2. 포트 (Port)

포트는 노드 간 데이터 또는 실행 흐름을 전달하는 연결점입니다. WPFNode는 네 가지 유형의 포트를 지원합니다:

- **InputPort<T>**: 데이터 입력 포트 (특정 타입 T의 데이터 수신)
- **OutputPort<T>**: 데이터 출력 포트 (특정 타입 T의 데이터 전송)
- **FlowInPort**: 실행 흐름 입력 포트 (노드 실행 시작점)
- **FlowOutPort**: 실행 흐름 출력 포트 (다음 실행 노드 지정)

```csharp
// 노드에 포트 정의하기
[NodeInput("숫자 입력")]
public InputPort<int> NumberInput { get; set; }

[NodeOutput("결과")]
public OutputPort<int> Result { get; set; }

[NodeFlowIn("실행")]
public FlowInPort FlowIn { get; set; }

[NodeFlowOut("완료")]
public FlowOutPort FlowOut { get; set; }
```

### 3. 프로퍼티 (Property)

노드 프로퍼티는 노드의 동작을 구성하는 설정값입니다. 사용자는 UI를 통해 이러한 프로퍼티를 편집할 수 있습니다.

```csharp
// 노드 프로퍼티 정의하기
[NodeProperty("값", CanConnectToPort = true)]
public NodeProperty<int> Value { get; set; }
```

`CanConnectToPort = true` 옵션을 설정하면 해당 프로퍼티는 입력 포트처럼 작동하여 다른 노드의 출력과 연결할 수 있습니다.

### 4. 연결 (Connection)

연결은 두 노드의 포트를 연결하여 데이터 또는 실행 흐름이 전달되도록 합니다. 연결은 항상 출력 포트에서 입력 포트로 향합니다.

### 5. 캔버스 (Canvas)

캔버스는 노드와 연결을 시각적으로 배치하고 관리하는 작업 공간입니다. `NodeCanvas` 클래스는 노드 추가, 삭제, 연결 관리 등의 기능을 제공합니다.

## 노드 유형 및 기능

WPFNode의 NodeBase 클래스는 두 가지 주요 노드 생성 방식을 지원합니다:

### 1. 정적 노드

어트리뷰트를 사용하여 포트와 프로퍼티를 정의하는 기본적인 방식입니다:

```csharp
[NodeName("덧셈")]
[NodeCategory("수학")]
[NodeDescription("두 숫자를 더합니다.")]
public class AdditionNode : NodeBase
{
    [NodeInput("A")]
    public InputPort<int> InputA { get; set; }
    
    [NodeInput("B")]
    public InputPort<int> InputB { get; set; }
    
    [NodeOutput("결과")]
    public OutputPort<int> Result { get; set; }
    
    // 생성자
    public AdditionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) { }
    
    // 실행 로직
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        int a = InputA.GetValueOrDefault(0);
        int b = InputB.GetValueOrDefault(0);
        Result.Value = a + b;
        yield break;
    }
}
```

### 2. 동적 노드

런타임에 포트 구성을 변경할 수 있는 방식으로, Configure 메서드를 오버라이드하여 구현합니다:

```csharp
[NodeName("동적 입력 노드")]
[NodeCategory("예제")]
[NodeDescription("입력 개수를 동적으로 변경할 수 있는 노드입니다.")]
public class DynamicInputNode : NodeBase
{
    [NodeProperty("입력 개수", OnValueChanged = nameof(ReconfigurePorts))]
    public NodeProperty<int> InputCount { get; set; }
    
    [NodeOutput("결과")]
    public OutputPort<int> Result { get; set; }
    
    public DynamicInputNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        InputCount.Value = 2; // 기본값 설정
    }
    
    protected override void Configure(NodeBuilder builder)
    {
        // 입력 개수에 따라 동적으로 포트 생성
        for (int i = 0; i < InputCount.Value; i++)
        {
            builder.Input<int>($"입력 {i + 1}");
        }
    }
    
    private void ReconfigurePorts()
    {
        base.ReconfigurePorts();
    }
    
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        int sum = 0;
        foreach (var port in InputPorts)
        {
            if (port is InputPort<int> intPort)
            {
                sum += intPort.GetValueOrDefault(0);
            }
        }
        Result.Value = sum;
        yield break;
    }
}
```

## 고급 기능

WPFNode는 다음과 같은 고급 기능도 제공합니다:

1. **타입 변환**: 서로 다른 타입 간의 자동 또는 명시적 변환
2. **비동기 실행**: 비동기 작업 지원
3. **직렬화/역직렬화**: 노드와 연결 구성 저장 및 복원
4. **유효성 검사**: 노드 및 연결 구성의 유효성 검증
5. **이벤트 처리**: 노드 상태 변경 알림
6. **확장성**: 플러그인 시스템을 통한 확장

## 실행 모델

WPFNode의 노드는 다음과 같은 방식으로 실행됩니다:

1. **실행 흐름**: 노드 실행은 FlowInPort를 통해 시작되어 FlowOutPort를 통해 다음 노드로 전파됩니다.
2. **데이터 흐름**: 연결된 OutputPort의 데이터는 InputPort로 전달됩니다.
3. **비동기 실행**: 노드 실행은 비동기적으로 이루어지며, 각 노드는 `ProcessAsync` 메서드에서 실행 로직을 구현합니다.

```csharp
// 노드 실행 로직 예시
protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
    CancellationToken cancellationToken = default)
{
    // 1. 입력 포트에서 데이터 가져오기
    int value = NumberInput?.GetValueOrDefault(0) ?? 0;
    
    // 2. 데이터 처리
    int result = value * 2;
    
    // 3. 결과를 출력 포트에 설정
    Result.Value = result;
    
    // 4. 비동기 작업 수행 (필요한 경우)
    await Task.Delay(100, cancellationToken);
    
    // 5. 다음 실행 노드 지정 (FlowOut 포트 반환)
    yield return FlowOut;
}
```

## 직렬화 및 역직렬화

WPFNode는 노드 캔버스의 상태를 JSON 형식으로 저장하고 불러올 수 있습니다. 이를 통해 사용자는 작업 내용을 파일로 저장하고 나중에 다시 불러올 수 있습니다.

각 노드 클래스는 `WriteJson`과 `ReadJson` 메서드를 제공하여 직렬화 및 역직렬화 과정을 제어할 수 있습니다.

## 다음 단계

이제 WPFNode의 기본 아키텍처를 이해했으므로, 다음 장에서는 실제로 기본 노드를 생성하는 방법에 대해 알아보겠습니다.
