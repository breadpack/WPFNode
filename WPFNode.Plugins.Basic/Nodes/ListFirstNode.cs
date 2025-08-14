using System.Collections;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Plugins.Basic.Nodes {

    [NodeName("List.First")]
    [NodeDescription("리스트의 첫번째 항목을 가져옵니다.")]
    [NodeCategory("컬렉션")]
    public class ListFirstNode : NodeBase {
        
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; set; }

        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; set; }

        // ElementType property is removed

        // ListInput: GenericInputPort 타입으로 선언. 연결된 소스에 따라 타입 결정.
        [NodeInput("리스트", ConnectionStateChangedCallback = nameof(ListInput_ConnectionChanged))]
        public GenericInputPort ListInput { get; private set; } // Attribute initializes as GenericInputPort

        
        // ResultOutput: Configure에서 동적으로 관리됨.
        private IOutputPort _resultOutput;

        public ListFirstNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
            Name = "List.First";
        }
        
        // Configure 메서드는 동적으로 관리해야 하는 포트만 정의
        protected override void Configure(NodeBuilder builder)
        {
            // ListInput은 Attribute로 정의되었으므로 builder로 다시 정의하지 않음.

            // ItemInput과 ResultOutput의 타입을 결정
            Type listType    = typeof(IList);  // 기본값
            Type elementType = typeof(object); // 기본값

            // ListInput의 현재 결정된 타입을 확인
            if (ListInput.CurrentResolvedType != null && ListInput.CurrentResolvedType != typeof(object))
            {
                listType    = ListInput.CurrentResolvedType;
                elementType = listType.GetElementType() ?? typeof(object); // 요소 타입 추출
                Logger?.LogDebug($"ListInput 타입({listType.Name}) 기반. ItemType: {elementType.Name}, Output ListType: {listType.Name} 사용.");
            }
            else
            {
                Logger?.LogDebug("ListInput 타입 불명확. ItemType: object, Output ListType: IList 사용.");
            }

            // NodeBuilder를 사용하여 ItemInput과 ResultOutput을 동적으로 정의/재정의
            _resultOutput = builder.Output("결과", elementType);
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(IExecutionContext? context, 
                                                                    CancellationToken cancellationToken = default) {
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

            // 결과 포트에 수정된 리스트 설정
            // IOutputPort.Value 속성 사용
            _resultOutput.Value = list.Count > 0 ? list[0] : null;
            yield return FlowOut;
        }
        
        
        // ListInput의 연결 상태 또는 타입이 변경될 때 호출될 콜백 메서드
        private void ListInput_ConnectionChanged(IInputPort port) // IInputPort로 받음
        {
            // GenericInputPort의 ConnectedType 변경도 PropertyChanged를 통해 감지됨
            Logger?.LogDebug($"ListInput 포트 상태 변경됨 (연결 또는 타입).");
            ReconfigurePorts(); // ItemInput과 ResultOutput 재구성을 위해 호출
        }
    }
}