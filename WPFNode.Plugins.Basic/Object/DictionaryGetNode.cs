using System;
using System.Collections; // Added for IDictionary
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models; // Assuming GenericInputPort is here
using WPFNode.Models.Execution;
// using WPFNode.Models.Properties; // NodeProperty<Type> is removed
using WPFNode.Utilities;            // Keep this if other utilities are used
using WPFNode.Extensions;           // Keep this for GetDictionaryKeyValueTypes
using Microsoft.Extensions.Logging; // Added for logging

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Dictionary.Get")]
[NodeDescription("Dictionary에서 키에 해당하는 값을 가져옵니다.")]
[NodeCategory("컬렉션")]
public class DictionaryGetNode : NodeBase {
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; private set; }

    [NodeFlowOut("성공")]
    public IFlowOutPort SuccessOut { get; private set; }

    [NodeFlowOut("실패")]
    public IFlowOutPort FailureOut { get; private set; }

    // KeyType, ValueType properties are removed

    // DictionaryInput: GenericInputPort 타입으로 선언.
    [NodeInput("Dictionary", ConnectionStateChangedCallback = nameof(DictionaryInput_ConnectionChanged))]
    public GenericInputPort DictionaryInput { get; private set; }

    // KeyInput, ValueOutput: Configure에서 동적으로 관리됨.
    private IInputPort _keyInput;

    private IOutputPort _valueOutput;

    // KeyExistsOutput: bool 타입으로 고정.
    [NodeOutput("Contains")]
    public OutputPort<bool> KeyExistsOutput { get; private set; }

    public DictionaryGetNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Name        = "Dictionary.Get";
        Description = "Dictionary에서 키에 해당하는 값을 가져옵니다. Dictionary 입력 타입에 따라 키/값 타입을 자동으로 결정합니다.";
    }

    // DictionaryInput의 연결 상태 또는 타입이 변경될 때 호출될 콜백 메서드
    private void DictionaryInput_ConnectionChanged(IInputPort port) {
        Logger?.LogDebug($"DictionaryInput 포트 상태 변경됨 (연결 또는 타입).");
        ReconfigurePorts(); // KeyInput, ValueOutput 재구성을 위해 호출
    }

    // Configure 메서드는 동적으로 관리해야 하는 포트만 정의
    protected override void Configure(NodeBuilder builder) {
        // DictionaryInput, KeyExistsOutput 등 Attribute로 정의된 포트는 builder로 다시 정의하지 않음.

        // KeyInput과 ValueOutput의 타입을 결정
        Type keyType   = typeof(object); // 기본값
        Type valueType = typeof(object); // 기본값

        // DictionaryInput의 현재 결정된 타입을 확인하고 Key/Value 타입 추출
        if (DictionaryInput != null && DictionaryInput.CurrentResolvedType != null && DictionaryInput.CurrentResolvedType != typeof(object)) {
            var dictType = DictionaryInput.CurrentResolvedType;
            // 유틸리티 함수를 사용하여 Key/Value 타입 추출 시도 (using WPFNode.Extensions; 필요)
            (keyType, valueType) = dictType.GetDictionaryKeyValueTypes() ?? (typeof(object), typeof(object));
            Logger?.LogDebug($"DictionaryInput 타입({dictType.Name}) 기반. KeyType: {keyType.Name}, ValueType: {valueType.Name} 사용.");
        }
        else {
            Logger?.LogDebug("DictionaryInput 타입 불명확. KeyType: object, ValueType: object 사용.");
        }

        // NodeBuilder를 사용하여 KeyInput과 ValueOutput을 동적으로 정의/재정의
        _keyInput    = builder.Input("Key", keyType);
        _valueOutput = builder.Output("Value", valueType);
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken  cancellationToken = default
    ) {
        var     dictionaryValue = DictionaryInput.Value;
        var     key             = _keyInput.Value;
        bool    keyExists       = false;
        object? foundValue      = null;

        if (dictionaryValue is IDictionary dictionary && key != null) // IDictionary 인터페이스 및 non-null key로 작업
        {
            try {
                keyExists = dictionary.Contains(key);
                if (keyExists) {
                    foundValue = dictionary[key]; // 값 가져오기
                    Logger?.LogDebug($"Dictionary에서 키 '{key}'에 해당하는 값 '{foundValue}'을(를) 찾았습니다.");
                }
                else {
                    Logger?.LogDebug($"Dictionary에 키 '{key}'이(가) 존재하지 않습니다.");
                }
            }
            catch (Exception ex) // Contains 또는 인덱서 접근 시 예외 발생 가능성
            {
                Logger?.LogError(ex, $"Dictionary Get 작업 중 오류 발생: {ex.Message}");
                keyExists  = false;
                foundValue = null;
            }
        }
        else {
            if (dictionaryValue == null) Logger?.LogWarning("DictionaryInput 값이 null입니다.");
            if (key == null) Logger?.LogWarning("KeyInput 값이 null입니다.");
            if (dictionaryValue != null && !(dictionaryValue is IDictionary)) Logger?.LogError("DictionaryInput 값이 IDictionary가 아닙니다.");
            keyExists  = false;
            foundValue = null;
        }

        // 결과 Output 설정
        KeyExistsOutput.Value = keyExists; // KeyExistsOutput은 OutputPort<bool> 타입이므로 .Value 사용
        _valueOutput.Value = foundValue;

        // 결과에 따라 적절한 Flow 출력
        if (keyExists)
            yield return SuccessOut;
        else
            yield return FailureOut;
    }
}