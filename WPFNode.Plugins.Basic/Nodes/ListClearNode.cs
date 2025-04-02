using System;
using System.Collections; // Added for IList
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models; // Assuming GenericInputPort is here
using WPFNode.Models.Execution;
// using WPFNode.Models.Properties; // NodeProperty<Type> is removed
using WPFNode.Interfaces;
using Microsoft.Extensions.Logging; // Added for logging

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("List.Clear")]
    [NodeDescription("리스트의 모든 항목을 제거합니다.")]
    [NodeCategory("컬렉션")]
    public class ListClearNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; private set; }

        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; private set; }

        // ElementType property is removed

        // ListInput: GenericInputPort 타입으로 선언.
        [NodeInput("리스트", ConnectionStateChangedCallback = nameof(ListInput_ConnectionChanged))]
        public GenericInputPort ListInput { get; private set; }

        // ResultOutput: Configure에서 동적으로 관리됨.
        private IOutputPort _resultOutput;

        public ListClearNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "List.Clear";
            Description = "리스트의 모든 항목을 제거합니다. 리스트 입력 타입에 따라 결과 타입을 자동으로 결정합니다.";
        }

        // ListInput의 연결 상태 또는 타입이 변경될 때 호출될 콜백 메서드
        private void ListInput_ConnectionChanged(IInputPort port)
        {
            Logger?.LogDebug($"ListInput 포트 상태 변경됨 (연결 또는 타입).");
            ReconfigurePorts(); // ResultOutput 재구성을 위해 호출
        }

        // Configure 메서드는 동적으로 관리해야 하는 포트만 정의
        protected override void Configure(NodeBuilder builder)
        {
            // ListInput은 Attribute로 정의되었으므로 builder로 다시 정의하지 않음.

            // ResultOutput의 타입을 결정
            Type listType = typeof(IList); // 기본값

            // ListInput의 현재 결정된 타입을 확인
            if (ListInput != null && ListInput.CurrentResolvedType != null && ListInput.CurrentResolvedType != typeof(object))
            {
                listType = ListInput.CurrentResolvedType;
                Logger?.LogDebug($"ListInput 타입({listType.Name}) 기반. Output ListType: {listType.Name} 사용.");
            }
            else
            {
                Logger?.LogDebug("ListInput 타입 불명확. Output ListType: IList 사용.");
            }

            // NodeBuilder를 사용하여 ResultOutput을 동적으로 정의/재정의
            _resultOutput = builder.Output("결과", listType);
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            // ListInput (GenericInputPort) 에서 값을 가져옴
            // GetValueOrDefault(Type) 사용, IList로 캐스팅
            var listValue = ListInput?.Value;

            if (listValue is IList list) // IList 인터페이스로 작업
            {
                try
                {
                    list.Clear();
                    Logger?.LogDebug($"리스트의 모든 항목을 제거했습니다.");
                }
                catch (NotSupportedException ex) // ReadOnly 리스트 등 Clear 미지원 시
                {
                     Logger?.LogError(ex, $"리스트 Clear 작업 실패: {ex.Message}");
                     // Clear 실패 시 원본 리스트를 그대로 반환할 수 있음
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, $"리스트 Clear 중 오류 발생: {ex.Message}");
                }
            }
            else
            {
                 Logger?.LogError("ListInput 값이 IList가 아닙니다.");
                 // IList가 아닌 경우 입력값을 그대로 반환할 수 있음
            }

            // IOutputPort.Value 속성 사용
            _resultOutput.Value = listValue; // 이미 .Value 사용 중, 변경 없음

            yield return FlowOut;
        }
    }
}
