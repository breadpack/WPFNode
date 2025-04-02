using System;
using System.Collections; // Added for IDictionary, DictionaryEntry
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

[NodeName("Dictionary.ForEach")]
[NodeDescription("Dictionary의 모든 키-값 쌍을 순회합니다.")]
[NodeCategory("컬렉션")]
public class DictionaryForEachNode : NodeBase {
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; private set; }

    [NodeFlowOut("Item")]
    public IFlowOutPort ItemOut { get; private set; }

    [NodeFlowOut("Complete")]
    public IFlowOutPort CompleteOut { get; private set; }

    // KeyType, ValueType properties are removed

    // DictionaryInput: GenericInputPort 타입으로 선언.
    [NodeInput("Dictionary", ConnectionStateChangedCallback = nameof(DictionaryInput_ConnectionChanged))]
    public GenericInputPort DictionaryInput { get; private set; }

    // CurrentKeyOutput, CurrentValueOutput: Configure에서 동적으로 관리됨.
    private IOutputPort _currentKeyOutput;

    private IOutputPort _currentValueOutput;

    // IndexOutput: int 타입으로 고정.
    [NodeOutput("Index")]
    public OutputPort<int> IndexOutput { get; private set; }

    public DictionaryForEachNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Name        = "Dictionary.ForEach";
        Description = "Dictionary의 모든 키-값 쌍을 순회합니다. Dictionary 입력 타입에 따라 키/값 타입을 자동으로 결정합니다.";
    }

    // DictionaryInput의 연결 상태 또는 타입이 변경될 때 호출될 콜백 메서드
    private void DictionaryInput_ConnectionChanged(IInputPort port) {
        Logger?.LogDebug($"DictionaryInput 포트 상태 변경됨 (연결 또는 타입).");
        ReconfigurePorts(); // CurrentKeyOutput, CurrentValueOutput 재구성을 위해 호출
    }

    // Configure 메서드는 동적으로 관리해야 하는 포트만 정의
    protected override void Configure(NodeBuilder builder) {
        // DictionaryInput, IndexOutput 등 Attribute로 정의된 포트는 builder로 다시 정의하지 않음.

        // CurrentKeyOutput과 CurrentValueOutput의 타입을 결정
        Type keyType   = typeof(object); // 기본값
        Type valueType = typeof(object); // 기본값

        // DictionaryInput의 현재 결정된 타입을 확인하고 Key/Value 타입 추출
        if (DictionaryInput.CurrentResolvedType != null && DictionaryInput.CurrentResolvedType != typeof(object)) {
            var dictType = DictionaryInput.CurrentResolvedType;
            // 유틸리티 함수를 사용하여 Key/Value 타입 추출 시도 (using WPFNode.Extensions; 필요)
            (keyType, valueType) = dictType.GetDictionaryKeyValueTypes() ?? (typeof(object), typeof(object));
            Logger?.LogDebug($"DictionaryInput 타입({dictType.Name}) 기반. KeyType: {keyType.Name}, ValueType: {valueType.Name} 사용.");
        }
        else {
            Logger?.LogDebug("DictionaryInput 타입 불명확. KeyType: object, ValueType: object 사용.");
        }

        // NodeBuilder를 사용하여 CurrentKeyOutput과 CurrentValueOutput을 동적으로 정의/재정의
        _currentKeyOutput   = builder.Output("CurrentKey", keyType);
        _currentValueOutput = builder.Output("CurrentValue", valueType);
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
            Logger?.LogWarning("DictionaryInput 값이 null이거나 IDictionary가 아닙니다.");
            yield return CompleteOut;
            yield break;
        }

        int index = 0;

        // IDictionary의 각 항목을 순회 (DictionaryEntry 사용)
        foreach (DictionaryEntry entry in dictionary) {
            if (cancellationToken.IsCancellationRequested) {
                Logger?.LogInformation("DictionaryForEach 작업 취소됨.");
                yield break;
            }

            // 현재 항목 정보 설정 (타입은 Configure에서 맞춰짐)
            // IOutputPort.Value 속성 사용
            if (_currentKeyOutput != null) _currentKeyOutput.Value     = entry.Key;
            if (_currentValueOutput != null) _currentValueOutput.Value = entry.Value;
            // OutputPort<int>.Value 속성 사용
            IndexOutput.Value = index; // index++ 대신 index 사용 후 증가

            // 항목 처리 Flow 출력
            yield return ItemOut;
        }

        // 순회 완료
        Logger?.LogDebug($"Dictionary 순회 완료. 총 {index}개 항목 처리.");
        yield return CompleteOut;
    }
}