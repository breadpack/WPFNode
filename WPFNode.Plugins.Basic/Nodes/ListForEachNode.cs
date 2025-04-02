using System;
using System.Collections;
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
    [NodeName("List.ForEach")]
    [NodeDescription("리스트의 각 항목을 순회합니다.")]
    [NodeCategory("컬렉션")]
    public class ListForEachNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; private set; }

        [NodeFlowOut("Item")]
        public IFlowOutPort ItemFlowOut { get; private set; }

        [NodeFlowOut("Complete")]
        public IFlowOutPort CompleteFlowOut { get; private set; }

        // ElementType property is removed

        // ListInput: GenericInputPort 타입으로 선언.
        [NodeInput("리스트", ConnectionStateChangedCallback = nameof(ListInput_ConnectionChanged))]
        public GenericInputPort ListInput { get; private set; }

        // CurrentItem: Configure에서 동적으로 관리됨.
        private IOutputPort _currentItem;
        // IndexOutput 추가 (필요 시)
        [NodeOutput("Index")]
        public OutputPort<int> IndexOutput { get; private set; } // Index Output 추가

        public ListForEachNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "List.ForEach";
            Description = "리스트의 각 항목을 순회합니다. 리스트 입력 타입에 따라 현재 항목 타입을 자동으로 결정합니다.";
        }

        // ListInput의 연결 상태 또는 타입이 변경될 때 호출될 콜백 메서드
        private void ListInput_ConnectionChanged(IInputPort port)
        {
            Logger?.LogDebug($"ListInput 포트 상태 변경됨 (연결 또는 타입).");
            ReconfigurePorts(); // CurrentItem 재구성을 위해 호출
        }

        // Configure 메서드는 동적으로 관리해야 하는 포트만 정의
        protected override void Configure(NodeBuilder builder)
        {
            // ListInput, FlowIn, FlowOut, IndexOutput 포트들은 Attribute로 정의되었으므로 builder로 다시 정의하지 않음.

            // CurrentItem Output 포트의 타입을 결정
            Type elementType = typeof(object); // 기본값

            // ListInput의 현재 결정된 타입을 확인하고 요소 타입 추출
            if (ListInput != null && ListInput.CurrentResolvedType != null && ListInput.CurrentResolvedType != typeof(object))
            {
                var listType = ListInput.CurrentResolvedType;
                elementType = listType.GetElementType() ?? typeof(object); // 요소 타입 추출
                Logger?.LogDebug($"ListInput 타입({listType.Name}) 기반. CurrentItem Type: {elementType.Name} 사용.");
            }
            else
            {
                Logger?.LogDebug("ListInput 타입 불명확. CurrentItem Type: object 사용.");
            }

            // NodeBuilder를 사용하여 CurrentItem Output 포트를 동적으로 정의/재정의
            _currentItem = builder.Output("현재 항목", elementType);
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            // ListInput (GenericInputPort) 에서 값을 가져옴
            // GetValueOrDefault(Type) 사용, IEnumerable로 캐스팅
            var listValue = ListInput.Value;

            if (listValue is not IEnumerable items) // IEnumerable 인터페이스로 작업
            {
                Logger?.LogWarning("ListInput 값이 null이거나 IEnumerable이 아닙니다.");
                yield return CompleteFlowOut;
                yield break;
            }

            // 각 항목 순회
            int index = 0;
            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger?.LogInformation("ListForEach 작업 취소됨.");
                    yield break;
                }

                // 현재 항목 정보 설정 (타입은 Configure에서 맞춰짐)
                // IOutputPort.Value 속성 사용
                if (_currentItem != null) _currentItem.Value = item;
                // IndexOutput 값 설정
                IndexOutput.Value = index; // .Value 속성 사용

                // 항목 플로우 활성화
                yield return ItemFlowOut;
                index++; // 인덱스 증가
            }

            // 모든 항목 처리 완료 후 Complete 플로우 활성화
            Logger?.LogDebug($"리스트 순회 완료. 총 {index}개 항목 처리.");
            yield return CompleteFlowOut;
        }
    }
}
