# WPFNode 빠른 참조 치트시트

이 문서는 WPFNode 라이브러리를 사용하여 노드를 개발할 때 필요한 핵심 정보를 빠르게 참조할 수 있도록 정리한 치트시트입니다.

## 주요 어트리뷰트

```csharp
// 노드 메타데이터 설정
[NodeName("노드 이름")]              // 노드의 표시 이름 (필수)
[NodeCategory("카테고리")]           // 노드의 카테고리 (필수)
[NodeDescription("노드 설명")]       // 노드의 설명 (선택)
[NodeStyle("스타일 키")]            // 노드의 시각적 스타일 (선택)

// 포트 및 프로퍼티 정의
[NodeInput("입력 포트 이름")]        // 데이터 입력 포트
[NodeOutput("출력 포트 이름")]       // 데이터 출력 포트
[NodeFlowIn("흐름 입력 포트 이름")]  // 실행 흐름 입력 포트
[NodeFlowOut("흐름 출력 포트 이름")] // 실행 흐름 출력 포트
[NodeProperty("프로퍼티 이름", CanConnectToPort = true/false, OnValueChanged = "메서드명", Format = "형식")]  // 노드 프로퍼티
```

## 기본 노드 템플릿

```csharp
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace YourNamespace;

[NodeName("노드 이름")]
[NodeCategory("카테고리")]
[NodeDescription("노드 설명")]
public class YourNodeName : NodeBase
{
    // 데이터 입력 포트
    [NodeInput("입력 이름")]
    public InputPort<데이터타입> InputName { get; set; }
    
    // 데이터 출력 포트
    [NodeOutput("출력 이름")]
    public OutputPort<데이터타입> OutputName { get; set; }
    
    // 흐름 입력 포트
    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }
    
    // 흐름 출력 포트
    [NodeFlowOut("완료")]
    public FlowOutPort FlowOut { get; set; }
    
    // 노드 프로퍼티
    [NodeProperty("프로퍼티 이름", CanConnectToPort = true)]
    public NodeProperty<데이터타입> PropertyName { get; set; }
    
    // 생성자
    public YourNodeName(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        // 프로퍼티 초기값 설정
        PropertyName.Value = 기본값;
    }
    
    // 노드 실행 로직
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 입력 포트에서 데이터 가져오기
        var input = InputName?.GetValueOrDefault(기본값) ?? 기본값;
        
        // 데이터 처리 로직
        var result = 처리_로직(input);
        
        // 출력 포트에 결과 설정
        OutputName.Value = result;
        
        // 비동기 작업 대기 (필요한 경우)
        await Task.CompletedTask;
        
        // 다음 노드로 실행 흐름 전달
        if (FlowOut != null)
            yield return FlowOut;
    }
}
```

## 동적 노드 템플릿

```csharp
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace YourNamespace;

[NodeName("동적 노드 이름")]
[NodeCategory("카테고리")]
[NodeDescription("노드 설명")]
public class YourDynamicNodeName : DynamicNode
{
    // 포트 구성을 결정하는 프로퍼티
    [NodeProperty("구성 프로퍼티", OnValueChanged = nameof(ReconfigurePorts))]
    public NodeProperty<데이터타입> ConfigProperty { get; set; }
    
    // 고정 포트 (선택사항)
    [NodeOutput("결과")]
    public OutputPort<데이터타입> Result { get; set; }
    
    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }
    
    [NodeFlowOut("완료")]
    public FlowOutPort FlowOut { get; set; }
    
    // 생성자
    public YourDynamicNodeName(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        ConfigProperty.Value = 기본값;
    }
    
    // 포트 구성 메서드
    protected override void Configure(NodeBuilder builder)
    {
        // ConfigProperty의 값에 따라 포트 구성
        for (int i = 0; i < ConfigProperty.Value; i++)
        {
            builder.Input<데이터타입>($"입력 {i + 1}");
        }
        
        // 추가 프로퍼티 생성 (필요한 경우)
        builder.Property<데이터타입>("동적프로퍼티", "표시이름", null, true);
    }
    
    // 포트 재구성 메서드
    private void ReconfigurePorts()
    {
        base.ReconfigurePorts();
    }
    
    // 노드 실행 로직
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 동적 포트 처리
        foreach (var port in InputPorts)
        {
            if (port is InputPort<데이터타입> typedPort)
            {
                // 각 포트 처리
            }
        }
        
        // 결과 설정
        Result.Value = 계산결과;
        
        // 비동기 작업 대기 (필요한 경우)
        await Task.CompletedTask;
        
        // 다음 노드로 실행 흐름 전달
        if (FlowOut != null)
            yield return FlowOut;
    }
}
```

