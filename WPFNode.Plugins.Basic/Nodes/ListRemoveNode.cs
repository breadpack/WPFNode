using System;
using System.Collections; // Added for IList
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models; // Assuming GenericInputPort is here
using WPFNode.Models.Execution;
// using WPFNode.Models.Properties; // NodeProperty<Type> is removed
using WPFNode.Interfaces;
using WPFNode.Utilities; // Added for GetElementType
using Microsoft.Extensions.Logging; // Added for logging

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("List.Remove")]
    [NodeDescription("리스트에서 항목을 제거합니다.")]
    [NodeCategory("컬렉션")]
    public class ListRemoveNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; private set; }

        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; private set; }

        // ElementType property is removed

        // ListInput: GenericInputPort 타입으로 선언.
        [NodeInput("리스트", ConnectionStateChangedCallback = nameof(ListInput_ConnectionChanged))]
        public GenericInputPort ListInput { get; private set; }

        // ItemInput: Configure에서 동적으로 관리됨.
        private IInputPort _itemInput;
        // ResultOutput: Configure에서 동적으로 관리됨.
        private IOutputPort _resultOutput;

        public ListRemoveNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "List.Remove";
            Description = "리스트에서 항목을 제거합니다. 리스트 입력 타입에 따라 항목 타입을 자동으로 결정합니다.";
        }

        // ListInput의 연결 상태 또는 타입이 변경될 때 호출될 콜백 메서드
        private void ListInput_ConnectionChanged(IInputPort port)
        {
            Logger?.LogDebug($"ListInput 포트 상태 변경됨 (연결 또는 타입).");
            ReconfigurePorts(); // ItemInput과 ResultOutput 재구성을 위해 호출
        }

        // Configure 메서드는 동적으로 관리해야 하는 포트만 정의
        protected override void Configure(NodeBuilder builder)
        {
            // ListInput은 Attribute로 정의되었으므로 builder로 다시 정의하지 않음.

            // ItemInput과 ResultOutput의 타입을 결정
            Type listType = typeof(IList); // 기본값
            Type elementType = typeof(object); // 기본값

            // ListInput의 현재 결정된 타입을 확인
            if (ListInput != null && ListInput.CurrentResolvedType != null && ListInput.CurrentResolvedType != typeof(object))
            {
                listType = ListInput.CurrentResolvedType;
                elementType = listType.GetElementType() ?? typeof(object); // 요소 타입 추출
                Logger?.LogDebug($"ListInput 타입({listType.Name}) 기반. ItemType: {elementType.Name}, Output ListType: {listType.Name} 사용.");
            }
            else
            {
                Logger?.LogDebug("ListInput 타입 불명확. ItemType: object, Output ListType: IList 사용.");
            }

            // NodeBuilder를 사용하여 ItemInput과 ResultOutput을 동적으로 정의/재정의
            _itemInput = builder.Input("항목", elementType);
            _resultOutput = builder.Output("결과", listType);
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            // ListInput (GenericInputPort) 에서 값을 가져옴
            // GetValueOrDefault(Type) 사용, IList로 캐스팅
            var listValue = ListInput.Value;
            if (listValue is not IList list)
            {
                Logger?.LogError("ListInput 값이 null이거나 IList가 아닙니다.");
                // IOutputPort.Value 사용
                _resultOutput.Value = listValue;
                yield return FlowOut;
                yield break;
            }

            // ItemInput (동적으로 생성된 InputPort<T>) 에서 값을 가져옴
            // GetValueOrDefault<object>() 사용
            var itemValue = _itemInput?.Value;

            if (itemValue != null)
            {
                try
                {
                    // IList.Remove는 object를 받음
                    list.Remove(itemValue);
                    Logger?.LogDebug($"항목 '{itemValue}' (Type: {itemValue.GetType().Name})을(를) 리스트에서 제거했습니다.");
                }
                catch (Exception ex) // Remove는 보통 예외를 던지지 않지만 안전하게 처리
                {
                    Logger?.LogError(ex, $"항목 제거 중 오류 발생: {ex.Message}");
                }
            }
            else
            {
                 Logger?.LogWarning("ItemInput 값이 null입니다.");
            }

            // 결과 포트에 수정된 리스트 설정 (참조 유지)
            // IOutputPort.Value 속성 사용
            _resultOutput.Value = list;

            yield return FlowOut;
        }
    }
}
