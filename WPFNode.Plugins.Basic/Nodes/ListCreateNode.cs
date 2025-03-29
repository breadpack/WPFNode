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
    [NodeName("List.Create")]
    [NodeDescription("빈 리스트를 생성합니다.")]
    [NodeCategory("컬렉션")]
    public class ListCreateNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; set; }
        
        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; set; }
        
        [NodeProperty("요소 타입", OnValueChanged = nameof(ElementType_Changed))]
        public NodeProperty<Type> ElementType { get; set; }
        
        private IOutputPort _listOutput;
        
        public ListCreateNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "List.Create";
            Description = "빈 리스트를 생성합니다.";
        }
        
        private void ElementType_Changed()
        {
            ReconfigurePorts();
        }
        
        protected override void Configure(NodeBuilder builder)
        {
            var elementType = ElementType?.Value ?? typeof(object);
            var listType = typeof(List<>).MakeGenericType(elementType);
            
            _listOutput = builder.Output("리스트", listType);
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            FlowExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            var elementType = ElementType?.Value ?? typeof(object);
            var listType = typeof(List<>).MakeGenericType(elementType);
            
            // 빈 리스트 생성
            var list = Activator.CreateInstance(listType);
            ((dynamic)_listOutput).Value = list;
            
            yield return FlowOut;
        }
    }
}
