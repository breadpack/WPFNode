# 동적 노드 만들기

이 가이드에서는 런타임에 포트 구성을 변경할 수 있는 동적 노드(DynamicNode)를 생성하는 방법을 설명합니다.

## 동적 노드 소개

`DynamicNode`는 `NodeBase`를 확장한 특수 클래스로, 런타임에 노드의 포트 및 프로퍼티 구성을 변경할 수 있습니다. 이러한 기능은 다음과 같은 상황에서 유용합니다:

- 사용자 입력에 따라 입력 또는 출력 포트의 수를 조정해야 할 때
- 선택된 설정에 따라 다른 프로퍼티를 표시해야 할 때
- 외부 데이터 소스의 스키마에 따라 포트를 동적으로 생성해야 할 때
- 노드의 동작을 런타임에 완전히 재구성해야 할 때

## 1단계: DynamicNode 클래스 상속

먼저, `DynamicNode` 클래스를 상속받는 새로운 클래스를 생성합니다:

```csharp
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace YourNamespace;

[NodeName("동적 입력 노드")]
[NodeCategory("예제")]
[NodeDescription("입력 개수를 동적으로 변경할 수 있는 노드입니다.")]
public class DynamicInputNode : DynamicNode
{
    public DynamicInputNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        // 초기화 코드
    }
    
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 노드 실행 로직 구현
        yield break;
    }
}
```

## 2단계: 동적 구성을 위한 프로퍼티 정의

동적 노드는 일반적으로 포트 구성을 제어하는 프로퍼티를 가집니다. 이 프로퍼티의 값이 변경되면 노드의 포트 구성이 다시 설정됩니다:

```csharp
[NodeProperty("입력 개수", OnValueChanged = nameof(ReconfigurePorts))]
public NodeProperty<int> InputCount { get; set; }

[NodeOutput("결과")]
public OutputPort<int> Result { get; set; }

[NodeFlowIn("실행")]
public FlowInPort FlowIn { get; set; }

[NodeFlowOut("완료")]
public FlowOutPort FlowOut { get; set; }

public DynamicInputNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
{
    InputCount.Value = 2; // 기본값 설정
}
```

`OnValueChanged = nameof(ReconfigurePorts)` 속성은 `InputCount` 값이 변경될 때마다 `ReconfigurePorts` 메서드가 호출되도록 지정합니다.

## 3단계: Configure 메서드 구현

`DynamicNode`의 핵심은 `Configure` 메서드입니다. 이 메서드는 노드의 포트 구성을 정의하며, 노드가 초기화될 때와 재구성될 때 호출됩니다:

```csharp
protected override void Configure(NodeBuilder builder)
{
    // InputCount에 따라 동적으로 입력 포트 생성
    for (int i = 0; i < InputCount.Value; i++)
    {
        builder.Input<int>($"입력 {i + 1}");
    }
    
    // 기존 포트(어트리뷰트로 정의된)는 자동으로 유지됩니다
    // 아래와 같은 고정 포트는 Configure에서 추가할 필요가 없습니다
    // - Result (OutputPort)
    // - FlowIn (FlowInPort)
    // - FlowOut (FlowOutPort)
}
```

`NodeBuilder` 클래스는 다음과 같은 메서드를 제공합니다:

- `Input<T>(string name)`: 특정 타입의 입력 포트 추가
- `Output<T>(string name)`: 특정 타입의 출력 포트 추가
- `FlowIn(string name)`: 흐름 입력 포트 추가
- `FlowOut(string name)`: 흐름 출력 포트 추가
- `Property<T>(string name, string displayName, ...)`: 프로퍼티 추가

## 4단계: 포트 재구성 메서드 구현

포트 구성을 변경하는 메서드를 구현합니다. 이 메서드는 프로퍼티 값이 변경될 때 호출됩니다:

```csharp
private void ReconfigurePorts()
{
    // DynamicNode의 ReconfigurePorts 메서드 호출
    // 이 메서드는 동적 포트를 제거하고 Configure를 다시 호출합니다
    base.ReconfigurePorts();
}
```

`DynamicNode.ReconfigurePorts()` 메서드는 다음과 같은 작업을 수행합니다:
1. 동적으로 추가된 포트와 프로퍼티를 제거합니다 (어트리뷰트로 정의된 포트는 유지)
2. 노드를 재초기화하고 `Configure` 메서드를 다시 호출합니다

