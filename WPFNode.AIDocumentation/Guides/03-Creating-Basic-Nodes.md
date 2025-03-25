# 기본 노드 만들기

이 가이드에서는 WPFNode 라이브러리를 사용하여 기본 노드를 생성하는 방법을 단계별로 설명합니다.

## 기본 노드의 구조

WPFNode에서 모든 노드는 `NodeBase` 클래스를 상속받아 구현됩니다. 노드는 다음과 같은 기본 구성 요소로 이루어집니다:

1. 어트리뷰트: 노드의 메타데이터 (이름, 카테고리, 설명 등)
2. 포트: 입력/출력 데이터 포트 및 흐름 포트
3. 프로퍼티: 노드의 설정값
4. 실행 로직: 노드가 실행될 때 수행할 작업

## 1단계: 노드 클래스 생성

먼저, `NodeBase`를 상속받는 새로운 클래스를 생성합니다:

```csharp
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace YourNamespace;

public class MyFirstNode : NodeBase
{
    public MyFirstNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) { }
    
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 노드 실행 로직 구현
        yield break;
    }
}
```

## 2단계: 노드 메타데이터 정의

어트리뷰트를 사용하여 노드의 이름, 카테고리, 설명 등의 메타데이터를 정의합니다:

```csharp
[NodeName("내 첫 번째 노드")]          // 노드의 표시 이름
[NodeCategory("예제")]                 // 노드의 카테고리
[NodeDescription("예제 노드입니다")]    // 노드의 설명
public class MyFirstNode : NodeBase
{
    // ...
}
```

## 3단계: 포트 정의

노드는 다양한 유형의 포트를 가질 수 있습니다. 어트리뷰트를 사용하여 포트를 정의합니다:

```csharp
// 데이터 입력 포트
[NodeInput("숫자 입력")]
public InputPort<int> NumberInput { get; set; }

// 데이터 출력 포트
[NodeOutput("결과")]
public OutputPort<int> Result { get; set; }

// 실행 흐름 입력 포트
[NodeFlowIn("실행")]
public FlowInPort FlowIn { get; set; }

// 실행 흐름 출력 포트
[NodeFlowOut("완료")]
public FlowOutPort FlowOut { get; set; }
```

포트의 제네릭 타입 파라미터는 해당 포트에서 처리할 데이터 타입을 나타냅니다.

## 4단계: 노드 프로퍼티 정의

노드 프로퍼티는 사용자가 UI를 통해 설정할 수 있는 값입니다:

```csharp
// 기본 프로퍼티
[NodeProperty("곱할 값")]
public NodeProperty<int> Multiplier { get; set; }

// 포트로 연결 가능한 프로퍼티
[NodeProperty("추가할 값", CanConnectToPort = true)]
public NodeProperty<int> AddValue { get; set; }

// 값 변경 시 콜백 함수를 호출하는 프로퍼티
[NodeProperty("최대값", OnValueChanged = nameof(OnMaxValueChanged))]
public NodeProperty<int> MaxValue { get; set; }

// 값 변경 이벤트 핸들러
private void OnMaxValueChanged()
{
    // 프로퍼티 값이 변경되었을 때 실행할 코드
}
```

`CanConnectToPort = true` 옵션을 설정하면 해당 프로퍼티는 입력 포트처럼 작동하여 다른 노드의 출력과 연결할 수 있습니다.

## 5단계: 생성자에서 초기값 설정

노드의 생성자에서 프로퍼티의 초기값을 설정할 수 있습니다:

```csharp
public MyFirstNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
{
    // 프로퍼티 초기값 설정
    Multiplier.Value = 2;
    AddValue.Value = 0;
    MaxValue.Value = 100;
}
```

## 6단계: 노드 실행 로직 구현

노드의 실제 동작은 `ProcessAsync` 메서드에서 구현합니다. 이 메서드는 노드가 실행될 때 호출됩니다:

```csharp
protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // 1. 입력 포트에서 데이터 가져오기
    int inputValue = NumberInput?.GetValueOrDefault(0) ?? 0;
    
    // 2. 데이터 처리
    int result = inputValue * Multiplier.Value + AddValue.Value;
    
    // 3. 최대값 제한 적용
    result = Math.Min(result, MaxValue.Value);
    
    // 4. 결과를 출력 포트에 설정
    if (Result != null)
        Result.Value = result;
    
    // 5. 비동기 작업 수행 (필요한 경우)
    await Task.CompletedTask;
    
    // 6. 다음 실행 노드 지정 (FlowOut 포트 반환)
    if (FlowOut != null)
        yield return FlowOut;
}
```

`ProcessAsync` 메서드는 `IAsyncEnumerable<IFlowOutPort>`을 반환합니다. 이는 노드가 여러 개의 FlowOut 포트를 순차적으로 활성화할 수 있음을 의미합니다. 각 FlowOut 포트는 `yield return` 문을 통해 반환됩니다.

## 완성된 노드 예제

다음은 두 숫자를 더하는 간단한 노드의 완전한 구현 예제입니다:

```csharp
using System;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WPFNode.Examples;

[NodeName("덧셈")]
[NodeCategory("수학")]
[NodeDescription("두 숫자를 더합니다.")]
public class AdditionNode : NodeBase
{
    [NodeInput("A")]
    public InputPort<double> InputA { get; set; }
    
    [NodeInput("B")]
    public InputPort<double> InputB { get; set; }
    
    [NodeOutput("결과")]
    public OutputPort<double> Result { get; set; }
    
    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }
    
    [NodeFlowOut("완료")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("반올림", CanConnectToPort = false)]
    public NodeProperty<bool> ShouldRound { get; set; }
    
    [NodeProperty("소수점 자릿수", CanConnectToPort = true)]
    public NodeProperty<int> DecimalPlaces { get; set; }
    
    public AdditionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        ShouldRound.Value = false;
        DecimalPlaces.Value = 2;
    }
    
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 입력값 가져오기
        double a = InputA?.GetValueOrDefault(0) ?? 0;
        double b = InputB?.GetValueOrDefault(0) ?? 0;
        
        // 덧셈 수행
        double sum = a + b;
        
        // 반올림 적용 (설정된 경우)
        if (ShouldRound.Value)
        {
            sum = Math.Round(sum, DecimalPlaces.Value);
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
}
```

## 노드 등록

새로 만든 노드를 WPFNode 시스템에서 사용하려면 해당 노드를 플러그인에 등록해야 합니다. 자세한 내용은 플러그인 관련 문서를 참조하세요.

## 모범 사례

노드를 만들 때 다음 모범 사례를 참고하세요:

1. **명확한 이름과 설명**: 노드의 이름과 설명은 해당 노드의 기능을 명확하게 나타내야 합니다.
2. **적절한 카테고리**: 관련 노드들을 같은 카테고리로 그룹화하여 찾기 쉽게 합니다.
3. **타입 안전성**: 포트의 데이터 타입을 명확하게 지정하여 타입 안전성을 보장합니다.
4. **기본값 제공**: 모든 프로퍼티에 적절한 기본값을 설정합니다.
5. **에러 처리**: 입력 데이터가 없거나 잘못된 경우를 적절히 처리합니다.
6. **비동기 작업 최적화**: 무거운 작업은 비동기적으로 처리하고, 취소 토큰을 적절히 사용합니다.

## 다음 단계

이제 기본 노드를 만드는 방법을 알았으니, 다음 장에서는 동적으로 포트를 변경할 수 있는 동적 노드(DynamicNode)를 만드는 방법에 대해 알아보겠습니다.
