# AI를 이용한 노드 생성 프롬프트

이 문서에는 AI를 통해 WPFNode 커스텀 노드를 생성하기 위한 효과적인 프롬프트 템플릿을 제공합니다.

## 기본 노드 생성 프롬프트

아래 템플릿을 사용하여 AI에게 새로운 기본 노드 생성을 요청할 수 있습니다:

```
# WPFNode 커스텀 노드 생성 요청

다음 요구사항에 맞는 WPFNode 노드 클래스를 C#으로 작성해주세요:

## 요구사항
- 노드 이름: [여기에 노드 이름 입력]
- 카테고리: [여기에 노드 카테고리 입력]
- 기능 설명: [여기에 노드 기능 설명 입력]

## 포트 정의
- 입력 포트:
  * [포트 이름]: [데이터 타입] - [설명]
  * ...
- 출력 포트:
  * [포트 이름]: [데이터 타입] - [설명]
  * ...
- 흐름 포트: [필요하면 포함, 아니면 제외]

## 노드 프로퍼티
- [프로퍼티 이름]: [데이터 타입] - [설명] (기본값: [값])
- ...

## 구현 세부사항
[여기에 노드의 기능 구현 방식에 대한 상세 내용 포함]
```

## 예시: 텍스트 변환 노드 생성 요청

아래는 실제 프롬프트 사용 예시입니다:

```
# WPFNode 커스텀 노드 생성 요청

다음 요구사항에 맞는 WPFNode 노드 클래스를 C#으로 작성해주세요:

## 요구사항
- 노드 이름: TextTransformer
- 카테고리: 문자열
- 기능 설명: 입력된 텍스트를 다양한 방식으로 변환합니다.

## 포트 정의
- 입력 포트:
  * InputText: string - 변환할 텍스트
- 출력 포트:
  * TransformedText: string - 변환된 텍스트
- 흐름 포트: 입력 포트 "실행", 출력 포트 "완료"

## 노드 프로퍼티
- TransformationType: enum(ToUpper, ToLower, Reverse, RemoveSpaces) - 변환 타입 (기본값: ToUpper)
- AddPrefix: string - 접두사 추가 (기본값: "")
- AddSuffix: string - 접미사 추가 (기본값: "")

## 구현 세부사항
입력된 텍스트를 TransformationType에 따라 변환한 후, 지정된 접두사와 접미사를 추가합니다.
```

이 프롬프트에 대한 AI의 구현 결과 예시:

```csharp
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace WPFNode.Plugins.String;

public enum TextTransformType
{
    ToUpper,
    ToLower,
    Reverse,
    RemoveSpaces
}

[NodeName("TextTransformer")]
[NodeCategory("문자열")]
[NodeDescription("입력된 텍스트를 다양한 방식으로 변환합니다.")]
public class TextTransformerNode : NodeBase
{
    [NodeInput("입력 텍스트")]
    public InputPort<string> InputText { get; set; }
    
    [NodeOutput("변환된 텍스트")]
    public OutputPort<string> TransformedText { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("완료")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("변환 타입", CanConnectToPort = true)]
    public NodeProperty<TextTransformType> TransformationType { get; set; }
    
    [NodeProperty("접두사 추가", CanConnectToPort = true)]
    public NodeProperty<string> AddPrefix { get; set; }
    
    [NodeProperty("접미사 추가", CanConnectToPort = true)]
    public NodeProperty<string> AddSuffix { get; set; }

    public TextTransformerNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        TransformationType.Value = TextTransformType.ToUpper;
        AddPrefix.Value = string.Empty;
        AddSuffix.Value = string.Empty;
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string text = InputText?.GetValueOrDefault(string.Empty) ?? string.Empty;
        string transformed = TransformText(text);
        
        // 접두사와 접미사 추가
        transformed = AddPrefix.Value + transformed + AddSuffix.Value;
        
        if (TransformedText != null)
            TransformedText.Value = transformed;
        
        await Task.CompletedTask;
        
        if (FlowOut != null)
        {
            yield return FlowOut;
        }
    }
    
    private string TransformText(string text)
    {
        switch (TransformationType.Value)
        {
            case TextTransformType.ToUpper:
                return text.ToUpper();
            case TextTransformType.ToLower:
                return text.ToLower();
            case TextTransformType.Reverse:
                return new string(text.Reverse().ToArray());
            case TextTransformType.RemoveSpaces:
                return text.Replace(" ", "");
            default:
                return text;
        }
    }
}
```

## 동적 노드 생성 프롬프트

동적 노드를 생성하기 위한 프롬프트 템플릿:

