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
    public class ListClearNode : DynamicNode
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
        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            FlowExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            // 입력 리스트 타입을 가져옴
            var elementType = ElementType?.Value ?? typeof(object);
            var listType = typeof(List<>).MakeGenericType(elementType);
            
            // 빈 리스트 생성
            var resultList = Activator.CreateInstance(listType);
            
            // 결과 출력
            ((dynamic)_resultOutput).Value = resultList;
            
            yield return FlowOut;
        }
    }
}
