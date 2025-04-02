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

[NodeName("Dictionary.Add")]
[NodeDescription("Dictionary에 키-값 쌍을 추가하거나 업데이트합니다.")]
[NodeCategory("컬렉션")]
public class DictionaryAddNode : NodeBase {
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; private set; }

    [NodeFlowOut]
    public IFlowOutPort FlowOut { get; private set; }

    // KeyType, ValueType properties are removed

    // DictionaryInput: GenericInputPort 타입으로 선언.
    [NodeInput("Dictionary", ConnectionStateChangedCallback = nameof(DictionaryInput_ConnectionChanged))]
    public GenericInputPort DictionaryInput { get; private set; }

    // KeyInput, ValueInput, UpdatedDictionaryOutput: Configure에서 동적으로 관리됨.
    private IInputPort  _keyInput;
    private IInputPort  _valueInput;
    private IOutputPort _updatedDictionaryOutput;

    public DictionaryAddNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Name        = "Dictionary.Add";
        Description = "Dictionary에 키-값 쌍을 추가하거나 업데이트합니다. Dictionary 입력 타입에 따라 키/값 타입을 자동으로 결정합니다.";
    }

    // DictionaryInput의 연결 상태 또는 타입이 변경될 때 호출될 콜백 메서드
    private void DictionaryInput_ConnectionChanged(IInputPort port) {
        Logger?.LogDebug($"DictionaryInput 포트 상태 변경됨 (연결 또는 타입).");
        ReconfigurePorts(); // KeyInput, ValueInput, UpdatedDictionaryOutput 재구성을 위해 호출
    }

    // Configure 메서드는 동적으로 관리해야 하는 포트만 정의
    protected override void Configure(NodeBuilder builder) {
        // DictionaryInput은 Attribute로 정의되었으므로 builder로 다시 정의하지 않음.

        // KeyInput, ValueInput, UpdatedDictionaryOutput의 타입을 결정
        Type dictType  = typeof(IDictionary); // 기본값
        Type keyType   = typeof(object);      // 기본값
        Type valueType = typeof(object);      // 기본값

        // DictionaryInput의 현재 결정된 타입을 확인하고 Key/Value 타입 추출
        if (DictionaryInput != null && DictionaryInput.CurrentResolvedType != null && DictionaryInput.CurrentResolvedType != typeof(object)) {
            dictType = DictionaryInput.CurrentResolvedType;
            // 유틸리티 함수를 사용하여 Key/Value 타입 추출 시도 (using WPFNode.Extensions; 필요)
            (keyType, valueType) = dictType.GetDictionaryKeyValueTypes() ?? (typeof(object), typeof(object));
            Logger?.LogDebug($"DictionaryInput 타입({dictType.Name}) 기반. KeyType: {keyType.Name}, ValueType: {valueType.Name} 사용.");
        }
        else {
            Logger?.LogDebug("DictionaryInput 타입 불명확. KeyType: object, ValueType: object, Output DictType: IDictionary 사용.");
        }

        // NodeBuilder를 사용하여 KeyInput, ValueInput, UpdatedDictionaryOutput을 동적으로 정의/재정의
        _keyInput                = builder.Input("Key", keyType);
        _valueInput              = builder.Input("Value", valueType);
        _updatedDictionaryOutput = builder.Output("Updated", dictType);
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken  cancellationToken = default
    ) {
        // DictionaryInput (GenericInputPort) 에서 값을 가져옴
        // GetValueOrDefault(Type) 사용, IDictionary로 캐스팅
        var dictionaryValue = DictionaryInput.Value;

        if (dictionaryValue is not IDictionary dictionary) // IDictionary 인터페이스로 작업
        {
            Logger?.LogError("DictionaryInput 값이 null이거나 IDictionary가 아닙니다.");
            // IOutputPort.Value 사용
            _updatedDictionaryOutput.Value = dictionaryValue;
            yield return FlowOut;
            yield break;
        }

        // KeyInput, ValueInput (동적으로 생성된 InputPort<T>) 에서 값을 가져옴
        // dynamic 캐스팅 사용
        var key   = _keyInput.Value;
        var value = _valueInput.Value;

        if (key == null) {
            Logger?.LogWarning("KeyInput 값이 null입니다. Dictionary를 수정할 수 없습니다.");
            // IOutputPort.Value 사용
            _updatedDictionaryOutput.Value = dictionary; // 원본 반환
            yield return FlowOut;
            yield break;
        }

        try {
            // IDictionary 인터페이스 사용 (타입 안전성은 Add/set_Item 구현에 따라 다름)
            dictionary[key] = value; // Add 또는 Update 동작
            Logger?.LogDebug($"Dictionary에 키 '{key}' (Type: {key.GetType().Name})와 값 '{value}' (Type: {value?.GetType().Name ?? "null"})을(를) 추가/업데이트했습니다.");
        }
        catch (ArgumentException ex) // 키 타입 불일치 등
        {
            Logger?.LogError(ex, $"Dictionary Add/Update 중 인수 오류 발생: {ex.Message}");
        }
        catch (Exception ex) {
            Logger?.LogError(ex, $"Dictionary Add/Update 중 오류 발생: {ex.Message}");
        }

        // 결과 포트에 수정된 Dictionary 설정 (참조 유지)
        // IOutputPort.Value 속성 사용
        _updatedDictionaryOutput.Value = dictionary;

        yield return FlowOut;
    }
}