## 자주 사용되는 패턴

### 입력 포트 값 가져오기

```csharp
// null 처리 포함
int value = InputPort?.GetValueOrDefault(0) ?? 0;

// 연결되지 않은 경우 확인
if (InputPort?.IsConnected == true) {
    var value = InputPort.GetValueOrDefault(defaultValue);
    // ...
}
```

### 조건부 흐름 포트 반환

```csharp
// 조건에 따라 다른 흐름 포트 반환
if (condition) {
    if (TrueFlowOut != null)
        yield return TrueFlowOut;
} else {
    if (FalseFlowOut != null)
        yield return FalseFlowOut;
}

// 여러 흐름 포트 순차적 반환
yield return FirstFlowOut;
await Task.Delay(1000); // 일부 작업 수행
yield return SecondFlowOut;
```

### 프로퍼티 값 변경 감지

```csharp
[NodeProperty("프로퍼티", OnValueChanged = nameof(OnPropertyChanged))]
public NodeProperty<int> MyProperty { get; set; }

private void OnPropertyChanged()
{
    // 프로퍼티 값이 변경되었을 때 수행할 작업
    // DynamicNode의 경우 ReconfigurePorts() 호출 등
}
```

### 비동기 작업 처리

```csharp
// 간단한 비동기 작업
await Task.CompletedTask;

// 실제 비동기 작업
await Task.Delay(1000, cancellationToken);

// 취소 가능한 비동기 작업
try {
    await LongRunningOperation(cancellationToken);
} catch (OperationCanceledException) {
    // 취소 처리
}
```

## 주요 클래스 및 인터페이스

| 클래스/인터페이스 | 설명 |
|------------------|------|
| `NodeBase` | 모든 노드의 기본 클래스 |
| `DynamicNode` | 동적 포트 구성을 지원하는 노드 클래스 |
| `INodeCanvas` | 노드 캔버스 인터페이스 |
| `InputPort<T>` | 제네릭 타입의 입력 포트 |
| `OutputPort<T>` | 제네릭 타입의 출력 포트 |
| `FlowInPort` | 실행 흐름 입력 포트 |
| `FlowOutPort` | 실행 흐름 출력 포트 |
| `NodeProperty<T>` | 제네릭 타입의 노드 프로퍼티 |

## 자주 사용되는 타입 변환

```csharp
// 문자열 → 숫자
int intValue = int.TryParse(stringValue, out var result) ? result : 0;
double doubleValue = double.TryParse(stringValue, out var result) ? result : 0.0;

// 숫자 → 문자열
string formattedValue = numericValue.ToString("F2"); // 소수점 2자리까지

// 객체 → 문자열
string stringValue = objectValue?.ToString() ?? "";

// 형변환 안전하게 처리
if (value is TargetType targetValue) {
    // targetValue 사용
} else {
    // 변환 실패 처리
}
```

## 주요 예외 처리 패턴

```csharp
try {
    // 위험한 작업
} catch (Exception ex) {
    // 로깅
    Logger?.LogError(ex, "오류 메시지");
    
    // 출력 포트에 기본값 설정
    OutputPort.Value = defaultValue;
    
    // 오류 플래그 설정 (필요한 경우)
    ErrorFlag.Value = true;
}
```

## 직렬화 및 역직렬화

```csharp
// JSON 직렬화 커스터마이징
public override void WriteJson(Utf8JsonWriter writer)
{
    // 기본 직렬화 수행
    base.WriteJson(writer);
    
    // 추가 데이터 저장
    writer.WritePropertyName("CustomData");
    writer.WriteStartObject();
    writer.WriteNumber("Value", _customValue);
    writer.WriteEndObject();
}

// JSON 역직렬화 커스터마이징
public override void ReadJson(JsonElement element, JsonSerializerOptions options)
{
    // 기본 역직렬화 수행
    base.ReadJson(element, options);
    
    // 추가 데이터 복원
    if (element.TryGetProperty("CustomData", out var customData))
    {
        if (customData.TryGetProperty("Value", out var value))
        {
            _customValue = value.GetInt32();
        }
    }
}
