using System;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;
using WPFNode.Interfaces;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("List.Clear")]
    [NodeDescription("리스트의 모든 항목을 제거합니다.")]
    [NodeCategory("컬렉션")]
    public class ListClearNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; set; }
        
        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; set; }
        
        [NodeProperty("요소 타입", OnValueChanged = nameof(ElementType_Changed))]
        public NodeProperty<Type> ElementType { get; set; }
        
        private IInputPort _listInput;
        private IOutputPort _resultOutput;
        
        public ListClearNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "List.Clear";
            Description = "리스트의 모든 항목을 제거합니다.";
        }
        
        private void ElementType_Changed()
        {
            ReconfigurePorts();
        }
        
        protected override void Configure(NodeBuilder builder)
        {
            var elementType = ElementType?.Value ?? typeof(object);
            var listType = typeof(List<>).MakeGenericType(elementType);
            
            _listInput = builder.Input("리스트", listType);
            _resultOutput = builder.Output("결과", listType);
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            FlowExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            // InputPort<T>.GetValueOrDefault()를 사용하여 값 가져오기
            var listValue = ((dynamic)_listInput).GetValueOrDefault();
            if (listValue == null)
            {
                // 입력 리스트가 없을 경우 새 리스트 생성
                var elementType = ElementType?.Value ?? typeof(object);
                var listType = typeof(List<>).MakeGenericType(elementType);
                listValue = Activator.CreateInstance(listType);
            }
            else
            {
                // 원본 리스트의 Clear 메서드 직접 호출 (참조 유지)
                var clearMethod = listValue.GetType().GetMethod("Clear");
                clearMethod?.Invoke(listValue, null);
            }
            
            // 원본 리스트 참조를 그대로 출력
            ((dynamic)_resultOutput).Value = listValue;
            
            System.Diagnostics.Debug.WriteLine($"ListClearNode: 리스트 참조 유지, HashCode: {listValue.GetHashCode()}");
            
            yield return FlowOut;
        }
    }
}
