using System;
using System.Collections; // Added for IList
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models; // Assuming GenericInputPort is in WPFNode.Models
using WPFNode.Models.Execution;
using WPFNode.Interfaces;
using WPFNode.Utilities; // Added for GetElementType
using Microsoft.Extensions.Logging; // Added for logging

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("List.Add")]
    [NodeDescription("리스트에 항목을 추가합니다.")]
    [NodeCategory("컬렉션")]
    public class ListAddNode : NodeBase // Keep the original class name
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; set; }

        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; set; }

        // ElementType property is removed

        // ListInput: GenericInputPort 타입으로 선언. 연결된 소스에 따라 타입 결정.
        [NodeInput("리스트", ConnectionStateChangedCallback = nameof(ListInput_ConnectionChanged))]
        public GenericInputPort ListInput { get; private set; } // Attribute initializes as GenericInputPort

        // ItemInput: Configure에서 동적으로 관리됨.
        private IInputPort _itemInput;
        // ResultOutput: Configure에서 동적으로 관리됨.
        private IOutputPort _resultOutput;

        public ListAddNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "List.Add";
        }

        // ListInput의 연결 상태 또는 타입이 변경될 때 호출될 콜백 메서드
        private void ListInput_ConnectionChanged(IInputPort port) // IInputPort로 받음
        {
            // GenericInputPort의 ConnectedType 변경도 PropertyChanged를 통해 감지됨
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
            if (ListInput.CurrentResolvedType != null && ListInput.CurrentResolvedType != typeof(object))
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
            // dynamic 캐스팅 사용 유지 (IInputPort 타입이므로)
            var itemValue = _itemInput.Value;
            if (itemValue != null)
            {
                try
                {
                    // IList.Add는 object를 받음
                    list.Add(itemValue);
                    Logger?.LogDebug($"항목 '{itemValue}' (Type: {itemValue.GetType().Name})을(를) 리스트에 추가했습니다.");
                }
                catch (ArgumentException ex) // 잘못된 타입 추가 시 발생 가능
                {
                     Logger?.LogError(ex, $"리스트에 항목 추가 시 타입 오류 발생: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, $"항목 추가 중 오류 발생: {ex.Message}");
                }
            }
            else
            {
                 Logger?.LogWarning("ItemInput 값이 null입니다.");
            }

            // 결과 포트에 수정된 리스트 설정
            // IOutputPort.Value 속성 사용
            _resultOutput.Value = list;

            yield return FlowOut;
        }
    }
}