```
# WPFNode 동적 노드 생성 요청

다음 요구사항에 맞는 WPFNode의 동적 노드 클래스를 C#으로 작성해주세요:

## 요구사항
- 노드 이름: [여기에 노드 이름 입력]
- 카테고리: [여기에 노드 카테고리 입력]
- 기능 설명: [여기에 노드 기능 설명 입력]

## 고정 포트 정의
- 입력 포트:
  * [포트 이름]: [데이터 타입] - [설명]
  * ...
- 출력 포트:
  * [포트 이름]: [데이터 타입] - [설명]
  * ...
- 흐름 포트: [필요하면 포함, 아니면 제외]

## 동적 포트 구성
- 동적 포트 타입: [입력/출력/둘 다]
- 동적 포트 데이터 타입: [데이터 타입]
- 동적 포트 수를 결정하는 프로퍼티: [프로퍼티 설명]

## 노드 프로퍼티
- [프로퍼티 이름]: [데이터 타입] - [설명] (기본값: [값])
- ...

## 구현 세부사항
[여기에 노드의 기능 구현 방식에 대한 상세 내용 포함]
- Configure 메서드 구현 방법
- ProcessAsync 메서드에서 동적 포트 처리 방법
- 기타 중요한 구현 세부사항
```

## 예시: 동적 집계 노드 생성 요청

아래는 실제 프롬프트 사용 예시입니다:

```
# WPFNode 동적 노드 생성 요청

다음 요구사항에 맞는 WPFNode의 동적 노드 클래스를 C#으로 작성해주세요:

## 요구사항
- 노드 이름: DynamicAggregator
- 카테고리: 수학
- 기능 설명: 가변적인 개수의 숫자 입력을 받아 다양한 방식으로 집계합니다.

## 고정 포트 정의
- 출력 포트:
  * Result: double - 집계 결과
- 흐름 포트: 입력 포트 "실행", 출력 포트 "완료"

## 동적 포트 구성
- 동적 포트 타입: 입력
- 동적 포트 데이터 타입: double
- 동적 포트 수를 결정하는 프로퍼티: InputCount (입력 개수)

## 노드 프로퍼티
- InputCount: int - 입력 포트 개수 (기본값: 2)
- AggregationType: enum(Sum, Average, Max, Min, Product) - 집계 방식 (기본값: Sum)
- RoundResult: bool - 결과 반올림 여부 (기본값: false)
- DecimalPlaces: int - 소수점 자리수 (기본값: 2)

## 구현 세부사항
- Configure 메서드에서 InputCount 값에 따라 동적으로 입력 포트를 생성합니다.
- ProcessAsync 메서드에서는 선택된 AggregationType에 따라 입력 값들을 집계합니다.
- RoundResult가 true인 경우, 결과를 DecimalPlaces 자리수까지 반올림합니다.
```

## AI 노드 개발 지침

AI에게 더 나은 코드를 생성하도록 안내하기 위한 추가 지침:

1. **명확한 요구사항 제공**: 노드의 목적과 기능을 명확하게 설명합니다.
2. **구체적인 입출력 명시**: 모든 포트의 이름, 타입, 설명을 명확하게 지정합니다.
3. **예상 동작 설명**: 노드가 실행될 때의 예상 동작과 특별한 처리 로직을 설명합니다.
4. **에지 케이스 고려**: 입력 값이 null이거나 잘못된 경우의 처리 방법을 명시합니다.
5. **성능 고려사항**: 대용량 데이터 처리나 비동기 작업 등 성능 관련 요구사항을 포함합니다.

## 주요 요소별 세부 프롬프트

### 데이터 변환 노드

```
# 데이터 변환 노드 생성

다음 요구사항에 맞는 데이터 변환 노드를 작성해주세요:

- 입력 데이터 타입: [입력 타입]
- 출력 데이터 타입: [출력 타입]
- 변환 방식: [변환 설명]
- 특별 처리: [예외 처리 또는 특별 케이스 설명]
```

### 로직 노드

```
# 로직 노드 생성

다음 요구사항에 맞는 로직 처리 노드를 작성해주세요:

- 입력 조건: [조건 설명]
- 분기 방식: [True/False 또는 다중 분기 설명]
- 출력 포트: [각 분기별 출력 포트 설명]
```

### 데이터 필터 노드

```
# 데이터 필터 노드 생성

다음 요구사항에 맞는 데이터 필터 노드를 작성해주세요:

- 필터링 대상: [데이터 타입 및 구조]
- 필터 조건: [필터링 조건 설명]
- 출력 형식: [필터링 결과 출력 형식]
```

## 테스트 코드 생성 프롬프트

노드의 테스트 코드를 함께 생성하려면 다음 프롬프트를 추가할 수 있습니다:

```
해당 노드에 대한 단위 테스트도 작성해주세요. 다음 시나리오를 테스트해야 합니다:

1. 기본 입력에 대한 예상 출력 검증
2. 경계값 테스트
3. null 또는 잘못된 입력 처리
4. 특별 케이스 처리
```

## 문제 해결 팁

노드 개발 중 발생할 수 있는 일반적인 문제와 해결 방법:

1. **포트 연결 문제**: 입력/출력 포트의 데이터 타입이 호환되는지 확인합니다.
2. **직렬화 문제**: 커스텀 타입을 사용할 경우 적절한 직렬화 처리가 필요합니다.
3. **성능 문제**: 무거운 작업은 비동기로 처리하고 취소 토큰을 적절히 사용합니다.
4. **메모리 누수**: 동적 노드에서 더 이상 필요없는 포트는 명시적으로 제거합니다.
5. **UI 업데이트**: 프로퍼티 변경 시 적절한 이벤트를 발생시켜 UI 업데이트를 보장합니다.
