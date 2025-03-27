using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;
using WPFNode.Interfaces;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("List.ForEach")]
    [NodeDescription("리스트의 각 항목을 순회합니다.")]
    [NodeCategory("컬렉션")]
    public class ListForEachNode : DynamicNode
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; set; }
        
        [NodeFlowOut("Item")]
        public IFlowOutPort ItemFlowOut { get; set; }
        
        [NodeFlowOut("Complete")]
        public IFlowOutPort CompleteFlowOut { get; set; }
        
        [NodeProperty("요소 타입", OnValueChanged = nameof(ElementType_Changed))]
        public NodeProperty<Type> ElementType { get; set; }
        
        private IInputPort _listInput;
        private IOutputPort _currentItem;
        
        public ListForEachNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "List.ForEach";
            Description = "리스트의 각 항목을 순회합니다.";
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
            _currentItem = builder.Output("현재 항목", elementType);
        }
        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            FlowExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            var listValue = ((dynamic)_listInput).GetValueOrDefault();
            if (listValue == null)
            {
                yield return CompleteFlowOut;
                yield break;
            }
            
            // IEnumerable로 변환하여 각 항목 처리
            IEnumerable items = listValue as IEnumerable;
            if (items == null)
            {
                yield return CompleteFlowOut;
                yield break;
            }
            
            // 각 항목 순회
            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                
                // 현재 항목 설정
                ((dynamic)_currentItem).Value = item;
                
                // 항목 플로우 활성화
                yield return ItemFlowOut;
            }
            
            // 모든 항목 처리 완료 후 Complete 플로우 활성화
            yield return CompleteFlowOut;
        }
    }
}
