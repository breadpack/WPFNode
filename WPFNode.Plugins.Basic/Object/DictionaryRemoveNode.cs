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

[NodeName("Dictionary.Remove")]
[NodeDescription("Dictionary에서 지정된 키와 연결된 항목을 삭제합니다.")]
[NodeCategory("컬렉션")]
public class DictionaryRemoveNode : NodeBase {
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; private set; }

    [NodeFlowOut("Success")]
    public IFlowOutPort SuccessOut { get; private set; }

    [NodeFlowOut("Failure")]
    public IFlowOutPort FailureOut { get; private set; }

    // KeyType, ValueType properties are removed

    // DictionaryInput: GenericInputPort 타입으로 선언.
    [NodeInput("Dictionary", ConnectionStateChangedCallback = nameof(DictionaryInput_ConnectionChanged))]
    public GenericInputPort DictionaryInput { get; private set; }

    // KeyInput, UpdatedDictionaryOutput: Configure에서 동적으로 관리됨.
    private IInputPort _keyInput;

    private IOutputPort _updatedDictionaryOutput;

    // RemoveSuccessOutput: bool 타입으로 고정.
    [NodeOutput("RemoveSuccess")]
    public OutputPort<bool> RemoveSuccessOutput { get; private set; }

    public DictionaryRemoveNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Name        = "Dictionary.Remove";
        Description = "Dictionary에서 지정된 키와 연결된 항목을 삭제합니다. Dictionary 입력 타입에 따라 키 타입을 자동으로 결정합니다.";
    }

    // DictionaryInput의 연결 상태 또는 타입이 변경될 때 호출될 콜백 메서드
    private void DictionaryInput_ConnectionChanged(IInputPort port) {
        Logger?.LogDebug($"DictionaryInput 포트 상태 변경됨 (연결 또는 타입).");
        ReconfigurePorts(); // KeyInput, UpdatedDictionaryOutput 재구성을 위해 호출
    }

    // Configure 메서드는 동적으로 관리해야 하는 포트만 정의
    protected override void Configure(NodeBuilder builder) {
        // DictionaryInput, RemoveSuccessOutput 등 Attribute로 정의된 포트는 builder로 다시 정의하지 않음.

        // KeyInput과 UpdatedDictionaryOutput의 타입을 결정
        Type dictType = typeof(IDictionary); // 기본값
        Type keyType  = typeof(object);      // 기본값

        // DictionaryInput의 현재 결정된 타입을 확인하고 Key/Value 타입 추출
        if (DictionaryInput.CurrentResolvedType != null && DictionaryInput.CurrentResolvedType != typeof(object)) {
            dictType = DictionaryInput.CurrentResolvedType;
            // 유틸리티 함수를 사용하여 Key 타입 추출 시도 (using WPFNode.Extensions; 필요)
            (keyType, _) = dictType.GetDictionaryKeyValueTypes() ?? (typeof(object), typeof(object));
            Logger?.LogDebug($"DictionaryInput 타입({dictType.Name}) 기반. KeyType: {keyType.Name}, Output DictType: {dictType.Name} 사용.");
        }
        else {
            Logger?.LogDebug("DictionaryInput 타입 불명확. KeyType: object, Output DictType: IDictionary 사용.");
        }

        // NodeBuilder를 사용하여 KeyInput과 UpdatedDictionaryOutput을 동적으로 정의/재정의
        _keyInput                = builder.Input("Key", keyType);
        _updatedDictionaryOutput = builder.Output("Updated", dictType);
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken  cancellationToken = default
    ) {
        var          dictionaryValue = DictionaryInput.Value;
        var          key             = _keyInput.Value;
        bool         removeSuccess   = false;
        IDictionary? dictionary      = dictionaryValue as IDictionary; // 미리 캐스팅 및 초기화

        if (dictionary != null && key != null) // IDictionary 인터페이스 및 non-null key로 작업
        {
            try {
                if (dictionary.Contains(key)) {
                    dictionary.Remove(key);
                    removeSuccess = true;
                    Logger?.LogDebug($"Dictionary에서 키 '{key}'을(를) 제거했습니다.");
                }
                else {
                    Logger?.LogDebug($"Dictionary에 키 '{key}'이(가) 존재하지 않아 제거할 수 없습니다.");
                    removeSuccess = false;
                }
            }
            catch (Exception ex) // Remove에서 예외 발생 가능성 낮지만 안전하게 처리
            {
                Logger?.LogError(ex, $"Dictionary Remove 작업 중 오류 발생: {ex.Message}");
                removeSuccess = false;
            }
        }
        else {
            if (dictionaryValue == null) Logger?.LogWarning("DictionaryInput 값이 null입니다.");
            if (key == null) Logger?.LogWarning("KeyInput 값이 null입니다.");
            if (dictionaryValue != null && !(dictionaryValue is IDictionary)) Logger?.LogError("DictionaryInput 값이 IDictionary가 아닙니다.");
            removeSuccess = false;
        }

        // 결과 Output 설정
        RemoveSuccessOutput.Value      = removeSuccess; // RemoveSuccessOutput은 OutputPort<bool> 타입이므로 .Value 사용
        _updatedDictionaryOutput.Value = dictionary;    // 수정된(또는 원본) Dictionary 설정

        // 성공/실패에 따라 다른 Flow 출력
        if (removeSuccess)
            yield return SuccessOut;
        else
            yield return FailureOut;
    }
}