## 5단계: 노드 실행 로직 구현

동적 노드의 실행 로직은 `ProcessAsync` 메서드에서 구현합니다. 이 메서드는 현재 포트 구성에 따라 동작해야 합니다:

```csharp
protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // 모든 입력 포트의 값을 합산
    int sum = 0;
    
    // InputPorts에서 동적으로 생성된 포트를 찾아 처리
    foreach (var port in InputPorts)
    {
        if (port is InputPort<int> intPort)
        {
            sum += intPort.GetValueOrDefault(0);
        }
    }
    
    // 결과 설정
    if (Result != null)
        Result.Value = sum;
    
    // 비동기 작업 대기 (예시)
    await Task.CompletedTask;
    
    // 다음 노드 실행
    if (FlowOut != null)
        yield return FlowOut;
}
```

## 6단계: 포트 정보 저장 및 복원

`DynamicNode`는 동적 포트 및 프로퍼티 정보를 자동으로 저장하고 복원합니다. 특수한 직렬화 요구사항이 있는 경우 `WriteJson`과 `ReadJson` 메서드를 오버라이드할 수 있습니다:

```csharp
public override void WriteJson(Utf8JsonWriter writer)
{
    // 기본 JSON 직렬화 수행 (필수)
    base.WriteJson(writer);
    
    // 추가 데이터 저장 (필요한 경우)
    writer.WritePropertyName("CustomData");
    writer.WriteStartObject();
    writer.WriteNumber("LastComputedResult", _lastComputedResult);
    writer.WriteEndObject();
}

public override void ReadJson(JsonElement element, JsonSerializerOptions options)
{
    // 기본 JSON 역직렬화 수행 (필수)
    base.ReadJson(element, options);
    
    // 추가 데이터 복원 (필요한 경우)
    if (element.TryGetProperty("CustomData", out var customData))
    {
        if (customData.TryGetProperty("LastComputedResult", out var lastResult))
        {
            _lastComputedResult = lastResult.GetInt32();
        }
    }
}
```

## 완성된 동적 노드 예제

다음은 입력 개수를 동적으로 변경할 수 있는 덧셈 노드의 완전한 구현 예제입니다:

```csharp
using System;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WPFNode.Examples;

[NodeName("동적 덧셈")]
[NodeCategory("수학")]
[NodeDescription("여러 숫자를 더합니다. 입력 개수를 동적으로 변경할 수 있습니다.")]
public class DynamicAdditionNode : DynamicNode
{
    [NodeProperty("입력 개수", OnValueChanged = nameof(ReconfigurePorts))]
    public NodeProperty<int> InputCount { get; set; }
    
    [NodeOutput("결과")]
    public OutputPort<double> Result { get; set; }
    
    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }
    
    [NodeFlowOut("완료")]
    public FlowOutPort FlowOut { get; set; }
    
    public DynamicAdditionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        InputCount.Value = 2; // 기본값 설정
    }
    
    protected override void Configure(NodeBuilder builder)
    {
        // 입력 개수 제한 (음수 방지)
        int count = Math.Max(0, InputCount.Value);
        
        // 동적으로 입력 포트 생성
        for (int i = 0; i < count; i++)
        {
            builder.Input<double>($"입력 {i + 1}");
        }
    }
    
    private void ReconfigurePorts()
    {
        // 입력 개수 값이 변경되었을 때 포트 재구성
        base.ReconfigurePorts();
    }
    
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 모든 입력 포트의 값을 합산
        double sum = 0;
        
        foreach (var port in InputPorts)
        {
            if (port is InputPort<double> doublePort)
            {
                sum += doublePort.GetValueOrDefault(0);
            }
        }
        
        // 결과 설정
        if (Result != null)
            Result.Value = sum;
        
        // 비동기 작업 대기 (필요한 경우)
        await Task.CompletedTask;
        
        // 다음 노드 실행
        if (FlowOut != null)
            yield return FlowOut;
    }
}
```

## 더 복잡한 동적 노드 예제: 필터 노드

다음은 선택된 필터 유형에 따라 다른 프로퍼티와 포트를 보여주는 필터 노드의 예제입니다:

