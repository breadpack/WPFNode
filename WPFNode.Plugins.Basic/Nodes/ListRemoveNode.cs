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
    [NodeName("List.Remove")]
    [NodeDescription("리스트에서 항목을 제거합니다.")]
    [NodeCategory("컬렉션")]
    public class ListRemoveNode : DynamicNode
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; set; }
        
        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; set; }
        
        [NodeProperty("요소 타입", OnValueChanged = nameof(ElementType_Changed))]
        public NodeProperty<Type> ElementType { get; set; }
        
        private IInputPort _listInput;
        private IInputPort _itemInput;
        private IOutputPort _resultOutput;
        
        public ListRemoveNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "List.Remove";
            Description = "리스트에서 항목을 제거합니다.";
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
            _itemInput = builder.Input("항목", elementType);
            _resultOutput = builder.Output("결과", listType);
        }
        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            FlowExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            // InputPort<T>.GetValueOrDefault()를 사용하여 값 가져오기
            var listValue = ((dynamic)_listInput).GetValueOrDefault();
            if (listValue == null)
            {
                yield return FlowOut;
                yield break;
            }
            
            var listType = listValue.GetType();
            
            // 항목 제거 - 원본 리스트 직접 수정 (참조 유지)
            var itemValue = ((dynamic)_itemInput).GetValueOrDefault();
            if (itemValue != null)
            {
                var removeMethod = listType.GetMethod("Remove");
                removeMethod?.Invoke(listValue, new[] { itemValue });
            }
            
            // 원본 리스트 참조를 그대로 출력
            ((dynamic)_resultOutput).Value = listValue;
            
            System.Diagnostics.Debug.WriteLine($"ListRemoveNode: 리스트 참조 유지, HashCode: {listValue.GetHashCode()}");
            
            yield return FlowOut;
        }
    }
}