```csharp
using System;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WPFNode.Examples;

public enum FilterType
{
    Range,
    Threshold,
    Pattern
}

[NodeName("동적 필터")]
[NodeCategory("데이터 처리")]
[NodeDescription("다양한 조건으로 데이터를 필터링합니다. 필터 유형에 따라 설정이 변경됩니다.")]
public class DynamicFilterNode : DynamicNode
{
    [NodeProperty("필터 유형", OnValueChanged = nameof(ReconfigurePorts))]
    public NodeProperty<FilterType> FilterTypeProperty { get; set; }
    
    [NodeInput("입력 데이터")]
    public InputPort<double> InputData { get; set; }
    
    [NodeOutput("필터링된 데이터")]
    public OutputPort<double> FilteredData { get; set; }
    
    [NodeOutput("필터 통과 여부")]
    public OutputPort<bool> Passed { get; set; }
    
    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }
    
    [NodeFlowOut("통과")]
    public FlowOutPort FlowPassed { get; set; }
    
    [NodeFlowOut("실패")]
    public FlowOutPort FlowFailed { get; set; }
    
    // 동적 프로퍼티를 저장할 필드
    private NodeProperty<double> _minValue;
    private NodeProperty<double> _maxValue;
    private NodeProperty<double> _threshold;
    private NodeProperty<string> _pattern;
    
    public DynamicFilterNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        FilterTypeProperty.Value = FilterType.Threshold; // 기본값 설정
    }
    
    protected override void Configure(NodeBuilder builder)
    {
        // 현재 필터 유형에 따라 다른 프로퍼티 구성
        switch (FilterTypeProperty.Value)
        {
            case FilterType.Range:
                _minValue = builder.Property<double>("MinValue", "최소값", null, true);
                _maxValue = builder.Property<double>("MaxValue", "최대값", null, true);
                break;
                
            case FilterType.Threshold:
                _threshold = builder.Property<double>("Threshold", "임계값", null, true);
                break;
                
            case FilterType.Pattern:
                _pattern = builder.Property<string>("Pattern", "패턴", null, true);
                break;
        }
    }
    
    private void ReconfigurePorts()
    {
        // 필터 유형이 변경되었을 때 포트 재구성
        base.ReconfigurePorts();
    }
    
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 입력 데이터 가져오기
        double input = InputData?.GetValueOrDefault(0) ?? 0;
        bool passes = false;
        
        // 필터 유형에 따라 다른 필터링 로직 적용
        switch (FilterTypeProperty.Value)
        {
            case FilterType.Range:
                double min = _minValue?.Value ?? 0;
                double max = _maxValue?.Value ?? 0;
                passes = (input >= min && input <= max);
                break;
                
            case FilterType.Threshold:
                double threshold = _threshold?.Value ?? 0;
                passes = (input >= threshold);
                break;
                
            case FilterType.Pattern:
                string pattern = _pattern?.Value ?? "";
                string inputStr = input.ToString();
                passes = !string.IsNullOrEmpty(pattern) && inputStr.Contains(pattern);
                break;
        }
        
        // 결과 설정
        if (FilteredData != null)
            FilteredData.Value = passes ? input : 0;
            
        if (Passed != null)
            Passed.Value = passes;
        
        // 비동기 작업 대기 (필요한 경우)
        await Task.CompletedTask;
        
        // 결과에 따라 다른 흐름 포트 반환
        if (passes)
        {
            if (FlowPassed != null)
                yield return FlowPassed;
        }
        else
        {
            if (FlowFailed != null)
                yield return FlowFailed;
        }
    }
}
```

## 모범 사례

동적 노드를 개발할 때 다음 모범 사례를 따르는 것이 좋습니다:

1. **포트 이름의 일관성**: 동적으로 생성되는 포트의 이름을 명확하고 일관되게 지정합니다.
2. **합리적인 기본값**: 모든 프로퍼티에 적절한 기본값을 제공합니다.
3. **최소/최대 제한**: 입력 개수와 같은 값에 적절한 최소/최대 제한을 적용합니다.
4. **효율적인 재구성**: 불필요한 재구성을 피하고, 값이 실제로 변경되었을 때만 재구성합니다.
5. **직렬화 고려**: 동적 포트와 프로퍼티가 올바르게 직렬화되고 역직렬화되는지 확인합니다.
6. **메모리 관리**: 더 이상 필요하지 않은 포트는 명시적으로 제거하여 메모리 누수를 방지합니다.

## 다음 단계

이제 기본 노드와 동적 노드를 만드는 방법을 알았습니다. 다음 장에서는 더 고급 기능과 최적화 기법에 대해 알아보겠습니다.
